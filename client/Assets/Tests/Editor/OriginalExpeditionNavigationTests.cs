using System.Linq;
using NUnit.Framework;
using Resonance.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.EditorTests
{
    // Real Unity layout fixtures; these do not replace ordinary Player navigation evidence.
    public sealed class OriginalExpeditionNavigationTests
    {
        GameObject _canvas;
        ExpeditionHud _hud;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("OriginalNavigationUiFixture", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            _hud = new ExpeditionHud(_canvas.transform);
        }

        [TearDown]
        public void TearDown()
        {
            _hud?.Dispose();
            if (_canvas != null) Object.DestroyImmediate(_canvas);
        }

        [Test]
        public void LongMap_CurrentChoiceIsClickableInInitialViewportBeforeRouteAndOwnedRelics()
        {
            var selected = 0;
            var model = LongMap();
            model.Options[0].Select = () => selected++;
            _hud.Render(model);
            Layout();

            var scroll = Find<ScrollRect>("ContentScroll");
            var button = Find<Button>("Select_begin");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height), "The fixture must exercise a scrollable screen.");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f), "A newly opened map starts at its top.");
            AssertFullyInside(button.GetComponent<RectTransform>(), scroll.viewport);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            Assert.That(button.targetGraphic, Is.Not.Null);
            Assert.That(button.targetGraphic.raycastTarget, Is.True);

            var choice = BoundsIn(Find<RectTransform>("Choice_begin"), scroll.content);
            var party = BoundsIn(Find<RectTransform>("Party"), scroll.content);
            Assert.That(party.yMin, Is.GreaterThanOrEqualTo(choice.yMax), "The current action follows the party summary.");
            foreach (var node in model.Nodes)
                Assert.That(BoundsIn(Find<RectTransform>("Node_" + node.Id), scroll.content).yMax,
                    Is.LessThan(choice.yMin), "Current action must precede route node " + node.Id);
            foreach (var relic in model.Relics)
                Assert.That(BoundsIn(Find<RectTransform>("Choice_" + relic.Id), scroll.content).yMax,
                    Is.LessThan(choice.yMin), "Current action must precede owned relic " + relic.Id);

            button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            Assert.That(selected, Is.EqualTo(1), "The visible choice forwards the ordinary button click exactly once.");
        }

        [Test]
        public void LongMap_VisibleScrollbarReachesBottomWithoutCoveringViewportContent()
        {
            _hud.Render(LongMap());
            Layout();
            var scroll = Find<ScrollRect>("ContentScroll");
            var scrollbar = Find<Scrollbar>("ContentScrollbar");
            Assert.That(scroll.verticalScrollbar, Is.SameAs(scrollbar));
            Assert.That(scroll.verticalScrollbarVisibility, Is.EqualTo(ScrollRect.ScrollbarVisibility.AutoHide));
            Assert.That(scrollbar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop));
            Assert.That(scrollbar.gameObject.activeInHierarchy && scrollbar.IsInteractable(), Is.True);
            Assert.That(scrollbar.targetGraphic, Is.Not.Null);
            Assert.That(scrollbar.targetGraphic.raycastTarget, Is.True);
            Assert.That(scrollbar.targetGraphic.color.a, Is.GreaterThan(0));
            Assert.That(scrollbar.handleRect, Is.Not.Null);
            Assert.That(scrollbar.size, Is.GreaterThan(0).And.LessThan(1), "A long page needs a nonzero draggable handle.");

            var root = _hud.Root.GetComponent<RectTransform>();
            var viewportBounds = BoundsIn(scroll.viewport, root);
            var trackBounds = BoundsIn(scrollbar.GetComponent<RectTransform>(), root);
            var handleBounds = BoundsIn(scrollbar.handleRect, root);
            Assert.That(trackBounds.width, Is.GreaterThan(0));
            Assert.That(trackBounds.height, Is.GreaterThan(0));
            Assert.That(handleBounds.width, Is.GreaterThan(0));
            Assert.That(handleBounds.height, Is.GreaterThan(0));
            Assert.That(trackBounds.xMin, Is.GreaterThanOrEqualTo(viewportBounds.xMax), "Reserve a separate right-side track, outside the masked content.");
            Assert.That(handleBounds.xMin, Is.GreaterThanOrEqualTo(viewportBounds.xMax), "The handle must not cover text or buttons.");
            AssertFullyInside(scrollbar.GetComponent<RectTransform>(), scroll.GetComponent<RectTransform>());
            AssertFullyInside(scrollbar.handleRect, scrollbar.GetComponent<RectTransform>());

            var lastRelic = Find<RectTransform>("Choice_C04");
            var initially = BoundsIn(lastRelic, scroll.viewport);
            Assert.That(initially.yMax, Is.LessThan(scroll.viewport.rect.yMin), "The last owned relic starts below the viewport.");
            var contentBefore = scroll.content.anchoredPosition;

            // Exercise the public bound control; do not move the ScrollRect or content directly.
            scrollbar.value = 0;
            Layout();

            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0).Within(0.001f));
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(contentBefore.y));
            AssertFullyInside(lastRelic, scroll.viewport);
            Assert.That(BoundsIn(Find<Button>("Select_begin").GetComponent<RectTransform>(), scroll.viewport).yMin,
                Is.GreaterThan(scroll.viewport.rect.yMax), "The upper action scrolls away while the lower information becomes visible.");
            Assert.That(BoundsIn(scrollbar.handleRect, root).xMin, Is.GreaterThanOrEqualTo(viewportBounds.xMax));
        }

        static ExpeditionHud.ScreenModel LongMap()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Map,
                Title = "失声剧院", Subtitle = "查看队伍后继续当前节点" };
            for (var i = 0; i < 5; i++)
                model.Party.Add(new ExpeditionHud.PartyMember { Id = "P" + i, Name = "队员" + (i + 1),
                    Role = "契灵", Skill = "主要技能", Hp = 1200, MaxHp = 2000 });
            foreach (var id in new[] { "N1", "N2-backstage", "N2-audience", "N3", "N4", "N5", "N6", "N7" })
                model.Nodes.Add(new ExpeditionHud.MapNode { Id = id, Title = "路线节点 " + id,
                    Description = "查看节点说明，战斗后继续前行。", Current = id == "N6", Visited = id != "N6" && id != "N7" });
            foreach (var id in new[] { "A01", "A02", "C01", "C04" })
                model.Relics.Add(new ExpeditionHud.Choice { Id = id, Title = "已持有强化 " + id,
                    Description = "本趟远征已经取得的强化，效果持续到本次远征结束。", Family = id.Substring(0, 1) });
            model.Options.Add(new ExpeditionHud.Choice { Id = "begin", Title = "进入当前节点",
                Description = "生命与当前构筑带入本场", Select = () => { } });
            return model;
        }

        T Find<T>(string name) where T : Component
        {
            var component = _hud.Root.GetComponentsInChildren<T>(true).FirstOrDefault(candidate => candidate.name == name);
            Assert.That(component, Is.Not.Null, "Missing " + typeof(T).Name + ": " + name);
            return component;
        }

        void Layout()
        {
            for (var i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var scroll in _hud.Root.GetComponentsInChildren<ScrollRect>(true))
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                    scroll.Rebuild(CanvasUpdate.PostLayout);
                }
            }
        }

        static void AssertFullyInside(RectTransform element, RectTransform container)
        {
            var bounds = BoundsIn(element, container);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(container.rect.xMin - 0.5f), element.name + " left edge");
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(container.rect.xMax + 0.5f), element.name + " right edge");
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(container.rect.yMin - 0.5f), element.name + " bottom edge");
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(container.rect.yMax + 0.5f), element.name + " top edge");
        }

        static Rect BoundsIn(RectTransform element, RectTransform container)
        {
            var corners = new Vector3[4];
            element.GetWorldCorners(corners);
            var min = container.InverseTransformPoint(corners[0]);
            var max = container.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
