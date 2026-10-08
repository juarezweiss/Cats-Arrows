using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Core.Localization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class GenerationLocalizationTests
    {
        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void Issue4EveryGenerationStatusUsesItsCompleteLocalizedResource(string locale)
        {
            var catalog = DefaultLocalization.CreateCatalog();
            var text = new LocalizationService(catalog, CultureInfo.GetCultureInfo(locale));
            var presenter = new GenerationLocalizer(text);
            var keys = new Dictionary<GenerationStatus, string>
            {
                { GenerationStatus.Success, "generation.success" },
                { GenerationStatus.InvalidProfile, "generation.invalid-profile" },
                { GenerationStatus.UnsupportedProfile, "generation.unsupported-profile" },
                { GenerationStatus.Exhausted, "generation.exhausted" },
                { GenerationStatus.Cancelled, "generation.cancelled" }
            };

            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(GenerationStatus)), keys.Keys);
            foreach (var pair in keys)
            {
                Assert.That(catalog.GetResource(locale).Messages.ContainsKey(pair.Value), Is.True);
                var message = presenter.Format(pair.Key);
                Assert.That(message, Is.EqualTo(text.GetText(pair.Value)).And.Not.Empty);
                Assert.That(message, Does.Not.Contain("{"));
                if (locale != "en") Assert.That(message, Is.Not.EqualTo(catalog.GetResource("en").Messages[pair.Value]));
            }
        }

        [Test]
        public void Issue4AdditionalLocaleUsesEnglishGenerationFallbackWithoutDiagnosticText()
        {
            var english = DefaultLocalization.CreateCatalog().GetResource("en");
            var catalog = new LocalizationCatalog(new[]
            {
                english,
                new LocaleResource("fr", new Dictionary<string, string> { { "generation.success", "Votre puzzle est prêt." } })
            });
            var presenter = new GenerationLocalizer(new LocalizationService(catalog, CultureInfo.GetCultureInfo("fr-CA")));
            Assert.That(presenter.Format(GenerationStatus.Success), Is.EqualTo("Votre puzzle est prêt."));
            foreach (GenerationStatus status in Enum.GetValues(typeof(GenerationStatus)).Cast<GenerationStatus>())
            {
                if (status == GenerationStatus.Success) continue;
                var fallback = new GenerationLocalizer(new LocalizationService(catalog, CultureInfo.GetCultureInfo("en")));
                Assert.That(presenter.Format(status), Is.EqualTo(fallback.Format(status)));
            }
        }

        [Test]
        public void Issue4InvalidGenerationStatusDoesNotBecomeAPlayerMessage()
        {
            var presenter = new GenerationLocalizer(new LocalizationService(DefaultLocalization.CreateCatalog()));
            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Format((GenerationStatus)int.MaxValue));
            Assert.Throws<ArgumentNullException>(() => new GenerationLocalizer(null));
        }
    }
}
