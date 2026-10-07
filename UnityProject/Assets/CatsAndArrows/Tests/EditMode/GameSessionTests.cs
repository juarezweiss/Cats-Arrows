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
        public void TapCyclesEmptyToXToCatToEmpty()
        {
            var session = new GameSession(MakePuzzle());
            var cell = new BoardCoordinate(2, 2);

            session.Tap(cell);
            Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.X));
            session.Tap(cell);
            Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.Cat));
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
            session.Tap(b); // Cat

            var changed = session.ApplyDrag(new[] { a, a, b });

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(session.GetMark(a), Is.EqualTo(PlayerMark.X));
            Assert.That(session.GetMark(b), Is.EqualTo(PlayerMark.Cat));
        }

        [Test]
        public void ExactCatsWinImmediatelyAndLockBoard()
        {
            var session = new GameSession(MakePuzzle());

            session.Tap(new BoardCoordinate(0, 0));
            session.Tap(new BoardCoordinate(0, 0)); // Cat
            Assert.That(session.IsCompleted, Is.False);

            session.Tap(new BoardCoordinate(1, 1));
            session.Tap(new BoardCoordinate(1, 1)); // Cat

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Tap(new BoardCoordinate(2, 2)), Is.False);
            Assert.That(session.Clear(), Is.False);
        }

        [Test]
        public void ExtraCatPreventsVictory()
        {
            var session = new GameSession(MakePuzzle());
            var extraCat = new BoardCoordinate(2, 2);
            var cells = new[]
            {
                // Issue #5: place the extra cat before completing the solution,
                // otherwise immediate victory correctly locks the board first.
                extraCat,
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 1)
            };

            foreach (var cell in cells)
            {
                Assert.That(session.Tap(cell), Is.True);
                Assert.That(session.Tap(cell), Is.True);
                Assert.That(session.GetMark(cell), Is.EqualTo(PlayerMark.Cat));
            }

            Assert.That(session.IsCompleted, Is.False);
            Assert.That(session.Tap(extraCat), Is.True); // Cat -> Empty restores the exact solution.
            Assert.That(session.IsCompleted, Is.True);
        }
    }
}
