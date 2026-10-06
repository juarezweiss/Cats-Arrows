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

## Derived metrics
- first puzzle completion rate;
- completion rate by difficulty;
- median completion time;
- hint rate;
- puzzles/session;
- D1/D7 retention once identity/session methodology is defined;
- crash-free sessions.

Do not add a vendor SDK directly to domain/gameplay code. Use an analytics interface and provider adapter.
