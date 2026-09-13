using System;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 战斗暂停 overlay：75% 压暗 + 金线卡。
    /// Primary Robin pause: large <c>PAUSE</c> + red <c>Repeat</c> + speed chips.
    /// 现档 ×1 / ×2 / ×3。只读对接 <c>BattleSim.Clocks</c>。不是 T04。无六页签。
    /// </summary>
    public static class PauseBoard
    {
        public const string LayoutStatus = "PRIMARY_PARTIAL_HANDOFF";

        // Title/Repeat from primary Robin pause frames; layout coords still engineering.
        const float CtaY = 0.28f;
        const float HomeX = 0.22f;
        const float ResumeX = 0.50f;
        const float RepeatX = 0.78f;

        public static void Draw(Transform parent, Action onResume, Action onHome, Action onRepeat = null)
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
            OverlayDraw.Label(panel, null, "PAUSE", 48, VisualTokens.GoldSelect,
                new Vector2(0.5f, 0.78f), new Vector2(420, 64), false, true, 3.5f, true);
            // Primary GT is EN-only on pause card (Robin); drop CN 暂停 substamp.
            // Clock-policy / "Leaving returns to Home" essays stay off the card (inventory in PauseClockReadout).

            OverlayDraw.Label(panel, null, PauseClockReadout.SpeedSection, 15, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.54f), new Vector2(160, 26), false, false);
            Chip(panel, "x1", PauseClockReadout.FormatSpeed(PauseClockReadout.SpeedLo),
                new Vector2(0.22f, 0.46f), PauseClockReadout.SpeedLo, out var chipLo, out var labLo);
            Chip(panel, "x2", PauseClockReadout.FormatSpeed(PauseClockReadout.SpeedMid),
                new Vector2(0.50f, 0.46f), PauseClockReadout.SpeedMid, out var chipMid, out var labMid);
            Chip(panel, "x3", PauseClockReadout.FormatSpeed(PauseClockReadout.SpeedHi),
                new Vector2(0.78f, 0.46f), PauseClockReadout.SpeedHi, out var chipHi, out var labHi);

            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.14f), 620f);
            OverlayDraw.Pic(panel, null, "端左", new Vector2(0.092f, 0.14f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());
            OverlayDraw.Pic(panel, null, "端右", new Vector2(0.908f, 0.14f), new Vector2(14, 14),
                VisualTokens.GoldMetal, UiSprites.Spark());

            var bind = root.gameObject.AddComponent<PauseBoardBind>();
            bind.Wire(null, null, chipLo, labLo, chipMid, labMid, chipHi, labHi);

            if (onHome != null)
                UiChrome.Cancel(root, null, BattleCueCopy.ResultHome, new Vector2(HomeX, CtaY), onHome);
            if (onResume != null)
            {
                UiChrome.Confirm(root, null, BattleCueCopy.ResumeHud, new Vector2(ResumeX, CtaY), onResume);
            }
            if (onRepeat != null)
            {
                // Primary GT red Repeat — restarts active stage.
                UiChrome.Confirm(root, null, "Repeat", new Vector2(RepeatX, CtaY), onRepeat);
            }
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
