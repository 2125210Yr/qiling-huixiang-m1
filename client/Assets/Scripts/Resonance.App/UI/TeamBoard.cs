using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 编队 / 契灵：010 竖条语法。顶槽跟 party 长度（默认 5），中名册竖条（印刷暗黑奢华：元素色框、
    /// 红焰进化星、选中烫金、顶部网点溶解、S 级橙标），底留给六页签。
    /// </summary>
    public static class TeamBoard
    {
        public const int PartySize = FightStats.DefaultPartyCap;
        public const float StripW = 132f;
        public const float StripH = 300f;
        public const float RosterW = 110f;
        public const float RosterH = 240f;

        const float PartyY = 0.84f;
        const float RosterY = 0.52f;
        const float BandY = 0.655f;
        const float DetailY = 0.418f;
        const float InfoY = 0.34f;

        public static void DrawTeam(Transform parent, List<GameObject> built, SaveBlob save, int editSlot,
            System.Action<int> onSelectSlot, System.Action<string> onPickUnit,
            System.Action<int> onLeader, System.Action<string> onDetail)
        {
            CharacterPresenter.MosaicFloor(parent, built);
            if (save == null) return;
            var cap = LivePartySize(save);
            var slot = Mathf.Clamp(editSlot, 0, Mathf.Max(0, cap - 1));
            DrawMemorial(parent, built, save, slot, true, onSelectSlot, onPickUnit, onLeader, onDetail);
        }

        public static void DrawRoster(Transform parent, List<GameObject> built, SaveBlob save,
            System.Action<string> onInspect)
        {
            CharacterPresenter.MosaicFloor(parent, built);
            if (save == null) return;
            var slot = FocusSlot(save, save.LeaderSlot);
            DrawMemorial(parent, built, save, slot, false,
                i =>
                {
                    var id = PartyId(save, i);
                    if (!string.IsNullOrEmpty(id) && onInspect != null) onInspect(id);
                },
                onInspect, null, onInspect);
        }

        static void DrawMemorial(Transform parent, List<GameObject> built, SaveBlob save, int slot,
            bool slotSelectable, System.Action<int> onSelectSlot, System.Action<string> onPickUnit,
            System.Action<int> onLeader, System.Action<string> onDetail)
        {
            DrawParty(parent, built, save, slot, slotSelectable, onSelectSlot);
            var picked = PartyId(save, slot);
            if (onLeader != null)
            {
                DrawLeaderBtn(parent, built, () =>
                {
                    if (!string.IsNullOrEmpty(picked)) onLeader(slot);
                });
            }
            DrawStrips(parent, built, save, picked, onPickUnit);
            DrawBand(parent, built, save, picked, onDetail);
            DrawInfo(parent, built, save, picked, new Vector2(0.08f, InfoY));
        }

        static void DrawParty(Transform parent, List<GameObject> built, SaveBlob save, int slot,
            bool slotSelectable, System.Action<int> onSelectSlot)
        {
            if (save == null || save.PartyIds == null) return;
            var n = LivePartySize(save);
            var dx = n <= PartySize ? 0.17f : (n > 0 ? 0.76f / n : 0.17f);
            for (int i = 0; i < n; i++)
            {
                var id = PartyId(save, i);
                var def = TryChar(id);
                var prog = TryProgress(save, id);
                var lv = prog != null ? prog.Level : 1;
                var uncap = prog != null ? prog.Uncap : 0;
                var x = 0.12f + i * dx;
                var selected = slotSelectable && i == slot;
                var leader = def != null && i == save.LeaderSlot;
                GameObject chip;
                if (def == null)
                    chip = DrawEmptySlot(parent, new Vector2(x, PartyY), new Vector2(StripW, StripH), selected);
                else
                    chip = CharacterPresenter.DrawChip(parent, def,
                        new Vector2(x, PartyY), new Vector2(StripW, StripH),
                        selected, leader, lv, uncap);
                if (chip == null) continue;
                Bind(chip, onSelectSlot, i);
                if (built != null) built.Add(chip);
            }
        }

        static void DrawStrips(Transform parent, List<GameObject> built, SaveBlob save, string selectedId,
            System.Action<string> onPick)
        {
            var order = RosterOrder(save, selectedId);
            var n = order.Count;
            if (n <= 0) return;
            const float x0 = 0.12f;
            const float x1 = 0.86f;
            var dx = n <= 1 ? 0f : Mathf.Min(0.095f, (x1 - x0) / (n - 1));
            for (int i = n - 1; i >= 0; i--)
            {
                var id = order[i];
                var def = TryChar(id);
                if (def == null) continue;
                var prog = TryProgress(save, id);
                var lv = prog != null ? prog.Level : 1;
                var uncap = prog != null ? prog.Uncap : 0;
                var chip = DrawRosterStrip(parent, def,
                    new Vector2(x0 + i * dx, RosterY), new Vector2(RosterW, RosterH),
                    id == selectedId, lv, uncap);
                if (chip == null) continue;
                Bind(chip, onPick, id);
                if (built != null) built.Add(chip);
            }
        }

        /// <summary>
        /// 名册竖条：印刷暗黑奢华。元素色卡框包深色卡底，肖像头顶网点溶解，
        /// 左缘红焰进化星，选中烫金细框，数据里原生 5 星以上烫 S 橙标。
        /// </summary>
        static GameObject DrawRosterStrip(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size,
            bool selected, int level, int uncap)
        {
            var el = VisualTokens.Element(def.Element);
            var box = selected ? size + new Vector2(10f, 10f) : size;
            var go = new GameObject("strip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.32f);
            rt.sizeDelta = box;
            rt.anchoredPosition = Vector2.zero;
            var rim = go.GetComponent<Image>();
            UiSprites.Apply(rim, UiSprites.Round());
            rim.color = selected ? VisualTokens.GoldSelect : el;
            rim.raycastTarget = true;

            Img(go.transform, "well", UiSprites.Round(), new Color(0.05f, 0.05f, 0.06f, 1f),
                new Vector2(0.5f, 0.5f), box - new Vector2(6f, 6f));

            var win = box - new Vector2(12f, 12f);
            var cut = new GameObject("cut", typeof(RectTransform), typeof(Image), typeof(Mask));
            cut.transform.SetParent(go.transform, false);
            var crt = cut.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = win;
            crt.anchoredPosition = Vector2.zero;
            var cimg = cut.GetComponent<Image>();
            UiSprites.Apply(cimg, UiSprites.Round());
            cimg.color = new Color(0.08f, 0.08f, 0.09f, 1f);
            cimg.raycastTarget = false;
            cut.GetComponent<Mask>().showMaskGraphic = true;

            var face = CharacterArt.Face(def.Id);
            if (face != null)
            {
                var paint = Img(cut.transform, "paint", face, Color.white,
                    new Vector2(0.5f, 0.40f), new Vector2(win.x * 1.16f, win.y * 1.34f));
                paint.preserveAspect = true;
            }
            else
            {
                var wash = Color.Lerp(el, new Color(0.05f, 0.05f, 0.05f, 1f), 0.62f);
                wash.a = 1f;
                Img(cut.transform, "sil", UiSprites.Soft(), wash,
                    new Vector2(0.5f, 0.48f), new Vector2(win.x * 0.72f, win.y * 0.88f));
            }
            Img(cut.transform, "shade", UiSprites.Soft(), new Color(0f, 0f, 0f, 0.30f),
                new Vector2(0.5f, 0.04f), new Vector2(win.x * 1.1f, win.y * 0.22f));
            HalftoneDissolve(cut.transform, win);

            StarColumn(go.transform, CharacterPresenter.EvolvedStars(def, uncap), box);
            if (def.NativeStar >= 5)
                SClassPatch(go.transform);

            var lvPx = Mathf.Clamp(Mathf.RoundToInt(box.y * 0.066f), 12, 20);
            var lv = CharacterPresenter.Label(go.transform, "LV." + level, lvPx, Color.white,
                new Vector2(0.30f, 0.148f), new Vector2(64f, 24f), true);
            lv.alignment = TextAnchor.MiddleLeft;
            lv.rectTransform.pivot = new Vector2(0f, 0.5f);
            if (uncap > 0)
            {
                var plus = CharacterPresenter.Label(go.transform, "+" + uncap, Mathf.Max(11, lvPx - 5),
                    VisualTokens.YellowValue, new Vector2(0.30f, 0.205f), new Vector2(48f, 20f), true);
                plus.alignment = TextAnchor.MiddleLeft;
                plus.rectTransform.pivot = new Vector2(0f, 0.5f);
            }

            var bar = Img(go.transform, "nbar", UiSprites.Round(), new Color(0f, 0f, 0f, 0.78f),
                new Vector2(0.5f, 0.058f), new Vector2(box.x - 14f, 24f));
            Img(bar.transform, "eline", null, el,
                new Vector2(0.5f, 0.08f), new Vector2(box.x - 22f, 2f));
            CharacterPresenter.Label(go.transform, def.Name, 16, Color.white,
                new Vector2(0.5f, 0.062f), new Vector2(box.x - 20f, 24f), true);

            if (selected)
                Img(go.transform, "wire", UiSprites.WireFrame(), VisualTokens.GoldSelect,
                    new Vector2(0.5f, 0.5f), box - new Vector2(9f, 9f));
            return go;
        }

        /// <summary>头顶网点溶解：软墨盖 + 三层平铺半调网点，浓度向上递增，肖像化进卡底。</summary>
        static void HalftoneDissolve(Transform parent, Vector2 win)
        {
            Img(parent, "cap", UiSprites.Soft(), new Color(0.05f, 0.05f, 0.06f, 0.80f),
                new Vector2(0.5f, 0.99f), new Vector2(win.x * 1.08f, win.y * 0.14f));
            for (int i = 0; i < 3; i++)
            {
                var h = win.y * 0.11f;
                var y = 0.985f - i * (h / win.y) * 0.92f;
                var tone = Img(parent, "tone", UiSprites.Halftone(),
                    new Color(0.05f, 0.05f, 0.06f, 0.95f - i * 0.30f),
                    new Vector2(0.5f, y), new Vector2(win.x, h));
                if (tone != null)
                {
                    tone.type = Image.Type.Tiled;
                    tone.preserveAspect = false;
                }
            }
        }

        /// <summary>进化星：红焰（StarEvolved）不是金，沿左缘竖排。</summary>
        static void StarColumn(Transform t, int n, Vector2 box)
        {
            n = Mathf.Clamp(n, 0, 6);
            if (n <= 0) return;
            var pip = Mathf.Clamp(box.x * 0.14f, 10f, 15f);
            var step = (pip + 2f) / Mathf.Max(1f, box.y);
            for (int i = 0; i < n; i++)
                Img(t, "star", UiSprites.Star(), VisualTokens.StarEvolved,
                    new Vector2(0.13f, 0.148f + i * step), new Vector2(pip, pip));
        }

        /// <summary>S 级橙标：仅当数据里原生 5 星以上。</summary>
        static void SClassPatch(Transform t)
        {
            var patch = Img(t, "sClass", UiSprites.Halftone(), new Color(0.08f, 0.04f, 0.02f, 0.92f),
                new Vector2(0.87f, 0.945f), new Vector2(32f, 22f));
            if (patch != null)
            {
                patch.type = Image.Type.Tiled;
                patch.pixelsPerUnitMultiplier = 0.7f;
            }
            var wire = Img(t, "sWire", UiSprites.WireFrame(), VisualTokens.OrangeSClass,
                new Vector2(0.87f, 0.945f), new Vector2(34f, 24f));
            CharacterPresenter.Label(patch != null ? patch.transform : t, "S", 14, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(26f, 18f), true);
        }

        static GameObject DrawEmptySlot(Transform parent, Vector2 anchor, Vector2 size, bool selected)
        {
            var box = selected ? size + new Vector2(8f, 42f) : size;
            var go = new GameObject("empty", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.32f);
            rt.sizeDelta = box;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = selected ? VisualTokens.GoldSelect : VisualTokens.SlotRim;
            img.raycastTarget = true;

            var inner = box - new Vector2(selected ? 12f : 8f, selected ? 12f : 8f);
            Img(go.transform, "well", UiSprites.Round(), VisualTokens.SlotWell,
                new Vector2(0.5f, 0.5f), inner);

            var ghost = VisualTokens.SlotRim;
            ghost.a = 0.55f;
            Img(go.transform, "ghost", UiSprites.Hex(), ghost,
                new Vector2(0.5f, 0.58f), new Vector2(52f, 52f));
            Img(go.transform, "plus", UiSprites.Plus(), VisualTokens.TextMuted,
                new Vector2(0.5f, 0.58f), new Vector2(26f, 26f));
            CharacterPresenter.Label(go.transform, "空位", 16, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.36f), new Vector2(100f, 28f), false);

            var dash = Img(go.transform, "dash", UiSprites.Dashed(), VisualTokens.SlotRim,
                new Vector2(0.5f, 0.22f), new Vector2(box.x * 0.62f, 8f));
            if (dash != null)
            {
                dash.type = Image.Type.Tiled;
                dash.preserveAspect = false;
            }
            return go;
        }

        static void DrawLeaderBtn(Transform parent, List<GameObject> built, System.Action click)
        {
            var size = new Vector2(100f, 36f);
            var go = HitPlate(parent, "队长", new Vector2(0.12f, PartyY), size,
                new Color(0.04f, 0.04f, 0.04f, 0.94f), click);
            DashRim(go.transform, size, new Color(1f, 1f, 1f, 0.94f));
            var tx = CharacterPresenter.Label(go.transform, "队长", 16, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(88f, 28f), false);
            tx.fontStyle = FontStyle.Bold;
            tx.rectTransform.localScale = new Vector3(0.72f, 1.16f, 1f);
            if (built != null) built.Add(go);
        }

        static void DrawBand(Transform parent, List<GameObject> built, SaveBlob save, string id,
            System.Action<string> onDetail)
        {
            if (!string.IsNullOrEmpty(id) && TryChar(id) != null && InParty(save, id))
            {
                var tag = Img(parent, "joined", UiSprites.Round(), new Color(0.05f, 0.05f, 0.05f, 0.94f),
                    new Vector2(0.18f, BandY), new Vector2(210f, 40f));
                CharacterPresenter.Label(tag.transform, "已加入队伍", 16, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(200f, 34f), false);
                if (built != null) built.Add(tag.gameObject);
            }

            DrawPower(parent, built, save, new Vector2(0.55f, BandY), 36);

            if (string.IsNullOrEmpty(id) || TryChar(id) == null) return;
            var detail = OutlineBtn(parent, "详情", new Vector2(0.22f, DetailY), new Vector2(168f, 46f),
                VisualTokens.GoldSelect, () =>
                {
                    if (onDetail != null) onDetail(id);
                });
            if (built != null) built.Add(detail);
        }

        static void DrawInfo(Transform parent, List<GameObject> built, SaveBlob save, string id, Vector2 anchor)
        {
            var def = TryChar(id);
            if (def == null)
            {
                Txt(parent, built, "空位", 28, VisualTokens.TextMuted, anchor,
                    new Vector2(720f, 40f), true, TextAnchor.MiddleLeft);
                Txt(parent, built, "点下方竖条加入编队", 18, VisualTokens.TextSecondary,
                    anchor + new Vector2(0f, -0.032f), new Vector2(720f, 32f), false, TextAnchor.MiddleLeft);
                return;
            }
            var prog = TryProgress(save, id);
            var grown = Growth.Apply(def, prog);
            if (grown == null) return;
            var n = CharacterPresenter.EvolvedStars(def, prog != null ? prog.Uncap : 0);
            var stars = "";
            for (int i = 0; i < n; i++) stars += "★";
            var el = VisualTokens.Element(def.Element);

            Txt(parent, built, def.Name, 28, Color.white, anchor, new Vector2(720f, 40f), true, TextAnchor.MiddleLeft);
            PlaceGem(parent, built, new Vector2(anchor.x + 0.018f, anchor.y - 0.030f), 26f, el);
            Txt(parent, built, CharacterPresenter.RoleLine(def), 18, el,
                anchor + new Vector2(0.052f, -0.030f), new Vector2(360f, 32f), false, TextAnchor.MiddleLeft);
            Txt(parent, built, stars, 20, VisualTokens.StarEvolved,
                anchor + new Vector2(0.30f, -0.030f), new Vector2(280f, 32f), false, TextAnchor.MiddleLeft);
            Txt(parent, built, "战斗力  " + Growth.CombatPower(grown), 34, VisualTokens.YellowValue,
                anchor + new Vector2(0f, -0.062f), new Vector2(720f, 40f), false, TextAnchor.MiddleLeft);
            Txt(parent, built,
                "生命  " + grown.Hp + "    攻击  " + grown.Atk + "    防御  " + grown.Def,
                18, VisualTokens.TextStat,
                anchor + new Vector2(0f, -0.092f), new Vector2(900f, 30f), false, TextAnchor.MiddleLeft);
        }

        static void DrawPower(Transform parent, List<GameObject> built, SaveBlob save, Vector2 anchor, int px)
        {
            Txt(parent, built, "战斗力  " + TeamPower(save), px, VisualTokens.YellowValue,
                anchor, new Vector2(420f, 48f), false, TextAnchor.MiddleCenter);
        }

        static int TeamPower(SaveBlob save)
        {
            if (save == null || save.PartyIds == null) return 0;
            var n = 0;
            var cap = LivePartySize(save);
            for (int i = 0; i < save.PartyIds.Length && i < cap; i++)
            {
                var def = TryChar(save.PartyIds[i]);
                if (def == null) continue;
                n += Growth.CombatPower(Growth.Apply(def, TryProgress(save, save.PartyIds[i])));
            }
            return n;
        }

        static int FocusSlot(SaveBlob save, int fallback)
        {
            var last = Mathf.Max(0, LivePartySize(save) - 1);
            var slot = Mathf.Clamp(fallback, 0, last);
            if (!string.IsNullOrEmpty(PartyId(save, slot))) return slot;
            var cap = LivePartySize(save);
            for (int i = 0; i < cap; i++)
                if (!string.IsNullOrEmpty(PartyId(save, i))) return i;
            return slot;
        }

        static List<string> RosterOrder(SaveBlob save, string selectedId)
        {
            var src = ListIds(save);
            var order = new List<string>();
            if (!string.IsNullOrEmpty(selectedId))
            {
                for (int i = 0; i < src.Count; i++)
                    if (src[i] == selectedId) { order.Add(selectedId); break; }
            }
            for (int i = 0; i < src.Count; i++)
            {
                var id = src[i];
                if (string.IsNullOrEmpty(id) || id == selectedId) continue;
                if (TryChar(id) == null) continue;
                order.Add(id);
            }
            return order;
        }

        static List<string> ListIds(SaveBlob save)
        {
            var ids = new List<string>();
            if (save != null && save.Roster != null && save.Roster.Count > 0)
            {
                for (int i = 0; i < save.Roster.Count; i++)
                    if (!string.IsNullOrEmpty(save.Roster[i])) ids.Add(save.Roster[i]);
                return ids;
            }
            var play = Catalog.PlayableIds;
            if (play == null) return ids;
            for (int i = 0; i < play.Length; i++)
                if (!string.IsNullOrEmpty(play[i])) ids.Add(play[i]);
            return ids;
        }

        static bool InParty(SaveBlob save, string id)
        {
            if (save == null || save.PartyIds == null || string.IsNullOrEmpty(id)) return false;
            var cap = LivePartySize(save);
            for (int i = 0; i < save.PartyIds.Length && i < cap; i++)
                if (save.PartyIds[i] == id) return true;
            return false;
        }

        public static int LivePartySize(SaveBlob save)
        {
            return save != null ? save.LivePartyCap : FightStats.DefaultPartyCap;
        }

        static string PartyId(SaveBlob save, int i)
        {
            if (save == null || save.PartyIds == null || i < 0 || i >= save.PartyIds.Length) return null;
            return save.PartyIds[i];
        }

        static CharacterDef TryChar(string id)
        {
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null) return null;
            CharacterDef def;
            return Catalog.Characters.TryGetValue(id, out def) ? def : null;
        }

        static UnitProgress TryProgress(SaveBlob save, string id)
        {
            if (save == null || save.Units == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < save.Units.Count; i++)
            {
                var u = save.Units[i];
                if (u != null && u.Id == id) return u;
            }
            return null;
        }

        static void Bind(GameObject go, System.Action<int> click, int value)
        {
            if (go == null || click == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var captured = value;
            btn.onClick.AddListener(() => click(captured));
        }

        static void Bind(GameObject go, System.Action<string> click, string value)
        {
            if (go == null || click == null || string.IsNullOrEmpty(value)) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var captured = value;
            btn.onClick.AddListener(() => click(captured));
        }

        static void PlaceGem(Transform parent, List<GameObject> built, Vector2 anchor, float size, Color color)
        {
            var go = PlaceGem(parent, anchor, size, color);
            if (built != null) built.Add(go);
        }

        static GameObject PlaceGem(Transform parent, Vector2 anchor, float size, Color color)
        {
            var hold = new GameObject("gem", typeof(RectTransform));
            hold.transform.SetParent(parent, false);
            var rt = hold.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size + 8f, size + 8f);
            rt.anchoredPosition = Vector2.zero;
            Img(hold.transform, "back", UiSprites.Circle(), new Color(0f, 0f, 0f, 0.78f),
                new Vector2(0.5f, 0.5f), new Vector2(size + 6f, size + 6f));
            var dim = color * 0.55f;
            dim.a = 1f;
            Img(hold.transform, "hex", UiSprites.Hex(), dim,
                new Vector2(0.5f, 0.5f), new Vector2(size + 2f, size + 2f));
            Img(hold.transform, "cut", UiSprites.IceCrystal(), color,
                new Vector2(0.5f, 0.5f), new Vector2(size, size));
            return hold;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (sprite != null) UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text Txt(Transform parent, List<GameObject> built, string text, int size, Color color,
            Vector2 anchor, Vector2 dim, bool outline, TextAnchor align)
        {
            var tx = CharacterPresenter.Label(parent, text, size, color, anchor, dim, outline);
            tx.alignment = align;
            if (align == TextAnchor.MiddleLeft)
            {
                var rt = tx.rectTransform;
                rt.pivot = new Vector2(0f, 0.5f);
            }
            if (built != null) built.Add(tx.gameObject);
            return tx;
        }

        static GameObject OutlineBtn(Transform parent, string label, Vector2 anchor, Vector2 size,
            Color line, System.Action click)
        {
            var fill = new Color(0.07f, 0.06f, 0.05f, 0.42f);
            var go = HitPlate(parent, label, anchor, size, fill, click);
            Img(go.transform, "wire", UiSprites.WireFrame(), line,
                new Vector2(0.5f, 0.5f), size);
            var tx = CharacterPresenter.Label(go.transform, label, 18, line,
                new Vector2(0.5f, 0.5f), size - new Vector2(10f, 8f), false);
            tx.fontStyle = FontStyle.Bold;
            return go;
        }

        static GameObject HitPlate(Transform parent, string name, Vector2 anchor, Vector2 size,
            Color fill, System.Action click)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = fill;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            if (click != null) btn.onClick.AddListener(() => click());
            return go;
        }

        static void DashRim(Transform parent, Vector2 box, Color color)
        {
            const float th = 8f;
            DashEdge(parent, "dashT", color, new Vector2(0.5f, 1f), new Vector2(box.x, th), 0f);
            DashEdge(parent, "dashB", color, new Vector2(0.5f, 0f), new Vector2(box.x, th), 0f);
            DashEdge(parent, "dashL", color, new Vector2(0f, 0.5f), new Vector2(box.y, th), 90f);
            DashEdge(parent, "dashR", color, new Vector2(1f, 0.5f), new Vector2(box.y, th), 90f);
        }

        static Image DashEdge(Transform parent, string name, Color color, Vector2 anchor, Vector2 size, float zRot)
        {
            var img = Img(parent, name, UiSprites.Dashed(), color, anchor, size);
            if (img == null) return null;
            img.type = Image.Type.Tiled;
            img.preserveAspect = false;
            if (Mathf.Abs(zRot) > 0.01f)
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, zRot);
            return img;
        }
    }
}
