using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 切磋 overlay：本机开关。开门残章 ×1.05。无对战、不联网、不拷贝网络。金/虚空铬。
    /// </summary>
    public static class PvpBoard
    {
        const float RuleW = 780f;

        public static void Draw(Transform parent, bool doorOpen, Action onClose, Action onToggle)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "PvpBoard");

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(920, 760));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 732));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);
            Core(panel, built, doorOpen);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.22f), RuleW);

            var rule = OverlayDraw.Label(panel, built,
                "开门，" + CartaMulWord(true) + "。关门还原。",
                16, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.15f), new Vector2(720, 36), false, false);
            rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            OverlayDraw.Label(panel, built, "无对战  ·  不联网  ·  不拷贝网络", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(700, 28), false, false);

            if (doorOpen)
                UiChrome.Cancel(root, built, "关门", new Vector2(0.5f, 0.235f), onToggle);
            else
                UiChrome.Confirm(root, built, "开门", new Vector2(0.5f, 0.235f), onToggle);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Slash()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "切磋", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "本机开关  ·  不联网", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void Core(Transform panel, List<GameObject> built, bool doorOpen)
        {
            var gold = doorOpen;
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
                gold ? VisualTokens.YellowValue : VisualTokens.RailIcon,
                gold ? UiSprites.Spark() : UiSprites.Slash()).raycastTarget = false;

            OverlayDraw.Label(panel, built, gold ? "门开" : "门关", 20,
                gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.335f), new Vector2(200, 36), false, false, 2f, true);
            OverlayDraw.Label(panel, built, CartaMulWord(doorOpen), 22,
                gold ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.275f), new Vector2(320, 36), false, true, 2f, true);
        }

        static string CartaMulWord(bool doorOpen)
        {
            var mul = PvpRules.CartaMulIfPvp(doorOpen);
            return "残章 ×" + mul.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
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
