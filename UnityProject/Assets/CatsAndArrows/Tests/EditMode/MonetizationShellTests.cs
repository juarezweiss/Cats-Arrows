using System;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class MonetizationShellTests
    {
        private sealed class Ads : IAdService
        {
            private Action<AdSignal> receive;
            public int HintShows, ProgressionShows;
            public AdAvailability Availability = AdAvailability.Ready;
            public bool IsPresenting => receive != null;
            public AdAvailability GetAvailability(AdPlacement placement) => Availability;
            public void Show(long requestId, AdPlacement placement, Action<AdSignal> callback)
            {
                if (placement == AdPlacement.Hint) HintShows++; else ProgressionShows++;
                receive = callback; callback(new AdSignal(AdSignalKind.Presented));
            }
            public void Cancel(long requestId) { }
            public void Close(bool rewarded)
            {
                var callback = receive; receive = null;
                if (rewarded) callback(new AdSignal(AdSignalKind.RewardEarned));
                callback(new AdSignal(AdSignalKind.Closed));
            }
        }
        private sealed class Purchases : IPurchaseService
        {
            public event Action<PurchaseResult> Updated { add { } remove { } }
            public bool Owned, OfferAvailable = true;
            public int Refreshes;
            public PurchaseOffer GetOffer() => new PurchaseOffer(OfferAvailable, "review-price", true);
            public bool VerifyCachedOwnership(string storeNamespace, string evidenceReference) => Owned;
            public void Refresh(Action<PurchaseResult> complete) { Refreshes++; complete(Result()); }
            public void Restore(long requestId, Action<PurchaseResult> complete) => complete(Result());
            public void Purchase(long requestId, Action<PurchaseResult> complete) { Owned = true; complete(Result()); }
            public void Finish(string transactionReference, Action<bool> complete) => complete(true);
            private PurchaseResult Result() => new PurchaseResult(Owned ? PurchaseOutcome.Owned : PurchaseOutcome.NotOwned,
                verified: true, completeInventory: true, pendingResolved: true, storeNamespace: "test",
                evidenceReference: Owned ? "verified" : "", transactionReference: Owned ? "purchase-1" : null);
        }

        private static GameShellView Started(MonetizationCoordinator coordinator, string locale = "en")
        {
            var shell = new GameShellView(14, locale, enableTutorial: false, monetization: coordinator);
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
            shell.ShowMenu();
            Assert.That(shell.Q<Button>("menu-remove-ads").text, Does.Contain("review-price"));
            Assert.That(shell.Q<Button>("menu-restore"), Is.Not.Null);
            shell.PurchaseRemoveAds();
            Assert.That(coordinator.IsOwned, Is.True);
            Assert.That(shell.Q<Button>("menu-remove-ads").enabledSelf, Is.False);
            shell.BackToGame();
            Assert.That(shell.Q<Button>("action-hint").text, Is.EqualTo(owned));
            shell.RequestHint();
            Assert.That(shell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(ads.HintShows, Is.Zero);
            var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
            foreach (var key in messages.Keys.Where(key => key.StartsWith("monetization.status.") && !key.EndsWith("None")))
                Assert.That(messages[key], Is.Not.Empty, key);
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
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(shell.Q<Label>("completion-status").text, Is.Not.Empty);
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

        [Test]
        public void FinalRewardOnFourthWinWaitsForExplicitFreshBoardBeforeProgression()
        {
            var ads = new Ads();
            using var coordinator = new MonetizationCoordinator(true, StoreWithCount(3), ads, new Purchases());
            var shell = Started(coordinator);
            for (var i = 0; i < 30 && !shell.ActiveSession.IsCompleted; i++) { shell.RequestHint(); ads.Close(true); }
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

        [Test]
        public void TutorialAndUnavailableAdsDoNotGrantUnpaidHintsOrCountTutorialWin()
        {
            var ads = new Ads { Availability = AdAvailability.Offline };
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, new Purchases());
            var shell = new GameShellView(14, "en", tutorialSeen: () => true, markTutorialSeen: () => { }, monetization: coordinator);
            shell.StartTutorial();
            foreach (var cat in shell.ActiveSession.Puzzle.SolutionCats) { shell.ActiveSession.Tap(cat); shell.ActiveSession.Tap(cat); }
            coordinator.RecordCompletion(); shell.RequestHint();
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
            Assert.That(coordinator.Snapshot.NextBoardInstance, Is.EqualTo(1));
            Assert.That(ads.HintShows, Is.Zero);
            Assert.That(shell.StartNewGame(), Is.True);
            shell.RequestHint();
            Assert.That(shell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(shell.BoardView.InteractionEnabled, Is.True);
            Assert.That(shell.Q<Label>("completion-status").text, Does.Contain("unavailable"));
        }

        [Test]
        public void RestoreRemainsVisibleWhenOfferUnavailableAndDisabledModeKeepsFreeHints()
        {
            var ads = new Ads(); var purchases = new Purchases { OfferAvailable = false };
            using var coordinator = new MonetizationCoordinator(true, new MemoryMonetizationStateStore(), ads, purchases);
            var shell = new GameShellView(14, "en", monetization: coordinator);
            Assert.That(shell.Q<Button>("menu-remove-ads").enabledSelf, Is.False);
            Assert.That(shell.Q<Button>("menu-restore").enabledSelf, Is.True);
            var unusedPurchases = new Purchases();
            using var disabled = new MonetizationCoordinator(false, new MemoryMonetizationStateStore(), ads, unusedPurchases);
            var freeShell = Started(disabled);
            Assert.That(unusedPurchases.Refreshes, Is.Zero);
            Assert.That(freeShell.Q<Button>("action-hint").text, Is.EqualTo("Hint"));
            freeShell.RequestHint();
            Assert.That(freeShell.ActiveSession.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            freeShell.ShowMenu(); Assert.That(freeShell.Q<Button>("menu-remove-ads"), Is.Null);
        }
    }
}
