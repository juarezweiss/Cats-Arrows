using System;
using System.IO;
using CatsAndArrows.Advertising.Review.Editor;
using CatsAndArrows.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.iOS.Xcode;

namespace CatsAndArrows.Advertising.BuildTests
{
    public sealed class AdMobReviewBuildTests
    {
        [Test] public void ProductionOptionsDoNotEnableAdMobOrReviewScenes()
        {
            var options = ProductionIOSBuild.CreateOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            CollectionAssert.AreEqual(new[] { "PAWS_PRODUCTION" }, options.extraScriptingDefines);
            CollectionAssert.AreEqual(new[] { ProductionIOSBuild.ScenePath }, options.scenes);
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
        }
        [Test] public void ReviewOptionsAreExplicitDevelopmentWithOnlyAdMobOptIn()
        {
            var options = AdMobReviewBuild.CreateOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            CollectionAssert.AreEqual(new[] { "PAWS_ADMOB_REVIEW" }, options.extraScriptingDefines);
            Assert.That(options.options, Is.EqualTo(BuildOptions.Development));
            CollectionAssert.AreEqual(new[] { AdMobReviewBuild.ScenePath }, options.scenes);
        }
        [TestCase("EEA", 1)] [TestCase("RegulatedUSState", 3)] [TestCase("Other", 4)] [TestCase(null, 1)]
        public void OnlySupportedDebugGeographiesAreAccepted(string value, int expected) => Assert.That(AdMobReviewBuild.ReadGeography(value), Is.EqualTo(expected));
        [TestCase("Disabled")] [TestCase("live")] [TestCase("")]
        public void InvalidGeographyFailsBeforeExport(string value) => Assert.Throws<ArgumentException>(() => AdMobReviewBuild.ReadGeography(value));
    }
}
