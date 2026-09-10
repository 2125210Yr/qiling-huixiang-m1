using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 成就 overlay：四枚自造印记。金/虚空铬。不画马赛克、不画六页签。
    /// </summary>
    public static class AchievementBoard
    {
        const float CardW = 400f;
        const float CardH = 292f;

        static readonly Seal[] Seals =
        {
            new Seal("首胜", "裂口第一次封上。", true, 0),
            new Seal("狂热一次", "核跳满，世界失声。", true, 1),
            new Seal("全队满编", "五槽都有回响。", false, 2),
            new Seal("深域", "短径走到炉心。", false, 3)
        };

        public static void Draw(Transform parent, Action onClose)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "AchievementBoard");

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.55f), new Vector2(920, 880));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 852));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), 780f);

            for (int i = 0; i < Seals.Length; i++)
            {
                var col = i % 2;
                var row = i / 2;
                DrawSeal(panel, built, Seals[i], new Vector2(0.27f + col * 0.46f, 0.60f - row * 0.36f));
            }

            OverlayDraw.Label(panel, built, "金印已应  ·  虚空未启", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);

            UiChrome.Confirm(root, built, "关闭", new Vector2(0.5f, 0.255f), onClose);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(28, 28),
                VisualTokens.GoldTitle, UiSprites.Star()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "成就", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "印记", 15, VisualTokens.GoldMetal,
                new Vector2(0.62f, 0.935f), new Vector2(120, 32), true, false);

            var have = 0;
            for (int i = 0; i < Seals.Length; i++)
                if (Seals[i].Gold) have++;
            OverlayDraw.Label(panel, built, "已应  " + have + " / " + Seals.Length, 15, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawSeal(Transform parent, List<GameObject> built, Seal seal, Vector2 anchor)
        {
            var gold = seal.Gold;
            var rim = gold ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            var titleCol = gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted;
            var hintCol = gold ? VisualTokens.TextSecondary : VisualTokens.TextMuted;
            var markCol = gold ? VisualTokens.GoldSelect : VisualTokens.SlotRim;
            var tint = gold ? VisualTokens.GoldTitle : VisualTokens.SlotRim;

            var go = new GameObject(seal.Title, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(CardW, CardH);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFillAlt;
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = rim;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            OverlayDraw.Pic(go.transform, built, "well", new Vector2(0.5f, 0.5f),
                new Vector2(CardW - 18f, CardH - 18f),
                gold ? VisualTokens.SlotWell : VisualTokens.BgVoid, UiSprites.Round())
                .raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "glow", new Vector2(0.5f, 0.70f), new Vector2(140, 96),
                new Color(tint.r, tint.g, tint.b, gold ? 0.20f : 0.08f), UiSprites.Soft())
                .raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "mark", new Vector2(0.5f, 0.72f), new Vector2(56, 56),
                markCol, MarkSprite(seal.Kind)).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, seal.Title, 22, titleCol,
                new Vector2(0.5f, 0.42f), new Vector2(340, 36), false, false, 2f, true);
            OverlayDraw.Label(go.transform, built, seal.Hint, 15, hintCol,
                new Vector2(0.5f, 0.28f), new Vector2(340, 28), false, false);
            OverlayDraw.Label(go.transform, built, gold ? "已应" : "虚空", 16,
                gold ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.13f), new Vector2(280, 32), false, true, 2f, true);
        }

        static Sprite MarkSprite(int kind)
        {
            if (kind == 0) return UiSprites.Spark();
            if (kind == 1) return UiSprites.Star();
            if (kind == 2) return UiSprites.Hex();
            return UiSprites.IceCrystal();
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

        struct Seal
        {
            public readonly string Title;
            public readonly string Hint;
            public readonly bool Gold;
            public readonly int Kind;

            public Seal(string title, string hint, bool gold, int kind)
            {
                Title = title;
                Hint = hint;
                Gold = gold;
                Kind = kind;
            }
        }
    }
}
