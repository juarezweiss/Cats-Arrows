using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;

namespace CatsAndArrows.Gameplay
{
    public enum TutorialStep
    {
        Welcome, Goal, Begin, StatesIntro, EmptyState, XState, CatState, HatchedCells, Arrows, Practice, ZeroColumn, ZeroRow,
        FirstCat, FirstColumnX, SecondCat, SecondRowX, DiagonalCat, FourthCat,
        SingleX, FinalCat, Independent, Complete
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
            ? Step == TutorialStep.CatState ? Cell(2, 1) : Cell(0, 0)
            : (BoardCoordinate?)null;
        public PlayerMark? DemoMark => Step switch
        {
            TutorialStep.EmptyState => PlayerMark.Empty,
            TutorialStep.XState => PlayerMark.X,
            TutorialStep.CatState => PlayerMark.Cat,
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
            TutorialStep.CatState => "tutorial.cat-state",
            TutorialStep.HatchedCells => "tutorial.hatched",
            TutorialStep.ZeroColumn => "tutorial.zero-column",
            TutorialStep.ZeroRow => "tutorial.zero-row",
            TutorialStep.FirstCat => "tutorial.first-cat",
            TutorialStep.FirstColumnX => "tutorial.first-column-x",
            TutorialStep.SecondCat => "tutorial.second-cat",
            TutorialStep.SecondRowX => "tutorial.second-row-x",
            TutorialStep.DiagonalCat => "tutorial.diagonal-cat",
            TutorialStep.FourthCat => "tutorial.fourth-cat",
            TutorialStep.SingleX => "tutorial.single-x",
            TutorialStep.FinalCat => "tutorial.final-cat",
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
                    : targets.All(cell => Session.GetMark(cell) == PlayerMark.Cat);
            if (!finished) return false;
            Advance();
            return true;
        }

        private bool IsXStep => Step == TutorialStep.ZeroColumn || Step == TutorialStep.ZeroRow ||
            Step == TutorialStep.FirstColumnX || Step == TutorialStep.SecondRowX || Step == TutorialStep.SingleX;

        private void Advance()
        {
            if (Step == TutorialStep.FinalCat)
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
                case TutorialStep.CatState:
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
                case TutorialStep.FirstCat:
                    editable = new[] { Cell(1, 4) };
                    highlights = new[] { Cell(1, 3), Cell(1, 4) };
                    break;
                case TutorialStep.FirstColumnX:
                    editable = new[] { Cell(2, 4), Cell(3, 4), Cell(4, 4) };
                    highlights = Column(4);
                    columns = new[] { 4 };
                    break;
                case TutorialStep.SecondCat:
                    editable = new[] { Cell(4, 2) };
                    highlights = new[] { Cell(3, 2), Cell(4, 2) };
                    break;
                case TutorialStep.SecondRowX:
                    editable = new[] { Cell(4, 1), Cell(4, 3) };
                    highlights = Row(4);
                    rows = new[] { 4 };
                    break;
                case TutorialStep.DiagonalCat:
                    editable = new[] { Cell(2, 1) };
                    highlights = new[] { Cell(0, 3), Cell(1, 2), Cell(2, 1), Cell(3, 0) };
                    break;
                case TutorialStep.FourthCat:
                    editable = new[] { Cell(2, 3) };
                    highlights = Row(2);
                    rows = new[] { 2 };
                    break;
                case TutorialStep.SingleX:
                    editable = new[] { Cell(3, 3) };
                    highlights = Column(3);
                    columns = new[] { 3 };
                    break;
                case TutorialStep.FinalCat:
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
