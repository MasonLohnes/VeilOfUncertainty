// =============================================================================
// SceneSetupEditor.cs — Editor Utility for Veil of Uncertainty
// Provides menu items to create the MainMenu scene and configure Build Settings.
// Auto-configures build settings on editor load via [InitializeOnLoad].
// Only runs in the Unity Editor, not in builds.
// =============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace VeilOfUncertainty.Editor
{
    /// <summary>
    /// Editor utility to set up scenes and build settings for the project.
    /// Auto-runs ConfigureBuildSettings on editor load to keep scenes in sync.
    /// </summary>
    [InitializeOnLoad]
    public class SceneSetupEditor
    {
        static SceneSetupEditor()
        {
            EditorApplication.delayCall += AutoConfigure;
        }

        static void AutoConfigure()
        {
            // Auto-configure build settings if scenes exist
            ConfigureBuildSettings();
        }

        [MenuItem("Veil of Uncertainty/Setup All Scenes")]
        public static void SetupAllScenes()
        {
            EnsureScenesDirectory();
            CreateMainMenuScene();
            SetupMainScene();
            ConfigureBuildSettings();
            Debug.Log("[VoU] All scenes set up and build settings configured.");
        }

        [MenuItem("Veil of Uncertainty/Create MainMenu Scene")]
        public static void CreateMainMenuScene()
        {
            EnsureScenesDirectory();

            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<MainMenuBootstrap>();
            camObj.AddComponent<MainMenuManager>();

            // Save scene
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log("[VoU] MainMenu scene created at " + scenePath);
        }

        [MenuItem("Veil of Uncertainty/Setup Main Game Scene")]
        public static void SetupMainScene()
        {
            // Open the main scene
            string mainScenePath = "Assets/main.unity";
            var scene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

            // Check if SceneBuilder already exists
            var existing = Object.FindFirstObjectByType<SceneBuilder>();
            if (existing == null)
            {
                // Create SceneBuilder GameObject
                GameObject builderObj = new GameObject("SceneBuilder");
                builderObj.AddComponent<SceneBuilder>();
            }

            // Save the scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[VoU] Main game scene set up with SceneBuilder.");
        }

        [MenuItem("Veil of Uncertainty/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // MainMenu as scene 0
            string menuPath = "Assets/Scenes/MainMenu.unity";
            if (File.Exists(Path.Combine(Application.dataPath, "..", menuPath)))
            {
                scenes.Add(new EditorBuildSettingsScene(menuPath, true));
            }

            // Main game scene as scene 1
            string mainPath = "Assets/main.unity";
            if (File.Exists(Path.Combine(Application.dataPath, "..", mainPath)))
            {
                scenes.Add(new EditorBuildSettingsScene(mainPath, true));
            }

            if (scenes.Count > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void EnsureScenesDirectory()
        {
            string scenesDir = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
