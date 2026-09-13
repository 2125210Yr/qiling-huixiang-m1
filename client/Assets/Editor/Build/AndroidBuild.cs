using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Resonance.EditorTools
{
    [InitializeOnLoad]
    public static class AndroidBuild
    {
        const string ApkPath = "Builds/Resonance.apk";
        const string RequestRel = "Temp/apk.build.request";
        const string ResultRel = "Temp/apk.build.result.txt";
        static double _nextTry;

        static AndroidBuild()
        {
            EditorApplication.update += Poll;
        }

        [MenuItem("Resonance/Build Android APK")]
        public static void Build()
        {
            var log = new System.Text.StringBuilder();
            try
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new System.Exception("SwitchActiveBuildTarget Android failed");
                ApplyPlayerSettings();
                PrepareGradleEnvironment();
                Directory.CreateDirectory("Builds");
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Boot.unity" },
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.CompressWithLz4
                };
                var report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.Exception("Android build failed: " + report.summary.result);
                var full = Path.GetFullPath(ApkPath);
                log.AppendLine("PASS");
                log.AppendLine("apk=" + full);
                log.AppendLine("size=" + new FileInfo(full).Length);
                Debug.Log("APK -> " + full);
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

        static void Poll()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.timeSinceStartup < _nextTry) return;
            if (!File.Exists(RequestRel)) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorUtility.scriptCompilationFailed) return;
            _nextTry = EditorApplication.timeSinceStartup + 20;
            File.Delete(RequestRel);
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            try { Build(); }
            catch (System.Exception e) { Debug.LogError(e); }
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Resonance";
            PlayerSettings.companyName = "Resonance";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.resonance.verticalslice");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            SetInputHandlerOld();
            EnableCustomGradleSettingsTemplate();
        }

        static void SetInputHandlerOld()
        {
            var asset = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            if (asset == null) return;
            var so = new SerializedObject(asset);
            var p = so.FindProperty("activeInputHandler");
            if (p == null) return;
            p.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnableCustomGradleSettingsTemplate()
        {
            var asset = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            if (asset == null) return;
            var so = new SerializedObject(asset);
            var p = so.FindProperty("useCustomGradleSettingsTemplate");
            if (p == null) return;
            p.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void PrepareGradleEnvironment()
        {
            const string gradleHome = @"F:\g";
            Directory.CreateDirectory(gradleHome);
            Directory.CreateDirectory(Path.Combine(gradleHome, "init.d"));
            System.Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHome, System.EnvironmentVariableTarget.Process);
            foreach (var key in new[]
            {
                "HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy",
                "ALL_PROXY", "all_proxy", "SOCKS_PROXY", "socks_proxy",
                "FTP_PROXY", "ftp_proxy"
            })
                System.Environment.SetEnvironmentVariable(key, null, System.EnvironmentVariableTarget.Process);

            var gradleOpts = System.Environment.GetEnvironmentVariable("GRADLE_OPTS") ?? "";
            if (gradleOpts.IndexOf("java.net.useSystemProxies", System.StringComparison.Ordinal) < 0)
                gradleOpts = (gradleOpts + " -Djava.net.useSystemProxies=false").Trim();
            System.Environment.SetEnvironmentVariable("GRADLE_OPTS", gradleOpts, System.EnvironmentVariableTarget.Process);
        }
    }
}

