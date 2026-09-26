using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HashiraChronicles.EditorTools
{
    /// <summary>
    /// One-time project setup on first open: creates Assets/Scenes/Main.unity (empty – the game bootstraps
    /// itself from code), adds it to Build Settings and applies mobile landscape player settings.
    /// Re-run any time via the "Hashira Chronicles" menu.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string DoneKey = "HashiraChronicles.SetupDone";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath) || !SessionState.GetBool(DoneKey, false)) Run(false);
            };
        }

        [MenuItem("Hashira Chronicles/Setup Main Scene and Build Settings")]
        public static void RunFromMenu()
        {
            Run(true);
        }

        [MenuItem("Hashira Chronicles/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        static void Run(bool verbose)
        {
            SessionState.SetBool(DoneKey, true);

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var active = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path))
                {
                    // Fresh project: replace the untitled scene with our (empty) main scene.
                    if (active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                else
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                    EditorSceneManager.CloseScene(scene, true);
                }
                AssetDatabase.Refresh();
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (PlayerSettings.productName == "Game" || string.IsNullOrEmpty(PlayerSettings.productName))
                PlayerSettings.productName = "Hashira Chronicles";

            if (verbose) Debug.Log("[Hashira Chronicles] Setup complete. Open " + ScenePath + " and press Play.");
        }
    }
}
