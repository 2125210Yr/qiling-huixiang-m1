using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 第1章 overlay：废都裂口十二节点。金/虚空马赛克 + 半调印刷底。金题印章、斜切金痕、
    /// 六角节点套金属环，底部确认胶囊直进当前关。不替代深途。
    /// </summary>
    public static class ChapterMap
    {
        public const int NodeCount = 12;

        const float Hex = 96f;
        const float Hit = 116f;
        const float Col0 = 0.22f;
        const float ColStep = 0.28f;
        const float TopY = 0.775f;
        const float RowStep = 0.148f;

        static readonly string[] Names =
        {
            "废都入口", "锈轨巷", "断桥", "浊潮井", "棘林边缘",
            "残灯回廊", "白昼裂口", "影缚地窟", "炉心外环", "城门广场",
            "守核下层", "城门守核"
        };

        public static void Draw(Transform parent, int cleared, Action<int> onPick, Action onClose)
        {
            if (parent == null) return;
            if (cleared < 0) cleared = 0;
            var built = new List<GameObject>(96);
            var root = OverlayDraw.Group(parent, built, "ChapterMap");

            var mosaic = OverlayDraw.Pic(root, built, "mosaic", new Vector2(0.5f, 0.5f),
                new Vector2(1080f, 1920f), Color.white, UiSprites.FloorMosaic());
            mosaic.raycastTarget = false;
            OverlayDraw.Wash(root, built, new Color(0f, 0f, 0f, 0.42f), true);

            var wash = OverlayDraw.Pic(root, built, "wash", new Vector2(0.5f, 0.48f), new Vector2(640, 1080),
                new Color(VisualTokens.UnderGlow.r, VisualTokens.UnderGlow.g, VisualTokens.UnderGlow.b, 0.18f),
                UiSprites.Soft());
            wash.raycastTarget = false;

            Header(root, built, cleared, onClose);
            OverlayDraw.DashLine(root, built, new Vector2(0.5f, 0.848f), 920f);

            var frame = OverlayDraw.Pic(root, built, "frame", new Vector2(0.5f, 0.519f), new Vector2(960f, 1240f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.55f),
                UiSprites.WireFrame());
            frame.raycastTarget = false;

            DrawLinks(root, built, cleared);
            for (int i = 0; i < NodeCount; i++)
                DrawNode(root, built, i, cleared, onPick);

            OverlayDraw.Label(root, built, "点节点进入  ·  已过可再战", 15, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.155f), new Vector2(640, 32), false, false);

            DrawCta(root, built, cleared, onPick);
        }

        static void DrawCta(Transform parent, List<GameObject> built, int cleared, Action<int> onPick)
        {
            if (onPick == null || cleared >= NodeCount) return;
            const float y = 0.088f;
            var glow = OverlayDraw.Pic(parent, built, "ctaGlow", new Vector2(0.5f, y), new Vector2(460f, 140f),
                new Color(VisualTokens.YellowConfirm.r, VisualTokens.YellowConfirm.g, VisualTokens.YellowConfirm.b, 0.26f),
                UiSprites.Soft());
            glow.raycastTarget = false;
            var slL = OverlayDraw.Pic(parent, built, "ctaSlashL", new Vector2(0.24f, y), new Vector2(110f, 26f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.80f),
                UiSprites.Slash());
            slL.raycastTarget = false;
            slL.rectTransform.localEulerAngles = new Vector3(0f, 0f, 16f);
            var slR = OverlayDraw.Pic(parent, built, "ctaSlashR", new Vector2(0.76f, y), new Vector2(110f, 26f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.80f),
                UiSprites.Slash());
            slR.raycastTarget = false;
            slR.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);
            UiChrome.Confirm(parent, built, "挑战 " + Names[cleared], new Vector2(0.5f, y), () => onPick(cleared));
        }

        static void Header(Transform parent, List<GameObject> built, int cleared, Action onClose)
        {
            var ht = OverlayDraw.Pic(parent, built, "halftone", new Vector2(0.5f, 0.920f), new Vector2(780f, 180f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.12f),
                UiSprites.Halftone());
            ht.raycastTarget = false;
            ht.type = Image.Type.Tiled;
            ht.pixelsPerUnitMultiplier = 0.5f;
            var halo = OverlayDraw.Pic(parent, built, "halo", new Vector2(0.5f, 0.932f), new Vector2(360, 80),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(parent, built, "stamp", new Vector2(0.400f, 0.936f), new Vector2(28, 28),
                VisualTokens.GoldTitle, UiSprites.Stamp(2)).raycastTarget = false;
            OverlayDraw.Pic(parent, built, "stampR", new Vector2(0.600f, 0.936f), new Vector2(28, 28),
                VisualTokens.GoldTitle, UiSprites.Stamp(2)).raycastTarget = false;
            var slL = OverlayDraw.Pic(parent, built, "slashL", new Vector2(0.315f, 0.936f), new Vector2(100f, 24f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.85f),
                UiSprites.Slash());
            slL.raycastTarget = false;
            slL.rectTransform.localEulerAngles = new Vector3(0f, 0f, 14f);
            var slR = OverlayDraw.Pic(parent, built, "slashR", new Vector2(0.685f, 0.936f), new Vector2(100f, 24f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.85f),
                UiSprites.Slash());
            slR.raycastTarget = false;
            slR.rectTransform.localEulerAngles = new Vector3(0f, 0f, -14f);
            OverlayDraw.Label(parent, built, "第1章", 28, VisualTokens.GoldTitle,
                new Vector2(0.430f, 0.936f), new Vector2(220, 44), true, true, 2f, true);
            OverlayDraw.Label(parent, built, "废都裂口", 18, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.888f), new Vector2(480, 32), false, true, 2f, true);
            var n = cleared > NodeCount ? NodeCount : cleared;
            OverlayDraw.Label(parent, built, n + " / " + NodeCount, 16, VisualTokens.GoldMetal,
                new Vector2(0.5f, 0.862f), new Vector2(240, 28), false, false);
            UiChrome.CloseX(parent, built, new Vector2(0.93f, 0.95f), onClose);
        }

        static void DrawLinks(Transform parent, List<GameObject> built, int cleared)
        {
            for (int i = 0; i < NodeCount - 1; i++)
            {
                var a = NodePos(i);
                var b = NodePos(i + 1);
                var lit = i + 1 <= cleared;
                var col = lit ? VisualTokens.GoldMetal : VisualTokens.Line;
                var thick = lit ? 6f : 4f;
                var mid = (a + b) * 0.5f;
                if (Mathf.Abs(a.y - b.y) < 0.001f)
                    OverlayDraw.Bar(parent, built, mid, new Vector2(Mathf.Abs(b.x - a.x) * 1080f, thick), col);
                else
                    OverlayDraw.Bar(parent, built, mid, new Vector2(thick, Mathf.Abs(b.y - a.y) * 1920f), col);
                var stud = OverlayDraw.Pic(parent, built, "stud", mid, new Vector2(18f, 18f), col, UiSprites.Hex());
                stud.raycastTarget = false;
            }
        }

        static void DrawNode(Transform parent, List<GameObject> built, int index, int cleared, Action<int> onPick)
        {
            var done = index < cleared;
            var current = index == cleared && index < NodeCount;
            var locked = index > cleared;
            var anchor = NodePos(index);
            var fill = locked ? VisualTokens.RailFill : (current ? VisualTokens.PanelFillAlt : VisualTokens.PanelFill);
            var rim = locked ? VisualTokens.SlotRim : (current ? VisualTokens.GoldSelect : VisualTokens.GoldMetal);
            var ink = locked ? VisualTokens.TextMuted : (current ? VisualTokens.YellowValue : VisualTokens.GoldTitle);
            var nameCol = locked ? VisualTokens.TextMuted : VisualTokens.TextPrimary;
            var markCol = locked ? VisualTokens.TextMuted : (current ? VisualTokens.YellowValue : VisualTokens.GoldMetal);

            if (current)
            {
                var glow = OverlayDraw.Pic(parent, built, "glow", anchor, new Vector2(168, 168),
                    new Color(VisualTokens.YellowNavOn.r, VisualTokens.YellowNavOn.g, VisualTokens.YellowNavOn.b, 0.42f),
                    UiSprites.Soft());
                glow.raycastTarget = false;
            }

            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(Hit, Hit), () =>
            {
                if (locked) return;
                if (onPick != null) onPick(index);
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

            var outer = OverlayDraw.Pic(go.transform, built, "rimOuter", new Vector2(0.5f, 0.5f),
                new Vector2(Hex + 16f, Hex + 16f), locked ? VisualTokens.Rim : VisualTokens.GoldMetal,
                UiSprites.HexRing());
            outer.raycastTarget = false;
            var ring = OverlayDraw.Pic(go.transform, built, "rim", new Vector2(0.5f, 0.5f), new Vector2(Hex, Hex),
                rim, UiSprites.HexRing());
            ring.raycastTarget = false;
            OverlayDraw.Label(go.transform, built, (index + 1).ToString("00"), 22, ink,
                new Vector2(0.5f, 0.5f), new Vector2(80, 48), false, true, 2f, true);

            OverlayDraw.Label(parent, built, Names[index], 16, nameCol,
                new Vector2(anchor.x, anchor.y - 0.048f), new Vector2(220, 28), false, true, 2f, true);
            OverlayDraw.Label(parent, built, StatusWord(done, current, locked), 13, markCol,
                new Vector2(anchor.x, anchor.y - 0.068f), new Vector2(160, 24), false, false);
        }

        static Vector2 NodePos(int i)
        {
            var row = i / 3;
            var col = i % 3;
            if ((row & 1) == 1) col = 2 - col;
            return new Vector2(Col0 + col * ColStep, TopY - row * RowStep);
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
