using System;
using System.Collections;
using CatsAndArrows.Advertising;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class AudienceShellTests
    {
        private sealed class Preference : IAudiencePreference
        {
            public AudienceBand Current { get; private set; }
            public event Action Changed;
            public void Select(AudienceBand band) { Current = band; Changed?.Invoke(); }
        }
        [UnityTest]
        public IEnumerator LocalizedPortraitSelectorIsScrollableNeutralAndRevisable()
        {
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            foreach (var size in new[] { new Vector2(320, 568), new Vector2(390, 844), new Vector2(430, 932) })
            {
                var host = new GameObject("audience selector", typeof(UIDocument));
                var panel = ScriptableObject.CreateInstance<PanelSettings>();
                var texture = new RenderTexture((int)size.x, (int)size.y, 0);
                panel.scaleMode = PanelScaleMode.ConstantPixelSize; panel.targetTexture = texture;
                GameShellView shell = null;
                try
                {
                    var doc = host.GetComponent<UIDocument>(); doc.panelSettings = panel;
                    doc.rootVisualElement.style.width = size.x; doc.rootVisualElement.style.height = size.y;
                    var preference = new Preference();
                    shell = new GameShellView(14, locale, PlayTestFactory.Loop(), enableTutorial: false,
                        tutorialSeen: () => true, difficultyHistory: new MemoryDifficultyHistoryStore(), audience: preference);
                    doc.rootVisualElement.Add(shell);
                    yield return null; yield return null;
                    shell.ApplySafeArea(new Rect(0, 0, size.x, size.y), size);
                    shell.ShowAudienceSelector(true);
                    yield return null; yield return null;
                    Assert.That(shell.Modal, Is.EqualTo("audience"));
                    Assert.That(preference.Current, Is.EqualTo(AudienceBand.Unknown));
                    Assert.That(shell.Q<ScrollView>("audience-scroll"), Is.Not.Null);
                    foreach (var band in new[] { AudienceBand.Under13, AudienceBand.Teen, AudienceBand.Adult, AudienceBand.PreferNotToAnswer })
                    {
                        var option = shell.Q<Button>("audience-" + band);
                        Assert.That(option, Is.Not.Null); Assert.That(option.resolvedStyle.minHeight.value, Is.GreaterThanOrEqualTo(48));
                        Assert.That(option.text, Does.Not.Contain("✓"));
                    }
                    shell.SelectAudience(AudienceBand.PreferNotToAnswer);
                    Assert.That(shell.Modal, Is.Null); Assert.That(shell.Screen, Is.EqualTo("menu"));
                    yield return null;
                    Assert.That(doc.rootVisualElement.focusController.focusedElement, Is.SameAs(shell.Q<Button>("menu-start")));
                    shell.ShowSettings(); shell.ShowAudienceSelector();
                    Assert.That(shell.Q<Button>("audience-PreferNotToAnswer").text, Does.Contain("✓"));
                    shell.SelectAudience(AudienceBand.Adult);
                    Assert.That(preference.Current, Is.EqualTo(AudienceBand.Adult));
                    Assert.That(shell.Screen, Is.EqualTo("settings"));
                }
                finally
                {
                    shell?.DisposeMonetization(); UnityEngine.Object.DestroyImmediate(host);
                    UnityEngine.Object.DestroyImmediate(panel); UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }
        [Test]
        public void LocalBandSurvivesRestartAndMalformedChoiceNeverAssumesAdult()
        {
            var had = PlayerPrefs.HasKey(LocalAudiencePreference.Key);
            var previous = PlayerPrefs.GetInt(LocalAudiencePreference.Key, 0);
            try
            {
                PlayerPrefs.DeleteKey(LocalAudiencePreference.Key);
                Assert.That(new LocalAudiencePreference().Current, Is.EqualTo(AudienceBand.Unknown));
                foreach (var band in new[] { AudienceBand.Under13, AudienceBand.Teen, AudienceBand.Adult, AudienceBand.PreferNotToAnswer })
                {
                    new LocalAudiencePreference().Select(band);
                    Assert.That(new LocalAudiencePreference().Current, Is.EqualTo(band));
                }
                PlayerPrefs.SetInt(LocalAudiencePreference.Key, 99);
                Assert.That(new LocalAudiencePreference().Current, Is.EqualTo(AudienceBand.Unknown));
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(LocalAudiencePreference.Key, previous); else PlayerPrefs.DeleteKey(LocalAudiencePreference.Key);
                PlayerPrefs.Save();
            }
        }
        [Test]
        public void AdultUnknownOwnershipRetainsExistingRestoreFirstAssistancePolicy()
        {
            var preference = new Preference(); preference.Select(AudienceBand.Adult);
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(),
                new UnavailableAdService(), new UnavailablePurchaseService(), audience: preference);
            var shell = new GameShellView(14, "en", PlayTestFactory.Loop(), enableTutorial: false, tutorialSeen: () => true,
                difficultyHistory: new MemoryDifficultyHistoryStore(), monetization: coordinator, audience: preference);
            try
            {
                Assert.That(shell.StartNewGame(), Is.True);
                shell.RequestHint();
                Assert.That(shell.ActiveSession.MarkRevision, Is.Zero);
                Assert.That(coordinator.Status, Is.EqualTo(MonetizationStatus.RestoreRequired));
                Assert.That(coordinator.Snapshot.Access, Is.EqualTo(AccessState.Unknown));
            }
            finally { shell.DisposeMonetization(); }
        }
        [TestCase(AudienceBand.Unknown)]
        [TestCase(AudienceBand.Under13)]
        [TestCase(AudienceBand.Teen)]
        [TestCase(AudienceBand.PreferNotToAnswer)]
        public void RestrictedAudienceKeepsFreeEligibleHintsAndPublicLinksRequireExplicitTap(AudienceBand band)
        {
            var preference = new Preference(); preference.Select(band);
            var constructions = 0;
            using var ads = new AudienceAdService(preference, () => { constructions++; throw new InvalidOperationException(); }, () => false, action => action());
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new UnavailablePurchaseService(), audience: preference);
            ads.Start();
            string opened = null;
            var shell = new GameShellView(14, "en", PlayTestFactory.Loop(), enableTutorial: false, tutorialSeen: () => true,
                difficultyHistory: new MemoryDifficultyHistoryStore(), monetization: coordinator, adPrivacy: ads, audience: preference,
                privacyPolicyUrl: "https://example.com/privacy", supportUrl: "https://example.com/support", openExternalUrl: url => opened = url);
            try
            {
                Assert.That(shell.StartNewGame(), Is.True);
                Assert.That(shell.Q<Button>("action-hint").text, Is.EqualTo("Hint"));
                var revision = shell.ActiveSession.MarkRevision;
                shell.RequestHint(); Assert.That(shell.ActiveSession.MarkRevision, Is.GreaterThan(revision));
                Assert.That(constructions, Is.Zero);
                Assert.That(coordinator.Snapshot.Access, Is.EqualTo(AccessState.Unknown));
                Assert.That(coordinator.IsOwned, Is.False);
                shell.ShowSettings();
                Assert.That(shell.Q<Button>("settings-remove-ads"), Is.Not.Null);
                Assert.That(shell.Q<Button>("settings-restore"), Is.Not.Null);
                Assert.That(shell.Q<Button>("settings-privacy-policy"), Is.Not.Null);
                Assert.That(shell.Q<Button>("settings-support"), Is.Not.Null);
                Assert.That(opened, Is.Null);
                shell.OpenPrivacyPolicy(); Assert.That(opened, Is.EqualTo("https://example.com/privacy"));
                shell.ShowAudienceSelector(); shell.OpenSupport();
                Assert.That(opened, Is.EqualTo("https://example.com/privacy"));
                shell.SelectAudience(AudienceBand.PreferNotToAnswer); shell.OpenSupport();
                Assert.That(opened, Is.EqualTo("https://example.com/support"));
            }
            finally { shell.DisposeMonetization(); }
        }
    }
}
