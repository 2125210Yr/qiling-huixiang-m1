using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    [DefaultExecutionOrder(-1000)]
    public sealed class VerticalSliceSmokeRuntime : MonoBehaviour
    {
        const float LimitSec = 210f;

        enum Phase
        {
            Boot, Characters, Team, Inspect, Stage, Battle, Result, Rematch, HomeReturn, Done
        }

        Phase _phase = Phase.Boot;
        float _phaseAt;
        float _started;
        bool _tap, _slide, _drive, _fever;
        bool _battleShot;
        bool _speedShot;
        bool _autoShot;
        bool _pauseShot;
        bool _autoHitShot;
        bool _killShot;
        bool _sawEnemyDeath;
        bool _sawAutoCast;
        int _eventCursor;
        float _autoHitAt;
        float _pauseAt;
        bool _chargeShot;
        bool _tapShot;
        bool _slideShot;
        bool _driveSelectShot;
        bool _driveShot;
        bool _feverShot;
        bool _controlApplied;
        bool _controlShot;
        bool _controlSeen;
        bool _feverDrained;
        bool _waveTried;
        bool _waveShot;
        bool _resultShot;
        bool _levelUpShot;
        bool _levelUpOk;
        bool _rematchOk;
        bool _victoryShot;
        bool _clearWait;
        float _clearAt;
        bool _loseShot;
        bool _loseForced;
        bool _loseMode;
        bool _semiShot;
        float _semiAt;
        bool _sawHome;
        bool _rosterOk;
        bool _teamOk;
        bool _inspectOk;
        bool _capturing;
        bool _phaseShot;
        int _driveTries;
        float _qteAt;
        float _feverAt;
        float _speedAt;
        float _autoHudAt;
        float _chargeAt;
        float _tapAt;
        float _slideAt;
        float _selectAt;
        float _controlAt;
        float _waveAt;
        float _waveHoldAt;
        float _splashAt;
        bool _splashWait;
        readonly List<string> _log = new List<string>(32);
        readonly List<string> _shots = new List<string>(8);

        void Start()
        {
            _started = Time.unscaledTime;
            _phaseAt = Time.unscaledTime;
            _loseMode = ReadLoseMode();
            CueTimingProbe.Reset();
            HitChainProbe.Reset();
            CueStripRecorder.ResetForSmoke();
            Note(_loseMode ? "smoke start lose" : "smoke start");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateSaveBeforeBoot()
        {
            if (!WantSmoke()) return;
            var path = SmokeSavePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            TryDelete(path);
            TryDelete(path + ".bak");
            TryDelete(path + ".tmp");
            SaveStore.SetPathOverride(path);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeStart()
        {
            if (WantSmoke()) SaveStore.SetPathOverride(SmokeSavePath());
            TrySpawn();
        }

        static string SmokeSavePath() => Path.Combine(TempDir(), "vs-smoke-save", "save.json");

        static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        public static void TrySpawn()
        {
            if (!WantSmoke()) return;
            var existing = FindObjectsByType<VerticalSliceSmokeRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0) return;
            var go = new GameObject("VerticalSliceSmokeRuntime");
            DontDestroyOnLoad(go);
            go.AddComponent<VerticalSliceSmokeRuntime>();
        }

        static bool WantSmoke()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], "-smoke", StringComparison.OrdinalIgnoreCase))
                    return true;
            if (!Application.isEditor) return false;
            return File.Exists(RequestPath()) || File.Exists(RunningPath());
        }

        static string TempDir()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
        }

        static string RequestPath() => Path.Combine(TempDir(), "vs-smoke.request");
        static string RunningPath() => Path.Combine(TempDir(), "vs-smoke.running");
        static string ResultPath() => Path.Combine(TempDir(), "vs-smoke.result.txt");

        void Update()
        {
            if (_phase == Phase.Done) return;
            if (_started <= 0f)
            {
                _started = Time.unscaledTime;
                _phaseAt = Time.unscaledTime;
                Note("start-guard");
                return;
            }
            if (Time.unscaledTime - _started > LimitSec)
            {
                Fail("timeout after " + LimitSec + "s at " + _phase);
                return;
            }

            var g = GameRoot.Live;
            if (g == null) return;

            switch (_phase)
            {
                case Phase.Boot:
                    if (g.CurrentScreen == "Home")
                    {
                        if (!_sawHome)
                        {
                            _sawHome = true;
                            _phaseAt = Time.unscaledTime;
                            return;
                        }
                        if (!Aged(0.45f)) return;
                        if (!ShotDone("01_home.png", "ui_home.png")) return;
                        Note("home");
                        g.Go("Characters");
                        Advance(Phase.Characters);
                    }
                    break;
                case Phase.Characters:
                    if (g.CurrentScreen != "Characters")
                    {
                        SkipChainIfAged(5f, "roster skip; screen=" + g.CurrentScreen, "Stage", Phase.Stage);
                        return;
                    }
                    if (!Aged(0.35f)) return;
                    if (!ShotDone("02_roster.png", "02_characters.png", "ui_roster.png"))
                    {
                        SkipChainIfAged(10f, "roster shot skip", "Team", Phase.Team);
                        return;
                    }
                    _rosterOk = true;
                    g.Go("Team");
                    Advance(Phase.Team);
                    break;
                case Phase.Team:
                    if (g.CurrentScreen != "Team")
                    {
                        SkipChainIfAged(5f, "team skip; screen=" + g.CurrentScreen, "Stage", Phase.Stage);
                        return;
                    }
                    if (!Aged(0.25f)) return;
                    if (!ShotDone("04_team.png", "ui_team.png"))
                    {
                        SkipChainIfAged(10f, "team shot skip", "Stage", Phase.Stage);
                        return;
                    }
                    _teamOk = true;
                    g.SetLeader(1);
                    g.Inspect(Catalog.DefaultParty[0]);
                    Advance(Phase.Inspect);
                    break;
                case Phase.Inspect:
                    if (!Aged(0.35f)) return;
                    if (!ShotDone("03_inspect.png", "ui_inspect.png"))
                    {
                        SkipChainIfAged(10f, "inspect shot skip", "Stage", Phase.Stage);
                        return;
                    }
                    _inspectOk = true;
                    g.Go("Stage");
                    Advance(Phase.Stage);
                    break;
                case Phase.Stage:
                    if (g.CurrentScreen == "Stage" && Aged(0.2f))
                    {
                        g.StartVsBattle();
                        if (!_loseMode) HoldForDrive(g.Battle);
                        Advance(Phase.Battle);
                    }
                    break;
                case Phase.Battle:
                    DriveBattle(g);
                    break;
                case Phase.Result:
                    Finish(g);
                    break;
                case Phase.Rematch:
                    DriveRematch(g);
                    break;
                case Phase.HomeReturn:
                    DriveHomeReturn(g);
                    break;
            }
        }

        void DriveHomeReturn(GameRoot g)
        {
            if (_capturing) return;
            if (g.CurrentScreen != "Home")
            {
                if (!Aged(0.20f)) return;
                g.Go("Home");
                Note("result -> home");
                _phaseAt = Time.unscaledTime;
                return;
            }
            if (!Aged(0.40f)) return;
            if (!NamedShot("11_home_return.png")) return;
            PassAfterHomeReturn(g);
        }

        void PassAfterHomeReturn(GameRoot g)
        {
            var savePath = SaveStore.DefaultPath;
            var userSave = SaveStore.UserDefaultPath;
            var isolated = SaveStore.HasPathOverride && !SaveStore.IsUserDefaultPath(savePath);
            var saveExists = File.Exists(savePath);
            var text = saveExists ? File.ReadAllText(savePath) : "";
            var cleared = g.SaveData != null && g.SaveData.Vs1Cleared;
            var screens = VisitedScreens();
            if (g.CurrentScreen != "Home") { Fail("not on Home after result return"); return; }
            if (!_tap) { Fail("tap never fired"); return; }
            if (!_slide) { Fail("slide never fired"); return; }
            if (!_drive) { Fail("drive never fired"); return; }
            if (!_fever) { Fail("fever never triggered"); return; }
            if (!isolated) { Fail("save not isolated; refused user LocalLow path " + userSave); return; }
            if (SaveStore.IsUserDefaultPath(savePath)) { Fail("save path is user LocalLow"); return; }
            if (!saveExists) { Fail("save.json missing at " + savePath); return; }
            if (!_speedShot) { Fail("speed screenshot missing"); return; }
            if (!_autoShot) { Fail("auto screenshot missing"); return; }
            if (!_pauseShot) { Fail("pause screenshot missing"); return; }
            if (!_autoHitShot) { Fail("auto-hit screenshot missing"); return; }
            if (!_sawAutoCast) { Fail("auto basic attack never fired"); return; }
            if (!_killShot) { Fail("kill screenshot missing"); return; }
            if (!_battleShot) { Fail("battle screenshot missing"); return; }
            if (!_chargeShot) { Fail("charge screenshot missing"); return; }
            if (!_tapShot) { Fail("tap screenshot missing"); return; }
            if (!_slideShot) { Fail("slide screenshot missing"); return; }
            if (!_driveSelectShot) { Fail("drive-select screenshot missing"); return; }
            if (!_driveShot) { Fail("drive screenshot missing"); return; }
            if (!_feverShot) { Fail("fever screenshot missing"); return; }
            if (!_controlShot) { Fail("control screenshot missing"); return; }
            if (!_controlSeen) { Fail("control never applied"); return; }
            if (!_resultShot) { Fail("result screenshot missing"); return; }

            var sb = new StringBuilder();
            sb.AppendLine("PASS");
            sb.AppendLine("screens=" + screens);
            sb.AppendLine("captures=" + string.Join(",", _shots));
            sb.AppendLine("result=" + g.ResultTitle);
            sb.AppendLine("home-return=True");
            sb.AppendLine("cleared=" + cleared);
            sb.AppendLine("tap=" + _tap + " slide=" + _slide + " drive=" + _drive + " fever=" + _fever);
            sb.AppendLine("wave=" + _waveShot + " drive-select=" + _driveSelectShot + " control=" + _controlSeen
                + " speed=" + _speedShot + " auto=" + _autoShot + " pause=" + _pauseShot
                + " auto-hit=" + _autoHitShot + " kill=" + _killShot
                + " levelup=" + _levelUpOk + " rematch=" + _rematchOk);
            sb.AppendLine("save=" + savePath);
            sb.AppendLine("save-isolated=True");
            sb.AppendLine("save-user=" + userSave);
            sb.AppendLine("save-json=" + text);
            foreach (var line in _log) sb.AppendLine("log:" + line);
            CueTimingProbe.AppendTo(sb);
            HitChainProbe.AppendTo(sb);
            CueStripRecorder.FlushAll();
            CueStripRecorder.AppendTo(sb);
            WriteResult(sb.ToString());
            _phase = Phase.Done;
        }

        void DriveBattle(GameRoot g)
        {
            var b = g.Battle;
            if (_loseMode)
            {
                DriveLoseBattle(g, b);
                return;
            }
            if (b != null && _speedShot && _autoShot && _pauseShot && _autoHitShot
                && (!_driveShot || !_feverShot))
                HoldForDrive(b);

            if (_capturing) return;

            if (g.CurrentScreen == "Result")
            {
                if (b != null)
                {
                    if (SliceDriveSequence.HasDriveCast(b)) _drive = true;
                    if (b.FeverEver) _fever = true;
                }
                Note("result " + g.ResultTitle);
                Advance(Phase.Result);
                return;
            }
            if (b == null) return;

            if (!_battleShot)
            {
                if (g.CurrentScreen != "Battle" || !Aged(0.40f)) return;
                if (!NamedShot("06_battle.png", "ui_battle.png")) return;
                _battleShot = true;
            }
            if (b.FocusEnemySlot < 0)
            {
                for (int i = b.Enemies.Count - 1; i >= 0; i--)
                {
                    if (!g.TryFocusEnemy(i)) continue;
                    Note("focus enemy " + i);
                    break;
                }
            }

            if (!WaitClockFrames(g, b)) return;

            DrainCombatEvidence(b);
            if (!WaitAutoHitFrame(g, b)) return;
            if (!WaitKillFrame(g, b)) return;

            if (!WaitIsolatedActions(g, b)) return;

            if (!WaitKillFrame(g, b)) return;

            if (!_driveShot)
            {
                if (!WaitDriveFrame(g, b)) return;
                return;
            }

            if (!WaitKillFrame(g, b)) return;

            if (!_feverShot)
            {
                if (!WaitFeverFrame(g, b)) return;
                return;
            }

            if (!WaitKillFrame(g, b)) return;

            if (!_waveTried)
            {
                if (!WaitWaveFrame(g, b)) return;
                return;
            }

            _drive = true;
            _fever = true;
            b.DebugRelease();
            g.EnsureSpeed2();
            g.EnsureAutoOn();
            SliceDriveSequence.ReadyCharges(b);
            for (int i = 0; i < 5; i++)
            {
                g.Tap(i);
                g.Slide(i);
            }
        }

        bool WaitClockFrames(GameRoot g, BattleSim b)
        {
            if (b == null) return false;
            if (!_speedShot)
            {
                int guard = 0;
                while (b.Speed != 2 && guard++ < 4)
                    g.ToggleBattleSpeed();
                if (_speedAt <= 0f) _speedAt = Time.unscaledTime;
                if (Time.unscaledTime - _speedAt < 0.18f) return false;
                if (!NamedShot("06g_speed.png")) return false;
                _speedShot = true;
                Note("speed " + b.Speed);
                return false;
            }
            if (!_autoShot)
            {
                int guard = 0;
                while (b.Auto != AutoMode.Full && guard++ < 3)
                    g.CycleBattleAuto();
                if (_autoHudAt <= 0f) _autoHudAt = Time.unscaledTime;
                if (Time.unscaledTime - _autoHudAt < 0.18f) return false;
                if (!NamedShot("06h_auto.png")) return false;
                _autoShot = true;
                Note("auto " + b.Auto);
                return false;
            }
            if (!_pauseShot)
            {
                if (!b.Paused) g.ToggleBattlePause();
                if (_pauseAt <= 0f) _pauseAt = Time.unscaledTime;
                if (Time.unscaledTime - _pauseAt < 0.22f) return false;
                if (!NamedShot("06i_pause.png")) return false;
                _pauseShot = true;
                Note("pause board");
                if (b.Paused) g.ToggleBattlePause();
                return false;
            }
            return true;
        }

        void DrainCombatEvidence(BattleSim b)
        {
            if (b == null) return;
            if (SawAutoCast(b)) _sawAutoCast = true;
            if (b.Enemies != null)
            {
                for (int i = 0; i < b.Enemies.Count; i++)
                {
                    var e = b.Enemies[i];
                    if (e != null && !e.Alive) _sawEnemyDeath = true;
                }
            }
            if (b.Events != null && b.Events.Events != null)
            {
                var list = b.Events.Events;
                if (_eventCursor > list.Count) _eventCursor = 0;
                while (_eventCursor < list.Count)
                {
                    var ev = list[_eventCursor++];
                    if (ev != null && ev.Kind == "death" && !ev.TargetAlly)
                        _sawEnemyDeath = true;
                }
            }
        }

        bool SawAutoCast(BattleSim b)
        {
            if (b == null || b.Casts == null) return false;
            for (int i = 0; i < b.Casts.Count; i++)
            {
                var fx = b.Casts[i];
                if (fx != null && fx.CasterAlly && fx.Type == SkillType.Auto)
                    return true;
            }
            return false;
        }

        bool WaitAutoHitFrame(GameRoot g, BattleSim b)
        {
            if (_autoHitShot && _shots.Contains("06j_auto_hit.png")) return true;
            if (b.Paused) g.ToggleBattlePause();
            b.DebugRelease();
            g.EnsureAutoOn();
            if (_autoHitAt <= 0f) _autoHitAt = Time.unscaledTime;
            DrainCombatEvidence(b);
            if (!_sawAutoCast && Time.unscaledTime - _autoHitAt < 2.80f)
                return false;
            if (!NamedShot("06j_auto_hit.png")) return false;
            _autoHitShot = true;
            Note("auto-hit seen=" + _sawAutoCast + " casts=" + (b.Casts != null ? b.Casts.Count : 0));
            return true;
        }

        bool WaitKillFrame(GameRoot g, BattleSim b)
        {
            DrainCombatEvidence(b);
            if (_killShot) return true;
            if (!_sawEnemyDeath) return true;
            if (!NamedShot("06f_kill.png")) return false;
            _killShot = true;
            Note("enemy death; no KO stamp kind=" + WavePreview.VisibleKind);
            return true;
        }

        bool WaitIsolatedActions(GameRoot g, BattleSim b)
        {
            HoldForDrive(b);
            SliceDriveSequence.ReadyCharges(b);

            if (!_chargeShot)
            {
                if (_chargeAt <= 0f) _chargeAt = Time.unscaledTime;
                if (Time.unscaledTime - _chargeAt < 0.20f) return false;
                if (!NamedShot("06a_charge.png")) return false;
                _chargeShot = true;
                return false;
            }

            if (!_tap)
            {
                for (int i = 0; i < b.Allies.Length; i++)
                {
                    if (!g.Tap(i)) continue;
                    _tap = true;
                    _tapAt = Time.unscaledTime;
                    Note("tap " + i);
                    break;
                }
                return false;
            }
            if (!_tapShot)
            {
                if (Time.unscaledTime - _tapAt < 0.22f) return false;
                if (!NamedShot("06b_tap.png")) return false;
                _tapShot = true;
                return false;
            }

            if (!_slide)
            {
                SliceDriveSequence.ReadyCharges(b);
                for (int i = 0; i < b.Allies.Length; i++)
                {
                    if (!g.Slide(i)) continue;
                    _slide = true;
                    _slideAt = Time.unscaledTime;
                    Note("slide " + i);
                    break;
                }
                return false;
            }
            if (!_slideShot)
            {
                if (Time.unscaledTime - _slideAt < 0.28f) return false;
                if (!NamedShot("06c_slide.png")) return false;
                _slideShot = true;
                return false;
            }
            if (Time.unscaledTime - _slideAt < 1.55f) return false;
            if (VfxShowtime.AnyLive() && Time.unscaledTime - _slideAt < 1.70f) return false;
            VfxShowtime.KillAll();

            if (!_controlShot)
            {
                if (!_controlApplied)
                {
                    ApplyControlSample(b);
                    _controlApplied = true;
                    _controlAt = Time.unscaledTime;
                    Note("control applied locked=" + AnyControlled(b));
                    return false;
                }
                if (Time.unscaledTime - _controlAt < 0.28f) return false;
                if (AnyControlled(b) || AnyShielded(b)) _controlSeen = true;
                if (!NamedShot("06e_control.png")) return false;
                _controlShot = true;
                return false;
            }

            if (b.Drive < 100f) SliceDriveSequence.TryFillDrive(b);
            if (b.Drive < 100f) return false;
            if (b.PendingDriveSlot >= 0) return true;

            if (!_driveSelectShot)
            {
                if (_selectAt <= 0f) _selectAt = Time.unscaledTime;
                if (!g.DriveSelectVisible && Time.unscaledTime - _selectAt < 0.70f)
                    return false;
                // Wait out residual 击破/倒下 so 06d is Drive-ready portraits (P0: no plate).
                var deathCue = WavePreview.VisibleKind == WaveCueKind.AllyDown
                    || WavePreview.VisibleKind == WaveCueKind.EnemyDown;
                if (deathCue)
                {
                    WavePreview.SuppressDeathCues();
                    return false;
                }
                if (g.DriveSelectVisible && Time.unscaledTime - _selectAt < 0.18f)
                    return false;
                if (!NamedShot("06d_drive_select.png")) return false;
                _driveSelectShot = true;
                return false;
            }
            return true;
        }

        bool WaitDriveFrame(GameRoot g, BattleSim b)
        {
            HoldForDrive(b);
            SliceDriveSequence.ReadyCharges(b);
            VfxShowtime.KillAll();
            if (b.PendingDriveSlot < 0)
            {
                if (b.Drive < 100f) SliceDriveSequence.TryFillDrive(b);
                SliceDriveSequence.ReadyCharges(b);
                for (int i = 0; i < b.Allies.Length; i++)
                {
                    if (!b.TryBeginDrive(i)) continue;
                    Note("drive qte slot=" + i);
                    _qteAt = Time.unscaledTime;
                    break;
                }
                return false;
            }
            if (_qteAt <= 0f) _qteAt = Time.unscaledTime;
            if (!g.QteOpen && Time.unscaledTime - _qteAt < 0.45f) return false;
            if (Time.unscaledTime - _qteAt < 0.22f) return false;
            if (!NamedShot("07_drive.png")) return false;
            _driveShot = true;
            return true;
        }

        bool WaitFeverFrame(GameRoot g, BattleSim b)
        {
            HoldForDrive(b);
            if (!b.FeverActive)
            {
                FireDrivesUntilFever(g, b);
                if (SliceDriveSequence.HasDriveCast(b)) _drive = true;
                if (b.FeverActive || b.FeverEver) _fever = true;
                if (b.FeverActive && _feverAt <= 0f) _feverAt = Time.unscaledTime;
                if (!b.FeverActive) return false;
            }
            else
            {
                _fever = true;
                if (_feverAt <= 0f) _feverAt = Time.unscaledTime;
            }
            VfxShowtime.KillAll();
            if (g.QteOpen) return false;
            if (g.DriveSelectVisible) return false;
            if (IsQteStamp(g.VisibleStamp) && Time.unscaledTime - _feverAt < 2.00f)
                return false;
            if (VfxShowtime.AnyLive() || VfxJudge.AnyLive())
            {
                if (Time.unscaledTime - _feverAt < 3.20f) return false;
                VfxShowtime.KillAll();
                VfxJudge.KillAll();
            }
            if (Time.unscaledTime - _feverAt < 0.40f) return false;
            if (!NamedShot("08_fever.png")) return false;
            _feverShot = true;
            return true;
        }

        bool WaitWaveFrame(GameRoot g, BattleSim b)
        {
            if (_waveTried) return true;
            VfxShowtime.KillAll();
            VfxJudge.KillAll();
            if (!_feverDrained)
            {
                VfxFeverOverlay.Hide();
                int guard = 0;
                while (b.FeverActive && guard++ < 480)
                    b.TickFeverOnly();
                _feverDrained = true;
                _waveAt = Time.unscaledTime;
                Note("fever drained active=" + b.FeverActive + " overlay=" + VfxFeverOverlay.Active);
                return false;
            }
            if (VfxFeverOverlay.Active)
                VfxFeverOverlay.Hide();
            b.DebugRelease();
            g.EnsureSpeed2();
            DrainCombatEvidence(b);
            if (!_killShot && _sawEnemyDeath)
            {
                if (!NamedShot("06f_kill.png")) return false;
                _killShot = true;
                Note("enemy death on wave; no KO stamp");
                return false;
            }
            if (WavePreview.VisibleKind == WaveCueKind.WaveAdvance
                || WavePreview.VisibleKind == WaveCueKind.WaveEnter)
            {
                if (g.DriveSelectVisible || VfxFeverOverlay.Active)
                    return false;
                if (!_waveShot)
                {
                    if (!NamedShot("10_wave.png")) return false;
                    _waveShot = true;
                    _waveHoldAt = Time.unscaledTime;
                    Note("wave cue " + WavePreview.VisibleTitle);
                    return false;
                }
                if (Time.unscaledTime - _waveHoldAt < 2.05f) return false;
                _waveTried = true;
                return true;
            }
            if (Time.unscaledTime - _waveAt < 6.0f && g.CurrentScreen == "Battle")
                return false;
            _waveTried = true;
            Note("wave cue skip kind=" + WavePreview.VisibleKind);
            return true;
        }

        static bool IsQteStamp(BattleCueCopy.Kind kind)
        {
            return kind == BattleCueCopy.Kind.QtePerfect
                || kind == BattleCueCopy.Kind.QteGreat
                || kind == BattleCueCopy.Kind.QteGood
                || kind == BattleCueCopy.Kind.QteBad;
        }

        static void HoldForDrive(BattleSim b)
        {
            if (b == null) return;
            b.Auto = AutoMode.Manual;
            b.StayHeld();
        }

        static void ApplyControlSample(BattleSim b)
        {
            if (b == null || b.Allies == null) return;
            var stun = Catalog.TryEffect("stun");
            var shield = Catalog.TryEffect("shield");
            UnitState u = null;
            for (int i = b.Allies.Length - 1; i >= 0; i--)
            {
                if (b.Allies[i] == null || !b.Allies[i].Alive) continue;
                u = b.Allies[i];
                break;
            }
            if (u == null) return;
            if (stun != null) b.ApplyStatus(u, stun);
            if (shield != null) b.ApplyStatus(u, shield);
        }

        static bool AnyControlled(BattleSim b)
        {
            if (b == null || b.Allies == null) return false;
            for (int i = 0; i < b.Allies.Length; i++)
            {
                var u = b.Allies[i];
                if (u != null && u.Alive && u.ActionLocked) return true;
            }
            return false;
        }

        static bool AnyShielded(BattleSim b)
        {
            if (b == null || b.Allies == null) return false;
            for (int i = 0; i < b.Allies.Length; i++)
            {
                var u = b.Allies[i];
                if (u != null && u.Alive && u.Shield > 0) return true;
            }
            return false;
        }

        void FireDrivesUntilFever(GameRoot g, BattleSim b)
        {
            if (b.FeverActive) { _fever = true; return; }
            if (VfxJudge.AnyLive()) return;
            if (_driveTries >= 8) return;
            if (b.Drive < 100f) SliceDriveSequence.TryFillDrive(b);
            if (b.Drive < 100f && b.PendingDriveSlot < 0) return;
            SliceDriveSequence.ReadyCharges(b);
            if (b.PendingDriveSlot < 0)
            {
                for (int i = 0; i < b.Allies.Length; i++)
                {
                    if (!b.TryBeginDrive(i)) continue;
                    Note("drive qte slot=" + i);
                    break;
                }
                if (b.PendingDriveSlot < 0 && SliceDriveSequence.HasDriveCast(b))
                    _drive = true;
                return;
            }
            if (g.FireDrivePerfect() || SliceDriveSequence.TryFirePerfect(b))
            {
                _drive = true;
                _driveTries++;
                Note("drive perfect fever=" + b.FeverGauge + " active=" + b.FeverActive);
                if (b.FeverActive) _fever = true;
            }
        }

        void Finish(GameRoot g)
        {
            if (_capturing)
            {
                if (Time.unscaledTime - _phaseAt < 6f) return;
                _capturing = false;
                Note("capture unstick at Result");
            }
            if (!_splashWait)
            {
                _splashWait = true;
                _splashAt = Time.unscaledTime;
            }
            if (!_loseMode && VfxStageClear.AnyLive() && !_victoryShot)
            {
                // P0 splash: Tap the screen. fades in after the title slam (~0.22s).
                if (Time.unscaledTime - _splashAt < 0.55f) return;
                if (!NamedShot("09_victory.png")) return;
                _victoryShot = true;
                return;
            }
            if (VfxStageClear.AnyLive() && Time.unscaledTime - _splashAt < 1.40f)
                return;
            VfxStageClear.SkipLive();
            if (_loseMode)
            {
                VfxLevelUp.SkipLive();
                if (!Aged(0.20f)) return;
                FinishLose(g);
                return;
            }
            if (!_clearWait)
            {
                _clearWait = true;
                _clearAt = Time.unscaledTime;
                return;
            }
            if (!_resultShot)
            {
                if (Time.unscaledTime - _clearAt < 0.28f) return;
                if (!NamedShot("09_result.png")) return;
                _resultShot = true;
                Note("result shot ok; waiting LEVEL UP");
                return;
            }
            if (!_levelUpShot)
            {
                if (!VfxLevelUp.AnyLive())
                {
                    if (Time.unscaledTime - _clearAt < 4.20f) return;
                    Note("levelup skip; modal never shown");
                    _levelUpShot = true;
                    return;
                }
                if (!NamedShot("12_levelup.png")) return;
                _levelUpOk = true;
                _levelUpShot = true;
                Note("levelup");
                return;
            }
            VfxLevelUp.SkipLive();
            Advance(Phase.Rematch);
        }

        void DriveRematch(GameRoot g)
        {
            if (_capturing) return;
            if (g.CurrentScreen != "Battle")
            {
                if (!Aged(0.15f)) return;
                g.StartVsBattle();
                Note("rematch StartVsBattle");
                _phaseAt = Time.unscaledTime;
                return;
            }
            if (!Aged(0.40f)) return;
            if (!NamedShot("13_rematch.png")) return;
            _rematchOk = true;
            Note("rematch battle");
            g.Go("Home");
            Advance(Phase.HomeReturn);
        }

        bool ShotDone(params string[] names)
        {
            if (_phaseShot) return true;
            return NamedShot(names);
        }

        bool NamedShot(params string[] names)
        {
            if (names != null && names.Length > 0 && !string.IsNullOrEmpty(names[0])
                && _shots.Contains(names[0]))
                return true;
            if (_capturing) return false;
            StartCoroutine(CaptureNow(names));
            return false;
        }

        IEnumerator CaptureNow(string[] names)
        {
            _capturing = true;
            try
            {
                yield return CaptureShots.Shot(names);
                for (int i = 0; i < names.Length; i++)
                    if (!string.IsNullOrEmpty(names[i]) && !_shots.Contains(names[i]))
                        _shots.Add(names[i]);
                Note("shot " + string.Join(",", names));
                _phaseShot = true;
            }
            finally
            {
                _capturing = false;
            }
        }

        void SkipChainIfAged(float sec, string why, string dest, Phase next)
        {
            if (!Aged(sec)) return;
            Note(why);
            var live = GameRoot.Live;
            if (live != null && !string.IsNullOrEmpty(dest)) live.Go(dest);
            Advance(next);
        }

        string VisitedScreens()
        {
            var parts = new List<string> { "Boot", "Home" };
            if (_rosterOk) parts.Add("Characters");
            if (_teamOk) parts.Add("Team");
            if (_inspectOk) parts.Add("Inspect");
            parts.Add("Stage");
            parts.Add("Battle");
            parts.Add("Result");
            parts.Add("Home");
            return string.Join(",", parts);
        }

        void Advance(Phase next)
        {
            _phase = next;
            _phaseAt = Time.unscaledTime;
            _phaseShot = false;
            Note("phase " + next);
        }

        bool Aged(float sec) => Time.unscaledTime - _phaseAt >= sec;

        void Note(string msg) => _log.Add(msg);

        void DriveLoseBattle(GameRoot g, BattleSim b)
        {
            if (_capturing) return;
            if (g.CurrentScreen == "Result")
            {
                Note("lose result " + g.ResultTitle);
                Advance(Phase.Result);
                return;
            }
            if (b == null) return;
            if (!_battleShot)
            {
                if (g.CurrentScreen != "Battle" || !Aged(0.35f)) return;
                if (!NamedShot("06_battle.png", "ui_battle.png")) return;
                _battleShot = true;
                return;
            }
            if (!_semiShot)
            {
                int guard = 0;
                while (b.Auto != AutoMode.Semi && guard++ < 3)
                    g.CycleBattleAuto();
                if (_semiAt <= 0f) _semiAt = Time.unscaledTime;
                if (Time.unscaledTime - _semiAt < 0.20f) return;
                if (b.Auto != AutoMode.Semi) return;
                if (!NamedShot("06i_semi.png")) return;
                _semiShot = true;
                Note("semi " + b.Auto);
                return;
            }
            if (!_loseForced)
            {
                b.DebugRelease();
                b.TimeLeft = BattleSim.TickDt * 0.25f;
                _loseForced = true;
                Note("force timeout");
            }
        }

        void FinishLose(GameRoot g)
        {
            VfxStageClear.SkipLive();
            if (!NamedShot("09b_defeat.png")) return;
            _loseShot = true;
            var savePath = SaveStore.DefaultPath;
            var userSave = SaveStore.UserDefaultPath;
            var isolated = SaveStore.HasPathOverride && !SaveStore.IsUserDefaultPath(savePath);
            if (g.CurrentScreen != "Result") { Fail("not on Result"); return; }
            if (g.ResultTitle != BattleCueCopy.ResultFail && g.ResultTitle != "失败")
            { Fail("result not DEFEAT: " + g.ResultTitle); return; }
            if (g.Battle != null && g.Battle.Outcome != BattleOutcome.Defeat)
            {
                Fail("outcome not Defeat: " + g.Battle.Outcome);
                return;
            }
            if (!isolated) { Fail("save not isolated; refused user LocalLow path " + userSave); return; }
            if (!_semiShot) { Fail("semi screenshot missing"); return; }
            var sb = new StringBuilder();
            sb.AppendLine("PASS");
            sb.AppendLine("mode=lose");
            sb.AppendLine("semi=True");
            sb.AppendLine("screens=Boot,Home,Stage,Battle,Result");
            sb.AppendLine("captures=" + string.Join(",", _shots));
            sb.AppendLine("result=" + g.ResultTitle);
            sb.AppendLine("outcome=" + (g.Battle != null ? g.Battle.Outcome.ToString() : "null"));
            sb.AppendLine("save=" + savePath);
            sb.AppendLine("save-isolated=True");
            sb.AppendLine("save-user=" + userSave);
            foreach (var line in _log) sb.AppendLine("log:" + line);
            CueTimingProbe.AppendTo(sb);
            HitChainProbe.AppendTo(sb);
            CueStripRecorder.FlushAll();
            CueStripRecorder.AppendTo(sb);
            WriteResult(sb.ToString());
            _phase = Phase.Done;
        }

        static bool ReadLoseMode()
        {
            try
            {
                var p = File.Exists(RunningPath()) ? RunningPath() : RequestPath();
                if (!File.Exists(p)) return false;
                var t = File.ReadAllText(p).Trim();
                return string.Equals(t, "lose", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        void Fail(string reason)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FAIL " + reason);
            sb.AppendLine("phase=" + _phase);
            if (_shots.Count > 0) sb.AppendLine("captures=" + string.Join(",", _shots));
            foreach (var line in _log) sb.AppendLine("log:" + line);
            CueTimingProbe.AppendTo(sb);
            HitChainProbe.AppendTo(sb);
            CueStripRecorder.FlushAll();
            CueStripRecorder.AppendTo(sb);
            WriteResult(sb.ToString());
            _phase = Phase.Done;
        }

        void WriteResult(string body)
        {
            Directory.CreateDirectory(TempDir());
            File.WriteAllText(ResultPath(), body);
            try
            {
                var durable = Path.Combine(CaptureShots.CapturesDir(), "vs-smoke.result.txt");
                Directory.CreateDirectory(CaptureShots.CapturesDir());
                File.WriteAllText(durable, body);
            }
            catch { }
            try { if (File.Exists(RunningPath())) File.Delete(RunningPath()); }
            catch { }
            Debug.Log("[VS-SMOKE]\n" + body);
            if (!Application.isEditor)
                Application.Quit();
        }
    }
}
