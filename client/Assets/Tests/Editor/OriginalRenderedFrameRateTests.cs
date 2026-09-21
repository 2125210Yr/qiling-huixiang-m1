using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Resonance.App;
using Resonance.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Resonance.EditorTests
{
    /// <summary>
    /// Opt-in, controlled real-rendering fixture, not ordinary Player play. The production
    /// GameRoot.Update owns every Tick. No captureFramerate, artificial delta, or Camera.Render.
    /// </summary>
    public sealed class OriginalRenderedFrameRateTests
    {
        const string OutputVariable = "RESONANCE_ORIGINAL_RENDERED_FPS_DIRECTORY";
        const string ProfileVariable = "RESONANCE_ORIGINAL_PROFILE";
        const string SessionKey = "Resonance.OriginalRenderedFrameRateTests.Session";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        RenderProbe _probe;

        [Serializable]
        sealed class Session
        {
            public string fixtureRoot, outputRoot, originalProfile, contentHash;
            public bool hadOriginalProfile, createdGameView;
            public int targetFrameRate, vSyncCount, gameViewId, previousWindowId;
            public float timeScale;
        }

        [Serializable]
        sealed class SourceIdentity { public string path, sha256; }

        [Serializable]
        sealed class FrameRow
        {
            public long stopwatchTicks;
            public int frame, renderedFrame, tick, areaCasts, phase, masks;
            public bool paused, casting;
            public double simulationSeconds;
            public string intentHash, remainingSecondsBits, nextIntentSecondsBits, hudTitle, hudCountdown;
        }

        [Serializable]
        sealed class ScreenshotRow
        {
            public string file, milestone;
            public int requestedFrame, requestedTick;
        }

        [Serializable]
        sealed class RunEvidence
        {
            public string scope = "controlled-Unity-PlayMode-real-rendering-not-ordinary-Player-play";
            public string result = "INCOMPLETE", inputHash, eventHash, damageHash, resolutionHash, finalStateHash;
            public string unityVersion, contentVersion, contentHash, outcome;
            public int requestedFps, endTick, actualRenderCallbacks, cameraInstanceId, screenWidth, screenHeight;
            public long stopwatchFrequency;
            public double measuredMeanFps, measuredMedianFps, observedSeconds;
            public List<FrameRow> frames = new List<FrameRow>();
            public List<ScreenshotRow> screenshots = new List<ScreenshotRow>();
            public List<string> damageEvents = new List<string>();
        }

        [Serializable]
        sealed class Comparison
        {
            public string result, scope = "O-023 rendered-frame-rate subcase";
            public double lowMeasuredFps, highMeasuredFps;
            public int commonTicks, commonCastingTicks;
            public string inputHash, commandHash, eventHash, damageHash, resolutionHash, finalStateHash;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrWhiteSpace(output))
                Assert.Ignore("Opt-in rendered test: set " + OutputVariable + " to a NEW evidence directory.");
            Assert.That(Application.isBatchMode, Is.False, "This test requires a visible Game View, not batch mode.");
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False, "Do not interrupt an existing Play Mode session.");
            Assert.That(OriginalModeSelection.UseOriginal(Environment.GetCommandLineArgs()), Is.True,
                "Legacy mode is forbidden: its Awake uses a different persistence path.");
            Assert.That(Time.captureFramerate, Is.Zero, "Artificial capture time cannot prove actual rendered frame rates.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(SessionState.GetString(SessionKey, ""), Is.Empty, "A previous fixture needs cleanup first.");
            Assert.That(Path.IsPathRooted(output), Is.True);
            output = Path.GetFullPath(output);
            Assert.That(Directory.Exists(output) || File.Exists(output), Is.False, "Never reuse or overwrite an evidence directory.");
            CheckPhysicalAncestors(output);

            var fixture = Path.Combine(Path.GetTempPath(), "original-rendered-fps-" + Guid.NewGuid().ToString("N"));
            CheckPhysicalAncestors(fixture);
            Directory.CreateDirectory(fixture);
            Directory.CreateDirectory(output);
            var previous = Environment.GetEnvironmentVariable(ProfileVariable);
            var session = new Session
            {
                fixtureRoot = fixture, outputRoot = output, originalProfile = previous,
                hadOriginalProfile = previous != null, targetFrameRate = Application.targetFrameRate,
                vSyncCount = QualitySettings.vSyncCount, timeScale = Time.timeScale,
                contentHash = ExpeditionContent.ContentHash,
                previousWindowId = EditorWindow.focusedWindow == null ? 0 : EditorWindow.focusedWindow.GetInstanceID()
            };
            SaveSession(session);
            PrepareIdenticalTemporaryCheckpoints(session);
            WriteNew(Path.Combine(output, "source-identity.json"), JsonUtility.ToJson(new SourceEvidence
            {
                contentHash = session.contentHash,
                files = new[]
                {
                    Identity(typeof(GameRoot).Assembly.Location),
                    Identity(typeof(BattleSim).Assembly.Location),
                    Identity(Path.Combine(Application.dataPath, "Scripts/Resonance.App/Core/GameRoot.Expedition.cs")),
                    Identity(Path.Combine(Application.dataPath, "Scripts/Resonance.App/Expedition/ExpeditionHud.cs")),
                    Identity(Path.Combine(Application.dataPath, "Tests/Editor/OriginalRenderedFrameRateTests.cs"))
                }
            }, true));

            // This process environment survives the Play Mode domain reload. Set it BEFORE AutoBoot/Awake.
            Environment.SetEnvironmentVariable(ProfileVariable, ProfilePath(session, 30));
            Assert.That(Environment.GetEnvironmentVariable(ProfileVariable), Is.EqualTo(ProfilePath(session, 30)));
            var gameViewType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.GameView", false))
                .FirstOrDefault(t => t != null);
            Assert.That(gameViewType, Is.Not.Null);
            session.createdGameView = Resources.FindObjectsOfTypeAll(gameViewType).Length == 0;
            var gameView = EditorWindow.GetWindow(gameViewType);
            session.gameViewId = gameView.GetInstanceID();
            SaveSession(session);
            gameView.Show();
            gameView.Focus();
            yield return new EnterPlayMode();
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator SameFrozenBossAndTickZeroCommandsMatchAtTwoActualRenderedFrameRates()
        {
            var session = LoadSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(ExpeditionContent.ContentHash, Is.EqualTo(session.contentHash));
            Assert.That(Time.captureFramerate, Is.Zero);
            var root = GameRoot.Live;
            Assert.That(root, Is.Not.Null, "Normal AutoBoot must create the real GameRoot.");
            Assert.That(root.IsOriginalMode, Is.True);
            Assert.That(root.OriginalProfile.ActiveRun.CurrentBattleCheckpoint.RunId,
                Is.EqualTo("rendered-frame-rate-controlled-fixture"), "AutoBoot must have loaded the isolated profile.");
            var camera = Camera.main;
            Assert.That(camera != null && camera.enabled && camera.gameObject.activeInHierarchy, Is.True);
            Assert.That(camera.targetTexture, Is.Null, "Render to the real Game View, not a manually rendered texture.");
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.Not.Null, "This URP probe requires the real SRP render callback.");
            var canvas = root.GetComponentInChildren<Canvas>();
            Assert.That(canvas != null && canvas.enabled && canvas.renderMode == RenderMode.ScreenSpaceOverlay, Is.True);
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);

            var runs = new List<RunEvidence>();
            var records = new List<OriginalBattleRecord>();
            foreach (var rate in new[] { 30, 120 })
            {
                var directory = Path.Combine(session.outputRoot, rate.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(directory);
                Environment.SetEnvironmentVariable(ProfileVariable, ProfilePath(session, rate));
                Invoke(root, "InitializeOriginalExpedition");
                Assert.That(root.OriginalProfile.ActiveRun.CurrentBattleCheckpoint.RunId,
                    Is.EqualTo("rendered-frame-rate-controlled-fixture"));
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = rate;
                var warmupUntil = Stopwatch.GetTimestamp() + 3L * Stopwatch.Frequency;
                while (Stopwatch.GetTimestamp() < warmupUntil) yield return null;

                var run = new RunEvidence { requestedFps = rate, unityVersion = Application.unityVersion,
                    contentVersion = ExpeditionContent.Version, contentHash = session.contentHash,
                    cameraInstanceId = camera.GetInstanceID(), screenWidth = Screen.width, screenHeight = Screen.height,
                    stopwatchFrequency = Stopwatch.Frequency };
                _probe = new RenderProbe(root, camera, run, directory);
                try
                {
                    Click(root, "Select_resume"); // Starts the frozen N7 checkpoint through the real HUD callback.
                    var sim = root.Battle;
                    Assert.That(sim, Is.Not.Null);
                    Assert.That(sim.TickIndex, Is.Zero);
                    Click(root, "Enemy_1");
                    Click(root, "Speed");
                    Click(root, "Pause");
                    Assert.That(sim.Speed, Is.EqualTo(2));
                    Assert.That(sim.Paused, Is.True);
                    Assert.That(sim.TickIndex, Is.Zero);
                    run.inputHash = ExpeditionContent.Fingerprint(sim.OpeningExpeditionInput);
                    _probe.Begin(sim);
                    var pausedHash = OriginalBattleSnapshot.Capture(sim).Hash;
                    var pauseUntil = Stopwatch.GetTimestamp() + 2L * Stopwatch.Frequency;
                    while (Stopwatch.GetTimestamp() < pauseUntil)
                    {
                        yield return null;
                        _probe.AssertHealthy();
                        Assert.That(OriginalBattleSnapshot.Capture(sim).Hash, Is.EqualTo(pausedHash),
                            "A rendered pause must not advance the authoritative clocks, charge, HP, or relic state.");
                    }
                    Assert.That(run.frames.Count(f => f.paused), Is.GreaterThanOrEqualTo(10), "Pause must actually render.");
                    Click(root, "Pause");
                    Assert.That(sim.Paused, Is.False);
                    Assert.That(sim.TickIndex, Is.Zero, "Resume is deliberately at the same logical tick in both runs.");

                    var deadline = Stopwatch.GetTimestamp() + 100L * Stopwatch.Frequency;
                    while (!sim.Settled)
                    {
                        yield return null; // GameRoot.Update, not this fixture, advances every simulation tick.
                        _probe.AssertHealthy();
                        Assert.That(Stopwatch.GetTimestamp(), Is.LessThan(deadline), "Natural battle exceeded real-time budget.");
                    }
                    Assert.That(sim.Outcome, Is.EqualTo(BattleOutcome.Defeat));
                    Assert.That(root.OriginalProfile.ActiveRun.Status, Is.EqualTo(ExpeditionStatus.BossRetry));
                    _probe.Dispose();
                    _probe = null;
                    var record = OriginalBattleRecord.Capture(sim);
                    Assert.That(record.Commands.Count, Is.EqualTo(4));
                    Assert.That(record.Commands.All(c => c.Tick == 0 && c.Accepted), Is.True);
                    CollectionAssert.AreEqual(new[] { BattleCommandKind.FocusEnemy, BattleCommandKind.SetSpeed,
                        BattleCommandKind.Pause, BattleCommandKind.Resume }, record.Commands.Select(c => c.Kind).ToArray());
                    var verification = OriginalBattleReplayer.Verify(record);
                    Assert.That(verification.Match, Is.True, string.Join(" | ", verification.Differences));
                    run.endTick = record.EndTick;
                    run.outcome = sim.Outcome.ToString();
                    run.eventHash = record.EventHash;
                    run.resolutionHash = record.ResolutionHash;
                    run.finalStateHash = record.FinalState.Hash;
                    run.damageEvents = record.Events.Where(e => e.Kind == "hit" && e.Amount > 0).Select(e => e.Canonical()).ToList();
                    Assert.That(run.damageEvents.Count, Is.GreaterThan(0));
                    run.damageHash = ExpeditionContent.Fingerprint(run.damageEvents);
                    ComputeFrameRates(run);
                    Assert.That(run.frames.Count, Is.GreaterThanOrEqualTo(100));
                    Assert.That(run.measuredMedianFps, Is.GreaterThan(8), "The Game View was not rendering at a usable sustained rate.");
                    Assert.That(run.frames.Count(f => f.casting), Is.GreaterThanOrEqualTo(15));
                    Assert.That(run.screenshots.Count, Is.EqualTo(3));
                    // CaptureScreenshot is asynchronous. Keep the normal render loop alive while files finish.
                    var imageDeadline = Stopwatch.GetTimestamp() + 5L * Stopwatch.Frequency;
                    while (run.screenshots.Any(s => !CompletePng(s.file)) && Stopwatch.GetTimestamp() < imageDeadline)
                        yield return null;
                    foreach (var shot in run.screenshots) Assert.That(CompletePng(shot.file), Is.True, shot.file);
                    WriteNew(Path.Combine(directory, "battle.original-replay.json"), record.ToJson());
                    run.result = "PASS_SINGLE_RENDER_RUN";
                    runs.Add(run);
                    records.Add(record);
                }
                finally
                {
                    _probe?.Dispose();
                    _probe = null;
                    WriteNew(Path.Combine(directory, "rendered-frames.json"), JsonUtility.ToJson(run, true));
                    WriteNew(Path.Combine(directory, "rendered-frame-intervals.csv"), FrameCsv(run));
                }
            }

            var result = Compare(runs[0], runs[1], records[0], records[1]);
            WriteNew(Path.Combine(session.outputRoot, "comparison.json"), JsonUtility.ToJson(result, true));
            TestContext.WriteLine("Rendered O-023 fixture: low={0:0.00}fps high={1:0.00}fps commonTicks={2}; evidence={3}",
                result.lowMeasuredFps, result.highMeasuredFps, result.commonTicks, session.outputRoot);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _probe?.Dispose();
            _probe = null;
            var session = LoadSession();
            if (session == null) yield break;
            RestoreFrameSettings(session);
            // Keep the temporary profile override in place until AutoBoot objects finish OnDestroy.
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            RestoreEditorSession();
        }

        [TearDown]
        public void RestoreWhenSetupFailedBeforePlayMode()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreEditorSession();
        }

        [Serializable]
        sealed class SourceEvidence { public string contentHash; public SourceIdentity[] files; }

        static void PrepareIdenticalTemporaryCheckpoints(Session session)
        {
            var template = Path.Combine(session.fixtureRoot, "template", "profile.v1.json");
            var store = new OriginalProfileStore(template);
            var flow = new ExpeditionFlow(store);
            flow.StartRun("C01", "single", 260921);
            var profile = flow.Profile;
            var run = profile.ActiveRun;
            // Explicit N6 fixture history, not a claim that four prior battles were played.
            run.RunId = "rendered-frame-rate-controlled-fixture";
            run.CurrentNode = "N6"; run.Status = ExpeditionStatus.Ready; run.BattleOrdinal = 4;
            run.VisitedNodeIds = new[] { "N0", "N1", "N2-backstage", "N3", "N4", "N5" };
            run.SettledBattleIds = new[] { run.RunId + "/N1/1", run.RunId + "/N2-backstage/2",
                run.RunId + "/N4/3", run.RunId + "/N5/4" };
            store.Save(profile.Revision, profile);
            flow.Reload();
            flow.BeginBattle(); // The exact same IDs, seed, HP, and input bytes are copied to both runs.
            foreach (var rate in new[] { 30, 120 })
            {
                var destination = ProfilePath(session, rate);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(template, destination, false);
            }
        }

        static Comparison Compare(RunEvidence low, RunEvidence high, OriginalBattleRecord a, OriginalBattleRecord b)
        {
            Assert.That(high.measuredMedianFps, Is.GreaterThanOrEqualTo(low.measuredMedianFps * 1.5),
                "Requested FPS values do not count: the measured render cadences must be materially different.");
            Assert.That(high.measuredMeanFps, Is.GreaterThanOrEqualTo(low.measuredMeanFps * 1.35));
            Assert.That(low.inputHash, Is.EqualTo(high.inputHash));
            Assert.That(a.EndTick, Is.EqualTo(b.EndTick));
            Assert.That(ExpeditionContent.Fingerprint(a.Inputs), Is.EqualTo(ExpeditionContent.Fingerprint(b.Inputs)));
            var commandHash = ExpeditionContent.Fingerprint(a.Commands);
            Assert.That(commandHash, Is.EqualTo(ExpeditionContent.Fingerprint(b.Commands)));
            Assert.That(low.eventHash, Is.EqualTo(high.eventHash));
            CollectionAssert.AreEqual(low.damageEvents, high.damageEvents, "Damage amounts AND logical event ticks must match exactly.");
            Assert.That(low.damageHash, Is.EqualTo(high.damageHash));
            Assert.That(low.resolutionHash, Is.EqualTo(high.resolutionHash));
            Assert.That(low.finalStateHash, Is.EqualTo(high.finalStateHash));
            var left = low.frames.Where(f => !f.paused).GroupBy(f => f.tick).ToDictionary(g => g.Key, g => g.Last());
            var right = high.frames.Where(f => !f.paused).GroupBy(f => f.tick).ToDictionary(g => g.Key, g => g.Last());
            var common = left.Keys.Intersect(right.Keys).OrderBy(t => t).ToArray();
            Assert.That(common.Length, Is.GreaterThanOrEqualTo(100));
            var commonCasting = 0;
            foreach (var tick in common)
            {
                var x = left[tick]; var y = right[tick];
                Assert.That(x.intentHash, Is.EqualTo(y.intentHash), "Authoritative intent differs at tick " + tick);
                Assert.That(x.hudTitle, Is.EqualTo(y.hudTitle), "Rendered title differs at tick " + tick);
                Assert.That(x.hudCountdown, Is.EqualTo(y.hudCountdown), "Rendered countdown differs at tick " + tick);
                if (x.casting) commonCasting++;
            }
            Assert.That(commonCasting, Is.GreaterThanOrEqualTo(15));
            return new Comparison { result = "PASS", lowMeasuredFps = low.measuredMedianFps,
                highMeasuredFps = high.measuredMedianFps, commonTicks = common.Length, commonCastingTicks = commonCasting,
                inputHash = low.inputHash, commandHash = commandHash, eventHash = low.eventHash,
                damageHash = low.damageHash, resolutionHash = low.resolutionHash, finalStateHash = low.finalStateHash };
        }

        sealed class RenderProbe : IDisposable
        {
            readonly GameRoot _root;
            readonly Camera _camera;
            readonly RunEvidence _run;
            readonly string _directory;
            readonly HashSet<string> _shots = new HashSet<string>();
            BattleSim _sim;
            int _lastFrame = -1;
            long _lastRender;
            Exception _error;
            bool _disposed;

            public RenderProbe(GameRoot root, Camera camera, RunEvidence run, string directory)
            {
                _root = root; _camera = camera; _run = run; _directory = directory;
                RenderPipelineManager.endCameraRendering += OnRendered;
                _lastRender = Stopwatch.GetTimestamp();
            }

            public void Begin(BattleSim sim) { _sim = sim; _lastRender = Stopwatch.GetTimestamp(); }

            void OnRendered(ScriptableRenderContext context, Camera camera)
            {
                if (camera != _camera || camera.cameraType != CameraType.Game || _sim == null || _sim.Settled
                    || Time.frameCount == _lastFrame) return;
                try
                {
                    _lastFrame = Time.frameCount;
                    _lastRender = Stopwatch.GetTimestamp();
                    var intent = _sim.OriginalIntentSnapshot;
                    var title = Find<Text>(_root, "IntentTitle").text;
                    var countdown = Find<Text>(_root, "IntentDescription").text;
                    var displayedTime = intent.IsCasting
                        ? "全队范围 · " + intent.RemainingCastSec.ToString("0.0") + " 秒后结算"
                        : "距离下一次预告 " + intent.NextIntentSec.ToString("0.0") + " 秒";
                    if (!countdown.StartsWith(displayedTime, StringComparison.Ordinal))
                        throw new InvalidOperationException("HUD countdown does not describe its authoritative rendered-tick snapshot.");
                    _run.actualRenderCallbacks++;
                    _run.frames.Add(new FrameRow { stopwatchTicks = _lastRender, frame = Time.frameCount,
                        renderedFrame = Time.renderedFrameCount, tick = _sim.TickIndex, paused = _sim.Paused,
                        simulationSeconds = intent.ElapsedSec, casting = intent.IsCasting, phase = intent.Phase,
                        areaCasts = intent.AreaCasts, masks = intent.AliveMasks, intentHash = ExpeditionContent.Fingerprint(intent),
                        remainingSecondsBits = FloatBits(intent.RemainingCastSec), nextIntentSecondsBits = FloatBits(intent.NextIntentSec),
                        hudTitle = title, hudCountdown = countdown });
                    if (intent.IsCasting) CaptureOnce("first-intent");
                    if (intent.IsCasting && intent.RemainingCastSec <= 1f) CaptureOnce("one-second-before-area");
                    if (intent.AreaCasts > 0) CaptureOnce("after-first-area");
                }
                catch (Exception error) { _error = error; }
            }

            void CaptureOnce(string milestone)
            {
                if (!_shots.Add(milestone)) return;
                var path = Path.Combine(_directory, milestone + ".png");
                if (File.Exists(path)) throw new IOException("Preserve existing screenshot: " + path);
                _run.screenshots.Add(new ScreenshotRow { file = path, milestone = milestone,
                    requestedFrame = Time.frameCount, requestedTick = _sim.TickIndex });
                // Unity schedules a real Game View capture. No offscreen Camera.Render or synthetic frame.
                ScreenCapture.CaptureScreenshot(path);
            }

            public void AssertHealthy()
            {
                if (_error != null) throw new InvalidOperationException("Actual rendering probe failed.", _error);
                Assert.That(Stopwatch.GetTimestamp() - _lastRender, Is.LessThan(8L * Stopwatch.Frequency),
                    "No real game-camera renders for eight seconds; Game View must remain visible.");
                Assert.That(Time.captureFramerate, Is.Zero);
            }

            public void Dispose()
            {
                if (_disposed) return;
                RenderPipelineManager.endCameraRendering -= OnRendered;
                _disposed = true;
            }
        }

        static void ComputeFrameRates(RunEvidence run)
        {
            Assert.That(run.frames.Count, Is.GreaterThan(1));
            var intervals = run.frames.Zip(run.frames.Skip(1), (a, b) =>
                (b.stopwatchTicks - a.stopwatchTicks) / (double)Stopwatch.Frequency).ToArray();
            Assert.That(intervals.All(t => t > 0), Is.True);
            Assert.That(run.frames.Select(f => f.renderedFrame).Distinct().Count(), Is.EqualTo(run.frames.Count));
            run.observedSeconds = intervals.Sum();
            run.measuredMeanFps = intervals.Length / run.observedSeconds;
            var ordered = intervals.OrderBy(x => x).ToArray();
            var median = ordered.Length % 2 == 0
                ? (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2d : ordered[ordered.Length / 2];
            run.measuredMedianFps = 1d / median;
        }

        static string FrameCsv(RunEvidence run)
        {
            var output = new StringBuilder("frame,renderedFrame,stopwatchTicks,intervalSeconds,tick,simulationSeconds,paused,casting,areaCasts\n");
            for (var i = 0; i < run.frames.Count; i++)
            {
                var f = run.frames[i];
                var dt = i == 0 ? 0d : (f.stopwatchTicks - run.frames[i - 1].stopwatchTicks) / (double)Stopwatch.Frequency;
                output.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3:R},{4},{5:R},{6},{7},{8}\n",
                    f.frame, f.renderedFrame, f.stopwatchTicks, dt, f.tick, f.simulationSeconds, f.paused, f.casting, f.areaCasts);
            }
            return output.ToString();
        }

        static bool CompletePng(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                var data = File.ReadAllBytes(path);
                return data.Length > 32 && data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71
                    && data[data.Length - 8] == 73 && data[data.Length - 7] == 69
                    && data[data.Length - 6] == 78 && data[data.Length - 5] == 68;
            }
            catch (IOException) { return false; }
        }

        static T Find<T>(GameRoot root, string name) where T : Component
        {
            var hud = (ExpeditionHud)typeof(GameRoot).GetField("_expeditionHud", Hidden).GetValue(root);
            var found = hud.Root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            if (found == null) throw new InvalidOperationException("Missing real HUD component: " + name);
            return found;
        }

        static void Click(GameRoot root, string name)
        {
            var button = Find<Button>(root, name);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True, name);
            button.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
        }

        static void Invoke(GameRoot root, string method) => typeof(GameRoot).GetMethod(method, Hidden).Invoke(root, null);
        static string FloatBits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8", CultureInfo.InvariantCulture);
        static string ProfilePath(Session s, int rate) => Path.Combine(s.fixtureRoot, rate.ToString(CultureInfo.InvariantCulture), "profile.v1.json");
        static Session LoadSession()
        {
            var json = SessionState.GetString(SessionKey, "");
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Session>(json);
        }
        static void SaveSession(Session s) => SessionState.SetString(SessionKey, JsonUtility.ToJson(s));
        static void RestoreFrameSettings(Session s)
        {
            Application.targetFrameRate = s.targetFrameRate;
            QualitySettings.vSyncCount = s.vSyncCount;
            Time.timeScale = s.timeScale;
        }
        static void RestoreEditorSession()
        {
            var s = LoadSession();
            if (s == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            RestoreFrameSettings(s);
            Environment.SetEnvironmentVariable(ProfileVariable, s.hadOriginalProfile ? s.originalProfile : null);
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var created = windows.FirstOrDefault(w => w.GetInstanceID() == s.gameViewId);
            var previous = windows.FirstOrDefault(w => w.GetInstanceID() == s.previousWindowId);
            if (s.createdGameView && created != null) created.Close();
            if (previous != null) previous.Focus();
            SessionState.EraseString(SessionKey);
            // Evidence and all unique Temp profile/checkpoint files are deliberately retained.
        }

        static void CheckPhysicalAncestors(string path)
        {
            for (var cursor = Path.GetFullPath(path); !string.IsNullOrEmpty(cursor); cursor = Path.GetDirectoryName(cursor))
                if ((File.Exists(cursor) || Directory.Exists(cursor))
                    && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked output/profile path is not permitted: " + cursor);
        }

        static SourceIdentity Identity(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return new SourceIdentity { path = Path.GetFullPath(path),
                    sha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() };
        }

        static void WriteNew(string path, string text)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(text);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
        }
    }
}
