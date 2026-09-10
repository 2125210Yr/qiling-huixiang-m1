using System;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 深途 overlay：短竖径四节点，通向更硬的关。金/虚空铬。不画马赛克、不画六页签、不画 CloseX。
    /// </summary>
    public static class DeepBoard
    {
        public const int NodeCount = 4;

        const float Cx = 0.38f;
        const float TopY = 0.74f;
        const float BotY = 0.26f;
        const float Hex = 108f;

        static readonly string[] Names =
        {
            "裂口余烬",
            "锈轨深层",
            "守核夹层",
            "炉心"
        };

        static readonly string[] Marks =
        {
            "困难 I",
            "困难 II",
            "困难 III",
            "困难 IV"
        };

        public static void Draw(Transform parent, int cleared, Action<int> onEnterNode)
        {
            if (parent == null) return;
            if (cleared < 0) cleared = 0;
            var root = OverlayDraw.Group(parent, null, "DeepBoard");

            var wash = OverlayDraw.Pic(root, null, "wash", new Vector2(0.5f, 0.48f), new Vector2(640, 1080),
                new Color(VisualTokens.UnderGlow.r, VisualTokens.UnderGlow.g, VisualTokens.UnderGlow.b, 0.20f),
                UiSprites.Soft());
            wash.raycastTarget = false;

            OverlayDraw.Label(root, null, "深途", 28, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.88f), new Vector2(320, 48), false, true, 2f, true);
            OverlayDraw.Label(root, null, "短径通向更硬的关", 16, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.842f), new Vector2(480, 32), false, false);
            OverlayDraw.DashLine(root, null, new Vector2(0.5f, 0.818f), 560f);

            DrawSpine(root, cleared);
            for (int i = 0; i < NodeCount; i++)
                DrawNode(root, i, cleared, onEnterNode);

            OverlayDraw.Label(root, null, "点节点进入  ·  已过可再战", 15, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.155f), new Vector2(640, 32), false, false);
        }

        static void DrawSpine(Transform parent, int cleared)
        {
            var step = (TopY - BotY) / (NodeCount - 1);
            for (int i = 0; i < NodeCount - 1; i++)
            {
                var y0 = NodeY(i);
                var y1 = NodeY(i + 1);
                var lit = i + 1 <= cleared;
                var col = lit ? VisualTokens.GoldMetal : VisualTokens.Line;
                OverlayDraw.Bar(parent, null, new Vector2(Cx, (y0 + y1) * 0.5f),
                    new Vector2(lit ? 6f : 4f, step * 1920f), col);
            }
        }

        static void DrawNode(Transform parent, int index, int cleared, Action<int> onEnterNode)
        {
            var done = index < cleared;
            var current = index == cleared && index < NodeCount;
            var locked = index > cleared;
            var anchor = new Vector2(Cx, NodeY(index));
            var fill = locked ? VisualTokens.RailFill : (current ? VisualTokens.PanelFillAlt : VisualTokens.PanelFill);
            var rim = locked ? VisualTokens.SlotRim : (current ? VisualTokens.GoldSelect : VisualTokens.GoldMetal);
            var ink = locked ? VisualTokens.TextMuted : (current ? VisualTokens.YellowValue : VisualTokens.GoldTitle);

            if (current)
            {
                var glow = OverlayDraw.Pic(parent, null, "glow", anchor, new Vector2(180, 180),
                    new Color(VisualTokens.YellowNavOn.r, VisualTokens.YellowNavOn.g, VisualTokens.YellowNavOn.b, 0.42f),
                    UiSprites.Soft());
                glow.raycastTarget = false;
            }

            var go = OverlayDraw.Hit(parent, null, anchor, new Vector2(128, 128), () =>
            {
                if (locked) return;
                if (onEnterNode != null) onEnterNode(index);
            });
            go.name = Names[index];
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Hex());
            img.color = fill;
            KeepHit(img);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = rim;
            ol.effectDistance = new Vector2(current ? 3f : 2f, current ? -3f : -2f);
            if (locked)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }

            var ring = OverlayDraw.Pic(go.transform, null, "rim", new Vector2(0.5f, 0.5f), new Vector2(Hex, Hex),
                rim, UiSprites.HexRing());
            ring.raycastTarget = false;
            OverlayDraw.Label(go.transform, null, (index + 1).ToString(), 28, ink,
                new Vector2(0.5f, 0.5f), new Vector2(80, 48), false, true, 2f, true);

            var lx = Cx + 0.16f;
            var nameCol = locked ? VisualTokens.TextMuted : VisualTokens.TextPrimary;
            OverlayDraw.Label(parent, null, Names[index], 22, nameCol,
                new Vector2(lx, NodeY(index) + 0.016f), new Vector2(420, 36), true, true, 2f, true);
            OverlayDraw.Label(parent, null, Marks[index] + "  ·  " + StatusWord(done, current, locked),
                15, locked ? VisualTokens.TextMuted : (current ? VisualTokens.YellowValue : VisualTokens.GoldMetal),
                new Vector2(lx, NodeY(index) - 0.016f), new Vector2(420, 28), true, false);
        }

        static float NodeY(int i)
        {
            return BotY + (TopY - BotY) * i / (NodeCount - 1);
        }

        static void KeepHit(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            img.canvasRenderer.cullTransparentMesh = false;
        }

        static string StatusWord(bool done, bool current, bool locked)
        {
            if (locked) return "锁定";
            if (current) return "进入";
            if (done) return "已过";
            return "进入";
        }
    }
}
