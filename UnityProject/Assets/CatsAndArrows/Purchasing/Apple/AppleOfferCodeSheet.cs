using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using CatsAndArrows.Monetization;

namespace CatsAndArrows.Purchasing.Apple
{
    internal interface IAppleOfferCodeSheet : IDisposable
    {
        void Present(Action<OfferCodeRedemptionResult> complete);
    }

    // Issue #65. Presentation outcomes never establish or persist ownership.
    internal sealed class NativeAppleOfferCodeSheet : IAppleOfferCodeSheet
    {
        private delegate void Receive(long request, int outcome);
        private static readonly Receive Receiver = OnReceive;
        private static readonly object Gate = new object();
        private static readonly Dictionary<long, NativeAppleOfferCodeSheet> Pending =
            new Dictionary<long, NativeAppleOfferCodeSheet>();
        private static long nextRequest;
        private readonly SynchronizationContext applicationContext = SynchronizationContext.Current;
        private Action<OfferCodeRedemptionResult> completion;
        private long activeRequest;
        private bool disposed;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PawsIapPresentOfferCodeSheet(long request, Receive receive);
#endif

        public void Present(Action<OfferCodeRedemptionResult> complete)
        {
            if (complete == null) throw new ArgumentNullException(nameof(complete));
            if (disposed || completion != null)
            {
                complete(OfferCodeRedemptionResult.Unavailable);
                return;
            }
#if UNITY_IOS && !UNITY_EDITOR
            var request = Interlocked.Increment(ref nextRequest);
            activeRequest = request;
            completion = complete;
            lock (Gate) Pending.Add(request, this);
            try { PawsIapPresentOfferCodeSheet(request, Receiver); }
            catch (DllNotFoundException) { Deliver(request, (int)OfferCodeRedemptionResult.Unavailable); }
            catch (EntryPointNotFoundException) { Deliver(request, (int)OfferCodeRedemptionResult.Unavailable); }
#else
            complete(OfferCodeRedemptionResult.Unsupported);
#endif
        }

        [MonoPInvokeCallback(typeof(Receive))]
        private static void OnReceive(long request, int outcome)
        {
            NativeAppleOfferCodeSheet owner;
            lock (Gate)
                if (!Pending.TryGetValue(request, out owner)) return;
            // StoreKit completes on the native main actor; post back through Unity's
            // captured application context before touching the coordinator/UI.
            if (owner.applicationContext != null)
                owner.applicationContext.Post(_ => owner.Deliver(request, outcome), null);
            else
                owner.Deliver(request, outcome);
        }

        private void Deliver(long request, int outcome)
        {
            lock (Gate)
            {
                if (disposed || activeRequest != request || !Pending.Remove(request)) return;
            }
            var callback = completion;
            completion = null;
            activeRequest = 0;
            var result = outcome == (int)OfferCodeRedemptionResult.Closed ? OfferCodeRedemptionResult.Closed :
                outcome == (int)OfferCodeRedemptionResult.Unsupported ? OfferCodeRedemptionResult.Unsupported :
                OfferCodeRedemptionResult.Unavailable;
            callback?.Invoke(result);
        }

        public void Dispose()
        {
            lock (Gate)
            {
                disposed = true;
                Pending.Remove(activeRequest);
                activeRequest = 0;
                completion = null;
            }
        }
    }
}
