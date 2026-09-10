using System;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 普通关结算 overlay：完成/失败、掉落一行、回首页、下一关/再战。
    /// 不是 Ragna 伤害排名。无六页签。确认钮在卡下。
    /// </summary>
    public static class ResultBoard
    {
        public const string LayoutStatus = "NEEDS_REFERENCE";

        // NEEDS_REFERENCE: not measured from primary GT. Not T27.
        const float CtaY = 0.34f;
        const float HomeX = 0.30f;
        const float NextX = 0.70f;

        public static void Draw(Transform parent, bool win, BattleSim battle, string lootLine, Action onHome, Action onNext)
        {
            if (parent == null) return;
            _ = battle;

            var root = OverlayDraw.Group(parent, null, "ResultBoard");
            UiChrome.ModalDim(root, null);

            var panelGo = UiChrome.Panel(root, null, new Vector2(0.5f, 0.56f), new Vector2(760, 500));
            var panel = panelGo.transform;
            GoldWire(panel, new Vector2(732, 472));

            var title = win ? "完成" : "失败";
            var titleCol = win ? VisualTokens.GoldSelect : VisualTokens.StarEvolved;
            var wash = OverlayDraw.Pic(panel, null, "halo", new Vector2(0.5f, 0.74f), new Vector2(360, 72),
                new Color(titleCol.r, titleCol.g, titleCol.b, 0.12f),
                UiSprites.Halftone());
            if (wash != null)
            {
                wash.raycastTarget = false;
                wash.type = Image.Type.Tiled;
            }
            OverlayDraw.Pic(panel, null, "slash", new Vector2(0.5f, 0.66f), new Vector2(280f, 56f),
                new Color(titleCol.r, titleCol.g, titleCol.b, 0.28f),
                UiSprites.Slash());
            OverlayDraw.Pic(panel, null, "芒左", new Vector2(0.31f, 0.66f), new Vector2(22, 22),
                titleCol, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "芒右", new Vector2(0.69f, 0.66f), new Vector2(22, 22),
                titleCol, UiSprites.Spark());
            OverlayDraw.Label(panel, null, "战斗结果", 16, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.88f), new Vector2(280, 32), false, false);
            OverlayDraw.Label(panel, null, title, 48, titleCol,
                new Vector2(0.5f, 0.66f), new Vector2(420, 64), false, true, 3.5f, true);

            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.48f), 620f);
            OverlayDraw.Pic(panel, null, "端左", new Vector2(0.092f, 0.48f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "端右", new Vector2(0.908f, 0.48f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());

            OverlayDraw.Label(panel, null, "掉落", 16, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.36f), new Vector2(160, 28), false, false);
            var loot = string.IsNullOrEmpty(lootLine) ? (win ? "材料 ×3" : "无掉落") : lootLine;
            OverlayDraw.Label(panel, null, loot, 15, win ? VisualTokens.TextSecondary : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.24f), new Vector2(560, 30), false, false);

            var homeX = onNext != null ? HomeX : 0.5f;
            var nextX = onHome != null ? NextX : 0.5f;
            if (onHome != null)
                UiChrome.Cancel(root, null, "回首页", new Vector2(homeX, CtaY), onHome);
            if (onNext != null)
                UiChrome.Confirm(root, null, win ? "下一关" : "再战", new Vector2(nextX, CtaY), onNext);
        }

        static void GoldWire(Transform panel, Vector2 size)
        {
            if (panel == null) return;
            var img = OverlayDraw.Pic(panel, null, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            if (img == null) return;
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }
}
