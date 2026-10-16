using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    // Presentation-only derived facts; never supplied as premises to a solver.
    public static class ClueProtectionRules
    {
        // Issue #37: Hard leaves every normal cell for the player to classify.
        // No proof is required and no consequence-derived hatches are selected.
        public static IReadOnlyList<BoardCoordinate> ForProfile(DifficultyProfile profile,
            PuzzleClues clues, HumanSolverResult proof)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            return string.Equals(profile.Id, "hard", StringComparison.Ordinal)
                ? Array.Empty<BoardCoordinate>() : Derive(clues, proof);
        }

        public static IReadOnlyList<BoardCoordinate> Derive(PuzzleClues clues, HumanSolverResult proof)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            if (proof.Status != HumanSolverStatus.Solved) return Array.Empty<BoardCoordinate>();

            var provenEmpty = new HashSet<BoardCoordinate>(proof.Assignments
                .Where(assignment => assignment.State == CellKnowledge.NotCat)
                .Select(assignment => assignment.Coordinate));
            var arrows = new HashSet<BoardCoordinate>(clues.Arrows.Select(arrow => arrow.Coordinate));
            var protectedCells = new HashSet<BoardCoordinate>();
            foreach (var arrow in clues.Arrows)
            {
                if (!DirectionGeometry.IsCardinal(arrow.Direction)) continue;
                var horizontal = arrow.Direction == Direction.Left || arrow.Direction == Direction.Right;
                var total = horizontal ? clues.RowTotals[arrow.Coordinate.Row] : clues.ColumnTotals[arrow.Coordinate.Column];
                if (total != 1) continue;

                var opposite = arrow.Direction switch
                {
                    Direction.Left => Direction.Right,
                    Direction.Right => Direction.Left,
                    Direction.Up => Direction.Down,
                    _ => Direction.Up
                };
                foreach (var coordinate in DirectionGeometry.GetDirectionalSegment(clues.Size, arrow.Coordinate, opposite))
                    if (!arrows.Contains(coordinate) && provenEmpty.Contains(coordinate))
                        protectedCells.Add(coordinate);
            }

            return Array.AsReadOnly(protectedCells.OrderBy(coordinate => coordinate.Row)
                .ThenBy(coordinate => coordinate.Column).ToArray());
        }
    }
}
