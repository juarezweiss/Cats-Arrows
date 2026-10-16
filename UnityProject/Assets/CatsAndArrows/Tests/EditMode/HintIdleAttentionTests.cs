using System;
using CatsAndArrows.UI;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #48: manual monotonic time, with no sleeps or gameplay mutations.
    public sealed class HintIdleAttentionTests
    {
        private double now;
        private HintIdleAttention attention;

        [SetUp]
        public void SetUp()
        {
            now = 100;
            attention = new HintIdleAttention(() => now);
        }

        [Test]
        public void ActivatesAtSixSecondsAndStaysStable()
        {
            attention.Update(true, false);
            now += 5.999; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.False);
            now = 106; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
            now = 1000; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
        }

        [TestCase(5.999)]
        [TestCase(6)]
        [TestCase(20)]
        public void ResetBeforeOrAfterActivationStartsAFullNewInterval(double delay)
        {
            attention.Update(true, false);
            now += delay; attention.Update(true, false);
            attention.Reset();
            Assert.That(attention.IsAttentionVisible, Is.False);
            now += 5.999; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.False);
            now += .002; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
        }

        [Test]
        public void ActivePressSuppressesAttentionUntilAFullIntervalAfterEndOrCancel()
        {
            attention.Update(true, false);
            now += 6; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
            attention.Update(true, true);
            Assert.That(attention.IsAttentionVisible, Is.False);
            now += 60; attention.Update(true, true);
            Assert.That(attention.IsAttentionVisible, Is.False);
            attention.Update(true, false);
            now += 5.999; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.False);
            now += .002; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
        }

        [Test]
        public void IneligibleTimeAndRepeatedResumeCyclesNeverAccumulate()
        {
            for (var cycle = 0; cycle < 4; cycle++)
            {
                attention.Update(false, false);
                now += 100;
                attention.Update(false, false);
                Assert.That(attention.IsAttentionVisible, Is.False);
                attention.Update(true, false);
                Assert.That(attention.IsAttentionVisible, Is.False);
                now += 6; attention.Update(true, false);
                Assert.That(attention.IsAttentionVisible, Is.True);
            }
        }

        [Test]
        public void EndingAPressWhileIneligibleDoesNotStartEligibleTime()
        {
            attention.Update(true, true);
            attention.Update(false, true);
            now += 20;
            attention.Update(false, false);
            now += 20;
            attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.False);
            now += 6; attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
        }

        [Test]
        public void FractionalResetTimeStillActivatesAtExactlySixSeconds()
        {
            attention.Update(true, false);
            now = 252.001;
            attention.Reset();
            now += 6;
            attention.Update(true, false);
            Assert.That(attention.IsAttentionVisible, Is.True);
        }

        [Test]
        public void ClockIsRequired() => Assert.Throws<ArgumentNullException>(() => new HintIdleAttention(null));
    }
}
