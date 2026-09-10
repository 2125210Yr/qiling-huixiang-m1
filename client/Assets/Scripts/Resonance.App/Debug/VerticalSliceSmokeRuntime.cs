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
        const float LimitSec = 120f;

        enum Phase
        {
            Boot, Characters, Team, Inspect, Stage, Battle, Result, Done
        }

        Phase _phase = Phase.Boot;
        float _phaseAt;
        float _started;
        bool _tap, _slide, _drive, _fever;
        bool _battleShot;
        bool _chargeShot;
        bool _tapShot;
        bool _slideShot;
        bool _driveSelectShot;
        bool _driveShot;
        bool _feverShot;
        bool _waveTried;
        bool _waveShot;
        bool _resultShot;
        bool _sawHome;
        bool _capturing;
        bool _phaseShot;
        int _driveTries;
        float _qteAt;
        float _feverAt;
        float _chargeAt;
        float _tapAt;
        float _slideAt;
        float _selectAt;
        float _waveAt;
        readonly List<string> _log = new List<string>(32);
        readonly List<string> _shots = new List<string>(8);

        void Start()
        {
            _started = Time.unscaledTime;
            _phaseAt = Time.unscaledTime;
            Note("smoke start");
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
            if (FindFirstObjectByType<VerticalSliceSmokeRuntime>() != null) return;
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
                    if (g.CurrentScreen == "Characters" && Aged(0.25f))
                    {
                        if (!ShotDone("02_roster.png", "02_characters.png", "ui_roster.png")) return;
                        g.Go("Team");
                        Advance(Phase.Team);
                    }
                    break;
                case Phase.Team:
                    if (g.CurrentScreen == "Team" && Aged(0.2f))
                    {
                        if (!ShotDone("04_team.png", "ui_team.png")) return;
                        g.SetLeader(1);
                        g.Inspect(Catalog.DefaultParty[0]);
                        Advance(Phase.Inspect);
                    }
                    break;
                case Phase.Inspect:
                    if (Aged(0.35f))
                    {
                        if (!ShotDone("03_inspect.png", "ui_inspect.png")) return;
                        g.Go("Stage");
                        Advance(Phase.Stage);
                    }
                    break;
                case Phase.Stage:
                    if (g.CurrentScreen == "Stage" && Aged(0.2f))
                    {
                        g.StartVsBattle();
                        HoldForDrive(g.Battle);
                        Advance(Phase.Battle);
                    }
                    break;
                case Phase.Battle:
                    DriveBattle(g);
                    break;
                case Phase.Result:
                    Finish(g);
                    break;
            }
        }

        void DriveBattle(GameRoot g)
        {
            var b = g.Battle;
            if (b != null && (!_driveShot || !_feverShot))
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

            if (!WaitIsolatedActions(g, b)) return;

            if (!_driveShot)
            {
                if (!WaitDriveFrame(g, b)) return;
                return;
            }

            if (!_feverShot)
            {
                if (!WaitFeverFrame(g, b)) return;
                return;
            }

            if (!_waveTried)
            {
                if (!WaitWaveFrame(g, b)) return;
                return;
            }

            _drive = true;
            _fever = true;
            b.HoldSim = false;
            g.EnsureSpeed2();
            g.EnsureAutoOn();
            SliceDriveSequence.ReadyCharges(b);
            for (int i = 0; i < 5; i++)
            {
                g.Tap(i);
                g.Slide(i);
            }
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
            if (Time.unscaledTime - _slideAt < 1.25f) return false;
            VfxShowtime.KillAll();

            if (b.Drive < 100f) SliceDriveSequence.TryFillDrive(b);
            if (b.Drive < 100f) return false;
            if (b.PendingDriveSlot >= 0) return true;

            if (!_driveSelectShot)
            {
                if (_selectAt <= 0f) _selectAt = Time.unscaledTime;
                if (!g.DriveSelectVisible && Time.unscaledTime - _selectAt < 0.70f)
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
            if (IsQteStamp(g.VisibleStamp) && Time.unscaledTime - _feverAt < 1.40f)
                return false;
            if (VfxShowtime.AnyLive() || VfxJudge.AnyLive())
            {
                if (Time.unscaledTime - _feverAt < 1.55f) return false;
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
            b.HoldSim = false;
            g.EnsureSpeed2();
            if (_waveAt <= 0f) _waveAt = Time.unscaledTime;
            if (WavePreview.VisibleKind != WaveCueKind.None)
            {
                if (!NamedShot("10_wave.png")) return false;
                _waveShot = true;
                _waveTried = true;
                Note("wave cue " + WavePreview.VisibleTitle);
                return true;
            }
            if (Time.unscaledTime - _waveAt < 4.0f && g.CurrentScreen == "Battle")
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

        void FireDrivesUntilFever(GameRoot g, BattleSim b)
        {
            if (b.FeverActive) { _fever = true; return; }
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
            if (!Aged(0.35f)) return;
            if (!NamedShot("09_result.png")) return;
            _resultShot = true;
            var savePath = SaveStore.DefaultPath;
            var userSave = SaveStore.UserDefaultPath;
            var isolated = SaveStore.HasPathOverride && !SaveStore.IsUserDefaultPath(savePath);
            var saveExists = File.Exists(savePath);
            var text = saveExists ? File.ReadAllText(savePath) : "";
            var cleared = g.SaveData != null && g.SaveData.Vs1Cleared;
            var screens = "Boot,Home,Characters,Team,Inspect,Stage,Battle,Result";
            if (g.CurrentScreen != "Result") { Fail("not on Result"); return; }
            if (!_tap) { Fail("tap never fired"); return; }
            if (!_slide) { Fail("slide never fired"); return; }
            if (!_drive) { Fail("drive never fired"); return; }
            if (!_fever) { Fail("fever never triggered"); return; }
            if (!isolated) { Fail("save not isolated; refused user LocalLow path " + userSave); return; }
            if (SaveStore.IsUserDefaultPath(savePath)) { Fail("save path is user LocalLow"); return; }
            if (!saveExists) { Fail("save.json missing at " + savePath); return; }
            if (!_battleShot) { Fail("battle screenshot missing"); return; }
            if (!_chargeShot) { Fail("charge screenshot missing"); return; }
            if (!_tapShot) { Fail("tap screenshot missing"); return; }
            if (!_slideShot) { Fail("slide screenshot missing"); return; }
            if (!_driveSelectShot) { Fail("drive-select screenshot missing"); return; }
            if (!_driveShot) { Fail("drive screenshot missing"); return; }
            if (!_feverShot) { Fail("fever screenshot missing"); return; }
            if (!_resultShot) { Fail("result screenshot missing"); return; }

            var sb = new StringBuilder();
            sb.AppendLine("PASS");
            sb.AppendLine("screens=" + screens);
            sb.AppendLine("captures=" + string.Join(",", _shots));
            sb.AppendLine("result=" + g.ResultTitle);
            sb.AppendLine("cleared=" + cleared);
            sb.AppendLine("tap=" + _tap + " slide=" + _slide + " drive=" + _drive + " fever=" + _fever);
            sb.AppendLine("wave=" + _waveShot + " drive-select=" + _driveSelectShot);
            sb.AppendLine("save=" + savePath);
            sb.AppendLine("save-isolated=True");
            sb.AppendLine("save-user=" + userSave);
            sb.AppendLine("save-json=" + text);
            foreach (var line in _log) sb.AppendLine("log:" + line);
            WriteResult(sb.ToString());
            _phase = Phase.Done;
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
            yield return CaptureShots.Shot(names);
            for (int i = 0; i < names.Length; i++)
                if (!string.IsNullOrEmpty(names[i]) && !_shots.Contains(names[i]))
                    _shots.Add(names[i]);
            Note("shot " + string.Join(",", names));
            _phaseShot = true;
            _capturing = false;
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

        void Fail(string reason)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FAIL " + reason);
            sb.AppendLine("phase=" + _phase);
            if (_shots.Count > 0) sb.AppendLine("captures=" + string.Join(",", _shots));
            foreach (var line in _log) sb.AppendLine("log:" + line);
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
