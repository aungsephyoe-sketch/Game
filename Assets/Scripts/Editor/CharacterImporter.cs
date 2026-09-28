using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HashiraChronicles.EditorTools
{
    /// <summary>
    /// Turns rigged characters + animation files into game-ready prefabs.
    ///
    /// Folder convention (one folder per character/enemy id from GameDatabase):
    ///   Assets/Art/Characters/ren_initiate/Ren.fbx              ← rigged model (no '@' in the name)
    ///   Assets/Art/Characters/ren_initiate/Ren@Idle.fbx         ← one clip per file, named after '@'
    ///   ... @Walk @Run @Sprint @Attack1..@Attack5 @Heavy @DashAttack @Skill1..@Skill3 @Ultimate
    ///       @Dodge @Guard @Hit @Knockdown @GetUp @Victory @Defeat @Death
    ///   Assets/Art/Enemies/grunt/...                             ← same for demons
    ///
    /// Menu "Hashira Chronicles/Build Character Prefabs" sets every FBX to Humanoid, builds an Animator
    /// Controller (locomotion blend tree + trigger states that blend back to locomotion) and saves
    /// Assets/Resources/Characters/&lt;id&gt;.prefab, which CharacterVisual loads automatically at runtime.
    /// Missing clips are simply skipped.
    /// </summary>
    public static class CharacterImporter
    {
        static readonly string[] Triggers =
        {
            "Attack1", "Attack2", "Attack3", "Attack4", "Attack5", "Heavy", "DashAttack",
            "Skill1", "Skill2", "Skill3", "Ultimate", "Dodge", "Hit", "Knockdown", "GetUp", "Victory", "Defeat", "Death"
        };

        static readonly HashSet<string> Loops = new HashSet<string> { "Idle", "Walk", "Run", "Sprint", "Guard" };

        /// <summary>States that hold their final pose instead of returning to locomotion.</summary>
        static readonly HashSet<string> Holds = new HashSet<string> { "Knockdown", "Victory", "Defeat", "Death" };

        [MenuItem("Blade Legends/Build Character Prefabs")]
        public static void BuildAll()
        {
            int built = Build("Assets/Art/Characters", "Assets/Resources/Characters");
            built += Build("Assets/Art/Enemies", "Assets/Resources/Enemies");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hashira Chronicles] Built " + built + " character prefab(s). Press Play to see them in game.");
        }

        static int Build(string srcRoot, string dstRoot)
        {
            if (!Directory.Exists(srcRoot)) return 0;
            int count = 0;
            foreach (var dir in Directory.GetDirectories(srcRoot))
            {
                string folder = dir.Replace('\\', '/');
                string id = Path.GetFileName(folder);
                string modelPath = null;
                var clipFiles = new Dictionary<string, string>();
                foreach (var f in Directory.GetFiles(folder, "*.fbx"))
                {
                    string p = f.Replace('\\', '/');
                    string name = Path.GetFileNameWithoutExtension(p);
                    int at = name.IndexOf('@');
                    if (at >= 0) clipFiles[name.Substring(at + 1)] = p;
                    else if (modelPath == null) modelPath = p;
                }
                if (modelPath == null)
                {
                    Debug.LogWarning("[Hashira Chronicles] " + folder + " has no model FBX (a file without '@' in its name).");
                    continue;
                }

                ConfigureImporter(modelPath, false);
                foreach (var kv in clipFiles) ConfigureImporter(kv.Value, Loops.Contains(kv.Key));

                var clips = new Dictionary<string, AnimationClip>();
                foreach (var kv in clipFiles)
                {
                    var clip = LoadClip(kv.Value);
                    if (clip != null) clips[kv.Key] = clip;
                }

                var controller = BuildController(folder + "/" + id + ".controller", clips);
                SavePrefab(modelPath, controller, dstRoot, id);
                count++;
            }
            return count;
        }

        static void ConfigureImporter(string path, bool loop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var defaults = importer.defaultClipAnimations;
            if (defaults != null && defaults.Length > 0)
            {
                foreach (var c in defaults)
                {
                    c.loopTime = loop;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                }
                importer.clipAnimations = defaults;
            }
            importer.SaveAndReimport();
        }

        static AnimationClip LoadClip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = asset as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
            }
            return null;
        }

        static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Guard", AnimatorControllerParameterType.Bool);
            foreach (var t in Triggers)
                if (clips.ContainsKey(t)) controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;
            BlendTree tree;
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AddChild(tree, clips, "Idle", 0f);
            AddChild(tree, clips, "Walk", 0.35f);
            AddChild(tree, clips, "Run", 0.7f);
            AddChild(tree, clips, "Sprint", 1f);
            sm.defaultState = locomotion;

            foreach (var t in Triggers)
            {
                AnimationClip clip;
                if (!clips.TryGetValue(t, out clip)) continue;
                var state = sm.AddState(t);
                state.motion = clip;
                var any = sm.AddAnyStateTransition(state);
                any.AddCondition(AnimatorConditionMode.If, 0f, t);
                any.hasExitTime = false;
                any.duration = t.StartsWith("Attack") ? 0.05f : 0.08f;
                any.canTransitionToSelf = t.StartsWith("Attack") || t == "Hit";
                if (!Holds.Contains(t))
                {
                    var back = state.AddTransition(locomotion);
                    back.hasExitTime = true;
                    back.exitTime = 0.88f;
                    back.duration = 0.12f;
                }
            }

            AnimationClip guardClip;
            if (clips.TryGetValue("Guard", out guardClip))
            {
                var guard = sm.AddState("Guard");
                guard.motion = guardClip;
                var into = sm.AddAnyStateTransition(guard);
                into.AddCondition(AnimatorConditionMode.If, 0f, "Guard");
                into.hasExitTime = false;
                into.duration = 0.08f;
                into.canTransitionToSelf = false;
                var outOf = guard.AddTransition(locomotion);
                outOf.AddCondition(AnimatorConditionMode.IfNot, 0f, "Guard");
                outOf.hasExitTime = false;
                outOf.duration = 0.1f;
            }
            return controller;
        }

        static void AddChild(BlendTree tree, Dictionary<string, AnimationClip> clips, string name, float threshold)
        {
            AnimationClip clip;
            if (clips.TryGetValue(name, out clip)) tree.AddChild(clip, threshold);
        }

        static void SavePrefab(string modelPath, AnimatorController controller, string dstRoot, string id)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null) return;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            var animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                var avatar = asset as Avatar;
                if (avatar != null) { animator.avatar = avatar; break; }
            }
            Directory.CreateDirectory(dstRoot);
            PrefabUtility.SaveAsPrefabAsset(inst, dstRoot + "/" + id + ".prefab");
            Object.DestroyImmediate(inst);
        }
    }
}
