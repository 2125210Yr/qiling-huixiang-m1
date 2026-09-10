using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 入门 overlay：三页自造引导（点按是刃 / 上滑是势 / 驱动把核压出去）。金/虚空铬。不写原作教程文案。
    /// </summary>
    public static class TutorialBoard
    {
        const float RuleW = 780f;

        static readonly Page[] Pages =
        {
            new Page("点按", "点按是刃",
                "指尖落核，刃口才亮。连击只负责不停，点按才是真正的一刀。",
                VisualTokens.TapWhite),
            new Page("上滑", "上滑是势",
                "刃口抬势，不是甩手。上滑把核里那一跳抬高，势够才肯出招。",
                VisualTokens.SlideGreen),
            new Page("驱动", "驱动把核压出去",
                "核跳满了就别再攒。驱动把核里那一跳全压出去，废都才会让路。",
                VisualTokens.DriveOrange)
        };

        public static void Draw(Transform parent, int page, System.Action onClose, System.Action onNext)
        {
            if (parent == null) return;
            page = Mathf.Clamp(page, 0, 2);
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "TutorialBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(920, 760));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 732));

            Header(panel, built, page);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);
            DrawPage(panel, built, Pages[page], page);

            var last = page >= 2;
            UiChrome.Confirm(root, built, last ? "知道了" : "下一页", new Vector2(0.5f, 0.235f),
                last ? onClose : onNext);
        }

        static void Header(Transform panel, List<GameObject> built, int page)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Slash()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "入门", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, (page + 1).ToString("00") + " / 03", 15, VisualTokens.GoldMetal,
                new Vector2(0.72f, 0.935f), new Vector2(120, 32), true, false);
            OverlayDraw.Label(panel, built, "三页入核  ·  点按 / 上滑 / 驱动", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
        }

        static void DrawPage(Transform panel, List<GameObject> built, Page page, int index)
        {
            var tint = page.Tint;
            var glow = OverlayDraw.Pic(panel, built, "glow", new Vector2(0.5f, 0.64f), new Vector2(360, 260),
                new Color(tint.r, tint.g, tint.b, 0.20f), UiSprites.Soft());
            glow.raycastTarget = false;

            var well = OverlayDraw.Pic(panel, built, "well", new Vector2(0.5f, 0.62f), new Vector2(220, 220),
                VisualTokens.BgVoid, UiSprites.Round());
            well.raycastTarget = false;
            var wellOl = well.gameObject.AddComponent<Outline>();
            wellOl.effectColor = VisualTokens.GoldMetal;
            wellOl.effectDistance = new Vector2(2f, -2f);

            OverlayDraw.Pic(panel, built, "ring", new Vector2(0.5f, 0.62f), new Vector2(168, 168),
                tint, UiSprites.HexRing()).raycastTarget = false;
            OverlayDraw.Pic(panel, built, "mark", new Vector2(0.5f, 0.62f), new Vector2(72, 72),
                tint, Mark(index)).raycastTarget = false;

            var chip = OverlayDraw.Pic(panel, built, "chip", new Vector2(0.5f, 0.42f), new Vector2(148, 42),
                VisualTokens.YellowConfirm, UiSprites.Pill());
            chip.raycastTarget = false;
            OverlayDraw.Label(panel, built, page.Tag, 18, VisualTokens.TextOnYellow,
                new Vector2(0.5f, 0.42f), new Vector2(140, 40), false, false, 2f, true);

            OverlayDraw.Label(panel, built, page.Title, 28, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.32f), new Vector2(780, 48), false, false, 2f, true);
            var body = OverlayDraw.Label(panel, built, page.Body, 16, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.20f), new Vector2(700, 80), false, false);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.alignment = TextAnchor.UpperCenter;
            body.lineSpacing = 0.94f;

            Pips(panel, built, index);
        }

        static void Pips(Transform panel, List<GameObject> built, int page)
        {
            for (int i = 0; i < 3; i++)
            {
                var on = i == page;
                OverlayDraw.Pic(panel, built, "pip" + i, new Vector2(0.44f + i * 0.06f, 0.08f),
                    new Vector2(on ? 16f : 10f, 10f),
                    on ? VisualTokens.GoldSelect : VisualTokens.SlotRim,
                    UiSprites.Circle()).raycastTarget = false;
            }
        }

        static Sprite Mark(int index)
        {
            if (index == 0) return UiSprites.Slash();
            if (index == 1) return UiSprites.Spark();
            return UiSprites.Hex();
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

        struct Page
        {
            public readonly string Tag;
            public readonly string Title;
            public readonly string Body;
            public readonly Color Tint;

            public Page(string tag, string title, string body, Color tint)
            {
                Tag = tag;
                Title = title;
                Body = body;
                Tint = tint;
            }
        }
    }
}
