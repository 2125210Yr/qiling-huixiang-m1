using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 商店 overlay：四包核尘兑换。金/虚空铬，黄字标价。不写真金 IAP。
    /// </summary>
    public static class ShopBoard
    {
        const float PackW = 400f;
        const float PackH = 292f;

        static readonly string[] Titles = { "金币", "魂石", "经验", "好感" };
        static readonly int[] Amounts = { 12000, 180, 8000, 40 };
        static readonly int[] Prices = { 80, 200, 110, 150 };

        public static void Draw(Transform parent, System.Action onClose, System.Action<int> onBuy)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "ShopBoard");

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.55f), new Vector2(920, 880));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 852));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), 780f);

            for (int i = 0; i < 4; i++)
            {
                var idx = i;
                var col = i % 2;
                var row = i / 2;
                var anchor = new Vector2(0.27f + col * 0.46f, 0.60f - row * 0.36f);
                DrawPack(panel, built, idx, anchor, () =>
                {
                    if (onBuy != null) onBuy(idx);
                });
            }

            OverlayDraw.Label(panel, built, "点包兑换  ·  核尘标价", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);

            UiChrome.Confirm(root, built, "关闭", new Vector2(0.5f, 0.255f), onClose);
        }

        static void Header(Transform panel, List<GameObject> built, System.Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(28, 28),
                VisualTokens.GoldTitle, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "商店", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(200, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "核尘兑包  ·  只走本机", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(600, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawPack(Transform parent, List<GameObject> built, int index, Vector2 anchor, System.Action click)
        {
            var title = Titles[index];
            var tint = Tint(index);
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(PackW, PackH), click);
            go.name = title;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFillAlt;
            KeepHit(img);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(2f, -2f);

            OverlayDraw.Pic(go.transform, built, "well", new Vector2(0.5f, 0.5f),
                new Vector2(PackW - 18f, PackH - 18f), VisualTokens.SlotWell, UiSprites.Round())
                .raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "glow", new Vector2(0.5f, 0.70f), new Vector2(140, 96),
                new Color(tint.r, tint.g, tint.b, 0.20f), UiSprites.Soft()).raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "mark", new Vector2(0.5f, 0.72f), new Vector2(56, 56),
                tint, Mark(index)).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, title, 22, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.42f), new Vector2(340, 36), false, false, 2f, true);
            OverlayDraw.Label(go.transform, built, "×  " + OverlayDraw.Comma(Amounts[index]), 16,
                VisualTokens.TextSecondary, new Vector2(0.5f, 0.28f), new Vector2(340, 28), false, false);
            OverlayDraw.Label(go.transform, built, OverlayDraw.Comma(Prices[index]), 28, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.13f), new Vector2(280, 44), false, true, 3.5f, true);
        }

        static Color Tint(int index)
        {
            if (index == 0) return VisualTokens.GoldSelect;
            if (index == 1) return VisualTokens.ElemDark;
            if (index == 2) return VisualTokens.GoldTitle;
            return VisualTokens.Ember;
        }

        static Sprite Mark(int index)
        {
            if (index == 0) return UiSprites.Hex();
            if (index == 1) return UiSprites.IceCrystal();
            if (index == 2) return UiSprites.Star();
            return UiSprites.Spark();
        }

        static void KeepHit(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            img.canvasRenderer.cullTransparentMesh = false;
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
