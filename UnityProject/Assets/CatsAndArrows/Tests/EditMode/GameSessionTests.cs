using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class GameSessionTests
    {
        private static PuzzleDefinition MakePuzzle() => new PuzzleDefinition(
            5,
            7,
            "test",
            new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 1) },
            new[] { new ArrowClue(new BoardCoordinate(0, 1), Direction.Left) });

        [Test]
        public void PawMarkKeepsLegacyNumericValue()
        {
            Assert.That((int)PlayerMark.Empty, Is.Zero);
            Assert.That((int)PlayerMark.X, Is.EqualTo(1));
            Assert.That((int)PlayerMark.Paw, Is.EqualTo(2));
        }

        [Test]
        public void TapCyclesEmptyToXToPawToEmpty()
        {
            var session = new GameSession(MakePuzzle());
            var cell = new BoardCoordinate(2, 2);

            session.Tap(cell);
            Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.X));
            session.Tap(cell);
            Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.Paw));
            session.Tap(cell);
            Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void ArrowCellIsNotInteractive()
        {
            var session = new GameSession(MakePuzzle());
            var arrow = new BoardCoordinate(0, 1);

            Assert.That(session.Tap(arrow), Is.False);
        }

        [Test]
        public void DragMarksOnlyEmptyCellsAndProcessesDuplicatesOnce()
        {
            var session = new GameSession(MakePuzzle());
            var a = new BoardCoordinate(2, 0);
            var b = new BoardCoordinate(2, 1);
            session.Tap(b); // X
            session.Tap(b); // Paw

            var changed = session.ApplyDrag(new[] { a, a, b });

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(session.GetMark(a), Is.EqualTo(PlayerMark.X));
            Assert.That(session.GetMark(b), Is.EqualTo(PlayerMark.Paw));
        }

        [Test]
        public void ExactPawsWinImmediatelyAndLockBoard()
        {
            var session = new GameSession(MakePuzzle());

            session.Tap(new BoardCoordinate(0, 0));
            session.Tap(new BoardCoordinate(0, 0)); // Paw
            Assert.That(session.IsCompleted, Is.False);

            session.Tap(new BoardCoordinate(1, 1));
            session.Tap(new BoardCoordinate(1, 1)); // Paw

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Tap(new BoardCoordinate(2, 2)), Is.False);
            Assert.That(session.Clear(), Is.False);
        }

        [Test]
        public void ExtraPawPreventsVictory()
        {
            var session = new GameSession(MakePuzzle());
            var extraPaw = new BoardCoordinate(2, 2);
            var cells = new[]
            {
                // Issue #5: place the extra paw before completing the solution,
                // otherwise immediate victory correctly locks the board first.
                extraPaw,
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 1)
            };

            foreach (var cell in cells)
            {
                Assert.That(session.Tap(cell), Is.True);
                Assert.That(session.Tap(cell), Is.True);
                Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.Paw));
            }

            Assert.That(session.IsCompleted, Is.False);
            Assert.That(session.Tap(extraPaw), Is.True); // Paw -> Empty restores the exact solution.
            Assert.That(session.IsCompleted, Is.True);
        }

        // Issue #5: session views must not expose mutation paths around the rules.
        [Test]
        public void MarksViewIsReadOnlyAndRemainsLiveAcrossTapAndClear()
        {
            var session = new GameSession(MakePuzzle());
            var cell = new BoardCoordinate(2, 2);
            var view = session.Marks;

            AssertMarkViewRejectsMutation(session, cell);
            Assert.That(session.Tap(cell), Is.True);
            Assert.That(view[cell], Is.EqualTo(PlayerMark.X));
            Assert.That(session.Tap(cell), Is.True);
            Assert.That(view[cell], Is.EqualTo(PlayerMark.Paw));
            Assert.That(session.Clear(), Is.True);
            Assert.That(view[cell], Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void CompletedMarksViewRejectsMutationAndPreservesTheWin()
        {
            var session = new GameSession(MakePuzzle());
            var view = session.Marks;
            PlacePaw(session, new BoardCoordinate(0, 0));
            PlacePaw(session, new BoardCoordinate(1, 1));

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(view[new BoardCoordinate(1, 1)], Is.EqualTo(PlayerMark.Paw));
            AssertMarkViewRejectsMutation(session, new BoardCoordinate(0, 0));
            Assert.That(session.IsCompleted, Is.True);
        }

        [Test]
        public void ProtectedCellsAreCopiedDeduplicatedAndReadOnlyBeforeAndAfterCompletion()
        {
            var protectedCell = new BoardCoordinate(3, 3);
            var addedLater = new BoardCoordinate(3, 4);
            var input = new List<BoardCoordinate> { protectedCell, protectedCell };
            var session = new GameSession(MakePuzzle(), input);
            var view = session.ProtectedCells;
            input.Clear();
            input.Add(addedLater);

            Assert.That(view, Is.EquivalentTo(new[] { protectedCell }));
            Assert.That(session.CanInteract(protectedCell), Is.False);
            Assert.That(session.CanInteract(addedLater), Is.True);
            AssertProtectedViewRejectsMutation(session, addedLater);

            PlacePaw(session, new BoardCoordinate(0, 0));
            PlacePaw(session, new BoardCoordinate(1, 1));

            Assert.That(session.IsCompleted, Is.True);
            AssertProtectedViewRejectsMutation(session, addedLater);
            Assert.That(view, Is.EquivalentTo(new[] { protectedCell }));
        }

        [Test]
        public void ProtectedCellsIgnoreTapDragAndClear()
        {
            var protectedCell = new BoardCoordinate(3, 3);
            var normalCell = new BoardCoordinate(3, 4);
            var session = new GameSession(MakePuzzle(), new[] { protectedCell });

            Assert.That(session.Marks.ContainsKey(protectedCell), Is.False);
            Assert.That(session.GetMark(protectedCell), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.Tap(protectedCell), Is.False);
            Assert.That(session.ApplyDrag(new[] { protectedCell, normalCell, protectedCell }), Is.EqualTo(1));
            Assert.That(session.GetMark(protectedCell), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.Clear(), Is.True);
            Assert.That(session.GetMark(normalCell), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.CanInteract(protectedCell), Is.False);
            Assert.That(session.ProtectedCells, Does.Contain(protectedCell));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(5, 0)]
        [TestCase(0, 5)]
        public void ProtectedCoordinatesOutsideAnyBoardEdgeAreRejected(int row, int column)
        {
            var error = Assert.Throws<ArgumentException>(() =>
                new GameSession(MakePuzzle(), new[] { new BoardCoordinate(row, column) }));

            Assert.That(error.ParamName, Is.EqualTo("protectedCells"));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(5, 0)]
        [TestCase(0, 5)]
        public void CoordinatesOutsideAnyBoardEdgeCannotChangeTheSession(int row, int column)
        {
            var session = new GameSession(MakePuzzle());
            var coordinate = new BoardCoordinate(row, column);
            var before = session.Marks.ToArray();

            Assert.That(session.CanInteract(coordinate), Is.False);
            Assert.That(session.GetMark(coordinate), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.Tap(coordinate), Is.False);
            Assert.That(session.ApplyDrag(new[] { coordinate, coordinate }), Is.Zero);
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void ProtectingASolutionPawIsRejectedInsteadOfCreatingAnUnwinnableSession()
        {
            var error = Assert.Throws<ArgumentException>(() =>
                new GameSession(MakePuzzle(), new[] { new BoardCoordinate(0, 0) }));

            Assert.That(error.ParamName, Is.EqualTo("protectedCells"));
        }

        [Test]
        public void NullPuzzleIsRejectedAndNullProtectedCellsMeansNoProtection()
        {
            var error = Assert.Throws<ArgumentNullException>(() => new GameSession(null));
            Assert.That(error.ParamName, Is.EqualTo("puzzle"));

            var session = new GameSession(MakePuzzle(), null);
            Assert.That(session.ProtectedCells, Is.Empty);
            Assert.That(session.Marks.Count, Is.EqualTo(24));
            Assert.That(session.CanInteract(new BoardCoordinate(0, 0)), Is.True);
        }

        [Test]
        public void ArrowCoordinatesAreExcludedFromAdditionalProtectionAndRemainNonInteractive()
        {
            var arrow = new BoardCoordinate(0, 1);
            var protectedCell = new BoardCoordinate(3, 3);
            var session = new GameSession(MakePuzzle(), new[] { arrow, protectedCell, arrow });

            Assert.That(session.ProtectedCells, Is.EquivalentTo(new[] { protectedCell }));
            Assert.That(session.Marks.ContainsKey(arrow), Is.False);
            Assert.That(session.CanInteract(arrow), Is.False);
            Assert.That(session.Tap(arrow), Is.False);
            Assert.That(session.ApplyDrag(new[] { arrow, arrow }), Is.Zero);
            Assert.That(session.Clear(), Is.True);
            Assert.That(session.CanInteract(arrow), Is.False);
        }

        [Test]
        public void MixedDragChangesOnlyEligibleEmptyCellsAndTheNextTapAdvancesXToPaw()
        {
            var emptyCell = new BoardCoordinate(2, 0);
            var xCell = new BoardCoordinate(2, 1);
            var pawCell = new BoardCoordinate(2, 2);
            var arrow = new BoardCoordinate(0, 1);
            var protectedCell = new BoardCoordinate(2, 3);
            var session = new GameSession(MakePuzzle(), new[] { protectedCell });
            Assert.That(session.Tap(xCell), Is.True);
            PlacePaw(session, pawCell);

            var changed = session.ApplyDrag(new[]
            {
                emptyCell, xCell, pawCell, arrow, protectedCell,
                new BoardCoordinate(-1, 0), new BoardCoordinate(5, 0),
                emptyCell, xCell, pawCell
            });

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(session.GetMark(emptyCell), Is.EqualTo(PlayerMark.X));
            Assert.That(session.GetMark(xCell), Is.EqualTo(PlayerMark.X));
            Assert.That(session.GetMark(pawCell), Is.EqualTo(PlayerMark.Paw));
            Assert.That(session.GetMark(arrow), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.GetMark(protectedCell), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.ApplyDrag(new[] { emptyCell, xCell, pawCell }), Is.Zero);
            Assert.That(session.Tap(emptyCell), Is.True);
            Assert.That(session.GetMark(emptyCell), Is.EqualTo(PlayerMark.Paw));
            Assert.That(session.Tap(emptyCell), Is.True);
            Assert.That(session.GetMark(emptyCell), Is.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void NullAndEmptyDragAreNoOps()
        {
            var session = new GameSession(MakePuzzle());
            var before = session.Marks.ToArray();

            Assert.That(session.ApplyDrag(null), Is.Zero);
            Assert.That(session.ApplyDrag(Array.Empty<BoardCoordinate>()), Is.Zero);
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void ClearResetsMarksWhileRetainingPuzzleSeedArrowsAndProtection()
        {
            var puzzle = MakePuzzle();
            var protectedCell = new BoardCoordinate(3, 3);
            var session = new GameSession(puzzle, new[] { protectedCell });
            Assert.That(session.Tap(new BoardCoordinate(2, 0)), Is.True);
            PlacePaw(session, new BoardCoordinate(2, 1));
            PlacePaw(session, new BoardCoordinate(0, 0));

            Assert.That(session.Clear(), Is.True);
            Assert.That(session.Puzzle, Is.SameAs(puzzle));
            Assert.That(session.Puzzle.Seed, Is.EqualTo(7));
            Assert.That(session.Puzzle.GeneratorVersion, Is.EqualTo("test"));
            Assert.That(session.Puzzle.Arrows[new BoardCoordinate(0, 1)], Is.EqualTo(Direction.Left));
            Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(session.Marks.Count, Is.EqualTo(23));
            Assert.That(session.ProtectedCells, Is.EquivalentTo(new[] { protectedCell }));
            Assert.That(session.CanInteract(protectedCell), Is.False);
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(session.Clear(), Is.True);
        }

        [Test]
        public void MixedEmptyAndXNonPawsPermitVictoryAndAllCompletedOperationsPreserveMarks()
        {
            var session = new GameSession(MakePuzzle());
            var xCell = new BoardCoordinate(2, 0);
            var emptyCell = new BoardCoordinate(2, 1);
            Assert.That(session.Tap(xCell), Is.True);
            PlacePaw(session, new BoardCoordinate(0, 0));
            PlacePaw(session, new BoardCoordinate(1, 1));
            var completedMarks = session.Marks.ToArray();

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.GetMark(xCell), Is.EqualTo(PlayerMark.X));
            Assert.That(session.GetMark(emptyCell), Is.EqualTo(PlayerMark.Empty));
            Assert.That(session.Marks.Keys.All(cell => !session.CanInteract(cell)), Is.True);
            Assert.That(session.Tap(new BoardCoordinate(0, 0)), Is.False);
            Assert.That(session.Tap(xCell), Is.False);
            Assert.That(session.Tap(emptyCell), Is.False);
            Assert.That(session.ApplyDrag(new[] { emptyCell, xCell, new BoardCoordinate(0, 0) }), Is.Zero);
            Assert.That(session.Clear(), Is.False);
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Marks, Is.EquivalentTo(completedMarks));
        }

        [Test]
        public void ZeroPawPuzzleCompletesAtConstructionAndIsReadOnlyImmediately()
        {
            var puzzle = new PuzzleDefinition(2, 12, "test", Array.Empty<BoardCoordinate>(), Array.Empty<ArrowClue>());
            var session = new GameSession(puzzle);
            var before = session.Marks.ToArray();

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.CanInteract(new BoardCoordinate(0, 0)), Is.False);
            Assert.That(session.Tap(new BoardCoordinate(0, 0)), Is.False);
            Assert.That(session.ApplyDrag(new[] { new BoardCoordinate(0, 0) }), Is.Zero);
            Assert.That(session.Clear(), Is.False);
            Assert.That(session.Marks, Is.EquivalentTo(before));
        }

        private static void PlacePaw(GameSession session, BoardCoordinate coordinate)
        {
            Assert.That(session.Tap(coordinate), Is.True);
            Assert.That(session.Tap(coordinate), Is.True);
            Assert.That(session.GetMark(coordinate), Is.EqualTo(PlayerMark.Paw));
        }

        private static void AssertMarkViewRejectsMutation(GameSession session, BoardCoordinate coordinate)
        {
            var before = session.Marks.ToArray();
            var dictionary = session.Marks as IDictionary<BoardCoordinate, PlayerMark>;
            Assert.That(dictionary, Is.Not.Null);
            Assert.That(dictionary.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => dictionary[coordinate] = PlayerMark.X);
            Assert.Throws<NotSupportedException>(() => dictionary.Add(new BoardCoordinate(-1, -1), PlayerMark.Paw));
            Assert.Throws<NotSupportedException>(() => dictionary.Remove(coordinate));
            Assert.Throws<NotSupportedException>(() => dictionary.Clear());
            var collection = (ICollection<KeyValuePair<BoardCoordinate, PlayerMark>>)dictionary;
            Assert.Throws<NotSupportedException>(() => collection.Add(new KeyValuePair<BoardCoordinate, PlayerMark>(coordinate, PlayerMark.Paw)));
            Assert.Throws<NotSupportedException>(() => collection.Remove(new KeyValuePair<BoardCoordinate, PlayerMark>(coordinate, session.GetMark(coordinate))));
            Assert.That(session.Marks, Is.EquivalentTo(before));
        }

        private static void AssertProtectedViewRejectsMutation(GameSession session, BoardCoordinate coordinate)
        {
            var before = session.ProtectedCells.ToArray();
            var collection = session.ProtectedCells as ICollection<BoardCoordinate>;
            Assert.That(collection, Is.Not.Null);
            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Add(coordinate));
            Assert.Throws<NotSupportedException>(() => collection.Remove(before[0]));
            Assert.Throws<NotSupportedException>(() => collection.Clear());
            Assert.That(session.ProtectedCells, Is.EquivalentTo(before));
        }
    }
}
