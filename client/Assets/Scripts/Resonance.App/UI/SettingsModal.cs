using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public sealed class SettingsHooks
    {
        public Action onAuto;
        public Action onSpeed;
        public Action onWipe;
        public Action onBack;
    }

    /// <summary>
    /// 虚空/金设定铬：75% 压暗、金线框、虚线律，确认胶囊脱在卡下。
    /// 危险操作配取消+确认对。无马赛克、无六页签。
    /// </summary>
    public static class SettingsModal
    {
        const float RuleW = 780f;

        public static void Draw(Transform parent, List<GameObject> built, AutoMode auto, int speed, SettingsHooks hooks)
        {
            if (parent == null) return;
            if (hooks == null) hooks = new SettingsHooks();
            var root = OverlayDraw.Group(parent, built, "SettingsModal");
            var path = SaveFilePath();

            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.58f), new Vector2(920, 640));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 612));

            Header(panel, built, hooks.onBack);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.84f), RuleW);

            Section(panel, built, "战斗", 0.78f);
            Row(panel, built, 0.68f, "自动", AutoWord(auto), auto != AutoMode.Manual, hooks.onAuto);
            Row(panel, built, 0.54f, "倍速", speed == 2 ? "2×" : "1×", speed == 2, hooks.onSpeed);

            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.42f), RuleW);
            Section(panel, built, "资料", 0.36f);
            PathLine(panel, built, path, 0.26f);
            GreyBar(panel, built, 0.12f, "清除本地存档", () => AskWipe(root, built, path, hooks.onWipe));

            UiChrome.Confirm(root, built, "确认", new Vector2(0.5f, 0.22f), hooks.onBack);
        }

        static void Header(Transform panel, List<GameObject> built, Action onBack)
        {
            var wash = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            wash.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "gear", new Vector2(0.47f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Gear()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "设定", 24, VisualTokens.GoldTitle,
                new Vector2(0.495f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "只改本机  ·  不联网", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(600, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onBack);
        }

        static void Section(Transform panel, List<GameObject> built, string title, float y)
        {
            OverlayDraw.Label(panel, built, title, 16, VisualTokens.GoldTitle,
                new Vector2(0.085f, y), new Vector2(240, 32), true, false);
        }

        static void Row(Transform panel, List<GameObject> built, float y, string label, string value, bool on, Action click)
        {
            var spine = OverlayDraw.Bar(panel, built, new Vector2(0.055f, y), new Vector2(2f, 28f),
                on ? VisualTokens.GoldSelect : VisualTokens.SlotRim);
            if (spine != null) spine.raycastTarget = false;

            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, y - 0.055f), RuleW);

            var go = OverlayDraw.Hit(panel, built, new Vector2(0.5f, y), new Vector2(820, 64), click);
            go.name = label;
            OverlayDraw.Label(go.transform, built, label, 22, VisualTokens.TextPrimary,
                new Vector2(0.04f, 0.5f), new Vector2(280, 44), true, false);
            var chip = OverlayDraw.Pic(go.transform, built, "chip", new Vector2(0.86f, 0.5f), new Vector2(148, 42),
                on ? VisualTokens.YellowConfirm : VisualTokens.Hex("424242"), UiSprites.Pill());
            chip.raycastTarget = false;
            if (on)
            {
                var chipOl = chip.gameObject.AddComponent<Outline>();
                chipOl.effectColor = VisualTokens.TextOnYellow;
                chipOl.effectDistance = new Vector2(1f, -1f);
            }
            OverlayDraw.Label(go.transform, built, value, 18,
                on ? VisualTokens.TextOnYellow : VisualTokens.TextMuted,
                new Vector2(0.86f, 0.5f), new Vector2(140, 40), false, false, 2f, true);
        }

        static void PathLine(Transform panel, List<GameObject> built, string path, float y)
        {
            var tx = OverlayDraw.Label(panel, built, path ?? "", 13, VisualTokens.TextMuted,
                new Vector2(0.5f, y), new Vector2(780, 56), false, false);
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.alignment = TextAnchor.MiddleCenter;
        }

        static void GreyBar(Transform panel, List<GameObject> built, float y, string label, Action click)
        {
            var go = OverlayDraw.Hit(panel, built, new Vector2(0.5f, y), new Vector2(640, 64), click);
            go.name = label;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Round());
                img.color = VisualTokens.RailFill;
            }
            var ol = go.AddComponent<Outline>();
            ol.effectColor = VisualTokens.SlotRim;
            ol.effectDistance = new Vector2(1f, -1f);
            OverlayDraw.Label(go.transform, built, label, 20, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.5f), new Vector2(600, 44), false, false);
        }

        static void AskWipe(Transform root, List<GameObject> built, string path, Action onWipe)
        {
            var layer = OverlayDraw.Group(root, built, "Notice");
            UiChrome.ModalDim(layer, built);
            var card = UiChrome.Panel(layer, built, new Vector2(0.5f, 0.56f), new Vector2(840, 200));
            GoldWire(card.transform, built, new Vector2(812, 172));
            OverlayDraw.Label(card.transform, built, "注意", 22, VisualTokens.GoldTitle,
                new Vector2(0.08f, 0.82f), new Vector2(280, 40), true, false, 0f, true);
            OverlayDraw.DashLine(card.transform, built, new Vector2(0.5f, 0.68f), 760f);
            var well = OverlayDraw.Pic(card.transform, built, "well", new Vector2(0.5f, 0.40f), new Vector2(792, 100),
                VisualTokens.BgVoid, UiSprites.Round());
            well.raycastTarget = false;
            OverlayDraw.Label(card.transform, built, "(i)  会清掉本机存档。", 22, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.48f), new Vector2(720, 48), false, false);
            var sub = OverlayDraw.Label(card.transform, built, path ?? "", 13, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.22f), new Vector2(760, 40), false, false);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;

            Action close = () =>
            {
                if (layer != null) UnityEngine.Object.Destroy(layer.gameObject);
            };
            UiChrome.Cancel(layer, built, "取消", new Vector2(0.32f, 0.36f), close);
            UiChrome.Confirm(layer, built, "确认", new Vector2(0.68f, 0.36f), () =>
            {
                close();
                if (onWipe != null) onWipe();
            });
        }

        static void GoldWire(Transform parent, List<GameObject> built, Vector2 size)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            if (img == null) return;
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }

        static string SaveFilePath() => SaveStore.DefaultPath;

        static string AutoWord(AutoMode auto)
        {
            if (auto == AutoMode.Full) return "全自动";
            if (auto == AutoMode.Semi) return "半自动";
            return "关";
        }
    }
}
