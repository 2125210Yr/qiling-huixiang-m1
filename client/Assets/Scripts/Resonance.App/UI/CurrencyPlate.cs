using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 顶栏金屑 / 残核胶囊。印刷暗金：墨晕托底、金线内框、六环铸印、网点底纹。
    /// 金属铬件与黄数值分色。不写真金内购。
    /// </summary>
    public static class CurrencyPlate
    {
        const float ChipY = 0.972f;
        static readonly Vector2 GoldAt = new Vector2(0.20f, ChipY);
        static readonly Vector2 StoneAt = new Vector2(0.46f, ChipY);
        static readonly Vector2 ChipSize = new Vector2(224f, 38f);

        public static void Draw(Transform parent, List<GameObject> built, int gold, int stone)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "金屑条");
            if (root == null) return;
            var cg = root.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            Chip(root, built, "金屑", OverlayDraw.Comma(Mathf.Max(0, gold)), GoldAt, true);
            Chip(root, built, "残核", OverlayDraw.Comma(Mathf.Max(0, stone)), StoneAt, false);
        }

        static void Chip(Transform parent, List<GameObject> built, string tag, string amount, Vector2 anchor, bool gold)
        {
            if (parent == null) return;
            var fill = gold ? VisualTokens.RailFill : VisualTokens.BgVoid;
            var rim = gold ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            var markCol = gold ? VisualTokens.GoldSelect : VisualTokens.IceCore;
            var tagCol = gold ? VisualTokens.GoldTitle : VisualTokens.TextMuted;
            var numCol = gold ? VisualTokens.YellowValue : VisualTokens.TextPrimary;

            // 墨晕托底，让胶囊贴住顶缘而不是浮在画上。
            OverlayDraw.Pic(parent, built, "墨晕", anchor, new Vector2(ChipSize.x + 96f, ChipSize.y + 60f),
                new Color(0f, 0f, 0f, 0.42f), UiSprites.Soft());

            var pill = OverlayDraw.Pic(parent, built, tag, anchor, ChipSize,
                new Color(fill.r, fill.g, fill.b, 0.94f), UiSprites.Pill(), false);
            if (pill == null) return;
            pill.raycastTarget = false;

            // 网点底纹：框内印刷颗粒，避开两端弧头。
            var grain = OverlayDraw.Pic(pill.transform, built, "网点", new Vector2(0.5f, 0.5f),
                new Vector2(ChipSize.x - 74f, ChipSize.y - 16f), new Color(rim.r, rim.g, rim.b, 0.10f), UiSprites.Halftone());
            if (grain != null)
            {
                grain.type = Image.Type.Tiled;
                grain.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            }

            // 金线内框：印在墨底上的细框，不靠描边组件。
            OverlayDraw.Pic(pill.transform, built, "线框", new Vector2(0.5f, 0.5f),
                ChipSize - new Vector2(8f, 8f), new Color(rim.r, rim.g, rim.b, 0.9f), UiSprites.WireFrame());

            // 铸印：六环套芯记，金屑用芒、残核用六面。
            var sealAt = new Vector2(0.093f, 0.5f);
            OverlayDraw.Pic(pill.transform, built, "铸环", sealAt, new Vector2(32, 32), rim, UiSprites.HexRing());
            OverlayDraw.Pic(pill.transform, built, "铸芯", sealAt, new Vector2(17, 17), markCol,
                gold ? UiSprites.Spark() : UiSprites.Hex());

            var tagTx = OverlayDraw.Label(pill.transform, built, tag, 15, tagCol,
                new Vector2(0.215f, 0.5f), new Vector2(64, 36), true, false, 2f, true);
            if (tagTx != null) tagTx.raycastTarget = false;
            // 标签与数值之间的竖丝。
            OverlayDraw.Bar(pill.transform, built, new Vector2(0.425f, 0.5f), new Vector2(2, 20),
                new Color(rim.r, rim.g, rim.b, 0.5f));
            var numTx = OverlayDraw.Label(pill.transform, built, amount ?? "", 19, numCol,
                new Vector2(0.452f, 0.5f), new Vector2(122, 40), true, true, 2f, true);
            if (numTx != null) numTx.raycastTarget = false;
            MuteRaycast(pill.transform);
        }

        static void MuteRaycast(Transform t)
        {
            if (t == null) return;
            var graphics = t.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }
    }
}
