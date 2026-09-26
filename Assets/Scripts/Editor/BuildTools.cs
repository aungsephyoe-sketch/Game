using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HashiraChronicles.EditorTools
{
    /// <summary>
    /// Command-line builds, e.g.
    ///   Unity -batchmode -quit -projectPath . -executeMethod HashiraChronicles.EditorTools.BuildTools.BuildDesktop -logFile build.log
    ///   Unity -batchmode -quit -projectPath . -executeMethod HashiraChronicles.EditorTools.BuildTools.BuildAndroid -logFile build.log
    /// </summary>
    public static class BuildTools
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Hashira Chronicles/Build Desktop")]
        public static void BuildDesktop()
        {
            ProjectSetup.RunFromMenu();
            BuildTarget target;
            string path;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    target = BuildTarget.StandaloneOSX;
                    path = "Builds/Desktop/HashiraChronicles.app";
                    break;
                case RuntimePlatform.WindowsEditor:
                    target = BuildTarget.StandaloneWindows64;
                    path = "Builds/Desktop/HashiraChronicles.exe";
                    break;
                default:
                    target = BuildTarget.StandaloneLinux64;
                    path = "Builds/Desktop/HashiraChronicles.x86_64";
                    break;
            }
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            Build(target, path);
        }

        [MenuItem("Hashira Chronicles/Build Android APK")]
        public static void BuildAndroid()
        {
            ProjectSetup.RunFromMenu();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.aungsephyoe.hashirachronicles");
            Build(BuildTarget.Android, "Builds/Android/HashiraChronicles.apk");
        }

        static void Build(BuildTarget target, string path)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Hashira Chronicles] Build succeeded: " + path);
            }
            else
            {
                Debug.LogError("[Hashira Chronicles] Build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
