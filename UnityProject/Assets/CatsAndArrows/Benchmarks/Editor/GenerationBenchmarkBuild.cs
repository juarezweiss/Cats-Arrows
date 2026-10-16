using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatsAndArrows.Benchmarks.Editor
{
    // Issue #4: opt-in diagnostic build, never changes production scenes or build settings.
    public static class GenerationBenchmarkBuild
    {
        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this diagnostic build in batch mode.");
            var output = Environment.GetEnvironmentVariable("CATS_ARROWS_BENCHMARK_OUTPUT");
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                throw new ArgumentException("CATS_ARROWS_BENCHMARK_OUTPUT must be an absolute build output path.");
            var requestedTarget = Environment.GetEnvironmentVariable("CATS_ARROWS_BENCHMARK_TARGET") ?? "StandaloneOSX";
            if (!Enum.TryParse(requestedTarget, out BuildTarget target) ||
                (target != BuildTarget.StandaloneOSX && target != BuildTarget.Android && target != BuildTarget.iOS))
                throw new ArgumentException("Benchmark target must be StandaloneOSX, Android, or iOS.");
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new InvalidOperationException("Pinned editor lacks build support for " + target + ". Install through Unity Hub separately.");
            var profile = Environment.GetEnvironmentVariable("CATS_ARROWS_BENCHMARK_PROFILE") ?? "easy";
            if (profile != "easy" && profile != "medium" && profile != "hard")
                throw new ArgumentException("Benchmark profile must be easy, medium or hard.");

            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scenePath = "Assets/__CatsArrowsBenchmark_" + Guid.NewGuid().ToString("N") + ".unity";
            var templateSettingsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "ProjectSettings", "SceneTemplateSettings.json");
            var templateSettings = File.Exists(templateSettingsPath) ? File.ReadAllBytes(templateSettingsPath) : null;
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var benchmark = new GameObject("Issue4GenerationBenchmark").AddComponent<GenerationBenchmark>();
                benchmark.SourceRevision = Environment.GetEnvironmentVariable("CATS_ARROWS_BENCHMARK_REVISION") ?? "unspecified";
                benchmark.ProfileId = profile;
                var camera = new GameObject("BenchmarkCamera").AddComponent<Camera>();
                camera.backgroundColor = new Color(.08f, .1f, .12f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Could not save temporary benchmark scene.");
                var development = Environment.GetEnvironmentVariable("CATS_ARROWS_BENCHMARK_DEVELOPMENT") == "1";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    target = target,
                    targetGroup = group,
                    locationPathName = output,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                    extraScriptingDefines = new[] { "CATS_ARROWS_BENCHMARK" }
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Benchmark build failed: " + report.summary.result);
                Debug.Log("GENERATION_BENCHMARK_BUILD " + output + " target=" + target + " development=" + development);
            }
            finally
            {
                try
                {
                    if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
                    else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                finally
                {
                    try { AssetDatabase.DeleteAsset(scenePath); }
                    finally
                    {
                        if (templateSettings != null) File.WriteAllBytes(templateSettingsPath, templateSettings);
                        else if (File.Exists(templateSettingsPath)) File.Delete(templateSettingsPath);
                    }
                }
            }
        }
    }
}
