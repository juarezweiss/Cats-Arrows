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

            foreach (var paw in puzzle.SolutionPaws)
            {
                if (!puzzle.IsInside(paw)) errors.Add($"Solution paw {paw} is outside the board.");
                if (puzzle.IsArrow(paw)) errors.Add($"Cell {paw} cannot be both an arrow and a solution paw.");
            }

            foreach (var arrow in puzzle.Arrows)
            {
                if (!puzzle.IsInside(arrow.Key))
                {
                    errors.Add($"Arrow {arrow.Key} is outside the board.");
                    continue;
                }

                var segmentHasPaw = puzzle.GetDirectionalSegment(arrow.Key, arrow.Value).Any(puzzle.IsSolutionPaw);
                if (!segmentHasPaw)
                    errors.Add($"Arrow at {arrow.Key} pointing {arrow.Value} has no solution paw in its pointed segment.");
            }

            var zeroRows = 0;
            var zeroColumns = 0;
            var hasRequiredLine = !profile.RequiredPawCountInSomeLine.HasValue;
            for (var i = 0; i < puzzle.Size; i++)
            {
                var rowCount = puzzle.GetRowPawCount(i);
                var columnCount = puzzle.GetColumnPawCount(i);
                if (rowCount == 0) zeroRows++;
                if (columnCount == 0) zeroColumns++;
                if (rowCount == profile.RequiredPawCountInSomeLine || columnCount == profile.RequiredPawCountInSomeLine)
                    hasRequiredLine = true;
                if (rowCount > profile.MaxPawsPerLine) errors.Add($"Row {i} has {rowCount} paws; max is {profile.MaxPawsPerLine}.");
                if (columnCount > profile.MaxPawsPerLine) errors.Add($"Column {i} has {columnCount} paws; max is {profile.MaxPawsPerLine}.");
            }

            if (zeroRows > profile.MaxZeroRows) errors.Add($"Puzzle has {zeroRows} zero-paw rows; max is {profile.MaxZeroRows}.");
            if (zeroColumns > profile.MaxZeroColumns) errors.Add($"Puzzle has {zeroColumns} zero-paw columns; max is {profile.MaxZeroColumns}.");

            if (!hasRequiredLine) errors.Add($"Puzzle requires a row or column with exactly {profile.RequiredPawCountInSomeLine} paws.");

            return new PuzzleValidationResult(errors);
        }
    }
}
