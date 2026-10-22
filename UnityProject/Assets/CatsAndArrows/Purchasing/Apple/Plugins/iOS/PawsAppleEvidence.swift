import Foundation
import Security
import StoreKit
import CryptoKit

// Issue #64. No networking/telemetry, account identifiers or secrets. StoreKit supplies
// verified evidence; Keychain preserves that evidence for offline coordinator restart.
// This is client-only protection, not a defence against a compromised device/process.
private struct PawsEvidenceRecord: Codable {
    let reference: String
    let product: String
    let bundle: String
    let transaction: String
    let jws: String
    let review: Bool
}

private func vaultKey(_ product: String, _ review: Bool) -> String { (review ? "review:" : "production:") + product }

private enum PawsVault {
    static var service: String { (Bundle.main.bundleIdentifier ?? "") + ".iap-evidence-v1" }
    static func query(_ key: String) -> [String: Any] {
        [kSecClass as String: kSecClassGenericPassword,
         kSecAttrService as String: service, kSecAttrAccount as String: key,
         kSecAttrSynchronizable as String: false]
    }
    static func read(_ key: String) -> (OSStatus, Data?) {
        var q = query(key)
        q[kSecReturnData as String] = true
        q[kSecMatchLimit as String] = kSecMatchLimitOne
        var result: CFTypeRef?
        let status = SecItemCopyMatching(q as CFDictionary, &result)
        return (status, result as? Data)
    }
    static func write(_ key: String, _ data: Data) -> Bool {
        let q = query(key)
        let updated = SecItemUpdate(q as CFDictionary, [kSecValueData as String: data] as CFDictionary)
        if updated == errSecSuccess { return true }
        guard updated == errSecItemNotFound else { return false }
        var insert = q
        insert[kSecValueData as String] = data
        insert[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        return SecItemAdd(insert as CFDictionary, nil) == errSecSuccess
    }
    // Tombstone rather than delete: a revoked reference must not be resurrected by
    // recovering an older coordinator envelope or by an ordinary ambiguous refresh.
    static func invalidate(_ product: String) -> Bool { write(product, Data()) }
    static func record(_ product: String) -> PawsEvidenceRecord? {
        let (_, data) = read(product)
        guard let data = data else { return nil }
        return try? JSONDecoder().decode(PawsEvidenceRecord.self, from: data)
    }
}

@_cdecl("PawsIapVerifyCache")
public func pawsIapVerifyCache(_ productPointer: UnsafePointer<CChar>, _ referencePointer: UnsafePointer<CChar>, _ review: Bool) -> Bool {
    let product = String(cString: productPointer), reference = String(cString: referencePointer)
    guard let record = PawsVault.record(vaultKey(product, review)) else { return false }
    return !reference.isEmpty && record.reference == reference && record.product == product &&
        record.bundle == Bundle.main.bundleIdentifier && record.review == review && !record.jws.isEmpty
}

@_cdecl("PawsIapSetDeferred")
public func pawsIapSetDeferred(_ productPointer: UnsafePointer<CChar>, _ value: Bool, _ review: Bool) -> Bool {
    PawsVault.write(vaultKey(String(cString: productPointer), review) + ".deferred", Data([value ? 1 : 0]))
}

private struct PawsEvidenceSnapshot: Encodable {
    var state = 0 // Unknown=0, Owned=1, Revoked=2, NotOwned=3
    var reference = ""
    var transaction = ""
    var unfinished: [String] = []
    var confirmed: [String] = []
    var durable = false
    var deferred = false
    var complete = false
}

private enum PawsReads { static var latest: [String: Int64] = [:] }

@available(iOS 16.3, *)
private func valid(_ t: Transaction, _ product: String, _ review: Bool) -> Bool {
    PawsEvidencePolicy.acceptsEnvironment(review: review, production: t.environment == .production,
        sandbox: t.environment == .sandbox, xcode: t.environment == .xcode) &&
        t.productID == product && t.productType == .nonConsumable &&
        t.appBundleID == Bundle.main.bundleIdentifier && !t.isUpgraded && t.expirationDate == nil
}

@available(iOS 16.3, *)
@MainActor
private func snapshot(_ product: String, _ synchronized: Bool, _ review: Bool, _ request: Int64) async -> PawsEvidenceSnapshot {
    var result = PawsEvidenceSnapshot()
    let key = vaultKey(product, review)
    let (cacheStatus, cacheData) = PawsVault.read(key)
    let (pendingStatus, pendingData) = PawsVault.read(key + ".deferred")
    result.deferred = pendingStatus == errSecSuccess && pendingData?.first == 1
    // Locked/unavailable Keychain must not be confused with an empty fresh install.
    guard cacheStatus == errSecSuccess || cacheStatus == errSecItemNotFound,
          pendingStatus == errSecSuccess || pendingStatus == errSecItemNotFound else { return result }
    var owned: (Transaction, String)?
    var unverified = false
    for await entry in Transaction.currentEntitlements {
        switch entry {
        case .verified(let t):
            if valid(t, product, review) && t.revocationDate == nil { owned = (t, entry.jwsRepresentation) }
            if t.productID == product && !valid(t, product, review) { unverified = true }
        case .unverified(let t, _):
            if t.productID == product { unverified = true }
        }
    }
    for await entry in Transaction.unfinished {
        switch entry {
        case .verified(let t):
            if valid(t, product, review) && t.revocationDate == nil { result.unfinished.append(String(t.id)) }
            if t.productID == product && !valid(t, product, review) { unverified = true }
        case .unverified(let t, _):
            if t.productID == product { unverified = true }
        }
    }
    // Preserve idempotent finish after a crash between Apple's finish and the
    // coordinator journal commit. Only verified history for this exact product,
    // bundle/environment can establish that an opaque reference was finished.
    for await entry in Transaction.all {
        switch entry {
        case .verified(let t):
            if valid(t, product, review) && t.revocationDate == nil && !result.unfinished.contains(String(t.id)) {
                result.confirmed.append(String(t.id))
            }
            if t.productID == product && !valid(t, product, review) { unverified = true }
        case .unverified(let t, _):
            if t.productID == product { unverified = true }
        }
    }
    // A signed explicit refund remains authoritative even if currentEntitlements is empty.
    if let latest = await Transaction.latest(for: product) {
        guard PawsReads.latest[vaultKey(product, review)] == request else { return result }
        switch latest {
        case .verified(let t):
            if t.productID == product && !valid(t, product, review) { unverified = true }
            if valid(t, product, review) && PawsEvidencePolicy.latestRevokes(ownedID: owned?.0.id,
                latestID: t.id, revoked: t.revocationDate != nil) {
                if PawsVault.invalidate(key) { result.state = 2 }
                return result
            }
        case .unverified: unverified = true
        }
    }
    guard PawsReads.latest[vaultKey(product, review)] == request else { return result }
    result.complete = !unverified
    if let (t, jws) = owned {
        result.state = 1
        result.transaction = String(t.id)
        result.reference = SHA256.hash(data: Data(jws.utf8)).map { String(format: "%02x", $0) }.joined()
        let record = PawsEvidenceRecord(reference: result.reference, product: product,
            bundle: Bundle.main.bundleIdentifier ?? "", transaction: result.transaction, jws: jws, review: review)
        if let data = try? JSONEncoder().encode(record) {
            result.durable = PawsVault.write(key, data)
        }
        // Clear unpaid deferred marker only after verified ownership. Failure keeps
        // finalization retryable without replacing the coordinator's durable state.
        result.deferred = false
        _ = PawsVault.write(key + ".deferred", Data([0]))
        return result
    }
    guard !unverified else { return result }
    // Empty local inventory cannot revoke a prior owner while offline. Explicit
    // Restore succeeds through AppStore.sync before synchronized=true. With no
    // previous evidence, a clean local inventory permits a first purchase.
    if synchronized || cacheData == nil || cacheData?.isEmpty == true {
        if PawsVault.invalidate(key) { result.state = 3 }
    }
    return result
}

@_cdecl("PawsIapRead")
public func pawsIapRead(_ productPointer: UnsafePointer<CChar>, _ synchronized: Bool, _ review: Bool,
                       _ request: Int64, _ receive: @escaping @convention(c) (Int64, UnsafePointer<CChar>) -> Void) {
    let product = String(cString: productPointer)
    Task { @MainActor in
        PawsReads.latest[vaultKey(product, review)] = request
        let result = await snapshot(product, synchronized, review, request)
        let data = (try? JSONEncoder().encode(result)) ?? Data("{}".utf8)
        let json = String(data: data, encoding: .utf8) ?? "{}"
        json.withCString { receive(request, $0) }
    }
}
