using System;
using System.IO;
using System.Linq;
using CatsAndArrows.Advertising.Google;
using CatsAndArrows.UI;
using CatsAndArrows.UI.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Advertising.Review.Editor
{
    public static class AdMobReviewBuild
    {
        public const string ScenePath = "Assets/AdMobReview.generated.unity";
        public static BuildPlayerOptions CreateOptions(string output)
        {
            var options = ProductionIOSBuild.CreateOptions(output);
            options.scenes = new[] { ScenePath };
            options.options = BuildOptions.Development;
            options.extraScriptingDefines = new[] { "PAWS_ADMOB_REVIEW" };
            return options;
        }
        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the explicit batch export.");
            ProductionIOSBuild.ValidateSettings();
            var output = Environment.GetEnvironmentVariable("PAWS_IOS_EXPORT");
            var options = CreateOptions(output);
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Generated review scene already exists.");
            var previous = EditorUserBuildSettings.iOSXcodeBuildConfig;
            var previousDevelopment = EditorUserBuildSettings.development;
            try
            {
                PrepareScene();
                EditorUserBuildSettings.iOSXcodeBuildConfig = XcodeBuildConfig.Release;
                if (BuildPipeline.BuildPlayer(options).summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("AdMob review export failed.");
                AddNativeDependencies(output);
                File.WriteAllText(Path.Combine(output, "paws-admob-review.json"), JsonUtility.ToJson(new Receipt
                {
                    sourceSha = Environment.GetEnvironmentVariable("PAWS_ADMOB_SOURCE_SHA"), unityVersion = Application.unityVersion,
                    geography = Environment.GetEnvironmentVariable("PAWS_UMP_GEOGRAPHY") ?? "EEA",
                    testDeviceCount = ReadTestDevices().Length
                }, true));
            }
            finally
            {
                EditorUserBuildSettings.iOSXcodeBuildConfig = previous;
                EditorUserBuildSettings.development = previousDevelopment;
                AssetDatabase.DeleteAsset(ScenePath);
            }
        }
        public static int ReadGeography(string value)
        {
            switch (value ?? "EEA")
            {
                case "EEA": return 1;
                case "RegulatedUSState": return 3;
                case "Other": return 4;
                default: throw new ArgumentException("PAWS_UMP_GEOGRAPHY must be EEA, RegulatedUSState or Other.");
            }
        }
        private static string[] ReadTestDevices() => (Environment.GetEnvironmentVariable("PAWS_UMP_TEST_DEVICE_IDS") ?? "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        private static void PrepareScene()
        {
            string panelPath;
            var preview = EditorSceneManager.OpenPreviewScene(ProductionIOSBuild.ScenePath);
            try { panelPath = AssetDatabase.GetAssetPath(preview.GetRootGameObjects().Select(go => go.GetComponent<UIDocument>()).Single(doc => doc != null).panelSettings); }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Review Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = PortraitBoardView.Background;
            var host = new GameObject("Google test ads and simulated purchases");
            var document = host.AddComponent<UIDocument>();
            var bootstrap = new SerializedObject(host.AddComponent<AdMobReviewBootstrap>());
            bootstrap.FindProperty("geography").intValue = ReadGeography(Environment.GetEnvironmentVariable("PAWS_UMP_GEOGRAPHY"));
            var devices = ReadTestDevices(); var property = bootstrap.FindProperty("testDevices"); property.arraySize = devices.Length;
            for (var i = 0; i < devices.Length; i++) property.GetArrayElementAtIndex(i).stringValue = devices[i];
            bootstrap.ApplyModifiedPropertiesWithoutUndo();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null || panel.themeStyleSheet == null) throw new InvalidOperationException("Missing production panel/theme.");
            document.panelSettings = panel;
            var serialized = new SerializedObject(document); serialized.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(document);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save review scene.");
        }
        // Manual native dependencies are supported by Google. No EDM installer, package
        // registry mutation, CocoaPods, Android setup, or editor analytics are necessary.
        public static void AddNativeDependencies(string output)
        {
            var source = Path.GetFullPath(Path.Combine(Application.dataPath, "../ThirdParty~/GoogleMobileAds"));
            var folder = "Frameworks/PawsAdMob";
            var projectPath = PBXProject.GetPBXProjectPath(output);
            var project = new PBXProject(); project.ReadFromFile(projectPath);
            var framework = project.GetUnityFrameworkTargetGuid();
            foreach (var name in new[] { "unity-plugin-library", "GoogleMobileAds", "UserMessagingPlatform" })
            {
                var relative = folder + "/" + name + ".xcframework";
                CopyDirectory(Path.Combine(source, name + ".xcframework"), Path.Combine(output, relative));
                var guid = project.AddFile(relative, relative, PBXSourceTree.Source);
                project.AddFileToBuild(framework, guid);
            }
            foreach (var name in new[] { "AudioToolbox", "AVFoundation", "CFNetwork", "CoreGraphics", "CoreMedia", "CoreTelephony", "CoreVideo", "JavaScriptCore", "MediaPlayer", "MessageUI", "MobileCoreServices", "Network", "QuartzCore", "Security", "StoreKit", "SystemConfiguration", "AdSupport", "SafariServices", "WebKit" })
                project.AddFrameworkToProject(framework, name + ".framework", false);
            foreach (var library in new[] { "libz.tbd", "libsqlite3.tbd", "libc++.tbd" })
            { var guid = project.AddFile("usr/lib/" + library, "Frameworks/" + library, PBXSourceTree.Sdk); project.AddFileToBuild(framework, guid); }
            project.AddBuildProperty(framework, "OTHER_LDFLAGS", "-ObjC");
            foreach (var name in new[] { "GoogleMobileAds", "UserMessagingPlatform" })
            {
                var relative = folder + "/" + name + "Resources.bundle";
                Directory.CreateDirectory(Path.Combine(output, relative));
                File.Copy(Path.Combine(source, name + ".xcframework/ios-arm64/" + name + ".framework/PrivacyInfo.xcprivacy"), Path.Combine(output, relative, "PrivacyInfo.xcprivacy"), true);
                if (name == "GoogleMobileAds") CopyDirectory(Path.Combine(source, "Licenses"), Path.Combine(output, relative, "Licenses"));
                var info = new PlistDocument(); info.root.SetString("CFBundleIdentifier", "com.google." + name + ".resources");
                info.root.SetString("CFBundlePackageType", "BNDL"); info.root.SetString("CFBundleVersion", "1");
                info.WriteToFile(Path.Combine(output, relative, "Info.plist"));
                project.AddFileToBuild(framework, project.AddFile(relative, relative, PBXSourceTree.Source));
            }
            project.WriteToFile(projectPath);
            var plistPath = Path.Combine(output, "Info.plist"); var plist = new PlistDocument(); plist.ReadFromFile(plistPath);
            plist.root.SetString("GADApplicationIdentifier", GoogleAdsBridge.TestAppId);
            plist.root.SetString("GADUUnityVersion", Application.unityVersion);
            plist.root.SetBoolean("GADDisableAdInspector", true);
            plist.root.values.Remove("NSUserTrackingUsageDescription");
            var networks = plist.root.CreateArray("SKAdNetworkItems");
            networks.AddDict().SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");
            plist.WriteToFile(plistPath);
        }
        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var path in Directory.GetFiles(source)) File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);
            foreach (var path in Directory.GetDirectories(source)) CopyDirectory(path, Path.Combine(destination, Path.GetFileName(path)));
        }
        [Serializable] private sealed class Receipt
        {
            public string sourceSha, unityVersion, geography;
            public int testDeviceCount;
            public string purpose = "issue66-google-test-ads-development-review";
            public string plugin = "11.6.0", gmaIOS = "13.11.0", umpIOS = "3.1.0";
            public bool developmentBuild = true, productionMonetizationEnabled = false, simulatedPurchases = true;
            public bool attPrompt = false, personalizedAds = false, firstPartyId = false, sdkCrashReporting = false, edmInstalled = false;
        }
    }
}
