# Analytics Plan

## Privacy-first principle
Collect only data needed to improve product/monetization decisions. No analytics vendor is selected in the bootstrap.

## Core events
- app_open
- difficulty_selected
- puzzle_generated (difficulty, generator_version, non-identifying seed hash if appropriate)
- puzzle_started
- cell_tapped (aggregate carefully; avoid excessive event volume)
- drag_mark_completed (cells_marked)
- hint_requested (deduction_type)
- puzzle_completed (duration, hints_used, difficulty metrics)
- puzzle_abandoned
- new_game_requested
- accessibility_setting_changed

## Issue #29 future monetization diagnostics

Design only; no events or SDK are implemented/activated. Manual trial feedback is
sufficient now. Correctness uses durable state and provider evidence, never telemetry.
Optional local diagnostics use normalized outcomes; export/collection requires a
separately defined learning question and privacy review of actual selected SDKs.

For a future controlled ad-free comparison, ask whether players start another
board after progression opportunities and whether requested hints actually deliver.
Extend existing `puzzle_completed`, `new_game_requested` and `hint_requested` with
coarse mode/owned and ordinary/tutorial flags rather than logging each cell.
Add only if this experiment needs them:

- `progression_ad_outcome`: opportunity milestone bucket, presented/skipped/failure
  reason, and duration bucket. Presentation is not inferred from milestone count.
- `rewarded_hint_outcome`: confirmed reward, applied/recovered/unavailable/cancelled
  outcome and reason; no reward inferred from dismissal.
- `remove_ads_outcome`: purchase/restore/refresh operation, pending/owned/revoked/
  cancelled/unavailable outcome and normalized reason.

No raw receipts, transaction/account IDs, provider tokens, advertising identifiers,
puzzle seeds, proof/board data or persistent user identifier. Use ephemeral local
operation correlation only if needed to deduplicate outcomes; durable request IDs
stay in state. Diagnostics are optional and failures never affect play or access.
Retention requires a separately approved identity methodology; do not add one for
this architecture. Price/revenue tracking and an acquisition dashboard are deferred.

## Derived metrics (existing plan)
- first puzzle completion rate;
- completion rate by difficulty;
- median completion time;
- hint rate;
- puzzles/session;
- D1/D7 retention once identity/session methodology is defined;
- crash-free sessions.

Do not add a vendor SDK directly to domain/gameplay code. Use an analytics interface and provider adapter.
