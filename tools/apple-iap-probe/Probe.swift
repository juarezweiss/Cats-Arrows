import UIKit
import StoreKit
import CryptoKit

// #64: owner-operated diagnostic only. No Unity, entitlement cache, sync or finish.
@main
final class ProbeDelegate: UIResponder, UIApplicationDelegate {
    func application(_ application: UIApplication, didFinishLaunchingWithOptions options: [UIApplication.LaunchOptionsKey: Any]? = nil) -> Bool { true }
    func application(_ application: UIApplication, configurationForConnecting session: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {
        let configuration = UISceneConfiguration(name: "Probe", sessionRole: session.role)
        configuration.delegateClass = ProbeSceneDelegate.self
        return configuration
    }
}

@MainActor
final class ProbeSceneDelegate: UIResponder, UIWindowSceneDelegate {
    var window: UIWindow?
    private let recorder = ProbeRecorder()
    private var product: Product?
    private var observer: Task<Void, Never>?
    private let status = UILabel()
    private let purchaseButton = UIButton(type: .system)
    private let checkpointButton = UIButton(type: .system)
    private let approvedButton = UIButton(type: .system)

    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options: UIScene.ConnectionOptions) {
        guard let windowScene = scene as? UIWindowScene else { return }
        let controller = UIViewController()
        controller.view.backgroundColor = .systemBackground
        status.text = "Native StoreKit diagnostic. Preparing…"
        status.numberOfLines = 0
        status.accessibilityIdentifier = "probe-status"
        purchaseButton.setTitle("Start one test purchase", for: .normal)
        purchaseButton.accessibilityIdentifier = "probe-purchase"
        purchaseButton.isEnabled = false
        purchaseButton.addTarget(self, action: #selector(buy), for: .touchUpInside)
        checkpointButton.setTitle("Record before-approval checkpoint", for: .normal)
        checkpointButton.accessibilityIdentifier = "probe-checkpoint"
        checkpointButton.isEnabled = false
        checkpointButton.addTarget(self, action: #selector(checkpoint), for: .touchUpInside)
        approvedButton.setTitle("Record after-approval checkpoint", for: .normal)
        approvedButton.accessibilityIdentifier = "probe-approved-checkpoint"
        approvedButton.addTarget(self, action: #selector(afterApproval), for: .touchUpInside)
        let stack = UIStackView(arrangedSubviews: [status, purchaseButton, checkpointButton, approvedButton])
        stack.axis = .vertical
        stack.spacing = 24
        stack.translatesAutoresizingMaskIntoConstraints = false
        controller.view.addSubview(stack)
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo: controller.view.safeAreaLayoutGuide.leadingAnchor, constant: 24),
            stack.trailingAnchor.constraint(equalTo: controller.view.safeAreaLayoutGuide.trailingAnchor, constant: -24),
            stack.centerYAnchor.constraint(equalTo: controller.view.safeAreaLayoutGuide.centerYAnchor),
            purchaseButton.heightAnchor.constraint(greaterThanOrEqualToConstant: 48),
            checkpointButton.heightAnchor.constraint(greaterThanOrEqualToConstant: 48)
        ])
        let window = UIWindow(windowScene: windowScene)
        window.rootViewController = controller
        window.makeKeyAndVisible()
        self.window = window
        recorder.emit("launch", ["unitySDK": false, "pawsOwnershipLogic": false, "cacheAccess": false])
        observer = Task { @MainActor [weak self] in
            self?.recorder.emit("updates-listener-started")
            for await entry in StoreKit.Transaction.updates {
                guard let self else { return }
                self.recorder.transaction("updates", entry, reason: "observer")
                await self.snapshot("update")
            }
        }
        Task { @MainActor in
            await snapshot(recorder.phase == "ready" ? "baseline" : "restart-before-approval")
            do {
                product = try await Product.products(for: [recorder.productId]).first
                recorder.emit("product-fetch", ["available": product != nil])
                if recorder.phase == "ready" {
                    purchaseButton.isEnabled = product != nil && recorder.lastEntitlementCount == 0 && recorder.lastUnfinishedCount == 0 && recorder.lastUnverifiedCount == 0 && recorder.lastUnexpectedEnvironmentCount == 0
                    status.text = purchaseButton.isEnabled ? "Ready. Store price: \(product!.displayPrice)" : "Baseline not clean or product unavailable. Stop this run."
                } else {
                    status.text = "Recorded restarted request. Do not purchase again."
                    checkpointButton.isEnabled = recorder.phase == "pending"
                }
            } catch {
                recorder.emit("product-fetch", ["available": false, "error": "store-error"])
                status.text = "Product unavailable. Stop this run."
            }
        }
    }

    func sceneDidBecomeActive(_ scene: UIScene) {
        Task { @MainActor in await snapshot("foreground") }
    }

    @objc private func buy() {
        guard recorder.phase == "ready", let product else { return }
        purchaseButton.isEnabled = false
        recorder.setPhase("purchase-in-flight")
        recorder.emit("purchase-start")
        Task { @MainActor in
            do {
                let result = try await product.purchase()
                switch result {
                case .pending:
                    recorder.setPhase("pending")
                    recorder.emit("purchase-result", ["result": "pending"])
                    status.text = "Native purchase result: Pending. No approval yet."
                    checkpointButton.isEnabled = true
                case .success(let entry):
                    recorder.setPhase("unexpected-success")
                    recorder.emit("purchase-result", ["result": "success"])
                    recorder.transaction("purchase-success", entry, reason: "purchase-result")
                    status.text = "Unexpected success before approval. Stop this run."
                case .userCancelled:
                    recorder.setPhase("cancelled")
                    recorder.emit("purchase-result", ["result": "cancelled"])
                    status.text = "Cancelled. Stop this run."
                @unknown default:
                    recorder.setPhase("unknown")
                    recorder.emit("purchase-result", ["result": "unknown"])
                    status.text = "Unknown result. Stop this run."
                }
            } catch {
                recorder.setPhase("failed")
                recorder.emit("purchase-result", ["result": "failed", "error": "store-error"])
                status.text = "Purchase failed. Stop this run."
            }
            await snapshot("purchase-return")
        }
    }

    @objc private func checkpoint() {
        guard recorder.phase == "pending" else { return }
        checkpointButton.isEnabled = false
        Task { @MainActor in
            await snapshot("owner-before-approval")
            recorder.emit("before-approval-checkpoint-complete")
            status.text = "Before-approval checkpoint saved. Wait for the next owner step."
        }
    }

    @objc private func afterApproval() {
        guard recorder.phase == "pending" else { return }
        approvedButton.isEnabled = false
        recorder.setPhase("owner-reports-approved")
        recorder.emit("owner-reports-approved")
        Task { @MainActor in
            await snapshot("owner-after-approval")
            recorder.emit("after-approval-checkpoint-complete")
            status.text = "After-approval snapshot saved. Diagnostic complete."
        }
    }

    private func snapshot(_ reason: String) async {
        recorder.emit("snapshot-begin", ["reason": reason])
        var current = 0, unfinished = 0, unverified = 0, unexpectedEnvironment = 0
        for await entry in StoreKit.Transaction.currentEntitlements {
            if recorder.transaction("currentEntitlements", entry, reason: reason) { current += 1 }
            if recorder.isUnverifiedProduct(entry) { unverified += 1 }
            if recorder.isUnexpectedEnvironment(entry) { unexpectedEnvironment += 1 }
        }
        for await entry in StoreKit.Transaction.unfinished {
            if recorder.transaction("unfinished", entry, reason: reason) { unfinished += 1 }
            if recorder.isUnverifiedProduct(entry) { unverified += 1 }
            if recorder.isUnexpectedEnvironment(entry) { unexpectedEnvironment += 1 }
        }
        if let latest = await StoreKit.Transaction.latest(for: recorder.productId) {
            _ = recorder.transaction("latest", latest, reason: reason)
            if recorder.isUnverifiedProduct(latest) { unverified += 1 }
            if recorder.isUnexpectedEnvironment(latest) { unexpectedEnvironment += 1 }
        }
        recorder.lastEntitlementCount = current
        recorder.lastUnfinishedCount = unfinished
        recorder.lastUnverifiedCount = unverified
        recorder.lastUnexpectedEnvironmentCount = unexpectedEnvironment
        recorder.emit("snapshot-end", ["reason": reason, "verifiedCurrentCount": current,
            "verifiedUnfinishedCount": unfinished, "unverifiedCount": unverified, "unexpectedEnvironmentCount": unexpectedEnvironment])
    }
}

@MainActor
final class ProbeRecorder {
    let productId: String
    let runId: String
    private let logURL: URL
    private let phaseURL: URL
    private let processId = UUID().uuidString
    private var sequence: Int
    private(set) var phase: String
    var lastEntitlementCount = -1
    var lastUnfinishedCount = -1
    var lastUnverifiedCount = -1
    var lastUnexpectedEnvironmentCount = -1

    init() {
        let info = Bundle.main.infoDictionary!
        productId = info["PawsDiagnosticProductId"] as! String
        runId = info["PawsDiagnosticRunId"] as! String
        let directory = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("issue64-native-diagnostic", isDirectory: true)
        try! FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        logURL = directory.appendingPathComponent(runId + ".jsonl")
        phaseURL = directory.appendingPathComponent(runId + ".phase")
        sequence = ((try? String(contentsOf: logURL, encoding: .utf8)) ?? "").split(separator: "\n").count
        phase = (try? String(contentsOf: phaseURL, encoding: .utf8)) ?? "ready"
    }

    func setPhase(_ value: String) {
        phase = value
        try! value.write(to: phaseURL, atomically: true, encoding: .utf8)
    }

    func emit(_ event: String, _ values: [String: Any] = [:]) {
        sequence += 1
        var fields = values
        fields["schema"] = "paws-native-ask-to-buy-v1"
        fields["runId"] = runId
        fields["process"] = processId
        fields["sequence"] = sequence
        fields["timeUTC"] = ISO8601DateFormatter().string(from: Date())
        fields["uptime"] = ProcessInfo.processInfo.systemUptime
        fields["phase"] = phase
        fields["event"] = event
        let data = try! JSONSerialization.data(withJSONObject: fields, options: [.sortedKeys])
        let line = String(data: data, encoding: .utf8)!
        if !FileManager.default.fileExists(atPath: logURL.path) { FileManager.default.createFile(atPath: logURL.path, contents: nil) }
        let file = try! FileHandle(forWritingTo: logURL)
        defer { try? file.close() }
        try! file.seekToEnd()
        try! file.write(contentsOf: Data((line + "\n").utf8))
        try! file.synchronize()
        NSLog("APPLE_IAP_BOUND_EVENT %@", line)
    }

    @discardableResult
    func transaction(_ source: String, _ entry: VerificationResult<StoreKit.Transaction>, reason: String) -> Bool {
        let t: StoreKit.Transaction
        let verified: Bool
        switch entry {
        case .verified(let value): t = value; verified = true
        case .unverified(let value, _): t = value; verified = false
        }
        guard t.productID == productId else { return false }
        let reference = SHA256.hash(data: Data((productId + ":" + String(t.id)).utf8)).map { String(format: "%02x", $0) }.joined()
        let environment = t.environment == .xcode ? "xcode" : t.environment == .sandbox ? "sandbox" : t.environment == .production ? "production" : "unknown"
        let eligible = verified && t.productType == .nonConsumable && t.appBundleID == Bundle.main.bundleIdentifier &&
            t.revocationDate == nil && t.expirationDate == nil && !t.isUpgraded
        emit("transaction", ["source": source, "reason": reason, "verified": verified, "environment": environment,
            "transactionRef": reference, "revoked": t.revocationDate != nil, "eligible": eligible])
        return eligible
    }

    func isUnexpectedEnvironment(_ entry: VerificationResult<StoreKit.Transaction>) -> Bool {
        switch entry {
        case .verified(let t), .unverified(let t, _): return t.productID == productId && t.environment != .xcode
        }
    }

    func isUnverifiedProduct(_ entry: VerificationResult<StoreKit.Transaction>) -> Bool {
        if case .unverified(let t, _) = entry { return t.productID == productId }
        return false
    }
}
