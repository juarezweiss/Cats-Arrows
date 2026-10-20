using System;
using System.IO;
using System.Linq;
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
        private const string Scene = "Assets/AppleIapReview.generated.unity";

        public static UnityEditor.BuildPlayerOptions CreateOptions(string output, string product)
        {
            if (product != ApprovedProductId) throw new ArgumentException("Inject the owner-approved PAWS_APPLE_PRODUCT_ID.");
            var options = ProductionIOSBuild.CreateOptions(output);
            options.scenes = new[] { Scene };
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
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Scene) != null)
                throw new InvalidOperationException("Remove the stale generated review scene before rebuilding.");
            PanelSettings panel;
            var preview = EditorSceneManager.OpenPreviewScene(ProductionIOSBuild.ScenePath);
            try { panel = preview.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).First(doc => doc != null).panelSettings; }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var previous = EditorUserBuildSettings.iOSXcodeBuildConfig;
            var previousDevelopment = EditorUserBuildSettings.development;
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Apple IAP development review");
                root.AddComponent<UIDocument>().panelSettings = panel;
                var bootstrap = new SerializedObject(root.AddComponent<AppleIapReviewBootstrap>());
                bootstrap.FindProperty("appleProductId").stringValue = product;
                bootstrap.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, Scene);
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
                AssetDatabase.DeleteAsset(Scene);
            }
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
