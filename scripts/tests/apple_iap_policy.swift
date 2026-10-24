// Issue #64: no device/StoreKit transaction automation. Runs the actual native decision seam.
@main struct AppleIapPolicyTests {
    static func main() {
        // Refund arriving between currentEntitlements and latest must override the same owned transaction.
        precondition(PawsEvidencePolicy.latestRevokes(ownedID: 123, latestID: 123, revoked: true))
        precondition(PawsEvidencePolicy.latestRevokes(ownedID: nil, latestID: 123, revoked: true))
        // A different valid current entitlement survives a historical refund.
        precondition(!PawsEvidencePolicy.latestRevokes(ownedID: 456, latestID: 123, revoked: true))
        precondition(!PawsEvidencePolicy.latestRevokes(ownedID: 123, latestID: 123, revoked: false))
        precondition(PawsEvidencePolicy.acceptsEnvironment(review: false, production: true, sandbox: false, xcode: false))
        // App Review/TestFlight use real Apple Sandbox; Xcode remains isolated to review.
        precondition(PawsEvidencePolicy.acceptsEnvironment(review: false, production: false, sandbox: true, xcode: false))
        precondition(!PawsEvidencePolicy.acceptsEnvironment(review: false, production: false, sandbox: false, xcode: true))
        precondition(PawsEvidencePolicy.acceptsEnvironment(review: true, production: false, sandbox: true, xcode: false))
        precondition(PawsEvidencePolicy.acceptsEnvironment(review: true, production: false, sandbox: false, xcode: true))
        precondition(!PawsEvidencePolicy.acceptsEnvironment(review: true, production: true, sandbox: false, xcode: false))
        precondition(!PawsEvidencePolicy.acceptsEnvironment(review: false, production: false, sandbox: false, xcode: false))
        print("Apple native policy: 11 assertions passed")
    }
}
