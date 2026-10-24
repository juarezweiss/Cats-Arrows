using System;

namespace CatsAndArrows.Monetization
{
    public enum AudienceBand { Unknown = 0, Under13 = 1, Teen = 2, Adult = 3, PreferNotToAnswer = 4 }
    public interface IAudiencePreference
    {
        AudienceBand Current { get; }
        event Action Changed;
        void Select(AudienceBand band);
    }
}
