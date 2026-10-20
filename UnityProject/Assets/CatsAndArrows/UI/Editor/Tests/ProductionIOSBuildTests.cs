using System;
using System.IO;
using CatsAndArrows.UI;
using CatsAndArrows.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

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
                    Assert.That(root.GetComponent<UIDocument>().panelSettings, Is.Not.Null);
                }
                Assert.That(count, Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
