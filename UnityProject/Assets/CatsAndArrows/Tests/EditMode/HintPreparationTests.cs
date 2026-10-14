using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #30: reward eligibility must not place a mark before a trusted reward.
    public sealed class HintPreparationTests
    {
        private static GameSession Session() => new GameSession(
            new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, 14).Puzzle);

        [Test]
        public void PreparationIsStableNonmutatingAndMatchesApplication()
        {
            var session = Session();
            var before = session.Marks.ToArray();
            var prepared = session.PrepareHint();
            var repeated = session.PrepareHint();
            Assert.That(prepared.Status, Is.EqualTo(HintStatus.Applied));
            Assert.That(prepared.Assignment, Is.EqualTo(repeated.Assignment));
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(session.MarkRevision, Is.Zero);
            var applied = session.ApplyHint();
            Assert.That(applied.Assignment, Is.EqualTo(prepared.Assignment));
            Assert.That(session.MarkRevision, Is.EqualTo(1));
        }

        [Test]
        public void EveryActualMutationInvalidatesPreparedRevisionButRejectedIntentsDoNot()
        {
            var session = Session();
            var cell = session.Marks.Keys.First();
            session.Clear();
            Assert.That(session.MarkRevision, Is.Zero);
            session.Tap(cell); Assert.That(session.MarkRevision, Is.EqualTo(1));
            session.Tap(cell); Assert.That(session.MarkRevision, Is.EqualTo(2));
            session.Tap(cell); Assert.That(session.MarkRevision, Is.EqualTo(3));
            session.ApplyDrag(new[] { cell, cell }); Assert.That(session.MarkRevision, Is.EqualTo(4));
            session.ApplyDrag(new[] { cell }); Assert.That(session.MarkRevision, Is.EqualTo(4));
            session.Tap(new BoardCoordinate(-1, -1)); Assert.That(session.MarkRevision, Is.EqualTo(4));
            session.Clear(); Assert.That(session.MarkRevision, Is.EqualTo(5));
            session.Clear(); Assert.That(session.MarkRevision, Is.EqualTo(5));
        }

        [Test]
        public void PreparationRechecksMarksAndCompletedBoardRemainsLocked()
        {
            var session = Session();
            var first = session.PrepareHint();
            session.ApplyHint();
            Assert.That(session.PrepareHint().Assignment, Is.Not.EqualTo(first.Assignment));
            for (var i = 0; i < 25 && !session.IsCompleted; i++) session.ApplyHint();
            Assert.That(session.IsCompleted, Is.True);
            var revision = session.MarkRevision;
            Assert.That(session.PrepareHint().Status, Is.EqualTo(HintStatus.Completed));
            Assert.That(session.ApplyHint().Status, Is.EqualTo(HintStatus.Completed));
            Assert.That(session.Clear(), Is.False);
            Assert.That(session.ApplyDrag(session.Marks.Keys), Is.Zero);
            Assert.That(session.MarkRevision, Is.EqualTo(revision));
        }
    }
}
