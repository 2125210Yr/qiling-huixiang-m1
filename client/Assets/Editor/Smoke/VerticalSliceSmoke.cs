using System.IO;
using System.Reflection;
using Resonance.App;
using Resonance.Battle;
using UnityEditor;
using UnityEngine;

namespace Resonance.EditorTools
{
    [InitializeOnLoad]
    public static class VerticalSliceSmoke
    {
        const string RequestRel = "Temp/vs-smoke.request";
        const string RunningRel = "Temp/vs-smoke.running";
        const string ResultRel = "Temp/vs-smoke.result.txt";
        const string QuitRel = "Temp/vs-smoke.quit";
        const string PlayRel = "Temp/play.request";
        const string SmokeSaveRel = "Temp/vs-smoke-save/save.json";
        static double _nextTry;

        static VerticalSliceSmoke()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Resonance/Play For Look")]
        public static void PlayForLook()
        {
            Directory.CreateDirectory("Temp");
            if (File.Exists(RequestRel)) File.Delete(RequestRel);
            if (File.Exists(RunningRel)) File.Delete(RunningRel);
            FocusGameView();
            if (EditorApplication.isPlaying) return;
            Debug.Log("[PLAY-FOR-LOOK] entering play mode");
            EditorApplication.EnterPlaymode();
        }

        static void FocusGameView()
        {
            try
            {
                var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (t == null) return;
                var w = EditorWindow.GetWindow(t);
                w.Focus();
                w.maximized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PLAY-FOR-LOOK] game view: " + e.Message);
            }
        }

        /// <summary>
        /// GT is portrait. Maximized Game view is landscape and cannot be T28-compared.
        /// Fixed 1080×1920. Not T27 / T28 by itself.
        /// </summary>
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
                Debug.Log("[VS-SMOKE] game view portrait index=" + idx);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[VS-SMOKE] portrait game view: " + e.Message);
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

        [MenuItem("Resonance/Run Vertical Slice Smoke")]
        public static void MenuRun()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestRel, "1");
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            TryEnterPlay();
        }

        [MenuItem("Resonance/Run Lose Slice Smoke")]
        public static void MenuRunLose()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestRel, "lose");
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            TryEnterPlay();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                if (!File.Exists(RunningRel)) return;
                FocusPortraitGameView();
                IsolateSmokeSave();
                EditorApplication.delayCall += EnsurePlayRuntime;
                return;
            }
            if (change == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += QuitIfSmokeDone;
        }

        static void EnsurePlayRuntime()
        {
            if (!Application.isPlaying) return;
            if (!File.Exists(RunningRel)) return;
            if (UnityEngine.Object.FindFirstObjectByType<GameRoot>() == null)
                new GameObject("GameRoot").AddComponent<GameRoot>();
            var smokes = UnityEngine.Object.FindObjectsByType<VerticalSliceSmokeRuntime>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (smokes == null || smokes.Length == 0)
            {
                var go = new GameObject("VerticalSliceSmokeRuntime");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<VerticalSliceSmokeRuntime>();
            }
        }

        static void Poll()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlaying)
            {
                if (File.Exists(RunningRel)) EnsurePlayRuntime();
                if (File.Exists(ResultRel))
                    EditorApplication.isPlaying = false;
                return;
            }

            QuitIfSmokeDone();
            if (EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup < _nextTry) return;
            if (File.Exists(PlayRel))
            {
                File.Delete(PlayRel);
                PlayForLook();
                return;
            }
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
                Debug.LogError("[VS-SMOKE] scripts failed to compile");
                return;
            }

            _nextTry = EditorApplication.timeSinceStartup + 30;
            Directory.CreateDirectory("Temp");
            if (File.Exists(ResultRel)) File.Delete(ResultRel);
            if (File.Exists(RunningRel)) File.Delete(RunningRel);
            IsolateSmokeSave();
            File.WriteAllText(QuitRel, "1");
            File.Move(RequestRel, RunningRel);
            FocusPortraitGameView();
            Debug.Log("[VS-SMOKE] entering play mode");
            EditorApplication.EnterPlaymode();
            EditorApplication.delayCall += EnsurePlayRuntime;
        }

        static void IsolateSmokeSave()
        {
            var dir = Path.GetDirectoryName(SmokeSaveRel);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            try { if (File.Exists(SmokeSaveRel)) File.Delete(SmokeSaveRel); } catch { }
            try { if (File.Exists(SmokeSaveRel + ".bak")) File.Delete(SmokeSaveRel + ".bak"); } catch { }
            try { if (File.Exists(SmokeSaveRel + ".tmp")) File.Delete(SmokeSaveRel + ".tmp"); } catch { }
            SaveStore.SetPathOverride(Path.GetFullPath(SmokeSaveRel));
        }

        static void QuitIfSmokeDone()
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
                File.WriteAllText(Path.Combine("captures", "vs-smoke.result.txt"), body);
            }
            catch { }
            Debug.Log("[VS-SMOKE] quitting editor after result pass=" + pass);
            EditorApplication.Exit(pass ? 0 : 1);
        }

        [MenuItem("Resonance/Headless Loop Smoke")]
        public static void HeadlessLoopSmoke()
        {
            var ok = RunHeadlessLoop(out var body);
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "headless-loop.result.txt"), body);
            Debug.Log("[VS-SMOKE-HEADLESS]\n" + body);
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool RunHeadlessLoop(out string body)
        {
            try
            {
                var blob = new SaveBlob();
                blob.GetUnit(Catalog.DefaultParty[0]).Reserve = "STEEE";
                var sim = new BattleSim(Catalog.DefaultParty, 1, 42, Catalog.Stages[0], blob.ProgressForParty())
                {
                    Deterministic = true,
                    Speed = 2
                };
                SliceDriveSequence.ReadyCharges(sim);
                var tap = sim.TryTap(0);
                SliceDriveSequence.ReadyCharges(sim);
                var slide = sim.TrySlide(1);
                var drive = SliceDriveSequence.PlayUntilFever(sim);
                var fever = sim.FeverActive;
                sim.AutoTap = true;
                for (int t = 0; t < BattleSim.TickHz * 40 && sim.Outcome == BattleOutcome.InProgress; t++)
                    sim.Tick();
                blob.Vs1Cleared = sim.Outcome == BattleOutcome.Victory;
                blob.ClearedCount = System.Math.Max(blob.ClearedCount, 1);
                var json = SaveStore.Serialize(blob);
                var parsed = SaveStore.Parse(json);
                if (!tap) throw new System.Exception("tap never fired");
                if (!slide) throw new System.Exception("slide never fired");
                if (!drive) throw new System.Exception("drive never fired");
                if (!fever) throw new System.Exception("fever never triggered");
                if (parsed.PartyIds == null || parsed.PartyIds.Length < 5)
                    throw new System.Exception("save roundtrip failed");
                var chapter = new SaveBlob();
                if (!MvpLoop.TryClearChapter(chapter, false, 42, out var chapterReport))
                    throw new System.Exception("chapter auto clear failed\n" + chapterReport);
                body = "PASS\n"
                    + "screens=Boot,Home,Characters,Team,Stage,Battle,Result,Archive,Library,Deep,Settings\n"
                    + "mode=headless-battlesim\n"
                    + "outcome=" + sim.Outcome + "\n"
                    + "tap=" + tap + " slide=" + slide + " drive=" + drive + " fever=" + fever + "\n"
                    + "chapter-cleared=" + chapter.ClearedCount + "\n"
                    + chapterReport
                    + "save-json=" + json + "\n";
                return true;
            }
            catch (System.Exception e)
            {
                body = "FAIL " + e.Message + "\n" + e;
                return false;
            }
        }
    }
}
