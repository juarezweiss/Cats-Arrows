using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CatsAndArrows.Monetization;
using UnityEngine;
using UnityEngine.Purchasing;
using PurchasePort = CatsAndArrows.Monetization.IPurchaseService;

namespace CatsAndArrows.Purchasing.Apple
{
    public enum ApplePurchaseEnvironment { Production, SandboxReview }
    // Issue #64. Application-thread adapter. Constructing it never connects to a store.
    public sealed class ApplePurchaseService : PurchasePort, IDisposable
    {
        public const string LogicalProduct = "RemoveAllAds";
        public const string StoreNamespace = "apple-storekit2-v1";
        private readonly string productId;
        private readonly Func<IAppleStoreClient> createClient;
        private readonly IAppleEvidence evidence;
        private readonly Func<double> clock;
        private readonly Dictionary<string, PendingOrder> unfinished = new Dictionary<string, PendingOrder>();
        private readonly HashSet<string> verifiedUnfinished = new HashSet<string>();
        private readonly HashSet<string> verifiedFinished = new HashSet<string>();
        private readonly HashSet<string> finished = new HashSet<string>();
        private readonly List<Action<PurchaseResult>> queries = new List<Action<PurchaseResult>>();
        private IAppleStoreClient client;
        private Product product;
        private Action<PurchaseResult> purchase;
        private Action<bool> finish;
        private string finishing, exposedTransaction, currentReference;
        private bool connected, connecting, querying, restoring, restoreSucceeded, restoreFetched, deferred, disposed;
        private bool durableEvidence;
        private bool nativePurchaseOpen;
        private long generation;
        private double started, finishStarted;
        public event Action<PurchaseResult> Updated;

        public ApplePurchaseService(string appleProductId, ApplePurchaseEnvironment environment = ApplePurchaseEnvironment.Production,
            bool recoveredPendingPurchase = false, bool simulateAskToBuy = false) : this(appleProductId,
            CreateNativeClientFactory(environment, simulateAskToBuy), new NativeAppleEvidence(environment == ApplePurchaseEnvironment.SandboxReview),
            () => Time.realtimeSinceStartupAsDouble, recoveredPendingPurchase) { }

        private static Func<IAppleStoreClient> CreateNativeClientFactory(ApplePurchaseEnvironment environment, bool simulateAskToBuy)
        {
#if PAWS_APPLE_IAP_REVIEW
            const bool reviewBuild = true;
#else
            const bool reviewBuild = false;
#endif
            ValidateAskToBuySimulation(environment, simulateAskToBuy, reviewBuild, Debug.isDebugBuild);
            return () => CreateNativeClient(simulateAskToBuy);
        }

        internal static void ValidateAskToBuySimulation(ApplePurchaseEnvironment environment, bool requested,
            bool reviewBuild, bool developmentBuild)
        {
            if (requested && (environment != ApplePurchaseEnvironment.SandboxReview || !reviewBuild || !developmentBuild))
                throw new InvalidOperationException("Ask to Buy simulation requires an explicit sandbox development review build.");
        }

        private static IAppleStoreClient CreateNativeClient(bool simulateAskToBuy)
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("The Apple store is available only in an iOS player.");
            return new UnityAppleStoreClient(simulateAskToBuy);
        }

        internal ApplePurchaseService(string appleProductId, Func<IAppleStoreClient> createClient,
            IAppleEvidence evidence, Func<double> clock, bool recoveredPendingPurchase = false)
        {
            if (string.IsNullOrWhiteSpace(appleProductId) || appleProductId.Trim() != appleProductId)
                throw new ArgumentException("An owner-approved Apple product mapping is required.", nameof(appleProductId));
            productId = appleProductId;
            this.createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
            this.evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            // Native inventory cannot enumerate unpaid Ask to Buy. Recover the coordinator's
            // normalized pending flag too, so a failed private-marker write cannot erase it.
            deferred = recoveredPendingPurchase;
        }

        public PurchaseOffer GetOffer() => new PurchaseOffer(!disposed && connected && !nativePurchaseOpen && !deferred && product?.availableToPurchase == true &&
            !string.IsNullOrWhiteSpace(product.metadata?.localizedPriceString), product?.metadata?.localizedPriceString);
        public bool VerifyCachedOwnership(string storeNamespace, string reference) => !disposed &&
            storeNamespace == StoreNamespace && !string.IsNullOrEmpty(reference) && evidence.VerifyCached(productId, reference);

        public void Refresh(Action<PurchaseResult> complete)
        {
            if (disposed) { complete(new PurchaseResult(PurchaseOutcome.Unavailable)); return; }
            Tick();
            queries.Add(complete);
            if (querying || restoring || purchase != null) return;
            querying = true; started = clock();
            EnsureConnected();
        }
        private async void EnsureConnected()
        {
            if (connected)
            {
                try { client.FetchProducts(productId); } catch { OnProductsFailed(); }
                return;
            }
            if (connecting) return;
            connecting = true;
            try
            {
                if (client == null)
                {
                    client = createClient();
                    client.Products += OnProducts; client.ProductsFailed += OnProductsFailed;
                    client.Purchases += OnPurchases; client.PurchasesFailed += OnPurchasesFailed;
                    client.Paid += OnPaid; client.Deferred += OnDeferred; client.Failed += OnFailed;
                    client.Confirmed += OnConfirmed; client.Revoked += OnRevoked; client.Disconnected += OnDisconnected;
                }
                await client.Connect();
                if (disposed) return;
                connected = true;
                client.FetchProducts(productId);
            }
            catch { OnDisconnected(); }
            finally { connecting = false; }
        }
        private void OnProducts(List<Product> products)
        {
            if (disposed) return;
            product = products?.FirstOrDefault(p => p.definition.id == LogicalProduct &&
                p.definition.storeSpecificId == productId && p.definition.type == ProductType.NonConsumable);
            Fetch();
        }
        private void OnProductsFailed() { product = null; Fetch(); }
        private void Fetch()
        {
            if (disposed) return;
            try
            {
                if (restoring) client.Restore(OnRestore);
                else client.FetchPurchases();
            }
            catch { OnPurchasesFailed(); }
        }

        public void Purchase(long requestId, Action<PurchaseResult> complete)
        {
            Tick();
            if (!GetOffer().Available || deferred || purchase != null || querying || restoring)
            { complete(new PurchaseResult(PurchaseOutcome.Unavailable)); return; }
            purchase = complete; nativePurchaseOpen = true; started = clock();
            try { client.Buy(product); }
            catch { nativePurchaseOpen = false; Complete(new PurchaseResult(PurchaseOutcome.Failed)); }
        }
        public void Restore(long requestId, Action<PurchaseResult> complete)
        {
            Tick();
            if (disposed || restoring || querying || purchase != null)
            { complete(new PurchaseResult(PurchaseOutcome.Unavailable)); return; }
            queries.Add(complete); restoring = true; restoreSucceeded = restoreFetched = false; started = clock();
            EnsureConnected();
        }
        private void OnRestore(bool success)
        {
            if (disposed || !restoring) return;
            if (!success) { OnPurchasesFailed(); return; }
            restoreSucceeded = true;
            // SDK starts FetchPurchases before its restore callback. Either event can arrive first.
            if (restoreFetched) Verify(true);
        }
        private bool Matches(Order order) => order?.CartOrdered?.Items()?.Any(item =>
            item.Product?.definition.storeSpecificId == productId &&
            item.Product.definition.type == ProductType.NonConsumable) == true;
        private string Reference(string id)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(productId + ":" + id))).Replace("-", "").ToLowerInvariant();
        }
        private void Remember(PendingOrder order)
        {
            if (Matches(order) && !string.IsNullOrEmpty(order.Info?.TransactionID))
                unfinished[Reference(order.Info.TransactionID)] = order;
        }
        private void OnPurchases(Orders orders)
        {
            if (disposed || orders == null) { OnPurchasesFailed(); return; }
            foreach (var order in orders.PendingOrders) Remember(order);
            if (orders.DeferredOrders.Any(Matches)) MarkDeferred();
            if (restoring) { restoreFetched = true; if (!restoreSucceeded) return; }
            Verify(restoreSucceeded);
        }
        private void OnPaid(PendingOrder order)
        {
            if (disposed || !Matches(order)) return;
            nativePurchaseOpen = true;
            Remember(order);
            // A store update must not settle Restore through an ordinary (unsynchronized)
            // read, even after sync succeeds. Wait for its inventory/acknowledgement pair.
            if (restoring)
            {
                if (restoreSucceeded && restoreFetched) Verify(true);
                return;
            }
            Verify(false); // PendingOrder is paid-but-unfinished, NOT Ask to Buy and NOT verification.
        }
        private void MarkDeferred() { deferred = true; evidence.SetDeferred(productId, true); }
        private void OnDeferred(DeferredOrder order)
        {
            if (disposed || !Matches(order)) return;
            generation++; nativePurchaseOpen = false; MarkDeferred();
            Complete(new PurchaseResult(PurchaseOutcome.Pending, transactionReference: Reference("deferred")));
        }
        private void OnFailed(FailedOrder order)
        {
            if (disposed || !Matches(order)) return;
            if (order.FailureReason == PurchaseFailureReason.DuplicateTransaction) { RefreshAfterAlreadyOwned(); return; }
            if (order.FailureReason == PurchaseFailureReason.ExistingPurchasePending) { OnDeferred(new DeferredOrder(order.CartOrdered, order.Info)); return; }
            generation++;
            // Only an actual terminal store result clears deferred state; missing inventory never does.
            var terminal = order.FailureReason == PurchaseFailureReason.UserCancelled ||
                order.FailureReason == PurchaseFailureReason.PaymentDeclined || order.FailureReason == PurchaseFailureReason.ProductUnavailable;
            var cleared = false;
            if (terminal) { nativePurchaseOpen = deferred = false; cleared = evidence.SetDeferred(productId, false); }
            Complete(new PurchaseResult(order.FailureReason == PurchaseFailureReason.UserCancelled ?
                PurchaseOutcome.Cancelled : PurchaseOutcome.Failed, pendingResolved: cleared));
        }
        private void RefreshAfterAlreadyOwned()
        { querying = true; started = clock(); client.FetchPurchases(); }
        private void OnRevoked(string id)
        {
            if (!disposed && (id == productId || id == LogicalProduct)) Verify(false);
        }

        private void Verify(bool synchronized)
        {
            var current = ++generation;
            evidence.Read(productId, synchronized, proof =>
            {
                if (disposed || current != generation) return;
                deferred = deferred || proof.deferred;
                if (proof.Ownership == AppleOwnership.Owned && !string.IsNullOrEmpty(proof.reference))
                {
                    currentReference = proof.reference; durableEvidence = proof.durable;
                    nativePurchaseOpen = deferred = false;
                    verifiedUnfinished.Clear();
                    foreach (var id in proof.unfinished ?? Array.Empty<string>()) verifiedUnfinished.Add(Reference(id));
                    verifiedFinished.Clear();
                    if (proof.complete)
                        foreach (var id in proof.confirmed ?? Array.Empty<string>()) verifiedFinished.Add(Reference(id));
                    // The coordinator journals one transaction. Keep exposing it until Finish succeeds;
                    // then emit the next unfinished order, so concurrent restores cannot replace its journal.
                    if (exposedTransaction == null || !verifiedUnfinished.Contains(exposedTransaction))
                        exposedTransaction = NextUnfinished();
                    Complete(Owned());
                }
                else if (proof.Ownership == AppleOwnership.Revoked || (proof.Ownership == AppleOwnership.NotOwned && proof.complete))
                {
                    if (synchronized && proof.complete && !deferred) nativePurchaseOpen = false;
                    currentReference = null; durableEvidence = false; exposedTransaction = null;
                    verifiedUnfinished.Clear(); verifiedFinished.Clear();
                    Complete(new PurchaseResult(proof.Ownership == AppleOwnership.Revoked ? PurchaseOutcome.Revoked :
                        deferred ? PurchaseOutcome.Pending : PurchaseOutcome.NotOwned, verified: true,
                        completeInventory: true, pendingResolved: !deferred, storeNamespace: StoreNamespace));
                }
                else Complete(new PurchaseResult(deferred ? PurchaseOutcome.Pending : PurchaseOutcome.Unavailable));
            });
        }
        private string NextUnfinished() => unfinished.Keys.OrderBy(id => id).FirstOrDefault(id =>
            verifiedUnfinished.Contains(id) && !finished.Contains(id));
        private PurchaseResult Owned() => new PurchaseResult(PurchaseOutcome.Owned, verified: true,
            pendingResolved: true, storeNamespace: StoreNamespace, evidenceReference: currentReference,
            transactionReference: exposedTransaction);
        private void Complete(PurchaseResult result)
        {
            querying = restoring = restoreSucceeded = restoreFetched = false;
            var callbacks = queries.ToArray(); queries.Clear();
            var buy = purchase; purchase = null;
            foreach (var callback in callbacks) callback(result);
            buy?.Invoke(result);
            // Requests receive one result. Unsolicited/late store updates use Updated.
            // Double delivery here could overwrite a subsequent transaction's single journal.
            if (callbacks.Length == 0 && buy == null) Updated?.Invoke(result);
        }
        private void OnPurchasesFailed()
        {
            if (disposed) return;
            generation++;
            Complete(new PurchaseResult(PurchaseOutcome.Unavailable));
        }
        private void OnDisconnected()
        { connected = false; product = null; OnPurchasesFailed(); CompleteFinish(false); }

        public void Finish(string transactionReference, Action<bool> complete)
        {
            if (disposed || !connected || finish != null || !durableEvidence ||
                !evidence.VerifyCached(productId, currentReference)) { complete(false); return; }
            if (finished.Contains(transactionReference) || verifiedFinished.Contains(transactionReference))
            { complete(true); return; }
            if (!verifiedUnfinished.Contains(transactionReference) || !unfinished.TryGetValue(transactionReference, out var order))
            { complete(false); return; }
            finishing = transactionReference; finish = complete; finishStarted = clock();
            try { client.Confirm(order); } catch { CompleteFinish(false); }
        }
        private void OnConfirmed(Order order)
        {
            if (disposed || !Matches(order) || string.IsNullOrEmpty(order.Info?.TransactionID)) return;
            var reference = Reference(order.Info.TransactionID);
            if (reference != finishing) return;
            if (!(order is ConfirmedOrder)) { CompleteFinish(false); return; }
            // SDK acknowledgement alone is insufficient: native finish can no-op.
            // Require a complete verified unfinished inventory with this ID absent.
            evidence.Read(productId, false, proof =>
            {
                if (disposed || finishing != reference) return;
                CompleteFinish(proof.complete && !(proof.unfinished ?? Array.Empty<string>()).Contains(order.Info.TransactionID));
            });
        }
        private void CompleteFinish(bool success)
        {
            var callback = finish; var reference = finishing;
            finish = null; finishing = null;
            if (success)
            {
                finished.Add(reference); unfinished.Remove(reference); verifiedUnfinished.Remove(reference);
                exposedTransaction = NextUnfinished();
            }
            callback?.Invoke(success);
            if (success && exposedTransaction != null && currentReference != null) Updated?.Invoke(Owned());
        }
        // Called from composition Update. The coordinator has its own deadline; this also releases
        // provider query/confirmation locks without assuming that a native purchase was cancelled.
        public void Tick()
        {
            if (disposed) return;
            if ((querying || restoring || purchase != null) && clock() - started >= MonetizationCoordinator.OperationTimeoutSeconds)
            { generation++; Complete(new PurchaseResult(PurchaseOutcome.Unavailable)); }
            if (finish != null && clock() - finishStarted >= MonetizationCoordinator.OperationTimeoutSeconds) CompleteFinish(false);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; generation++;
            client?.Dispose(); evidence.Dispose(); queries.Clear(); purchase = null; finish = null; Updated = null;
        }
    }
}
