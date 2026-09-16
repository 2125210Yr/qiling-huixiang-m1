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
        VerificationRunPlan _plan;
        string _sessionId;
        string _catalogNote;
        readonly List<NaturalPlayBattleEvidence> _battles = new List<NaturalPlayBattleEvidence>(8);
        NaturalPlayBattleEvidence _current;
        int _fightIndex;
        readonly List<int> _feverSlots = new List<int>(4);
        bool _catalogApplied;

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

        static string ReadScenarioToken()
        {
            try
            {
                if (File.Exists(RunningPath()))
                {
                    var t = File.ReadAllText(RunningPath()).Trim();
                    if (!string.IsNullOrEmpty(t)) return t;
                }
                if (File.Exists(RequestPath()))
                {
                    var t = File.ReadAllText(RequestPath()).Trim();
                    if (!string.IsNullOrEmpty(t)) return t;
                }
            }
            catch { }
            return "matrix";
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
            Note("historical_baseline=" + NaturalPlayBattleEvidence.HistoricalRun7Rel
                + " verdict=FAIL BattlePlay missing=Tap,Slide (kept; not rewritten)");
            Note("pointer=" + NaturalPlayBattleEvidence.PointerPath + " os_touch=NOT_CLAIMED");
            _sessionId = NaturalPlayBattleEvidence.NewSessionId();
            _plan = VerificationRunPlan.Resolve(ReadScenarioToken(), Environment.GetCommandLineArgs());
            Note("plan mode=" + _plan.Mode
                + " fights=" + (_plan.Fights != null ? _plan.Fights.Length : 0)
                + " provenance=" + DesignPlaceholderPolicy.Provenance
                + " schema=" + DesignPlaceholderPolicy.SchemaVersion);

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

            try
            {
                _catalogNote = VerificationCatalog.Apply(_plan);
                _catalogApplied = true;
                Note("catalog " + _catalogNote);
                Record("VerificationCatalog", "DESIGN_PLACEHOLDER before FIGHT", _catalogNote, true);
            }
            catch (Exception e)
            {
                Fail("VerificationCatalog", "apply before start", e.Message);
                yield break;
            }

            yield return WaitScreen("HomeVisible", "Home", "screen=Home", 20f);
            if (_done) yield break;
            yield return TapNamed("TapToStage", 8f, "关卡");
            if (_done) yield break;
            yield return WaitScreen("StageVisible", "Stage", "screen=Stage", 10f);
            if (_done) yield break;
            yield return TapNamed("TapStart", 8f, "FIGHT");
            if (_done) yield break;

            _fightIndex = 0;
            while (!_done)
            {
                var sc = _plan.FightAt(_fightIndex) ?? VerificationScenario.Basic;
                var lastFight = _plan.Fights == null || _fightIndex >= _plan.Fights.Length - 1;
                yield return WaitBattleBuilt(_fightIndex == 0 ? "BattleVisible" : "BattleVisible#" + (_fightIndex + 1), 15f);
                if (_done) yield break;
                BeginBattleCapture(sc);
                if (_fightIndex == 0) yield return Shot("np_01_battle.png");
                if (_done) yield break;

                yield return PlayBattleNaturally();
                if (_done) yield break;

                var autoExit = lastFight && sc.Name == VerificationScenario.Auto.Name && !_plan.RematchExitAfterLastWin;
                if (autoExit)
                {
                    PersistCurrentBattle("auto-exit");
                    break;
                }

                if (ScreenOf() != "Result")
                    yield return WaitResult("ResultVisible", ResultTimeoutSec());
                else
                    Record("ResultVisible", "screen=Result", "screen=Result title=" + ResultTitle(), true);
                if (_done) yield break;

                PersistCurrentBattle("pre-next");
                yield return DismissSplash("ResultVisible", 12f);
                if (_done) yield break;
                if (_fightIndex == 0) yield return Shot("np_09_result.png");
                if (_done) yield break;

                if (!lastFight)
                {
                    yield return TapNamed("Rematch", 12f, "RETRY", "NEXT");
                    if (_done) yield break;
                    _fightIndex++;
                    continue;
                }

                if (_plan.RematchExitAfterLastWin)
                {
                    yield return TapNamed("Rematch", 12f, "RETRY", "NEXT");
                    if (_done) yield break;
                    yield return WaitBattleBuilt("RematchBattle", 15f);
                    if (_done) yield break;
                    BeginBattleCapture(sc);
                    yield return Shot("np_10_rematch.png");
                    if (_done) yield break;
                    PersistCurrentBattle("rematch-enter");
                }
                break;
            }

            if (_done) yield break;
            yield return TapNamed("ExitPause", 8f, "|| PAUSE");
            if (_done) yield break;
            yield return WaitPauseBoard("ExitPause", true, 8f);
            if (_done) yield break;
            PersistCurrentBattle("pre-home");
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
        /// State-driven battle play through EventSystem pointers only. HUD Drive&gt;=100 → DriveBegin
        /// is not remapped. TickUnit still floors auto DriveGain at 14 until the integrator hunk.
        /// Fever is required only in np.fever.v1.
        /// </summary>
        IEnumerator PlayBattleNaturally()
        {
            var sc = _plan != null ? _plan.FightAt(_fightIndex) : null;
            var name = sc != null ? sc.Name : VerificationScenario.Basic.Name;
            if (name == VerificationScenario.Fever.Name)
            {
                yield return PlayFeverNaturally();
                yield break;
            }
            if (name == VerificationScenario.Auto.Name)
            {
                yield return PlayAutoNaturally();
                yield break;
            }
            yield return PlayBasicNaturally();
        }

        IEnumerator PlayBasicNaturally()
        {
            bool tapDone = false, slideDone = false, pauseDone = false, speedDone = false, goalsNoted = false;
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
                if (!tapDone && NaturalPlayBattleEvidence.HasAccepted(b, BattleCommandKind.Tap, 0, CommandSource.Player))
                    tapDone = true;
                if (!slideDone && NaturalPlayBattleEvidence.HasAccepted(b, BattleCommandKind.Slide, 0, CommandSource.Player))
                    slideDone = true;
                var missing = MissingPair(tapDone, slideDone, "Tap", "Slide");
                var ended = g.CurrentScreen == "Result" || b.Outcome != BattleOutcome.InProgress;
                if (missing.Length == 0 && !goalsNoted)
                {
                    goalsNoted = true;
                    Record("BattlePlay", "Tap+Slide via pointer before Drive remap",
                        "reached " + ObserveBattle(), true);
                }
                if (ended)
                {
                    if (missing.Length != 0)
                        SoftFail("BattlePlay", "Tap+Slide before battle end",
                            "missing=" + missing + " battleSec=" + (Time.unscaledTime - t0).ToString("0.0") + " " + ObserveBattle());
                    yield break;
                }
                if (TimedOut(t0, 150f) || OverLimit())
                {
                    Fail("BattlePlay", "Tap+Slide within 150s", "missing=" + missing + " " + ObserveBattle());
                    yield break;
                }

                if (g.QteOpen || b.PendingDriveSlot >= 0)
                {
                    yield return TapQte("JudgeQte#" + (qteCount + 1), 8f, true);
                    if (_done) yield break;
                    qteCount++;
                    continue;
                }

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

                // Slide is not remapped by Drive. Take it on the first charged window.
                if (!slideDone && charged && u0.SlideCd <= 0f)
                {
                    yield return SlideNamed("SlidePortrait", 10f, "p0", SlideDragPx);
                    if (_done) yield break;
                    slideDone = NaturalPlayBattleEvidence.HasAccepted(b, BattleCommandKind.Slide, 0, CommandSource.Player);
                    if (slideDone) yield return Shot("np_03_slide.png");
                    continue;
                }

                // HUD contract: Drive>=100 remaps portrait tap to DriveBegin. Dump Drive if Tap is still missing.
                if (b.Drive >= 100f && !tapDone)
                {
                    yield return TapNamed("OpenQte#" + (qteCount + 1), 8f, "p0");
                    if (_done) yield break;
                    yield return WaitQteOpen("OpenQte#" + (qteCount + 1), 4f);
                    if (_done) yield break;
                    continue;
                }

                if (b.Drive >= 100f && tapDone && slideDone)
                {
                    yield return TapNamed("OpenQte#" + (qteCount + 1), 8f, "p0");
                    if (_done) yield break;
                    yield return WaitQteOpen("OpenQte#" + (qteCount + 1), 4f);
                    if (_done) yield break;
                    continue;
                }

                if (!tapDone && charged && b.Drive < 100f)
                {
                    var chargeBefore = u0.Charge;
                    yield return TapNamed("TapPortrait", 8f, "p0");
                    if (_done) yield break;
                    yield return null;
                    var after = b.Allies[0];
                    var accepted = NaturalPlayBattleEvidence.HasAccepted(b, BattleCommandKind.Tap, 0, CommandSource.Player);
                    if (accepted && after != null && after.Charge < chargeBefore)
                    {
                        tapDone = true;
                        Record("TapSkill", "p0 Tap Submit + Charge consumed",
                            "charge " + chargeBefore.ToString("0.#") + "->" + after.Charge.ToString("0.#"), true);
                        yield return Shot("np_02_tap.png");
                    }
                    else
                    {
                        Record("TapSkill", "p0 Tap Submit + Charge consumed",
                            "accepted=" + accepted + " " + ObserveBattle(), true);
                    }
                    continue;
                }

                yield return null;
            }
        }

        IEnumerator PlayFeverNaturally()
        {
            bool qteDone = false, feverDone = false, tapA = false, tapB = false, goalsNoted = false;
            var t0 = Time.unscaledTime;
            var qteCount = 0;
            while (true)
            {
                if (_done) yield break;
                var g = GameRoot.Live;
                var b = Battle();
                if (g == null || b == null)
                {
                    Fail("FeverPlay", "battle alive", "GameRoot/Battle null " + ObserveBattle());
                    yield break;
                }
                if (b.FeverActive || b.FeverEver) feverDone = true;
                NaturalPlayBattleEvidence.DistinctAcceptedFeverSlots(b, _feverSlots);
                if (_feverSlots.Count >= 1) tapA = true;
                if (_feverSlots.Count >= 2) tapB = true;
                var missing = "";
                if (!qteDone) missing += "QTE,";
                if (!feverDone) missing += "Fever,";
                if (!tapA) missing += "FeverTapA,";
                if (!tapB) missing += "FeverTapB,";
                missing = missing.TrimEnd(',');
                var ended = g.CurrentScreen == "Result" || b.Outcome != BattleOutcome.InProgress;
                if (missing.Length == 0)
                {
                    if (!goalsNoted)
                    {
                        goalsNoted = true;
                        Record("FeverPlay", "Fever required + two slot FeverTap",
                            "slots=" + string.Join(",", _feverSlots.ConvertAll(x => x.ToString()).ToArray())
                            + " " + ObserveBattle(), true);
                    }
                    yield break;
                }
                if (ended)
                {
                    SoftFail("FeverPlay", "Fever + two FeverTap slots before end",
                        "missing=" + missing + " " + ObserveBattle());
                    yield break;
                }
                if (TimedOut(t0, 160f) || OverLimit())
                {
                    Fail("FeverPlay", "Fever required within 160s", "missing=" + missing + " " + ObserveBattle());
                    yield break;
                }

                if (g.QteOpen || b.PendingDriveSlot >= 0)
                {
                    if (!qteDone) yield return Shot("np_04_qte.png");
                    if (_done) yield break;
                    yield return TapQte("JudgeQte#" + (qteCount + 1), 8f, true);
                    if (_done) yield break;
                    qteCount++;
                    qteDone = true;
                    if (qteCount == 1) yield return Shot("np_05_judge.png");
                    continue;
                }

                if (b.FeverActive)
                {
                    if (!_feverShot)
                    {
                        yield return Shot("np_08_fever.png");
                        _feverShot = true;
                        Record("FeverReached", "Fever via QTE Submit (not injected)", ObserveBattle(), true);
                    }
                    if (!tapB)
                    {
                        yield return TapDistinctFeverSlots();
                        yield return new WaitForSecondsRealtime(0.25f);
                        continue;
                    }
                }

                if (b.Drive >= 100f)
                {
                    yield return OpenFeverDriveQte("OpenQte#" + (qteCount + 1));
                    if (_done) yield break;
                    continue;
                }

                yield return null;
            }
        }

        IEnumerator PlayAutoNaturally()
        {
            var t0 = Time.unscaledTime;
            yield return ToggleAutoTo("AutoFull", AutoMode.Full, 10f);
            if (_done) yield break;
            var b = Battle();
            if (b == null)
            {
                Fail("AutoPlay", "battle alive", ObserveBattle());
                yield break;
            }
            var evFrom = b.Events != null && b.Events.Events != null ? b.Events.Events.Count : 0;
            var chargeSnap = SnapshotAllyCharge(b);
            var sawCast = false;
            var sawSubmit = false;
            while (!sawSubmit || !sawCast)
            {
                if (_done) yield break;
                b = Battle();
                if (b == null || ScreenOf() == "Result" || b.Outcome != BattleOutcome.InProgress)
                {
                    SoftFail("AutoPlay", "Submit Auto skills + observed auto casts",
                        "ended early submit=" + sawSubmit + " cast=" + sawCast + " " + ObserveBattle());
                    yield break;
                }
                if (TimedOut(t0, 55f) || OverLimit())
                    break;
                sawSubmit = NaturalPlayBattleEvidence.HasAutoSkillSubmit(b);
                var casts = NaturalPlayBattleEvidence.AllyCastsSince(b, evFrom);
                if (casts > 0 || ChargeDropped(b, chargeSnap)) sawCast = true;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (sawCast)
                Record("AutoSkillObserved", "auto policy fired (cast/charge)", ObserveBattle(), true);
            else
                SoftFail("AutoSkillObserved", "auto cast or charge drop", ObserveBattle());
            if (sawSubmit)
                Record("AutoSubmit", "CommandLog Source=Auto skill", ObserveBattle(), true);
            else
                SoftFail("AutoSubmit", "CommandLog Source=Auto Tap/Slide/Drive/FeverTap",
                    "AutoFire still bypasses Submit (X05). TickFever FeverTap is the Submit path. " + ObserveBattle());
            yield return ToggleAutoTo("AutoManual", AutoMode.Manual, 10f);
        }

        /// <summary>
        /// Fever Drive-open only: EventSystem tap on a living, Drive-ready portrait.
        /// Does not Submit DriveBegin / DriveResolve from this runtime.
        /// </summary>
        IEnumerator OpenFeverDriveQte(string phase)
        {
            int slot;
            string obs;
            if (!TryLivingDrivePortrait(out slot, out obs))
            {
                Fail(phase, "living Drive portrait p0..p4", obs);
                yield break;
            }
            var name = "p" + slot;
            Note("fever-drive-open phase=" + phase + " slot=" + slot + " " + name + " " + obs);
            yield return TapNamed(phase, 8f, name);
            if (_done) yield break;
            yield return WaitQteOpen(phase, 4f);
        }

        bool TryLivingDrivePortrait(out int slot, out string observed)
        {
            slot = -1;
            var b = Battle();
            if (b == null || b.Allies == null)
            {
                observed = "no-allies " + ObserveBattle();
                return false;
            }
            var skip = new StringBuilder(64);
            for (int i = 0; i < b.Allies.Length; i++)
            {
                var u = b.Allies[i];
                var name = "p" + i;
                if (u == null)
                {
                    skip.Append(name).Append("=null,");
                    continue;
                }
                if (!u.Alive)
                {
                    skip.Append(name).Append("=dead,");
                    continue;
                }
                CommandReject reason;
                if (!b.CanAcceptSkillInput(i, out reason))
                {
                    skip.Append(name).Append('=').Append(reason).Append(',');
                    continue;
                }
                var go = FindActive(name);
                if (go == null)
                {
                    skip.Append(name).Append("=no-portrait,");
                    continue;
                }
                var driveId = u.Def != null ? u.Def.DriveSkillId : null;
                var sk = Catalog.TrySkill(driveId);
                if (sk != null && !Catalog.IsPlayable(sk))
                {
                    skip.Append(name).Append("=unplayable,");
                    continue;
                }
                slot = i;
                observed = "slot=" + i + " " + name
                    + " alive DriveBegin-ready skipped=" + skip.ToString().TrimEnd(',');
                return true;
            }
            observed = "none living Drive-ready skipped=" + skip.ToString().TrimEnd(',')
                + " " + ObserveBattle();
            return false;
        }

        IEnumerator TapDistinctFeverSlots()
        {
            var b = Battle();
            if (b == null || b.Allies == null || !b.FeverActive) yield break;
            NaturalPlayBattleEvidence.DistinctAcceptedFeverSlots(b, _feverSlots);
            for (int i = 0; i < b.Allies.Length; i++)
            {
                var u = b.Allies[i];
                if (u == null || !u.Alive) continue;
                if (_feverSlots.Contains(i)) continue;
                var go = FindActive("p" + i);
                if (go == null) continue;
                yield return PointerAt(ScreenCenter(go), "tap");
                yield return new WaitForSecondsRealtime(0.22f);
                yield break;
            }
            yield return TapReadyPortraits();
        }

        IEnumerator ToggleAutoTo(string phase, AutoMode want, float timeout)
        {
            var t0 = Time.unscaledTime;
            while (true)
            {
                var b = Battle();
                if (b != null && b.Auto == want)
                {
                    Record(phase, "Auto=" + want + " via HUD", "Auto=" + b.Auto, true);
                    yield break;
                }
                if (TimedOut(t0, timeout) || OverLimit())
                {
                    Fail(phase, "Auto=" + want + " via HUD", "Auto=" + (b != null ? b.Auto.ToString() : "null"));
                    yield break;
                }
                yield return TapNamed(phase + "Tap", 6f, "> MANUAL", "> SEMI AUTO", "> FULL AUTO");
                if (_done) yield break;
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        static float[] SnapshotAllyCharge(BattleSim b)
        {
            if (b == null || b.Allies == null) return new float[0];
            var a = new float[b.Allies.Length];
            for (int i = 0; i < b.Allies.Length; i++)
                a[i] = b.Allies[i] != null ? b.Allies[i].Charge : -1f;
            return a;
        }

        static bool ChargeDropped(BattleSim b, float[] snap)
        {
            if (b == null || b.Allies == null || snap == null) return false;
            var n = Math.Min(snap.Length, b.Allies.Length);
            for (int i = 0; i < n; i++)
            {
                var u = b.Allies[i];
                if (u == null) continue;
                if (snap[i] >= 90f && u.Charge < snap[i] - 5f) return true;
            }
            return false;
        }

        static string MissingPair(bool a, bool b, string nameA, string nameB)
        {
            var s = "";
            if (!a) s += nameA + ",";
            if (!b) s += nameB + ",";
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

        IEnumerator TapQte(string phase, float timeout, bool waitWindow = false)
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
            // Poll the live coin until early-in-window, then EventSystem tap. Not FirePerfect.
            if (waitWindow)
            {
                while (true)
                {
                    var coin = FindLiveCoin();
                    if (coin != null && coin.IsEarlyInWindow)
                    {
                        Note("qte-window phase=" + phase
                            + " age=" + coin.Age.ToString("0.000")
                            + " until=" + coin.SecondsUntilWindow.ToString("0.000")
                            + " in=" + coin.IsInWindow);
                        break;
                    }
                    if (TimedOut(t0, timeout) || OverLimit())
                    {
                        var obs = coin == null
                            ? "missing coin after find"
                            : "age=" + coin.Age.ToString("0.000")
                              + " in=" + coin.IsInWindow
                              + " early=" + coin.IsEarlyInWindow
                              + " until=" + coin.SecondsUntilWindow.ToString("0.000");
                        Fail(phase, "vfxGood early Perfect window (not widened)", obs);
                        yield break;
                    }
                    yield return null;
                }
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
                    Record(phase, "QTE resolved via vfxGood tap", ObserveQteResolve(b), true);
                    yield break;
                }
                if (Time.unscaledTime - t1 > 4f || OverLimit())
                {
                    Fail(phase, "QTE resolved via vfxGood tap", ObserveQteResolve(b));
                    yield break;
                }
                yield return null;
            }
        }

        static GameObject FindQteButton()
        {
            var coin = FindLiveCoin();
            return coin != null ? coin.gameObject : null;
        }

        static VfxGoodButton FindLiveCoin()
        {
            var live = VfxGoodButton.Live;
            if (live != null && live.gameObject.activeInHierarchy) return live;
            var named = FindActive("vfxGood");
            if (named != null && named.activeInHierarchy)
            {
                var onNamed = named.GetComponent<VfxGoodButton>();
                if (onNamed != null) return onNamed;
            }
            var fx = UnityEngine.Object.FindObjectsByType<VfxGoodButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (fx != null)
            {
                for (int i = 0; i < fx.Length; i++)
                {
                    if (fx[i] != null && fx[i].gameObject.activeInHierarchy)
                        return fx[i];
                }
            }
            return null;
        }

        string ObserveQteResolve(BattleSim b)
        {
            var timing = b != null ? b.LastDriveTiming.ToString() : "null";
            var gauge = b != null ? b.FeverGauge.ToString("0.#") : "?";
            return ObserveBattle() + " lastDrive=" + timing + " feverGauge=" + gauge;
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
            _line.Append(" feverEver=").Append(b.FeverEver);
            _line.Append(" feverGauge=").Append(b.FeverGauge.ToString("0.#"));
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
            if (b.CommandLog != null)
            {
                NoteReflect("BattleSim.CommandLog (public, read-only)");
                return b.CommandLog;
            }
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
            var typed = rec as CommandRecord;
            if (typed != null) return NaturalPlayBattleEvidence.FormatCommand(typed);
            if (rec == null) return "";
            var t = rec.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            object Kind() => t.GetField("Kind", flags)?.GetValue(rec) ?? t.GetProperty("Kind", flags)?.GetValue(rec, null);
            object Acc() => t.GetField("Accepted", flags)?.GetValue(rec) ?? t.GetProperty("Accepted", flags)?.GetValue(rec, null);
            object Reason() => t.GetField("Reason", flags)?.GetValue(rec) ?? t.GetProperty("Reason", flags)?.GetValue(rec, null);
            object Slot() => t.GetField("Slot", flags)?.GetValue(rec) ?? t.GetProperty("Slot", flags)?.GetValue(rec, null);
            object Seq() => t.GetField("Seq", flags)?.GetValue(rec) ?? t.GetProperty("Seq", flags)?.GetValue(rec, null);
            object Src() => t.GetField("Source", flags)?.GetValue(rec) ?? t.GetProperty("Source", flags)?.GetValue(rec, null);
            object Tick() => t.GetField("Tick", flags)?.GetValue(rec) ?? t.GetProperty("Tick", flags)?.GetValue(rec, null);
            return "seq=" + Seq() + " tick=" + Tick() + " kind=" + Kind() + " slot=" + Slot()
                + " source=" + Src() + " accepted=" + Acc() + " reason=" + Reason();
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

        void BeginBattleCapture(VerificationScenario sc)
        {
            var live = Battle();
            if (live == null) return;
            if (_current != null && _current.Sim == live) return;
            if (_current != null && !_current.Persisted)
                PersistCurrentBattle("late-begin");
            DesignPlaceholderPolicy.Bind(sc);
            // After GameRoot StartBattleAt Freeze (Speed/Auto/Profile already set).
            _current = NaturalPlayBattleEvidence.Begin(live, _sessionId, _battles.Count + 1, sc);
            _battles.Add(_current);
            _cmdLogSeen = 0;
            _feverShot = false;
            Record("BattleId", "unique battle_id + frozen BattleInitialHeader + RunHeader",
                "id=" + _current.BattleId + " scenario=" + _current.ScenarioName
                + " frozen=" + (_current.FrozenInitial != null && _current.FrozenInitial.FrozenAtStart)
                + " header=" + _current.FrozenHeader, true);
        }

        void PersistCurrentBattle(string why)
        {
            if (_current == null) return;
            var live = Battle();
            try
            {
                var status = _current.Persist(CapturesDir(), live);
                if (status == NaturalPlayBattleEvidence.RefuseSimMismatch
                    || status == NaturalPlayBattleEvidence.RefuseBattleIdMismatch)
                {
                    if (!_current.Persisted)
                        status = _current.Persist(CapturesDir(), null);
                    else
                    {
                        Note("persist-refuse " + why + " " + status);
                        return;
                    }
                }
                Note("persist " + _current.BattleId + " why=" + why
                    + " status=" + status
                    + " outcome=" + _current.OutcomeAtPersist
                    + " digest=" + _current.Digest
                    + " replay=" + (_current.Dir != null
                        ? Path.Combine(_current.Dir, NaturalPlayBattleEvidence.ReplayFileName) : ""));
            }
            catch (Exception e)
            {
                Note("persist-fail " + why + " " + e.Message);
            }
        }

        void WriteAndQuit(bool pass)
        {
            _done = true;
            PersistCurrentBattle("quit");
            WriteEvents();
            if (_catalogApplied)
            {
                try { VerificationCatalog.RestoreBuiltin(); }
                catch (Exception e) { Debug.LogWarning("[NATURAL-PLAY] restore catalog: " + e.Message); }
                _catalogApplied = false;
            }
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
                NaturalPlayBattleEvidence.WriteSessionIndex(CapturesDir(), _battles);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NATURAL-PLAY] events index: " + e.Message);
            }
        }

        string BuildResult(bool pass)
        {
            var sb = new StringBuilder(4096);
            if (pass) sb.AppendLine("PASS");
            else sb.AppendLine("FAIL " + (_failPhase ?? "unknown"));
            sb.AppendLine("historical_baseline=" + NaturalPlayBattleEvidence.HistoricalRun7Rel);
            sb.AppendLine("historical_verdict=FAIL BattlePlay missing=Tap,Slide");
            sb.AppendLine("historical_note=run7 kept as FAIL; this session does not rewrite it to PASS");
            sb.AppendLine("pointer=" + NaturalPlayBattleEvidence.PointerPath);
            sb.AppendLine("os_touch=NOT_CLAIMED");
            sb.AppendLine("provenance=" + DesignPlaceholderPolicy.Provenance);
            sb.AppendLine("schema=" + DesignPlaceholderPolicy.SchemaVersion);
            sb.AppendLine("plan=" + (_plan != null ? _plan.Mode : ""));
            sb.AppendLine("session=" + _sessionId);
            var ids = new StringBuilder();
            for (int i = 0; i < _battles.Count; i++)
            {
                if (i > 0) ids.Append(',');
                ids.Append(_battles[i].BattleId);
            }
            sb.AppendLine("battle_ids=" + ids);
            for (int i = 0; i < _battles.Count; i++)
            {
                var ev = _battles[i];
                sb.Append("battle_").Append(i + 1).Append('=')
                    .Append(ev.BattleId)
                    .Append(" scenario=").Append(ev.ScenarioName)
                    .Append(" stage=").Append(ev.StageId)
                    .Append(" persisted=").Append(ev.Persisted ? "1" : "0")
                    .Append(" outcome=").Append(ev.OutcomeAtPersist)
                    .Append(" digest=").Append(ev.Digest)
                    .Append(" dir=").Append(ev.Dir)
                    .Append('\n');
            }
            sb.AppendLine("catalog=" + (_catalogNote ?? ""));
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
            sb.AppendLine("--- frozen battle headers (authoritative) ---");
            if (_battles.Count == 0) sb.AppendLine("(none)");
            for (int i = 0; i < _battles.Count; i++)
            {
                var ev = _battles[i];
                sb.Append(ev.BattleId).Append('\t').Append(ev.FrozenHeader).Append('\n');
            }
            sb.AppendLine();
            sb.AppendLine("--- live RunHeader (current GameRoot.Battle; may be fight 2) ---");
            var header = ReadRunHeader(b);
            sb.AppendLine(string.IsNullOrEmpty(header) ? "(unavailable)" : header);
            sb.AppendLine();
            sb.AppendLine("--- live CommandLog (current GameRoot.Battle; per-fight files are authoritative) ---");
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
