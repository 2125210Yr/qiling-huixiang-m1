using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 助战 overlay：三名自造过客行。DefaultParty 有契灵则画 PixelStandIn 芯片。金/虚空铬。
    /// </summary>
    public static class FriendBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 156f;
        const float ChipW = 96f;
        const float ChipH = 128f;
        const float WellW = 84f;
        const float WellH = 112f;

        static readonly Friend[] Friends =
        {
            new Friend("核边过客", "城门底下那枚还在跳。", 0, 80, 14820),
            new Friend("锈轨借刃", "巷口刃口还热着。", 1, 74, 12140),
            new Friend("井口回响", "浊潮井核还没散。", 2, 68, 10960)
        };

        public static void Draw(Transform parent, Action onClose)
        {
            if (parent == null) return;
            var built = new List<GameObject>(64);
            var root = OverlayDraw.Group(parent, built, "FriendBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.54f), new Vector2(920, 820));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 792));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            for (int i = 0; i < Friends.Length; i++)
                DrawRow(panel, built, Friends[i], 0.70f - i * 0.22f);

            OverlayDraw.Label(panel, built, "借刃一回  ·  不入编队", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "助战", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "过客", 15, VisualTokens.GoldMetal,
                new Vector2(0.62f, 0.935f), new Vector2(140, 32), true, false);
            OverlayDraw.Label(panel, built, "三名过客  ·  核还亮着", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, Friend friend, float y)
        {
            var def = TryParty(friend.Slot);
            var go = new GameObject(friend.Name, typeof(RectTransform), typeof(Image));
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
            ol.effectColor = def != null ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.018f, 0.5f),
                new Vector2(8f, CardH - 36f), def != null ? VisualTokens.GoldSelect : VisualTokens.SlotRim);
            spine.raycastTarget = false;

            PaintChip(go.transform, built, def, new Vector2(0.12f, 0.5f));

            OverlayDraw.Label(go.transform, built, friend.Name, 20, VisualTokens.GoldTitle,
                new Vector2(0.24f, 0.74f), new Vector2(280, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, friend.Hint, 15, VisualTokens.TextSecondary,
                new Vector2(0.24f, 0.28f), new Vector2(360, 36), true, false);

            if (def != null)
            {
                OverlayDraw.Label(go.transform, built, def.Name, 16, VisualTokens.TextPrimary,
                    new Vector2(0.24f, 0.50f), new Vector2(160, 28), true, false, 2f, true);
                OverlayDraw.Label(go.transform, built, CharacterPresenter.RoleMark(def.Role), 14,
                    VisualTokens.Element(def.Element),
                    new Vector2(0.46f, 0.50f), new Vector2(36, 28), false, true, 2f, true);
                OverlayDraw.Label(go.transform, built, "等级  " + friend.Level, 14, VisualTokens.GoldMetal,
                    new Vector2(0.56f, 0.50f), new Vector2(80, 28), true, false);
            }
            else
            {
                OverlayDraw.Label(go.transform, built, "虚位", 16, VisualTokens.TextMuted,
                    new Vector2(0.24f, 0.50f), new Vector2(160, 28), true, false, 2f, true);
            }

            OverlayDraw.Label(go.transform, built, OverlayDraw.Comma(friend.Power), 22, VisualTokens.YellowValue,
                new Vector2(0.86f, 0.70f), new Vector2(160, 36), false, true, 2f, true);

            var chip = OverlayDraw.Pic(go.transform, built, "borrow", new Vector2(0.86f, 0.32f),
                new Vector2(156, 48), VisualTokens.RailFill, UiSprites.Pill());
            chip.raycastTarget = false;
            OverlayDraw.Label(go.transform, built, def != null ? "可借" : "虚空", 16,
                def != null ? VisualTokens.GoldMetal : VisualTokens.TextMuted,
                new Vector2(0.86f, 0.32f), new Vector2(148, 40), false, false, 2f, true);
        }

        static void PaintChip(Transform parent, List<GameObject> built, CharacterDef def, Vector2 anchor)
        {
            var rim = def != null ? VisualTokens.Element(def.Element) : VisualTokens.SlotRim;
            var brick = OverlayDraw.Pic(parent, built, "chip", anchor, new Vector2(ChipW, ChipH),
                rim, UiSprites.Round());
            brick.raycastTarget = false;
            OverlayDraw.Pic(brick.transform, built, "well", new Vector2(0.5f, 0.52f),
                new Vector2(WellW, WellH), VisualTokens.SlotWell, UiSprites.Round())
                .raycastTarget = false;

            if (def != null && PaintFace(brick.transform, built, def))
            {
                OverlayDraw.Pic(brick.transform, built, "el", new Vector2(0.18f, 0.88f),
                    new Vector2(16, 16), VisualTokens.Element(def.Element), UiSprites.Circle())
                    .raycastTarget = false;
                OverlayDraw.Pic(brick.transform, built, "roleRim", new Vector2(0.84f, 0.88f),
                    new Vector2(20, 20), VisualTokens.Element(def.Element), UiSprites.Circle())
                    .raycastTarget = false;
                OverlayDraw.Pic(brick.transform, built, "role", new Vector2(0.84f, 0.88f),
                    new Vector2(16, 16), VisualTokens.RoleDisc, UiSprites.Circle()).raycastTarget = false;
                OverlayDraw.Label(brick.transform, built, CharacterPresenter.RoleMark(def.Role), 11,
                    VisualTokens.TextPrimary, new Vector2(0.84f, 0.88f), new Vector2(22, 20), false, true, 2f, true);
                return;
            }

            var ghost = VisualTokens.SlotRim;
            ghost.a = 0.50f;
            OverlayDraw.Pic(brick.transform, built, "wm", new Vector2(0.5f, 0.54f), new Vector2(36, 36),
                ghost, UiSprites.Hex()).raycastTarget = false;
        }

        static bool PaintFace(Transform brick, List<GameObject> built, CharacterDef def)
        {
            var spr = CharacterArt.Face(def.Id);
            if (spr == null) spr = PixelStandIn.Get(def, "");
            if (spr == null || spr.texture == null || spr.texture.width < 8)
                return false;

            var cut = new GameObject("cut", typeof(RectTransform), typeof(Image), typeof(Mask));
            cut.transform.SetParent(brick, false);
            var rt = cut.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.52f);
            rt.sizeDelta = new Vector2(WellW, WellH);
            rt.anchoredPosition = Vector2.zero;
            var mask = cut.GetComponent<Image>();
            if (mask != null)
            {
                UiSprites.Apply(mask, UiSprites.Round());
                mask.color = new Color(0.07f, 0.07f, 0.07f, 1f);
                mask.raycastTarget = false;
            }
            var cutMask = cut.GetComponent<Mask>();
            if (cutMask != null) cutMask.showMaskGraphic = true;
            OverlayDraw.Track(built, cut);

            var pix = OverlayDraw.Pic(cut.transform, built, "pixel", new Vector2(0.5f, 0.42f),
                new Vector2(WellW * 1.08f, WellH * 1.32f), Color.white, spr);
            pix.preserveAspect = true;
            pix.raycastTarget = false;
            OverlayDraw.Pic(cut.transform, built, "shade", new Vector2(0.5f, 0.08f),
                new Vector2(WellW * 1.1f, 36f), new Color(0f, 0f, 0f, 0.40f), UiSprites.Soft())
                .raycastTarget = false;
            return true;
        }

        static CharacterDef TryParty(int slot)
        {
            var party = Catalog.DefaultParty;
            if (party == null || slot < 0 || slot >= party.Length) return null;
            var id = party[slot];
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null) return null;
            CharacterDef def;
            return Catalog.Characters.TryGetValue(id, out def) ? def : null;
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

        struct Friend
        {
            public readonly string Name;
            public readonly string Hint;
            public readonly int Slot;
            public readonly int Level;
            public readonly int Power;

            public Friend(string name, string hint, int slot, int level, int power)
            {
                Name = name;
                Hint = hint;
                Slot = slot;
                Level = level;
                Power = power;
            }
        }
    }
}
