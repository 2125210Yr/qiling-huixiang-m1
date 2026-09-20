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
    /// Real controller/HUD EditMode checks with isolated committed profiles and inactive GameRoot hosts.
    /// Readiness and presentation-expiry boundaries are explicit fixtures, not ordinary-player evidence.
    /// Displayed values and trigger serials always come from actual BattleSim actions and resolutions.
    /// </summary>
    public sealed class OriginalFeedbackUiTests
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
            _folder = Path.Combine(Path.GetTempPath(), "original-feedback-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _legacyPath = Path.Combine(_folder, "save.json");
            File.WriteAllText(_legacyPath, "PERSONAL-SAVE-SENTINEL");
            File.WriteAllText(_legacyPath + ".bak", "PERSONAL-BACKUP-SENTINEL");
            _previousLegacyOverride = SaveStore.HasPathOverride ? SaveStore.DefaultPath : null;
            SaveStore.SetPathOverride(_legacyPath);
            _host = new GameObject("original-feedback-inactive-root");
            _host.SetActive(false);
            _root = _host.AddComponent<GameRoot>();
            Set("_originalMode", true);
            Set("_save", new SaveBlob());
            _canvas = new GameObject("original-feedback-canvas", typeof(RectTransform), typeof(Canvas));
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
            // Keep the isolated fixture artifacts; never recursively delete directories.
        }

        [Test]
        public void ActualRootModel_AEnergy_CHarmonyAndForte_BTargets_AndTotalsMatchAuthoritativeState()
        {
            Start("A01", "B01", "C01", "C03", "C04");
            ReadyTap(1);
            ReadyTap(2);
            Assert.That(Model().RelicState, Does.Contain("C 和声 2/3").And.Contain("强奏未储存"));
            ReadyTap(4);
            Assert.That(_root.Battle.ExpeditionRelics.ForteStored, Is.True);
            Assert.That(Model().RelicState, Does.Contain("C 和声 0/3").And.Contain("强奏已储存"));
            ReadyTap(0);
            Assert.That(_root.Battle.ExpeditionRelics.ForteStored, Is.False);
            Assert.That(Model().RelicState, Does.Contain("强奏未储存"));
            var sim = _root.Battle;
            for (var i = 0; i < 300 && sim.Outcome == BattleOutcome.InProgress && sim.ExpeditionRelics.BarrierEnergy == 0; i++) sim.Tick();
            Assert.That(sim.ExpeditionRelics.BarrierEnergy, Is.GreaterThan(0), "A energy must originate in real shield absorption.");
            Refresh();
            var facts = ExpeditionBattleFeedback.Capture(sim);
            var model = Model();
            Assert.That(model.RelicState, Does.Contain("A 蓄能 " + facts.BarrierEnergy + "/" + facts.BarrierThreshold));
            Assert.That(facts.LatestScatterTargets.Count, Is.GreaterThan(0));
            foreach (var target in facts.LatestScatterTargets)
                Assert.That(model.RelicState, Does.Contain("敌" + (target.Slot + 1) + "（" + target.EffectiveDamage + "）"));
            Assert.That(model.Feedback, Does.Contain("本场对敌 " + facts.EnemyEffectiveDamage)
                .And.Contain("己方吸收 " + facts.AllyShieldAbsorbed)
                .And.Contain("有效治疗 " + facts.AllyEffectiveHealing)
                .And.Contain("成功主动 " + facts.SuccessfulActiveCommands + " 次"));
            Assert.That(Find<Text>("RelicState").text, Is.EqualTo(model.RelicState));
            Assert.That(Find<Text>("Feedback").text, Is.EqualTo(model.Feedback));
            for (var i = 0; i < model.RelicIds.Length; i++)
                Assert.That(model.RelicTriggerSerials[i], Is.EqualTo(sim.ExpeditionRelics.GetTriggerSerial(model.RelicIds[i])));
            AssertUnchanged();
        }

        [Test]
        public void OneRealNativeAction_LightsEveryTriggeredBIcon_NotOnlyTheLastRelic()
        {
            Start("B01", "B02", "B03", "B04");
            var unlit = Icon("B04").color;
            var sim = _root.Battle;
            ReadyTap(0);
            Assert.That(sim.ExpeditionRelics.LastTriggeredRelicId, Is.EqualTo("B03"));
            Assert.That(sim.ExpeditionResolutions.Where(r => r.SourceRelicId == "B01").Select(r => r.RootActionId).Distinct().Count(), Is.EqualTo(1));
            foreach (var id in new[] { "B01", "B02", "B03" })
            {
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial(id), Is.GreaterThan(0));
                Assert.That(Icon(id).color, Is.Not.EqualTo(unlit), id + " needs its own visible highlight.");
                Assert.That(IconLabel(id).text, Is.EqualTo("◆ " + id));
            }
            Assert.That(sim.ExpeditionRelics.GetTriggerSerial("B04"), Is.EqualTo(0));
            Assert.That(Icon("B04").color, Is.EqualTo(unlit));
            Assert.That(IconLabel("B04").text, Is.EqualTo("B04"));
            AssertUnchanged();
        }

        [Test]
        public void RefreshWithoutANewTrigger_DoesNotInventASerialOrExtendAnExpiredGlow()
        {
            Start("B01", "B02", "B03");
            var unlit = Icon("B01").color;
            ReadyTap(0);
            var sim = _root.Battle;
            var serials = (long[])Model().RelicTriggerSerials.Clone();
            var commandCount = sim.CommandLog.Count;
            var state = BattleStateDigest.Of(sim).ToCanonicalString();
            Assert.That(IconLabel("B01").text, Does.StartWith("◆ "));
            // Deliberate presentation-only expiry boundary: no battle clock, serial, or runtime state is changed.
            var deadlines = (float[])typeof(ExpeditionHud).GetField("_triggerUntil", Hidden).GetValue(_hud);
            for (var i = 0; i < deadlines.Length; i++) deadlines[i] = Time.unscaledTime - 1f;
            var expired = (float[])deadlines.Clone();
            for (var i = 0; i < 3; i++) Refresh();

            Assert.That(Model().RelicTriggerSerials, Is.EqualTo(serials));
            Assert.That(deadlines, Is.EqualTo(expired), "Refreshing the same serial must not restart presentation time.");
            Assert.That(IconLabel("B01").text, Is.EqualTo("B01"));
            Assert.That(Icon("B01").color, Is.EqualTo(unlit));
            Assert.That(sim.CommandLog.Count, Is.EqualTo(commandCount));
            Assert.That(BattleStateDigest.Of(sim).ToCanonicalString(), Is.EqualTo(state));
            AssertUnchanged();
        }

        [Test]
        public void ExitAndRestartTheSavedOpening_ResetsAllRelicGlowsAndRuntimeSerials()
        {
            Start("B01", "B02", "B03", "B04");
            var unlit = Icon("B01").color;
            ReadyTap(0);
            var previous = _root.Battle;
            Assert.That(IconLabel("B01").text, Does.StartWith("◆ "));
            Click("ExitBattle", false);
            Assert.That(_root.Battle, Is.Null);
            Invoke("StartOriginalBattle", false); // Reopen the same persisted checkpoint with a fresh simulation.
            Assert.That(_root.Battle, Is.Not.SameAs(previous));
            Refresh();

            Assert.That(Model().RelicTriggerSerials.All(s => s == 0), Is.True);
            foreach (var id in Model().RelicIds)
            {
                Assert.That(_root.Battle.ExpeditionRelics.GetTriggerSerial(id), Is.EqualTo(0));
                Assert.That(Icon(id).color, Is.EqualTo(unlit));
                Assert.That(IconLabel(id).text, Is.EqualTo(id));
            }
            AssertUnchanged();
        }

        [TestCase("forte-and-scatter")]
        [TestCase("split-scatter")]
        public void ActualLongChineseFeedback_RendersEveryGlyphAndStaysInsideItsPanel(string build)
        {
            var ids = build == "forte-and-scatter" ? new[] { "A01", "B01", "C01", "C03", "C04" }
                : new[] { "A01", "B01", "B03", "C01", "C03" };
            Start(ids);
            ReadyTap(0);
            ReadyTap(1);
            ReadyTap(2);
            var model = Model();
            var state = Find<Text>("RelicState");
            var feedback = Find<Text>("Feedback");
            Assert.That(state.text, Is.EqualTo(model.RelicState));
            Assert.That(feedback.text, Is.EqualTo(model.Feedback));
            Assert.That(state.text, Does.Contain("\nB 最近散射：").And.Not.Contain("尚未触发"));
            Assert.That(feedback.text, Does.Contain("\n有效治疗"));
            if (build == "forte-and-scatter") Assert.That(state.text, Does.Contain("强奏已储存"));
            else Assert.That(ExpeditionBattleFeedback.Capture(_root.Battle).LatestScatterTargets.Count, Is.EqualTo(2));
            AssertEveryGlyphVisible(state);
            AssertEveryGlyphVisible(feedback);
            var stateBottom = -state.rectTransform.anchoredPosition.y + state.rectTransform.rect.height;
            var iconTop = -Icon(ids[0]).rectTransform.anchoredPosition.y;
            Assert.That(stateBottom, Is.LessThanOrEqualTo(iconTop), "The state label must not cover the relic icons.");
            var feedbackBottom = -feedback.rectTransform.anchoredPosition.y + feedback.rectTransform.rect.height;
            Assert.That(feedbackBottom, Is.LessThanOrEqualTo(Find<RectTransform>("RelicFeedback").rect.height));
            AssertUnchanged();
        }

        void Start(params string[] relics)
        {
            _store = new OriginalProfileStore(Path.Combine(_folder, "OriginalExpedition", "profile.v1.json"));
            var flow = new ExpeditionFlow(_store);
            flow.StartRun(relics[0], ExpeditionContent.SinglePreset, 260921);
            // Committed route/build fixture only. Stats, native skills, crit and encounter data stay authored.
            var boundary = flow.Profile;
            boundary.ActiveRun.CurrentNode = "N5";
            boundary.ActiveRun.OwnedRelicIds = (string[])relics.Clone();
            boundary.DiscoveredRelics = (string[])relics.Clone();
            _store.Save(boundary.Revision, boundary);
            flow.Reload();
            Set("_expedition", flow);
            Invoke("StartOriginalBattle", false);
            Assert.That(_root.Battle, Is.Not.Null);
            Assert.That(_root.Battle.IsOriginalExpedition, Is.True);
            _checkpointBytes = File.ReadAllBytes(_store.FilePath);
            Click("Enemy_0");
        }

        void ReadyTap(int slot)
        {
            _root.Battle.Allies[slot].Charge = 100; // Explicit command-boundary fixture.
            Refresh();
            Click("MainSkill_" + slot);
            var result = _root.Battle.CommandLog.Last();
            Assert.That(result.Kind, Is.EqualTo(BattleCommandKind.Tap));
            Assert.That(result.Accepted, Is.True, result.ToString());
        }

        void Click(string name, bool refresh = true)
        {
            var button = Find<Button>(name);
            Assert.That(button.interactable && button.gameObject.activeInHierarchy, Is.True, name);
            button.onClick.Invoke();
            if (refresh) Refresh();
        }
        void Refresh()
        {
            _hud.RefreshBattle(_root.Battle, Model());
            Canvas.ForceUpdateCanvases();
        }
        ExpeditionHud.BattleModel Model() => (ExpeditionHud.BattleModel)Invoke("OriginalBattleView");
        Image Icon(string id) => Find<Image>("RelicIcon_" + Array.IndexOf(Model().RelicIds, id));
        Text IconLabel(string id) => Icon(id).GetComponentInChildren<Text>();
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
            Assert.That(File.ReadAllBytes(_store.FilePath), Is.EqualTo(_checkpointBytes));
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
            Assert.That(File.ReadAllText(_legacyPath + ".bak"), Is.EqualTo("PERSONAL-BACKUP-SENTINEL"));
            Assert.That(File.Exists(_legacyPath + ".tmp"), Is.False);
            Assert.That(_host.activeInHierarchy, Is.False, "GameRoot must never enter its Awake/personal-save lifecycle.");
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
            Debug.Log("ORIGINAL_FEEDBACK_UI_GLYPH_CHECK " + diagnostic);
            Assert.That(visible, Is.EqualTo(expected), diagnostic);
        }
    }
}
