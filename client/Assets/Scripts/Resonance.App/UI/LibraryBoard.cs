using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 书库：契灵回响残章。印版暗金马赛克条目：元素环序号、金脊、
    /// 虚线规、半调洗、印记戳。短条目，不写原作剧情。无六页签。
    /// 狂热/上滑用词跟 <see cref="BattleCueCopy"/> 对齐；书库不是战斗坐标。
    /// </summary>
    public static class LibraryBoard
    {
        const float CardW = 920f;
        const float CardH = 156f;
        const float FirstY = 0.790f;
        const float StepY = 0.100f;

        static readonly Entry[] Entries =
        {
            new Entry("01", "契核", "废都城门底下那枚还在跳的核。听见它的人，才会应成契灵。"),
            new Entry("02", "契灵", "二十五道回响，五色五职。名字写进核里，不是被叫来的奴。"),
            new Entry("03", "废都裂口", "锈轨巷、浊潮井、棘林、残灯回廊。裂口从入口咬到守核，封上才算一章。"),
            new Entry("04", "五色", "火克木，木克水，水克火。光与暗对咬。刃口选边，核才肯亮。"),
            new Entry("05", "点按 · " + BattleCueCopy.SlideSkill + " · " + BattleCueCopy.DriveCast, "点按是刃，上滑是势，驱动把核里那一跳全压出去。连击只负责不停。"),
            new Entry("06", BattleCueCopy.FeverTime, "核跳得太满，世界会短暂失声。那几秒里只有回响还听得见。"),
            new Entry("07", "城门守核", "尽头的门不是门。守核坐在核上，不让任何人把回响带走。")
        };

        static readonly Element[] RimCycle =
        {
            Element.Fire, Element.Water, Element.Wood, Element.Light, Element.Dark
        };

        public static void Draw(Transform parent)
        {
            if (parent == null) return;
            var built = new List<GameObject>(160);
            var root = OverlayDraw.Group(parent, built, "LibraryBoard");
            if (root == null) return;
            UiChrome.MosaicFill(root, built);
            OverlayDraw.Wash(root, built, new Color(0f, 0f, 0f, 0.50f), false);
            Tone(root, built, "print", new Vector2(0.5f, 0.48f), new Vector2(1080f, 1420f),
                new Color(1f, 1f, 1f, 0.04f));
            Masthead(root, built);
            OverlayDraw.DashLine(root, built, new Vector2(0.5f, 0.856f), 920f);
            for (int i = 0; i < Entries.Length; i++)
                DrawEntry(root, built, Entries[i], FirstY - i * StepY, i);
            Footer(root, built);
        }

        static void Masthead(Transform parent, List<GameObject> built)
        {
            var c = new Vector2(0.5f, 0.920f);
            Quiet(OverlayDraw.Pic(parent, built, "plate", c, new Vector2(760, 140),
                new Color(0.08f, 0.06f, 0.05f, 1f), UiSprites.Round()));
            Wire(parent, built, c, new Vector2(748, 128),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f));
            Tone(parent, built, "tone", c, new Vector2(748, 128), new Color(1f, 1f, 1f, 0.05f));
            Quiet(OverlayDraw.Pic(parent, built, "seal", new Vector2(0.448f, 0.932f), new Vector2(28, 28),
                VisualTokens.GoldTitle, UiSprites.Stamp(3)));
            OverlayDraw.Label(parent, built, "书库", 26, VisualTokens.GoldTitle,
                new Vector2(0.468f, 0.932f), new Vector2(120, 44), true, false, 2f, true);
            Quiet(OverlayDraw.Pic(parent, built, "slash", new Vector2(0.556f, 0.932f), new Vector2(46, 14),
                VisualTokens.GoldMetal, UiSprites.Slash()));
            OverlayDraw.Label(parent, built, "契灵回响  ·  残章", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.892f), new Vector2(640, 26), false, false);
        }

        static void DrawEntry(Transform parent, List<GameObject> built, Entry entry, float y, int i)
        {
            var go = new GameObject(entry.Index, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(CardW, CardH);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            OverlayDraw.Track(built, go);
            var t = go.transform;

            Tone(t, built, "tone", new Vector2(0.5f, 0.48f), new Vector2(CardW, 72f),
                new Color(1f, 1f, 1f, 0.04f));
            var spine = OverlayDraw.Pic(t, built, "spine", new Vector2(0.008f, 0.5f),
                new Vector2(3f, CardH - 28f), VisualTokens.GoldSelect, UiSprites.Pixel());
            Quiet(spine);

            var elCol = VisualTokens.Element(RimCycle[i % RimCycle.Length]);
            Quiet(OverlayDraw.Pic(t, built, "rim", new Vector2(0.082f, 0.66f), new Vector2(44, 44),
                elCol, UiSprites.Circle()));
            Quiet(OverlayDraw.Pic(t, built, "core", new Vector2(0.082f, 0.66f), new Vector2(37, 37),
                VisualTokens.PanelFill, UiSprites.Circle()));
            OverlayDraw.Label(t, built, entry.Index, 15, VisualTokens.TextPrimary,
                new Vector2(0.082f, 0.66f), new Vector2(48, 32), false, true, 2f, true);

            OverlayDraw.Label(t, built, entry.Title, 20, VisualTokens.GoldTitle,
                new Vector2(0.140f, 0.66f), new Vector2(680, 36), true, false, 2f, true);
            Quiet(OverlayDraw.Pic(t, built, "seal", new Vector2(0.940f, 0.66f), new Vector2(34, 34),
                VisualTokens.GoldMetal, UiSprites.Stamp(i % 6)));

            OverlayDraw.DashLine(t, built, new Vector2(0.520f, 0.460f), 780f);

            var body = OverlayDraw.Label(t, built, entry.Body, 16, VisualTokens.TextSecondary,
                new Vector2(0.075f, 0.235f), new Vector2(790, 58), true, false);
            if (body != null)
            {
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Truncate;
                body.alignment = TextAnchor.UpperLeft;
                body.lineSpacing = 0.94f;
            }
        }

        static void Footer(Transform parent, List<GameObject> built)
        {
            OverlayDraw.DashLine(parent, built, new Vector2(0.5f, 0.106f), 920f);
            Quiet(OverlayDraw.Pic(parent, built, "sealL", new Vector2(0.452f, 0.068f), new Vector2(18, 18),
                VisualTokens.GoldMetal, UiSprites.Stamp(3)));
            Quiet(OverlayDraw.Pic(parent, built, "sealR", new Vector2(0.548f, 0.068f), new Vector2(18, 18),
                VisualTokens.GoldMetal, UiSprites.Stamp(3)));
            OverlayDraw.Label(parent, built, "残章七则", 13, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.068f), new Vector2(160, 24), false, false);
        }

        static Image Wire(Transform parent, List<GameObject> built, Vector2 anchor, Vector2 size, Color color)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", anchor, size, color, UiSprites.WireFrame());
            if (img != null) img.raycastTarget = false;
            return img;
        }

        static Image Tone(Transform parent, List<GameObject> built, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var img = OverlayDraw.Pic(parent, built, name, anchor, size, color, UiSprites.Halftone());
            if (img == null) return null;
            img.type = Image.Type.Tiled;
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        static void Quiet(Image img)
        {
            if (img != null) img.raycastTarget = false;
        }

        struct Entry
        {
            public readonly string Index;
            public readonly string Title;
            public readonly string Body;

            public Entry(string index, string title, string body)
            {
                Index = index;
                Title = title;
                Body = body;
            }
        }
    }
}
