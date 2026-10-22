using System;
using System.Collections;
using System.IO;
using System.Linq;
using CatsAndArrows.UI;
using CatsAndArrows.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

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

    public sealed class AppleIapReviewSceneTests
    {
        private SceneSetup[] previous;
        private byte[] production;

        [SetUp] public void CaptureSceneState()
        {
            previous = null;
            Assert.That(File.Exists(AppleIapReviewBuild.ScenePath), Is.False, "Preserve an existing generated scene.");
            previous = EditorSceneManager.GetSceneManagerSetup();
            production = File.ReadAllBytes(ProductionIOSBuild.ScenePath);
        }
        [UnityTearDown] public IEnumerator RestoreSceneState()
        {
            if (previous == null) yield break;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(AppleIapReviewBuild.ScenePath);
            if (previous.Any(scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            Assert.That(File.ReadAllBytes(ProductionIOSBuild.ScenePath), Is.EqualTo(production));
            // Let destroyed runtime panels drain before unrelated mounted-UI tests.
            yield return null;
            yield return null;
        }
        [Test] public void SavedSceneRetainsProductionPanelThemeAndReviewComposition()
        {
            AppleIapReviewBuild.PrepareReviewScene(AppleIapReviewBuild.ApprovedProductId);
            // Force another unload/reload: an in-memory reference is insufficient for a player export.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var saved = EditorSceneManager.OpenScene(AppleIapReviewBuild.ScenePath, OpenSceneMode.Single);
            var document = saved.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).Single(doc => doc != null);
            Assert.That(AssetDatabase.GetAssetPath(document.panelSettings), Is.EqualTo("Assets/CatsAndArrows/UI/BoardReviewPanel.asset"));
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            Assert.That(new SerializedObject(document).FindProperty("m_PanelSettings").objectReferenceValue, Is.EqualTo(document.panelSettings));
            var bootstrap = document.GetComponent<AppleIapReviewBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(new SerializedObject(bootstrap).FindProperty("appleProductId").stringValue, Is.EqualTo(AppleIapReviewBuild.ApprovedProductId));
            Assert.That(saved.GetRootGameObjects().Any(go => go.GetComponent<PawsBootstrap>() != null), Is.False);
            Assert.That(saved.GetRootGameObjects().Any(go => go.GetComponent<Camera>()?.enabled == true), Is.True);
        }
        [Test] public void SavedSceneWithoutPanelCannotPassExportGate()
        {
            AppleIapReviewBuild.PrepareReviewScene(AppleIapReviewBuild.ApprovedProductId);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var document = scene.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).Single(doc => doc != null);
            var serialized = new SerializedObject(document);
            serialized.FindProperty("m_PanelSettings").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(document);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            Assert.Throws<InvalidOperationException>(AppleIapReviewBuild.ValidateSavedScene);
        }
    }
}
