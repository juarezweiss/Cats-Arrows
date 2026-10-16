using System;
using CatsAndArrows.UI;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #44: time jumps and cancellation are deterministic without Unity scheduling.
    public sealed class CompletionCelebrationTests
    {
        private double now;
        private object session;
        private CompletionCelebration celebration;

        [SetUp] public void SetUp()
        {
            now = 100; session = new object();
            celebration = new CompletionCelebration(() => now);
        }
        private void Tick(double elapsed) { now = 100 + elapsed; celebration.Tick(); }

        [Test] public void ConfettiEndsAtThreeSecondsWhileMessageRemains()
        {
            Assert.That(celebration.TryStart(session), Is.True);
            Assert.That(celebration.MessageOpacity, Is.Zero);
            Assert.That(celebration.HasConfetti, Is.True);
            Tick(.125); Assert.That(celebration.MessageOpacity, Is.EqualTo(.5f));
            Tick(.25); Assert.That(celebration.MessageOpacity, Is.EqualTo(1));
            Tick(2.999); Assert.That(celebration.HasConfetti, Is.True);
            Tick(3); Assert.That(celebration.HasConfetti, Is.False);
            Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.Message));
            Assert.That(celebration.MessageOpacity, Is.EqualTo(1));
            Assert.That(celebration.BoardOpacity, Is.LessThan(1));
        }

        [Test] public void MessageFinishesFadingBeforeActionsAppearAndBecomeInteractive()
        {
            celebration.TryStart(session);
            Tick(3.999); Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.Message));
            Tick(4); Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.MessageFadeOut));
            Assert.That(celebration.MessageOpacity, Is.EqualTo(1));
            Tick(4.125); Assert.That(celebration.MessageOpacity, Is.EqualTo(.5f));
            Assert.That(celebration.ActionsOpacity, Is.Zero);
            Assert.That(celebration.ActionsEnabled, Is.False);
            Tick(4.25); Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.ActionsFadeIn));
            Assert.That(celebration.MessageOpacity, Is.Zero);
            Assert.That(celebration.ActionsOpacity, Is.Zero);
            Tick(4.375); Assert.That(celebration.ActionsOpacity, Is.EqualTo(.5f));
            Assert.That(celebration.ActionsEnabled, Is.False);
            Tick(4.5); Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.Actions));
            Assert.That(celebration.ActionsOpacity, Is.EqualTo(1));
            Assert.That(celebration.ActionsEnabled, Is.True);
        }

        [Test] public void JumpPastAnimationShowsActionsWithoutReplayingEffects()
        {
            celebration.TryStart(session);
            Tick(9000);
            Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.Actions));
            Assert.That(celebration.HasConfetti, Is.False);
            Assert.That(celebration.MessageOpacity, Is.Zero);
            Assert.That(celebration.ActionsEnabled, Is.True);
        }

        [Test] public void DuplicateStartAndCancelNeverReplaySameSessionButNewSessionStartsFresh()
        {
            celebration.TryStart(session); Tick(2);
            Assert.That(celebration.TryStart(session), Is.False);
            Tick(3); Assert.That(celebration.HasConfetti, Is.False);
            celebration.Cancel(); AssertStatic();
            Tick(100); AssertStatic();
            Assert.That(celebration.TryStart(session), Is.False); AssertStatic();
            Assert.That(celebration.TryStart(new object()), Is.True);
            Assert.That(celebration.Elapsed, Is.Zero);
            Assert.That(celebration.HasConfetti, Is.True);
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidClockCancelsAndConsumesVictory(double invalid)
        {
            celebration.TryStart(session); now = invalid; celebration.Tick();
            AssertStatic(); Assert.That(celebration.TryStart(session), Is.False);
            Assert.That(celebration.TryStart(new object()), Is.False); AssertStatic();
        }

        [Test] public void BackwardsClockCancelsRatherThanRewinding()
        {
            celebration.TryStart(session); Tick(2); Tick(1); AssertStatic();
            Assert.That(celebration.TryStart(session), Is.False);
        }

        [Test] public void RejectsMissingClockOrIdentity()
        {
            Assert.Throws<ArgumentNullException>(() => new CompletionCelebration(null));
            Assert.Throws<ArgumentNullException>(() => celebration.TryStart(null));
        }

        private void AssertStatic()
        {
            Assert.That(celebration.IsVisible, Is.False);
            Assert.That(celebration.Phase, Is.EqualTo(CompletionPhase.Static));
            Assert.That(celebration.BoardOpacity, Is.EqualTo(1));
            Assert.That(celebration.MessageOpacity, Is.Zero);
            Assert.That(celebration.ActionsOpacity, Is.Zero);
            Assert.That(celebration.ActionsEnabled, Is.False);
            Assert.That(celebration.HasConfetti, Is.False);
        }
    }
}
