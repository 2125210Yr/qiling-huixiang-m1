using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Natural-play E2E: only EventSystem pointer events. No GameRoot.Tap/Slide/
    /// FireDrivePerfect/StartVsBattle, no BattleSim field writes, no VFX clears.
    /// Read-only inspection of GameRoot.Live / BattleSim is allowed.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class NaturalPlayRuntime : MonoBehaviour
    {
        public const float SlideThresholdPx = 80f;
        public const int SlideDragFrames = 6;
        public const float SlideDragPx = 120f;
        const float LimitSec = 360f;

        static bool _armed;

        readonly List<string> _inputs = new List<string>(256);
        readonly List<PhaseRow> _phases = new List<PhaseRow>(32);
        readonly List<string> _shots = new List<string>(16);
        readonly List<string> _reflect = new List<string>(8);
        readonly StringBuilder _line = new StringBuilder(256);

        string _failPhase;
        bool _done;
        bool _capturing;
        float _started;
        int _waveAtBattle;
        bool _feverShot;
        int _cmdLogSeen;

        struct PhaseRow
        {
            public string Phase;
            public string Expected;
            public string Observed;
            public int Frame;
            public int Ms;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateSaveBeforeBoot()
        {
            if (!WantNatural()) return;
            var path = SmokeSavePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            TryDelete(path);
            TryDelete(path + ".bak");
            TryDelete(path + ".tmp");
            SaveStore.SetPathOverride(path);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeArm()
        {
            if (!WantNatural()) return;
            SaveStore.SetPathOverride(SmokeSavePath());
            Arm();
        }

        public static void Arm()
        {
            _armed = true;
            if (!WantNatural() && !Application.isPlaying) return;
            TryAttach();
        }

        static void TryAttach()
        {
            var live = GameRoot.Live;
            if (live == null) return;
            if (live.GetComponent<NaturalPlayRuntime>() != null) return;
            live.gameObject.AddComponent<NaturalPlayRuntime>();
        }

        static bool WantNatural()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-natural-play", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            if (!Application.isEditor) return false;
            return File.Exists(RequestPath()) || File.Exists(RunningPath()) || _armed;
        }

        static string TempDir()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
        }

        static string CapturesDir()
        {
            return CaptureShots.CapturesDir();
        }

        static string RequestPath() => Path.Combine(TempDir(), "natural-play.request");
        static string RunningPath() => Path.Combine(TempDir(), "natural-play.running");
        static string TempResultPath() => Path.Combine(TempDir(), "natural-play.result.txt");
        static string DurableResultPath() => Path.Combine(CapturesDir(), "natural-play.result.txt");
        static string EventsPath() => Path.Combine(CapturesDir(), "natural-play.events.txt");
        static string SmokeSavePath() => Path.Combine(TempDir(), "natural-play-save", "save.json");

        static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        void Start()
        {
            _started = Time.unscaledTime;
            StartCoroutine(Run());
        }

        void Update()
        {
            if (_done) return;
            if (GameRoot.Live == null && _armed) TryAttach();
        }

        IEnumerator Run()
        {
            Note("natural-play start");
            var until = Time.unscaledTime + LimitSec;
            while (GameRoot.Live == null)
            {
                if (Time.unscaledTime > until)
                {
                    Fail("HomeVisible", "GameRoot.Live", "null");
                    yield break;
                }
                yield return null;
            }

            yield return WaitScreen("HomeVisible", "Home", "screen=Home", 20f);
            if (_done) yield break;
            yield return TapNamed("TapToStage", 8f, "关卡");
            if (_done) yield break;
            yield return WaitScreen("StageVisible", "Stage", "screen=Stage", 10f);
            if (_done) yield break;
            yield return TapNamed("TapStart", 8f, "FIGHT");
            if (_done) yield break;
            yield return WaitBattleBuilt("BattleVisible", 15f);
            if (_done) yield break;
            yield return Shot("np_01_battle.png");
            if (_done) yield break;

            // Natural pacing (observed 2026-09-13 run 4): five allies' auto attacks fill Drive in ~3.5 s, long
            // before p0's Charge (~9 s), and the HUD maps a portrait tap to DriveBegin whenever Drive>=100.
            // A real player therefore judges the QTE first and only gets a Tap window while Drive is refilling.
            // VS-1 also self-resolves in ~24 s with no input. So the battle segment is state-driven, not scripted.
            yield return PlayBattleNaturally();
            if (_done) yield break;

            yield return WaitResult("ResultVisible", ResultTimeoutSec());
            if (_done) yield break;
            yield return DismissSplash("ResultVisible", 12f);
            if (_done) yield break;
            yield return Shot("np_09_result.png");
            if (_done) yield break;

            yield return TapNamed("Rematch", 12f, "RETRY", "NEXT");
            if (_done) yield break;
            yield return WaitBattleBuilt("RematchBattle", 15f);
            if (_done) yield break;
            yield return Shot("np_10_rematch.png");
            if (_done) yield break;

            yield return TapNamed("ExitPause", 8f, "|| PAUSE");
            if (_done) yield break;
            yield return WaitPauseBoard("ExitPause", true, 8f);
            if (_done) yield break;
            yield return TapNamed("ExitHome", 8f, "HOME");
            if (_done) yield break;
            yield return WaitScreen("HomeReturn", "Home", "screen=Home", 12f);
            if (_done) yield break;
            yield return Shot("np_11_home.png");
            if (_done) yield break;

            Pass();
        }

        float ResultTimeoutSec()
        {
            var b = Battle();
            var left = b != null ? b.TimeLeft : 0f;
            var cap = left > 1f ? left + 45f : 140f;
            if (cap < 120f) cap = 120f;
            if (cap > 200f) cap = 200f;
            return cap;
        }

        IEnumerator WaitScreen(string phase, string screen, string expected, float timeout)
        {
            var t0 = Time.unscaledTime;
            string observed;
            while (true)
            {
                observed = ScreenOf();
                if (observed == screen)
                {
                    Record(phase, expected, "screen=" + observed, true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, expected, "screen=" + observed);
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator WaitBattleBuilt(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var g = GameRoot.Live;
                var battle = g != null && g.CurrentScreen == "Battle" && g.Battle != null;
                var hud = FindActive("p0") != null;
                if (battle && hud)
                {
                    _waveAtBattle = g.Battle.WaveIndex;
                    Record(phase, "screen=Battle hud=p0", "screen=Battle hud=p0 wave=" + _waveAtBattle, true);
                    yield break;
                }
                var obs = "screen=" + ScreenOf() + " battle=" + (g != null && g.Battle != null) + " p0=" + hud;
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "screen=Battle hud=p0", obs);
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>
        /// State-driven battle play through real pointer events only. Goals that a Manual player can reach in
        /// one VS-1 run: judge a QTE, fire p0 Tap, fire p0 Slide, pause+resume, toggle speed. Fever is played
        /// when it happens but is not required (it depends on QTE grade / gauge). Ends when every goal is met
        /// or the battle ends; missing goals are reported honestly as the failure reason.
        /// </summary>
        IEnumerator PlayBattleNaturally()
        {
            bool qteDone = false, tapDone = false, slideDone = false, pauseDone = false, speedDone = false;
            var t0 = Time.unscaledTime;
            var qteCount = 0;
            while (true)
            {
                if (_done) yield break;
                var g = GameRoot.Live;
                var b = Battle();
                if (g == null || b == null)
                {
                    Fail("BattlePlay", "battle alive", "GameRoot/Battle null " + ObserveBattle());
                    yield break;
                }
                var missing = MissingGoals(qteDone, tapDone, slideDone, pauseDone, speedDone);
                if (missing.Length == 0)
                {
                    Record("BattlePlay", "QTE+Tap+Slide+Pause+Speed via pointer", "all reached qte=" + qteCount + " " + ObserveBattle(), true);
                    yield break;
                }
                var ended = g.CurrentScreen == "Result" || b.Outcome != BattleOutcome.InProgress;
                if (ended)
                {
                    // Pacing finding, not a crash: keep going so Result/Rematch/Home still get exercised.
                    SoftFail("BattlePlay", "QTE+Tap+Slide+Pause+Speed before battle end",
                        "missing=" + missing + " battleSec=" + (Time.unscaledTime - t0).ToString("0.0") + " " + ObserveBattle());
                    yield break;
                }
                if (TimedOut(t0, 150f) || OverLimit())
                {
                    Fail("BattlePlay", "QTE+Tap+Slide+Pause+Speed within 150s", "missing=" + missing + " " + ObserveBattle());
                    yield break;
                }

                // 1. QTE window open: judge it (the coin is the only sensible target now).
                if (g.QteOpen || b.PendingDriveSlot >= 0)
                {
                    if (!qteDone) yield return Shot("np_04_qte.png");
                    if (_done) yield break;
                    yield return TapQte(qteDone ? "JudgeQte#" + (qteCount + 1) : "JudgeQte", 8f);
                    if (_done) yield break;
                    qteCount++;
                    if (!qteDone)
                    {
                        qteDone = true;
                        yield return Shot("np_05_judge.png");
                    }
                    continue;
                }

                // 2. Fever running: mash portraits like a player would.
                if (b.FeverActive)
                {
                    if (!_feverShot)
                    {
                        yield return Shot("np_08_fever.png");
                        _feverShot = true;
                        Record("FeverPlay", "Fever reached via QTE", ObserveBattle(), true);
                    }
                    yield return TapReadyPortraits();
                    yield return new WaitForSecondsRealtime(0.22f);
                    continue;
                }

                // 3. Pause/resume once, early (does not advance the sim).
                if (!pauseDone && !b.Paused)
                {
                    yield return TapNamed("TapPause", 8f, "|| PAUSE");
                    if (_done) yield break;
                    yield return WaitPauseBoard("PauseVisible", true, 8f);
                    if (_done) yield break;
                    yield return Shot("np_06_pause.png");
                    if (_done) yield break;
                    yield return TapNamed("Resume", 8f, "CONTINUE");
                    if (_done) yield break;
                    yield return WaitPauseBoard("Resume", false, 8f);
                    if (_done) yield break;
                    pauseDone = true;
                    continue;
                }

                // 4. Speed toggle once, early (a player sets speed up front).
                if (!speedDone)
                {
                    var speedBefore = ReadSpeed();
                    yield return TapNamed("SpeedToggle", 8f, ">> X1 SPEED");
                    if (_done) yield break;
                    yield return WaitSpeedChanged("SpeedToggle", speedBefore, 6f);
                    if (_done) yield break;
                    yield return Shot("np_07_speed.png");
                    if (_done) yield break;
                    speedDone = true;
                    continue;
                }

                var u0 = b.Allies != null && b.Allies.Length > 0 ? b.Allies[0] : null;
                var charged = u0 != null && u0.Alive && u0.Charge >= 100f;

                // 5. Slide is a drag gesture and is not remapped by Drive; take it whenever p0 is charged.
                if (!slideDone && charged && u0.SlideCd <= 0f)
                {
                    yield return SlideNamed("SlidePortrait", 10f, "p0", SlideDragPx);
                    if (_done) yield break;
                    slideDone = true;
                    yield return Shot("np_03_slide.png");
                    continue;
                }

                // 6. Drive full: a portrait tap opens the QTE (HUD contract), so open it and loop back to judge.
                if (b.Drive >= 100f)
                {
                    yield return TapNamed(qteDone ? "OpenQte#" + (qteCount + 1) : "OpenQte", 8f, "p0");
                    if (_done) yield break;
                    yield return WaitQteOpen(qteDone ? "OpenQte#" + (qteCount + 1) : "OpenQte", 4f);
                    if (_done) yield break;
                    continue;
                }

                // 7. Drive refilling and p0 charged: this is the only window where a tap is a Tap skill.
                if (!tapDone && charged)
                {
                    var chargeBefore = u0.Charge;
                    yield return TapNamed("TapPortrait", 8f, "p0");
                    if (_done) yield break;
                    yield return null;
                    var after = b.Allies[0];
                    if (after != null && after.Charge < chargeBefore)
                    {
                        tapDone = true;
                        Record("TapSkill", "p0 Charge consumed by Tap", "charge " + chargeBefore.ToString("0.#") + "->" + after.Charge.ToString("0.#"), true);
                        yield return Shot("np_02_tap.png");
                    }
                    else
                    {
                        Record("TapSkill", "p0 Charge consumed by Tap", "not consumed " + ObserveBattle(), true);
                    }
                    continue;
                }

                yield return null;
            }
        }

        static string MissingGoals(bool qte, bool tap, bool slide, bool pause, bool speed)
        {
            var s = "";
            if (!qte) s += "QTE,";
            if (!tap) s += "Tap,";
            if (!slide) s += "Slide,";
            if (!pause) s += "Pause,";
            if (!speed) s += "Speed,";
            return s.TrimEnd(',');
        }

        IEnumerator WaitSlotReady(string phase, bool tap, bool slide, float timeout, bool rejectDriveWindow = false)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                if (ScreenOf() == "Result")
                {
                    Fail(phase, ReadyExpected(tap, slide), "screen=Result (battle ended)");
                    yield break;
                }
                string obs;
                if (SlotReady(0, tap, slide, out obs, rejectDriveWindow))
                {
                    Record(phase, ReadyExpected(tap, slide), obs, true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, ReadyExpected(tap, slide), obs);
                    yield break;
                }
                yield return null;
            }
        }

        static string ReadyExpected(bool tap, bool slide)
        {
            return slide ? "p0 Charge>=100 SlideCd<=0" : "p0 Charge>=100";
        }

        bool SlotReady(int slot, bool tap, bool slide, out string observed, bool rejectDriveWindow = false)
        {
            var b = Battle();
            if (b == null || b.Allies == null || slot < 0 || slot >= b.Allies.Length)
            {
                observed = "no-ally";
                return false;
            }
            var u = b.Allies[slot];
            if (u == null)
            {
                observed = "ally-null";
                return false;
            }
            var g = GameRoot.Live;
            if (g != null && g.QteOpen)
            {
                observed = "QteOpen (cannot treat as tap/slide ready)";
                return false;
            }
            if (b.PendingDriveSlot >= 0)
            {
                observed = "PendingDrive=" + b.PendingDriveSlot;
                return false;
            }
            var dash = ReadyDashLit(slot);
            observed = "alive=" + u.Alive + " charge=" + u.Charge.ToString("0.#")
                + " slideCd=" + u.SlideCd.ToString("0.#") + " readyDash=" + dash
                + " drive=" + b.Drive.ToString("0.#");
            if (!u.Alive) return false;
            if (tap && u.Charge < 100f && !dash) return false;
            if (slide && u.SlideCd > 0f) return false;
            if (rejectDriveWindow && b.Drive >= 100f)
            {
                observed += " (Drive already 100; tap would open QTE before Slide)";
                return false;
            }
            return u.Charge >= 100f || dash;
        }

        static bool ReadyDashLit(int slot)
        {
            var port = FindActive("p" + slot);
            if (port == null) return false;
            var dash = port.transform.Find("readyDash");
            if (dash == null) return false;
            var img = dash.GetComponent<Image>();
            return img != null && img.color.a > 0.05f;
        }

        IEnumerator WaitDrive(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var b = Battle();
                var drive = b != null ? b.Drive : -1f;
                if (b != null && drive >= 100f)
                {
                    Record(phase, "Drive>=100", "Drive=" + drive.ToString("0.#"), true);
                    yield break;
                }
                if (ScreenOf() == "Result")
                {
                    Fail(phase, "Drive>=100", "screen=Result Drive=" + drive.ToString("0.#"));
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "Drive>=100", "Drive=" + drive.ToString("0.#"));
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator WaitQteOpen(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var g = GameRoot.Live;
                var b = Battle();
                var open = g != null && g.QteOpen;
                var pending = b != null && b.PendingDriveSlot >= 0;
                if (open || pending)
                {
                    Record(phase, "QteOpen or PendingDrive",
                        "QteOpen=" + open + " pending=" + (b != null ? b.PendingDriveSlot : -1), true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "QteOpen or PendingDrive",
                        "QteOpen=" + open + " pending=" + (b != null ? b.PendingDriveSlot : -1));
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator TapQte(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (FindQteButton() == null)
            {
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "vfxGood / VfxGoodButton active", "missing (GO name is vfxGood; children raycastTarget=false)");
                    yield break;
                }
                yield return null;
            }
            var go = FindQteButton();
            var pos = ScreenCenter(go);
            var hit = PeekRaycast(pos);
            var hitPath = hit != null ? PathOf(hit) : "";
            var owns = hit != null && (hit == go || hit.transform.IsChildOf(go.transform) || go.transform.IsChildOf(hit.transform));
            yield return PointerAt(pos, "tap");
            if (!owns)
            {
                Fail(phase, "raycast hits VfxGoodButton",
                    "raycast=" + (string.IsNullOrEmpty(hitPath) ? "none" : hitPath)
                    + " — vfxGood has no raycastTarget Graphic");
                yield break;
            }
            var t1 = Time.unscaledTime;
            while (true)
            {
                var g = GameRoot.Live;
                var b = Battle();
                var closed = (g == null || !g.QteOpen) && (b == null || b.PendingDriveSlot < 0);
                if (closed)
                {
                    Record(phase, "QTE resolved via vfxGood tap", ObserveBattle(), true);
                    yield break;
                }
                if (Time.unscaledTime - t1 > 4f || OverLimit())
                {
                    Fail(phase, "QTE resolved via vfxGood tap", ObserveBattle());
                    yield break;
                }
                yield return null;
            }
        }

        static GameObject FindQteButton()
        {
            var named = FindActive("vfxGood");
            if (named != null && named.activeInHierarchy) return named;
            var fx = UnityEngine.Object.FindObjectsByType<VfxGoodButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (fx != null)
            {
                for (int i = 0; i < fx.Length; i++)
                {
                    if (fx[i] != null && fx[i].gameObject.activeInHierarchy)
                        return fx[i].gameObject;
                }
            }
            return null;
        }

        IEnumerator WaitPauseBoard(string phase, bool want, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var board = FindActive("PauseBoard") != null;
                var paused = Battle() != null && Battle().Paused;
                if (want && board && paused)
                {
                    Record(phase, "PauseBoard + Paused", "PauseBoard Paused=true", true);
                    yield break;
                }
                if (!want && !board && !paused)
                {
                    Record(phase, "no PauseBoard + !Paused", "resumed", true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, want ? "PauseBoard + Paused" : "no PauseBoard + !Paused",
                        "board=" + board + " paused=" + paused);
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator WaitSpeedChanged(string phase, int before, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var now = ReadSpeed();
                if (now > 0 && now != before)
                {
                    Record(phase, "Speed!=" + before, "Speed=" + now, true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "Speed!=" + before, "Speed=" + now);
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator HuntFever(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            var wave0 = Battle() != null ? Battle().WaveIndex : _waveAtBattle;
            var sawFever = false;
            while (true)
            {
                var g = GameRoot.Live;
                var b = Battle();
                if (g != null && g.CurrentScreen == "Result")
                {
                    Record(phase, "Fever or wave or Result", "Result (no fever required)", true);
                    yield break;
                }
                if (b != null && b.Outcome != BattleOutcome.InProgress && !b.FeverActive)
                {
                    Record(phase, "Fever or wave or end", "outcome=" + b.Outcome, true);
                    yield break;
                }
                if (b != null && (b.FeverActive || b.FeverEver || (g != null && g.FeverOn)))
                {
                    sawFever = true;
                    if (!_feverShot)
                    {
                        yield return Shot("np_08_fever.png");
                        if (_done) yield break;
                        _feverShot = true;
                    }
                    if (b.FeverActive)
                    {
                        yield return TapReadyPortraits();
                        yield return new WaitForSecondsRealtime(0.22f);
                    }
                    else
                    {
                        Record(phase, "Fever or wave or end", "FeverEver Drive=" + b.Drive.ToString("0.#"), true);
                        yield break;
                    }
                }
                else if (b != null && b.WaveIndex != wave0)
                {
                    Record(phase, "Fever or wave or end", "wave " + wave0 + "->" + b.WaveIndex, true);
                    yield break;
                }
                else
                {
                    yield return TapReadyPortraits();
                    yield return new WaitForSecondsRealtime(0.28f);
                }

                if (sawFever && b != null && !b.FeverActive)
                {
                    Record(phase, "Fever or wave or end", "fever ended", true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    if (sawFever)
                    {
                        Record(phase, "Fever or wave or end", "fever seen; hunt timeout", true);
                        yield break;
                    }
                    Fail(phase, "Fever or wave change or battle end", ObserveBattle());
                    yield break;
                }
            }
        }

        IEnumerator TapReadyPortraits()
        {
            var b = Battle();
            if (b == null || b.Allies == null || b.Paused) yield break;
            if (GameRoot.Live != null && GameRoot.Live.QteOpen)
            {
                var qte = FindQteButton();
                if (qte != null) yield return PointerAt(ScreenCenter(qte), "tap");
                yield break;
            }
            for (int i = 0; i < b.Allies.Length; i++)
            {
                string obs;
                if (!SlotReady(i, true, false, out obs) && !b.FeverActive) continue;
                var u = b.Allies[i];
                if (u == null || !u.Alive) continue;
                var go = FindActive("p" + i);
                if (go == null) continue;
                yield return PointerAt(ScreenCenter(go), "tap");
                yield break;
            }
        }

        IEnumerator WaitResult(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                if (ScreenOf() == "Result")
                {
                    Record(phase, "screen=Result", "screen=Result title=" + ResultTitle(), true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "screen=Result", "screen=" + ScreenOf() + " " + ObserveBattle());
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator DismissSplash(string phase, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (VfxStageClear.AnyLive())
            {
                var veil = FindActive("veil");
                if (veil != null)
                    yield return PointerAt(ScreenCenter(veil), "tap");
                else
                    yield return TapAt(0.5f, 0.5f, "tap");
                if (TimedOut(t0, timeout))
                {
                    Record(phase, "splash dismissed or timeout", "vfxStageClear still live", true);
                    yield break;
                }
                yield return null;
            }
            var board = FindActive("ResultBoard");
            var cg = board != null ? board.GetComponent<CanvasGroup>() : null;
            var wait = Time.unscaledTime + 4f;
            while (cg != null && !cg.interactable && Time.unscaledTime < wait)
                yield return null;
            Record(phase, "ResultBoard interactable",
                "board=" + (board != null) + " interactable=" + (cg == null || cg.interactable), true);
        }

        IEnumerator TapNamed(string phase, float timeout, params string[] names)
        {
            var t0 = Time.unscaledTime;
            GameObject go = null;
            while (go == null)
            {
                go = FindActive(names);
                if (go != null) break;
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "button " + string.Join("|", names), "not found under canvas");
                    yield break;
                }
                yield return null;
            }
            var pos = ScreenCenter(go);
            if (!RaycastOwns(pos, go))
            {
                Fail(phase, "raycast hits " + go.name, "raycast=" + PathOf(PeekRaycast(pos)));
                yield break;
            }
            // The tap usually navigates away and destroys `go`; capture identity before pressing.
            var goName = go.name;
            var goPath = PathOf(go);
            yield return PointerAt(pos, "tap");
            Record(phase, "tap " + goName, "tapped " + goPath, true);
            yield return new WaitForSecondsRealtime(0.12f);
        }

        IEnumerator SlideNamed(string phase, float timeout, string name, float pixels)
        {
            var t0 = Time.unscaledTime;
            GameObject go = null;
            while (go == null)
            {
                go = FindActive(name);
                if (go != null) break;
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "portrait " + name, "not found");
                    yield break;
                }
                yield return null;
            }
            var pos = ScreenCenter(go);
            if (!RaycastOwns(pos, go))
            {
                Fail(phase, "raycast hits " + name, "raycast=" + PathOf(PeekRaycast(pos)));
                yield break;
            }
            yield return PointerAt(pos, "slide", pixels, SlideDragFrames);
            yield return new WaitForSecondsRealtime(0.20f);
            var b = Battle();
            var u = b != null && b.Allies != null && b.Allies.Length > 0 ? b.Allies[0] : null;
            if (u == null || u.SlideCd <= 0f)
            {
                Fail(phase, "SlideCd>0 after slide-up",
                    u == null ? "no ally0" : "SlideCd=" + u.SlideCd.ToString("0.#") + " " + ObserveBattle());
                yield break;
            }
            Record(phase, "slideUp " + name + " dy>" + SlideThresholdPx, "SlideCd=" + u.SlideCd.ToString("0.#"), true);
        }

        IEnumerator Shot(string name)
        {
            if (_shots.Contains(name)) yield break;
            if (_capturing)
            {
                while (_capturing) yield return null;
                yield break;
            }
            _capturing = true;
            try
            {
                yield return CaptureShots.Shot(name);
                if (!_shots.Contains(name)) _shots.Add(name);
                Note("shot " + name);
            }
            finally
            {
                _capturing = false;
            }
        }

        public IEnumerator TapAt(float nx, float ny, string kind)
        {
            var pos = new Vector2(nx * Screen.width, ny * Screen.height);
            yield return PointerAt(pos, kind);
        }

        public IEnumerator SlideUpAt(float nx, float ny, float pixels)
        {
            var pos = new Vector2(nx * Screen.width, ny * Screen.height);
            yield return PointerAt(pos, "slide", pixels, SlideDragFrames);
        }

        public bool TapButtonByName(string name)
        {
            var go = FindActive(name);
            if (go == null) return false;
            StartCoroutine(PointerAt(ScreenCenter(go), "tap"));
            return true;
        }

        public IEnumerator PointerAt(Vector2 screenPos, string kind, float dragPixels = 0f, int dragFrames = 0)
        {
            var es = EventSystem.current;
            if (es == null)
            {
                LogInput(kind, screenPos, null, "EventSystem.current=null");
                yield break;
            }

            var ped = new PointerEventData(es)
            {
                pointerId = -1,
                button = PointerEventData.InputButton.Left,
                position = screenPos,
                pressPosition = screenPos,
                clickTime = Time.unscaledTime,
                eligibleForClick = dragPixels <= 0f,
                pointerEnter = null
            };

            var hit = FirstRaycast(es, ped);
            ped.pointerCurrentRaycast = hit.displayIndex >= 0 || hit.gameObject != null ? hit : default;
            var go = hit.gameObject;
            var down = go != null ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(go) : null;
            if (down == null) down = go;

            if (go != null)
                ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerEnterHandler);
            if (down != null)
            {
                ped.pointerPress = down;
                ped.rawPointerPress = go;
                ped.pointerPressRaycast = ped.pointerCurrentRaycast;
                ExecuteEvents.Execute(down, ped, ExecuteEvents.pointerDownHandler);
            }

            var slide = string.Equals(kind, "slide", StringComparison.OrdinalIgnoreCase) && dragPixels > 0f;
            if (slide)
            {
                var drag = down != null ? ExecuteEvents.GetEventHandler<IDragHandler>(down) : null;
                if (drag == null && go != null) drag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
                ped.pointerDrag = drag != null ? drag : down;
                ped.dragging = true;
                ped.useDragThreshold = true;
                if (ped.pointerDrag != null)
                {
                    ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.beginDragHandler);
                }
                var frames = dragFrames < 1 ? 1 : dragFrames;
                for (int i = 1; i <= frames; i++)
                {
                    var y = screenPos.y + dragPixels * (i / (float)frames);
                    ped.position = new Vector2(screenPos.x, y);
                    ped.delta = new Vector2(0f, dragPixels / frames);
                    RefreshRaycast(es, ped);
                    if (ped.pointerDrag != null)
                        ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.dragHandler);
                    yield return null;
                }
                if (ped.pointerDrag != null)
                    ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.endDragHandler);
                ped.dragging = false;
            }

            if (down != null)
                ExecuteEvents.Execute(down, ped, ExecuteEvents.pointerUpHandler);
            if (!slide)
            {
                var click = down != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(down) : null;
                if (click == null && go != null) click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
                if (click != null)
                    ExecuteEvents.Execute(click, ped, ExecuteEvents.pointerClickHandler);
            }

            LogInput(kind, screenPos, go ?? down, ObserveBattle());
        }

        static RaycastResult FirstRaycast(EventSystem es, PointerEventData ped)
        {
            var results = new List<RaycastResult>(16);
            es.RaycastAll(ped, results);
            return results.Count > 0 ? results[0] : default;
        }

        static void RefreshRaycast(EventSystem es, PointerEventData ped)
        {
            var hit = FirstRaycast(es, ped);
            ped.pointerCurrentRaycast = hit;
        }

        GameObject PeekRaycast(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return null;
            var ped = new PointerEventData(es) { position = screenPos };
            var hit = FirstRaycast(es, ped);
            return hit.gameObject;
        }

        bool RaycastOwns(Vector2 screenPos, GameObject go)
        {
            var hit = PeekRaycast(screenPos);
            if (go == null || hit == null) return false;
            return hit == go
                || hit.transform.IsChildOf(go.transform)
                || go.transform.IsChildOf(hit.transform);
        }

        void LogInput(string kind, Vector2 pos, GameObject target, string after)
        {
            _line.Length = 0;
            _line.Append("frame=").Append(Time.frameCount);
            _line.Append(" t=").Append((Time.unscaledTime - _started).ToString("0.000"));
            _line.Append(" kind=").Append(kind);
            _line.Append(" pos=").Append(pos.x.ToString("0")).Append(',').Append(pos.y.ToString("0"));
            _line.Append(" target=").Append(target != null ? PathOf(target) : "none");
            _line.Append(" after=").Append(after ?? "");
            _inputs.Add(_line.ToString());
        }

        string ObserveBattle()
        {
            var b = Battle();
            if (b == null) return "battle=null screen=" + ScreenOf();
            var cmd = ReadLastCommand(b);
            _line.Length = 0;
            _line.Append("screen=").Append(ScreenOf());
            _line.Append(" last=").Append(b.LastEvent ?? "");
            _line.Append(" drive=").Append(b.Drive.ToString("0.#"));
            _line.Append(" qte=").Append(b.PendingDriveSlot);
            _line.Append(" fever=").Append(b.FeverActive);
            _line.Append(" paused=").Append(b.Paused);
            _line.Append(" speed=").Append(b.Speed);
            _line.Append(" auto=").Append(b.Auto);
            _line.Append(" wave=").Append(b.WaveIndex);
            _line.Append(" outcome=").Append(b.Outcome);
            if (!string.IsNullOrEmpty(cmd)) _line.Append(" cmd=").Append(cmd);
            return _line.ToString();
        }

        string ReadLastCommand(BattleSim b)
        {
            var list = ReadCommandLog(b);
            if (list == null || list.Count == 0) return "";
            if (list.Count == _cmdLogSeen) return FormatCommand(list[list.Count - 1]);
            _cmdLogSeen = list.Count;
            return FormatCommand(list[list.Count - 1]);
        }

        IList ReadCommandLog(BattleSim b)
        {
            if (b == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var t = b.GetType();
            var prop = t.GetProperty("CommandLog", flags);
            if (prop != null && prop.CanRead)
            {
                NoteReflect("BattleSim.CommandLog property (read-only)");
                return prop.GetValue(b, null) as IList;
            }
            var field = t.GetField("CommandLog", flags);
            if (field != null)
            {
                NoteReflect("BattleSim.CommandLog field (read-only)");
                return field.GetValue(b) as IList;
            }
            NoteReflect("BattleSim.CommandLog missing — integrator Commands.cs not visible");
            return null;
        }

        string ReadRunHeader(BattleSim b)
        {
            if (b == null) return "";
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var m = b.GetType().GetMethod("RunHeader", flags, null, Type.EmptyTypes, null);
            if (m == null)
            {
                NoteReflect("BattleSim.RunHeader() missing — integrator Commands.cs not visible");
                return "";
            }
            NoteReflect("BattleSim.RunHeader() (read-only)");
            try { return m.Invoke(b, null) as string ?? ""; }
            catch (Exception e) { return "RunHeader error: " + e.Message; }
        }

        static string FormatCommand(object rec)
        {
            if (rec == null) return "";
            var t = rec.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            object Kind() => t.GetField("Kind", flags)?.GetValue(rec) ?? t.GetProperty("Kind", flags)?.GetValue(rec, null);
            object Acc() => t.GetField("Accepted", flags)?.GetValue(rec) ?? t.GetProperty("Accepted", flags)?.GetValue(rec, null);
            object Reason() => t.GetField("Reason", flags)?.GetValue(rec) ?? t.GetProperty("Reason", flags)?.GetValue(rec, null);
            object Slot() => t.GetField("Slot", flags)?.GetValue(rec) ?? t.GetProperty("Slot", flags)?.GetValue(rec, null);
            object Seq() => t.GetField("Seq", flags)?.GetValue(rec) ?? t.GetProperty("Seq", flags)?.GetValue(rec, null);
            return "seq=" + Seq() + " kind=" + Kind() + " slot=" + Slot() + " ok=" + Acc() + " reason=" + Reason();
        }

        void NoteReflect(string msg)
        {
            if (_reflect.Contains(msg)) return;
            _reflect.Add(msg);
        }

        static BattleSim Battle()
        {
            var g = GameRoot.Live;
            return g != null ? g.Battle : null;
        }

        static int ReadSpeed()
        {
            var b = Battle();
            return b != null ? b.Speed : 0;
        }

        static string ScreenOf()
        {
            var g = GameRoot.Live;
            return g != null ? g.CurrentScreen : "null";
        }

        static string ResultTitle()
        {
            var g = GameRoot.Live;
            return g != null ? g.ResultTitle : "";
        }

        static GameObject FindActive(params string[] names)
        {
            var canvas = CanvasRoot();
            if (canvas == null || names == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                var found = FindNamed(canvas, names[i]);
                if (found != null) return found;
            }
            return null;
        }

        static Transform CanvasRoot()
        {
            var g = GameRoot.Live;
            if (g == null) return null;
            var c = g.GetComponentInChildren<Canvas>(true);
            return c != null ? c.transform : g.transform;
        }

        static GameObject FindNamed(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name && root.gameObject.activeInHierarchy)
                return root.gameObject;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindNamed(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        static Vector2 ScreenCenter(GameObject go)
        {
            if (go == null) return Vector2.zero;
            var rt = go.transform as RectTransform;
            if (rt == null)
                return RectTransformUtility.WorldToScreenPoint(null, go.transform.position);
            var canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            var world = rt.TransformPoint(rt.rect.center);
            return RectTransformUtility.WorldToScreenPoint(cam, world);
        }

        static string PathOf(GameObject go)
        {
            if (go == null) return "";
            var t = go.transform;
            var stack = new Stack<string>(8);
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        static bool TimedOut(float t0, float timeout) => Time.unscaledTime - t0 >= timeout;

        bool OverLimit()
        {
            if (Time.unscaledTime - _started <= LimitSec) return false;
            if (string.IsNullOrEmpty(_failPhase))
                _failPhase = "timeout";
            return true;
        }

        void Record(string phase, string expected, string observed, bool ok)
        {
            _phases.Add(new PhaseRow
            {
                Phase = phase,
                Expected = expected,
                Observed = observed,
                Frame = Time.frameCount,
                Ms = (int)((Time.unscaledTime - _started) * 1000f)
            });
            Note((ok ? "ok " : "fail ") + phase + " " + observed);
        }

        void Note(string msg) => _inputs.Add("note " + msg);

        void Fail(string phase, string expected, string observed)
        {
            if (_done) return;
            _failPhase = phase;
            Record(phase, expected, observed, false);
            WriteAndQuit(false);
        }

        /// <summary>Records a failed phase but lets the flow continue so later phases still produce evidence.</summary>
        void SoftFail(string phase, string expected, string observed)
        {
            if (_done) return;
            if (string.IsNullOrEmpty(_failPhase)) _failPhase = phase;
            Record(phase, expected, observed, false);
        }

        void Pass()
        {
            if (_done) return;
            WriteAndQuit(string.IsNullOrEmpty(_failPhase));
        }

        void WriteAndQuit(bool pass)
        {
            _done = true;
            WriteEvents();
            var body = BuildResult(pass);
            try
            {
                Directory.CreateDirectory(TempDir());
                File.WriteAllText(TempResultPath(), body);
            }
            catch (Exception e) { Debug.LogWarning("[NATURAL-PLAY] temp result: " + e.Message); }
            try
            {
                Directory.CreateDirectory(CapturesDir());
                File.WriteAllText(DurableResultPath(), body);
            }
            catch (Exception e) { Debug.LogWarning("[NATURAL-PLAY] captures result: " + e.Message); }
            try { if (File.Exists(RunningPath())) File.Delete(RunningPath()); }
            catch { }
            Debug.Log("[NATURAL-PLAY]\n" + body);
            if (!Application.isEditor)
                Application.Quit();
        }

        void WriteEvents()
        {
            try
            {
                Directory.CreateDirectory(CapturesDir());
                var b = Battle();
                var text = "";
                if (b != null && b.Events != null)
                    text = b.Events.ExportCanonical();
                File.WriteAllText(EventsPath(), text ?? "");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NATURAL-PLAY] events: " + e.Message);
            }
        }

        string BuildResult(bool pass)
        {
            var sb = new StringBuilder(4096);
            if (pass) sb.AppendLine("PASS");
            else sb.AppendLine("FAIL " + (_failPhase ?? "unknown"));
            sb.AppendLine("captures=" + string.Join(",", _shots));
            sb.AppendLine("screen=" + ScreenOf());
            sb.AppendLine("title=" + ResultTitle());
            var b = Battle();
            if (b != null)
            {
                sb.AppendLine("drive=" + b.Drive.ToString("0.#")
                    + " fever=" + b.FeverActive + " ever=" + b.FeverEver
                    + " speed=" + b.Speed + " auto=" + b.Auto
                    + " outcome=" + b.Outcome + " wave=" + b.WaveIndex);
            }
            sb.AppendLine();
            sb.AppendLine("phase\texpected\tobserved\tframe\tms");
            for (int i = 0; i < _phases.Count; i++)
            {
                var p = _phases[i];
                sb.Append(p.Phase).Append('\t')
                    .Append(p.Expected).Append('\t')
                    .Append(p.Observed).Append('\t')
                    .Append(p.Frame).Append('\t')
                    .Append(p.Ms).Append('\n');
            }
            sb.AppendLine();
            sb.AppendLine("--- inputs ---");
            for (int i = 0; i < _inputs.Count; i++)
                sb.AppendLine(_inputs[i]);
            sb.AppendLine();
            sb.AppendLine("--- RunHeader ---");
            var header = ReadRunHeader(b);
            sb.AppendLine(string.IsNullOrEmpty(header) ? "(unavailable)" : header);
            sb.AppendLine();
            sb.AppendLine("--- CommandLog ---");
            var log = ReadCommandLog(b);
            if (log == null || log.Count == 0) sb.AppendLine("(unavailable or empty)");
            else
            {
                for (int i = 0; i < log.Count; i++)
                    sb.AppendLine(FormatCommand(log[i]));
            }
            if (_reflect.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- reflection (read-only) ---");
                for (int i = 0; i < _reflect.Count; i++)
                    sb.AppendLine(_reflect[i]);
            }
            return sb.ToString();
        }
    }
}
