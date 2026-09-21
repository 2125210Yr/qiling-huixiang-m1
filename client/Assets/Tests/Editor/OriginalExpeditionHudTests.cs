using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Resonance.App;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.EditorTests
{
    /// <summary>UI component fixtures only. They do not boot GameRoot or read/write any player profile.</summary>
    public sealed class OriginalExpeditionHudTests
    {
        GameObject _canvasObject;
        ExpeditionHud _hud;
        BattleSim _sim;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("OriginalExpeditionUiFixture", typeof(RectTransform), typeof(Canvas));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            _hud = new ExpeditionHud(_canvasObject.transform);
            _sim = RunBattleFactory.Create(RunBattleFactory.CreateInput("N7", ExpeditionContent.SinglePreset,
                260921, new[] { "A01" }, null));
        }

        [TearDown]
        public void TearDown()
        {
            _hud?.Dispose();
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        [Test]
        public void ScreenTransition_RemovesOldInteractiveRoot_AndDisposeLeavesNone()
        {
            _hud.Render(new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Home, Title = "据点" });
            var old = _hud.Root;
            _hud.Render(new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Reward, Title = "奖励" });
            Assert.That(old == null || !old.activeInHierarchy, Is.True);
            Assert.That(_canvasObject.transform.childCount, Is.EqualTo(1));
            Assert.That(_hud.Root.name, Is.EqualTo("Expedition_Reward"));
            _hud.Dispose();
            Assert.That(_hud.Root, Is.Null);
            Assert.That(_canvasObject.transform.childCount, Is.Zero);
        }

        [Test]
        public void BattleToMapTransition_RemovesSkills_AndClearsBattleFlag()
        {
            Battle();
            var old = _hud.Root;
            _hud.Render(new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Map });
            Assert.That(old == null || !old.activeInHierarchy, Is.True);
            Assert.That(_hud.IsBattle, Is.False);
            Assert.That(_hud.Root.GetComponentsInChildren<Button>(true).Any(b => b.name.StartsWith("MainSkill_")), Is.False);
        }

        [Test]
        public void FiveSkills_ForwardExactSlots_WithoutMutatingSimulation()
        {
            var calls = new List<int>();
            var before = BattleStateDigest.Of(_sim).Hash;
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel(), new ExpeditionHud.BattleActions { OnSkill = calls.Add });
            for (var slot = 0; slot < 5; slot++)
            {
                var button = Find<Button>("MainSkill_" + slot);
                Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
                button.onClick.Invoke();
            }
            Assert.That(calls, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(BattleStateDigest.Of(_sim).Hash, Is.EqualTo(before));
        }

        [Test]
        public void EnemySelection_ForwardsSlot_WithoutDirectlyChangingFocus()
        {
            var calls = new List<int>();
            var before = BattleStateDigest.Of(_sim).Hash;
            var focus = _sim.FocusEnemySlot;
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel(), new ExpeditionHud.BattleActions { OnFocus = calls.Add });
            foreach (var enemy in _sim.Enemies)
            {
                var button = Find<Button>("Enemy_" + enemy.Slot);
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
            }
            Assert.That(calls, Is.EqualTo(_sim.Enemies.Select(e => e.Slot).ToArray()));
            Assert.That(_sim.FocusEnemySlot, Is.EqualTo(focus));
            Assert.That(BattleStateDigest.Of(_sim).Hash, Is.EqualTo(before));
        }

        [Test]
        public void BattleControls_ForwardCallbacks_AndNeverChangeClockOrPause()
        {
            var pause = 0; var speed = 0; var exit = 0;
            var before = BattleStateDigest.Of(_sim).Hash;
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel(), new ExpeditionHud.BattleActions
            { OnPause = () => pause++, OnSpeed = () => speed++, OnExit = () => exit++ });
            Find<Button>("Pause").onClick.Invoke();
            Find<Button>("Speed").onClick.Invoke();
            Find<Button>("ExitBattle").onClick.Invoke();
            Assert.That(new[] { pause, speed, exit }, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(_sim.Paused, Is.False);
            Assert.That(BattleStateDigest.Of(_sim).Hash, Is.EqualTo(before));
        }

        [Test]
        public void PauseQueue_ClearIsVisibleOnlyForQueuedMembers_AndForwardsExactSlot()
        {
            var cleared = new List<int>();
            var model = new ExpeditionHud.BattleModel { QueuedCommands = new[] { null, "1. 全队屏障", null, "2. 破幕突刺", null } };
            _sim.Paused = true; // Explicit UI fixture, not natural-play evidence.
            _hud.RenderBattle(_sim, model, new ExpeditionHud.BattleActions { OnClear = cleared.Add });
            for (var i = 0; i < 5; i++)
                Assert.That(Find<Button>("ClearQueue_" + i).gameObject.activeSelf, Is.EqualTo(i == 1 || i == 3));
            Find<Button>("ClearQueue_3").onClick.Invoke();
            Assert.That(cleared, Is.EqualTo(new[] { 3 }));
            _sim.Paused = false;
            _hud.RefreshBattle(_sim, model);
            for (var i = 0; i < 5; i++) Assert.That(Find<Button>("ClearQueue_" + i).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void RefreshBattle_ReusesObjects_AndShowsExactUnitIdentityAndHealth()
        {
            Battle();
            var oldRoot = _hud.Root;
            var oldSkill = Find<Button>("MainSkill_0");
            _sim.Allies[0].Hp = 1234; // Synthetic snapshot change to test presentation mapping.
            _sim.Allies[0].Charge = 43;
            _hud.RefreshBattle(_sim, new ExpeditionHud.BattleModel());
            Assert.That(_hud.Root, Is.SameAs(oldRoot));
            Assert.That(Find<Button>("MainSkill_0"), Is.SameAs(oldSkill));
            for (var i = 0; i < 5; i++)
            {
                var unitRoot = Find<RectTransform>("Ally_" + i);
                Assert.That(unitRoot.Find("Name").GetComponent<Text>().text, Is.EqualTo(_sim.Allies[i].Def.Name));
                Assert.That(unitRoot.Find("Identity").GetComponent<Text>().text, Is.EqualTo(_sim.Allies[i].Def.Id));
                Assert.That(unitRoot.Find("HP").GetComponent<Text>().text, Is.EqualTo(_sim.Allies[i].Hp + " / " + _sim.Allies[i].MaxHp));
            }
            Assert.That(Find<RectTransform>("Ally_0").Find("Charge").GetComponent<Text>().text, Is.EqualTo("充能 43/100"));
        }

        [Test]
        public void DeadMember_DisablesSkill_AndDeadEnemyDisablesTarget()
        {
            _sim.Allies[2].Hp = 0;
            _sim.Enemies[1].Hp = 0;
            Battle();
            Assert.That(Find<Button>("MainSkill_2").interactable, Is.False);
            Assert.That(Find<Button>("Enemy_1").interactable, Is.False);
            Assert.That(Find<Button>("MainSkill_2").GetComponentInChildren<Text>().text, Is.EqualTo("已倒下"));
        }

        [Test]
        public void IntentProgress_UsesSuppliedSnapshot_AndDoesNotAdvanceOnRefresh()
        {
            var model = new ExpeditionHud.BattleModel
            { IntentTitle = "终幕回响", IntentDescription = "剩余 2.0 秒 · 全体 · 2 个面具", IntentProgress = 0.5f };
            _sim.Paused = true;
            _hud.RenderBattle(_sim, model, new ExpeditionHud.BattleActions());
            var fill = Find<RectTransform>("IntentProgress").Find("Fill").GetComponent<RectTransform>();
            for (var i = 0; i < 12; i++) _hud.RefreshBattle(_sim, model);
            Assert.That(fill.anchorMax.x, Is.EqualTo(0.5f));
            Assert.That(Find<Text>("IntentDescription").text, Is.EqualTo(model.IntentDescription));
            Assert.That(Find<Text>("IntentTitle").text, Is.EqualTo(model.IntentTitle));
        }

        [Test]
        public void RelicFeedback_UsesProvidedRealResultText_AndOnlySuppliedIconIds()
        {
            var model = new ExpeditionHud.BattleModel { RelicIds = new[] { "B01", "B02", "B04" },
                RelicState = "B 系：扩散目标 2", Feedback = "B04 有效伤害 540 · 溢出 120" };
            _hud.RenderBattle(_sim, model, new ExpeditionHud.BattleActions());
            Assert.That(Find<Text>("Feedback").text, Is.EqualTo(model.Feedback));
            Assert.That(Find<Text>("RelicState").text, Is.EqualTo(model.RelicState));
            for (var i = 0; i < 5; i++)
            {
                var icon = Find<RectTransform>("RelicIcon_" + i);
                Assert.That(icon.gameObject.activeSelf, Is.EqualTo(i < 3));
                if (i < 3) Assert.That(icon.GetComponentInChildren<Text>().text, Is.EqualTo(model.RelicIds[i]));
            }
        }

        [Test]
        public void LongDetails_ExpandInsideScroll_WithoutClippingParagraph()
        {
            var model = RewardWithLongDetails();
            _hud.Render(model);
            Layout();
            var detail = Find<Text>("ChoiceDetail");
            var scroll = Find<ScrollRect>("ContentScroll");
            var collapsedHeight = scroll.content.rect.height;
            Assert.That(detail.gameObject.activeSelf, Is.False);
            Find<Button>("Details_A02").onClick.Invoke();
            Layout();
            Assert.That(detail.gameObject.activeSelf, Is.True);
            Assert.That(detail.rectTransform.rect.height + 2, Is.GreaterThanOrEqualTo(detail.preferredHeight));
            Assert.That(scroll.content.rect.height, Is.GreaterThan(collapsedHeight + 100));
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(scroll.vertical && !scroll.horizontal, Is.True);
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Find<Button>("Details_A02").onClick.Invoke();
            Layout();
            Assert.That(detail.gameObject.activeSelf, Is.False);
            Assert.That(scroll.content.rect.height, Is.EqualTo(collapsedHeight).Within(2));
        }

        [Test]
        public void RewardSelection_ForwardsOnlySpecifiedCallback_AndDisabledChoiceStaysDisabled()
        {
            var selected = 0;
            var model = RewardWithLongDetails();
            model.Options[0].Select = () => selected++;
            model.Options.Add(new ExpeditionHud.Choice { Id = "locked", Title = "不可用", Enabled = false, Select = () => selected += 100 });
            _hud.Render(model);
            Find<Button>("Select_A02").onClick.Invoke();
            Assert.That(selected, Is.EqualTo(1));
            Assert.That(Find<Button>("Select_locked").interactable, Is.False);
        }

        [Test]
        public void Map_ShowsBothMutuallyExclusiveBranchesSideBySide()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Map };
            model.Nodes.Add(new ExpeditionHud.MapNode { Id = "N1", Title = "前厅", Visited = true });
            model.Nodes.Add(new ExpeditionHud.MapNode { Id = "N2-backstage", Title = "后台", Description = "较少耐打敌人 · A/C 倾向", Current = true });
            model.Nodes.Add(new ExpeditionHud.MapNode { Id = "N2-audience", Title = "观众席", Description = "更多脆弱敌人 · B/C 倾向", Current = true });
            _hud.Render(model);
            Layout();
            var a = Find<RectTransform>("Node_N2-backstage");
            var b = Find<RectTransform>("Node_N2-audience");
            Assert.That(a.parent, Is.SameAs(b.parent));
            Assert.That(a.parent.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(Math.Abs(a.anchoredPosition.x - b.anchoredPosition.x), Is.GreaterThan(300));
            Assert.That(a.rect.width, Is.GreaterThan(400));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Decoration_DoesNotInterceptRaycasts(bool battle)
        {
            if (battle) Battle(); else _hud.Render(RewardWithLongDetails());
            foreach (var text in _hud.Root.GetComponentsInChildren<Text>(true)) Assert.That(text.raycastTarget, Is.False, text.name);
            foreach (var graphic in _hud.Root.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget)
                    Assert.That(graphic.GetComponent<Button>() != null || graphic.name == "Viewport"
                        || graphic.GetComponentInParent<Scrollbar>() != null, Is.True, graphic.name);
        }

        [Test]
        public void ScreenTitle_GeneratesEveryVisibleGlyph_WithinReservedHeader()
        {
            _hud.Render(new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Home, Title = "失声剧院" });
            Layout();
            var title = Find<Text>("ScreenTitle");
            AssertEveryGlyphVisible(title);
            Assert.That(BoundsInRoot(title.rectTransform).Overlaps(BoundsInRoot(Find<Text>("ChapterLabel").rectTransform)), Is.False);
            Assert.That(BoundsInRoot(title.rectTransform).Overlaps(BoundsInRoot(Find<Text>("ScreenSubtitle").rectTransform)), Is.False);
        }

        [Test]
        public void BattleTitle_GeneratesEveryVisibleGlyph_WithinReservedHeader()
        {
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel { Title = "白面指挥者" }, new ExpeditionHud.BattleActions());
            Layout();
            var title = Find<Text>("BattleTitle");
            AssertEveryGlyphVisible(title);
            Assert.That(BoundsInRoot(title.rectTransform).Overlaps(BoundsInRoot(Find<Text>("BattleSubtitle").rectTransform)), Is.False);
        }

        [Test]
        public void TwoLineRelicState_GeneratesBothLines_WithoutOverlappingIcons()
        {
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel
            { RelicState = "防护反击蓄能\n过载共振已就绪", RelicIds = new[] { "A01" } }, new ExpeditionHud.BattleActions());
            Layout();
            var text = Find<Text>("RelicState");
            AssertEveryGlyphVisible(text);
            Assert.That(BoundsInRoot(text.rectTransform).Overlaps(BoundsInRoot(Find<RectTransform>("RelicIcon_0"))), Is.False);
        }

        [Test]
        public void PausedClearControls_AndNotice_DoNotOverlapOrLeavePortraitFrame()
        {
            _sim.Paused = true;
            _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel
            { QueuedCommands = new[] { "1", "2", "3", "4", "5" }, Notice = "充能不足：该条已拒绝，其余指令继续。" }, new ExpeditionHud.BattleActions());
            Layout();
            var notice = Find<Text>("BattleNotice").rectTransform;
            var noticeBounds = BoundsInRoot(notice);
            foreach (var button in _hud.Root.GetComponentsInChildren<Button>())
            {
                var bounds = BoundsInRoot(button.GetComponent<RectTransform>());
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-0.1f), button.name);
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(1080.1f), button.name);
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-1920.1f), button.name);
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(0.1f), button.name);
                if (button.name.StartsWith("ClearQueue_")) Assert.That(bounds.Overlaps(noticeBounds), Is.False, button.name);
            }
        }

        void Battle() => _hud.RenderBattle(_sim, new ExpeditionHud.BattleModel(), new ExpeditionHud.BattleActions());

        ExpeditionHud.ScreenModel RewardWithLongDetails()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Reward, Title = "选择一件临时强化" };
            var details = string.Join("\n", Enumerable.Repeat("实际护盾吸收才积蓄能；护盾过期或替换不算吸收。每次施法最多触发一次。", 34));
            model.Options.Add(new ExpeditionHud.Choice { Id = "A02", Title = "厚壁", Description = "本队基础护盾提高 25%。",
                Detail = details, Family = "A", Relationship = "接续 A01 蓄能屏障。", Select = () => { } });
            return model;
        }

        T Find<T>(string name) where T : Component
        {
            var component = _hud.Root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            Assert.That(component, Is.Not.Null, "Missing " + typeof(T).Name + ": " + name);
            return component;
        }

        void Layout()
        {
            for (var i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var scroll in _hud.Root.GetComponentsInChildren<ScrollRect>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            }
        }

        static void AssertEveryGlyphVisible(Text text)
        {
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            text.cachedTextGenerator.Invalidate();
            text.cachedTextGenerator.Populate(text.text, settings);
            var vertices = text.cachedTextGenerator.verts;
            var visibleQuads = 0;
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
                if (maxX > minX && maxY > minY) visibleQuads++;
            }
            var expected = text.text.Count(c => !char.IsWhiteSpace(c));
            var diagnostic = text.name + ": font=" + text.font.name + ", size=" + text.fontSize
                + ", box=" + text.rectTransform.rect.height + ", preferred=" + text.preferredHeight
                + ", visibleGlyphs=" + visibleQuads + ", expected=" + expected;
            Debug.Log("ORIGINAL_UI_GLYPH_CHECK " + diagnostic);
            Assert.That(visibleQuads, Is.EqualTo(expected), diagnostic);
        }

        Rect BoundsInRoot(RectTransform rect)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            var root = _hud.Root.GetComponent<RectTransform>();
            var min = root.InverseTransformPoint(points[0]); var max = root.InverseTransformPoint(points[2]);
            // Normalize from the root's center pivot to its top-left origin.
            return Rect.MinMaxRect(min.x + root.rect.width / 2, min.y - root.rect.height / 2,
                max.x + root.rect.width / 2, max.y - root.rect.height / 2);
        }
    }
}
