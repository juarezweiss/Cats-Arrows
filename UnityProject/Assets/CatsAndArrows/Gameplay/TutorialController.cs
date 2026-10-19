using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public enum TutorialStep
    {
        Welcome = 0, Goal = 1, Begin = 2, StatesIntro = 3, EmptyState = 4, XState = 5, PawState = 6,
        HatchedCells = 7, Arrows = 8, Practice = 9, ZeroColumn = 10, ZeroRow = 11,
        FirstPaw = 12, FirstColumnX = 13, SecondPaw = 14, SecondRowX = 15, DiagonalPaw = 16, FourthPaw = 17,
        SingleX = 18, FinalPaw = 19, Independent = 20, Complete = 21
    }

    // Issue #26: controls lesson progression, while GameSession owns every mark and win.
    // UI must route intents here and end the gesture before refreshing lesson targets.
    public sealed class TutorialController
    {
        private int practiceTaps;
        private IReadOnlyList<BoardCoordinate> targets;
        private IReadOnlyList<BoardCoordinate> highlightedCells;
        private IReadOnlyList<int> highlightedRows;
        private IReadOnlyList<int> highlightedColumns;

        public GameSession Session { get; private set; }
        public TutorialStep Step { get; private set; } = TutorialStep.Welcome;
        public bool CanContinue => Step <= TutorialStep.Arrows;
        public bool IsComplete => Step == TutorialStep.Complete;
        // These are read-only presentation samples, never player/session mutations.
        public BoardCoordinate? DemoCoordinate => DemoMark.HasValue
            ? Step == TutorialStep.PawState ? Cell(2, 1) : Cell(0, 0)
            : (BoardCoordinate?)null;
        public PlayerMark? DemoMark => Step switch
        {
            TutorialStep.EmptyState => PlayerMark.Empty,
            TutorialStep.XState => PlayerMark.X,
            TutorialStep.PawState => PlayerMark.Paw,
            _ => (PlayerMark?)null
        };
        public IReadOnlyList<BoardCoordinate> Targets => targets;
        public IReadOnlyList<BoardCoordinate> HighlightedCells => highlightedCells;
        public IReadOnlyList<int> HighlightedRows => highlightedRows;
        public IReadOnlyList<int> HighlightedColumns => highlightedColumns;
        public string MessageKey => Step switch
        {
            TutorialStep.StatesIntro => "tutorial.states-intro",
            TutorialStep.EmptyState => "tutorial.empty-state",
            TutorialStep.XState => "tutorial.x-state",
            TutorialStep.PawState => "tutorial.paw-state",
            TutorialStep.HatchedCells => "tutorial.hatched",
            TutorialStep.ZeroColumn => "tutorial.zero-column",
            TutorialStep.ZeroRow => "tutorial.zero-row",
            TutorialStep.FirstPaw => "tutorial.first-paw",
            TutorialStep.FirstColumnX => "tutorial.first-column-x",
            TutorialStep.SecondPaw => "tutorial.second-paw",
            TutorialStep.SecondRowX => "tutorial.second-row-x",
            TutorialStep.DiagonalPaw => "tutorial.diagonal-paw",
            TutorialStep.FourthPaw => "tutorial.fourth-paw",
            TutorialStep.SingleX => "tutorial.single-x",
            TutorialStep.FinalPaw => "tutorial.final-paw",
            _ => "tutorial." + Step.ToString().ToLowerInvariant()
        };

        public TutorialController()
        {
            Session = TutorialFixture.CreateSession();
            SetTargets();
        }

        public bool Continue()
        {
            if (!CanContinue) return false;
            Advance();
            return true;
        }

        public bool CanInteract(BoardCoordinate coordinate) =>
            !CanContinue && !IsComplete && targets.Contains(coordinate) && Session.CanInteract(coordinate);

        public bool CanTap(BoardCoordinate coordinate) => CanInteract(coordinate);

        public bool CanDrag(BoardCoordinate coordinate) => CanInteract(coordinate) && (IsXStep || Step == TutorialStep.Independent);

        public bool Tap(BoardCoordinate coordinate)
        {
            if (!CanTap(coordinate) || !Session.Tap(coordinate)) return false;
            if (Step == TutorialStep.Practice) practiceTaps++;
            return true;
        }

        public int ApplyDrag(IEnumerable<BoardCoordinate> coordinates)
        {
            if (coordinates == null) return 0;
            return Session.ApplyDrag(coordinates.Where(CanDrag));
        }

        // Called after release or cancellation. At most one step advances, even if the
        // same swept path intersects targets belonging to the next lesson.
        public bool EndGesture()
        {
            if (CanContinue || IsComplete) return false;
            var finished = Step == TutorialStep.Independent
                ? Session.IsCompleted
                : Step == TutorialStep.Practice
                ? practiceTaps >= 3 && Session.GetMark(targets[0]) == PlayerMark.Empty
                : IsXStep
                    ? targets.All(cell => Session.GetMark(cell) == PlayerMark.X)
                    : targets.All(cell => Session.GetMark(cell) == PlayerMark.Paw);
            if (!finished) return false;
            Advance();
            return true;
        }

        private bool IsXStep => Step == TutorialStep.ZeroColumn || Step == TutorialStep.ZeroRow ||
            Step == TutorialStep.FirstColumnX || Step == TutorialStep.SecondRowX || Step == TutorialStep.SingleX;

        private void Advance()
        {
            if (Step == TutorialStep.FinalPaw)
                Session = TutorialFixture.CreateSession();
            Step = (TutorialStep)((int)Step + 1);
            SetTargets();
        }

        private static BoardCoordinate Cell(int row, int column) => new BoardCoordinate(row, column);
        private static BoardCoordinate[] Row(int row) => Enumerable.Range(0, 5).Select(column => Cell(row, column)).ToArray();
        private static BoardCoordinate[] Column(int column) => Enumerable.Range(0, 5).Select(row => Cell(row, column)).ToArray();

        private void SetTargets()
        {
            var editable = Array.Empty<BoardCoordinate>();
            var highlights = Array.Empty<BoardCoordinate>();
            var rows = Array.Empty<int>();
            var columns = Array.Empty<int>();
            switch (Step)
            {
                case TutorialStep.EmptyState:
                case TutorialStep.XState:
                case TutorialStep.PawState:
                    highlights = new[] { DemoCoordinate.Value };
                    break;
                case TutorialStep.HatchedCells:
                    highlights = Session.ProtectedCells.ToArray();
                    break;
                case TutorialStep.Arrows:
                    highlights = Session.Puzzle.Arrows.Keys.ToArray();
                    break;
                case TutorialStep.Practice:
                    editable = new[] { Cell(0, 0) };
                    break;
                case TutorialStep.ZeroColumn:
                    highlights = Column(0);
                    editable = highlights.Where(Session.CanInteract).ToArray();
                    columns = new[] { 0 };
                    break;
                case TutorialStep.ZeroRow:
                    highlights = Row(0);
                    editable = highlights.Where(cell => Session.CanInteract(cell) && cell.Column != 0).ToArray();
                    rows = new[] { 0 };
                    break;
                case TutorialStep.FirstPaw:
                    editable = new[] { Cell(1, 4) };
                    highlights = new[] { Cell(1, 3), Cell(1, 4) };
                    break;
                case TutorialStep.FirstColumnX:
                    editable = new[] { Cell(2, 4), Cell(3, 4), Cell(4, 4) };
                    highlights = Column(4);
                    columns = new[] { 4 };
                    break;
                case TutorialStep.SecondPaw:
                    editable = new[] { Cell(4, 2) };
                    highlights = new[] { Cell(3, 2), Cell(4, 2) };
                    break;
                case TutorialStep.SecondRowX:
                    editable = new[] { Cell(4, 1), Cell(4, 3) };
                    highlights = Row(4);
                    rows = new[] { 4 };
                    break;
                case TutorialStep.DiagonalPaw:
                    editable = new[] { Cell(2, 1) };
                    highlights = new[] { Cell(0, 3), Cell(1, 2), Cell(2, 1), Cell(3, 0) };
                    break;
                case TutorialStep.FourthPaw:
                    editable = new[] { Cell(2, 3) };
                    highlights = Row(2);
                    rows = new[] { 2 };
                    break;
                case TutorialStep.SingleX:
                    editable = new[] { Cell(3, 3) };
                    highlights = Column(3);
                    columns = new[] { 3 };
                    break;
                case TutorialStep.FinalPaw:
                    editable = new[] { Cell(3, 1) };
                    highlights = Column(1);
                    columns = new[] { 1 };
                    break;
                case TutorialStep.Independent:
                    editable = Session.Marks.Keys.ToArray();
                    break;
            }
            targets = Array.AsReadOnly(editable);
            highlightedCells = Array.AsReadOnly(highlights.Length == 0 && Step != TutorialStep.Independent ? editable : highlights);
            highlightedRows = Array.AsReadOnly(rows);
            highlightedColumns = Array.AsReadOnly(columns);
        }
    }
}
