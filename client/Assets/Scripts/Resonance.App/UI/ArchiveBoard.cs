using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 图录：印版暗金马赛克。顶匾、四分类、金星筛、六列方砖。
    /// 元素丝框、目录金星（非战队红星）、金线、虚线规、半调洗。无六页签。
    /// </summary>
    public static class ArchiveBoard
    {
        const int Cols = 6;
        const int Rows = 5;
        const int UnitCount = 25;
        const int Slots = 30;
        const float Brick = 150f;
        const float Well = 136f;
        const float TileW = 150f;
        const float TileH = 178f;
        const float PlaqueW = 1080f;
        const float PlaqueH = 160f;

        static readonly string[] Cats = { "契灵", "残章", "契偶", "造型" };
        static readonly int[] CatSeals = { 1, 3, 4, 5 };
        static readonly int[] StarKeys = { 5, 4, 3, 2, 1 };
        static readonly string[] StarCaps = { "5+", "4", "3", "2", "1" };

        static int _starFilter = 5;
        static Transform _host;
        static IList<string> _owned;
        static Action<string> _inspect;

        public static void Draw(Transform parent, IList<string> ownedIds, Action<string> onInspect)
        {
            if (parent == null) return;
            _host = parent;
            _owned = ownedIds;
            _inspect = onInspect;

            var old = parent.Find("ArchiveBoard");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var built = new List<GameObject>(512);
            var root = OverlayDraw.Group(parent, built, "ArchiveBoard");
            if (root == null) return;
            root.SetAsFirstSibling();

            UiChrome.MosaicFill(root, built);
            OverlayDraw.Wash(root, built, new Color(0f, 0f, 0f, 0.42f), false);
            Tone(root, built, "print", new Vector2(0.5f, 0.46f), new Vector2(1080f, 1180f),
                new Color(1f, 1f, 1f, 0.045f));

            var ids = Ids();
            Plaque(root, built, CountOwned(ids, ownedIds));
            Chips(root, built);
            StarRow(root, built);
            Wall(root, built, ids, ownedIds, onInspect);
            Footer(root, built);
        }

        static string[] Ids()
        {
            var play = Catalog.PlayableIds;
            if (play != null && play.Length > 0) return play;
            var matrix = Catalog.MatrixIds;
            if (matrix != null && matrix.Length > 0) return matrix;
            return new string[0];
        }

        static void Plaque(Transform parent, List<GameObject> built, int have)
        {
            var y = 1f - (PlaqueH * 0.5f) / 1920f;
            var bar = OverlayDraw.Pic(parent, built, "plaque", new Vector2(0.5f, y),
                new Vector2(PlaqueW, PlaqueH), new Color(0.07f, 0.05f, 0.04f, 1f), UiSprites.Pixel());
            if (bar != null) bar.raycastTarget = false;

            Tone(parent, built, "tone", new Vector2(0.5f, y),
                new Vector2(PlaqueW - 24f, PlaqueH - 28f), new Color(0f, 0f, 0f, 0.28f));

            OverlayDraw.Bar(parent, built, new Vector2(0.5f, y + 0.040f),
                new Vector2(PlaqueW, 3f), VisualTokens.GoldMetal);
            OverlayDraw.Bar(parent, built, new Vector2(0.5f, y - 0.040f),
                new Vector2(PlaqueW, 3f), VisualTokens.GoldSelect);
            Wire(parent, built, new Vector2(0.5f, y),
                new Vector2(PlaqueW - 14f, PlaqueH - 14f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.75f));

            var halo = OverlayDraw.Pic(parent, built, "halo", new Vector2(0.5f, y),
                new Vector2(560, 120),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.22f),
                UiSprites.Soft());
            if (halo != null) halo.raycastTarget = false;

            Ornament(parent, built, new Vector2(0.080f, y + 0.010f));
            Ornament(parent, built, new Vector2(0.862f, y + 0.010f));
            var slashGold = new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.90f);
            Quiet(OverlayDraw.Pic(parent, built, "slashL", new Vector2(0.392f, y + 0.008f),
                new Vector2(54, 15), slashGold, UiSprites.Slash()));
            Quiet(OverlayDraw.Pic(parent, built, "slashR", new Vector2(0.608f, y + 0.008f),
                new Vector2(54, 15), slashGold, UiSprites.Slash()));

            OverlayDraw.Label(parent, built, "图录", 48, VisualTokens.GoldTitle,
                new Vector2(0.50f, y + 0.008f), new Vector2(480, 64), false, true, 3f, true);
            OverlayDraw.Label(parent, built, "图鉴", 16, VisualTokens.GoldMetal,
                new Vector2(0.50f, y - 0.028f), new Vector2(200, 28), false, false);

            OverlayDraw.Label(parent, built, "已录  " + have + "/" + UnitCount, 20, VisualTokens.YellowValue,
                new Vector2(0.74f, y - 0.010f), new Vector2(170, 32), false, true, 2f, true);
        }

        static void Ornament(Transform parent, List<GameObject> built, Vector2 anchor)
        {
            var gold = new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.60f);
            Quiet(OverlayDraw.Pic(parent, built, "seal", anchor, new Vector2(40, 40), gold, UiSprites.Stamp(4)));
            Quiet(OverlayDraw.Pic(parent, built, "star", anchor + new Vector2(0.017f, 0.021f),
                new Vector2(14, 14),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.85f),
                UiSprites.Star()));
        }

        static void Chips(Transform parent, List<GameObject> built)
        {
            OverlayDraw.DashLine(parent, built, new Vector2(0.5f, 0.870f), PlaqueW - 60f);

            const float x0 = 0.118f;
            const float dx = 0.168f;
            for (int i = 0; i < Cats.Length; i++)
            {
                var on = i == 0;
                var anchor = new Vector2(x0 + i * dx, 0.900f);
                var fill = on ? new Color(0.17f, 0.12f, 0.05f, 1f) : new Color(0.08f, 0.07f, 0.06f, 0.96f);
                Quiet(OverlayDraw.Pic(parent, built, Cats[i], anchor, new Vector2(176, 54), fill, UiSprites.Round()));
                Wire(parent, built, anchor, new Vector2(168, 46),
                    on ? new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.95f)
                        : new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.60f));
                if (on)
                    Tone(parent, built, "tone", anchor, new Vector2(168, 46),
                        new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.10f));
                Quiet(OverlayDraw.Pic(parent, built, "seal", anchor + new Vector2(-0.052f, 0f),
                    new Vector2(24, 24), on ? VisualTokens.GoldSelect : VisualTokens.GoldMetal,
                    UiSprites.Stamp(CatSeals[i % CatSeals.Length])));
                OverlayDraw.Label(parent, built, Cats[i], 18,
                    on ? VisualTokens.GoldTitle : VisualTokens.GoldMetal,
                    anchor + new Vector2(0.012f, 0f), new Vector2(120, 44), false, true, 2f, on);
            }
        }

        static void StarRow(Transform parent, List<GameObject> built)
        {
            const float x0 = 0.22f;
            const float dx = 0.125f;
            for (int i = 0; i < StarKeys.Length; i++)
            {
                var key = StarKeys[i];
                var on = _starFilter == key;
                var anchor = new Vector2(x0 + i * dx, 0.828f);
                var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(112, 44), () => SetStar(key));
                go.name = "star" + StarCaps[i];
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    UiSprites.Apply(img, UiSprites.Round());
                    img.color = on ? new Color(0.20f, 0.15f, 0.06f, 1f) : new Color(0.09f, 0.09f, 0.09f, 0.94f);
                }
                Wire(go.transform, built, new Vector2(0.5f, 0.5f), new Vector2(104, 36),
                    on ? new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.95f)
                        : new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.70f));
                if (on)
                    Tone(go.transform, built, "tone", new Vector2(0.5f, 0.5f), new Vector2(104, 36),
                        new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.12f));

                Quiet(OverlayDraw.Pic(go.transform, built, "star", new Vector2(0.28f, 0.5f),
                    new Vector2(20, 20), VisualTokens.StarCatalog, UiSprites.Star()));
                OverlayDraw.Label(go.transform, built, StarCaps[i], 18,
                    on ? VisualTokens.YellowValue : VisualTokens.GoldMetal,
                    new Vector2(0.62f, 0.5f), new Vector2(64, 36), false, true, 2f, on);
            }

            OverlayDraw.Label(parent, built, "星等", 15, VisualTokens.TextMuted,
                new Vector2(0.052f, 0.772f), new Vector2(64, 28), true, false);
            var n = _starFilter <= 0 ? 5 : _starFilter;
            GoldStars(parent, built, new Vector2(0.098f, 0.772f), n, 18f);
            OverlayDraw.DashLine(parent, built, new Vector2(0.60f, 0.772f), 660f);
        }

        static void SetStar(int star)
        {
            _starFilter = _starFilter == star ? 0 : star;
            if (_host != null) Draw(_host, _owned, _inspect);
        }

        static void Wall(Transform parent, List<GameObject> built, string[] ids, IList<string> ownedIds,
            Action<string> onInspect)
        {
            const float x0 = 0.108f;
            const float dx = 0.157f;
            const float topY = 0.688f;
            const float stepY = 0.110f;

            var wall = Slice(ids);
            for (int i = 0; i < Slots; i++)
            {
                var c = i % Cols;
                var r = i / Cols;
                if (r >= Rows) break;
                var id = wall[i];
                DrawTile(parent, built, id, Has(ownedIds, id),
                    new Vector2(x0 + c * dx, topY - r * stepY), onInspect, i);
            }
        }

        static string[] Slice(string[] ids)
        {
            var wall = new string[Slots];
            var n = 0;
            if (ids == null) return wall;
            for (int i = 0; i < ids.Length && n < Slots; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (!StarOk(TryChar(id))) continue;
                wall[n++] = id;
            }
            return wall;
        }

        static bool StarOk(CharacterDef def)
        {
            if (def == null) return false;
            if (_starFilter <= 0) return true;
            if (_starFilter >= 5) return def.NativeStar >= 5;
            return def.NativeStar == _starFilter;
        }

        static void DrawTile(Transform parent, List<GameObject> built, string id, bool owned,
            Vector2 anchor, Action<string> onInspect, int seed)
        {
            var def = TryChar(id);
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(TileW, TileH), () =>
            {
                if (onInspect != null && !string.IsNullOrEmpty(id)) onInspect(id);
            });
            go.name = string.IsNullOrEmpty(id) ? "empty" : id;
            var hit = go.GetComponent<Image>();
            if (hit != null)
            {
                hit.sprite = UiSprites.Pixel();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                var cr = hit.canvasRenderer;
                if (cr != null) cr.cullTransparentMesh = false;
            }

            var brickAt = new Vector2(0.5f, 0.62f);
            if (def == null)
            {
                var cell = CharacterPresenter.DrawTile(go.transform, null, brickAt, new Vector2(Brick, Brick));
                MuteInner(cell);
                OverlayDraw.Track(built, cell);
                if (cell != null)
                {
                    var mosaic = OverlayDraw.Pic(cell.transform, built, "mosaic", new Vector2(0.5f, 0.5f),
                        new Vector2(Well, Well), new Color(1f, 1f, 1f, 0.32f), UiSprites.FloorMosaic());
                    if (mosaic != null)
                    {
                        mosaic.raycastTarget = false;
                        mosaic.preserveAspect = false;
                        mosaic.transform.SetSiblingIndex(1);
                    }
                    Wire(cell.transform, built, new Vector2(0.5f, 0.5f), new Vector2(Well - 6f, Well - 6f),
                        new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.30f));
                    var wmSprite = Watermark(seed);
                    var wmSize = wmSprite == UiSprites.Slash() ? new Vector2(64f, 22f) : new Vector2(44f, 44f);
                    Quiet(OverlayDraw.Pic(cell.transform, built, "wm", new Vector2(0.5f, 0.54f),
                        wmSize, new Color(0.55f, 0.48f, 0.32f, 0.50f), wmSprite));
                }
                return;
            }

            var elCol = VisualTokens.Element(def.Element);
            var cellLive = CharacterPresenter.DrawTile(go.transform, def, brickAt, new Vector2(Brick, Brick), true, false);
            MuteInner(cellLive);
            OverlayDraw.Track(built, cellLive);
            StripOnTileName(cellLive);

            Wire(go.transform, built, brickAt, new Vector2(Well + 4f, Well + 4f),
                new Color(elCol.r, elCol.g, elCol.b, 0.85f));
            Tone(go.transform, built, "tone", new Vector2(0.5f, 0.305f), new Vector2(Well, 44f),
                new Color(0.02f, 0.02f, 0.02f, 0.50f));
            Quiet(OverlayDraw.Pic(go.transform, built, "slash", new Vector2(0.135f, 0.952f),
                new Vector2(34f, 12f), new Color(elCol.r, elCol.g, elCol.b, 0.55f), UiSprites.Slash()));

            if (!owned)
                Quiet(OverlayDraw.Pic(go.transform, built, "dim", brickAt,
                    new Vector2(Well, Well), new Color(0f, 0f, 0f, 0.55f), UiSprites.Round()));

            CatalogStars(go.transform, built, new Vector2(0.5f, 0.27f), def.NativeStar, 12f);

            OverlayDraw.Label(go.transform, built, def.Name, 15,
                owned ? VisualTokens.TextPrimary : VisualTokens.TextMuted,
                new Vector2(0.5f, 0.10f), new Vector2(148, 28), false, true, 2f);
        }

        static void CatalogStars(Transform parent, List<GameObject> built, Vector2 center, int n, float pip)
        {
            n = Mathf.Clamp(n, 0, 6);
            if (n <= 0) return;
            const float gap = 3f;
            var total = n * pip + (n - 1) * gap;
            Quiet(OverlayDraw.Pic(parent, built, "starBg", center,
                new Vector2(total + 12f, pip + 8f), new Color(0f, 0f, 0f, 0.62f), UiSprites.Round()));
            var hold = new GameObject("tileStars", typeof(RectTransform));
            hold.transform.SetParent(parent, false);
            var rt = hold.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = center;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(total, pip);
            rt.anchoredPosition = Vector2.zero;
            OverlayDraw.Track(built, hold);
            for (int i = 0; i < n; i++)
            {
                var x = (i * (pip + gap) + pip * 0.5f) / total;
                Quiet(OverlayDraw.Pic(hold.transform, built, "star", new Vector2(x, 0.5f),
                    new Vector2(pip, pip), VisualTokens.StarCatalog, UiSprites.Star()));
            }
        }

        static Sprite Watermark(int seed)
        {
            var kind = ((seed % 3) + 3) % 3;
            if (kind == 0) return UiSprites.Star();
            if (kind == 1) return UiSprites.Stamp(seed % 6);
            return UiSprites.Slash();
        }

        static void Footer(Transform parent, List<GameObject> built)
        {
            OverlayDraw.DashLine(parent, built, new Vector2(0.5f, 0.152f), 920f);
            Quiet(OverlayDraw.Pic(parent, built, "starL", new Vector2(0.418f, 0.118f),
                new Vector2(14, 14), VisualTokens.StarCatalog, UiSprites.Star()));
            Quiet(OverlayDraw.Pic(parent, built, "starR", new Vector2(0.582f, 0.118f),
                new Vector2(14, 14), VisualTokens.StarCatalog, UiSprites.Star()));
            OverlayDraw.Label(parent, built, "契灵回响 · 图录", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.118f), new Vector2(320, 26), false, false);
        }

        static Image Wire(Transform parent, List<GameObject> built, Vector2 anchor, Vector2 size, Color color)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", anchor, size, color, UiSprites.WireFrame());
            if (img != null) img.raycastTarget = false;
            return img;
        }

        static Image Tone(Transform parent, List<GameObject> built, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var img = OverlayDraw.Pic(parent, built, name, anchor, size, color, UiSprites.Halftone());
            if (img == null) return null;
            img.type = Image.Type.Tiled;
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        static void Quiet(Image img)
        {
            if (img != null) img.raycastTarget = false;
        }

        static void MuteInner(GameObject cell)
        {
            if (cell == null) return;
            var btn = cell.GetComponent<Button>();
            if (btn != null) btn.enabled = false;
            var img = cell.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        static void StripOnTileName(GameObject tile)
        {
            if (tile == null) return;
            var t = tile.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                if (c.name == "nbar" || c.name == "t")
                    c.gameObject.SetActive(false);
            }
        }

        static void GoldStars(Transform parent, List<GameObject> built, Vector2 anchor, int n, float pip)
        {
            n = Mathf.Clamp(n, 0, 6);
            if (n <= 0) return;
            var hold = new GameObject("filterStars", typeof(RectTransform));
            hold.transform.SetParent(parent, false);
            var rt = hold.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(n * (pip + 4f), pip);
            rt.anchoredPosition = Vector2.zero;
            OverlayDraw.Track(built, hold);
            for (int i = 0; i < n; i++)
            {
                Quiet(OverlayDraw.Pic(hold.transform, built, "star", new Vector2((i + 0.5f) / n, 0.5f),
                    new Vector2(pip, pip), VisualTokens.StarCatalog, UiSprites.Star()));
            }
        }

        static int CountOwned(string[] ids, IList<string> ownedIds)
        {
            if (ids == null) return 0;
            var n = 0;
            var seen = 0;
            for (int i = 0; i < ids.Length && seen < UnitCount; i++)
            {
                if (string.IsNullOrEmpty(ids[i])) continue;
                seen++;
                if (Has(ownedIds, ids[i])) n++;
            }
            return n;
        }

        static bool Has(IList<string> ownedIds, string id)
        {
            if (ownedIds == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < ownedIds.Count; i++)
                if (ownedIds[i] == id) return true;
            return false;
        }

        static CharacterDef TryChar(string id)
        {
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null) return null;
            CharacterDef def;
            return Catalog.Characters.TryGetValue(id, out def) ? def : null;
        }
    }
}
