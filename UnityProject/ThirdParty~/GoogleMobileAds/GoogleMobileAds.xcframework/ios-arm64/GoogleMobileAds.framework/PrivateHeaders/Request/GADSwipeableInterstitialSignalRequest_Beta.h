//
//  GADSwipeableInterstitialSignalRequest_Beta.h
//  Google Mobile Ads SDK
//
//  Copyright 2026 Google LLC
//

#import <UIKit/UIKit.h>

#import <GoogleMobileAds/GADAdSize.h>
#import <GoogleMobileAds/Request/GADSignalRequest.h>

/// A swipeable interstitial signal request that can be used as input in server-to-server signal
/// generation.
NS_SWIFT_NAME(SwipeableInterstitialSignalRequest)
@interface GADSwipeableInterstitialSignalRequest : GADSignalRequest

/// Specifies the maximum duration (in seconds) the app commits to holding the ad on screen before
/// it is eligible for dismissal. The SDK only returns ads with a hold requirement that is less than
/// or equal to this value. Defaults to 0.
///
/// Only set this property if your app implements logic to hold swipeable interstitial ads on
/// screen.
@property(nonatomic, assign) NSTimeInterval maxScreenHoldDuration;

/// An optional ad size to request.
/// If this property is set to CGSizeZero (the default), the SDK requests an ad using the device's
/// full screen size.
/// If the requested ad size is determined to be too small for an interstitial experience, the ad
/// request will fail.
@property(nonatomic, assign) CGSize adSize;

/// The direction in which swipe gestures should be detected as custom click gestures for swipeable
/// interstitial ads.
@property(nonatomic, assign, readonly)
    UISwipeGestureRecognizerDirection customClickSwipeGestureDirection;

/// Determines whether the creative handles standard tap clicks when custom click swipe gestures are
/// enabled. Defaults to YES.
@property(nonatomic, assign, readonly, getter=areCustomClickSwipeGestureTapsAllowed)
    BOOL customClickSwipeGestureTapsAllowed NS_SWIFT_NAME(areCustomClickSwipeGestureTapsAllowed);

/// Enables a custom click swipe gesture on the swipeable interstitial ad. Available for allowlisted
/// publishers only. This setting is ignored for publishers not allowlisted.
///
/// @param direction A swipe direction on the ad that should trigger an ad click.
/// @param tapsAllowed Whether to treat tap gestures as ad clicks.
- (void)enableCustomClickSwipeGestureWithDirection:(UISwipeGestureRecognizerDirection)direction
                                       tapsAllowed:(BOOL)tapsAllowed
    NS_SWIFT_NAME(enableCustomClickSwipeGesture(direction:tapsAllowed:));

/// Returns an initialized swipeable interstitial signal request.
/// @param signalType The type of signal to request.
- (nonnull instancetype)initWithSignalType:(nonnull NSString *)signalType;

@end
