using System;
using System.Collections.Generic;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class DifficultySelectionTests
    {
        [TestCase("easy", 0, "medium")]
        [TestCase("easy", 1, "hard")]
        [TestCase("medium", 0, "easy")]
        [TestCase("medium", 1, "hard")]
        [TestCase("hard", 0, "easy")]
        [TestCase("hard", 1, "medium")]
        public void TwoConsecutiveStartsExcludeOnlyPreviousMode(string last, int index, string expected)
        {
            var store = new Store(new DifficultyHistory(last, 2));
            var policy = new DifficultySelection(bound => { Assert.That(bound, Is.EqualTo(2)); return index; }, store);
            Assert.That(policy.Choose().Id, Is.EqualTo(expected));
            Assert.That(store.Writes, Is.Zero);
            Assert.That(policy.CommitStart(), Is.True);
            Assert.That(policy.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [TestCase("easy", 0, "easy")]
        [TestCase("easy", 1, "medium")]
        [TestCase("medium", 2, "hard")]
        [TestCase("hard", 2, "hard")]
        public void FirstRepeatLeavesEveryModeEligible(string last, int index, string expected)
        {
            var policy = new DifficultySelection(bound => { Assert.That(bound, Is.EqualTo(3)); return index; },
                new Store(new DifficultyHistory(last, 1)));
            Assert.That(policy.Choose().Id, Is.EqualTo(expected));
            policy.CommitStart();
            Assert.That(policy.History.ConsecutiveStarts, Is.EqualTo(expected == last ? 2 : 1));
        }

        [Test]
        public void PendingSelectionAndDuplicateCommitDoNotConsumeRandomnessOrHistory()
        {
            var draws = 0; var store = new Store();
            var policy = new DifficultySelection(bound => { draws++; return 0; }, store);
            Assert.That(policy.CommitStart(), Is.False);
            Assert.That(policy.Choose(), Is.SameAs(policy.Choose()));
            Assert.That(draws, Is.EqualTo(1)); Assert.That(store.Writes, Is.Zero);
            Assert.That(policy.CommitStart(), Is.True);
            Assert.That(policy.CommitStart(), Is.False);
            Assert.That(store.Writes, Is.EqualTo(1));
        }

        [TestCase("easy", 0, 1)] [TestCase("easy", 3, 1)]
        [TestCase("other", 1, 1)] [TestCase(null, 1, 1)]
        [TestCase("hard", 2, 2)] [TestCase("medium", -1, 1)]
        public void InvalidOrUnsupportedHistoryStartsWithAllModes(string last, int count, int version)
        {
            var store = new Store(new DifficultyHistory(last, count, version));
            var policy = new DifficultySelection(bound => { Assert.That(bound, Is.EqualTo(3)); return 0; }, store);
            Assert.That(policy.History, Is.Null);
            Assert.That(policy.Choose().Id, Is.EqualTo("easy"));
            Assert.That(store.Writes, Is.Zero, "loading/selection must not persist an unstarted board");
            policy.CommitStart(); Assert.That(policy.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [TestCase(-1)] [TestCase(3)]
        public void InvalidInjectedRandomIndexCannotCreatePendingOrHistory(int index)
        {
            var policy = new DifficultySelection(_ => index);
            Assert.Throws<InvalidOperationException>(() => policy.Choose());
            Assert.That(policy.Pending, Is.Null); Assert.That(policy.History, Is.Null);
        }

        [Test]
        public void SeededSequenceIsReproducibleAndReloadsContinueTheStreak()
        {
            var first = Sequence(431, false);
            CollectionAssert.AreEqual(first, Sequence(431, true));
            CollectionAssert.AreEqual(first, Sequence(431, false));
            CollectionAssert.AreNotEqual(first, Sequence(432, false));
            Assert.That(new HashSet<string>(first).Count, Is.EqualTo(3));
            for (var i = 2; i < first.Count; i++)
                Assert.That(first[i] == first[i - 1] && first[i] == first[i - 2], Is.False, "start " + i);
        }

        [Test]
        public void SaveFailureKeepsPendingAndPreviousHistoryUntilRetrySucceeds()
        {
            var old = new DifficultyHistory("hard", 2);
            var store = new Store(old) { CanSave = false };
            var draws = 0;
            var policy = new DifficultySelection(_ => { draws++; return 0; }, store);
            var choice = policy.Choose();
            Assert.That(policy.CommitStart(), Is.False);
            Assert.That(policy.History, Is.SameAs(old));
            Assert.That(policy.Choose(), Is.SameAs(choice)); Assert.That(draws, Is.EqualTo(1));
            store.CanSave = true;
            Assert.That(policy.CommitStart(), Is.True);
            Assert.That(policy.History.LastDifficulty, Is.EqualTo("easy"));
            Assert.That(policy.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [Test]
        public void SaveFailureDoesNotExposeSessionAndRetryRetainsChoiceWithNextSeed()
        {
            var store = new Store { CanSave = false };
            var policy = new DifficultySelection(_ => 0, store);
            var loop = new EasyGameLoop(14, difficultySelection: policy);
            Assert.That(loop.NewGame(), Is.False);
            Assert.That(loop.Session, Is.Null); Assert.That(loop.SessionDifficulty, Is.Null);
            Assert.That(loop.LastGenerationStatus, Is.EqualTo(GenerationStatus.Exhausted));
            Assert.That(policy.History, Is.Null); Assert.That(policy.Pending.Id, Is.EqualTo("easy"));
            store.CanSave = true;
            Assert.That(loop.NewGame(), Is.True);
            Assert.That(loop.RequestedSeed, Is.EqualTo(15));
            Assert.That(loop.SessionDifficulty.Id, Is.EqualTo("easy"));
            Assert.That(policy.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [Test]
        public void ReentrantStartCannotConsumeASeedOrCommitTwice()
        {
            EasyGameLoop loop = null;
            var calls = 0;
            loop = PlayTestFactory.Loop(14, seed =>
            {
                calls++;
                Assert.That(loop.NewGame(), Is.False);
                return new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, seed);
            });
            Assert.That(loop.NewGame(), Is.True);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(loop.RequestedSeed, Is.EqualTo(14));
            Assert.That(loop.DifficultySelection.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [Test]
        public void DefaultSeededPolicyFreezesCrossRuntimeSequence()
        {
            var policy = DifficultySelection.CreateSeeded(431);
            var actual = new List<string>();
            for (var i = 0; i < 12; i++)
            { actual.Add(policy.Choose().Id); policy.CommitStart(); }
            Assert.That(actual, Is.EqualTo(new[] { "medium", "hard", "hard", "medium", "hard", "hard", "medium", "easy", "hard", "easy", "medium", "hard" }));
        }

        private static List<string> Sequence(int seed, bool reload)
        {
            var store = new Store();
            var random = new SplitMix64Random(SplitMix64Random.SeedToState(seed));
            Func<int, int> next = bound =>
            {
                Assert.That(random.TryNextInt(bound, 128, out var index), Is.True);
                return index;
            };
            var policy = new DifficultySelection(next, store);
            var starts = new List<string>();
            for (var i = 0; i < 512; i++)
            {
                if (reload) policy = new DifficultySelection(next, store);
                starts.Add(policy.Choose().Id); Assert.That(policy.CommitStart(), Is.True);
            }
            return starts;
        }

        private sealed class Store : IDifficultyHistoryStore
        {
            private DifficultyHistory value;
            public bool CanSave = true;
            public int Writes;
            public Store(DifficultyHistory initial = null) { value = initial; }
            public DifficultyHistory Load() => value;
            public bool TrySave(DifficultyHistory history)
            { Writes++; if (!CanSave) return false; value = history; return true; }
        }
    }
}
