using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 召唤 overlay：金/虚空铬，标题召唤，一枚抽取，关闭 X。契灵回响自有词，不写原作抽卡文案。
    /// 抽取：残核一枚入册。已满则补金屑。
    /// </summary>
    public static class SummonBoard
    {
        const float RuleW = 780f;

        public static void Draw(Transform parent, Action onClose, Action onPull)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "SummonBoard");

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(920, 760));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 732));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);
            Core(panel, built);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.22f), RuleW);
            OverlayDraw.Label(panel, built, "核跳一下，契灵应一声。", 16, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.14f), new Vector2(700, 36), false, false);
            OverlayDraw.Label(panel, built, "残核 ×1  ·  入册不入队", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.075f), new Vector2(400, 28), false, false);

            UiChrome.Confirm(root, built, "抽取", new Vector2(0.5f, 0.235f), onPull);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var wash = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(320, 80),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            wash.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "mark", new Vector2(0.38f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "召唤", 24, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.935f), new Vector2(200, 44), false, false, 2f, true);
            OverlayDraw.Label(panel, built, "契核应声  ·  回响入册", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(600, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void Core(Transform panel, List<GameObject> built)
        {
            var glow = OverlayDraw.Pic(panel, built, "glow", new Vector2(0.5f, 0.545f), new Vector2(420, 420),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.20f),
                UiSprites.Soft());
            glow.raycastTarget = false;

            var well = OverlayDraw.Pic(panel, built, "well", new Vector2(0.5f, 0.545f), new Vector2(320, 320),
                VisualTokens.BgVoid, UiSprites.Round());
            well.raycastTarget = false;
            var wellOl = well.gameObject.AddComponent<Outline>();
            wellOl.effectColor = VisualTokens.GoldMetal;
            wellOl.effectDistance = new Vector2(2f, -2f);

            OverlayDraw.Pic(panel, built, "ring", new Vector2(0.5f, 0.545f), new Vector2(220, 220),
                VisualTokens.GoldMetal, UiSprites.HexRing()).raycastTarget = false;
            OverlayDraw.Pic(panel, built, "hex", new Vector2(0.5f, 0.545f), new Vector2(148, 148),
                VisualTokens.GoldTitle, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Pic(panel, built, "core", new Vector2(0.5f, 0.545f), new Vector2(72, 72),
                VisualTokens.YellowValue, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "契核", 18, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.30f), new Vector2(200, 32), false, false, 2f, true);
        }

        static void GoldWire(Transform panel, List<GameObject> built, Vector2 size)
        {
            var img = OverlayDraw.Pic(panel, built, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }
}
