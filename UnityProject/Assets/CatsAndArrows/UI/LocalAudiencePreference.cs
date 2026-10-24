using System;
using CatsAndArrows.Monetization;
using UnityEngine;

namespace CatsAndArrows.UI
{
    // Local coarse band only; no birth date, account, network or analytics.
    public sealed class LocalAudiencePreference : IAudiencePreference
    {
        public const string Key = "paws.ad-audience.v1";
        public AudienceBand Current { get; private set; }
        public event Action Changed;
        public LocalAudiencePreference()
        {
            var saved = PlayerPrefs.GetInt(Key, 0);
            Current = Enum.IsDefined(typeof(AudienceBand), saved) ? (AudienceBand)saved : AudienceBand.Unknown;
        }
        public void Select(AudienceBand band)
        {
            if (!Enum.IsDefined(typeof(AudienceBand), band)) throw new ArgumentOutOfRangeException(nameof(band));
            if (Current == band) return;
            Current = band;
            try { PlayerPrefs.SetInt(Key, (int)band); PlayerPrefs.Save(); }
            catch { Current = AudienceBand.Unknown; } // Storage failure must not assume adult.
            Changed?.Invoke();
        }
    }
}
