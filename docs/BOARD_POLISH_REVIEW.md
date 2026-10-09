# Issue #19 — Calm portrait board review

Status: owner approved the iPhone visuals and portrait lock on 2026-10-09,
and authorized commit/push on `feat/19-calm-board-polish`. Merge requires further
explicit approval.

## Changes

Arrow and protected cells share a subtly darker base fill; their arrow/hatch
symbols remain intact. The tile grid itself is centered in the safe board region;
its left/top clue gutters follow it. Symmetric space is reserved so clues stay
inside bounds. Pointer mapping continues to use the same BoardLayout rectangles.

Mobile PlayerSettings locks upright portrait and disables rotation. Exported iOS
Info.plist lists only UIInterfaceOrientationPortrait.

Completion stays immediate/read-only and displays the localized All cats found!
phrase below Easy, while completed tile fills gain a subtle grey tint. No automatic
victory overlay is shown. The header reserves its space before completion so the
grid does not jump. Hint/Clear are disabled; Menu/New Game stay active. An optional
Last hint action appears only when a Hint finished the game, opening the retained
clue-derived explanation on request and formatting it in the current language.
New Game clears completion feedback and remembered hint. No puzzle-domain code
changed. The approved project plan includes the owner’s presentation amendment.

## Native evidence

Unity 6000.3.25f1 compile passed (`compile.PllPPK`). Native EditMode suite passed
**219/219**, zero failed/skipped/inconclusive (`editmode.ZwmUpM`). New tests cover
centered tile geometry and clue bounds, fixed-cell/completed styling and rebind,
inline completion in all three locales, optional proof review, menu/language
preservation and fresh-game reset. Existing domain and pointer tests still pass.

Native macOS mounted flows exited 0 in en/pt-BR/es (`issue19-flow-*.log`). They
check rendered grid centering, final-hint completion without an automatic modal,
optional explanation, fresh-game reset, and completion through mounted board
PointerDown/Up events. Completed pointer taps are no-ops and action locks hold.
These synthetic checks do not establish physical input comfort.

21 rendered PNGs are saved under `.unity-results/issue19-captures`:
en-320x568 (safe top/bottom 50/34), pt-BR-390x844 (50/34), es-430x932 (59/34),
covering Menu, Rules, Play, Hint, Last hint, Completed and Completed by tap.
Narrow phones have smaller tiles because symmetric clue space is necessary;
QA/UX reviewed all 21 captures and found no blockers or completion layout jump.
The extreme 320×568/inset preview has approximately 39-pixel tiles, so physical
comfort at that size remains unverified. Owner touch/visual comfort is an explicit
review gate.

## Phone review

Native iOS export and Xcode Release build/signing passed
(`issue19-ios-export.log`, `issue19-ios-xcode.log`). Installed and launched on the
paired iPhone 17 as com.juarezweiss.catsarrows.inputreview. The signed app is
preserved at `Builds/Issue19Board/Playable/UnityProject.app`; its built Info.plist
contains only UIInterfaceOrientationPortrait. Reproducible Xcode DerivedData was
removed after preservation. On 2026-10-09 the owner confirmed the centered grid
looks perfect, darker cells look better, orientation lock works, and completion
feels much better.

Check darker fixed tiles and centered grid, rotate sideways/upside-down on Menu
and Play, complete by taps or Hints, inspect the unobscured grey board and status,
try Last hint if present, then Menu/resume and New Game. Confirm fresh-game
styling resets and symbols remain easy to read. Android physical verification
remains pending; initial delivery remains iOS.

## Risk and rollback

Only presentation, orientation settings, tests and docs change. Static tint is
used; no animation or dependency is added. Symmetric gutters reduce tile size on
small displays, requiring visual/comfort review. Revert Issue #19 to restore the
prior layout/modal; domain win and solvability rules are untouched. No purchases,
production deployment or vendor SDK changes. original_material.md remains
untouched and untracked.
