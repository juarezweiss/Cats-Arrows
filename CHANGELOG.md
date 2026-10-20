# Changelog

## Unreleased
- Added the isolated Unity IAP 5 Apple Remove All Ads adapter, native verified
  offline evidence, transaction recovery tests and a separate sandbox review export
  (#64). Production monetization remains disabled; owner store validation is pending.
- Established Paws production iOS identity (`com.jweiss.paws`), iPhone-only support,
  minimum iOS 16.3, and initial version 0.1.0 (build 1) with increasing build numbers.
- Added a production game entry and reproducible iOS export/archive tooling with
  source, toolchain, framework and privacy-manifest receipts. Review, benchmark and
  monetization mock code is excluded; monetization remains disabled by default.
- Added release configuration/build tests and recorded owner acceptance on iPhone 17
  / iOS 27.0.1. App Store distribution remains subject to separate owner approval.
- Repository/Codex/Unity foundation.
- Initial pure C# puzzle definition and validation contracts.
- Initial gameplay marking state and EditMode test suite.
