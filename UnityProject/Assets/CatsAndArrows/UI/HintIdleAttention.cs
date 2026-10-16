using System;

namespace CatsAndArrows.UI
{
    // Issue #48: presentation timing only; never requests or applies a Hint.
    public sealed class HintIdleAttention
    {
        public const double IdleSeconds = 6;
        private readonly Func<double> clock;
        private bool eligible;
        private bool interacting;
        private double attentionAt;
        public bool IsAttentionVisible { get; private set; }

        public HintIdleAttention(Func<double> clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Update(bool isEligible, bool hasActiveInteraction)
        {
            if (eligible != isEligible || interacting != hasActiveInteraction)
            {
                eligible = isEligible;
                interacting = hasActiveInteraction;
                Reset();
            }
            if (!eligible || interacting) { IsAttentionVisible = false; return; }
            if (clock() >= attentionAt) IsAttentionVisible = true;
        }

        public void Reset()
        {
            attentionAt = clock() + IdleSeconds;
            IsAttentionVisible = false;
        }
    }
}
