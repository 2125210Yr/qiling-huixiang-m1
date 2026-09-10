using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 造型 overlay：三行本貌 / 残响 / 夜巡。金/虚空铬。不画马赛克、不画六页签。
    /// </summary>
    public static class CostumeBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 148f;

        public static void Draw(Transform parent, string charId, string currentSkin, Action onClose, Action<string> onPick)
        {
            if (parent == null) return;
            if (currentSkin == null) currentSkin = "";
            var built = new List<GameObject>(64);
            var root = OverlayDraw.Group(parent, built, "CostumeBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(920, 780));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 752));

            Header(panel, built, charId, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            var ids = SkinCatalog.Ids;
            var n = ids != null ? ids.Length : 0;
            for (int i = 0; i < n; i++)
                DrawRow(panel, built, charId, currentSkin, ids[i], 0.70f - i * 0.22f, i, onPick);

            var confirm = UiChrome.Confirm(root, built, "确认", new Vector2(0.5f, 0.30f), onClose);
            if (confirm != null) confirm.transform.SetAsLastSibling();
        }

        static void Header(Transform panel, List<GameObject> built, string charId, Action onClose)
        {
            var def = Catalog.TryChar(charId);
            var who = def != null && !string.IsNullOrEmpty(def.Name) ? def.Name : "";
            var title = string.IsNullOrEmpty(who) ? "衣柜" : who + "的衣柜";
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(360, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            if (halo != null) halo.raycastTarget = false;
            OverlayDraw.Label(panel, built, title, 24, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.935f), new Vector2(640, 44), false, false, 2f, true);
            OverlayDraw.Label(panel, built, "从清单中选择造型", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, string charId, string currentSkin,
            string id, float y, int index, Action<string> onPick)
        {
            if (id == null) id = "";
            var open = Open(charId, id);
            var worn = open && currentSkin == id;
            var title = SkinCatalog.Label(id);
            var rim = worn ? VisualTokens.GoldSelect : (open ? VisualTokens.GoldMetal : VisualTokens.SlotRim);
            var spineCol = worn ? VisualTokens.GoldSelect : (open ? VisualTokens.GoldMetal : VisualTokens.SlotRim);

            var go = new GameObject(title, typeof(RectTransform), typeof(Image));
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
            ol.effectColor = rim;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.022f, 0.5f),
                new Vector2(8f, CardH - 36f), spineCol);
            spine.raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "mark", new Vector2(0.08f, 0.70f), new Vector2(22, 22),
                open ? MarkTint(index) : VisualTokens.RailIcon, Mark(index)).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, (index + 1).ToString("00"), 14, VisualTokens.GoldMetal,
                new Vector2(0.12f, 0.72f), new Vector2(48, 28), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, title, 20,
                open ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.18f, 0.72f), new Vector2(360, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, Hint(id), 15,
                open ? VisualTokens.TextSecondary : VisualTokens.TextMuted,
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
            OverlayDraw.Label(go.transform, built, ChipWord(open, worn), 16,
                worn ? VisualTokens.TextOnYellow : (open ? VisualTokens.TextSecondary : VisualTokens.TextMuted),
                new Vector2(0.86f, 0.5f), new Vector2(148, 40), false, false, 2f, true);

            if (!open) return;
            var pick = id;
            OverlayDraw.Hit(go.transform, built, new Vector2(0.5f, 0.5f), new Vector2(CardW, CardH), () =>
            {
                if (onPick != null) onPick(pick);
            });
        }

        static bool Open(string charId, string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return Costume.Unlocked(charId, id);
        }

        static string Hint(string id)
        {
            if (id == "echo") return "回声叠在刃上。核还记得那一跳。";
            if (id == "night") return "夜里才肯露面。虚空里那一层。";
            return "核里最初那一面。本貌常在。";
        }

        static string ChipWord(bool open, bool worn)
        {
            if (worn) return "着装";
            if (open) return "穿着";
            return "虚空";
        }

        static Color MarkTint(int index)
        {
            if (index == 0) return VisualTokens.GoldSelect;
            if (index == 1) return VisualTokens.GoldMetal;
            return VisualTokens.IceShard;
        }

        static Sprite Mark(int index)
        {
            if (index == 0) return UiSprites.Spark();
            if (index == 1) return UiSprites.Hex();
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
    }
}
