using System;
using System.IO;
using CatsAndArrows.UI;
using CatsAndArrows.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine;
using CatsAndArrows.Production;

namespace CatsAndArrows.Tests
{
    // Issue #63: validate actual committed settings and reject unsafe build inputs.
    public sealed class ProductionIOSBuildTests
    {
        [Test]
        public void CommittedProductionSettingsAreApprovedAndUnsigned()
        {
            ProductionIOSBuild.ValidateSettings();
            Assert.That(PlayerSettings.iOS.targetDevice, Is.EqualTo(iOSTargetDevice.iPhoneOnly));
            Assert.That(PlayerSettings.iOS.targetOSVersionString, Is.EqualTo("16.3"));
        }

        [Test]
        public void ProductionOptionsContainOnlyNormalGameAndNoDevelopmentFlags()
        {
            var options = ProductionIOSBuild.CreateOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            Assert.That(options.scenes, Is.EqualTo(new[] { ProductionIOSBuild.ScenePath }));
            Assert.That(options.target, Is.EqualTo(BuildTarget.iOS));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            Assert.That(options.extraScriptingDefines, Is.EqualTo(new[] { "PAWS_PRODUCTION" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("relative/export")]
        public void OutputMustBeAbsoluteAndFresh(string path) =>
            Assert.Throws<ArgumentException>(() => ProductionIOSBuild.CreateOptions(path));

        [Test]
        public void ExistingOutputCannotBeReused() =>
            Assert.Throws<ArgumentException>(() => ProductionIOSBuild.CreateOptions(Path.GetTempPath()));

        [TestCase("CATS_ARROWS_BENCHMARK")]
        [TestCase("CATS_ARROWS_GAME_LOOP_CHECK")]
        [TestCase("CATS_ARROWS_LEGACY_HINTS")]
        [TestCase("DEVELOPMENT_BUILD")]
        public void InheritedDefinesCannotEnableDiagnostics(string define)
        {
            var previous = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, define);
                Assert.Throws<InvalidOperationException>(ProductionIOSBuild.ValidateSettings);
            }
            finally { PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, previous); }
        }

        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("1.2")]
        [TestCase("01")]
        public void BuildNumbersArePositiveCanonicalIntegers(string build)
        {
            var previous = PlayerSettings.iOS.buildNumber;
            try
            {
                PlayerSettings.iOS.buildNumber = build;
                Assert.Throws<InvalidOperationException>(ProductionIOSBuild.ValidateSettings);
            }
            finally { PlayerSettings.iOS.buildNumber = previous; }
        }

        [TestCase("production", new[] { "PAWS_PRODUCTION", "PAWS_MONETIZATION" })]
        [TestCase("validation", new[] { "PAWS_PRODUCTION", "PAWS_MONETIZATION", "PAWS_MONETIZATION_VALIDATION" })]
        [TestCase("disabled", new[] { "PAWS_PRODUCTION" })]
        public void ProfilesUseExplicitNonDevelopmentCompilation(string profile, string[] defines)
        {
            var options = ProductionIOSBuild.CreateOptions(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), profile);
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            Assert.That(options.extraScriptingDefines, Is.EqualTo(defines));
        }

        [Test]
        public void ProductionRequiresLocalConfigurationAndRejectsTestIds()
        {
            Assert.Throws<ArgumentException>(() => ProductionIOSBuild.ReadConfiguration("production", null));
            var config = ProductionIOSBuild.ReadConfiguration("validation", null);
            config.profile = "production";
            Assert.Throws<ArgumentException>(config.Validate);
        }

        [Test]
        public void ConfigurationSeparatesPlacementsAndRejectsForeignPublisher()
        {
            var config = new ProductionMonetizationConfig
            {
                profile = "production", audiencePolicy = ProductionMonetizationConfig.AudiencePolicy,
                privacyPolicyUrl = "https://example.com/privacy", supportUrl = "https://example.com/support", adMobAppId = "ca-app-pub-1111111111111111~1111111111",
                interstitialAdUnitId = "ca-app-pub-1111111111111111/2222222222",
                rewardedAdUnitId = "ca-app-pub-1111111111111111/3333333333"
            };
            Assert.DoesNotThrow(config.Validate);
            config.rewardedAdUnitId = config.interstitialAdUnitId;
            Assert.Throws<ArgumentException>(config.Validate);
            config.rewardedAdUnitId = "ca-app-pub-2222222222222222/3333333333";
            Assert.Throws<ArgumentException>(config.Validate);
            config.profile = "disabled";
            Assert.Throws<ArgumentException>(config.Validate);
        }

        [Test]
        public void ValidationUsesRegisteredUmpAppAndForcesTestPlacements()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "{\"adMobAppId\":\"ca-app-pub-1111111111111111~1111111111\",\"interstitialAdUnitId\":\"ca-app-pub-1111111111111111/2222222222\",\"rewardedAdUnitId\":\"ca-app-pub-1111111111111111/3333333333\",\"consentDebugGeography\":\"EEA\",\"umpTestDeviceIds\":[\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"]}");
                var config = ProductionIOSBuild.ReadConfiguration("validation", path);
                Assert.That(config.adMobAppId, Is.EqualTo("ca-app-pub-1111111111111111~1111111111"));
                Assert.That(config.interstitialAdUnitId, Is.EqualTo("ca-app-pub-3940256099942544/4411468910"));
                Assert.That(config.rewardedAdUnitId, Is.EqualTo("ca-app-pub-3940256099942544/1712485313"));
                Assert.That(config.umpTestDeviceIds, Has.Length.EqualTo(1));
                Assert.That(config.consentDebugGeography, Is.EqualTo("EEA"));
                Assert.Throws<ArgumentException>(() => ProductionIOSBuild.ReadConfiguration("production", path));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void ValidationGeographyRequiresRegisteredDeviceAndValidPolicy()
        {
            var config = ProductionIOSBuild.ReadConfiguration("validation", null);
            config.consentDebugGeography = "EEA";
            Assert.Throws<ArgumentException>(config.Validate);
            config.umpTestDeviceIds = new[] { "not-a-test-device-hash" };
            Assert.Throws<ArgumentException>(config.Validate);
            config.umpTestDeviceIds = new[] { "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" };
            Assert.DoesNotThrow(config.Validate);
            config.consentDebugGeography = "unknown";
            Assert.Throws<ArgumentException>(config.Validate);
        }

        [Test]
        public void ProductionRequiresExplicitAudiencePolicyAndPublicHttpsLinks()
        {
            var config = new ProductionMonetizationConfig
            {
                profile = "production", adMobAppId = "ca-app-pub-1111111111111111~1111111111",
                interstitialAdUnitId = "ca-app-pub-1111111111111111/2222222222", rewardedAdUnitId = "ca-app-pub-1111111111111111/3333333333"
            };
            Assert.Throws<ArgumentException>(config.Validate);
            config.audiencePolicy = ProductionMonetizationConfig.AudiencePolicy;
            Assert.Throws<ArgumentException>(config.Validate);
            config.privacyPolicyUrl = "https://example.com/privacy"; config.supportUrl = "http://example.com/support";
            Assert.Throws<ArgumentException>(config.Validate);
            config.supportUrl = "https://example.com/support";
            Assert.DoesNotThrow(config.Validate);
            config.privacyPolicyUrl = "https://user:password@example.com/privacy";
            Assert.Throws<ArgumentException>(config.Validate);
        }

        [TestCase(null)]
        [TestCase("live")]
        [TestCase("Production")]
        public void UnknownProfileCannotSilentlyDisableMonetization(string profile) =>
            Assert.Throws<ArgumentException>(() => ProductionIOSBuild.ReadConfiguration(profile, null));

        [Test]
        public void DisabledRuntimeTouchesNoProviderOrLedgerAndExposesNoPrivacyService()
        {
            var host = new GameObject("disabled production composition");
            try
            {
                var runtime = host.AddComponent<ProductionMonetizationRuntime>();
                Assert.That(runtime.Initialize(), Is.Null);
                Assert.That(runtime.Privacy, Is.Null);
                Assert.DoesNotThrow(runtime.StartProviders);
                Assert.DoesNotThrow(runtime.Tick);
                Assert.DoesNotThrow(() => runtime.SetPaused(true));
                Assert.DoesNotThrow(runtime.Dispose);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void ProductionSceneMountsOnlyProductionBootstrap()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ProductionIOSBuild.ScenePath);
            try
            {
                var count = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    Assert.That(root.GetComponent<BoardReviewBootstrap>(), Is.Null);
                    if (root.GetComponent<PawsBootstrap>() == null) continue;
                    count++;
                    Assert.That(root.GetComponent<ProductionMonetizationRuntime>(), Is.Not.Null);
                    Assert.That(root.GetComponent<UIDocument>().panelSettings, Is.Not.Null);
                }
                Assert.That(count, Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
