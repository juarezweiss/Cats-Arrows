using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI.Editor
{
    public static class BoardReviewBuild
    {
        public const string ScenePath = "Assets/CatsAndArrows/Scenes/BoardReview.unity";
        private const string PanelPath = "Assets/CatsAndArrows/UI/BoardReviewPanel.asset";
        public static void PrepareReviewScene()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Prepare this review scene explicitly in batch mode.");
            var project = Directory.GetParent(Application.dataPath).FullName;
            var templatePath = Path.Combine(project, "ProjectSettings", "SceneTemplateSettings.json");
            var originalTemplate = File.Exists(templatePath) ? File.ReadAllBytes(templatePath) : null;
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/CatsAndArrows/Scenes")) AssetDatabase.CreateFolder("Assets/CatsAndArrows", "Scenes");
                var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
                if (panel == null)
                {
                    panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.name = "BoardReviewPanel";
                    panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    panel.referenceResolution = new Vector2Int(390, 844);
                    panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 0;
                    AssetDatabase.CreateAsset(panel, PanelPath);
                }
                AssetDatabase.SaveAssets();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Review Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = PortraitBoardView.Background;
                var host = new GameObject("Board Review");
                var document = host.AddComponent<UIDocument>();
                host.AddComponent<BoardReviewBootstrap>();
                // Scene replacement may unload ScriptableObject instances; reacquire after the scene and reset hooks.
                panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
                if (panel == null) throw new InvalidOperationException("The review panel asset could not be reloaded.");
                document.panelSettings = panel;
                // Persist the native UIDocument backing field explicitly after all component reset hooks.
                var serializedDocument = new SerializedObject(document);
                serializedDocument.FindProperty("m_PanelSettings").objectReferenceValue = panel;
                serializedDocument.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(document);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save board review scene.");
                AssetDatabase.SaveAssets();
                // Verify the actual saved scene, rather than trusting only its in-memory document.
                var savedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var mounted = false;
                foreach (var root in savedScene.GetRootGameObjects())
                {
                    var savedDocument = root.GetComponent<UIDocument>();
                    if (savedDocument != null && savedDocument.panelSettings != null) mounted = true;
                }
                if (!mounted) throw new InvalidOperationException("Saved review scene is missing its panel settings reference.");
                Debug.Log("BOARD_REVIEW_SCENE " + ScenePath);
            }
            finally
            {
                if (originalTemplate != null) File.WriteAllBytes(templatePath, originalTemplate);
                else if (File.Exists(templatePath)) File.Delete(templatePath);
            }
        }
        public static void Build()
        {
            var output = Environment.GetEnvironmentVariable("CATS_ARROWS_BOARD_REVIEW_OUTPUT");
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                throw new ArgumentException("CATS_ARROWS_BOARD_REVIEW_OUTPUT must be an absolute path.");
            var requestedTarget = Environment.GetEnvironmentVariable("CATS_ARROWS_BOARD_REVIEW_TARGET") ?? "StandaloneOSX";
            if (!Enum.TryParse(requestedTarget, out BuildTarget target) ||
                (target != BuildTarget.StandaloneOSX && target != BuildTarget.iOS && target != BuildTarget.Android))
                throw new ArgumentException("Review target must be StandaloneOSX, iOS or Android.");
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new InvalidOperationException("Pinned editor lacks build support for " + target);
            PrepareReviewScene();
            var defines = new List<string>();
            if (Environment.GetEnvironmentVariable("CATS_ARROWS_GAME_LOOP_AUTORUN") == "1") defines.Add("CATS_ARROWS_GAME_LOOP_CHECK");
            if (Environment.GetEnvironmentVariable("CATS_ARROWS_HINT_FLOW") == "legacy") defines.Add("CATS_ARROWS_LEGACY_HINTS");
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            try
            {
                // Review builds must never replace the production app/sandbox (#63).
                if (target == BuildTarget.iOS)
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.juarezweiss.catsarrows.inputreview");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, target = target, targetGroup = group,
                    locationPathName = output,
                    options = Environment.GetEnvironmentVariable("CATS_ARROWS_MONETIZATION_REVIEW") == "1"
                        ? BuildOptions.Development : BuildOptions.None,
                    // Per-build diagnostic opt-in; never persist this in PlayerSettings.
                    extraScriptingDefines = defines.ToArray()
                });
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Board review build failed: " + report.summary.result);
                Debug.Log("BOARD_REVIEW_BUILD " + output);
            }
            finally
            {
                if (target == BuildTarget.iOS)
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, previousIdentifier);
            }
        }
    }
}
