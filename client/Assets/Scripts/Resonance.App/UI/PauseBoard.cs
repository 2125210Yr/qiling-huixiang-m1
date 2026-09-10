using System;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 战斗暂停 overlay：75% 压暗 + 金线卡 + 虚线律。
    /// 现档 ×1 / ×2 可观察，只读对接 <c>BattleSim.Clocks</c>。取消「回首页」+ 确认「继续」胶囊对脱在卡下。
    /// 布局未测自 primary GT。不是 T04。无六页签。
    /// </summary>
    public static class PauseBoard
    {
        public const string LayoutStatus = "NEEDS_REFERENCE";

        // NEEDS_REFERENCE: not measured from primary GT. Not T27. Not T04.
        const float CtaY = 0.30f;
        const float HomeX = 0.30f;
        const float ResumeX = 0.70f;

        public static void Draw(Transform parent, Action onResume, Action onHome)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, null, "PauseBoard");
            UiChrome.ModalDim(root, null);

            var panelGo = UiChrome.Panel(root, null, new Vector2(0.5f, 0.58f), new Vector2(760, 520));
            var panel = panelGo.transform;
            GoldWire(panel, new Vector2(732, 492));

            var wash = OverlayDraw.Pic(panel, null, "halo", new Vector2(0.5f, 0.80f), new Vector2(360, 72),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.12f),
                UiSprites.Halftone());
            if (wash != null)
            {
                wash.raycastTarget = false;
                wash.type = Image.Type.Tiled;
            }
            OverlayDraw.Pic(panel, null, "slash", new Vector2(0.5f, 0.76f), new Vector2(280f, 56f),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.28f),
                UiSprites.Slash());
            OverlayDraw.Pic(panel, null, "芒左", new Vector2(0.31f, 0.76f), new Vector2(22, 22),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "芒右", new Vector2(0.69f, 0.76f), new Vector2(22, 22),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Label(panel, null, PauseClockReadout.Title, 48, VisualTokens.GoldSelect,
                new Vector2(0.5f, 0.76f), new Vector2(420, 64), false, true, 3.5f, true);

            var view = PauseClockReadout.Read(GameRoot.Live != null ? GameRoot.Live.Battle : null);
            var status = OverlayDraw.Label(panel, null, view.StatusLine, 16, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.62f), new Vector2(620, 30), false, false);
            if (status != null) status.gameObject.name = "暂停状态";

            OverlayDraw.Label(panel, null, PauseClockReadout.SpeedSection, 15, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.54f), new Vector2(160, 26), false, false);
            Chip(panel, "倍速×1", PauseClockReadout.FormatSpeed(PauseClockReadout.SpeedLo),
                new Vector2(0.34f, 0.46f), PauseClockReadout.SpeedLo, out var chipLo, out var labLo);
            Chip(panel, "倍速×2", PauseClockReadout.FormatSpeed(PauseClockReadout.SpeedHi),
                new Vector2(0.66f, 0.46f), PauseClockReadout.SpeedHi, out var chipHi, out var labHi);

            var policy = OverlayDraw.Label(panel, null, view.PolicyLine, 13, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.34f), new Vector2(640, 56), false, false);
            if (policy != null)
            {
                policy.gameObject.name = "时钟政策";
                policy.horizontalOverflow = HorizontalWrapMode.Wrap;
                policy.verticalOverflow = VerticalWrapMode.Overflow;
            }

            OverlayDraw.Label(panel, null, PauseClockReadout.HomeWarn, 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.22f), new Vector2(560, 30), false, false);

            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.14f), 620f);
            OverlayDraw.Pic(panel, null, "端左", new Vector2(0.092f, 0.14f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "端右", new Vector2(0.908f, 0.14f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());

            var bind = root.gameObject.AddComponent<PauseBoardBind>();
            bind.Wire(status, policy, chipLo, labLo, chipHi, labHi);

            var homeX = onResume != null ? HomeX : 0.5f;
            var resumeX = onHome != null ? ResumeX : 0.5f;
            if (onHome != null)
                UiChrome.Cancel(root, null, "回首页", new Vector2(homeX, CtaY), onHome);
            if (onResume != null)
                UiChrome.Confirm(root, null, "继续", new Vector2(resumeX, CtaY), onResume);
        }

        static void Chip(Transform panel, string name, string word, Vector2 anchor, int speed,
            out Image chip, out Text lab)
        {
            chip = null;
            lab = null;
            var go = OverlayDraw.Hit(panel, null, anchor, new Vector2(168, 48),
                () => PauseBoardBind.RequestHostSpeed(speed));
            if (go == null) return;
            go.name = name;
            chip = go.GetComponent<Image>();
            if (chip != null)
            {
                UiSprites.Apply(chip, UiSprites.Pill());
                chip.color = VisualTokens.Hex("424242");
            }
            lab = OverlayDraw.Label(go.transform, null, word, 20, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.5f), new Vector2(148, 40), false, false, 2f, true);
            if (lab != null) lab.raycastTarget = false;
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
