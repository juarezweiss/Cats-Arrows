using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using CatsAndArrows.UI;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class BoardLocalizationTests
    {
        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void BoardCatalogHasCompleteMatchingKeysAndPreservesExistingCoreTranslations(string locale)
        {
            var catalog = BoardLocalization.CreateCatalog();
            var messages = catalog.GetResource(locale).Messages;
            var english = catalog.GetResource("en").Messages;
            Assert.That(catalog.AvailableLocales, Is.EquivalentTo(new[] { "en", "pt-BR", "es" }));
            Assert.That(messages.Keys, Is.EquivalentTo(english.Keys));
            foreach (var original in DefaultLocalization.CreateCatalog().GetResource(locale).Messages)
                Assert.That(messages[original.Key], Is.EqualTo(original.Value), original.Key);
            foreach (var message in messages.Where(pair => pair.Key.StartsWith("board.", StringComparison.Ordinal)))
            {
                Assert.That(message.Value, Is.Not.Empty, message.Key);
                Assert.That(Placeholders(message.Value), Is.EquivalentTo(Placeholders(english[message.Key])), message.Key);
            }
            foreach (var kind in Enum.GetValues(typeof(BoardCellKind)).Cast<BoardCellKind>())
                Assert.That(messages.ContainsKey("board." + kind.ToString().ToLowerInvariant()), Is.True, kind.ToString());
        }

        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void BoardDescriptionsFormatCoordinatesTotalsAndAllEightDirectionsWithoutLeftoverTokens(string locale)
        {
            var catalog = BoardLocalization.CreateCatalog();
            var text = new LocalizationService(catalog, CultureInfo.GetCultureInfo(locale));
            var values = new Dictionary<string, object>
            {
                { "row", 3 }, { "column", 4 }, { "index", 2 }, { "total", 1 },
                { "state", text.GetText("board.protected") }
            };
            var cell = text.Format("board.cell-description", values);
            Assert.That(cell, Does.Contain("3").And.Contain("4").And.Contain(values["state"].ToString()));
            Assert.That(cell, Does.Not.Contain("{"));
            foreach (var key in new[] { "board.row-total", "board.column-total" })
            {
                var clue = text.Format(key, values);
                Assert.That(clue, Does.Contain("2").And.Contain("1"));
                Assert.That(clue, Does.Not.Contain("{"));
            }
            foreach (var direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                var translated = text.GetText("direction." + direction.ToString().ToLowerInvariant());
                values["direction"] = translated;
                var arrow = text.Format("board.arrow-description", values);
                Assert.That(arrow, Does.Contain(translated));
                Assert.That(arrow, Does.Not.Contain("{"));
            }
        }

        [TestCase("fr-CA", "en")]
        [TestCase("pt-PT", "en")]
        [TestCase("es-MX", "es")]
        [TestCase("pt-BR", "pt-BR")]
        public void BoardCatalogUsesCoreLocaleResolutionAndEnglishFallback(string requested, string expected)
        {
            var catalog = BoardLocalization.CreateCatalog();
            var text = new LocalizationService(catalog, CultureInfo.GetCultureInfo(requested));
            Assert.That(text.SelectedLocale, Is.EqualTo(expected));
            Assert.That(text.GetText("board.intro"), Is.EqualTo(catalog.GetResource(expected).Messages["board.intro"]));
        }

        [Test]
        public void FuturePartialLocaleCanFallBackToEnglishBoardResources()
        {
            var english = BoardLocalization.CreateCatalog().GetResource("en");
            var future = new LocaleResource("fr", new Dictionary<string, string> { { "board.cat", "Chat" } });
            var text = new LocalizationService(new LocalizationCatalog(new[] { english, future }), CultureInfo.GetCultureInfo("fr-CA"));
            Assert.That(text.GetText("board.cat"), Is.EqualTo("Chat"));
            Assert.That(text.GetText("board.protected"), Is.EqualTo(english.Messages["board.protected"]));
            Assert.That(text.GetText("direction.downright"), Is.EqualTo(english.Messages["direction.downright"]));
        }

        private static string[] Placeholders(string message) =>
            Regex.Matches(message, @"\{([A-Za-z][A-Za-z0-9_]*)(:[^{}]+)?\}")
                .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
    }
}
