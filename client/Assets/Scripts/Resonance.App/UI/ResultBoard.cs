using System;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 普通关结算 overlay：CLEAR!!/失败、掉落一行、回首页、下一关/再战。
    /// Primary P0 ~t396: <c>CLEAR!!</c> + 弧形三星（t396/t472 中星更大；t530 右星发光更大）+ 奖励头像条（uncap★ + E pip）+ 黄带
    /// 左 <c>n LEVEL</c>/<c>n EXP</c>/<c>n GOLD</c>、右账号名 + <c>EXP n/m</c>（数值仍 UNKNOWN，用 —）。
    /// 揭板后可叠 LEVEL UP 模态（P0 ~t470 <c>Level n ▶ m</c>）。不是 Ragna 伤害排名。无六页签。确认钮在卡下。
    /// </summary>
    public static class ResultBoard
    {
        public const string LayoutStatus = "PRIMARY_PARTIAL_HANDOFF";

        // NEEDS_REFERENCE for star counts / exact loot; title cue from P0 t396.
        const float CtaY = 0.30f;
        const float HomeX = 0.30f;
        const float NextX = 0.70f;

        public static void HoldUntilSplash(Transform root)
        {
            if (root == null) return;
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        /// <param name="playLevelUp">
        /// Win path: after CLEAR is readable, play account LEVEL UP (P0 t470).
        /// Delayed so t396 CLEAR!! is not covered. Amounts stay —.
        /// </param>
        public static void Reveal(Transform root, bool playLevelUp = false)
        {
            if (root == null) return;
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
            if (playLevelUp)
                VfxLevelUp.PlayClearModalDelayed(
                    root.parent != null ? root.parent : root, VfxLevelUp.ClearHoldSec);
        }

        public static Transform Draw(Transform parent, bool win, BattleSim battle, string lootLine, Action onHome, Action onNext, string stageName = null)
        {
            if (parent == null) return null;
            _ = battle;

            var root = OverlayDraw.Group(parent, null, "ResultBoard");
            UiChrome.ModalDim(root, null);

            var panelGo = UiChrome.Panel(root, null, new Vector2(0.5f, 0.56f), new Vector2(760, 540));
            var panel = panelGo.transform;
            GoldWire(panel, new Vector2(732, 512));

            var title = win ? "CLEAR!!" : BattleCueCopy.ResultFail;
            var titleCol = win ? VisualTokens.GoldSelect : VisualTokens.StarEvolved;
            var wash = OverlayDraw.Pic(panel, null, "halo", new Vector2(0.5f, 0.78f), new Vector2(360, 72),
                new Color(titleCol.r, titleCol.g, titleCol.b, 0.12f),
                UiSprites.Halftone());
            if (wash != null)
            {
                wash.raycastTarget = false;
                wash.type = Image.Type.Tiled;
            }
            OverlayDraw.Pic(panel, null, "slash", new Vector2(0.5f, 0.74f), new Vector2(280f, 56f),
                new Color(titleCol.r, titleCol.g, titleCol.b, 0.28f),
                UiSprites.Slash());
            OverlayDraw.Pic(panel, null, "芒左", new Vector2(0.31f, 0.74f), new Vector2(22, 22),
                titleCol, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "芒右", new Vector2(0.69f, 0.74f), new Vector2(22, 22),
                titleCol, UiSprites.Spark());
            var head = win && !string.IsNullOrEmpty(stageName) ? stageName : "RESULT";
            OverlayDraw.Label(panel, null, head, 16, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.90f), new Vector2(420, 32), false, false);
            if (win)
            {
                // P0 t398: small Total n above CLEAR!! (count math UNKNOWN).
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearTotalStub, 14, VisualTokens.TapWhite,
                    new Vector2(0.5f, 0.845f), new Vector2(200, 24), false, false);
            }
            OverlayDraw.Label(panel, null, title, 48, titleCol,
                new Vector2(0.5f, 0.74f), new Vector2(420, 64), false, true, 3.5f, true);
            if (win)
            {
                // Decorative 3-star arc (P0 t395): ascending right — right star largest. Not star-grade logic.
                float[] starX = { 0.34f, 0.48f, 0.62f };
                float[] starY = { 0.63f, 0.655f, 0.68f };
                float[] starSz = { 28f, 36f, 48f };
                for (int i = 0; i < 3; i++)
                {
                    OverlayDraw.Pic(panel, null, "clearStar" + i, new Vector2(starX[i], starY[i]),
                        new Vector2(starSz[i], starSz[i]), VisualTokens.FeverGold, UiSprites.Star());
                }

                // Party reward portrait stubs (P0 t396: square face + Lv + name + thin EXP bar + uncap stars).
                DrawRewardPortraitStub(panel, 0, new Vector2(0.38f, 0.52f), stars: 2);
                DrawRewardPortraitStub(panel, 1, new Vector2(0.62f, 0.52f), stars: 3);

                // Yellow account band (P0 t396/t472): left LEVEL + EXP/GOLD; right name + EXP n/m.
                // t472 Stage 3 Tutorial frame-locked stubs — not a formula.
                var band = OverlayDraw.Pic(panel, null, "expBand", new Vector2(0.5f, 0.33f), new Vector2(720, 118),
                    VisualTokens.FeverGold, UiSprites.Round());
                if (band != null) band.raycastTarget = false;
                // Checkered rail stubs (top/bottom of yellow band).
                OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.39f), 700f);
                OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.27f), 700f);

                // P0 t472: large yellow numerals + black unit labels.
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearLevelStub, 40, new Color(0.12f, 0.06f, 0.00f, 1f),
                    new Vector2(0.16f, 0.36f), new Vector2(64, 48), false, true, 0f, true);
                OverlayDraw.Label(panel, null, "LEVEL", 15, Color.black,
                    new Vector2(0.28f, 0.36f), new Vector2(90, 28), false, true, 0f, true);
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearExpGainStub, 22, new Color(0.12f, 0.06f, 0.00f, 1f),
                    new Vector2(0.16f, 0.30f), new Vector2(48, 28), false, true, 0f, true);
                OverlayDraw.Label(panel, null, "EXP", 15, Color.black,
                    new Vector2(0.26f, 0.30f), new Vector2(56, 26), false, true, 0f, true);
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearGoldGainStub, 22, new Color(0.12f, 0.06f, 0.00f, 1f),
                    new Vector2(0.16f, 0.255f), new Vector2(48, 28), false, true, 0f, true);
                OverlayDraw.Label(panel, null, "GOLD", 15, Color.black,
                    new Vector2(0.28f, 0.255f), new Vector2(70, 26), false, true, 0f, true);

                OverlayDraw.Label(panel, null, BattleCueCopy.ClearAccountStub, 14, Color.black,
                    new Vector2(0.62f, 0.37f), new Vector2(200, 22), false, false);
                var expTrack = OverlayDraw.Bar(panel, null, new Vector2(0.68f, 0.325f), new Vector2(240, 12),
                    new Color(0.15f, 0.08f, 0.18f, 0.85f));
                if (expTrack != null) expTrack.raycastTarget = false;
                var expFill = OverlayDraw.Bar(panel, null, new Vector2(0.58f, 0.325f), new Vector2(100, 12),
                    new Color(0.55f, 0.28f, 0.78f, 0.95f));
                if (expFill != null) expFill.raycastTarget = false;
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearExpBarStub, 13, Color.black,
                    new Vector2(0.68f, 0.28f), new Vector2(220, 22), false, false);
                // P0 t472: green LEVEL UP chip on the yellow band (before modal).
                OverlayDraw.Label(panel, null, BattleCueCopy.ClearLevelUpChip, 13,
                    new Color(0.12f, 0.55f, 0.28f, 1f),
                    new Vector2(0.88f, 0.325f), new Vector2(90, 24), false, true, 0f, true);
            }

            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, win ? 0.18f : 0.48f), 620f);
            OverlayDraw.Pic(panel, null, "端左", new Vector2(0.092f, win ? 0.18f : 0.48f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "端右", new Vector2(0.908f, win ? 0.18f : 0.48f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());

            OverlayDraw.Label(panel, null, "LOOT", 16, VisualTokens.GoldTitle,
                new Vector2(0.5f, win ? 0.12f : 0.36f), new Vector2(160, 28), false, false);
            var loot = string.IsNullOrEmpty(lootLine) ? (win ? "Materials ×3" : "No loot") : lootLine;
            OverlayDraw.Label(panel, null, loot, 15, win ? VisualTokens.TextSecondary : VisualTokens.TextMuted,
                new Vector2(0.5f, win ? 0.06f : 0.24f), new Vector2(560, 30), false, false);

            var homeX = onNext != null ? HomeX : 0.5f;
            var nextX = onHome != null ? NextX : 0.5f;
            if (onHome != null)
            {
                UiChrome.Cancel(root, null, BattleCueCopy.ResultHome, new Vector2(homeX, CtaY), onHome);
                OverlayDraw.Label(root, null, "HOME", 12, VisualTokens.TextMuted,
                    new Vector2(homeX, CtaY - 0.055f), new Vector2(140, 22), false, false);
            }
            if (onNext != null)
            {
                UiChrome.Confirm(root, null, win ? BattleCueCopy.ResultNext : BattleCueCopy.ResultRetry, new Vector2(nextX, CtaY), onNext);
                OverlayDraw.Label(root, null, win ? "NEXT" : "RETRY", 12, VisualTokens.TextMuted,
                    new Vector2(nextX, CtaY - 0.055f), new Vector2(140, 22), false, false);
            }
            // P0 t472 right-rail: BOSS / Retry / Back (CTA under panel may stay HOME/NEXT).
            if (win)
            {
                OverlayDraw.Label(root, null, BattleCueCopy.ResultBoss, 14, VisualTokens.FeverGold,
                    new Vector2(0.92f, 0.42f), new Vector2(80, 24), false, true, 0f, true);
                OverlayDraw.Label(root, null, BattleCueCopy.ResultRetryTitle, 12, VisualTokens.YellowValue,
                    new Vector2(0.92f, 0.34f), new Vector2(80, 22), false, false);
                OverlayDraw.Label(root, null, BattleCueCopy.ResultBack, 12, VisualTokens.TextMuted,
                    new Vector2(0.92f, 0.28f), new Vector2(80, 22), false, false);
            }
            return root;
        }

        static void DrawRewardPortraitStub(Transform panel, int i, Vector2 anchor, int stars = 2)
        {
            var face = OverlayDraw.Pic(panel, null, "rewardFace" + i, anchor, new Vector2(88, 88),
                new Color(0.12f, 0.10f, 0.12f, 0.95f), UiSprites.Round());
            if (face != null) face.raycastTarget = false;
            // P0 t396 CLEAR rewards: Silent Pixie (2★) + Mona (3★), Lv.1, E pip.
            OverlayDraw.Label(panel, null, "Lv.1", 12, Color.white,
                new Vector2(anchor.x - 0.04f, anchor.y + 0.06f), new Vector2(64, 18), false, false);
            OverlayDraw.Label(panel, null, "E", 11, Color.white,
                new Vector2(anchor.x + 0.04f, anchor.y + 0.06f), new Vector2(24, 18), false, true, 0f, true);
            var n = stars < 1 ? 1 : (stars > 5 ? 5 : stars);
            for (int s = 0; s < n; s++)
            {
                var ox = (s - (n - 1) * 0.5f) * 0.028f;
                OverlayDraw.Pic(panel, null, "rewardStar" + i + "_" + s,
                    new Vector2(anchor.x + ox, anchor.y - 0.055f),
                    new Vector2(14f, 14f), VisualTokens.FeverGold, UiSprites.Star());
            }
            var rewardName = i == 0 ? BattleCueCopy.ClearRewardPixie : BattleCueCopy.ClearRewardMona;
            OverlayDraw.Label(panel, null, rewardName, 13, VisualTokens.TextSecondary,
                new Vector2(anchor.x, anchor.y - 0.10f), new Vector2(120, 20), false, false);
            var bar = OverlayDraw.Bar(panel, null, new Vector2(anchor.x, anchor.y - 0.14f), new Vector2(88, 8),
                new Color(0.45f, 0.22f, 0.70f, 0.9f));
            if (bar != null) bar.raycastTarget = false;
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
