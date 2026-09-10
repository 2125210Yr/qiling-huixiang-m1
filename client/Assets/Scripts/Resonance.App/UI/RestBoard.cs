using System;
using System.Collections.Generic;
using System.Globalization;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 休息 overlay：金/虚空铬。契合层与属性倍率。歇一会入核。不画马赛克、不画六页签。不写温泉、不写原作标题。
    /// </summary>
    public static class RestBoard
    {
        const float RuleW = 780f;

        public static void Draw(Transform parent, int points, Action onClose, Action onRest)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "RestBoard");

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(920, 760));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 732));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);
            Core(panel, built, points);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.22f), RuleW);
            OverlayDraw.Label(panel, built, "核歇一回，契合才肯涨。", 16, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.15f), new Vector2(720, 36), false, false);
            OverlayDraw.Label(panel, built, "歇一会入核  ·  不联网", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);

            UiChrome.Confirm(root, built, "歇一会", new Vector2(0.5f, 0.235f), onRest);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "休息", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "契合入核  ·  核歇一回", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void Core(Transform panel, List<GameObject> built, int points)
        {
            var level = Bond.Level(points);
            var gold = level > 0;
            var glowCol = gold
                ? new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.22f)
                : new Color(0f, 0f, 0f, 0.28f);
            var glow = OverlayDraw.Pic(panel, built, "glow", new Vector2(0.5f, 0.56f), new Vector2(400, 400),
                glowCol, UiSprites.Soft());
            glow.raycastTarget = false;

            var well = OverlayDraw.Pic(panel, built, "well", new Vector2(0.5f, 0.56f), new Vector2(300, 300),
                VisualTokens.BgVoid, UiSprites.Round());
            well.raycastTarget = false;
            var wellOl = well.gameObject.AddComponent<Outline>();
            wellOl.effectColor = gold ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            wellOl.effectDistance = new Vector2(2f, -2f);

            OverlayDraw.Pic(panel, built, "ring", new Vector2(0.5f, 0.56f), new Vector2(200, 200),
                gold ? VisualTokens.GoldMetal : VisualTokens.SlotRim, UiSprites.HexRing()).raycastTarget = false;
            OverlayDraw.Pic(panel, built, "hex", new Vector2(0.5f, 0.56f), new Vector2(132, 132),
                gold ? VisualTokens.GoldTitle : VisualTokens.RailFill, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Pic(panel, built, "core", new Vector2(0.5f, 0.56f), new Vector2(64, 64),
                gold ? VisualTokens.YellowValue : VisualTokens.RailIcon, UiSprites.Spark()).raycastTarget = false;

            OverlayDraw.Label(panel, built, level + " / " + Bond.MaxLevel, 22,
                gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.335f), new Vector2(280, 36), false, false, 2f, true);
            OverlayDraw.Label(panel, built, StatWord(level), 22,
                gold ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.275f), new Vector2(320, 36), false, true, 2f, true);
        }

        static string StatWord(int level)
        {
            var mul = Bond.StatMul(level);
            return "属性 ×" + mul.ToString("0.00", CultureInfo.InvariantCulture);
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
