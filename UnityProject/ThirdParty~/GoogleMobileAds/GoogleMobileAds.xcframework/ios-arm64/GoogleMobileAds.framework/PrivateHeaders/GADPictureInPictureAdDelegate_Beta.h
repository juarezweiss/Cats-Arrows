//
//  GADPictureInPictureAdDelegate_Beta.h
//  Google Mobile Ads SDK
//
//  Copyright 2026 Google LLC
//

#import <Foundation/Foundation.h>

@class GADPictureInPictureAd;

/// Delegate protocol for receiving Picture-in-Picture (PiP) ad presentation,
/// lifecycle, and interaction events.
NS_SWIFT_NAME(PictureInPictureAdDelegate)
@protocol GADPictureInPictureAdDelegate <NSObject>

@optional

#pragma mark - Presentation Lifecycle

/// Called when the PiP ad is shown on screen.
- (void)pictureInPictureAdDidShow:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdDidShow(_:)) NS_SWIFT_UI_ACTOR;

/// Called when the PiP ad is dismissed/hidden.
- (void)pictureInPictureAdDidHide:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdDidHide(_:)) NS_SWIFT_UI_ACTOR;

/// Called when the PiP ad fails to show on screen.
- (void)pictureInPictureAdDidFailToShow:(nonnull GADPictureInPictureAd *)pictureInPictureAd
                              withError:(nonnull NSError *)error
    NS_SWIFT_NAME(pictureInPictureAdDidFailToShow(_:error:)) NS_SWIFT_UI_ACTOR;

#pragma mark - Ad Lifecycle Events

/// Called when a click is recorded for a PiP ad.
- (void)pictureInPictureAdDidRecordClick:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdDidRecordClick(_:)) NS_SWIFT_UI_ACTOR;

/// Called when an impression is recorded for a PiP ad.
- (void)pictureInPictureAdDidRecordImpression:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdDidRecordImpression(_:)) NS_SWIFT_UI_ACTOR;

#pragma mark - Click-Time Lifecycle Notifications

/// Called when the PiP ad opens a view that covers the entire screen.
- (void)pictureInPictureAdWillPresentScreen:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdWillPresentScreen(_:)) NS_SWIFT_UI_ACTOR;

/// Called when the full-screen view opened from the PiP ad will be closed.
- (void)pictureInPictureAdWillDismissScreen:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdWillDismissScreen(_:)) NS_SWIFT_UI_ACTOR;

/// Called when the full-screen view opened from the PiP ad is closed.
- (void)pictureInPictureAdDidDismissScreen:(nonnull GADPictureInPictureAd *)pictureInPictureAd
    NS_SWIFT_NAME(pictureInPictureAdDidDismissScreen(_:)) NS_SWIFT_UI_ACTOR;

@end
