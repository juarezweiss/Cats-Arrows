# Puzzle Workflow

## Generation contract
Generate candidate solution -> derive clues -> validate local invariants -> prove unique solution -> solve with documented human deductions -> score difficulty -> accept/reject.

Never accept a puzzle merely because its row/column totals and arrows are internally true.

## Required test categories
- fixed seeds for known valid puzzles;
- fixed seeds for rejected ambiguous/stalled puzzles;
- property-style loops over many seeds per difficulty;
- uniqueness verifier agrees with expected solution;
- human solver never branches/guesses for accepted puzzles;
- generator terminates within a bounded attempt/time budget and reports failure cleanly.

## Explainability
Solver steps should be represented as data (`DeductionStep`) so the same reasoning can later power Hint and debugging. Prefer deductions a player can infer from visible rules.
