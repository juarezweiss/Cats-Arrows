using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class LocalizationTests
    {
        [TestCase("en-US", "en")]
        [TestCase("pt-BR", "pt-BR")]
        [TestCase("pt-PT", "en")]
        [TestCase("pt", "en")]
        [TestCase("es-MX", "es")]
        [TestCase("es-ES", "es")]
        [TestCase("fr-FR", "en")]
        public void Issue3DeviceLocaleSelectsOnlyRegisteredExactOrParentLanguage(string device, string expected)
        {
            var service = new LocalizationService(DefaultLocalization.CreateCatalog(), CultureInfo.GetCultureInfo(device));
            Assert.That(service.SelectedLocale, Is.EqualTo(expected));
        }

        [Test]
        public void Issue3ExplicitSelectionCanBeResetToDeviceLanguage()
        {
            var service = new LocalizationService(DefaultLocalization.CreateCatalog(), CultureInfo.GetCultureInfo("es-MX"));
            service.SelectLanguage("pt-BR");
            Assert.That(service.GetText("control.hint"), Is.EqualTo("Dica"));
            service.UseDeviceLanguage(CultureInfo.GetCultureInfo("es-MX"));
            Assert.That(service.GetText("control.hint"), Is.EqualTo("Pista"));
            service.SelectLanguage(null);
            Assert.That(service.SelectedLocale, Is.EqualTo("en"));
        }

        [Test]
        public void Issue3DefaultDeviceDetectionReadsCurrentUiCulture()
        {
            var before = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
                var service = new LocalizationService(DefaultLocalization.CreateCatalog());
                Assert.That(service.SelectedLocale, Is.EqualTo("pt-BR"));
            }
            finally { CultureInfo.CurrentUICulture = before; }
        }

        [Test]
        public void Issue3EveryInitialTranslationHasTheEnglishKeysAndPlaceholderContract()
        {
            var catalog = DefaultLocalization.CreateCatalog();
            var english = catalog.GetResource("en").Messages;
            foreach (var locale in catalog.AvailableLocales)
            {
                var messages = catalog.GetResource(locale).Messages;
                CollectionAssert.AreEquivalent(english.Keys, messages.Keys, locale);
                foreach (var pair in english)
                {
                    Assert.That(messages[pair.Key], Is.Not.Empty, locale + ":" + pair.Key);
                    CollectionAssert.AreEquivalent(Placeholders(pair.Value), Placeholders(messages[pair.Key]), locale + ":" + pair.Key);
                }
            }
        }

        [Test]
        public void Issue3AdditionalLocaleDataAllowsEnglishFallbackAndTranslationReordering()
        {
            var source = new Dictionary<string, string> { { "greeting", "{name}: {count:N1}" }, { "fallback", "English" } };
            var english = new LocaleResource("en", source);
            source["fallback"] = "Changed after construction";
            var catalog = new LocalizationCatalog(new[]
            {
                english,
                new LocaleResource("fr", new Dictionary<string, string> { { "greeting", "{count:N1} pour {name}" } })
            });
            var service = new LocalizationService(catalog, CultureInfo.GetCultureInfo("fr-CA"));
            Assert.That(service.SelectedLocale, Is.EqualTo("fr"));
            Assert.That(service.GetText("fallback"), Is.EqualTo("English"));
            Assert.That(service.Format("greeting", new Dictionary<string, object> { { "name", "Ana" }, { "count", 12.5m } }), Is.EqualTo("12,5 pour Ana"));
            Assert.Throws<KeyNotFoundException>(() => service.GetText("unknown"));
            Assert.Throws<ArgumentException>(() => service.Format("greeting", new Dictionary<string, object>()));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)english.Messages).Add("mutation", "value"));
        }

        [Test]
        public void Issue3FormattingUsesSelectedLocaleAndPreservesAccentsWithoutChangingGlobalCulture()
        {
            var before = CultureInfo.CurrentCulture;
            var uiBefore = CultureInfo.CurrentUICulture;
            var service = new LocalizationService(DefaultLocalization.CreateCatalog(), CultureInfo.GetCultureInfo("en"));
            service.SelectLanguage("pt-BR");
            Assert.That(service.FormatNumber(1234.5m, "N1"), Is.EqualTo("1.234,5"));
            Assert.That(service.GetText("solver.stalled"), Does.Contain("dedução"));
            service.SelectLanguage("es");
            Assert.That(service.GetText("control.menu"), Is.EqualTo("Menú"));
            service.SelectLanguage("en");
            Assert.That(service.FormatNumber(1234.5m, "N1"), Is.EqualTo("1,234.5"));
            Assert.That(CultureInfo.CurrentCulture, Is.SameAs(before));
            Assert.That(CultureInfo.CurrentUICulture, Is.SameAs(uiBefore));
        }

        [Test]
        public void Issue3CatalogRequiresEnglishAndRejectsDuplicateLocales()
        {
            var resource = new LocaleResource("es", new Dictionary<string, string>());
            Assert.Throws<ArgumentException>(() => new LocalizationCatalog(new[] { resource }));
            var english = new LocaleResource("en", new Dictionary<string, string>());
            Assert.Throws<ArgumentException>(() => new LocalizationCatalog(new[] { english, english }));
        }

        [Test]
        public void Issue3EveryRuleAndResultCanBePresentedInEachLocaleFromStructuredData()
        {
            var row = new ConstraintReference(ConstraintKind.Row, 2);
            var arrow = new ConstraintReference(ConstraintKind.Arrow, -1, new BoardCoordinate(2, 3), Direction.Left);
            var linePremise = new DeductionPremise(row, 2, new[]
            {
                new CellAssignment(new BoardCoordinate(2, 0), CellKnowledge.Cat),
                new CellAssignment(new BoardCoordinate(2, 1), CellKnowledge.Unknown)
            });
            var arrowPremise = new DeductionPremise(arrow, 1, new[]
            {
                new CellAssignment(new BoardCoordinate(2, 1), CellKnowledge.Unknown)
            });
            var rules = new[] { DeductionRules.ZeroLine, DeductionRules.LineSatisfied,
                DeductionRules.FillRemainingLine, DeductionRules.ArrowSingleton, DeductionRules.ArrowLineExclusion };
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                var presenter = new DeductionLocalizer(new LocalizationService(DefaultLocalization.CreateCatalog(), CultureInfo.GetCultureInfo(locale)));
                foreach (var rule in rules)
                {
                    var step = new DeductionStep(0, rule, DeductionKind.ConstraintPropagation,
                        new[] { linePremise, arrowPremise }, Array.Empty<CellAssignment>());
                    var message = presenter.Format(step);
                    Assert.That(message, Does.Not.Contain("{"), locale + ":" + rule);
                    Assert.That(message, Does.Contain("3")); // Domain row 2 is displayed as row 3.
                    if (rule == DeductionRules.ArrowSingleton) Assert.That(message, Does.Contain("4"));
                    Assert.That(step.Sources[0].Index, Is.EqualTo(2));
                }
                foreach (HumanSolverStatus status in Enum.GetValues(typeof(HumanSolverStatus)))
                    Assert.That(presenter.Format(status), Is.Not.Empty);
                foreach (UniquenessStatus status in Enum.GetValues(typeof(UniquenessStatus)))
                    Assert.That(presenter.Format(status), Is.Not.Empty);
            }
        }

        [Test]
        public void Issue3ActualProofAndUniquenessRemainIdenticalAcrossInitialCultures()
        {
            var before = CultureInfo.CurrentCulture;
            var uiBefore = CultureInfo.CurrentUICulture;
            var clues = new PuzzleClues(2, 3, "issue-3-localization-test", new[] { 0, 2 },
                new[] { 1, 1 }, Array.Empty<ArrowClue>());
            HumanSolverResult baseline = null;
            UniquenessResult uniqueBaseline = null;
            try
            {
                foreach (var locale in new[] { "en", "pt-BR", "es" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                    var proof = new HumanDeductionSolver().Solve(clues);
                    var uniqueness = new UniquenessVerifier().Verify(clues);
                    Assert.That(proof.Status, Is.EqualTo(HumanSolverStatus.Solved));
                    Assert.That(uniqueness.Status, Is.EqualTo(UniquenessStatus.Unique));
                    if (baseline == null) { baseline = proof; uniqueBaseline = uniqueness; }
                    CollectionAssert.AreEqual(baseline.Assignments, proof.Assignments);
                    CollectionAssert.AreEqual(baseline.Steps.Select(s => s.RuleId), proof.Steps.Select(s => s.RuleId));
                    CollectionAssert.AreEqual(baseline.Steps.SelectMany(s => s.Conclusions), proof.Steps.SelectMany(s => s.Conclusions));
                    CollectionAssert.AreEqual(uniqueBaseline.Solutions[0], uniqueness.Solutions[0]);
                    Assert.That(uniqueness.NodesVisited, Is.EqualTo(uniqueBaseline.NodesVisited));
                    var presenter = new DeductionLocalizer(new LocalizationService(DefaultLocalization.CreateCatalog()));
                    foreach (var step in proof.Steps)
                        Assert.That(presenter.Format(step), Does.Not.Contain("{"));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = before;
                CultureInfo.CurrentUICulture = uiBefore;
            }
        }

        private static string[] Placeholders(string message) => Regex.Matches(message, @"\{[^{}]+\}")
            .Cast<Match>().Select(match => match.Value).ToArray();
    }
}
