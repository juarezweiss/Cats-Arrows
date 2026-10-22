using System;
using System.IO;
using System.Linq;
using CatsAndArrows.UI;
using CatsAndArrows.UI.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Purchasing.Apple.Review.Editor
{
    public static class AppleIapReviewBuild
    {
        public const string ApprovedProductId = "com.jweiss.paws.removeads"; // Owner approval, issue #64, 2026-10-20.
        public const string ScenePath = "Assets/AppleIapReview.generated.unity";

        public static UnityEditor.BuildPlayerOptions CreateOptions(string output, string product)
        {
            if (product != ApprovedProductId) throw new ArgumentException("Inject the owner-approved PAWS_APPLE_PRODUCT_ID.");
            var options = ProductionIOSBuild.CreateOptions(output);
            options.scenes = new[] { ScenePath };
            options.extraScriptingDefines = new[] { "PAWS_PRODUCTION", "PAWS_APPLE_IAP_REVIEW" };
            options.options = BuildOptions.Development;
            return options;
        }

        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a clean batch export.");
            ProductionIOSBuild.ValidateSettings();
            var output = Environment.GetEnvironmentVariable("PAWS_IOS_EXPORT");
            var product = Environment.GetEnvironmentVariable("PAWS_APPLE_PRODUCT_ID");
            var options = CreateOptions(output, product);
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Remove the stale generated review scene before rebuilding.");
            var previous = EditorUserBuildSettings.iOSXcodeBuildConfig;
            var previousDevelopment = EditorUserBuildSettings.development;
            try
            {
                PrepareReviewScene(product);
                EditorUserBuildSettings.iOSXcodeBuildConfig = XcodeBuildConfig.Release;
                if (BuildPipeline.BuildPlayer(options).summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Apple IAP review export failed.");
                File.WriteAllText(Path.Combine(output, "paws-iap-review.json"), JsonUtility.ToJson(new Receipt
                    { unityVersion = Application.unityVersion, productId = product,
                        sourceSha = Environment.GetEnvironmentVariable("PAWS_IAP_SOURCE_SHA") }, true));
            }
            finally
            {
                EditorUserBuildSettings.iOSXcodeBuildConfig = previous;
                EditorUserBuildSettings.development = previousDevelopment;
                AssetDatabase.DeleteAsset(ScenePath);
            }
        }

        public static void PrepareReviewScene(string product)
        {
            if (product != ApprovedProductId) throw new ArgumentException("Use the owner-approved product mapping.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Remove the stale generated review scene before rebuilding.");
            // Only an asset path survives preview/scene replacement reliably. Component Reset hooks
            // can also clear UIDocument's panel; persist it after all components have been added.
            string panelPath;
            var preview = EditorSceneManager.OpenPreviewScene(ProductionIOSBuild.ScenePath);
            try
            {
                var document = preview.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).Single(doc => doc != null);
                panelPath = AssetDatabase.GetAssetPath(document.panelSettings);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Review Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PortraitBoardView.Background;
            var root = new GameObject("Apple IAP development review");
            var mounted = root.AddComponent<UIDocument>();
            var bootstrap = new SerializedObject(root.AddComponent<AppleIapReviewBootstrap>());
            bootstrap.FindProperty("appleProductId").stringValue = product;
            bootstrap.ApplyModifiedPropertiesWithoutUndo();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null || panel.themeStyleSheet == null)
                throw new InvalidOperationException("Apple IAP review requires the production panel and runtime theme.");
            mounted.panelSettings = panel;
            var serialized = new SerializedObject(mounted);
            serialized.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mounted);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Apple IAP review scene.");
            ValidateSavedScene();
        }

        public static void ValidateSavedScene()
        {
            var saved = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var documents = saved.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).Where(doc => doc != null).ToArray();
            if (documents.Length != 1 || documents[0].panelSettings == null || documents[0].panelSettings.themeStyleSheet == null)
                throw new InvalidOperationException("Saved Apple IAP review scene is missing its panel or runtime theme.");
            if (!saved.GetRootGameObjects().Any(go => go.GetComponent<Camera>()?.enabled == true))
                throw new InvalidOperationException("Saved Apple IAP review scene is missing its camera.");
        }
        [Serializable] private sealed class Receipt
        {
            public string unityVersion, productId, sourceSha;
            public string purpose = "issue64-owner-StoreKit-sandbox-review";
            public string iapPackage = "5.3.1";
            public string purchaseEnvironment = "SandboxReview";
            public bool reviewMonetizationEnabled = true, adProviderEnabled = false;
            public bool productionMonetizationEnabled = false, developmentBuild = true;
        }
    }
}
