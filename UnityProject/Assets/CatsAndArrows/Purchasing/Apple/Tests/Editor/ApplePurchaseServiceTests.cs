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
            service = CreateService();
        }
        private ApplePurchaseService CreateService() => new ApplePurchaseService(ProductId,
            () => { creations++; return client; }, evidence, () => now);
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

        [Test] public void DisabledCompositionDoesNotInitializeStoreOrEvidence()
        {
            using (new MonetizationCoordinator(false, new MemoryMonetizationStateStore(), new UnavailableAdService(), service)) { }
            Assert.That(creations, Is.Zero); Assert.That(evidence.Reads, Is.Zero);
        }
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
