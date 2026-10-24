// Pure snapshot reconciliation used by the native verifier and desktop regressions.
enum PawsEvidencePolicy {
    static func latestRevokes(ownedID: UInt64?, latestID: UInt64, revoked: Bool) -> Bool {
        revoked && (ownedID == nil || ownedID == latestID)
    }
    static func acceptsEnvironment(review: Bool, production: Bool, sandbox: Bool, xcode: Bool) -> Bool {
        review ? (sandbox || xcode) : (production || sandbox)
    }
}
