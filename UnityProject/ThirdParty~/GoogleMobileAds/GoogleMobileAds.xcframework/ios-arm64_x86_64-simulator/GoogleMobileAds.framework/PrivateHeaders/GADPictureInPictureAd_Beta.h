//
//  GADPictureInPictureAd_Beta.h
//  Google Mobile Ads SDK
//
//  Copyright 2026 Google LLC
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

#import <GoogleMobileAds/GADAdValue.h>
#import <GoogleMobileAds/GADPictureInPictureAdDelegate_Beta.h>
#import <GoogleMobileAds/GADPictureInPictureAdOptions_Beta.h>
#import <GoogleMobileAds/GADRequest.h>
#import <GoogleMobileAds/GADResponseInfo.h>

@class GADPictureInPictureAd;

/// A block to be executed when a Picture-in-Picture (PiP) request operation
/// completes. On success, ad is non-nil and |error| is nil. On failure, ad is
/// nil and |error| is non-nil.
typedef void (^GADPictureInPictureAdLoadCompletionHandler)(GADPictureInPictureAd *_Nullable ad,
                                                           NSError *_Nullable error)
    NS_SWIFT_NAME(PictureInPictureAdLoadCompletionHandler);

/// A Picture-in-Picture (PiP) ad is a floating ad that overlays
/// application content.
NS_SWIFT_NAME(PictureInPictureAd)
@interface GADPictureInPictureAd : NSObject

/// The unique identifier assigned to a specific ad placement in an app,
/// created on the AdMob or Ad Manager UI.
@property(nonatomic, readonly, nonnull) NSString *adUnitID;

/// Information about the ad response that returned the ad.
@property(nonatomic, readonly, nonnull) GADResponseInfo *responseInfo;

/// Optional delegate to receive state change notifications.
@property(nonatomic, weak, nullable) id<GADPictureInPictureAdDelegate> delegate;

/// Executed when the ad is estimated to have earned money.
@property(nonatomic, nullable, copy) GADPaidEventHandler paidEventHandler;

/// The current position of the PiP ad. Value is
/// `GADPictureInPictureAdPositionDefault` until the ad is assigned a position.
@property(nonatomic, readonly, assign) GADPictureInPictureAdPosition position;

/// The presentation scope of the PiP ad.
@property(nonatomic, readonly, assign) GADPictureInPictureAdPresentationScope presentationScope;

/// Unavailable. Use +loadWithAdUnitID:request:completionHandler: instead.
- (nonnull instancetype)init NS_UNAVAILABLE;

/// Loads a PiP ad.
///
/// @param adUnitID An ad unit ID created in the AdMob or Ad Manager UI.
/// @param request An ad request object. If nil, a default ad request object is
/// used.
/// @param completionHandler A handler to execute when the load operation
/// finishes or times out.
+ (void)loadWithAdUnitID:(nonnull NSString *)adUnitID
                 request:(nullable GADRequest *)request
       completionHandler:(nonnull GADPictureInPictureAdLoadCompletionHandler)completionHandler
    NS_SWIFT_NAME(load(with:request:completionHandler:))NS_SWIFT_SENDING;

/// Shows the PiP ad on the screen using specified GADPictureInPictureAdOptions.
///
/// @param options The options specifying presentation behavior. Must be called
/// on the main thread.
- (void)showWithOptions:(nonnull GADPictureInPictureAdOptions *)options
    NS_SWIFT_NAME(show(with:)) NS_SWIFT_UI_ACTOR;

/// Hides the PiP ad by removing it from the view hierarchy.
/// Must be called on the main thread.
- (void)hide NS_SWIFT_NAME(hide()) NS_SWIFT_UI_ACTOR;

@end
