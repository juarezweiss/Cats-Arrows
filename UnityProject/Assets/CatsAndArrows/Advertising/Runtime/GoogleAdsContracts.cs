using System;
using System.Collections.Generic;
using CatsAndArrows.Monetization;

namespace CatsAndArrows.Advertising
{
    // #66: only the Google binding assembly references SDK types. False/null are
    // normalized infrastructure failure, never evidence of a voluntary close.
    public interface IGoogleAdsBridge
    {
        bool CanRequestAds { get; }
        bool PrivacyOptionsRequired { get; }
        void UpdateConsent(Action<bool> complete);
        void LoadAndShowRequiredConsentForm(Action<bool> complete);
        void ShowPrivacyOptions(Action<bool> complete);
        void Initialize(Action<bool> complete);
        void Load(AdPlacement placement, Action<IGoogleAd> complete);
    }

    public interface IGoogleAd
    {
        bool CanShow { get; }
        // Closed/failed certify safe native closure. Closed also certifies that
        // reward callbacks settled (AdMob-only reward precedes dismissal).
        // Destroy is not a supported way to dismiss a presented native ad.
        void Show(Action presented, Action rewardEarned, Action closed, Action failed);
        void Destroy();
    }

    public sealed class QueuedAdDispatcher
    {
        private readonly object gate = new object();
        private readonly Queue<Action> pending = new Queue<Action>();
        public void Post(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (gate) pending.Enqueue(action);
        }
        // Called by the composition root on the application thread, before Tick.
        public void Drain()
        {
            while (true)
            {
                Action action;
                lock (gate) { if (pending.Count == 0) return; action = pending.Dequeue(); }
                action();
            }
        }
    }
}
