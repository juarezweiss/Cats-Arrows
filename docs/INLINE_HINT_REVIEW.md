# Issue #23 — Immediate hints

Status: owner approved the simplified iPhone flow on 2026-10-10 as the version
going forward. Native validation passed. Owner authorized commit and push on 2026-10-10;
merge remains subject to explicit approval.

## Approved behavior

Hint immediately applies one correct clue-derived Cat or X and briefly outlines
its target. No Why button, explanation panel, hint modal or Last hint action is
shown in the default flow. Hint/Clear share the first row; Menu/New Game stay
below. The grid stays centered and playable until completion; completed boards
stay visible and readonly with the usual localized completion phrase.
Puzzle logic is unchanged.

## Rollback

The accepted Issue #19 signed iPhone app is preserved at
Builds/Issue19Board/Playable/UnityProject.app (develop baseline dcae803).
GameShellView useInlineHints=false, the bootstrap setting, or the per-build
CATS_ARROWS_HINT_FLOW=legacy restore the previous modal/Last hint flow. Desktop
review also supports --hint-flow=legacy. Revert only scoped Issue #23 changes;
never discard the unrelated #21/#22 plans or original_material.md.

Rollback requires no purchases or data migration. A reinstall may reset the
in-memory board; signing may need reprovisioning if its profile expires.

## Review gates

Fresh Unity tests and repository validation; mounted immediate and legacy flows;
three locales at compact, standard and tall portrait sizes; immediate hint,
continued board input, correction, rapid hint/reset, clear/new game, menu/locale
and completion lock. Owner iPhone review accepted the simplified flow.

## Fresh validation

Pinned Unity 6000.3.25f1 compile passed (compile.Q3aGXm); EditMode passed
227/227 with zero failed/skipped/inconclusive cases (editmode.jaOnNR). Localized
How to Play wording was updated afterward and compiled into the native builds.
Repository validation passed. Existing planning/source materials match the
pre-task SHA-256 snapshot.

Fresh native macOS flows passed in English 320x568, Brazilian Portuguese
390x844, Spanish 430x932 and English legacy rollback 390x844. Evidence is in
ignored .unity-results/issue23-simple-*-player.log and issue23-simple-captures/.
QA found no remaining blockers after reviewing logs, captures and the updated
localized instructions. The owner accepted the simplified flow after iPhone review. Individual physical
edge-case results were not separately reported.

The revised iOS IL2CPP/Xcode Release build succeeded, was installed and launched
on the paired iPhone 17. Saved app: Builds/Issue23Board/Playable/UnityProject.app.
The accepted Issue #19 app remains untouched for rollback. Owner review approved.
