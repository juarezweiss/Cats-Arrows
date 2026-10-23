import Foundation
import StoreKit
import UIKit

// Issue #65. This bridge reports only sheet presentation, never code validity or
// ownership. Verified transactions continue through the existing purchase adapter.
@MainActor
private enum PawsOfferCodeSheet {
    static var presenting = false

    static func present(_ request: Int64, _ receive: @escaping @convention(c) (Int64, Int32) -> Void) async {
        // Non-consumable offer codes require iOS 16.3 even though the sheet API
        // itself was introduced in iOS 16.0. Do not alter the app's deployment target.
        guard #available(iOS 16.3, *) else {
            receive(request, 1) // Unsupported
            return
        }
        guard !presenting else {
            receive(request, 2) // Unavailable
            return
        }
        // Unity 6.3's UnityAppController owns this window. Avoid selecting an
        // unrelated connected scene when the Unity window is inactive/backgrounded.
        guard let window = UIApplication.shared.delegate?.window ?? nil,
              window.isKeyWindow, !window.isHidden,
              let scene = window.windowScene, scene.activationState == .foregroundActive else {
            receive(request, 2)
            return
        }
        presenting = true
        let outcome: Int32
        do {
            try await AppStore.presentOfferCodeRedeemSheet(in: scene)
            outcome = 0 // Closed; redemption is not inferred from dismissal.
        } catch StoreKitError.userCancelled {
            outcome = 0
        } catch is CancellationError {
            outcome = 0
        } catch {
            outcome = 2
        }
        presenting = false
        receive(request, outcome)
    }
}

@_cdecl("PawsIapPresentOfferCodeSheet")
public func pawsIapPresentOfferCodeSheet(_ request: Int64,
    _ receive: @escaping @convention(c) (Int64, Int32) -> Void) {
    Task { @MainActor in await PawsOfferCodeSheet.present(request, receive) }
}
