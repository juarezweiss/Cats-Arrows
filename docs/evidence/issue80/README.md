# Branding — issue #80

Build source: `4a29c6e593b5b3856e20e7d7f038593decd3dc88`, based on develop
`dff1845`. Unity **6000.3.25f1**. No gameplay symbols, navigation or release
identity changed.

## References and rendering

- Production icon is byte-identical to `assets/icon.png` on gh-pages
  `a9cbaae7f5a9bd053b9fffa5b59e565e5271d188`; existing iOS GUID assignments
  remain valid, and the default Unity icon now uses the same asset.
- Menu PNG is byte-identical to the owner's `transparent_paw.png` attachment.
  Import swizzles RGB to white and preserves source alpha; UI tints with the
  existing `BoardSymbolElement.Ink`. No RGB gradient or background is displayed.
- Menu uses an independent Image. Its UV bounds exclude transparent reference
  margins (only alpha=1 export noise lies outside); silhouette aspect is retained.
  The existing 340×230 slot remains; 220×192 inner art keeps the previous paw's
  approximate height while fitting the new silhouette's wider proportions.

Native macOS review-player captures (simulated safe areas; not physical iPhone):

| EN · 320×568 | pt-BR · 390×844 | ES · 430×932 |
| --- | --- | --- |
| [Home](home-en320.png) | [Home](home-pt390.png) | [Home](home-es430.png) |

Reproduce with `BoardReviewBuild.Build`, then the player arguments
`--board-locale=<locale> --board-width=<width> --board-height=<height>
--board-safe-top=<pixels> --board-safe-bottom=<pixels>
--menu-capture=<absolute-PNG-path>`. Compact uses 20/0 insets; other captures
use 59/34. Desktop review confirms shape, flat ink, centering and no clipping.

## Validation

- `scripts/validate.sh`, whitespace checks and pinned compilation passed
  (`compile.bJEfPI`).
- Full EditMode gate: **501/501 passed**, zero failed/skipped/inconclusive,
  successful editor exit (`editmode.72UGMO`). Covers original file hashes,
  every iOS icon slot/default, alpha-only importer, mounted menu art and nine
  portrait/locale combinations, plus existing gameplay/interaction regressions.
  First run (`editmode.RxyhzY`) passed 500/501 with an existing Settings Back
  layout assertion failure; unchanged full rerun passed. No test was disabled.
- Native review player built successfully (`issue80-review-build.p0iXgE`);
  all three captures exited successfully. Generated review scene changes were
  discarded; the committed review scene is unchanged.
- Clean unsigned production iOS export passed (`ios-export.VUskYF`), including
  the standard release inspector. All ten exported iPhone/App Store icon PNGs
  match catalog dimensions and the approved image after required resizing;
  all four Xcode configurations select the included `AppIcon` catalog.
  [Icon report](ios-icons.json) records hashes and pixel comparisons against a
  Lanczos-resized reference (Unity's resampling differs slightly). Original PNG
  bytes are unchanged. The export was reused for the device preparation below,
  then removed to save storage.
- Independent QA found no blocking code/configuration or menu-rendering findings.

Owner physical iPhone appearance review passed on 2026-10-21 ("approved") for
the installed icon and menu paw shape, color and spacing on iPhone 17 / iOS 27.0.1.
At PR creation, no archive, signing, installation, upload, production release or
merge had been done. Owner-authorized installation is recorded below.
Rollback: revert the branding PR.

## Owner-authorized local iPhone preparation — 2026-10-21

Paws **0.1.0 (1)** was installed in place on **iPhone 17 / iOS 27.0.1** and
launched normally; its running process and installed identity were verified.
[Sanitized device receipt](device-preparation.json) records the signed app hash.
No physical interaction, screenshot, appearance or lifecycle test was automated.
The owner approved the new Home Screen icon and menu paw shape, color and
spacing on 2026-10-21. This report applies to this installed source/device.

The existing validated Unity export from `4a29c6e` was reused; later PR build-input
changes are only trailing whitespace in metadata. One **Release arm64** build
with **Xcode 27.1 (27A9275)** succeeded. Existing development profile/certificate,
device membership, identity/portrait/minimum OS, exact applied entitlements and
strict app/framework signatures were verified. The compiled 120px icon decodes
to exactly the validated generated icon pixels. Local development signing expires
**2026-10-27 17:36:44 UTC**. This is not a distribution signature.

To limit storage, no archive, IPA or external dSYMs were generated; ordinary
Release local-symbol stripping removed about 24.7 MiB before resigning. Retained
`Builds/Issue80iPhone/Paws.app` is **53.0 MiB**, with small export metadata and
technical logs/receipts. Task-owned export, DerivedData and desktop review player
were removed after success, reclaiming **2.26 GiB**. Owner documentation edits, Git history and main remain unchanged.

## Owner-approved completion cleanup — 2026-10-21

Following the owner's physical approval and instruction to merge/clean up,
superseded #76 local review app/archive and inactive generated Unity Library/Logs
were removed, reclaiming **939.4 MiB** beyond the earlier 2.26 GiB. The approved
#80 signed app and metadata remain; its recorded tree hash is unchanged. Ongoing
#64 IAP work/artifacts, small historical metadata, source/evidence and uncommitted
owner policy documentation are preserved. No release tag or main history is
changed. Cleanup receipt remains under ignored
`.unity-results/issue80-device-4a29c6e/final-cleanup.json`.
