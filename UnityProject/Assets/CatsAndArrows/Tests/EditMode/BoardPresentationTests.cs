using System;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    // Issue #6: presentation must expose visible clues and marks, never hidden paws.
    public sealed class BoardPresentationTests
    {
        [Test]
        public void PawCellKindKeepsLegacyNumericValue()
        {
            Assert.That((int)BoardCellKind.Empty, Is.Zero);
            Assert.That((int)BoardCellKind.X, Is.EqualTo(1));
            Assert.That((int)BoardCellKind.Paw, Is.EqualTo(2));
            Assert.That((int)BoardCellKind.Arrow, Is.EqualTo(3));
            Assert.That((int)BoardCellKind.Protected, Is.EqualTo(4));
        }

        [Test]
        public void ImmovableTilesRemainDarkerAndSymbolsFullyVisibleAfterCompletionAndNewGame()
        {
            // Issue #19: completion must not flatten arrow/protection distinction or fade ink.
            var protectedCell = new BoardCoordinate(3, 1);
            var arrow = new BoardCoordinate(4, 2);
            var normal = new BoardCoordinate(3, 2);
            var session = new GameSession(MakePuzzle(), new[] { protectedCell });
            var view = new PortraitBoardView();
            view.Bind(session, "en");
            var normalFill = Cell(view, normal).style.backgroundColor.value;
            var fixedFill = Cell(view, protectedCell).style.backgroundColor.value;
            Assert.That(Cell(view, arrow).style.backgroundColor.value, Is.EqualTo(fixedFill));
            Assert.That(Luminance(fixedFill), Is.LessThan(Luminance(normalFill)));
            foreach (var paw in session.Puzzle.SolutionPaws)
            {
                session.Tap(paw);
                session.Tap(paw);
            }
            view.Refresh();
            var completedNormal = Cell(view, normal).style.backgroundColor.value;
            var completedFixed = Cell(view, protectedCell).style.backgroundColor.value;
            Assert.That(completedNormal, Is.Not.EqualTo(normalFill));
            Assert.That(Luminance(completedFixed), Is.LessThan(Luminance(completedNormal)));
            foreach (var coordinate in new[] { arrow, protectedCell, new BoardCoordinate(1, 1) })
            {
                var tile = Cell(view, coordinate);
                Assert.That(tile.Q<BoardSymbolElement>(), Is.Not.Null);
                Assert.That(tile.style.opacity.keyword == StyleKeyword.Null || tile.style.opacity.value == 1, Is.True);
            }
            view.Bind(new GameSession(MakePuzzle(), new[] { protectedCell }), "en");
            Assert.That(Cell(view, normal).style.backgroundColor.value, Is.EqualTo(normalFill));
            Assert.That(Cell(view, protectedCell).style.backgroundColor.value, Is.EqualTo(fixedFill));
            Assert.That(view.Presentation.IsCompleted, Is.False);
        }

        private static VisualElement Cell(PortraitBoardView view, BoardCoordinate coordinate) =>
            view.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column);

        private static float Luminance(Color color) => .2126f * color.linear.r + .7152f * color.linear.g + .0722f * color.linear.b;

        [Test]
        public void UnmarkedSolutionCellsAreIndistinguishableFromOtherEmptyCells()
        {
            var session = new GameSession(MakePuzzle());
            var presentation = BoardPresentation.FromSession(session);

            Assert.That(presentation.Size, Is.EqualTo(5));
            Assert.That(presentation.Seed, Is.EqualTo(6));
            Assert.That(presentation.Cells.Count, Is.EqualTo(25));
            Assert.That(presentation.Cells.Select(cell => cell.Coordinate).Distinct().Count(), Is.EqualTo(25));
            Assert.That(presentation.Cells.Count(cell => cell.Kind == BoardCellKind.Paw), Is.Zero);
            foreach (var coordinate in session.Puzzle.SolutionPaws)
            {
                Assert.That(presentation.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.Empty));
                Assert.That(presentation.GetCell(coordinate).IsInteractive, Is.True);
            }
            Assert.That(presentation.GetCell(new BoardCoordinate(3, 2)).Kind, Is.EqualTo(BoardCellKind.Empty));
            Assert.That(presentation.RowTotals, Is.EqualTo(new[] { 0, 1, 1, 0, 0 }));
            Assert.That(presentation.ColumnTotals, Is.EqualTo(new[] { 0, 1, 1, 0, 0 }));
        }

        [Test]
        public void AllEightArrowDirectionsRetainTheirCoordinatesAndAreReadOnly()
        {
            var puzzle = MakePuzzle();
            var presentation = BoardPresentation.FromSession(new GameSession(puzzle));

            Assert.That(presentation.Cells.Count(cell => cell.Kind == BoardCellKind.Arrow), Is.EqualTo(8));
            foreach (var arrow in puzzle.Arrows)
            {
                var cell = presentation.GetCell(arrow.Key);
                Assert.That(cell.Coordinate, Is.EqualTo(arrow.Key));
                Assert.That(cell.Kind, Is.EqualTo(BoardCellKind.Arrow));
                Assert.That(cell.Direction, Is.EqualTo(arrow.Value));
                Assert.That(cell.IsInteractive, Is.False);
            }
        }

        [Test]
        public void PresentationReflectsProtectedAndPlayerMarksWithoutMutatingTheSession()
        {
            var protectedCell = new BoardCoordinate(3, 1);
            var xCell = new BoardCoordinate(1, 2);
            var wrongPaw = new BoardCoordinate(3, 3);
            var session = new GameSession(MakePuzzle(), new[] { protectedCell });
            session.Tap(xCell);
            session.Tap(wrongPaw);
            session.Tap(wrongPaw);
            var marksBefore = session.Marks.ToArray();
            var protectedBefore = session.ProtectedCells.ToArray();
            var arrowsBefore = session.Puzzle.Arrows.ToArray();

            var presentation = BoardPresentation.FromSession(session);
            Assert.That(presentation.GetCell(protectedCell).Kind, Is.EqualTo(BoardCellKind.Protected));
            Assert.That(presentation.GetCell(protectedCell).IsInteractive, Is.False);
            Assert.That(presentation.GetCell(xCell).Kind, Is.EqualTo(BoardCellKind.X));
            Assert.That(presentation.GetCell(wrongPaw).Kind, Is.EqualTo(BoardCellKind.Paw));
            Assert.That(presentation.GetCell(wrongPaw).IsInteractive, Is.True);
            Assert.That(presentation.IsCompleted, Is.False);
            Assert.That(session.Marks, Is.EquivalentTo(marksBefore));
            Assert.That(session.ProtectedCells, Is.EquivalentTo(protectedBefore));
            Assert.That(session.Puzzle.Arrows, Is.EquivalentTo(arrowsBefore));
            Assert.That(presentation.RowTotals, Is.EqualTo(new[] { 0, 1, 1, 0, 0 }));
        }

        [Test]
        public void FreshSnapshotTracksClearAndCompletionWithoutChangingEarlierSnapshots()
        {
            var session = new GameSession(MakePuzzle());
            var coordinate = new BoardCoordinate(1, 1);
            var before = BoardPresentation.FromSession(session);
            session.Tap(coordinate);
            var marked = BoardPresentation.FromSession(session);
            session.Clear();
            var cleared = BoardPresentation.FromSession(session);
            foreach (var paw in session.Puzzle.SolutionPaws)
            {
                session.Tap(paw);
                session.Tap(paw);
            }
            var completed = BoardPresentation.FromSession(session);

            Assert.That(before.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.Empty));
            Assert.That(marked.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.X));
            Assert.That(cleared.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.Empty));
            Assert.That(completed.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.Paw));
            Assert.That(before.IsCompleted, Is.False);
            Assert.That(completed.IsCompleted, Is.True);
            Assert.That(completed.Cells.All(cell => !cell.IsInteractive), Is.True);
            Assert.That(completed.Cells.Count(cell => cell.Kind == BoardCellKind.Paw), Is.EqualTo(2));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(5, 0)]
        [TestCase(0, 5)]
        public void OutOfBoardCoordinatesCannotResolveToAnotherCell(int row, int column)
        {
            var presentation = BoardPresentation.FromSession(new GameSession(MakePuzzle()));
            Assert.Throws<ArgumentOutOfRangeException>(() => presentation.GetCell(new BoardCoordinate(row, column)));
        }

        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void ViewCanBindAndRefreshBeforePanelLayoutWithoutThrowingOrChangingMarks(string locale)
        {
            // The real player binds in OnEnable before UI Toolkit has resolved geometry.
            var session = new GameSession(MakePuzzle());
            var markedCell = new BoardCoordinate(3, 2);
            session.Tap(markedCell);
            var before = session.Marks.ToArray();
            var view = new PortraitBoardView();
            Assert.That(view.panel, Is.Null);

            Assert.DoesNotThrow(() => view.Bind(session, locale));
            Assert.DoesNotThrow(() => view.ApplySafeArea(new Rect(0, 34, 393, 759), new Vector2(393, 852)));
            Assert.DoesNotThrow(() => view.Refresh());

            Assert.That(view.Presentation, Is.Not.Null);
            Assert.That(view.Presentation.Cells.Count, Is.EqualTo(25));
            Assert.That(view.Presentation.GetCell(markedCell).Kind, Is.EqualTo(BoardCellKind.X));
            Assert.That(view.Presentation.GetCell(new BoardCoordinate(1, 1)).Kind, Is.EqualTo(BoardCellKind.Empty));
            Assert.That(view.Q<VisualElement>("cell-3-2"), Is.Not.Null);
            Assert.That(view.Q<Label>("row-clue-1").text, Is.EqualTo("1"));
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void GenerationFailureBeforePanelLayoutClearsOldBoardAndRefreshCannotRestoreIt()
        {
            var view = new PortraitBoardView();
            Assert.DoesNotThrow(() => view.Bind(new GameSession(MakePuzzle()), "en"));
            Assert.DoesNotThrow(() => view.ShowGenerationFailure(GenerationStatus.Exhausted, "pt-BR"));
            Assert.DoesNotThrow(() => view.Refresh());
            Assert.That(view.Presentation, Is.Null);
            Assert.That(view.Q<VisualElement>("cell-3-2"), Is.Null);
        }

        [Test]
        public void CompletedHeaderReturnsToLocalizedIntroAfterRebindAndGenerationFailure()
        {
            var view = new PortraitBoardView();
            var session = new GameSession(MakePuzzle());
            view.Bind(session, "en");
            foreach (var paw in session.Puzzle.SolutionPaws)
            {
                session.Tap(paw);
                session.Tap(paw);
            }
            view.Refresh();
            var catalog = BoardLocalization.CreateCatalog();
            var header = view.Q<Label>("board-status");
            Assert.That(header.text, Is.EqualTo(catalog.GetResource("en").Messages["board.completed"]));

            view.Bind(new GameSession(MakePuzzle()), "pt-BR");
            Assert.That(header.text, Is.EqualTo(catalog.GetResource("pt-BR").Messages["board.intro"]));
            Assert.That(view.Presentation.IsCompleted, Is.False);

            // Returning from a completed board to a failure view must also clear victory text.
            view.Bind(session, "pt-BR");
            Assert.That(header.text, Is.EqualTo(catalog.GetResource("pt-BR").Messages["board.completed"]));
            view.ShowGenerationFailure(GenerationStatus.Exhausted, "es");
            Assert.That(header.text, Is.EqualTo(catalog.GetResource("es").Messages["board.intro"]));
            Assert.That(view.Presentation, Is.Null);
        }

        private static PuzzleDefinition MakePuzzle() => new PuzzleDefinition(5, 6, "issue-6-presentation-test",
            new[] { new BoardCoordinate(1, 1), new BoardCoordinate(2, 2) },
            new[]
            {
                new ArrowClue(new BoardCoordinate(4, 2), Direction.Up),
                new ArrowClue(new BoardCoordinate(0, 2), Direction.Down),
                new ArrowClue(new BoardCoordinate(2, 4), Direction.Left),
                new ArrowClue(new BoardCoordinate(2, 0), Direction.Right),
                new ArrowClue(new BoardCoordinate(4, 4), Direction.UpLeft),
                new ArrowClue(new BoardCoordinate(4, 0), Direction.UpRight),
                new ArrowClue(new BoardCoordinate(0, 4), Direction.DownLeft),
                new ArrowClue(new BoardCoordinate(0, 0), Direction.DownRight)
            });
    }
}
