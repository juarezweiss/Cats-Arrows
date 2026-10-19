using System;
using System.Collections.Generic;
using System.Linq;

namespace CatsAndArrows.Core
{
    // Geometry shared by the engines; their reasoning/search algorithms are separate.
    internal sealed class ClueConstraint
    {
        public ConstraintReference Source { get; }
        public int RequiredPaws { get; }
        public int[] Cells { get; }
        public bool IsExact => Source.Kind != ConstraintKind.Arrow;
        public ClueConstraint(ConstraintReference source, int requiredPaws, IEnumerable<int> cells)
        { Source = source; RequiredPaws = requiredPaws; Cells = cells.OrderBy(i => i).ToArray(); }
    }

    internal sealed class ClueConstraints
    {
        public PuzzleClues Clues { get; }
        public ClueConstraint[] Rows { get; }
        public ClueConstraint[] Columns { get; }
        public ClueConstraint[] Arrows { get; }
        public ClueConstraint[] All { get; }
        private readonly bool[] _isArrow;

        public ClueConstraints(PuzzleClues clues)
        {
            Clues = clues;
            _isArrow = new bool[clues.Size * clues.Size];
            foreach (var arrow in clues.Arrows) _isArrow[Id(arrow.Coordinate)] = true;
            Rows = Enumerable.Range(0, clues.Size).Select(row => new ClueConstraint(
                new ConstraintReference(ConstraintKind.Row, row), clues.RowTotals[row],
                Enumerable.Range(0, clues.Size).Select(column => row * clues.Size + column).Where(i => !_isArrow[i]))).ToArray();
            Columns = Enumerable.Range(0, clues.Size).Select(column => new ClueConstraint(
                new ConstraintReference(ConstraintKind.Column, column), clues.ColumnTotals[column],
                Enumerable.Range(0, clues.Size).Select(row => row * clues.Size + column).Where(i => !_isArrow[i]))).ToArray();
            Arrows = clues.Arrows.Select(arrow => new ClueConstraint(
                new ConstraintReference(ConstraintKind.Arrow, -1, arrow.Coordinate, arrow.Direction), 1,
                DirectionGeometry.GetDirectionalSegment(clues.Size, arrow.Coordinate, arrow.Direction)
                    .Select(Id).Where(i => !_isArrow[i]))).ToArray();
            All = Rows.Concat(Columns).Concat(Arrows).ToArray();
        }

        public int Id(BoardCoordinate coordinate) => coordinate.Row * Clues.Size + coordinate.Column;
        public BoardCoordinate Coordinate(int id) => new BoardCoordinate(id / Clues.Size, id % Clues.Size);
        public CellKnowledge[] InitialStates() => _isArrow.Select(a => a ? CellKnowledge.NotPaw : CellKnowledge.Unknown).ToArray();
        public CellAssignment[] Assignments(CellKnowledge[] states) => states.Select((state, i) => new CellAssignment(Coordinate(i), state)).ToArray();
        public string InitialContradiction() => Clues.RowTotals.Sum(t => (long)t) != Clues.ColumnTotals.Sum(t => (long)t)
            ? "Row and column totals disagree." : null;

    }
}
