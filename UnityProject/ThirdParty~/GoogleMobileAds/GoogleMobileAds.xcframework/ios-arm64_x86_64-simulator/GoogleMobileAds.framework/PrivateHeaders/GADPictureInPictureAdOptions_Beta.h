//
//  GADPictureInPictureAdOptions_Beta.h
//  Google Mobile Ads SDK
//
//  Copyright 2026 Google LLC
//

#import <UIKit/UIKit.h>

#import <GoogleMobileAds/GoogleMobileAdsDefines.h>

/// Options for the position of a Picture-in-Picture (PiP) ad.
typedef NS_ENUM(NSInteger, GADPictureInPictureAdPosition) {
  /// Uses the SDK's default ad position. Currently resolves to the ad's last
  /// known position or the bottom-right corner if there is no last known
  /// position.
  ///
  /// Use `GADPictureInPictureAdPositionDefault` when you want the SDK to
  /// manage placement; use an explicit position when your layout depends on a
  /// specific, stable location.
  GADPictureInPictureAdPositionDefault = 0,
  /// Bottom-right corner of the screen.
  GADPictureInPictureAdPositionBottomRight = 1,
  /// Bottom-left corner of the screen.
  GADPictureInPictureAdPositionBottomLeft = 2,
  /// Top-left corner of the screen.
  GADPictureInPictureAdPositionTopLeft = 3,
  /// Top-right corner of the screen.
  GADPictureInPictureAdPositionTopRight = 4
} NS_SWIFT_NAME(PictureInPictureAdPosition);

/// Options for the presentation scope of a Picture-in-Picture (PiP) ad.
typedef NS_ENUM(NSInteger, GADPictureInPictureAdPresentationScope) {
  /// The Picture-in-Picture ad is scoped to a single screen.
  GADPictureInPictureAdPresentationScopeScreen = 0,
  /// The Picture-in-Picture ad persists globally across the application.
  GADPictureInPictureAdPresentationScopeApplication = 1
} NS_SWIFT_NAME(PictureInPictureAdPresentationScope);

/// Options for presenting a Picture-in-Picture (PiP) ad.
NS_SWIFT_NAME(PictureInPictureAdOptions)
@interface GADPictureInPictureAdOptions : NSObject

/// The current position of the PiP ad.
///
/// Value is `GADPictureInPictureAdPositionDefault` until the ad is assigned a
/// position.
@property(nonatomic, assign) GADPictureInPictureAdPosition position;

/// The Picture-in-Picture ad's presentation scope.
/// Defaults to GADPictureInPictureAdPresentationScopeScreen.
@property(nonatomic, assign) GADPictureInPictureAdPresentationScope presentationScope;

/// The window scene where the Picture-in-Picture ad will be presented. Defaults
/// to nil. If nil, the SDK automatically resolves the active window scene.
@property(nonatomic, weak, nullable) UIWindowScene *windowScene;

@end
