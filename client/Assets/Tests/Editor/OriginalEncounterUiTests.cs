using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Resonance.App;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.EditorTests
{
    /// <summary>
    /// EditMode controller/UI boundary fixtures, not normal-play evidence. The real root builds and
    /// binds the buttons, but its host remains inactive so Awake and the personal-save lifecycle never run.
    /// Intent assertions advance the real encounter clock; they do not inject presentation snapshots.
    /// </summary>
    public sealed class OriginalEncounterUiTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _host, _canvas;
        GameRoot _root;
        ExpeditionHud _hud;
        OriginalProfileStore _store;
        string _folder, _legacyPath, _previousLegacyOverride;
        byte[] _checkpointBytes;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "original-encounter-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _legacyPath = Path.Combine(_folder, "save.json");
            File.WriteAllText(_legacyPath, "PERSONAL-SAVE-SENTINEL");
            File.WriteAllText(_legacyPath + ".bak", "PERSONAL-BACKUP-SENTINEL");
            _previousLegacyOverride = SaveStore.HasPathOverride ? SaveStore.DefaultPath : null;
            SaveStore.SetPathOverride(_legacyPath);
            _host = new GameObject("original-encounter-inactive-root");
            _host.SetActive(false);
            _root = _host.AddComponent<GameRoot>();
            Set("_originalMode", true);
            Set("_save", new SaveBlob());
            _canvas = new GameObject("original-encounter-canvas", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            _hud = new ExpeditionHud(_canvas.transform);
            Set("_expeditionHud", _hud);
        }

        [TearDown]
        public void TearDown()
        {
            _hud?.Dispose();
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas);
            SaveStore.SetPathOverride(_previousLegacyOverride);
            // Retain the isolated fixture artifacts; never recursively remove folders or user evidence.
        }

        [Test]
        public void ActualButtons_PauseQueueClearAndResume_ProduceOrderedSubmitResultsWithoutSaving()
        {
            Start("N4");
            var sim = _root.Battle;
            foreach (var ally in sim.Allies) ally.Charge = 100; // Explicit command-readiness boundary.
            Refresh();
            Click("Pause");
            Assert.That(sim.Paused, Is.True);
            Click("Enemy_0");
            Click("MainSkill_0");
            Click("MainSkill_1");
            Assert.That(sim.ExpeditionQueue.Select(c => c.ActorSlot), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(sim.ExpeditionQueue[0].RequiredEnemyGeneration, Is.EqualTo(sim.Enemies[0].InstanceGeneration));
            Assert.That(Find<Button>("ClearQueue_0").gameObject.activeInHierarchy, Is.True);
            Assert.That(Find<Text>("PauseQueue").text, Does.Contain(sim.Allies[0].Def.Name));
            Click("ClearQueue_0");
            Assert.That(Find<Button>("ClearQueue_0").gameObject.activeInHierarchy, Is.False);
            Click("MainSkill_0");
            Assert.That(sim.ExpeditionQueue.Select(c => c.ActorSlot), Is.EqualTo(new[] { 1, 0 }));
            var logStart = sim.CommandLog.Count;
            Click("Pause");

            Assert.That(sim.Paused, Is.False);
            Assert.That(sim.ExpeditionQueue, Is.Empty);
            var actual = sim.CommandLog.Skip(logStart).ToArray();
            Assert.That(actual.Select(c => c.Kind), Is.EqualTo(new[] { BattleCommandKind.Resume, BattleCommandKind.Tap, BattleCommandKind.Tap }));
            Assert.That(actual.Skip(1).Select(c => c.Slot), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(actual.All(c => c.Accepted && c.Source == CommandSource.Player), Is.True);
            Assert.That(sim.ExpeditionQueueResults.Select(c => c.Result.Seq), Is.EqualTo(actual.Skip(1).Select(c => c.Seq)));
            Assert.That(Find<Text>("BattleNotice").text, Does.Contain("2 条"));
            Assert.That(Find<Text>("PauseQueue").text, Does.Contain(sim.Allies[1].Def.Name + "已执行")
                .And.Contain(sim.Allies[0].Def.Name + "已执行"));
            Assert.That(Find<Button>("ClearQueue_1").gameObject.activeInHierarchy, Is.False);
            AssertUnchanged();
        }

        [Test]
        public void ActualButtons_AnEarlierKillRejectsTheStaleTarget_AndShowTheReasonWithLaterSuccess()
        {
            Start("N4");
            var sim = _root.Battle;
            foreach (var ally in sim.Allies) ally.Charge = 100;
            sim.Enemies[1].Hp = 1; // The first submitted skill, not the fixture, performs the actual kill.
            Refresh();
            Click("Pause");
            Click("Enemy_1");
            Click("MainSkill_0");
            Click("MainSkill_3");
            Click("MainSkill_2");
            Click("Pause");

            Assert.That(sim.Enemies[1].Alive, Is.False);
            Assert.That(sim.ExpeditionQueueResults.Select(c => c.Result.Reason),
                Is.EqualTo(new[] { CommandReject.None, CommandReject.TargetInvalid, CommandReject.None }));
            var rejected = sim.CommandLog.Last(c => c.Kind == BattleCommandKind.Tap && c.Slot == 3);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RequiredEnemySlot, Is.EqualTo(1));
            Assert.That(rejected.RequiredEnemyGeneration, Is.GreaterThan(0));
            var notice = Find<Text>("BattleNotice").text;
            Assert.That(notice, Does.Contain("原目标已失效"));
            var execution = Find<Text>("PauseQueue").text;
            Assert.That(execution, Does.Contain(sim.Allies[3].Def.Name).And.Contain("原目标已失效"));
            Assert.That(execution, Does.Contain(sim.Allies[2].Def.Name + "已执行"));
            Assert.That(sim.Allies[3].Charge, Is.EqualTo(100));
            AssertUnchanged();
        }

        [Test]
        public void ActualButtons_FiveNotChargedCommands_AllShowTheirOwnRejectionWithEveryGlyphVisible()
        {
            Start("N4");
            var sim = _root.Battle;
            Assert.That(sim.Allies.All(a => a.Charge < 100), Is.True, "Use the actual uncharged opening.");
            Click("Pause");
            for (var slot = 0; slot < 5; slot++) Click("MainSkill_" + slot);
            Assert.That(sim.ExpeditionQueue.Count, Is.EqualTo(5));
            var logStart = sim.CommandLog.Count;
            Click("Pause");

            Assert.That(sim.Paused, Is.False);
            Assert.That(sim.ExpeditionQueue, Is.Empty);
            Assert.That(sim.ExpeditionQueueResults.Count, Is.EqualTo(5));
            Assert.That(sim.ExpeditionQueueResults.All(r => !r.Result.Accepted && r.Result.Reason == CommandReject.NotCharged), Is.True);
            var attempts = sim.CommandLog.Skip(logStart).Where(c => c.Kind == BattleCommandKind.Tap).ToArray();
            Assert.That(attempts.Select(c => c.Slot), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(attempts.All(c => !c.Accepted && c.Reason == CommandReject.NotCharged), Is.True);
            var execution = Find<Text>("PauseQueue");
            Assert.That(execution.text, Is.EqualTo(Model().QueueSummary));
            foreach (var ally in sim.Allies)
                Assert.That(execution.text, Does.Contain(ally.Def.Name + "：充能尚未就绪"), "Every actor needs its own readable reason.");
            AssertEveryGlyphVisible(execution);
            Assert.That(Find<Text>("BattleNotice").text, Does.Contain("未执行 5 条").And.Contain("充能尚未就绪"));
            AssertUnchanged();
        }

        [Test]
        public void ActualExitButton_ClearsTransientQueueAndLeavesAnActiveNodeScreen()
        {
            Start("N4");
            var sim = _root.Battle;
            Click("Pause");
            Click("MainSkill_0");
            Assert.That(sim.ExpeditionQueue.Count, Is.EqualTo(1));
            Click("ExitBattle", refresh: false);

            Assert.That(_root.Battle, Is.Null);
            Assert.That(sim.ExpeditionQueue, Is.Empty);
            Assert.That(sim.ExpeditionQueueResults, Is.Empty);
            Assert.That(_hud.Root, Is.Not.Null);
            Assert.That(_hud.Root.activeInHierarchy, Is.True);
            Assert.That(_hud.Root.name, Is.Not.EqualTo("Expedition_Battle"));
            AssertUnchanged();
        }

        [TestCase("N4")]
        [TestCase("N7")]
        public void RealEncounterClock_MapsCastingSnapshotToTextAndProgress_AndFreezesThroughPauseButton(string node)
        {
            Start(node);
            var sim = _root.Battle;
            var idle = sim.OriginalIntentSnapshot;
            Assert.That(idle, Is.Not.Null);
            Assert.That(idle.Stage, Is.EqualTo(node));
            Assert.That(idle.NextIntentSec, Is.GreaterThan(0));
            Assert.That(Model().IntentProgress, Is.EqualTo(0));
            // Unmodified content and normal simulation ticks: no fabricated EncounterSnapshot or timer writes.
            for (var i = 0; i < BattleSim.TickHz * 20 && sim.Outcome == BattleOutcome.InProgress && !sim.OriginalIntentSnapshot.IsCasting; i++) sim.Tick();
            Assert.That(sim.OriginalIntentSnapshot.IsCasting, Is.True, "The real configured encounter must reach its first warning.");
            for (var i = 0; i < 3; i++) sim.Tick();
            Refresh();
            var snapshot = sim.OriginalIntentSnapshot;
            var model = Model();
            Assert.That(snapshot.IsBoss, Is.EqualTo(node == "N7"));
            Assert.That(snapshot.AreaName, Is.Not.Null.And.Not.Empty);
            Assert.That(Find<Text>("IntentTitle").text, Does.Contain(snapshot.AreaName).And.Contain("正在蓄势"));
            Assert.That(Find<Text>("IntentDescription").text, Is.EqualTo(model.IntentDescription));
            Assert.That(model.IntentDescription, Does.Contain(snapshot.RemainingCastSec.ToString("0.0") + " 秒后结算"));
            if (snapshot.IsBoss)
                Assert.That(model.IntentDescription, Does.Contain("阶段 " + snapshot.Phase).And.Contain("面具 " + snapshot.AliveMasks + "/2"));
            else Assert.That(model.IntentDescription, Does.Contain("护盾可吸收群攻"));
            var expected = 1f - snapshot.RemainingCastSec / snapshot.CastDurationSec;
            Assert.That(expected, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(model.IntentProgress, Is.EqualTo(expected).Within(0.00001f));
            Assert.That(IntentFill().anchorMax.x, Is.EqualTo(expected).Within(0.00001f));

            Click("Pause");
            var paused = sim.OriginalIntentSnapshot;
            var text = Find<Text>("IntentDescription").text;
            for (var i = 0; i < 90; i++) sim.Tick();
            Refresh();
            var after = sim.OriginalIntentSnapshot;
            Assert.That(after.ElapsedSec, Is.EqualTo(paused.ElapsedSec));
            Assert.That(after.RemainingCastSec, Is.EqualTo(paused.RemainingCastSec));
            Assert.That(after.NextIntentSec, Is.EqualTo(paused.NextIntentSec));
            Assert.That(after.ActionSerial, Is.EqualTo(paused.ActionSerial));
            Assert.That(Find<Text>("IntentDescription").text, Is.EqualTo(text));
            Assert.That(IntentFill().anchorMax.x, Is.EqualTo(expected).Within(0.00001f));
            AssertUnchanged();
        }

        [TestCase("N4")]
        [TestCase("N7")]
        public void RealRootIntentDescription_RendersEveryNonWhitespaceGlyph_WithoutCoveringItsProgressBar(string node)
        {
            Start(node);
            var text = Find<Text>("IntentDescription");
            Assert.That(text.text, Is.EqualTo(Model().IntentDescription));
            Assert.That(text.text, Does.Contain("\n"), "Exercise the actual two-line root description.");
            Canvas.ForceUpdateCanvases();
            AssertEveryGlyphVisible(text);
            // Both rectangles use top-left anchors in the same intent panel.
            var bottom = -text.rectTransform.anchoredPosition.y + text.rectTransform.rect.height;
            var progressTop = -Find<RectTransform>("IntentProgress").anchoredPosition.y;
            Assert.That(bottom, Is.LessThanOrEqualTo(progressTop), "A glyph fix must not cover the progress bar.");
            AssertUnchanged();
        }

        void Start(string node)
        {
            _store = new OriginalProfileStore(Path.Combine(_folder, "OriginalExpedition", "profile.v1.json"));
            var flow = new ExpeditionFlow(_store);
            flow.StartRun("A01", ExpeditionContent.SinglePreset, 260921);
            // Route-position fixture only; the factory and encounter definitions remain unchanged.
            var boundary = flow.Profile;
            boundary.ActiveRun.CurrentNode = node == "N7" ? "N6" : node;
            _store.Save(boundary.Revision, boundary);
            flow.Reload();
            Set("_expedition", flow);
            Invoke("StartOriginalBattle", false); // Builds the actual production callback bindings.
            Assert.That(_root.Battle, Is.Not.Null);
            Assert.That(_root.Battle.IsOriginalExpedition, Is.True);
            Assert.That(_root.Battle.OpeningExpeditionInput.Stage.Id, Is.EqualTo(node));
            Assert.That(_host.activeInHierarchy, Is.False, "Never enable the GameRoot host or run Awake.");
            _checkpointBytes = File.ReadAllBytes(_store.FilePath);
            Refresh();
        }

        void Click(string name, bool refresh = true)
        {
            var button = Find<Button>(name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
            if (refresh) Refresh();
        }

        void Refresh()
        {
            _hud.RefreshBattle(_root.Battle, Model());
            Canvas.ForceUpdateCanvases();
        }

        ExpeditionHud.BattleModel Model() => (ExpeditionHud.BattleModel)Invoke("OriginalBattleView");
        RectTransform IntentFill() => Find<RectTransform>("IntentProgress").Find("Fill").GetComponent<RectTransform>();
        object Invoke(string method, params object[] args) => typeof(GameRoot).GetMethod(method, Hidden).Invoke(_root, args);
        void Set(string field, object value) => typeof(GameRoot).GetField(field, Hidden).SetValue(_root, value);
        T Find<T>(string name) where T : Component
        {
            var found = _hud.Root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            Assert.That(found, Is.Not.Null, "Missing " + typeof(T).Name + ": " + name);
            return found;
        }

        void AssertUnchanged()
        {
            Assert.That(File.ReadAllBytes(_store.FilePath), Is.EqualTo(_checkpointBytes), "Combat UI cannot rewrite the persisted opening.");
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
            Assert.That(File.ReadAllText(_legacyPath + ".bak"), Is.EqualTo("PERSONAL-BACKUP-SENTINEL"));
            Assert.That(File.Exists(_legacyPath + ".tmp"), Is.False);
            Assert.That(_host.activeInHierarchy, Is.False);
        }

        static void AssertEveryGlyphVisible(Text text)
        {
            text.cachedTextGenerator.Invalidate();
            text.cachedTextGenerator.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
            var vertices = text.cachedTextGenerator.verts;
            var visible = 0;
            for (var i = 0; i + 3 < vertices.Count; i += 4)
            {
                var minX = float.MaxValue; var maxX = float.MinValue;
                var minY = float.MaxValue; var maxY = float.MinValue;
                for (var j = 0; j < 4; j++)
                {
                    var p = vertices[i + j].position;
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                }
                if (maxX > minX && maxY > minY) visible++;
            }
            var expected = text.text.Count(c => !char.IsWhiteSpace(c));
            var diagnostic = text.name + ": font=" + text.font.name + ", size=" + text.fontSize
                + ", box=" + text.rectTransform.rect.height + ", preferred=" + text.preferredHeight
                + ", visibleGlyphs=" + visible + ", expected=" + expected;
            Debug.Log("ORIGINAL_ENCOUNTER_GLYPH_CHECK " + diagnostic);
            Assert.That(visible, Is.EqualTo(expected), diagnostic);
        }
    }
}
