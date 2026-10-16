using System;

namespace CatsAndArrows.UI
{
    public enum CompletionPhase { Static, MessageFadeIn, Message, MessageFadeOut, ActionsFadeIn, Actions }

    // Issue #44: presentation timing only. Session identity prevents duplicate/rebound wins.
    public sealed class CompletionCelebration
    {
        public const double ConfettiSeconds = 3;
        public const double MessageFadeOutAt = 4;
        public const double FadeSeconds = .25;
        public const float CelebrationBoardOpacity = .82f;
        private readonly Func<double> clock;
        private object session;
        private double startedAt;
        private double lastTime;
        public CompletionPhase Phase { get; private set; }
        public double Elapsed { get; private set; }
        public bool IsVisible => Phase != CompletionPhase.Static;
        public bool HasConfetti => IsVisible && Elapsed < ConfettiSeconds;
        public bool ActionsEnabled => Phase == CompletionPhase.Actions;
        public float BoardOpacity => IsVisible ? CelebrationBoardOpacity : 1;
        public float MessageOpacity => Phase == CompletionPhase.MessageFadeIn ? (float)(Elapsed / FadeSeconds) :
            Phase == CompletionPhase.Message ? 1 : Phase == CompletionPhase.MessageFadeOut ? (float)(1 - (Elapsed - MessageFadeOutAt) / FadeSeconds) : 0;
        public float ActionsOpacity => Phase == CompletionPhase.Actions ? 1 :
            Phase == CompletionPhase.ActionsFadeIn ? (float)((Elapsed - MessageFadeOutAt - FadeSeconds) / FadeSeconds) : 0;

        public CompletionCelebration(Func<double> clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool TryStart(object sessionIdentity)
        {
            if (sessionIdentity == null) throw new ArgumentNullException(nameof(sessionIdentity));
            if (ReferenceEquals(session, sessionIdentity)) return false;
            session = sessionIdentity;
            startedAt = lastTime = clock(); Elapsed = 0;
            if (!double.IsFinite(startedAt)) { Cancel(); return false; }
            Phase = CompletionPhase.MessageFadeIn;
            return true;
        }

        public void Tick()
        {
            if (!IsVisible) return;
            var now = clock();
            if (!double.IsFinite(now) || now < lastTime) { Cancel(); return; }
            lastTime = now; Elapsed = now - startedAt;
            if (!double.IsFinite(Elapsed)) { Cancel(); return; }
            Phase = Elapsed < FadeSeconds ? CompletionPhase.MessageFadeIn :
                Elapsed < MessageFadeOutAt ? CompletionPhase.Message :
                Elapsed < MessageFadeOutAt + FadeSeconds ? CompletionPhase.MessageFadeOut :
                Elapsed < MessageFadeOutAt + FadeSeconds * 2 ? CompletionPhase.ActionsFadeIn : CompletionPhase.Actions;
        }

        // Keep the consumed identity: cancellation never replays the same victory.
        public void Cancel() => Phase = CompletionPhase.Static;
    }
}
