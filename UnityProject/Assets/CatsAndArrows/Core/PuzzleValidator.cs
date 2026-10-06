using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    public static class PuzzleValidator
    {
        public static PuzzleValidationResult Validate(PuzzleDefinition puzzle, DifficultyProfile profile)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var errors = new List<string>();

            if (puzzle.Size != profile.Size)
                errors.Add($"Puzzle size {puzzle.Size} does not match profile size {profile.Size}.");

            foreach (var cat in puzzle.SolutionCats)
            {
                if (!puzzle.IsInside(cat)) errors.Add($"Solution cat {cat} is outside the board.");
                if (puzzle.IsArrow(cat)) errors.Add($"Cell {cat} cannot be both an arrow and a solution cat.");
            }

            foreach (var arrow in puzzle.Arrows)
            {
                if (!puzzle.IsInside(arrow.Key))
                {
                    errors.Add($"Arrow {arrow.Key} is outside the board.");
                    continue;
                }

                var segmentHasCat = puzzle.GetDirectionalSegment(arrow.Key, arrow.Value).Any(puzzle.IsSolutionCat);
                if (!segmentHasCat)
                    errors.Add($"Arrow at {arrow.Key} pointing {arrow.Value} has no solution cat in its pointed segment.");
            }

            var zeroRows = 0;
            var zeroColumns = 0;
            for (var i = 0; i < puzzle.Size; i++)
            {
                var rowCount = puzzle.GetRowCatCount(i);
                var columnCount = puzzle.GetColumnCatCount(i);
                if (rowCount == 0) zeroRows++;
                if (columnCount == 0) zeroColumns++;
                if (rowCount > profile.MaxCatsPerLine) errors.Add($"Row {i} has {rowCount} cats; max is {profile.MaxCatsPerLine}.");
                if (columnCount > profile.MaxCatsPerLine) errors.Add($"Column {i} has {columnCount} cats; max is {profile.MaxCatsPerLine}.");
            }

            if (zeroRows > profile.MaxZeroRows) errors.Add($"Puzzle has {zeroRows} zero-cat rows; max is {profile.MaxZeroRows}.");
            if (zeroColumns > profile.MaxZeroColumns) errors.Add($"Puzzle has {zeroColumns} zero-cat columns; max is {profile.MaxZeroColumns}.");

            return new PuzzleValidationResult(errors);
        }
    }
}
