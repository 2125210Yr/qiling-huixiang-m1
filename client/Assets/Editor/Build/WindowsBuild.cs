using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Resonance.EditorTools
{
    public static class WindowsBuild
    {
        const string ExePath = "Builds/Win64/Resonance.exe";
        const string ResultRel = "Temp/win.build.result.txt";

        [MenuItem("Resonance/Build Windows Player")]
        public static void Build()
        {
            var log = new System.Text.StringBuilder();
            try
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new System.Exception("SwitchActiveBuildTarget Win64 failed");
                ApplyWindowedPhoneSettings();
                Directory.CreateDirectory("Builds/Win64");
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Boot.unity" },
                    locationPathName = ExePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.CompressWithLz4
                };
                var report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.Exception("Windows build failed: " + report.summary.result);
                var full = Path.GetFullPath(ExePath);
                log.AppendLine("PASS");
                log.AppendLine("exe=" + full);
                log.AppendLine("size=" + new FileInfo(full).Length);
                Debug.Log("WIN -> " + full);
            }
            catch (System.Exception e)
            {
                log.AppendLine("FAIL " + e.Message);
                Debug.LogError(e);
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ResultRel, log.ToString());
            if (log.ToString().StartsWith("FAIL"))
                throw new System.Exception(log.ToString());
        }

        static void ApplyWindowedPhoneSettings()
        {
            PlayerSettings.productName = "契灵回响";
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        public static void BuildAndExit()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
