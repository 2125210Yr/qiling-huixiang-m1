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
    /// Recovery boundary fixtures. The prior-content profile is validly committed using the real
    /// atomic store, but its frozen run cannot be interpreted using the current balance table.
    /// GameRoot remains inactive: these tests never execute its normal personal-save lifecycle.
    /// </summary>
    public sealed class OriginalExpeditionRecoveryTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _host, _canvas;
        GameRoot _root;
        ExpeditionHud _hud;
        ExpeditionFlow _flow;
        OriginalProfileStore _store;
        OriginalProfile _saved;
        byte[] _profileBytes, _backupBytes;
        string _legacyPath, _previousLegacyOverride;

        [SetUp]
        public void SetUp()
        {
            var directory = Path.Combine(Path.GetTempPath(), "original-recovery-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _legacyPath = Path.Combine(directory, "save.json");
            File.WriteAllText(_legacyPath, "PERSONAL-SAVE-SENTINEL");
            File.WriteAllText(_legacyPath + ".bak", "PERSONAL-BACKUP-SENTINEL");
            _previousLegacyOverride = SaveStore.HasPathOverride ? SaveStore.DefaultPath : null;
            SaveStore.SetPathOverride(_legacyPath);

            _store = new OriginalProfileStore(Path.Combine(directory, "OriginalExpedition", "profile.v1.json"));
            var initial = new ExpeditionFlow(_store);
            initial.StartRun("A01", ExpeditionContent.SinglePreset, 260921);
            var priorContent = initial.Profile;
            priorContent.ContentVersion = "original-expedition-prior-balance-fixture";
            priorContent.ActiveRun.FrozenContentHash = "prior-content-hash-ui-recovery-fixture";
            priorContent.DiscoveredRelics = new[] { "A01", "B01", "C01" };
            // These were legal HP in an older balance. Persistence must preserve them until the
            // player explicitly ends the incompatible run; current MaxHP is not a migration rule.
            var currentMaxHp = ExpeditionContent.GetPartyMaxHp(ExpeditionContent.SinglePreset);
            for (var i = 0; i < currentMaxHp.Length; i++) priorContent.ActiveRun.PartyHp[i] = currentMaxHp[i] + 10000;
            _saved = _store.Save(priorContent.Revision, priorContent);
            _profileBytes = File.ReadAllBytes(_store.FilePath);
            _backupBytes = File.ReadAllBytes(_store.FilePath + ".bak");
            _flow = new ExpeditionFlow(_store);

            _host = new GameObject("original-recovery-ui-host");
            _host.SetActive(false);
            _root = _host.AddComponent<GameRoot>();
            Set("_originalMode", true);
            Set("_save", new SaveBlob());
            Set("_expedition", _flow);
            _canvas = new GameObject("original-recovery-ui-canvas", typeof(RectTransform), typeof(Canvas));
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
            // Preserve the isolated fixture folder; never recursively remove data or user evidence.
        }

        [Test]
        public void PriorContentWithHigherHp_RendersIncompatibilityAndRecoveryActions_WithoutSaving()
        {
            ShowRecovery();

            AssertRecoveryUi();
            AssertUnchanged();
            Assert.That(_root.Battle, Is.Null, "An incompatible run must not be silently rebuilt with current balance.");
            Assert.That(_flow.Profile.ActiveRun.PartyHp, Is.EqualTo(_saved.ActiveRun.PartyHp));
            Assert.That(_flow.Profile.ActiveRun.FrozenContentHash, Is.EqualTo(_saved.ActiveRun.FrozenContentHash));
            ActionButton("reload").onClick.Invoke();
            AssertRecoveryUi();
            AssertUnchanged();
            var reloaded = _store.Load();
            Assert.That(reloaded.Revision, Is.EqualTo(_saved.Revision));
            Assert.That(reloaded.ActiveRun.RunId, Is.EqualTo(_saved.ActiveRun.RunId));
            Assert.That(reloaded.ActiveRun.PartyHp, Is.EqualTo(_saved.ActiveRun.PartyHp));
        }

        [Test]
        public void ExplicitEndRun_FromIncompatibleRecoveryUi_CommitsOnceAndPreservesDiscoveries()
        {
            ShowRecovery();
            AssertUnchanged();

            ActionButton("end_run").onClick.Invoke();

            var after = _store.Load();
            Assert.That(after.Revision, Is.EqualTo(_saved.Revision + 1));
            Assert.That(after.ActiveRun, Is.Null);
            Assert.That(_flow.Profile.ActiveRun, Is.Null);
            Assert.That(after.DiscoveredRelics, Is.EqualTo(_saved.DiscoveredRelics));
            Assert.That(after.UnlockedPresets, Is.EqualTo(_saved.UnlockedPresets));
            Assert.That(after.ClearedChapters, Is.EqualTo(_saved.ClearedChapters));
            Assert.That(after.LastRunSummary, Is.Not.Null);
            Assert.That(after.LastRunSummary.RunId, Is.EqualTo(_saved.ActiveRun.RunId));
            Assert.That(after.LastRunSummary.Victory, Is.False);
            Assert.That(after.LastRunSummary.UnlockedNewPreset, Is.False);
            Assert.That(after.LastRunSummary.RelicIds, Is.EqualTo(_saved.ActiveRun.OwnedRelicIds));
            Assert.That(_hud.Root, Is.Not.Null, "Explicit recovery must leave an actionable screen.");
            AssertLegacyUnchanged();
        }

        void ShowRecovery()
        {
            Assert.DoesNotThrow(() => typeof(GameRoot).GetMethod("ShowOriginalExpedition", Hidden).Invoke(_root, null),
                "The recovery screen must be selected before AddOriginalParty validates old HP against current MaxHP.");
        }

        void AssertRecoveryUi()
        {
            Assert.That(_hud.Root, Is.Not.Null);
            var text = string.Join("\n", _hud.Root.GetComponentsInChildren<Text>().Select(t => t.text));
            Assert.That(text.Contains("不兼容") || (text.Contains("版本") &&
                (text.Contains("无法") || text.Contains("不能") || text.Contains("不匹配"))), Is.True,
                "The visible screen must explain content-version incompatibility, not call it player defeat or generic failure.");
            Assert.That(ActionButton("reload").interactable, Is.True);
            Assert.That(ActionButton("end_run").interactable, Is.True);
            Assert.That(_hud.Root.GetComponentsInChildren<Button>().Any(b => b.interactable &&
                (b.name == "Select_begin" || b.name == "Select_resume" || b.name == "Select_retry" || b.name == "Select_begin_boss")), Is.False);
        }

        Button ActionButton(string id)
        {
            Assert.That(_hud.Root, Is.Not.Null);
            var button = _hud.Root.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Select_" + id || b.name == "Action_" + id);
            Assert.That(button, Is.Not.Null, "Missing explicit recovery action: " + id);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, id);
            return button;
        }

        void AssertUnchanged()
        {
            Assert.That(File.ReadAllBytes(_store.FilePath), Is.EqualTo(_profileBytes), "Viewing recovery cannot rewrite the profile.");
            Assert.That(File.ReadAllBytes(_store.FilePath + ".bak"), Is.EqualTo(_backupBytes), "Viewing recovery cannot rewrite the last valid backup.");
            Assert.That(_flow.Profile.Revision, Is.EqualTo(_saved.Revision));
            Assert.That(_flow.Profile.DiscoveredRelics, Is.EqualTo(_saved.DiscoveredRelics));
            AssertLegacyUnchanged();
        }

        void AssertLegacyUnchanged()
        {
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
            Assert.That(File.ReadAllText(_legacyPath + ".bak"), Is.EqualTo("PERSONAL-BACKUP-SENTINEL"));
            Assert.That(File.Exists(_legacyPath + ".tmp"), Is.False);
        }

        void Set(string field, object value) => typeof(GameRoot).GetField(field, Hidden).SetValue(_root, value);
    }
}
