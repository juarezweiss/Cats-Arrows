using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;

namespace CatsAndArrows.UI
{
    public enum BoardCellKind { Empty = 0, X = 1, Paw = 2, Arrow = 3, Protected = 4 }

    public sealed class BoardCellPresentation
    {
        public BoardCoordinate Coordinate { get; }
        public BoardCellKind Kind { get; }
        public Direction Direction { get; }
        public bool IsInteractive { get; }
        internal BoardCellPresentation(BoardCoordinate coordinate, BoardCellKind kind, Direction direction, bool interactive)
        { Coordinate = coordinate; Kind = kind; Direction = direction; IsInteractive = interactive; }
    }

    // A detached display snapshot: visible clues and player marks only, never the answer.
    public sealed class BoardPresentation
    {
        public int Size { get; }
        public int Seed { get; }
        public bool IsCompleted { get; }
        public IReadOnlyList<int> RowTotals { get; }
        public IReadOnlyList<int> ColumnTotals { get; }
        public IReadOnlyList<BoardCellPresentation> Cells { get; }
        private BoardPresentation(GameSession session)
        {
            var clues = PuzzleClues.FromPuzzle(session.Puzzle);
            Size = clues.Size; Seed = clues.Seed; IsCompleted = session.IsCompleted;
            RowTotals = clues.RowTotals; ColumnTotals = clues.ColumnTotals;
            var arrows = clues.Arrows.ToDictionary(a => a.Coordinate, a => a.Direction);
            var protectedCells = new HashSet<BoardCoordinate>(session.ProtectedCells);
            var cells = new List<BoardCellPresentation>();
            for (var row = 0; row < Size; row++)
            for (var column = 0; column < Size; column++)
            {
                var coordinate = new BoardCoordinate(row, column);
                var kind = BoardCellKind.Empty;
                var direction = Direction.Up;
                if (arrows.TryGetValue(coordinate, out var arrow)) { kind = BoardCellKind.Arrow; direction = arrow; }
                else if (protectedCells.Contains(coordinate)) kind = BoardCellKind.Protected;
                else if (session.GetMark(coordinate) == PlayerMark.X) kind = BoardCellKind.X;
                else if (session.GetMark(coordinate) == PlayerMark.Paw) kind = BoardCellKind.Paw;
                cells.Add(new BoardCellPresentation(coordinate, kind, direction, session.CanInteract(coordinate)));
            }
            Cells = cells.AsReadOnly();
        }
        public static BoardPresentation FromSession(GameSession session) =>
            new BoardPresentation(session ?? throw new ArgumentNullException(nameof(session)));
        public BoardCellPresentation GetCell(BoardCoordinate coordinate)
        {
            if (coordinate.Row < 0 || coordinate.Row >= Size || coordinate.Column < 0 || coordinate.Column >= Size)
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            return Cells[coordinate.Row * Size + coordinate.Column];
        }
    }
}
