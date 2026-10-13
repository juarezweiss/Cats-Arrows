# Issue #26 — Interactive first-play tutorial

The owner approved the design and refined it after iPhone review on 2026-10-13.
Implementation is on feat/26-interactive-tutorial; the owner accepted all revised iPhone tutorial checks on 2026-10-13. Owner authorized commit, push and integration into develop. Unrelated #21/#22 planning files
and original_material.md are preserved.

## Revised flow

First ordinary Start enters the fixed tutorial once. Tutorial in the menu always
restarts the same board and flow. Local seen history is saved on entry; an app
update does not reset it, so returning reviewers should use the menu Tutorial.
Menu/language changes retain the in-memory lesson; force-quit does not save its
marks/step. Starting Tutorial replaces the current game. Skip explicitly opens a
fresh random Easy board from either guided or independent practice.

Welcome names and highlights Continue. The goal explains that the row/column
numbers count cats. An editable-cell introduction precedes individual highlighted
Empty, X and Cat examples, with the status names bold. The X sample is on a
non-cat cell; the Cat sample is on a true cat cell. These are read-only render
samples, never player marks or solver premises. Hatching highlights only protected
cells and explains they cannot change; arrows get their own non-editable lesson.

Guided play teaches a complete tap cycle, zero-column/zero-row drags, cardinal
singletons, the exact diagonal ray and two-cat quotas. Input is restricted to
current targets, and progression reconciles after release/cancellation. On the
last guided Cat, that session completes normally; after release it is replaced
with a fresh session of the same fixed puzzle and all editable cells Empty.

“Now try it yourself” introduces unrestricted ordinary input without highlights
or Hint assistance. Only independently solving this second board shows completion
and Play for random boards. Extra wrong Cats prevent victory; other cells may
remain Empty/X. The completed independent board remains visible and readonly.
Normal Hint/Clear/New Game controls do not appear during either tutorial pass.

## Fixture and architecture

Tutorial-v1, seed identity 26; row totals 0,1,2,1,1 and column totals 0,2,1,1,1.
Cats: R2C5, R5C3, R3C2, R3C4, R4C2 (1-based).
Arrows: R2C4 Right; R4C3 Down; R1C4 DownLeft.
Derived hatching: R1C3, R2C1, R2C2, R2C3, R3C3.

The current human solver and independent exhaustive verifier accept this fixture.
Ordinary boards remain seeded procedural puzzles. TutorialController owns lesson
gates; GameSession owns player marks and victory. The view projects demo symbols
without changing session/BoardPresentation truth. The shell rebinds the fresh
independent session and input authority after the guided gesture ends.

All text is localized in English, Brazilian Portuguese and Spanish. The normal
message slot remains 48 units; only the longer goal uses 60, preserving the
16-unit font and fixed grid position. Native checks enforce text fit and no
intersection with column clue bounds.

## Validation

- Revised pinned Unity compile passed (compile.fZkeeS).
- Expanded EditMode suite passed 248/248, zero failed/skipped/inconclusive
  (editmode.F4Tghj); final source confirmation and layout checks passed.
- Current tests cover fixture quotas, solver/uniqueness, protection, separate
  demo rendering and mark isolation, actual practice taps, restricted input,
  release gates, guided-to-independent session reset, free input, wrong-cat
  correction, independent victory, replay, first-play preferences and retry.
- Revised mounted pointer walkthroughs include all five portrait/locale cases
  (EN320, PT390, ES430, PT320, ES320), normal and legacy game loops, and ordinary
  mouse/touch regressions. All five strict bounds checks passed.
- Revised captures/logs: ignored .unity-results/issue26-revised-captures/ and
  issue26-revised-*-player.log. Synthetic pointer evidence is not physical touch.

## Owner device review

Use Menu → Tutorial, since first-entry history persists from the earlier build.
Check highlighted Continue, full goal text, separate bold state demos and fixed
cell lessons, guided swipes, automatic empty same-board reset, unrestricted
independent play, completion only after that solve, then Play → random board.
Also check menu/language return, replay, Skip in independent play and
background/reopen. The owner confirmed all revised tutorial checks work on the iPhone on
2026-10-13.

## Rollback and storage

Disable enableTutorial/bootstrap tutorial setting (desktop --tutorial=off) to
restore ordinary random Start without a Tutorial entry. Seen history is separate
from puzzle generation. The owner authorized purging the superseded Issue #23
apps after accepting this tutorial; rollback now uses configuration/source
history rather than a prior prebuilt app. Revert only #26
source changes, preserving unrelated local files. No purchases, production
release or store submission is required. Temporary Xcode export/DerivedData can
be regenerated and should be removed after saving the signed review app.

## Revised phone installation

Revised Unity iOS export and Xcode Release build succeeded. The signed two-pass
tutorial app was saved to Builds/Issue26Tutorial/Playable/UnityProject.app,
installed and launched on the paired iPhone 17. Owner reviewed via the menu Tutorial entry and confirmed all revised checks
work on 2026-10-13. Temporary export/DerivedData were purged after installation.
Commit and push were authorized after owner acceptance. All pre-existing local
changes match the before-task hashes.

## Approved post-acceptance cleanup

The superseded Issue #23 apps, older capture sets and regenerable Unity Library
cache were removed at the owner’s request. Current tutorial phone/macOS apps,
final revised captures and validation reports are retained. The next Unity run
will reimport the project. No source, local planning or original material was
removed.
