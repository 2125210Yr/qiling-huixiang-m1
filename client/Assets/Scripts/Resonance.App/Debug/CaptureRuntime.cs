using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Resonance.App
{
    public static class CaptureShots
    {
        static string _overrideDir;

        public static void SetOverrideDir(string dir)
        {
            _overrideDir = string.IsNullOrEmpty(dir) ? null : dir;
        }

        public static string CapturesDir()
        {
            if (!string.IsNullOrEmpty(_overrideDir))
                return Path.GetFullPath(_overrideDir);
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "captures"));
        }

        public static string FindHiresTiles()
        {
            var dir = new DirectoryInfo(Application.dataPath);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var a = Path.Combine(dir.FullName, "_hires_tiles");
                if (Directory.Exists(a)) return a;
                var b = Path.Combine(dir.FullName, "天命之子数据", "_hires_tiles");
                if (Directory.Exists(b)) return b;
            }
            return null;
        }

        public static bool Busy { get; private set; }

        public static IEnumerator Shot(params string[] names)
        {
            var guard = 0;
            while (CueStripRecorder.Grabbing && guard++ < 8)
                yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D tex = null;
            Busy = true;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null)
                    Write(tex.EncodeToPNG(), names);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CAPTURE] " + e.Message);
            }
            finally
            {
                Busy = false;
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            yield return null;
        }

        public static void Write(byte[] png, params string[] names)
        {
            if (png == null || png.Length == 0 || names == null) return;
            var captures = CapturesDir();
            Directory.CreateDirectory(captures);
            var tiles = FindHiresTiles();
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (string.IsNullOrEmpty(name)) continue;
                File.WriteAllBytes(Path.Combine(captures, name), png);
                if (tiles != null && name.StartsWith("ui_"))
                    File.WriteAllBytes(Path.Combine(tiles, name), png);
            }
        }
    }

    public sealed class CaptureRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeStart()
        {
            if (!WantCapture()) return;
            if (FindFirstObjectByType<CaptureRuntime>() != null) return;
            var go = new GameObject("CaptureRuntime");
            DontDestroyOnLoad(go);
            go.AddComponent<CaptureRuntime>();
        }

        static bool WantCapture()
        {
            var args = System.Environment.GetCommandLineArgs();
            var capture = false;
            var smoke = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-capture", System.StringComparison.OrdinalIgnoreCase))
                    capture = true;
                else if (string.Equals(args[i], "-smoke", System.StringComparison.OrdinalIgnoreCase))
                    smoke = true;
                else if (string.Equals(args[i], "-captureDir", System.StringComparison.OrdinalIgnoreCase)
                         && i + 1 < args.Length)
                    CaptureShots.SetOverrideDir(args[++i]);
            }
            return capture && !smoke;
        }

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var dir = CaptureShots.CapturesDir();
            Directory.CreateDirectory(dir);
            var done = Path.Combine(dir, "DONE.txt");
            if (File.Exists(done)) File.Delete(done);

            float wait = 0f;
            while (GameRoot.Live == null || GameRoot.Live.CurrentScreen != "Home")
            {
                wait += Time.unscaledDeltaTime;
                if (wait > 12f)
                {
                    File.WriteAllText(done, "FAIL timeout waiting for Home screen=" +
                        (GameRoot.Live == null ? "null" : GameRoot.Live.CurrentScreen));
                    Quit();
                    yield break;
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.45f);
            yield return CaptureShots.Shot("01_home.png", "ui_home.png");
            yield return new WaitForSecondsRealtime(1.05f);
            yield return CaptureShots.Shot("01_home_idle.png");

            GameRoot.Live.Go("Characters");
            yield return new WaitForSecondsRealtime(0.35f);
            yield return CaptureShots.Shot("02_roster.png", "02_characters.png", "ui_roster.png");

            GameRoot.Live.Go("Team");
            yield return new WaitForSecondsRealtime(0.35f);
            yield return CaptureShots.Shot("04_team.png", "ui_team.png");

            GameRoot.Live.Inspect("C001");
            yield return new WaitForSecondsRealtime(0.40f);
            yield return CaptureShots.Shot("03_inspect.png", "ui_inspect.png");

            GameRoot.Live.Go("Stage");
            yield return new WaitForSecondsRealtime(0.30f);
            yield return CaptureShots.Shot("05_stage.png");

            GameRoot.Live.EnsureAutoOn();
            GameRoot.Live.StartVsBattle();
            GameRoot.Live.EnsureSpeed2();
            if (GameRoot.Live.Battle != null) GameRoot.Live.Battle.DebugForceHold();
            yield return new WaitForSecondsRealtime(0.55f);
            yield return CaptureShots.Shot("06_battle.png", "ui_battle.png");
            if (GameRoot.Live.Battle != null) GameRoot.Live.Battle.DebugRelease();

            var t = 0f;
            var feverShot = false;
            while (GameRoot.Live.CurrentScreen == "Battle" && t < 95f)
            {
                GameRoot.Live.FireDrivePerfect();
                if (!feverShot && GameRoot.Live.FeverOn)
                {
                    yield return CaptureShots.Shot("07_fever.png");
                    feverShot = true;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameRoot.Live.CurrentScreen == "Result")
            {
                yield return new WaitForSecondsRealtime(0.35f);
                yield return CaptureShots.Shot("08_result.png");
            }
            else
            {
                File.WriteAllText(done, "FAIL still screen=" + GameRoot.Live.CurrentScreen
                    + " fever=" + (GameRoot.Live.FeverSeen ? "yes" : "no"));
                Quit();
                yield break;
            }

            var files = Directory.GetFiles(dir, "*.png");
            var tiles = CaptureShots.FindHiresTiles();
            var body = "PASS " + files.Length + " pngs screen=" + GameRoot.Live.CurrentScreen
                + " result=" + GameRoot.Live.ResultTitle
                + " fever=" + (GameRoot.Live.FeverSeen ? "yes" : "no")
                + " captures=" + dir
                + (tiles != null ? " hires=" + tiles : "")
                + "\n" + string.Join("\n", files);
            File.WriteAllText(done, body);
            Quit();
        }

        static void Quit()
        {
            if (!Application.isEditor)
                Application.Quit();
        }
    }
}
