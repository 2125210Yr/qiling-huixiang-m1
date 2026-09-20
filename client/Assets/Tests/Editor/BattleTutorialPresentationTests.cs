using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Resonance.App;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.TestTools;

namespace Resonance.EditorTests
{
    // Exercise the real HUD and tip component without booting GameRoot or touching player saves.
    public sealed class BattleTutorialPresentationTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        GameObject _hostObject;
        GameObject _canvasObject;
        BattleHud _hud;
        BattleSim _battle;

        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("tutorial-test-host");
            _hostObject.SetActive(false); // Keep Awake / SaveStore out of the fixture.
            var host = _hostObject.AddComponent<GameRoot>();
            // C007 has implemented Tap / Slide / Drive effects in the ordinary catalog.
            _battle = new BattleSim(new[] { "C007" }, 0, 260920)
            {
                Auto = AutoMode.Manual,
                Speed = 1
            };
            Set(host, "_battle", _battle);
            Set(host, "_save", new SaveBlob());
            _canvasObject = new GameObject("tutorial-test-canvas", typeof(RectTransform), typeof(Canvas));
            _hud = new BattleHud(host, _canvasObject.transform, new List<GameObject>());
            Set(_hud, "_presentation", new NoopPresentation());
            Set(_hud, "_tipTapDelay", 0.85f);
            Set(_hud, "_castCursor", _battle.Casts.Count);
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
            if (_hostObject != null) UnityEngine.Object.DestroyImmediate(_hostObject);
        }

        [Test]
        public void Suppression_HidesImmediately_PreservesRemainingLife_AndRestoresTheSameTip()
        {
            ShowOrdinaryTip();
            var tip = Tip();
            var life = Get<float>(tip, "_life");

            Suppress(true);
            Invoke(tip, "LateUpdate");

            Assert.That(tip.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(Get<float>(tip, "_life"), Is.EqualTo(life), "A hidden tutorial must not expire during a cut.");
            Assert.That(VfxTipPlate.AnyLive(), Is.True, "Suppression retains queued content.");
            Suppress(false);
            Assert.That(Tip(), Is.SameAs(tip));
            Assert.That(tip.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
        }

        [Test]
        public void Suppression_RestoresTheRemainingFadeWithoutFlashingAtFullOpacity()
        {
            ShowOrdinaryTip();
            var tip = Tip();
            Set(tip, "_life", 0.1f);

            Suppress(true);
            Suppress(false);

            Assert.That(Get<float>(tip, "_life"), Is.EqualTo(0.1f));
            Assert.That(tip.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.1f / 0.55f).Within(0.00001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplacingATipWhileSuppressed_DoesNotExposeTheNewPlate(bool fever)
        {
            ShowOrdinaryTip();
            Suppress(true);

            if (fever) VfxTipPlate.ShowFeverActivate(_canvasObject.transform);
            else VfxTipPlate.Show(_canvasObject.transform, BattleCueCopy.TeamHpTip, BattleCueCopy.TipTeamHpBody);

            Assert.That(VfxTipPlate.AnyLive(), Is.True);
            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(VfxTipPlate.FeverTipSession(), Is.EqualTo(fever));
            Suppress(false);
            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
        }

        [Test]
        public void OrdinaryTipAfterFever_IsNotProtectedAsAFeverSession()
        {
            VfxTipPlate.ShowFeverActivate(_canvasObject.transform);
            Assert.That(VfxTipPlate.FeverTipSession(), Is.True);

            VfxTipPlate.Show(_canvasObject.transform, BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);

            Assert.That(VfxTipPlate.FeverTipSession(), Is.False);
            Assert.That(Get<bool>(Tip(), "_chainRates"), Is.False);
            var hide = typeof(VfxTipPlate).GetMethod("HideIfShowing", BindingFlags.Static | BindingFlags.Public);
            Assert.That(hide, Is.Not.Null);
            hide.Invoke(null, new object[] { BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody });
            Assert.That(VfxTipPlate.AnyLive(), Is.False, "A later ordinary lesson must remain individually dismissible.");
        }

        [Test]
        public void DestroyingThePreviousRoot_DoesNotSuppressATipOnTheNextRoot()
        {
            ShowOrdinaryTip();
            Suppress(true);
            UnityEngine.Object.DestroyImmediate(_canvasObject);
            _canvasObject = new GameObject("tutorial-test-next-canvas", typeof(RectTransform), typeof(Canvas));

            ShowOrdinaryTip();

            Assert.That(VfxTipPlate.AnyLive(), Is.True);
            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
        }

        [Test]
        public void BeginShowtime_ImmediatelySuppressesAnAlreadyVisibleTutorial()
        {
            ShowOrdinaryTip();
            var tick = _battle.TickIndex;
            var commands = _battle.CommandLog.Count;
            var hold = _battle.HoldLeftSec;

            Invoke(_hud, "BeginShowtime", null, "showtime", "slide", "skill", Color.red,
                1.47f, CombatCut.Slide, true);

            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(VfxTipPlate.AnyLive(), Is.True);
            AssertCoreUnchanged(tick, commands, hold);
        }

        [TestCase("paused")]
        [TestCase("terminal")]
        [TestCase("qte")]
        [TestCase("pending-drive")]
        public void BlockingPresentation_HidesExistingTipWithoutChangingBattleState(string blocker)
        {
            ShowOrdinaryTip();
            SetBlocker(blocker, true);
            var tick = _battle.TickIndex;
            var commands = _battle.CommandLog.Count;
            var hold = _battle.HoldLeftSec;
            var delay = Get<float>(_hud, "_tipTapDelay");

            Invoke(_hud, "TickOverlays");

            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.Zero, blocker);
            Assert.That(Get<float>(_hud, "_tipTapDelay"), Is.EqualTo(delay), "Blocked tutorial time must not advance.");
            AssertCoreUnchanged(tick, commands, hold);
            SetBlocker(blocker, false);
            Invoke(_hud, "TickOverlays");
            Assert.That(Tip().GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
        }

        [Test]
        public void Qte_DoesNotGenerateAnOverdueTip_AndRetriesAfterClosing()
        {
            Set(_hud, "_tipTapDelay", 0f);
            SetBlocker("qte", true);
            Invoke(_hud, "TickOverlays");
            Assert.That(VfxTipPlate.AnyLive(), Is.False);
            Assert.That(Get<bool>(_hud, "_tipTapShown"), Is.False);

            SetBlocker("qte", false);
            Invoke(_hud, "TickOverlays");

            Assert.That(Get<bool>(_hud, "_tipTapShown"), Is.True);
            Assert.That(VfxTipPlate.AnyLive(), Is.True);
        }

        [TestCase("_tipTapShown", "_tipTapDelay", null, 0f)]
        [TestCase("_tipTapShown", "_tipTapDelay", null, -0.1f)]
        [TestCase("_tipKeepShown", "_tipKeepDelay", "_tipTapShown", 0f)]
        [TestCase("_tipTeamHpShown", "_tipTeamHpDelay", "_tipKeepShown", 0f)]
        [TestCase("_tipChildsShown", "_tipChildsDelay", "_tipTeamHpShown", 0f)]
        [TestCase("_tipChildsMoreShown", "_tipChildsMoreDelay", "_tipChildsShown", 0f)]
        public void OverdueTutorial_RetriesAfterAnotherTipExpires(string shown, string delay, string prerequisite, float overdue)
        {
            // Earlier stages are complete; only the specified next lesson is pending.
            var stages = new[] { "_tipTapShown", "_tipKeepShown", "_tipTeamHpShown", "_tipChildsShown", "_tipChildsMoreShown" };
            foreach (var stage in stages)
            {
                if (stage == shown) break;
                Set(_hud, stage, true);
            }
            if (prerequisite != null) Set(_hud, prerequisite, true);
            Set(_hud, delay, overdue);
            ShowOrdinaryTip();
            Invoke(_hud, "TickOverlays");
            Assert.That(Get<bool>(_hud, shown), Is.False, "A live plate must not be replaced.");

            VfxTipPlate.Hide();
            Invoke(_hud, "TickOverlays");

            Assert.That(Get<bool>(_hud, shown), Is.True, "An elapsed delay is a pending lesson, not a lost callback.");
            Assert.That(VfxTipPlate.AnyLive(), Is.True);
        }

        [TestCase(SkillType.Tap)]
        [TestCase(SkillType.Slide)]
        [TestCase(SkillType.Drive)]
        public void SuccessfulPlayerSkill_ConsumesObsoleteOperationLessons(SkillType skill)
        {
            VfxTipPlate.Show(_canvasObject.transform, BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);
            var slot = ReadyAlly();
            CommandResult result;
            if (skill == SkillType.Drive)
            {
                _battle.Drive = 100f;
                var begin = _battle.Submit(BattleCommand.DriveBegin(slot));
                Assert.That(begin.Accepted, Is.True, begin.Reason.ToString());
                result = _battle.Submit(BattleCommand.DriveResolve(DriveTiming.Good));
            }
            else
            {
                var command = skill == SkillType.Slide ? BattleCommand.Slide(slot) : BattleCommand.Tap(slot);
                result = _battle.Submit(command);
            }
            Assert.That(result.Accepted, Is.True, result.Reason.ToString());
            var tick = _battle.TickIndex;
            var commands = _battle.CommandLog.Count;
            var hold = _battle.HoldLeftSec;

            if (skill == SkillType.Drive)
            {
                // DriveCrush schedules its temporary GameObject's runtime destruction. EditMode
                // reports that one known call; retain strict handling for every other log.
                LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\n"
                    + "Destroying an object in edit mode destroys it permanently.");
            }
            Invoke(_hud, "DrainCasts");

            Assert.That(Get<bool>(_hud, "_tipTapShown"), Is.True);
            Assert.That(Get<bool>(_hud, "_tipSkillReadyShown"), Is.True);
            Assert.That(Get<float>(_hud, "_tipKeepDelay"), Is.GreaterThan(0f), "Consuming Tap must still schedule the next general lesson.");
            Assert.That(VfxTipPlate.AnyLive(), Is.False, "The obsolete ordinary plate must leave immediately.");
            if (skill == SkillType.Slide)
            {
                Assert.That(Get<bool>(_hud, "_tipSlideShown"), Is.True);
                Assert.That(Get<bool>(_hud, "_tipSlideMonaShown"), Is.True);
                Assert.That(Get<bool>(_hud, "_tipSlidePowerShown"), Is.True);
            }
            if (skill == SkillType.Drive)
            {
                Assert.That(Get<bool>(_hud, "_tipDriveShown"), Is.True);
                Assert.That(Get<bool>(_hud, "_tipDriveTimingShown"), Is.True);
                Assert.That(Get<bool>(_hud, "_tipDriveTablesShown"), Is.True);
            }
            AssertCoreUnchanged(tick, commands, hold);
        }

        [Test]
        public void SuccessfulTap_KeepsUsefulTeamHpLessonVisible()
        {
            VfxTipPlate.Show(_canvasObject.transform, BattleCueCopy.TeamHpTip, BattleCueCopy.TipTeamHpBody);
            var tip = Tip();
            var result = _battle.Submit(BattleCommand.Tap(ReadyAlly()));
            Assert.That(result.Accepted, Is.True, result.Reason.ToString());

            Invoke(_hud, "DrainCasts");

            Assert.That(VfxTipPlate.AnyLive(), Is.True);
            Assert.That(Tip(), Is.SameAs(tip));
            Assert.That(tip.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
        }

        [Test]
        public void RejectedSlide_DoesNotConsumeTheInstruction()
        {
            foreach (var ally in _battle.Allies) if (ally != null) ally.Charge = 0f;
            var result = _battle.Submit(BattleCommand.Slide(0));
            Assert.That(result.Accepted, Is.False);

            Invoke(_hud, "DrainCasts");

            Assert.That(Get<bool>(_hud, "_tipTapShown"), Is.False);
            Assert.That(Get<bool>(_hud, "_tipSlideShown"), Is.False);
        }

        [TestCase(false, SkillType.Slide)]
        [TestCase(true, SkillType.Auto)]
        public void EnemySkillOrBasicAutoAttack_DoesNotConsumePlayerOperationLessons(bool ally, SkillType skill)
        {
            VfxTipPlate.Show(_canvasObject.transform, BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);
            // Feed an already-emitted presentation event; this test concerns the HUD consumer.
            _battle.Casts.Add(new CastFx { CasterAlly = ally, CasterSlot = 0, Type = skill, Name = "fixture cast" });

            Invoke(_hud, "DrainCasts");

            Assert.That(Get<bool>(_hud, "_tipTapShown"), Is.False);
            Assert.That(Get<bool>(_hud, "_tipSlideShown"), Is.False);
            Assert.That(VfxTipPlate.AnyLive(), Is.True);
        }

        [Test]
        public void SuccessfulSkill_DoesNotDiscardTheFeverTipChain()
        {
            VfxTipPlate.ShowFeverActivate(_canvasObject.transform);
            var result = _battle.Submit(BattleCommand.Slide(ReadyAlly()));
            Assert.That(result.Accepted, Is.True, result.Reason.ToString());

            Invoke(_hud, "DrainCasts");

            Assert.That(VfxTipPlate.FeverTipSession(), Is.True);
            Assert.That(Get<bool>(Tip(), "_chainRates"), Is.True);
        }

        [Test]
        public void FeverActivation_StillAdvancesToRatesAfterTemporarySuppression()
        {
            VfxTipPlate.ShowFeverActivate(_canvasObject.transform);
            var tip = Tip();
            Suppress(true);
            Invoke(tip, "LateUpdate");
            Assert.That(Get<bool>(tip, "_chainRates"), Is.True);
            Suppress(false);
            // Expire activation deterministically; the production LateUpdate owns the transition.
            Set(tip, "_life", float.Epsilon);
            Invoke(tip, "LateUpdate");

            Assert.That(VfxTipPlate.FeverTipSession(), Is.True);
            Assert.That(Get<bool>(tip, "_chainRates"), Is.False);
            Assert.That(Get<bool>(tip, "_showArt"), Is.True);
            Assert.That(Get<float>(tip, "_life"), Is.GreaterThan(4.2f));
        }

        int ReadyAlly()
        {
            for (var slot = 0; slot < _battle.Allies.Length; slot++)
            {
                if (_battle.Allies[slot] == null || !_battle.Allies[slot].Alive) continue;
                _battle.Allies[slot].Charge = 100f;
                _battle.Allies[slot].SlideCd = 0f;
                return slot;
            }
            Assert.Fail("The catalog fixture requires a living default ally.");
            return -1;
        }

        void SetBlocker(string blocker, bool enabled)
        {
            switch (blocker)
            {
                case "paused": _battle.Paused = enabled; break;
                case "terminal": _battle.Outcome = enabled ? BattleOutcome.Victory : BattleOutcome.InProgress; break;
                case "pending-drive": _battle.PendingDriveSlot = enabled ? 0 : -1; break;
                case "qte": Set(_hud, "<QteOpen>k__BackingField", enabled); break;
                default: throw new ArgumentOutOfRangeException(nameof(blocker));
            }
        }

        void AssertCoreUnchanged(int tick, int commands, float hold)
        {
            Assert.That(_battle.TickIndex, Is.EqualTo(tick));
            Assert.That(_battle.CommandLog.Count, Is.EqualTo(commands));
            Assert.That(_battle.HoldLeftSec, Is.EqualTo(hold));
        }

        void ShowOrdinaryTip() => VfxTipPlate.Show(_canvasObject.transform, "Existing lesson", "Keep this pending while presentation is busy.");
        VfxTipPlate Tip() => _canvasObject.GetComponentInChildren<VfxTipPlate>(true);

        static void Suppress(bool suppressed)
        {
            var method = typeof(VfxTipPlate).GetMethod("SetSuppressed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "VfxTipPlate needs reversible suppression, not cancellation of a queued lesson.");
            method.Invoke(null, new object[] { suppressed });
        }

        static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, Instance);
            Assert.That(info, Is.Not.Null, field);
            info.SetValue(target, value);
        }

        static T Get<T>(object target, string field)
        {
            var info = target.GetType().GetField(field, Instance);
            Assert.That(info, Is.Not.Null, field);
            return (T)info.GetValue(target);
        }

        static object Invoke(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Instance);
            Assert.That(info, Is.Not.Null, method);
            return info.Invoke(target, args);
        }

        sealed class NoopPresentation : ICharacterPresentation
        {
            public void PlayCue(PresentationCue cue) { }
        }
    }
}
