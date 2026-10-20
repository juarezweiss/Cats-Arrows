using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class GameShellTests
    {
        private bool hadPreference;
        private string previousPreference;

        [SetUp]
        public void PreserveOwnerLanguagePreference()
        {
            hadPreference = PlayerPrefs.HasKey(LanguagePreference.Key);
            previousPreference = PlayerPrefs.GetString(LanguagePreference.Key);
        }

        [TearDown]
        public void RestoreOwnerLanguagePreference()
        {
            if (hadPreference) PlayerPrefs.SetString(LanguagePreference.Key, previousPreference);
            else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            PlayerPrefs.Save();
        }

        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void ShellCatalogPreservesBoardResourcesAndFormatsHintTargets(string locale)
        {
            var catalog = GameLocalization.CreateCatalog();
            var messages = catalog.GetResource(locale).Messages;
            var english = catalog.GetResource("en").Messages;
            Assert.That(messages.Keys, Is.EquivalentTo(english.Keys));
            foreach (var original in BoardLocalization.CreateCatalog().GetResource(locale).Messages)
                Assert.That(messages[original.Key], Is.EqualTo(original.Value));
            foreach (var entry in messages.Where(pair => pair.Key.StartsWith("game.", StringComparison.Ordinal)))
            {
                Assert.That(entry.Value, Is.Not.Empty, entry.Key);
                Assert.That(Tokens(entry.Value), Is.EquivalentTo(Tokens(english[entry.Key])), entry.Key);
            }
            var text = new LocalizationService(catalog, CultureInfo.GetCultureInfo(locale));
            var target = text.Format("game.hint-target", new Dictionary<string, object>
                { { "row", 3 }, { "column", 4 }, { "state", text.GetText("board.paw") } });
            Assert.That(target, Does.Contain("3").And.Contain("4").And.Contain(text.GetText("board.paw")));
            Assert.That(target, Does.Not.Contain("{"));
        }

        [TestCase("fr-CA", "en")]
        [TestCase("es-MX", "es")]
        [TestCase("pt-BR", "pt-BR")]
        public void LanguagePreferenceResolvesAndPersistsSupportedLocale(string requested, string expected)
        {
            LanguagePreference.Save(requested);
            Assert.That(LanguagePreference.Load("en"), Is.EqualTo(expected));
            Assert.That(PlayerPrefs.GetString(LanguagePreference.Key), Is.EqualTo(expected));
        }

        [Test]
        public void HintOverlayPreventsCommandsUntilReturnAndClearKeepsSamePuzzle()
        {
            var view = PlayTestFactory.Shell(14, "en", useInlineHints: false);
            Assert.That(view.Screen, Is.EqualTo("menu"));
            Assert.That(view.Loop.Session, Is.Null);
            Assert.That(view.StartNewGame(), Is.True);
            var session = view.Loop.Session;
            view.RequestHint();
            Assert.That(view.Modal, Is.EqualTo("hint"));
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            var before = session.Marks.ToArray();
            view.ClearBoard();
            view.RequestHint();
            Assert.That(session.Marks, Is.EquivalentTo(before));
            view.BackToGame();
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.BoardView.InteractionEnabled, Is.True);
            view.ClearBoard();
            Assert.That(view.Loop.Session, Is.SameAs(session));
            Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
        }

        [Test]
        public void MenuAndLanguagePreserveLiveSessionAndReturnWithoutNewGeneration()
        {
            var view = PlayTestFactory.Shell(14, "en", useInlineHints: false);
            Assert.That(view.StartNewGame(), Is.True);
            var session = view.Loop.Session;
            session.Tap(session.Marks.Keys.First());
            var before = session.Marks.ToArray();
            view.ShowMenu();
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            view.SelectLanguage("pt-BR");
            Assert.That(view.Locale, Is.EqualTo("pt-BR"));
            Assert.That(view.Screen, Is.EqualTo("menu"));
            view.BackToGame();
            Assert.That(view.Screen, Is.EqualTo("play"));
            Assert.That(view.BoardView.InteractionEnabled, Is.True);
            Assert.That(view.Loop.Session, Is.SameAs(session));
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(view.NewGameRequestId, Is.EqualTo(1));
        }

        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void LastHintKeepsCompletedBoardVisibleAndExplanationIsOptional(string locale)
        {
            var view = PlayTestFactory.Shell(14, locale, useInlineHints: false);
            Assert.That(view.StartNewGame(), Is.True);
            for (var request = 0; request < 25 && !view.Loop.Session.IsCompleted; request++)
            {
                view.RequestHint();
                if (!view.Loop.Session.IsCompleted) view.BackToGame();
            }
            Assert.That(view.Loop.Session.IsCompleted, Is.True);
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.Screen, Is.EqualTo("play"));
            Assert.That(view.BoardView.Q<Label>("completion-status").text,
                Is.EqualTo(GameLocalization.CreateCatalog().GetResource(locale).Messages["game.victory"]));
            Assert.That(view.Q<Button>("completion-hint"), Is.Not.Null);
            view.ShowLastHint();
            Assert.That(view.Modal, Is.EqualTo("last-hint"));
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            var completed = view.Loop.Session.Marks.ToArray();
            view.BackToGame();
            Assert.That(view.Q<Button>("action-hint").enabledSelf, Is.False);
            Assert.That(view.Q<Button>("action-clear").enabledSelf, Is.False);
            view.ClearBoard();
            view.RequestHint();
            Assert.That(view.Loop.Session.Marks, Is.EquivalentTo(completed));
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.Loop.Session.IsCompleted, Is.False);
            Assert.That(view.Loop.Session.Puzzle.Seed, Is.EqualTo(15));
            Assert.That(view.Q<Button>("action-hint").enabledSelf, Is.True);
            Assert.That(view.BoardView.Q<Label>("completion-status").text, Is.Empty);
            Assert.That(view.Q<Button>("completion-hint").style.display.value, Is.EqualTo(DisplayStyle.None));
            view.ShowLastHint();
            Assert.That(view.Modal, Is.Null);
        }

        [Test]
        public void CompletedBoardSurvivesMenuAndLocaleChangeWithoutAutomaticOverlay()
        {
            var view = PlayTestFactory.Shell(14, "en", useInlineHints: false);
            view.StartNewGame();
            for (var request = 0; request < 30 && !view.Loop.Session.IsCompleted; request++)
            { view.RequestHint(); view.BackToGame(); }
            Assert.That(view.Loop.Session.IsCompleted, Is.True);
            var session = view.Loop.Session;
            view.ShowMenu(); view.SelectLanguage("pt-BR"); view.BackToGame();
            Assert.That(view.Loop.Session, Is.SameAs(session));
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            Assert.That(view.BoardView.Q<Label>("completion-status").text,
                Is.EqualTo(GameLocalization.CreateCatalog().GetResource("pt-BR").Messages["game.victory"]));
            view.ShowLastHint();
            Assert.That(view.Modal, Is.EqualTo("last-hint"));
            Assert.That(view.Q<Label>("last-hint-explanation").text, Does.Contain("Linha").Or.Contain("coluna"));
        }

        [Test]
        public void FailedGenerationShowsRetryAndRecoversWithoutLeavingOldSession()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var attempts = 0;
            var loop = PlayTestFactory.Loop(14, seed => ++attempts == 2 ? null
                : generator.Generate(DifficultyProfile.Easy, seed));
            var view = PlayTestFactory.Shell(14, "es", loop, useInlineHints: false);
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.StartNewGame(), Is.False);
            Assert.That(view.Screen, Is.EqualTo("failure"));
            Assert.That(view.Modal, Is.EqualTo("failure"));
            Assert.That(loop.Session, Is.Null);
            Assert.That(view.Q<Button>("modal-retry"), Is.Not.Null);
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.Screen, Is.EqualTo("play"));
            Assert.That(view.Modal, Is.Null);
            Assert.That(loop.Session.Puzzle.Seed, Is.EqualTo(16));
        }

        private static string[] Tokens(string message) => Regex.Matches(message, @"\{([A-Za-z][A-Za-z0-9_]*)\}")
            .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
    }
}
