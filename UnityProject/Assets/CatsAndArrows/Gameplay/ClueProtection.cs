using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public static class ClueProtection
    {
        public static IReadOnlyList<BoardCoordinate> Derive(PuzzleClues clues)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            var proof = new HumanDeductionSolver().Solve(clues);
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
