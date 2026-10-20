using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace CatsAndArrows.Purchasing.Apple
{
    internal enum AppleOwnership { Unknown, Owned, Revoked, NotOwned }

    [Serializable]
    internal sealed class AppleEvidence
    {
        public int state;
        public string reference;
        public string transaction;
        public string[] unfinished = Array.Empty<string>();
        public string[] confirmed = Array.Empty<string>();
        public bool durable;
        public bool deferred;
        public bool complete;
        public AppleOwnership Ownership => (AppleOwnership)state;
    }

    internal interface IAppleEvidence : IDisposable
    {
        bool VerifyCached(string product, string reference);
        void Read(string product, bool synchronized, Action<AppleEvidence> complete);
        bool SetDeferred(string product, bool value);
    }

    // Only native StoreKit VerificationResult.verified creates cache evidence.
    // An editable coordinator envelope or a decoded JWS never establishes ownership.
    internal sealed class NativeAppleEvidence : IAppleEvidence
    {
        private delegate void Receive(long request, string json);
        private static readonly Receive Receiver = OnReceive;
        private static readonly Dictionary<long, Action<AppleEvidence>> Pending = new Dictionary<long, Action<AppleEvidence>>();
        private readonly HashSet<long> requests = new HashSet<long>();
        private readonly bool review;
        private static long nextRequest;
        public NativeAppleEvidence(bool review = false) { this.review = review; }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.I1)] private static extern bool PawsIapVerifyCache(string product, string reference, [MarshalAs(UnmanagedType.I1)] bool review);
        [DllImport("__Internal")] private static extern void PawsIapRead(string product, [MarshalAs(UnmanagedType.I1)] bool synchronized,
            [MarshalAs(UnmanagedType.I1)] bool review, long request, Receive receive);
        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.I1)] private static extern bool PawsIapSetDeferred(string product,
            [MarshalAs(UnmanagedType.I1)] bool value, [MarshalAs(UnmanagedType.I1)] bool review);
#endif
        public bool VerifyCached(string product, string reference)
        {
#if UNITY_IOS && !UNITY_EDITOR
            return PawsIapVerifyCache(product, reference, review);
#else
            return false;
#endif
        }
        public bool SetDeferred(string product, bool value)
        {
#if UNITY_IOS && !UNITY_EDITOR
            return PawsIapSetDeferred(product, value, review);
#else
            return false;
#endif
        }
        public void Read(string product, bool synchronized, Action<AppleEvidence> complete)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var request = ++nextRequest;
            requests.Add(request);
            Pending[request] = evidence => { requests.Remove(request); complete(evidence); };
            PawsIapRead(product, synchronized, review, request, Receiver);
#else
            complete(new AppleEvidence());
#endif
        }
        [MonoPInvokeCallback(typeof(Receive))]
        private static void OnReceive(long request, string json)
        {
            if (!Pending.TryGetValue(request, out var callback)) return;
            Pending.Remove(request);
            AppleEvidence result;
            try { result = JsonUtility.FromJson<AppleEvidence>(json) ?? new AppleEvidence(); }
            catch { result = new AppleEvidence(); }
            callback(result);
        }
        public void Dispose()
        {
            foreach (var request in requests) Pending.Remove(request);
            requests.Clear();
        }
    }
}
