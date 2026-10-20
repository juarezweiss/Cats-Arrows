using System;
using System.IO;
using CatsAndArrows.UI.Editor;
using NUnit.Framework;
using UnityEditor;

namespace CatsAndArrows.Purchasing.Apple.Review.Editor.Tests
{
    public sealed class AppleIapReviewBuildTests
    {
        [Test] public void ReviewOptionsUseSeparateSceneAndExplicitDevelopmentDefine()
        {
            var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var review = AppleIapReviewBuild.CreateOptions(output, AppleIapReviewBuild.ApprovedProductId);
            Assert.That(review.scenes, Is.EqualTo(new[] { "Assets/AppleIapReview.generated.unity" }));
            Assert.That(review.options, Is.EqualTo(BuildOptions.Development));
            Assert.That(review.extraScriptingDefines, Does.Contain("PAWS_APPLE_IAP_REVIEW"));
            var production = ProductionIOSBuild.CreateOptions(output);
            Assert.That(production.scenes, Is.EqualTo(new[] { ProductionIOSBuild.ScenePath }));
            Assert.That(production.extraScriptingDefines, Does.Not.Contain("PAWS_APPLE_IAP_REVIEW"));
        }
        [TestCase(null)] [TestCase("")] [TestCase("invented.product")]
        public void UnapprovedProductCannotBeBuilt(string product) => Assert.Throws<ArgumentException>(() =>
            AppleIapReviewBuild.CreateOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), product));
    }
}
