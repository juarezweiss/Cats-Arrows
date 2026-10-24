using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Player;
using UnityEngine;
using CatsAndArrows.Production;

namespace CatsAndArrows.UI.Editor
{
    // Issue #63. PlayerSettings is the checked-in identity/version source of truth.
    public static class ProductionIOSBuild
    {
        public const string ScenePath = "Assets/CatsAndArrows/Scenes/Paws.unity";
        public const string ProductionDefine = "PAWS_PRODUCTION";
        public const string ConfigAssetPath = "Assets/Resources/PawsMonetization.generated.json";
        public const string BundleIdentifier = "com.jweiss.paws";

        public static void ValidateSettings()
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != BundleIdentifier ||
                PlayerSettings.productName != "Paws")
                throw new InvalidOperationException("Production identity must be Paws / " + BundleIdentifier);
            if (!Regex.IsMatch(PlayerSettings.bundleVersion, @"^(0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)|1\.0\.0)$") ||
                !int.TryParse(PlayerSettings.iOS.buildNumber, out var build) || build <= 0 ||
                build.ToString() != PlayerSettings.iOS.buildNumber)
                throw new InvalidOperationException("Use an approved 0.x.y or 1.0.0 version and a positive integer iOS build number.");
            if (PlayerSettings.iOS.targetDevice != iOSTargetDevice.iPhoneOnly ||
                PlayerSettings.iOS.targetOSVersionString != "16.3" ||
                PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                throw new InvalidOperationException("Approved support is iPhone only, minimum iOS 16.3, upright portrait.");
            if (PlayerSettings.iOS.sdkVersion != iOSSdkVersion.DeviceSDK ||
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS) != ScriptingImplementation.IL2CPP ||
                PlayerSettings.GetArchitecture(NamedBuildTarget.iOS) != 1 ||
                PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.iOS) != Il2CppCompilerConfiguration.Release)
                throw new InvalidOperationException("Production requires device SDK, IL2CPP and ARM64.");
            if (!string.IsNullOrWhiteSpace(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)))
                throw new InvalidOperationException("Production rejects inherited iOS scripting defines. Use the explicit entry point.");
            if (EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging ||
                EditorUserBuildSettings.connectProfiler || EditorUserBuildSettings.buildScriptsOnly)
                throw new InvalidOperationException("Disable development, script debugging, profiler and scripts-only build settings.");
            if (!string.IsNullOrWhiteSpace(PlayerSettings.iOS.appleDeveloperTeamID) ||
                !string.IsNullOrWhiteSpace(PlayerSettings.iOS.iOSManualProvisioningProfileID) ||
                PlayerSettings.iOS.appleEnableAutomaticSigning)
                throw new InvalidOperationException("Signing must remain local to Xcode; remove signing inputs from Unity settings.");
        }

        public static BuildPlayerOptions CreateOptions(string output, string profile = "disabled")
        {
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) || Directory.Exists(output) || File.Exists(output))
                throw new ArgumentException("PAWS_IOS_EXPORT must name a fresh absolute output directory.");
            if (profile != "production" && profile != "validation" && profile != "disabled")
                throw new ArgumentException("Invalid monetization profile.");
            return new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.iOS, targetGroup = BuildTargetGroup.iOS,
                locationPathName = output, options = BuildOptions.None,
                extraScriptingDefines = profile == "disabled" ? new[] { ProductionDefine } :
                    profile == "validation" ? new[] { ProductionDefine, "PAWS_MONETIZATION", "PAWS_MONETIZATION_VALIDATION" } :
                    new[] { ProductionDefine, "PAWS_MONETIZATION" }
            };
        }

        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use scripts/build-ios.sh in batch mode.");
            ValidateSettings();
            var config = ReadConfiguration(Environment.GetEnvironmentVariable("PAWS_MONETIZATION_PROFILE") ?? "production",
                Environment.GetEnvironmentVariable("PAWS_MONETIZATION_CONFIG"));
            var options = CreateOptions(Environment.GetEnvironmentVariable("PAWS_IOS_EXPORT"), config.profile);
            if (File.Exists(ConfigAssetPath)) throw new InvalidOperationException("Generated monetization config already exists.");
            var project = Directory.GetParent(Application.dataPath).FullName;
            var repository = Directory.GetParent(project).FullName;
            // Documentation-only owner edits do not change compiled inputs; never ignore dirty player/tool source.
            var dirty = Git(repository, "status --porcelain --untracked-files=all -- UnityProject scripts");
            if (!string.IsNullOrWhiteSpace(dirty)) throw new InvalidOperationException("Commit all UnityProject/scripts changes before a production export.");
            var revision = Git(repository, "rev-parse HEAD").Trim();
            var pin = File.ReadAllText(Path.Combine(project, "ProjectSettings/ProjectVersion.txt"));
            if (!pin.Contains("m_EditorVersion: " + Application.unityVersion + "\n"))
                throw new InvalidOperationException("Use the exact pinned Unity editor.");
            var previous = EditorUserBuildSettings.iOSXcodeBuildConfig;
            try
            {
                var runtimeJson = JsonUtility.ToJson(config, true);
                if (config.Enabled)
                {
                    File.WriteAllText(ConfigAssetPath, runtimeJson, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(ConfigAssetPath, ImportAssetOptions.ForceSynchronousImport);
                }
                EditorUserBuildSettings.iOSXcodeBuildConfig = XcodeBuildConfig.Release;
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Production iOS export failed: " + report.summary.result);
                if (config.Enabled) NativeAdMobBuild.Add(options.locationPathName, config.adMobAppId);
                if (config.Enabled)
                {
                    var redacted = JsonUtility.FromJson<ProductionMonetizationConfig>(runtimeJson);
                    redacted.umpTestDeviceIds = Array.Empty<string>();
                    File.WriteAllText(Path.Combine(options.locationPathName, "paws-monetization.json"), JsonUtility.ToJson(redacted, true));
                }
                File.WriteAllText(Path.Combine(options.locationPathName, "paws-export.json"), JsonUtility.ToJson(new ExportReceipt
                {
                    sourceSha = revision, unityVersion = Application.unityVersion,
                    bundleIdentifier = BundleIdentifier, version = PlayerSettings.bundleVersion,
                    buildNumber = PlayerSettings.iOS.buildNumber, minimumIOS = PlayerSettings.iOS.targetOSVersionString,
                    deviceFamily = new[] { 1 }, productionDefine = ProductionDefine,
                    profile = config.profile, monetizationEnabled = config.Enabled,
                    appleIapEnabled = config.Enabled, offerCodesEnabled = config.Enabled,
                    admobEnabled = config.Enabled, umpEnabled = config.Enabled, testAds = config.TestAds,
                    appleProductId = config.Enabled ? ProductionMonetizationConfig.ProductId : "",
                    adMobAppId = config.adMobAppId, interstitialAdUnitId = config.interstitialAdUnitId,
                    rewardedAdUnitId = config.rewardedAdUnitId,
                    consentDebugGeography = config.consentDebugGeography, testDeviceCount = config.umpTestDeviceIds.Length,
                    runtimeConfigurationSha256 = config.Enabled ? ConfigurationHash(runtimeJson) : "",
                    privacyPolicyUrl = config.privacyPolicyUrl, supportUrl = config.supportUrl, audiencePolicy = config.audiencePolicy
                }, true));
            }
            finally
            {
                EditorUserBuildSettings.iOSXcodeBuildConfig = previous;
                if (config.Enabled) AssetDatabase.DeleteAsset(ConfigAssetPath);
            }
        }

        public static ProductionMonetizationConfig ReadConfiguration(string profile, string localConfig)
        {
            var config = new ProductionMonetizationConfig { profile = profile };
            if (profile == "production")
            {
                config = ReadLocalConfiguration(localConfig, profile);
            }
            else if (profile == "validation")
            {
                if (!string.IsNullOrWhiteSpace(localConfig)) config = ReadLocalConfiguration(localConfig, profile);
                if (string.IsNullOrWhiteSpace(config.audiencePolicy)) config.audiencePolicy = ProductionMonetizationConfig.AudiencePolicy;
                if (string.IsNullOrWhiteSpace(config.adMobAppId)) config.adMobAppId = "ca-app-pub-3940256099942544~1458002511";
                config.interstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
                config.rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
            }
            config.Validate();
            return config;
        }

        // Compile the production-only source path without creating an installable app or supplying ad IDs.
        public static void CompileProductionScripts()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use explicit batch compilation.");
            var output = Environment.GetEnvironmentVariable("PAWS_PLAYER_SCRIPT_OUTPUT");
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) || Directory.Exists(output))
                throw new ArgumentException("PAWS_PLAYER_SCRIPT_OUTPUT must name a fresh absolute directory.");
            var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
            {
                target = BuildTarget.iOS, group = BuildTargetGroup.iOS, options = ScriptCompilationOptions.None,
                extraScriptingDefines = new[] { ProductionDefine, "PAWS_MONETIZATION" }
            }, output);
            if (result.assemblies == null || !System.Linq.Enumerable.Any(result.assemblies))
                throw new InvalidOperationException("Production player-script compilation returned no assemblies.");
        }

        private static ProductionMonetizationConfig ReadLocalConfiguration(string path, string profile)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !File.Exists(path))
                throw new ArgumentException("PAWS_MONETIZATION_CONFIG must name an existing absolute owner-local JSON file.");
            var config = JsonUtility.FromJson<ProductionMonetizationConfig>(File.ReadAllText(path));
            if (config == null) throw new ArgumentException("Invalid monetization configuration.");
            config.profile = profile;
            return config;
        }
        public static string ConfigurationHash(string json)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
        }

        private static string Git(string repository, string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = repository, UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true
            }))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Git source provenance check failed.");
                return output;
            }
        }

        [Serializable]
        private sealed class ExportReceipt
        {
            public string sourceSha, unityVersion, bundleIdentifier, version, buildNumber, minimumIOS, productionDefine;
            public int[] deviceFamily;
            public string profile, appleProductId, adMobAppId, interstitialAdUnitId, rewardedAdUnitId, runtimeConfigurationSha256;
            public string audiencePolicy, privacyPolicyUrl, supportUrl;
            public string consentDebugGeography = "Disabled";
            public int testDeviceCount = 0;
            public bool appleIapEnabled, offerCodesEnabled, admobEnabled, umpEnabled, testAds;
            public bool simulatedPurchases = false;
            public bool monetizationEnabled = false, developmentBuild = false, scriptDebugging = false, profilerEnabled = false;
        }
    }
}
