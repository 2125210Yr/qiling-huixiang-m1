using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 称号 overlay：四枚自造核印（裂口初开 / 狂热一瞬 / 五人同核 / 听核）。金/虚空铬。不画马赛克、不画六页签。
    /// </summary>
    public static class TitleBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 128f;

        static readonly Title[] Titles =
        {
            new Title("裂口初开", "第一次把废都裂口封上。核才肯记住你的名字。", true, true),
            new Title("狂热一瞬", "核跳得太满，世界失声那几秒。你还听得见。", true, false),
            new Title("五人同核", "五人同台，五色同跳。圆台上核才肯亮。", true, false),
            new Title("听核", "城门底下那枚还在跳。听见它，才算入核。", false, false)
        };

        public static void Draw(Transform parent, Action onClose)
        {
            if (parent == null) return;
            var built = new List<GameObject>(64);
            var root = OverlayDraw.Group(parent, built, "TitleBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.54f), new Vector2(920, 900));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 872));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            for (int i = 0; i < Titles.Length; i++)
                DrawRow(panel, built, Titles[i], 0.735f - i * 0.165f, i);

            OverlayDraw.Label(panel, built, "核印不联网  ·  只记本机", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Star()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "称号", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "核印", 15, VisualTokens.GoldMetal,
                new Vector2(0.62f, 0.935f), new Vector2(120, 32), true, false);
            OverlayDraw.Label(panel, built, "四枚核印  ·  佩戴一枚", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, Title title, float y, int index)
        {
            var earned = title.Earned;
            var worn = title.Worn && earned;
            var go = new GameObject(title.Name, typeof(RectTransform), typeof(Image));
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
            ol.effectColor = worn ? VisualTokens.GoldSelect : (earned ? VisualTokens.GoldMetal : VisualTokens.SlotRim);
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.022f, 0.5f),
                new Vector2(8f, CardH - 36f), worn ? VisualTokens.GoldSelect : (earned ? VisualTokens.GoldMetal : VisualTokens.SlotRim));
            spine.raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "mark", new Vector2(0.08f, 0.68f), new Vector2(22, 22),
                earned ? MarkTint(index) : VisualTokens.RailIcon, Mark(index)).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, (index + 1).ToString("00"), 14, VisualTokens.GoldMetal,
                new Vector2(0.12f, 0.70f), new Vector2(48, 28), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, title.Name, 20,
                earned ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.18f, 0.70f), new Vector2(360, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, title.Hint, 15,
                earned ? VisualTokens.TextSecondary : VisualTokens.TextMuted,
                new Vector2(0.12f, 0.32f), new Vector2(480, 36), true, false);

            var chip = OverlayDraw.Pic(go.transform, built, "chip", new Vector2(0.86f, 0.5f),
                new Vector2(156, 48), worn ? VisualTokens.GoldSelect : VisualTokens.RailFill, UiSprites.Pill());
            chip.raycastTarget = false;
            if (worn)
            {
                var chipOl = chip.gameObject.AddComponent<Outline>();
                chipOl.effectColor = VisualTokens.TextOnYellow;
                chipOl.effectDistance = new Vector2(1f, -1f);
            }
            OverlayDraw.Label(go.transform, built, ChipWord(title), 16,
                worn ? VisualTokens.TextOnYellow : (earned ? VisualTokens.GoldMetal : VisualTokens.TextMuted),
                new Vector2(0.86f, 0.5f), new Vector2(148, 40), false, false, 2f, true);
        }

        static string ChipWord(Title title)
        {
            if (title.Worn && title.Earned) return "佩戴";
            if (title.Earned) return "已录";
            return "未闻";
        }

        static Color MarkTint(int index)
        {
            if (index == 0) return VisualTokens.GoldSelect;
            if (index == 1) return VisualTokens.FeverGold;
            if (index == 2) return VisualTokens.GoldTitle;
            return VisualTokens.RailIcon;
        }

        static Sprite Mark(int index)
        {
            if (index == 0) return UiSprites.Slash();
            if (index == 1) return UiSprites.Spark();
            if (index == 2) return UiSprites.Star();
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

        struct Title
        {
            public readonly string Name;
            public readonly string Hint;
            public readonly bool Earned;
            public readonly bool Worn;

            public Title(string name, string hint, bool earned, bool worn)
            {
                Name = name;
                Hint = hint;
                Earned = earned;
                Worn = worn;
            }
        }
    }
}
