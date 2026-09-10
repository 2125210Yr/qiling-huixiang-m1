using System;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 掉落 overlay：标题「获得」，最多五枚材料片。金/虚空铬。收下关闭。
    /// 不铺全屏底暗，避免挡住结算「下一关」。
    /// </summary>
    public static class LootPopup
    {
        const float CtaY = 0.34f;

        public static void Draw(Transform parent, string[] itemNames, Action onClose)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, null, "LootPopup");

            var panelGo = UiChrome.Panel(root, null, new Vector2(0.5f, 0.56f), new Vector2(920, 640));
            var panel = panelGo.transform;
            GoldWire(panel, new Vector2(892, 612));

            var wash = OverlayDraw.Pic(panel, null, "halo", new Vector2(0.5f, 0.88f), new Vector2(360, 88),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.18f),
                UiSprites.Soft());
            if (wash != null) wash.raycastTarget = false;
            OverlayDraw.Label(panel, null, "掉落", 16, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.935f), new Vector2(280, 32), false, false);
            OverlayDraw.Label(panel, null, "获得", 48, VisualTokens.GoldSelect,
                new Vector2(0.5f, 0.86f), new Vector2(420, 64), false, true, 3.5f, true);

            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.76f), 780f);

            int n = Count(itemNames);
            float span = Span(n);
            float start = 0.5f - (n - 1) * span * 0.5f;
            for (int i = 0; i < n; i++)
                Chip(panel, new Vector2(start + i * span, 0.40f), NameAt(itemNames, i));

            OverlayDraw.Label(panel, null, "金屑入袋  ·  虚空不留", 15, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.10f), new Vector2(780, 36), false, false);

            if (onClose != null)
                UiChrome.Confirm(root, null, "收下", new Vector2(0.5f, CtaY), onClose);
        }

        static int Count(string[] itemNames)
        {
            if (itemNames == null || itemNames.Length == 0) return 1;
            return itemNames.Length > 5 ? 5 : itemNames.Length;
        }

        static float Span(int n)
        {
            if (n <= 1) return 0f;
            if (n == 2) return 0.28f;
            if (n == 3) return 0.28f;
            if (n == 4) return 0.22f;
            return 0.175f;
        }

        static string NameAt(string[] itemNames, int i)
        {
            if (itemNames == null || i < 0 || i >= itemNames.Length) return "材料";
            return string.IsNullOrEmpty(itemNames[i]) ? "材料" : itemNames[i];
        }

        static void Chip(Transform panel, Vector2 anchor, string name)
        {
            if (panel == null) return;
            var label = string.IsNullOrEmpty(name) ? "材料" : name;
            var go = OverlayDraw.Pic(panel, null, "chip", anchor, new Vector2(148, 176),
                VisualTokens.GoldMetal, UiSprites.Round());
            if (go != null) go.raycastTarget = false;
            var host = go != null ? go.transform : panel;
            var well = OverlayDraw.Pic(host, null, "well", new Vector2(0.5f, 0.5f), new Vector2(132, 160),
                VisualTokens.SlotWell, UiSprites.Round());
            if (well != null) well.raycastTarget = false;
            var gem = OverlayDraw.Pic(host, null, "gem", new Vector2(0.5f, 0.58f), new Vector2(48, 48),
                VisualTokens.YellowValue, UiSprites.Hex());
            if (gem != null) gem.raycastTarget = false;
            var spark = OverlayDraw.Pic(host, null, "spark", new Vector2(0.5f, 0.58f), new Vector2(24, 24),
                VisualTokens.GoldTitle, UiSprites.Spark());
            if (spark != null) spark.raycastTarget = false;
            OverlayDraw.Label(host, null, label, 16, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.18f), new Vector2(128, 36), false, true, 2f, true);
        }

        static void GoldWire(Transform panel, Vector2 size)
        {
            if (panel == null) return;
            var img = OverlayDraw.Pic(panel, null, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            if (img == null) return;
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }
}
