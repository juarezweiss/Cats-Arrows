using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing;

namespace CatsAndArrows.Purchasing.Apple
{
    // Provider-private seam. Neither these SDK models nor raw store payloads leave this assembly.
    internal interface IAppleStoreClient : IDisposable
    {
        event Action<List<Product>> Products;
        event Action ProductsFailed;
        event Action<Orders> Purchases;
        event Action PurchasesFailed;
        event Action<PendingOrder> Paid;
        event Action<DeferredOrder> Deferred;
        event Action<FailedOrder> Failed;
        event Action<Order> Confirmed;
        event Action<string> Revoked;
        event Action Disconnected;
        Task Connect();
        void FetchProducts(string productId);
        void FetchPurchases();
        void Buy(Product product);
        void Restore(Action<bool> complete);
        void Confirm(PendingOrder order);
    }

    internal sealed class UnityAppleStoreClient : IAppleStoreClient
    {
        private readonly StoreController store = new StoreController(AppleAppStore.Name);
        public event Action<List<Product>> Products;
        public event Action ProductsFailed;
        public event Action<Orders> Purchases;
        public event Action PurchasesFailed;
        public event Action<PendingOrder> Paid;
        public event Action<DeferredOrder> Deferred;
        public event Action<FailedOrder> Failed;
        public event Action<Order> Confirmed;
        public event Action<string> Revoked;
        public event Action Disconnected;

        public UnityAppleStoreClient(bool simulateAskToBuy = false)
        {
            // Issue #64: SDK-supported sandbox initiation only; does not change entitlement verification.
            // IAP caches this service by store name. Clear a previous review client's flag too.
            store.AppleStoreExtendedPurchaseService.simulateAskToBuy = simulateAskToBuy;
            store.SetStoreReconnectionRetryPolicyOnDisconnection(null);
            store.OnProductsFetched += OnProducts;
            store.OnProductsFetchFailed += OnProductsFailed;
            store.OnPurchasesFetched += OnPurchases;
            store.OnPurchasesFetchFailed += OnPurchasesFailed;
            store.OnPurchasePending += OnPaid;
            store.OnPurchaseDeferred += OnDeferred;
            store.OnPurchaseFailed += OnFailed;
            store.OnPurchaseConfirmed += OnConfirmed;
            store.OnStoreDisconnected += OnDisconnected;
            store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += OnRevoked;
#pragma warning disable CS0612, CS0618
            // Use StoreKit 2 verification, never an SK1 receipt-refresh credential prompt on launch.
            store.AppleStoreExtendedPurchaseService.SetRefreshAppReceipt(false);
#pragma warning restore CS0612, CS0618
        }
        public Task Connect() => store.Connect();
        public void FetchProducts(string id) => store.FetchProductsWithNoRetries(new List<ProductDefinition>
            { new ProductDefinition(ApplePurchaseService.LogicalProduct, id, ProductType.NonConsumable) });
        public void FetchPurchases() => store.FetchPurchases();
        public void Buy(Product product) => store.PurchaseProduct(product);
        // IAP 5.3.1 itself fetches purchases after successful AppStore.sync. Do not fetch twice.
        public void Restore(Action<bool> complete) => store.RestoreTransactions((ok, _) => complete(ok));
        public void Confirm(PendingOrder order) => store.ConfirmPurchase(order);
        private void OnProducts(List<Product> value) => Products?.Invoke(value);
        private void OnProductsFailed(ProductFetchFailed _) => ProductsFailed?.Invoke();
        private void OnPurchases(Orders value) => Purchases?.Invoke(value);
        private void OnPurchasesFailed(PurchasesFetchFailureDescription _) => PurchasesFailed?.Invoke();
        private void OnPaid(PendingOrder value) => Paid?.Invoke(value);
        private void OnDeferred(DeferredOrder value) => Deferred?.Invoke(value);
        private void OnFailed(FailedOrder value) => Failed?.Invoke(value);
        private void OnConfirmed(Order value) => Confirmed?.Invoke(value);
        private void OnRevoked(string value) => Revoked?.Invoke(value);
        private void OnDisconnected(StoreConnectionFailureDescription _) => Disconnected?.Invoke();
        public void Dispose()
        {
            store.OnProductsFetched -= OnProducts; store.OnProductsFetchFailed -= OnProductsFailed;
            store.OnPurchasesFetched -= OnPurchases; store.OnPurchasesFetchFailed -= OnPurchasesFailed;
            store.OnPurchasePending -= OnPaid; store.OnPurchaseDeferred -= OnDeferred;
            store.OnPurchaseFailed -= OnFailed; store.OnPurchaseConfirmed -= OnConfirmed;
            store.OnStoreDisconnected -= OnDisconnected;
            store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked -= OnRevoked;
        }
    }
}
