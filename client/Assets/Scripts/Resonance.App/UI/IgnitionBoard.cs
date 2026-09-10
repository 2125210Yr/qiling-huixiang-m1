using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 燃起 overlay：攻 / 暴 / 敏三色核石，上限 12。金/虚空铬。预览 Ignition.Of。非树。
    /// </summary>
    public static class IgnitionBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 148f;

        public static void Draw(Transform parent, int atkStones, int crtStones, int aglStones, int cap, Action onClose, Action<int> onAdd)
        {
            if (parent == null) return;
            cap = FitCap(cap);
            atkStones = Fit(atkStones, cap);
            crtStones = Fit(crtStones, cap);
            aglStones = Fit(aglStones, cap);

            var built = new List<GameObject>(64);
            var root = OverlayDraw.Group(parent, built, "IgnitionBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.55f), new Vector2(920, 880));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 852));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            var bonus = Ignition.Of(atkStones, crtStones, aglStones);
            Preview(panel, built, bonus);

            DrawRow(panel, built, 0.64f, 0, "攻", atkStones, cap, bonus.Atk, onAdd);
            DrawRow(panel, built, 0.44f, 1, "暴", crtStones, cap, bonus.Crt, onAdd);
            DrawRow(panel, built, 0.24f, 2, "敏", aglStones, cap, bonus.Agl, onAdd);

            OverlayDraw.Label(panel, built, "首核四百红  ·  余核一百  ·  非树", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);

            UiChrome.Confirm(root, built, "关闭", new Vector2(0.5f, 0.255f), onClose);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "燃起", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "点火", 15, VisualTokens.GoldMetal,
                new Vector2(0.64f, 0.935f), new Vector2(140, 32), true, false);
            OverlayDraw.Label(panel, built, "攻 / 暴 / 敏  ·  各十二核", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void Preview(Transform panel, List<GameObject> built, IgnitionBonus bonus)
        {
            var well = OverlayDraw.Pic(panel, built, "preview", new Vector2(0.5f, 0.80f), new Vector2(820, 56),
                VisualTokens.SlotWell, UiSprites.Round());
            well.raycastTarget = false;
            OverlayDraw.Label(panel, built,
                "攻  " + Pct(bonus.Atk) + "      暴  " + Pct(bonus.Crt) + "      敏  " + Pct(bonus.Agl),
                18, VisualTokens.YellowValue, new Vector2(0.5f, 0.80f), new Vector2(780, 40), false, true, 2f, true);
        }

        static void DrawRow(Transform panel, List<GameObject> built, float y, int kind, string title, int stones, int cap, float bonus, Action<int> onAdd)
        {
            var gold = stones > 0;
            var full = stones >= cap;
            var go = new GameObject(title, typeof(RectTransform), typeof(Image));
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
                gold ? VisualTokens.GoldMetal : VisualTokens.RailIcon, UiSprites.Hex()).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, title, 22, gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.12f, 0.72f), new Vector2(64, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, stones + "/" + cap, 18, gold ? VisualTokens.TextPrimary : VisualTokens.TextMuted,
                new Vector2(0.22f, 0.72f), new Vector2(120, 36), true, true, 2f);
            OverlayDraw.Label(go.transform, built, Pct(bonus), 18, gold ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.40f, 0.72f), new Vector2(180, 36), true, true, 2f, true);

            for (int i = 0; i < cap; i++)
            {
                var on = i < stones;
                var main = i == 0;
                var x = 0.14f + i * 0.048f;
                var size = main ? 22f : 16f;
                var col = on ? VisualTokens.GoldSelect : VisualTokens.SlotRim;
                var spr = main ? UiSprites.Hex() : UiSprites.Circle();
                OverlayDraw.Pic(go.transform, built, "s" + i, new Vector2(x, 0.32f),
                    new Vector2(size, size), col, spr).raycastTarget = false;
            }

            if (full)
            {
                var chip = OverlayDraw.Pic(go.transform, built, "full", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 48), VisualTokens.RailFill, UiSprites.Pill());
                chip.raycastTarget = false;
                OverlayDraw.Label(go.transform, built, "满", 16, VisualTokens.TextMuted,
                    new Vector2(0.86f, 0.5f), new Vector2(148, 40), false, false, 2f, true);
            }
            else
            {
                OverlayDraw.Pill(go.transform, built, "加核", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 52), () =>
                    {
                        if (onAdd != null) onAdd(kind);
                    });
            }
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

        static string Pct(float v)
        {
            var p = v * 100f;
            if (p < 0f) p = 0f;
            return "+" + p.ToString("0.##") + "%";
        }

        static int FitCap(int cap)
        {
            if (cap < 1) cap = Ignition.StoneCap;
            if (cap > Ignition.StoneCap) cap = Ignition.StoneCap;
            return cap;
        }

        static int Fit(int n, int cap)
        {
            if (n < 0) n = 0;
            if (n > cap) n = cap;
            return n;
        }
    }
}
