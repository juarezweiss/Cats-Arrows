using System.Linq;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;

namespace CatsAndArrows.Tests
{
    // Issue #43 adapter/migration tests are the only Play tests touching durable
    // preferences. Restore every touched key, including absence, for the owner.
    public sealed class PlayPreferenceTests
    {
        private readonly string[] keys = { LanguagePreference.Key, TutorialPreference.Key,
            FirstPlayPreference.Key, DifficultyHistoryPreference.Key };
        private bool[] existed;
        private string language, history;
        private int seen, played;
        [SetUp] public void PreserveOwnerPreferences()
        {
            existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            language = PlayerPrefs.GetString(LanguagePreference.Key);
            history = PlayerPrefs.GetString(DifficultyHistoryPreference.Key);
            seen = PlayerPrefs.GetInt(TutorialPreference.Key);
            played = PlayerPrefs.GetInt(FirstPlayPreference.Key);
            foreach (var key in keys.Skip(1)) PlayerPrefs.DeleteKey(key);
        }
        [TearDown] public void RestoreOwnerPreferences()
        {
            if (existed[0]) PlayerPrefs.SetString(keys[0], language); else PlayerPrefs.DeleteKey(keys[0]);
            if (existed[1]) PlayerPrefs.SetInt(keys[1], seen); else PlayerPrefs.DeleteKey(keys[1]);
            if (existed[2]) PlayerPrefs.SetInt(keys[2], played); else PlayerPrefs.DeleteKey(keys[2]);
            if (existed[3]) PlayerPrefs.SetString(keys[3], history); else PlayerPrefs.DeleteKey(keys[3]);
            PlayerPrefs.Save();
        }

        [TestCase("easy", 1)] [TestCase("medium", 2)] [TestCase("hard", 2)]
        public void HistoryRoundTripsThroughNewAdapterInstances(string id, int count)
        {
            Assert.That(new DifficultyHistoryPreference().TrySave(new DifficultyHistory(id, count)), Is.True);
            var reloaded = new DifficultyHistoryPreference().Load();
            Assert.That(reloaded.Version, Is.EqualTo(1)); Assert.That(reloaded.LastDifficulty, Is.EqualTo(id));
            Assert.That(reloaded.ConsecutiveStarts, Is.EqualTo(count));
        }

        [TestCase("")] [TestCase("junk")] [TestCase("1|easy")] [TestCase("1|easy|bad")]
        [TestCase("2|easy|2")] [TestCase("1|other|2")] [TestCase("1|hard|4")]
        public void CorruptOrUnsupportedRecordCannotRestrictFreshSelection(string record)
        {
            PlayerPrefs.SetString(DifficultyHistoryPreference.Key, record);
            var policy = new DifficultySelection(bound => { Assert.That(bound, Is.EqualTo(3)); return 0; },
                new DifficultyHistoryPreference());
            Assert.That(policy.History, Is.Null); Assert.That(policy.Choose().Id, Is.EqualTo("easy"));
        }

        [Test]
        public void RestartMenuAndTutorialKeepSuccessfulOrdinaryStreak()
        {
            var first = ShellWithDurableHistory();
            Assert.That(first.StartNewGame(), Is.True);
            first.ShowMenu(); first.BackToGame();
            Assert.That(first.Loop.DifficultySelection.History.ConsecutiveStarts, Is.EqualTo(1));
            var second = ShellWithDurableHistory();
            Assert.That(second.StartNewGame(), Is.True);
            Assert.That(second.Loop.SessionDifficulty.Id, Is.EqualTo("easy"));
            second.StartTutorial(); second.StartTutorial();
            Assert.That(new DifficultyHistoryPreference().Load().ConsecutiveStarts, Is.EqualTo(2));
            var third = ShellWithDurableHistory();
            Assert.That(third.StartNewGame(), Is.True);
            Assert.That(third.Loop.SessionDifficulty.Id, Is.EqualTo("medium"));
            Assert.That(new DifficultyHistoryPreference().Load().ConsecutiveStarts, Is.EqualTo(1));
        }

        [Test]
        public void FreshManualTutorialDoesNotConsumeFirstPlayAcrossRestart()
        {
            var manual = FreshShell();
            Assert.That(PlayerPrefs.HasKey(FirstPlayPreference.Key), Is.True, "migration freezes before manual entry");
            manual.StartTutorial();
            Assert.That(FirstPlayPreference.HasPlayed(), Is.False);
            // Even legacy writers after initialization cannot change the frozen gate.
            TutorialPreference.MarkSeen();
            var firstPlay = FreshShell(); firstPlay.StartPlaying();
            Assert.That(firstPlay.Tutorial, Is.Not.Null);
            Assert.That(FirstPlayPreference.HasPlayed(), Is.True);
            Assert.That(TutorialPreference.HasSeen(), Is.True, "legacy rollback retains first-Play history");
            Assert.That(firstPlay.Loop.RequestedSeed, Is.Null);
            var nextPlay = FreshShell(); nextPlay.StartPlaying();
            Assert.That(nextPlay.Tutorial, Is.Null); Assert.That(nextPlay.Modal, Is.EqualTo("loading"));
        }

        [Test]
        public void LegacyReturningUserMigratesOnceAndAvoidsRepeatedFirstPlay()
        {
            TutorialPreference.MarkSeen();
            var returning = FreshShell();
            Assert.That(FirstPlayPreference.HasPlayed(), Is.True);
            returning.StartPlaying();
            Assert.That(returning.Tutorial, Is.Null); Assert.That(returning.Modal, Is.EqualTo("loading"));
            PlayerPrefs.DeleteKey(TutorialPreference.Key);
            Assert.That(FirstPlayPreference.HasPlayed(), Is.True);
        }

        [Test]
        public void OrdinaryDirectStartsAndHistoryDoNotConsumeFirstPlayGate()
        {
            var shell = FreshShell(); Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(FirstPlayPreference.HasPlayed(), Is.False);
            var session = shell.ActiveSession;
            shell.ShowMenu(); shell.StartPlaying();
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(shell.Tutorial, Is.Null);
            Assert.That(FirstPlayPreference.HasPlayed(), Is.False);
            var restarted = FreshShell(); restarted.StartPlaying();
            Assert.That(restarted.Tutorial, Is.Not.Null);
            Assert.That(FirstPlayPreference.HasPlayed(), Is.True);
        }

        private static GameShellView FreshShell() => new GameShellView(14, "en",
            loop: PlayTestFactory.Loop(), difficultyHistory: new MemoryDifficultyHistoryStore());
        private static GameShellView ShellWithDurableHistory() => PlayTestFactory.Shell(loop:
            PlayTestFactory.Loop(difficultySelection: new DifficultySelection(_ => 0, new DifficultyHistoryPreference())));
    }
}
