using System;
using System.IO;
using System.Reflection;
using Resonance.App;
using Resonance.Battle;
using UnityEditor;
using UnityEngine;

namespace Resonance.EditorTools
{
    /// <summary>
    /// Editor/CLI tokens for natural-play. Request body is one token.
    /// Tokens: np.basic.v1 | np.fever.v1 | np.auto.v1 | np.matrix.v1
    /// Aliases: basic/n01, fever/n02, auto/n03, matrix/all/1.
    /// CLI: -executeMethod Resonance.EditorTools.NaturalPlaySmoke.Cli{Basic,Fever,Auto,Matrix,FromArgs}
    ///      and/or -natural-play [-natural-scenario &lt;token&gt;].
    /// No -batchmode / -nographics: Poll and TryEnterPlay return immediately in batch mode.
    /// Do not pass -quit; QuitIfDone exits after Temp/natural-play.result.txt exists.
    /// </summary>
    [InitializeOnLoad]
    public static class NaturalPlaySmoke
    {
        const string RequestRel = "Temp/natural-play.request";
        const string RunningRel = "Temp/natural-play.running";
        const string ResultRel = "Temp/natural-play.result.txt";
        const string QuitRel = "Temp/natural-play.quit";
        const string SmokeSaveRel = "Temp/natural-play-save/save.json";
        static double _nextTry;

        static NaturalPlaySmoke()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayMode;
            TryArmFromCommandLine();
        }

        [MenuItem("Resonance/Smoke/Natural Play")]
        public static void MenuRun()
        {
            RequestRun("matrix");
        }

        [MenuItem("Resonance/Smoke/Natural Play Matrix")]
        public static void MenuRunMatrix()
        {
            RequestRun("np.matrix.v1");
        }

        [MenuItem("Resonance/Smoke/Natural Play Basic")]
        public static void MenuRunBasic()
        {
            RequestRun("np.basic.v1");
        }

        [MenuItem("Resonance/Smoke/Natural Play Fever")]
        public static void MenuRunFever()
        {
            RequestRun("np.fever.v1");
        }

        [MenuItem("Resonance/Smoke/Natural Play Auto")]
        public static void MenuRunAuto()
        {
            RequestRun("np.auto.v1");
        }

        /// <summary>Unity.exe -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliMatrix</summary>
        public static void CliMatrix() => RequestRun("np.matrix.v1");

        /// <summary>Unity.exe -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliBasic</summary>
        public static void CliBasic() => RequestRun("np.basic.v1");

        /// <summary>Unity.exe -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliFever</summary>
        public static void CliFever() => RequestRun("np.fever.v1");

        /// <summary>Unity.exe -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliAuto</summary>
        public static void CliAuto() => RequestRun("np.auto.v1");

        /// <summary>Unity.exe -natural-scenario TOKEN -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliFromArgs</summary>
        public static void CliFromArgs() => RequestRun(ReadCliScenarioToken());

        static void TryArmFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            if (args == null || args.Length == 0) return;
            var want = false;
            string scenario = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-natural-play", StringComparison.OrdinalIgnoreCase))
                    want = true;
                if (!string.Equals(args[i], "-natural-scenario", StringComparison.OrdinalIgnoreCase))
                    continue;
                want = true;
                if (i + 1 < args.Length && !string.IsNullOrEmpty(args[i + 1]) && args[i + 1][0] != '-')
                    scenario = args[i + 1];
            }
            if (!want) return;
            if (File.Exists(RequestRel) || File.Exists(RunningRel)) return;
            if (EditorApplication.isPlaying) return;
            RequestRun(string.IsNullOrEmpty(scenario) ? "matrix" : scenario);
        }

        static string ReadCliScenarioToken()
        {
            var args = Environment.GetCommandLineArgs();
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (!string.Equals(args[i], "-natural-scenario", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (i + 1 < args.Length && !string.IsNullOrEmpty(args[i + 1]) && args[i + 1][0] != '-')
                        return args[i + 1];
                }
            }
            return "matrix";
        }

        static void RequestRun(string scenario)
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestRel, string.IsNullOrEmpty(scenario) ? "matrix" : scenario);
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            if (EditorApplication.isPlaying)
            {
                if (File.Exists(RunningRel)) File.Delete(RunningRel);
                if (File.Exists(RequestRel)) File.Move(RequestRel, RunningRel);
                IsolateSave();
                NaturalPlayRuntime.Arm();
                return;
            }
            TryEnterPlay();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                if (!File.Exists(RunningRel)) return;
                FocusPortraitGameView();
                IsolateSave();
                EditorApplication.delayCall += EnsurePlayRuntime;
                return;
            }
            if (change == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += QuitIfDone;
        }

        static void EnsurePlayRuntime()
        {
            if (!Application.isPlaying) return;
            if (!File.Exists(RunningRel) && !File.Exists(RequestRel)) return;
            if (UnityEngine.Object.FindFirstObjectByType<GameRoot>() == null)
                new GameObject("GameRoot").AddComponent<GameRoot>();
            NaturalPlayRuntime.Arm();
        }

        static void Poll()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlaying)
            {
                if (File.Exists(RunningRel) || File.Exists(RequestRel))
                    EnsurePlayRuntime();
                if (File.Exists(ResultRel))
                    EditorApplication.isPlaying = false;
                return;
            }

            QuitIfDone();
            if (EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup < _nextTry) return;
            if (!File.Exists(RequestRel)) return;
            TryEnterPlay();
        }

        static void TryEnterPlay()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!File.Exists(RequestRel)) return;
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[NATURAL-PLAY] scripts failed to compile");
                return;
            }

            _nextTry = EditorApplication.timeSinceStartup + 30;
            Directory.CreateDirectory("Temp");
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            if (File.Exists(RunningRel)) File.Delete(RunningRel);
            IsolateSave();
            File.WriteAllText(QuitRel, "1");
            File.Move(RequestRel, RunningRel);
            FocusPortraitGameView();
            Debug.Log("[NATURAL-PLAY] entering play mode");
            EditorApplication.EnterPlaymode();
            EditorApplication.delayCall += EnsurePlayRuntime;
        }

        static void IsolateSave()
        {
            var dir = Path.GetDirectoryName(SmokeSaveRel);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            try { if (File.Exists(SmokeSaveRel)) File.Delete(SmokeSaveRel); } catch { }
            try { if (File.Exists(SmokeSaveRel + ".bak")) File.Delete(SmokeSaveRel + ".bak"); } catch { }
            try { if (File.Exists(SmokeSaveRel + ".tmp")) File.Delete(SmokeSaveRel + ".tmp"); } catch { }
            SaveStore.SetPathOverride(Path.GetFullPath(SmokeSaveRel));
        }

        static void QuitIfDone()
        {
            if (EditorApplication.isPlaying) return;
            if (!File.Exists(QuitRel) || !File.Exists(ResultRel)) return;
            try { File.Delete(QuitRel); } catch { }
            var pass = false;
            try
            {
                var body = File.ReadAllText(ResultRel);
                pass = body.StartsWith("PASS");
                Directory.CreateDirectory("captures");
                File.WriteAllText(Path.Combine("captures", "natural-play.result.txt"), body);
            }
            catch { }
            Debug.Log("[NATURAL-PLAY] quitting editor after result pass=" + pass);
            EditorApplication.Exit(pass ? 0 : 1);
        }

        static void FocusPortraitGameView()
        {
            try
            {
                var idx = EnsurePortraitSize(1080, 1920, "M1 Portrait 1080x1920");
                var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (t == null) return;
                var w = EditorWindow.GetWindow(t);
                w.maximized = false;
                if (idx >= 0)
                {
                    var prop = t.GetProperty("selectedSizeIndex",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.CanWrite)
                        prop.SetValue(w, idx, null);
                }
                w.Focus();
                Debug.Log("[NATURAL-PLAY] game view portrait index=" + idx);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NATURAL-PLAY] portrait game view: " + e.Message);
            }
        }

        static int EnsurePortraitSize(int w, int h, string label)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            if (sizesType == null) return -1;
            var singleton = sizesType.BaseType;
            if (singleton == null) return -1;
            var instProp = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            if (instProp == null) return -1;
            var instance = instProp.GetValue(null, null);
            var groupEnum = asm.GetType("UnityEditor.GameViewSizeGroupType");
            var standalone = System.Enum.Parse(groupEnum, "Standalone");
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { standalone });
            var groupT = group.GetType();
            var getTotal = groupT.GetMethod("GetTotalCount");
            var getSize = groupT.GetMethod("GetGameViewSize");
            var addCustom = groupT.GetMethod("AddCustomSize");
            var total = (int)getTotal.Invoke(group, null);
            for (int i = 0; i < total; i++)
            {
                var s = getSize.Invoke(group, new object[] { i });
                var sw = (int)s.GetType().GetProperty("width").GetValue(s, null);
                var sh = (int)s.GetType().GetProperty("height").GetValue(s, null);
                if (sw == w && sh == h) return i;
            }
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
            var fixedRes = System.Enum.Parse(sizeKind, "FixedResolution");
            var ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
            if (ctor == null || addCustom == null) return -1;
            var custom = ctor.Invoke(new object[] { fixedRes, w, h, label });
            addCustom.Invoke(group, new object[] { custom });
            return (int)getTotal.Invoke(group, null) - 1;
        }
    }
}
