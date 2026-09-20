using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Resonance.App;
using Resonance.Battle;
using UnityEngine;

namespace Resonance.EditorTests
{
    // EditMode boundary fixtures. These do not stand in for ordinary startup/play evidence.
    public sealed class OriginalModeIsolationTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _go;
        GameRoot _root;
        string _legacyPath;
        string _previousOverride;
        SaveBlob _view;

        [SetUp]
        public void SetUp()
        {
            var folder = Path.Combine(Path.GetTempPath(), "original-isolation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            _legacyPath = Path.Combine(folder, "save.json");
            File.WriteAllText(_legacyPath, "PERSONAL-SAVE-SENTINEL");
            File.WriteAllText(_legacyPath + ".bak", "PERSONAL-BACKUP-SENTINEL");
            _previousOverride = SaveStore.HasPathOverride ? SaveStore.DefaultPath : null;
            SaveStore.SetPathOverride(_legacyPath);
            _go = new GameObject("original-isolation-host");
            _go.SetActive(false);
            _root = _go.AddComponent<GameRoot>();
            typeof(GameRoot).GetField("_originalMode", Hidden).SetValue(_root, true);
            _view = new SaveBlob { Gold = 777, Stone = 333, LeaderSlot = 2 };
            typeof(GameRoot).GetField("_save", Hidden).SetValue(_root, _view);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            SaveStore.SetPathOverride(_previousOverride);
            // Keep this isolated fixture directory as evidence; never recursively remove it.
        }

        [TestCase("Persist")]
        [TestCase("OnApplicationQuit")]
        [TestCase("OnDestroy")]
        public void OriginalLifecycle_DoesNotWriteTheLegacyProfile(string method)
        {
            typeof(GameRoot).GetMethod(method, Hidden).Invoke(_root, null);
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
            Assert.That(File.ReadAllText(_legacyPath + ".bak"), Is.EqualTo("PERSONAL-BACKUP-SENTINEL"));
            Assert.That(File.Exists(_legacyPath + ".tmp"), Is.False);
        }

        [TestCase("OnApplicationPause", true)]
        [TestCase("OnApplicationFocus", false)]
        public void OriginalFocusAndPause_DoNotWriteLegacyProfile(string method, bool value)
        {
            typeof(GameRoot).GetMethod(method, Hidden).Invoke(_root, new object[] { value });
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
            Assert.That(File.ReadAllText(_legacyPath + ".bak"), Is.EqualTo("PERSONAL-BACKUP-SENTINEL"));
        }

        [Test]
        public void OriginalEntry_BlocksLegacyMutationAndAutomaticOwnershipMethods()
        {
            _root.SetLeader(0);
            _root.EnsureAutoOn();
            _root.CycleBattleAuto();
            _root.StartVsBattle();
            _root.RepeatCurrentBattle();
            Assert.That(_root.FireDrivePerfect(), Is.False);
            Assert.That(_view.LeaderSlot, Is.EqualTo(2));
            Assert.That(_view.Auto, Is.EqualTo(AutoMode.Manual));
            Assert.That(_view.Gold, Is.EqualTo(777));
            Assert.That(_view.Stone, Is.EqualTo(333));
            Assert.That(_root.Battle, Is.Null);
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
        }

        [TestCase("Shop")]
        [TestCase("Summon")]
        [TestCase("Daily")]
        [TestCase("Mail")]
        [TestCase("Characters")]
        [TestCase("Stage")]
        [TestCase("Result")]
        public void OriginalNavigation_CannotEnterLegacyScreens(string destination)
        {
            _root.Go(destination);
            _root.Inspect("C001");
            typeof(GameRoot).GetMethod("StartBattleAt", Hidden).Invoke(_root, new object[] { 0 });
            Assert.That(_root.CurrentScreen, Is.EqualTo("Original"));
            Assert.That(_root.Battle, Is.Null);
            Assert.That(_view.Gold, Is.EqualTo(777));
            Assert.That(File.ReadAllText(_legacyPath), Is.EqualTo("PERSONAL-SAVE-SENTINEL"));
        }
    }
}
