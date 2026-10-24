using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CatsAndArrows.Monetization;
using NUnit.Framework;
using UnityEngine.Purchasing;

namespace CatsAndArrows.Purchasing.Apple.Tests
{
    // Issue #64. SDK models and provider events through the real adapter, no store or charge.
    public sealed class ApplePurchaseServiceTests
    {
        private const string ProductId = "test.paws.removeads";
        private FakeStore client;
        private FakeEvidence evidence;
        private FakeOfferCodeSheet sheet;
        private ApplePurchaseService service;
        private double now;
        private int creations;
        private Product product;
        [SetUp] public void Setup()
        {
            now = 0; creations = 0;
            product = CreateProduct();
            client = new FakeStore { Product = product };
            evidence = new FakeEvidence();
            sheet = new FakeOfferCodeSheet();
            service = CreateService();
        }
        private ApplePurchaseService CreateService() => new ApplePurchaseService(ProductId,
            () => { creations++; return client; }, evidence, () => now, offerCodeSheet: sheet);
        [TearDown] public void Teardown() => service.Dispose();
        private PurchaseResult Refresh()
        {
            PurchaseResult result = null; service.Refresh(value => result = value); return result;
        }
        private PendingOrder Paid(string id = "123") => new PendingOrder(new Cart(new CartItem(product)), new Info(id));
        private void Own(params string[] ids)
        {
            evidence.Proof = new AppleEvidence { state = 1, reference = "opaque-proof", durable = true,
                transaction = "123", unfinished = ids, complete = true };
            evidence.Cached = "opaque-proof";
        }
        private MonetizationCoordinator Coordinator(IMonetizationStateStore store = null) => new MonetizationCoordinator(true,
            store ?? new MemoryMonetizationStateStore(), new UnavailableAdService(), service, () => now);

        // Issue #65: sheet outcomes never establish ownership or fabricate a pending purchase.
        [TestCase(OfferCodeRedemptionResult.Closed)]
        [TestCase(OfferCodeRedemptionResult.Unsupported)]
        [TestCase(OfferCodeRedemptionResult.Unavailable)]
        public void RedemptionPresentationOutcomesDoNotGrantOrBlockPlay(OfferCodeRedemptionResult result)
        {
            using var coordinator = Coordinator();
            coordinator.RedeemOfferCode(); coordinator.RedeemOfferCode();
            Assert.That(sheet.Presentations, Is.EqualTo(1));
            Assert.That(coordinator.IsBusy, Is.False);
            Assert.That(coordinator.CanPurchase, Is.False);
            coordinator.RestorePurchases();
            Assert.That(client.RestoreComplete, Is.Null);
            now = 100; service.Tick(); coordinator.Tick();
            Assert.That(coordinator.IsBusy, Is.False, "waiting for system UI is not a gameplay lock");
            sheet.Close(result);
            Assert.That(coordinator.IsOwned, Is.False);
            Assert.That(coordinator.Snapshot.PendingPurchase, Is.False);
            Assert.That(coordinator.CanRedeemOfferCode, Is.True);
            Assert.That(client.Buys, Is.Zero); Assert.That(client.Confirms, Is.Zero);
            Assert.That(coordinator.Status, Is.EqualTo(result == OfferCodeRedemptionResult.Closed ? MonetizationStatus.None :
                result == OfferCodeRedemptionResult.Unsupported ? MonetizationStatus.OfferCodeUnsupported : MonetizationStatus.OfferCodeUnavailable));
        }

        [Test] public void DisabledRedemptionDoesNotPresentInitializeOrRead()
        {
            using var coordinator = new MonetizationCoordinator(false, new MemoryMonetizationStateStore(), new UnavailableAdService(), service);
            coordinator.RedeemOfferCode();
            Assert.That(sheet.Presentations, Is.Zero); Assert.That(creations, Is.Zero); Assert.That(evidence.Reads, Is.Zero);
        }

        [Test] public void FailedPresentationAndDuplicateCallbacksLeaveCachedOwnershipUntouched()
        {
            Own();
            using var coordinator = Coordinator();
            coordinator.RedeemOfferCode();
            var callback = sheet.Complete;
            sheet.Close(OfferCodeRedemptionResult.Unavailable);
            callback(OfferCodeRedemptionResult.Closed);
            Assert.That(coordinator.IsOwned, Is.True);
            Assert.That(coordinator.Status, Is.EqualTo(MonetizationStatus.OfferCodeUnavailable));
            Assert.That(client.Fetches, Is.EqualTo(1));
        }

        [Test] public void OldSheetCallbackCannotCloseNewRedemptionIntent()
        {
            using var coordinator = Coordinator();
            coordinator.RedeemOfferCode(); var old = sheet.Complete;
            sheet.Close(OfferCodeRedemptionResult.Closed);
            coordinator.RedeemOfferCode();
            old(OfferCodeRedemptionResult.Unavailable);
            Assert.That(coordinator.IsRedeemingOfferCode, Is.True);
            Assert.That(coordinator.Status, Is.EqualTo(MonetizationStatus.Busy));
            sheet.Close(OfferCodeRedemptionResult.Closed);
            Assert.That(coordinator.IsRedeemingOfferCode, Is.False);
        }

        [Test] public void SuccessfulRedemptionUsesVerifiedGrantAndDurableFinalizationOnly()
        {
            var store = new MemoryMonetizationStateStore();
            using var coordinator = Coordinator(store);
            coordinator.RedeemOfferCode();
            client.EmitPaid(Paid());
            Assert.That(coordinator.IsOwned, Is.False, "unverified system/store success never grants");
            Own("123");
            client.BeforeConfirm = _ => {
                Assert.That(store.TryLoad(out var saved), Is.True);
                Assert.That(saved.Access, Is.EqualTo(AccessState.Owned));
                Assert.That(saved.UnfinishedTransaction, Is.Not.Empty);
            };
            client.EmitPaid(Paid());
            Assert.That(coordinator.IsOwned, Is.True);
            Assert.That(client.Confirms, Is.EqualTo(1));
            sheet.Close(OfferCodeRedemptionResult.Closed);
            client.EmitPaid(Paid());
            Assert.That(client.Confirms, Is.EqualTo(1), "duplicate delivery cannot finish twice while confirmation is outstanding");
            Own();
            client.EmitConfirmed(new ConfirmedOrder(new Cart(new CartItem(product)), new Info("123")));
            Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Empty);
            Assert.That(coordinator.Snapshot.LastFinishedTransaction, Is.Not.Empty);
        }

        [Test] public void OfflineClosedSheetAndLateRedemptionReuseLiveStoreUpdates()
        {
            using var coordinator = Coordinator();
            coordinator.RedeemOfferCode();
            client.FetchFailure = true;
            sheet.Close(OfferCodeRedemptionResult.Closed);
            Assert.That(coordinator.IsOwned, Is.False); Assert.That(coordinator.IsBusy, Is.False);
            Assert.That(coordinator.Snapshot.PendingPurchase, Is.False);
            Own("123"); client.EmitPaid(Paid());
            Assert.That(coordinator.IsOwned, Is.True); Assert.That(client.Confirms, Is.EqualTo(1));
        }

        [Test] public void PreFirstLaunchRedemptionUsesFreshStateRefreshAndExistingJournal()
        {
            Own("123");
            client.Inventory = new Orders(Array.Empty<ConfirmedOrder>(), new[] { Paid() }, Array.Empty<DeferredOrder>());
            var store = new MemoryMonetizationStateStore();
            using var coordinator = Coordinator(store);
            Assert.That(coordinator.IsOwned, Is.True); Assert.That(sheet.Presentations, Is.Zero);
            Assert.That(client.Confirms, Is.EqualTo(1));
            Assert.That(store.TryLoad(out var saved), Is.True);
            Assert.That(saved.StoreNamespace, Is.EqualTo(ApplePurchaseService.StoreNamespace));
            Assert.That(saved.EvidenceReference, Is.EqualTo("opaque-proof"));
        }

        [Test] public void ExternalResumeRefreshPreservesOfflineCacheAndHonorsRevocation()
        {
            using var coordinator = Coordinator();
            Own(); coordinator.RefreshPurchases();
            Assert.That(coordinator.IsOwned, Is.True); Assert.That(sheet.Presentations, Is.Zero);
            client.FetchFailure = true; coordinator.RefreshPurchases();
            Assert.That(coordinator.IsOwned, Is.True, "offline errors do not revoke externally redeemed ownership");
            client.FetchFailure = false;
            evidence.Proof = new AppleEvidence { state = 2, complete = true };
            client.EmitRevoked(ProductId);
            Assert.That(coordinator.IsOwned, Is.False);
            Assert.That(coordinator.Snapshot.Access, Is.EqualTo(AccessState.Revoked));
        }

        [Test] public void ClosedSheetDoesNotEraseDeferredPurchaseReleaseBlocker()
        {
            using var coordinator = Coordinator();
            client.EmitDeferred(new DeferredOrder(new Cart(new CartItem(product)), new Info("deferred")));
            coordinator.RedeemOfferCode(); sheet.Close(OfferCodeRedemptionResult.Closed);
            Assert.That(coordinator.Snapshot.PendingPurchase, Is.True);
            Assert.That(coordinator.CanPurchase, Is.False); Assert.That(coordinator.IsOwned, Is.False);
        }

        [Test] public void DisposedRedemptionIgnoresLateSheetAndStoreCallbacks()
        {
            var coordinator = Coordinator(); coordinator.RedeemOfferCode();
            var callback = sheet.Complete;
            coordinator.Dispose(); service.Dispose();
            callback(OfferCodeRedemptionResult.Closed);
            Assert.That(sheet.Disposed, Is.True);
            Assert.That(client.Fetches, Is.EqualTo(1));
        }

        [Test] public void DisabledCompositionDoesNotInitializeStoreOrEvidence()
        {
            using (new MonetizationCoordinator(false, new MemoryMonetizationStateStore(), new UnavailableAdService(), service)) { }
            Assert.That(creations, Is.Zero); Assert.That(evidence.Reads, Is.Zero);
        }
        [TestCase(ApplePurchaseEnvironment.Production, false, false, false)]
        [TestCase(ApplePurchaseEnvironment.Production, false, true, true)]
        [TestCase(ApplePurchaseEnvironment.SandboxReview, false, false, false)]
        [TestCase(ApplePurchaseEnvironment.SandboxReview, true, true, true)]
        public void AskToBuySimulationAllowsOnlyExplicitDevelopmentReviewOrDisabled(ApplePurchaseEnvironment environment,
            bool requested, bool reviewBuild, bool developmentBuild) => Assert.DoesNotThrow(() =>
            ApplePurchaseService.ValidateAskToBuySimulation(environment, requested, reviewBuild, developmentBuild));

        [TestCase(ApplePurchaseEnvironment.Production, true, true)]
        [TestCase(ApplePurchaseEnvironment.SandboxReview, false, true)]
        [TestCase(ApplePurchaseEnvironment.SandboxReview, true, false)]
        [TestCase(ApplePurchaseEnvironment.SandboxReview, false, false)]
        public void AskToBuySimulationRejectsNonReviewOrNonDevelopmentOrProduction(ApplePurchaseEnvironment environment,
            bool reviewBuild, bool developmentBuild) => Assert.Throws<InvalidOperationException>(() =>
            ApplePurchaseService.ValidateAskToBuySimulation(environment, true, reviewBuild, developmentBuild));

        [Test] public void ProductionConstructorRejectsSimulationBeforeAnyStoreConnection() =>
            Assert.Throws<InvalidOperationException>(() => new ApplePurchaseService(ProductId, simulateAskToBuy: true));

        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase(" id")]
        public void MissingMappingIsRejected(string id) => Assert.Throws<ArgumentException>(() =>
            new ApplePurchaseService(id, () => client, evidence, () => now));

        [Test] public void ConnectFetchAndPriceUseOnlyStoreMetadata()
        {
            Assert.That(Refresh().Outcome, Is.EqualTo(PurchaseOutcome.NotOwned));
            Assert.That(client.MappedProduct, Is.EqualTo(ProductId));
            Assert.That(client.Fetches, Is.EqualTo(1));
            Assert.That(service.GetOffer().LocalizedPrice, Is.EqualTo("localized test price"));
            Assert.That(service.GetOffer().Available, Is.True);
            Assert.That(service.GetOffer().IsReviewOffer, Is.False);
        }
        [Test] public void MissingPriceNeverCreatesAnOffer()
        {
            client.Product = CreateProduct(price: ""); Refresh();
            Assert.That(service.GetOffer().Available, Is.False);
        }
        [Test] public void ProductFailureStillAllowsRestoreAndVerifiedOwnership()
        {
            client.ProductFailure = true; Refresh();
            Own();
            PurchaseResult result = null;
            service.Restore(1, value => result = value); client.RestoreComplete(true); client.EmitInventory();
            Assert.That(result.Outcome, Is.EqualTo(PurchaseOutcome.Owned));
            Assert.That(evidence.WasSynchronized, Is.True);
            Assert.That(service.GetOffer().Available, Is.False);
        }
        [Test] public void PaidUnverifiedOrderNeverGrantsOrFinishes()
        {
            using (var coordinator = Coordinator())
            {
                evidence.Proof = new AppleEvidence(); client.EmitPaid(Paid());
                Assert.That(coordinator.IsOwned, Is.False); Assert.That(client.Confirms, Is.Zero);
            }
        }
        [Test] public void PaidOrderGrantsDurablyBeforeConfirmation()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store))
            {
                client.BeforeConfirm = _ => {
                    Assert.That(store.TryLoad(out var state), Is.True);
                    Assert.That(state.Access, Is.EqualTo(AccessState.Owned));
                    Assert.That(state.UnfinishedTransaction, Is.Not.Empty);
                };
                Own("123"); client.EmitPaid(Paid());
                Assert.That(coordinator.IsOwned, Is.True);
                Assert.That(client.Confirms, Is.EqualTo(1));
                Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Not.EqualTo("123"));
                evidence.Proof.unfinished = Array.Empty<string>(); client.EmitConfirmed(new ConfirmedOrder(Paid().CartOrdered, Paid().Info));
                Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Empty);
            }
        }
        [Test] public void FailedStateCommitRetainsVerifiedMemoryAccessWithoutFinishing()
        {
            var store = new FaultStore();
            using (var coordinator = Coordinator(store))
            {
                store.Fail = true; Own("123"); client.EmitPaid(Paid());
                Assert.That(coordinator.IsOwned, Is.True);
                Assert.That(coordinator.Status, Is.EqualTo(MonetizationStatus.StorageRecovery));
                Assert.That(client.Confirms, Is.Zero);
            }
        }
        [Test] public void FailedPrivateCacheCommitNeverFinishes()
        {
            using (var coordinator = Coordinator())
            {
                Own("123"); evidence.Proof.durable = false; evidence.Cached = null; client.EmitPaid(Paid());
                Assert.That(coordinator.IsOwned, Is.True); Assert.That(client.Confirms, Is.Zero);
            }
        }
        [TestCase(PurchaseFailureReason.UserCancelled, PurchaseOutcome.Cancelled)]
        [TestCase(PurchaseFailureReason.PaymentDeclined, PurchaseOutcome.Failed)]
        [TestCase(PurchaseFailureReason.SignatureInvalid, PurchaseOutcome.Failed)]
        public void CancelAndFailureNeverGrant(PurchaseFailureReason reason, PurchaseOutcome outcome)
        {
            Refresh(); PurchaseResult result = null;
            service.Purchase(1, value => result = value);
            client.EmitFailure(new FailedOrder(Paid(), reason, "private details"));
            Assert.That(result.Outcome, Is.EqualTo(outcome)); Assert.That(result.Verified, Is.False);
            Assert.That(result.EvidenceReference, Is.Null); Assert.That(client.Confirms, Is.Zero);
        }
        [Test] public void DeferredPersistsAndAmbiguousRestartInventoryKeepsPurchaseDisabled()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store))
            {
                client.EmitDeferred(new DeferredOrder(Paid().CartOrdered, new Info("")));
                Assert.That(coordinator.Snapshot.PendingPurchase, Is.True);
                Assert.That(coordinator.IsOwned, Is.False);
                Assert.That(evidence.Deferred, Is.True);
            }
            service.Dispose(); service = CreateService();
            evidence.Proof = new AppleEvidence { deferred = true };
            using (var restarted = Coordinator(store))
            {
                Assert.That(restarted.Snapshot.PendingPurchase, Is.True); Assert.That(restarted.CanPurchase, Is.False);
                Own("123"); client.EmitPaid(Paid());
                Assert.That(restarted.IsOwned, Is.True); Assert.That(restarted.Snapshot.PendingPurchase, Is.False);
            }
        }
        [Test] public void AlreadyOwnedFailureRequiresVerifiedRefresh()
        {
            using (var coordinator = Coordinator())
            {
                coordinator.PurchaseRemoveAds(); Own();
                client.EmitFailure(new FailedOrder(Paid(), PurchaseFailureReason.DuplicateTransaction, "private"));
                Assert.That(coordinator.IsOwned, Is.True); Assert.That(client.Buys, Is.EqualTo(1));
            }
        }
        [Test] public void PrivateMarkerWriteFailureCannotEraseRecoveredCoordinatorPendingState()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store))
            {
                evidence.MarkerWritesSucceed = false;
                client.EmitDeferred(new DeferredOrder(Paid().CartOrdered, new Info("")));
                Assert.That(coordinator.Snapshot.PendingPurchase, Is.True);
                Assert.That(evidence.Deferred, Is.False);
            }
            Assert.That(store.TryLoad(out var recovered), Is.True);
            service.Dispose();
            service = new ApplePurchaseService(ProductId, () => client, evidence, () => now, recovered.PendingPurchase);
            evidence.Proof = new AppleEvidence { state = 3, complete = true, deferred = false };
            using (var restarted = Coordinator(store))
            {
                Assert.That(restarted.Snapshot.PendingPurchase, Is.True);
                Assert.That(restarted.CanPurchase, Is.False); Assert.That(restarted.IsOwned, Is.False);
            }
        }
        [Test] public void FinishFailureKeepsJournalAndRestartReplays()
        {
            var store = new MemoryMonetizationStateStore(); string reference;
            using (var coordinator = Coordinator(store))
            {
                Own("123"); client.EmitPaid(Paid());
                reference = coordinator.Snapshot.UnfinishedTransaction;
                client.EmitConfirmed(new FailedOrder(Paid(), PurchaseFailureReason.Unknown, "private"));
                Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.EqualTo(reference));
            }
            service.Dispose(); service = CreateService(); client.Inventory = new Orders(Array.Empty<ConfirmedOrder>(), new[] { Paid() }, Array.Empty<DeferredOrder>());
            using (var restarted = Coordinator(store))
            {
                Assert.That(restarted.IsOwned, Is.True);
                Assert.That(restarted.Snapshot.UnfinishedTransaction, Is.EqualTo(reference));
                Assert.That(client.Confirms, Is.EqualTo(2));
            }
        }
        [Test] public void ConfirmedCallbackDoesNotFinishIfNativeTransactionStillUnfinished()
        {
            using (var coordinator = Coordinator())
            {
                Own("123"); client.EmitPaid(Paid());
                client.EmitConfirmed(new ConfirmedOrder(Paid().CartOrdered, Paid().Info));
                Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Not.Empty);
            }
        }
        [Test] public void CrashAfterNativeFinishBeforeJournalClearRecoversIdempotently()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store))
            { Own("123"); client.EmitPaid(Paid()); Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Not.Empty); }
            service.Dispose(); service = CreateService();
            evidence.Proof.unfinished = Array.Empty<string>(); evidence.Proof.confirmed = new[] { "123" };
            using (var restarted = Coordinator(store))
            {
                Assert.That(restarted.IsOwned, Is.True); Assert.That(restarted.Snapshot.UnfinishedTransaction, Is.Empty);
                Assert.That(client.Confirms, Is.EqualTo(1)); // No second confirmation of an already finished order.
            }
        }
        // #67: an SDK-replayed/previously finished non-consumable must remain
        // recoverable from current entitlement even when no PendingOrder is delivered.
        // This models the callback boundary; it does not prove native SDK replay safety.
        [Test] public void FinishedPurchaseWithoutPaidCallbackRestoresAndSurvivesRestartWithoutRebuy()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store))
            {
                coordinator.RestorePurchases();
                Own(); evidence.Proof.confirmed = new[] { "123" };
                client.RestoreComplete(true); client.EmitInventory();
                Assert.That(coordinator.IsOwned, Is.True);
                Assert.That(coordinator.CanPurchase, Is.False);
                coordinator.PurchaseRemoveAds();
                Assert.That(client.Buys, Is.Zero);
                Assert.That(client.Confirms, Is.Zero, "already-finished entitlement needs no new finish");
                Assert.That(store.TryLoad(out var saved), Is.True);
                Assert.That(saved.Access, Is.EqualTo(AccessState.Owned));
            }
            service.Dispose(); service = CreateService(); client.FetchFailure = true;
            using (var restarted = Coordinator(store))
            {
                Assert.That(restarted.IsOwned, Is.True);
                restarted.PurchaseRemoveAds();
                Assert.That(client.Buys, Is.Zero);
            }
        }

        [Test] public void FinishedHistoryWithoutCurrentEntitlementCannotGrantThroughRestore()
        {
            using var coordinator = Coordinator();
            coordinator.RestorePurchases();
            evidence.Proof = new AppleEvidence { state = 3, complete = true, confirmed = new[] { "123" } };
            client.RestoreComplete(true); client.EmitInventory();
            Assert.That(coordinator.IsOwned, Is.False);
            Assert.That(client.Confirms, Is.Zero);
        }
        [Test] public void DuplicatePaidAndConfirmedCallbacksAreIdempotent()
        {
            using (var coordinator = Coordinator())
            {
                Own("123"); client.EmitPaid(Paid()); client.EmitPaid(Paid());
                Assert.That(client.Confirms, Is.EqualTo(1));
                evidence.Proof.unfinished = Array.Empty<string>();
                var confirmed = new ConfirmedOrder(Paid().CartOrdered, Paid().Info);
                client.EmitConfirmed(confirmed); client.EmitConfirmed(confirmed); client.EmitPaid(Paid());
                Assert.That(client.Confirms, Is.EqualTo(1)); Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Empty);
            }
        }
        [Test] public void MultipleUnfinishedOrdersNeverOverwriteTheDurableJournal()
        {
            using (var coordinator = Coordinator())
            {
                Own("123", "456"); client.Inventory = new Orders(Array.Empty<ConfirmedOrder>(), new[] { Paid("123"), Paid("456") }, Array.Empty<DeferredOrder>());
                coordinator.RefreshPurchases(); var first = client.LastConfirmed;
                Assert.That(client.Confirms, Is.EqualTo(1));
                evidence.Proof.unfinished = evidence.Proof.unfinished.Where(id => id != first.Info.TransactionID).ToArray();
                client.EmitConfirmed(new ConfirmedOrder(first.CartOrdered, first.Info));
                Assert.That(client.Confirms, Is.EqualTo(2)); Assert.That(client.LastConfirmed.Info.TransactionID, Is.Not.EqualTo(first.Info.TransactionID));
                var second = client.LastConfirmed; evidence.Proof.unfinished = Array.Empty<string>();
                client.EmitConfirmed(new ConfirmedOrder(second.CartOrdered, second.Info));
                Assert.That(coordinator.Snapshot.UnfinishedTransaction, Is.Empty);
            }
        }
        [Test] public void OfflineRestartPreservesOnlyVerifiedCache()
        {
            var store = new MemoryMonetizationStateStore();
            using (var coordinator = Coordinator(store)) { Own(); client.EmitInventory(); Assert.That(coordinator.IsOwned, Is.True); }
            service.Dispose(); service = CreateService(); client.FetchFailure = true;
            using (var restarted = Coordinator(store)) Assert.That(restarted.IsOwned, Is.True);
            Assert.That(service.VerifyCachedOwnership("other-store", "opaque-proof"), Is.False);
            Assert.That(service.VerifyCachedOwnership(ApplePurchaseService.StoreNamespace, "forged"), Is.False);
        }
        [Test] public void PartialOrEmptyUnverifiedInventoryCannotRevoke()
        {
            using (var coordinator = Coordinator())
            {
                Own(); client.EmitInventory(); evidence.Proof = new AppleEvidence();
                client.EmitInventory(); Assert.That(coordinator.IsOwned, Is.True);
            }
        }
        [Test] public void FreshInstallRestoresThroughSameVerifiedGrantPath()
        {
            using (var coordinator = Coordinator())
            {
                coordinator.RestorePurchases(); Own(); client.RestoreComplete(true); client.EmitInventory();
                Assert.That(coordinator.IsOwned, Is.True); Assert.That(evidence.WasSynchronized, Is.True);
            }
        }
        [TestCase(true)] [TestCase(false)] public void RestoreHandlesEitherCallbackOrder(bool inventoryFirst)
        {
            Refresh(); PurchaseResult result = null; Own();
            service.Restore(1, value => result = value);
            if (inventoryFirst) client.EmitInventory();
            Assert.That(result, Is.Null);
            client.RestoreComplete(true);
            if (!inventoryFirst) client.EmitInventory();
            Assert.That(result.Outcome, Is.EqualTo(PurchaseOutcome.Owned)); Assert.That(evidence.WasSynchronized, Is.True);
        }
        [Test] public void RestoreFailurePreservesCachedAccess()
        {
            using (var coordinator = Coordinator())
            {
                Own(); client.EmitInventory(); coordinator.RestorePurchases(); client.RestoreComplete(false);
                Assert.That(coordinator.IsOwned, Is.True);
            }
        }
        [Test] public void PaidUpdateAfterRestoreSuccessCannotSettleBeforeSynchronizedInventory()
        {
            using (var coordinator = Coordinator())
            {
                Own(); client.EmitInventory();
                Assert.That(coordinator.IsOwned, Is.True);
                PurchaseResult restored = null;
                service.Restore(1, value => restored = value);
                client.RestoreComplete(true);
                evidence.Proof = new AppleEvidence(); // Ambiguous ordinary refresh must preserve cached access.
                var readsBeforeUpdate = evidence.Reads;
                client.EmitPaid(Paid());
                Assert.That(restored, Is.Null, "A paid update must not complete the explicit Restore.");
                Assert.That(evidence.Reads, Is.EqualTo(readsBeforeUpdate));
                Assert.That(coordinator.IsOwned, Is.True);
                evidence.Cached = null;
                evidence.Proof = new AppleEvidence { state = 3, complete = true };
                client.EmitInventory();
                Assert.That(evidence.WasSynchronized, Is.True);
                Assert.That(restored.Outcome, Is.EqualTo(PurchaseOutcome.NotOwned));
                Assert.That(restored.Verified && restored.CompleteInventory, Is.True);
            }
        }
        [Test] public void PaidUpdateDuringSynchronizedReadSupersedesStaleAbsence()
        {
            Refresh(); evidence.Delay = true;
            PurchaseResult restored = null; var completions = 0;
            service.Restore(1, value => { restored = value; completions++; });
            client.RestoreComplete(true); client.EmitInventory();
            var stale = evidence.Pending;
            var reads = evidence.Reads;
            client.EmitPaid(Paid());
            Assert.That(evidence.Reads, Is.EqualTo(reads + 1));
            Assert.That(evidence.WasSynchronized, Is.True);
            var current = evidence.Pending;
            stale(new AppleEvidence { state = 3, complete = true });
            Assert.That(restored, Is.Null, "The older synchronized absence predates the paid update.");
            Own("123"); current(evidence.Proof);
            Assert.That(restored.Outcome, Is.EqualTo(PurchaseOutcome.Owned));
            Assert.That(restored.Verified, Is.True);
            Assert.That(completions, Is.EqualTo(1));
        }
        [Test] public void AccountChangeSynchronizedAbsenceRevokesWithoutCatchupDebt()
        {
            using (var coordinator = Coordinator())
            {
                Own(); client.EmitInventory(); coordinator.RestorePurchases();
                evidence.Cached = null; evidence.Proof = new AppleEvidence { state = 3, complete = true };
                client.RestoreComplete(true); client.EmitInventory();
                Assert.That(coordinator.IsOwned, Is.False);
                Assert.That(coordinator.Snapshot.HandledProgressionMilestone, Is.EqualTo(coordinator.Snapshot.OrdinaryBoardsCompleted / 4));
            }
        }
        [Test] public void RevocationInvalidatesCachedEvidenceAndIgnoresStaleProof()
        {
            using (var coordinator = Coordinator())
            {
                Own(); client.EmitInventory(); evidence.Delay = true; client.EmitPaid(Paid());
                var old = evidence.Pending; evidence.Proof = new AppleEvidence { state = 2, complete = true };
                evidence.Cached = null; client.EmitRevoked(ProductId); var revoke = evidence.Pending;
                revoke(evidence.Proof); old(new AppleEvidence { state = 1, reference = "opaque-proof", durable = true });
                Assert.That(coordinator.Snapshot.Access, Is.EqualTo(AccessState.Revoked));
                Assert.That(service.VerifyCachedOwnership(ApplePurchaseService.StoreNamespace, "opaque-proof"), Is.False);
                Assert.That(client.Confirms, Is.Zero);
            }
        }
        [Test] public void TimedOutPurchaseAcceptsLateVerifiedStoreUpdate()
        {
            using (var coordinator = Coordinator())
            {
                coordinator.PurchaseRemoveAds(); now = 31; coordinator.Tick(); service.Tick();
                Own("123"); client.EmitPaid(Paid()); Assert.That(coordinator.IsOwned, Is.True);
            }
        }
        [Test] public void FocusRefreshCannotSettleOrRepeatAnOutstandingNativeBuy()
        {
            Refresh(); PurchaseResult buy = null, refresh = null;
            service.Purchase(1, value => buy = value);
            service.Refresh(value => refresh = value);
            Assert.That(buy, Is.Null); Assert.That(refresh, Is.Null); Assert.That(client.Fetches, Is.EqualTo(1));
            Assert.That(service.GetOffer().Available, Is.False);
            client.EmitDeferred(new DeferredOrder(Paid().CartOrdered, new Info("")));
            Assert.That(buy.Outcome, Is.EqualTo(PurchaseOutcome.Pending));
            Assert.That(refresh.Outcome, Is.EqualTo(PurchaseOutcome.Pending)); Assert.That(client.Buys, Is.EqualTo(1));
        }
        [Test] public void NativeBuyTimeoutDoesNotEnableASecondSheet()
        {
            Refresh(); service.Purchase(1, _ => { }); now = 31; service.Tick();
            Assert.That(service.GetOffer().Available, Is.False);
            PurchaseResult second = null; service.Purchase(2, value => second = value);
            Assert.That(second.Outcome, Is.EqualTo(PurchaseOutcome.Unavailable)); Assert.That(client.Buys, Is.EqualTo(1));
            client.EmitFailure(new FailedOrder(Paid(), PurchaseFailureReason.UserCancelled, "private"));
            Assert.That(service.GetOffer().Available, Is.True);
        }
        [Test] public void VerificationFailureCanRetryAfterSynchronizedCompleteAbsence()
        {
            Refresh(); service.Purchase(1, _ => { });
            client.EmitFailure(new FailedOrder(Paid(), PurchaseFailureReason.SignatureInvalid, "private"));
            Assert.That(service.GetOffer().Available, Is.False);
            service.Restore(2, _ => { }); client.RestoreComplete(true); client.EmitInventory();
            Assert.That(service.GetOffer().Available, Is.True);
            service.Purchase(3, _ => { }); Assert.That(client.Buys, Is.EqualTo(2));
        }
        [Test] public void ForeignProductsAndDisposedCallbacksCannotGrant()
        {
            using (var coordinator = Coordinator())
            {
                Own(); var other = CreateProduct(id: "other");
                client.EmitPaid(new PendingOrder(new Cart(new CartItem(other)), new Info("123")));
                Assert.That(coordinator.IsOwned, Is.False);
                service.Dispose(); client.EmitPaid(Paid()); Assert.That(coordinator.IsOwned, Is.False);
            }
        }

        // Unity's Product constructors/setters are internal; reflection builds SDK fixtures only.
        private static Product CreateProduct(string id = ProductId, string price = "localized test price")
        {
            var result = (Product)Activator.CreateInstance(typeof(Product), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { new ProductDefinition(ApplePurchaseService.LogicalProduct, id, ProductType.NonConsumable),
                    new ProductMetadata(price, "store title", "store description", "USD", 1m) }, null);
            typeof(Product).GetProperty(nameof(Product.availableToPurchase)).SetValue(result, true);
            return result;
        }
        private sealed class Info : IOrderInfo
        {
            public Info(string id) { TransactionID = id; }
            public IAppleOrderInfo Apple => null; public IGoogleOrderInfo Google => null;
            public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; } = new List<IPurchasedProductInfo>();
            public string Receipt => "private-test-receipt"; public string TransactionID { get; }
        }
        private sealed class FaultStore : IMonetizationStateStore
        {
            private readonly MemoryMonetizationStateStore store = new MemoryMonetizationStateStore();
            public bool Fail;
            public bool TryLoad(out MonetizationState state) => store.TryLoad(out state);
            public bool TryCommit(MonetizationState state, long revision) => !Fail && store.TryCommit(state, revision);
        }
        private sealed class FakeOfferCodeSheet : IAppleOfferCodeSheet
        {
            public int Presentations;
            public bool Disposed;
            public Action<OfferCodeRedemptionResult> Complete;
            public void Present(Action<OfferCodeRedemptionResult> complete) { Presentations++; Complete = complete; }
            public void Close(OfferCodeRedemptionResult result) { var callback = Complete; Complete = null; callback(result); }
            public void Dispose() { Disposed = true; Complete = null; }
        }
        private sealed class FakeEvidence : IAppleEvidence
        {
            public AppleEvidence Proof = new AppleEvidence { state = 3, complete = true };
            public string Cached; public bool Deferred, Delay, WasSynchronized; public bool MarkerWritesSucceed = true; public int Reads;
            public Action<AppleEvidence> Pending;
            public bool VerifyCached(string product, string reference) => reference != null && reference == Cached;
            public void Read(string product, bool synchronized, Action<AppleEvidence> complete)
            { Reads++; WasSynchronized = synchronized; if (Delay) Pending = complete; else complete(Proof); }
            public bool SetDeferred(string product, bool value)
            { if (MarkerWritesSucceed) Deferred = value; return MarkerWritesSucceed; }
            public void Dispose() { }
        }
        private sealed class FakeStore : IAppleStoreClient
        {
            public event Action<List<Product>> Products; public event Action ProductsFailed;
            public event Action<Orders> Purchases; public event Action PurchasesFailed;
            public event Action<PendingOrder> Paid; public event Action<DeferredOrder> Deferred;
            public event Action<FailedOrder> Failed; public event Action<Order> Confirmed;
            public event Action<string> Revoked; public event Action Disconnected;
            public Product Product; public bool ProductFailure, FetchFailure;
            public int Fetches, Buys, Confirms; public string MappedProduct; public PendingOrder LastConfirmed;
            public Action<bool> RestoreComplete; public Action<PendingOrder> BeforeConfirm;
            public Orders Inventory = new Orders(Array.Empty<ConfirmedOrder>(), Array.Empty<PendingOrder>(), Array.Empty<DeferredOrder>());
            public Task Connect() => Task.CompletedTask;
            public void FetchProducts(string id)
            { MappedProduct = id; if (ProductFailure) ProductsFailed?.Invoke(); else Products?.Invoke(new List<Product> { Product }); }
            public void FetchPurchases() { Fetches++; if (FetchFailure) PurchasesFailed?.Invoke(); else EmitInventory(); }
            public void Buy(Product value) { Buys++; }
            public void Restore(Action<bool> complete) { RestoreComplete = complete; }
            public void Confirm(PendingOrder order) { BeforeConfirm?.Invoke(order); Confirms++; LastConfirmed = order; }
            public void EmitInventory() => Purchases?.Invoke(Inventory);
            public void EmitPaid(PendingOrder order) => Paid?.Invoke(order);
            public void EmitDeferred(DeferredOrder order) => Deferred?.Invoke(order);
            public void EmitFailure(FailedOrder order) => Failed?.Invoke(order);
            public void EmitConfirmed(Order order) => Confirmed?.Invoke(order);
            public void EmitRevoked(string product) => Revoked?.Invoke(product);
            public void Dispose() { Products = null; ProductsFailed = null; Purchases = null; PurchasesFailed = null;
                Paid = null; Deferred = null; Failed = null; Confirmed = null; Revoked = null; Disconnected = null; }
        }
    }
}
