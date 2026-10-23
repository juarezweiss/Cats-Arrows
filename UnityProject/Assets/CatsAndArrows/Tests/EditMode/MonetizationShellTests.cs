using System;
using System.Collections;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class MonetizationShellTests
    {
        private GameObject host;
        private PanelSettings panelSettings;
        private RenderTexture panelTexture;
        private GameShellView mountedShell;
        private double now;
        private bool hadLanguage;
        private string previousLanguage;
        [SetUp] public void PreserveLanguage()
        {
            hadLanguage = PlayerPrefs.HasKey(LanguagePreference.Key);
            previousLanguage = PlayerPrefs.GetString(LanguagePreference.Key);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (panelSettings != null) UnityEngine.Object.DestroyImmediate(panelSettings);
            if (panelTexture != null) UnityEngine.Object.DestroyImmediate(panelTexture);
            if (hadLanguage) PlayerPrefs.SetString(LanguagePreference.Key, previousLanguage); else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            PlayerPrefs.Save();
        }

        private IEnumerator Mount(MonetizationCoordinator coordinator)
        {
            now = 0;
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize; panelSettings.scale = 1;
            panelTexture = new RenderTexture(390, 844, 0);
            panelSettings.targetTexture = panelTexture;
            host = new GameObject("issue30-idle-integration", typeof(UIDocument));
            host.hideFlags = HideFlags.HideAndDontSave;
            var document = host.GetComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.rootVisualElement.style.width = 390;
            document.rootVisualElement.style.height = 844;
            mountedShell = new GameShellView(14, "en", PlayTestFactory.Loop(), enableTutorial: false,
                idleClock: () => now, monetization: coordinator);
            document.rootVisualElement.Add(mountedShell);
            Assert.That(mountedShell.StartNewGame(), Is.True);
            yield return null;
            yield return null;
            mountedShell.TickHintAttention();
        }
        private Button MountedHint => mountedShell.Q<Button>("action-hint");
        private void Advance(double seconds) { now += seconds; mountedShell.TickHintAttention(); }
        private void AssertAttention(bool expected)
        {
            Assert.That(MountedHint.style.backgroundColor.value, Is.EqualTo(expected ? BoardSymbolElement.Ink : PortraitBoardView.Tile));
            Assert.That(MountedHint.style.color.value, Is.EqualTo(expected ? PortraitBoardView.Tile : BoardSymbolElement.Ink));
            Assert.That(MountedHint.style.transitionDuration.value?.FirstOrDefault().value ?? 0, Is.EqualTo(expected ? .2f : 0));
        }

        [UnityTest]
        public IEnumerator RewardWaitSuppressesAttentionAndCloseStartsFreshInterval()
        {
            foreach (var rewarded in new[] { false, true })
            {
                var ads = new Ads();
                using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
                yield return Mount(coordinator);
                Advance(6); AssertAttention(true);
                var oldHint = MountedHint;
                mountedShell.RequestHint(); AssertAttention(false);
                Assert.That(oldHint.style.backgroundColor.value, Is.EqualTo(PortraitBoardView.Tile));
                Assert.That(oldHint.style.transitionDuration.value[0].value, Is.Zero);
                Assert.That(MountedHint.enabledInHierarchy, Is.False);
                Advance(60); AssertAttention(false);
                ads.Close(rewarded); AssertAttention(false);
                if (!rewarded) yield return DismissAlert();
                Assert.That(MountedHint.enabledInHierarchy, Is.True);
                Assert.That(mountedShell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(rewarded ? 1 : 0));
                Advance(5.999); AssertAttention(false);
                Advance(.002); AssertAttention(true);
                TearDown(); host = null; panelSettings = null;
            }
        }

        [UnityTest]
        public IEnumerator BackgroundRewardWaitsForExplicitHintAndAttentionResumesAfterFullInterval()
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            yield return Mount(coordinator);
            Advance(6); AssertAttention(true);
            mountedShell.RequestHint(); AssertAttention(false);
            mountedShell.SetApplicationFocus(false); mountedShell.SuspendMonetizationDelivery();
            mountedShell.SetApplicationPaused(true); mountedShell.SuspendMonetizationDelivery();
            ads.Close(true); Advance(60); AssertAttention(false);
            Assert.That(coordinator.HasEarnedHint, Is.True);
            Assert.That(mountedShell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            mountedShell.SetApplicationFocus(true); mountedShell.RefreshMonetization();
            Advance(60); AssertAttention(false);
            mountedShell.SetApplicationPaused(false); mountedShell.RefreshMonetization(); AssertAttention(false);
            Advance(5.999); AssertAttention(false);
            Advance(.002); AssertAttention(true);
            Assert.That(mountedShell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            mountedShell.RequestHint(); AssertAttention(false);
            Assert.That(ads.HintShows, Is.EqualTo(1));
            Assert.That(coordinator.HasEarnedHint, Is.False);
            Assert.That(mountedShell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
        }

        private IEnumerator DismissAlert(bool touch = false)
        {
            yield return null; yield return null;
            var ok = mountedShell.Q<Button>("notification-ok");
            Assert.That(ok, Is.Not.Null);
            var position = ok.worldBound.center;
            var picked = mountedShell.panel.Pick(position);
            Assert.That(picked == ok || (picked != null && ok.Contains(picked)), Is.True, "OK receives input");
            if (touch)
            {
                var input = new Touch { fingerId = 0, position = position, phase = TouchPhase.Began };
                using (var down = PointerDownEvent.GetPooled(input, EventModifiers.None))
                { down.target = ok; ok.SendEvent(down); }
                input.phase = TouchPhase.Ended;
                using (var up = PointerUpEvent.GetPooled(input, EventModifiers.None))
                { up.target = ok; ok.SendEvent(up); }
            }
            else
            {
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                { down.target = ok; ok.SendEvent(down); }
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                { up.target = ok; ok.SendEvent(up); }
            }
            Assert.That(mountedShell.Modal, Is.Null);
            Assert.That(mountedShell.Q("notification-alert"), Is.Null, "dismissal removes the alert tree");
        }

        // Issue #83: message + OK only, localized, scoped to this screen/board, no persistent layout labels.
        [UnityTest]
        public IEnumerator AlertsFitPortraitsBlockInputAndDismissWithoutReplayingRetainedStatus()
        {
            foreach (var size in new[] { new Vector2(320, 568), new Vector2(390, 844), new Vector2(430, 932) })
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                // #66: unavailable infrastructure grants a free Hint. A presented
                // ad closed without reward still exercises the dismissible alert.
                var ads = new Ads { CloseWithoutRewardOnShow = true };
                using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
                yield return Mount(coordinator);
                var root = host.GetComponent<UIDocument>().rootVisualElement;
                root.style.width = size.x; root.style.height = size.y;
                mountedShell.SelectLanguage(locale); mountedShell.BackToGame();
                var session = mountedShell.ActiveSession; var marks = session.Marks.ToArray();
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                mountedShell.RequestHint();
                yield return null; yield return null;
                Assert.That(mountedShell.Modal, Is.EqualTo("notification"));
                Assert.That(mountedShell.Q<Label>("notification-message").text, Is.EqualTo(messages["monetization.status.RewardNotEarned"]));
                var card = mountedShell.Q("notification-alert"); var ok = mountedShell.Q<Button>("notification-ok");
                Assert.That(card.Query<Button>().ToList().Count, Is.EqualTo(1));
                Assert.That(ok.text, Is.EqualTo(messages["notification.ok"]));
                Assert.That(ok.worldBound.height, Is.GreaterThanOrEqualTo(48));
                Assert.That(root.worldBound.Contains(card.worldBound.min), Is.True);
                Assert.That(root.worldBound.Contains(card.worldBound.max), Is.True);
                Assert.That(card.worldBound.Contains(ok.worldBound.center), Is.True);
                Assert.That(mountedShell.focusController.focusedElement, Is.SameAs(ok));
                Assert.That(mountedShell.BoardView.InteractionEnabled, Is.False);
                Assert.That(mountedShell.Q("game-controls").enabledInHierarchy, Is.False);
                mountedShell.RequestHint(); mountedShell.ClearBoard();
                Assert.That(session.Marks, Is.EquivalentTo(marks));
                Assert.That(mountedShell.Q<Label>("completion-status").text, Is.Null.Or.Empty);
                yield return DismissAlert(touch: locale == "es");
                Assert.That(mountedShell.BoardView.InteractionEnabled, Is.True);
                Assert.That(mountedShell.focusController.focusedElement, Is.SameAs(MountedHint));
                mountedShell.ShowMenu(); mountedShell.BackToGame();
                coordinator.BindSession(session); mountedShell.TickMonetization();
                Assert.That(mountedShell.Modal, Is.Null);
                mountedShell.RequestHint();
                using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
                { escape.target = mountedShell.Q<Button>("notification-ok"); mountedShell.SendEvent(escape); }
                Assert.That(mountedShell.Modal, Is.Null);
                Assert.That(mountedShell.BoardView.InteractionEnabled, Is.True);
                mountedShell.RequestHint();
                root.Remove(mountedShell);
                Assert.That(mountedShell.Modal, Is.Null, "detach clears alerts");
                root.Add(mountedShell); yield return null;
                mountedShell.RequestHint(); // The same error on a new explicit attempt must still show.
                Assert.That(mountedShell.Modal, Is.EqualTo("notification"));
                Assert.That(mountedShell.StartNewGame(), Is.True);
                Assert.That(mountedShell.Modal, Is.Null);
                Assert.That(mountedShell.Q("notification-alert"), Is.Null);
                Assert.That(mountedShell.Q<Label>("completion-status").text, Is.Null.Or.Empty);
                mountedShell.ShowSettings();
                Assert.That(mountedShell.Q<Label>("monetization-settings-status"), Is.Null);
                Assert.That(mountedShell.Q("settings-list").Query<Label>().ToList().Any(label =>
                    label.text == messages["monetization.explanation"]), Is.False);
                TearDown(); host = null; panelSettings = null; panelTexture = null;
            }
        }

        [Test]
        public void NavigationRebindAndSuspensionDiscardAlertsAndLateNotifications()
        {
            var ads = new Ads { CloseWithoutRewardOnShow = true };
            var purchases = new Purchases { HoldPurchase = true };
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, purchases);
            var shell = new GameShellView(14, "en", PlayTestFactory.Loop(), tutorialSeen: () => true,
                markTutorialSeen: () => { }, monetization: coordinator);
            Assert.That(shell.StartNewGame(), Is.True);
            foreach (Action leave in new Action[] { shell.ShowMenu, shell.ShowSettings, shell.StartTutorial,
                () => shell.SetApplicationFocus(false), () => shell.SetApplicationPaused(true),
                () => shell.BoardView.Bind(shell.ActiveSession, shell.Locale) })
            {
                shell.RequestHint(); Assert.That(shell.Modal, Is.EqualTo("notification"));
                leave(); Assert.That(shell.Modal, Is.Null); Assert.That(shell.Q("notification-alert"), Is.Null);
                shell.SetApplicationFocus(true); shell.SetApplicationPaused(false);
                Assert.That(shell.StartNewGame(), Is.True);
            }
            shell.ShowSettings(); shell.PurchaseRemoveAds();
            shell.SetApplicationFocus(false); // In-flight completion after lifecycle invalidation must stay quiet.
            purchases.PurchaseCallback(new PurchaseResult(PurchaseOutcome.Pending));
            shell.SetApplicationFocus(true); shell.RefreshMonetization();
            Assert.That(shell.Modal, Is.Null);
            shell.BackToGame(); shell.ShowSettings();
            Assert.That(shell.Modal, Is.Null);
            Assert.That(shell.Q<Label>("monetization-settings-status"), Is.Null);
        }

        private sealed class Ads : IAdService
        {
            private Action<AdSignal> receive;
            public int HintShows, ProgressionShows;
            public bool CloseWithoutRewardOnShow;
            public AdAvailability Availability = AdAvailability.Ready;
            public bool IsPresenting => receive != null;
            public AdAvailability GetAvailability(AdPlacement placement) => Availability;
            public void Show(long requestId, AdPlacement placement, Action<AdSignal> callback)
            {
                if (placement == AdPlacement.Hint) HintShows++; else ProgressionShows++;
                receive = callback; callback(new AdSignal(AdSignalKind.Presented));
                if (CloseWithoutRewardOnShow) Close(false);
            }
            public void Cancel(long requestId) { }
            public void Close(bool rewarded)
            {
                var callback = receive; receive = null;
                if (rewarded) callback(new AdSignal(AdSignalKind.RewardEarned));
                callback(new AdSignal(AdSignalKind.Closed));
            }
        }
        private sealed class Purchases : IPurchaseService, IOfferCodeRedemptionService
        {
            public event Action<PurchaseResult> Updated { add { } remove { } }
            public bool Owned, OfferAvailable = true;
            public int Refreshes, PurchaseCalls, RestoreCalls, Redemptions;
            public Action<OfferCodeRedemptionResult> RedemptionCallback;
            public void RedeemOfferCode(Action<OfferCodeRedemptionResult> complete) { Redemptions++; RedemptionCallback = complete; }
            public bool HoldPurchase, HoldRestore;
            public Action<PurchaseResult> PurchaseCallback, RestoreCallback;
            public PurchaseOffer GetOffer() => new PurchaseOffer(OfferAvailable, "review-price", true);
            public bool VerifyCachedOwnership(string storeNamespace, string evidenceReference) => Owned;
            public void Refresh(Action<PurchaseResult> complete) { Refreshes++; complete(Result()); }
            public void Restore(long requestId, Action<PurchaseResult> complete)
            {
                RestoreCalls++;
                if (HoldRestore) RestoreCallback = complete; else complete(Result());
            }
            public void Purchase(long requestId, Action<PurchaseResult> complete)
            {
                PurchaseCalls++;
                if (HoldPurchase) PurchaseCallback = complete;
                else { Owned = true; complete(Result()); }
            }
            public void Finish(string transactionReference, Action<bool> complete) => complete(true);
            private PurchaseResult Result() => new PurchaseResult(Owned ? PurchaseOutcome.Owned : PurchaseOutcome.NotOwned,
                verified: true, completeInventory: true, pendingResolved: true, storeNamespace: "test",
                evidenceReference: Owned ? "verified" : "", transactionReference: Owned ? "purchase-1" : null);
        }

        private static GameShellView Started(MonetizationCoordinator coordinator, string locale = "en", string difficulty = "easy")
        {
            var shell = new GameShellView(14, locale, PlayTestFactory.Loop(difficultySelection: PlayTestFactory.Selection(difficulty)),
                enableTutorial: false, monetization: coordinator);
            Assert.That(shell.StartNewGame(), Is.True);
            return shell;
        }
        private static MemoryMonetizationStateStore StoreWithCount(long count)
        {
            var store = new MemoryMonetizationStateStore();
            Assert.That(store.TryCommit(new MonetizationState { Revision = 1, OrdinaryBoardsCompleted = count }, 0), Is.True);
            return store;
        }

        [TestCase("en", "Hint (ads)", "Hint")]
        [TestCase("pt-BR", "Dica (anúncios)", "Dica")]
        [TestCase("es", "Pista (anuncios)", "Pista")]
        public void MenuAndHintLabelsReflectVerifiedRemovalInEveryLocale(string locale, string unowned, string owned)
        {
            var ads = new Ads(); var purchases = new Purchases();
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, purchases);
            var shell = Started(coordinator, locale);
            Assert.That(shell.Q<Button>("action-hint").text, Is.EqualTo(unowned));
            shell.ShowSettings();
            Assert.That(shell.Q<Button>("settings-remove-ads").text, Does.Contain("review-price"));
            Assert.That(shell.Q<Button>("settings-restore"), Is.Not.Null);
            shell.PurchaseRemoveAds();
            Assert.That(coordinator.IsOwned, Is.True);
            Assert.That(shell.Q<Button>("settings-remove-ads").enabledSelf, Is.False);
            shell.BackToGame();
            Assert.That(shell.Q<Button>("action-hint").text, Is.EqualTo(owned));
            shell.RequestHint();
            Assert.That(shell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(ads.HintShows, Is.Zero);
            var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
            foreach (var key in messages.Keys.Where(key => key.StartsWith("monetization.status.") && !key.EndsWith("None")))
                Assert.That(messages[key], Is.Not.Empty, key);
        }

        private void CaptureOfferCodeEvidence(string name)
        {
            var directory = Environment.GetEnvironmentVariable("PAWS_OFFER_CODE_CAPTURE");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            System.IO.Directory.CreateDirectory(directory);
            var previous = RenderTexture.active;
            var image = new Texture2D(panelTexture.width, panelTexture.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = panelTexture;
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
        }

        // Issue #65: retain the Settings row system and accessible targets in every locale/portrait.
        [UnityTest]
        public IEnumerator OfferCodeRowFitsPortraitsAndForwardsOneIntentWithoutMutatingBoard()
        {
            foreach (var size in new[] { new Vector2(320, 568), new Vector2(390, 844), new Vector2(430, 932) })
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                var purchases = new Purchases { OfferAvailable = false };
                using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), new Ads(), purchases);
                yield return Mount(coordinator);
                var root = host.GetComponent<UIDocument>().rootVisualElement;
                root.style.width = size.x; root.style.height = size.y;
                var session = mountedShell.ActiveSession; var marks = session.Marks.ToArray();
                mountedShell.ShowSettings(); mountedShell.SelectLanguage(locale);
                yield return null; yield return null;
                var button = mountedShell.Q<Button>("settings-redeem-offer-code");
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                Assert.That(button.text, Is.EqualTo(messages["monetization.redeem"]));
                Assert.That(button.tooltip, Is.EqualTo(button.text));
                Assert.That(button.enabledInHierarchy, Is.True, "redemption does not require product pricing");
                Assert.That(button.worldBound.height, Is.GreaterThanOrEqualTo(56));
                var scroll = mountedShell.Q<ScrollView>("settings-scroll");
                scroll.ScrollTo(button);
                yield return null; yield return null;
                Assert.That(button.worldBound.xMin, Is.GreaterThanOrEqualTo(root.worldBound.xMin));
                Assert.That(button.worldBound.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax));
                if (size.x == 390) CaptureOfferCodeEvidence("settings-" + locale);
                var position = button.worldBound.center;
                var picked = mountedShell.panel.Pick(position);
                Assert.That(picked == button || (picked != null && button.Contains(picked)), Is.True, "reachable after scroll");
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                { down.target = button; button.SendEvent(down); }
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                { up.target = button; button.SendEvent(up); }
                mountedShell.RedeemOfferCode();
                Assert.That(purchases.Redemptions, Is.EqualTo(1));
                Assert.That(mountedShell.Q<Button>("settings-redeem-offer-code").enabledSelf, Is.False);
                Assert.That(mountedShell.Modal, Is.Null);
                purchases.RedemptionCallback(OfferCodeRedemptionResult.Closed);
                Assert.That(mountedShell.Modal, Is.Null, "closure/cancellation alone is not a success or error alert");
                Assert.That(coordinator.IsOwned, Is.False);
                Assert.That(coordinator.IsBusy, Is.False);
                Assert.That(session.Marks, Is.EquivalentTo(marks));
                mountedShell.BackToGame(); Assert.That(mountedShell.BoardView.InteractionEnabled, Is.True);
                TearDown(); host = null; panelSettings = null; panelTexture = null;
            }
        }

        [UnityTest]
        public IEnumerator UnsupportedAndFailedOfferSheetsUseLocalizedDismissibleAlerts()
        {
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            foreach (var result in new[] { OfferCodeRedemptionResult.Unsupported, OfferCodeRedemptionResult.Unavailable })
            {
                var purchases = new Purchases();
                using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), new Ads(), purchases);
                yield return Mount(coordinator);
                mountedShell.ShowSettings(); mountedShell.SelectLanguage(locale);
                mountedShell.RedeemOfferCode();
                purchases.RedemptionCallback(result);
                var key = result == OfferCodeRedemptionResult.Unsupported ? "OfferCodeUnsupported" : "OfferCodeUnavailable";
                Assert.That(mountedShell.Q<Label>("notification-message").text,
                    Is.EqualTo(GameLocalization.CreateCatalog().GetResource(locale).Messages["monetization.status." + key]));
                yield return null; yield return null;
                CaptureOfferCodeEvidence(key + "-" + locale);
                yield return DismissAlert();
                Assert.That(mountedShell.focusController.focusedElement,
                    Is.SameAs(mountedShell.Q<Button>("settings-redeem-offer-code")));
                mountedShell.BackToGame(); Assert.That(mountedShell.BoardView.InteractionEnabled, Is.True);
                TearDown(); host = null; panelSettings = null; panelTexture = null;
            }
        }

        // Issue #76: UI only forwards settings intents; coordinator owns pending/verification state.
        [UnityTest]
        public IEnumerator SettingsPurchasesKeepBusyPendingAndRestoreStateAcrossLocaleChanges()
        {
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                var purchases = new Purchases { HoldPurchase = true, HoldRestore = true };
                using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), new Ads(), purchases);
                yield return Mount(coordinator);
                var session = mountedShell.ActiveSession; var marks = session.Marks.ToArray();
                mountedShell.ShowMenu();
                mountedShell.PurchaseRemoveAds(); mountedShell.RestorePurchases();
                Assert.That(purchases.PurchaseCalls, Is.Zero); Assert.That(purchases.RestoreCalls, Is.Zero);
                mountedShell.ShowSettings(); mountedShell.SelectLanguage(locale);
                yield return null; yield return null;
                var purchase = mountedShell.Q<Button>("settings-remove-ads");
                Assert.That(purchase.text, Does.Contain("review-price"));
                var position = purchase.worldBound.center;
                Assert.That(mountedShell.panel.visualTree.layout.Contains(position), Is.True, "inside panel pointer area position=" + position + " panel=" + mountedShell.panel.visualTree.layout);
                var picked = mountedShell.panel.Pick(position);
                Assert.That(picked == purchase || (picked != null && purchase.Contains(picked)), Is.True);
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                { down.target = purchase; purchase.SendEvent(down); }
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                { up.target = purchase; purchase.SendEvent(up); }
                Assert.That(purchases.PurchaseCalls, Is.EqualTo(1));
                Assert.That(coordinator.IsBusy, Is.True);
                Assert.That(mountedShell.Q("game-menu").enabledInHierarchy, Is.False);
                mountedShell.ShowMenu(); mountedShell.StartTutorial(); mountedShell.SelectLanguage("en");
                mountedShell.PurchaseRemoveAds(); mountedShell.RestorePurchases();
                Assert.That(mountedShell.Screen, Is.EqualTo("settings"));
                Assert.That(purchases.PurchaseCalls, Is.EqualTo(1)); Assert.That(purchases.RestoreCalls, Is.Zero);
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                Assert.That(mountedShell.Q<Label>("monetization-settings-status"), Is.Null);
                Assert.That(mountedShell.Modal, Is.Null, "native operation retains its existing busy lock");
                purchases.PurchaseCallback(new PurchaseResult(PurchaseOutcome.Pending, transactionReference: "pending-76"));
                Assert.That(coordinator.IsBusy, Is.False);
                Assert.That(mountedShell.Q("game-menu").enabledInHierarchy, Is.False, "alert blocks underlying settings");
                Assert.That(mountedShell.Q<Button>("settings-remove-ads").enabledSelf, Is.False);
                Assert.That(mountedShell.Q<Button>("settings-restore").enabledSelf, Is.True);
                Assert.That(mountedShell.Q<Label>("notification-message").text, Is.EqualTo(messages["monetization.status.PurchasePending"]));
                yield return DismissAlert();
                mountedShell.ShowLanguageSelector(); mountedShell.SelectLanguage(locale == "en" ? "es" : "en");
                var changedMessages = GameLocalization.CreateCatalog().GetResource(mountedShell.Locale).Messages;
                Assert.That(mountedShell.Q<Label>("monetization-settings-status"), Is.Null);
                Assert.That(mountedShell.Modal, Is.Null, "locale changes do not replay dismissed notifications");
                mountedShell.RestorePurchases();
                Assert.That(purchases.RestoreCalls, Is.EqualTo(1));
                Assert.That(coordinator.IsBusy, Is.True);
                Assert.That(mountedShell.Screen, Is.EqualTo("settings"));
                Assert.That(mountedShell.Q<Label>("monetization-settings-status"), Is.Null);
                purchases.RestoreCallback(new PurchaseResult(PurchaseOutcome.NotOwned, verified: true,
                    completeInventory: true, pendingResolved: true, storeNamespace: "test"));
                Assert.That(coordinator.IsBusy, Is.False);
                Assert.That(mountedShell.Screen, Is.EqualTo("settings"));
                Assert.That(mountedShell.Q<Button>("settings-remove-ads").enabledSelf, Is.True);
                Assert.That(mountedShell.ActiveSession, Is.SameAs(session));
                Assert.That(session.Marks, Is.EquivalentTo(marks));
                mountedShell.BackToGame(); Assert.That(mountedShell.BoardView.InteractionEnabled, Is.True);
                TearDown(); host = null; panelSettings = null;
            }
        }

        [Test]
        public void RewardWaitLocksAllShellIntentsAndCloseWithoutRewardRestoresPlay()
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            var shell = Started(coordinator);
            var session = shell.ActiveSession; var before = session.Marks.ToArray();
            shell.RequestHint();
            Assert.That(ads.HintShows, Is.EqualTo(1));
            Assert.That(shell.BoardView.InteractionEnabled, Is.False);
            Assert.That(shell.Q<Button>("action-hint").enabledSelf, Is.False);
            Assert.That(shell.Q<Button>("action-clear").enabledSelf, Is.False);
            shell.RequestHint(); shell.ClearBoard(); shell.ShowMenu(); shell.StartTutorial();
            Assert.That(shell.StartNewGame(), Is.False);
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(shell.Screen, Is.EqualTo("play"));
            ads.Close(false);
            Assert.That(shell.BoardView.InteractionEnabled, Is.False, "alert owns input until dismissed");
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(shell.Modal, Is.EqualTo("notification"));
            Assert.That(shell.Q<Label>("notification-message").text, Does.Contain("No reward"));
            Assert.That(shell.Q<Label>("completion-status").text, Is.Null.Or.Empty);
        }

        [Test]
        public void RewardAppliesOneFactThenClearAndFreshBoardWorkNormally()
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            var shell = Started(coordinator); var session = shell.ActiveSession;
            shell.RequestHint(); ads.Close(true);
            Assert.That(session.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            Assert.That(shell.Modal, Is.Null);
            shell.ClearBoard();
            Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.ActiveSession, Is.Not.SameAs(session));
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
        }

        [Test]
        public void SuspendedDeliveryRetainsEarnedHintUntilExplicitRequest()
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            var shell = Started(coordinator);
            shell.RequestHint(); shell.SuspendMonetizationDelivery(); ads.Close(true);
            Assert.That(shell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(coordinator.HasEarnedHint, Is.True);
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            shell.RequestHint();
            Assert.That(ads.HintShows, Is.EqualTo(1));
            Assert.That(shell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(coordinator.HasEarnedHint, Is.False);
        }

        [TestCase("easy")]
        [TestCase("medium")]
        [TestCase("hard")]
        public void FinalRewardOnFourthWinWaitsForExplicitFreshBoardBeforeProgression(string difficulty)
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, StoreWithCount(3), ads, new Purchases());
            var shell = Started(coordinator, difficulty: difficulty);
            var cellCount = shell.ActiveSession.Puzzle.Size * shell.ActiveSession.Puzzle.Size;
            for (var i = 0; i < cellCount && !shell.ActiveSession.IsCompleted; i++) { shell.RequestHint(); ads.Close(true); }
            var completed = shell.ActiveSession;
            Assert.That(completed.IsCompleted, Is.True);
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(4));
            Assert.That(shell.BoardView.InteractionEnabled, Is.False);
            Assert.That(shell.Q<Button>("action-hint").enabledSelf, Is.False);
            Assert.That(shell.Q<Button>("action-clear").enabledSelf, Is.False);
            Assert.That(ads.ProgressionShows, Is.Zero);
            shell.ShowMenu(); shell.BackToGame();
            Assert.That(ads.ProgressionShows, Is.Zero);
            Assert.That(shell.StartNewGame(), Is.False);
            Assert.That(ads.ProgressionShows, Is.EqualTo(1));
            Assert.That(shell.ActiveSession, Is.SameAs(completed));
            Assert.That(shell.StartNewGame(), Is.False);
            ads.Close(false);
            Assert.That(shell.ActiveSession, Is.Not.SameAs(completed));
            Assert.That(shell.NewGameRequestId, Is.EqualTo(2));
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(4));
        }

        [TestCase(AdAvailability.Offline)]
        [TestCase(AdAvailability.Unavailable)]
        [TestCase(AdAvailability.Restricted)]
        public void TutorialWinIsExcludedAndUnavailableOrdinaryHintIsDeliveredFree(AdAvailability availability)
        {
            var ads = new Ads { Availability = availability };
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            var shell = new GameShellView(14, "en", PlayTestFactory.Loop(), tutorialSeen: () => true,
                markTutorialSeen: () => { }, monetization: coordinator);
            shell.StartTutorial();
            foreach (var paw in shell.ActiveSession.Puzzle.SolutionPaws) { shell.ActiveSession.Tap(paw); shell.ActiveSession.Tap(paw); }
            var tutorialMarks = shell.ActiveSession.Marks.ToArray();
            coordinator.RecordCompletion(); shell.RequestHint();
            Assert.That(shell.ActiveSession.Marks, Is.EquivalentTo(tutorialMarks));
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
            Assert.That(coordinator.Snapshot.NextBoardInstance, Is.EqualTo(1));
            Assert.That(ads.HintShows, Is.Zero);
            Assert.That(shell.StartNewGame(), Is.True);
            shell.RequestHint();
            Assert.That(shell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            Assert.That(shell.Q<Button>("action-hint").enabledSelf, Is.True);
            Assert.That(shell.Modal, Is.Null);
            Assert.That(coordinator.IsBusy, Is.False);
            Assert.That(coordinator.HasEarnedHint, Is.False);
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
            Assert.That(ads.HintShows, Is.Zero);
            Assert.That(shell.Q<Label>("completion-status").text, Is.Null.Or.Empty);
        }

        [Test]
        public void RestoreRemainsVisibleWhenOfferUnavailableAndDisabledModeKeepsFreeHints()
        {
            var ads = new Ads(); var purchases = new Purchases { OfferAvailable = false };
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, purchases);
            var shell = new GameShellView(14, "en", monetization: coordinator);
            shell.ShowSettings();
            Assert.That(shell.Q<Button>("settings-remove-ads").enabledSelf, Is.False);
            Assert.That(shell.Q<Button>("settings-restore").enabledSelf, Is.True);
            var unusedPurchases = new Purchases();
            using var disabled = new MonetizationCoordinator(false, new MemoryMonetizationStateStore(), ads, unusedPurchases);
            var freeShell = Started(disabled);
            Assert.That(unusedPurchases.Refreshes, Is.Zero);
            Assert.That(freeShell.Q<Button>("action-hint").text, Is.EqualTo("Hint"));
            freeShell.RequestHint();
            Assert.That(freeShell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            freeShell.ShowMenu(); Assert.That(freeShell.Q<Button>("settings-remove-ads"), Is.Null);
            freeShell.ShowSettings();
            Assert.That(freeShell.Q<Button>("settings-remove-ads").enabledSelf, Is.False);
            Assert.That(freeShell.Q<Button>("settings-restore").enabledSelf, Is.False);
            Assert.That(freeShell.Q<Button>("settings-redeem-offer-code").enabledSelf, Is.False);
            Assert.That(unusedPurchases.Redemptions, Is.Zero);
            Assert.That(freeShell.Q<Label>("monetization-settings-status"), Is.Null);
            Assert.That(freeShell.Modal, Is.Null);
        }
    }
}
