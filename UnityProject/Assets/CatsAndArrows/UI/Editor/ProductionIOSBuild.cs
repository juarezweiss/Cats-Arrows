using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatsAndArrows.UI.Editor
{
    // Issue #63. PlayerSettings is the checked-in identity/version source of truth.
    public static class ProductionIOSBuild
    {
        public const string ScenePath = "Assets/CatsAndArrows/Scenes/Paws.unity";
        public const string ProductionDefine = "PAWS_PRODUCTION";
        public const string BundleIdentifier = "com.jweiss.paws";

        public static void ValidateSettings()
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != BundleIdentifier ||
                PlayerSettings.productName != "Paws")
                throw new InvalidOperationException("Production identity must be Paws / " + BundleIdentifier);
            if (!Regex.IsMatch(PlayerSettings.bundleVersion, @"^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$") ||
                !int.TryParse(PlayerSettings.iOS.buildNumber, out var build) || build <= 0 ||
                build.ToString() != PlayerSettings.iOS.buildNumber)
                throw new InvalidOperationException("Use an approved 0.x.y version and a positive integer iOS build number.");
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

        public static BuildPlayerOptions CreateOptions(string output)
        {
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) || Directory.Exists(output) || File.Exists(output))
                throw new ArgumentException("PAWS_IOS_EXPORT must name a fresh absolute output directory.");
            return new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.iOS, targetGroup = BuildTargetGroup.iOS,
                locationPathName = output, options = BuildOptions.None,
                extraScriptingDefines = new[] { ProductionDefine }
            };
        }

        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use scripts/build-ios.sh in batch mode.");
            ValidateSettings();
            var options = CreateOptions(Environment.GetEnvironmentVariable("PAWS_IOS_EXPORT"));
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
                EditorUserBuildSettings.iOSXcodeBuildConfig = XcodeBuildConfig.Release;
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Production iOS export failed: " + report.summary.result);
                File.WriteAllText(Path.Combine(options.locationPathName, "paws-export.json"), JsonUtility.ToJson(new ExportReceipt
                {
                    sourceSha = revision, unityVersion = Application.unityVersion,
                    bundleIdentifier = BundleIdentifier, version = PlayerSettings.bundleVersion,
                    buildNumber = PlayerSettings.iOS.buildNumber, minimumIOS = PlayerSettings.iOS.targetOSVersionString,
                    deviceFamily = new[] { 1 }, productionDefine = ProductionDefine
                }, true));
            }
            finally { EditorUserBuildSettings.iOSXcodeBuildConfig = previous; }
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
            public bool monetizationEnabled = false, developmentBuild = false, scriptDebugging = false, profilerEnabled = false;
        }
    }
}
