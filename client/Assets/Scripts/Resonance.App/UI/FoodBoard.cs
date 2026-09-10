using System;
using System.Collections.Generic;
using System.Globalization;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 用餐 overlay：三餐入核（余烬羹 / 契核饼 / 潮汐粥）。金/虚空铬。不画马赛克、不画六页签。
    /// </summary>
    public static class FoodBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 148f;

        public static void Draw(Transform parent, int selected, Action onClose, Action<int> onEat)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "FoodBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.54f), new Vector2(920, 820));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 792));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            var meals = Food.All;
            var n = meals != null ? meals.Length : 0;
            for (int i = 0; i < n; i++)
                DrawRow(panel, built, meals[i], 0.70f - i * 0.22f, i, selected == i, onEat);

            OverlayDraw.Label(panel, built, "点用餐入核  ·  战攻一时", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "用餐", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "入核", 15, VisualTokens.GoldMetal,
                new Vector2(0.62f, 0.935f), new Vector2(120, 32), true, false);
            OverlayDraw.Label(panel, built, "三餐入核  ·  战攻一时", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, Meal meal, float y, int index, bool eaten, Action<int> onEat)
        {
            var gold = !string.IsNullOrEmpty(meal.Name);
            var go = new GameObject(gold ? meal.Name : "void", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(CardW, CardH);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFillAlt;
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = gold ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.022f, 0.5f),
                new Vector2(8f, CardH - 36f), gold ? VisualTokens.GoldSelect : VisualTokens.SlotRim);
            spine.raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "hex", new Vector2(0.08f, 0.72f), new Vector2(22, 22),
                gold ? MarkTint(index) : VisualTokens.RailIcon, Mark(index)).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, (index + 1).ToString("00"), 14,
                gold ? VisualTokens.GoldMetal : VisualTokens.TextMuted,
                new Vector2(0.12f, 0.72f), new Vector2(48, 28), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, gold ? meal.Name : "虚空", 20,
                gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.20f, 0.72f), new Vector2(280, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, AtkWord(meal), 16,
                gold ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.12f, 0.34f), new Vector2(280, 32), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, MinuteWord(meal), 15,
                gold ? VisualTokens.TextSecondary : VisualTokens.TextMuted,
                new Vector2(0.46f, 0.34f), new Vector2(160, 32), true, false);

            var idx = index;
            if (gold)
            {
                OverlayDraw.Pill(go.transform, built, eaten ? "已用" : "用餐", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 52), () =>
                    {
                        if (onEat != null) onEat(idx);
                    });
            }
            else
            {
                var chip = OverlayDraw.Pic(go.transform, built, "wait", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 48), VisualTokens.RailFill, UiSprites.Pill());
                chip.raycastTarget = false;
                OverlayDraw.Label(go.transform, built, "虚空", 16, VisualTokens.TextMuted,
                    new Vector2(0.86f, 0.5f), new Vector2(148, 40), false, false, 2f, true);
            }
        }

        static string AtkWord(Meal meal)
        {
            return "攻击 ×" + meal.AtkMul.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static string MinuteWord(Meal meal)
        {
            var n = meal.Minutes;
            if (n == (int)n)
                return ((int)n).ToString(CultureInfo.InvariantCulture) + " 分钟";
            return n.ToString("0.#", CultureInfo.InvariantCulture) + " 分钟";
        }

        static Color MarkTint(int index)
        {
            if (index == 0) return VisualTokens.Ember;
            if (index == 1) return VisualTokens.GoldSelect;
            return VisualTokens.IceShard;
        }

        static Sprite Mark(int index)
        {
            if (index == 0) return UiSprites.Spark();
            if (index == 1) return UiSprites.Hex();
            return UiSprites.IceCrystal();
        }

        static void GoldWire(Transform parent, List<GameObject> built, Vector2 size)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }
}
