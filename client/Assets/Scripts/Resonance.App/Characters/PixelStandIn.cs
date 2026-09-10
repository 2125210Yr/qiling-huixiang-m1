using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    // 48×64 point-filter SNES body. Roster, chips, and battle. Never the painted lobby presenter.
    public static class PixelStandIn
    {
        public const int W = 48;
        public const int H = 64;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(CharacterDef def, string skinId)
        {
            return Get(def, skinId, 0);
        }

        public static Sprite Get(CharacterDef def, string skinId, int frame)
        {
            if (def == null) return Blank();
            frame = frame <= 0 ? 0 : 1;
            var key = def.Id + "|" + (skinId ?? "") + "|" + frame;
            Sprite sprite;
            if (Cache.TryGetValue(key, out sprite) && sprite != null) return sprite;
            sprite = Bake(def, skinId, frame);
            Cache[key] = sprite;
            return sprite;
        }

        public static Texture2D Tex(CharacterDef def, string skinId = "", int frame = 0)
        {
            if (def == null) return null;
            var s = Get(def, skinId, frame);
            return s != null ? s.texture as Texture2D : null;
        }

        static Sprite _blank;

        static Sprite Blank()
        {
            if (_blank != null) return _blank;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels32(new Color32[4]);
            tex.Apply(false, false);
            _blank = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 24f);
            return _blank;
        }

        static Sprite Bake(CharacterDef def, string skinId, int frame)
        {
            var pix = new Pix();
            var pal = Pal.From(def, skinId);
            if (def.IsBoss) PaintBoss(pix, pal);
            else if (def.IsEnemy) PaintEnemy(pix, pal, def);
            else PaintAlly(pix, pal, def);
            if (frame == 1) WalkPose(pix);
            pix.Outline(pal.Ink);
            PaintFace(pix, pal, def.Id, def.IsEnemy, def.IsBoss);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pix.D);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.18f), 24f);
        }

        static void PaintAlly(Pix p, Pal pal, CharacterDef def)
        {
            p.Oval(24, 6, 10, 3, pal.Shadow);
            switch (def.Id)
            {
                case "C001":
                    p.Box(16, 8, 6, 10, pal.ClothDark);
                    p.Box(26, 8, 6, 10, pal.ClothDark);
                    p.Box(15, 16, 18, 8, pal.Cloth);
                    p.Box(17, 22, 14, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(18, 44, 16, 10, pal.Hair);
                    p.Box(28, 48, 12, 8, pal.Hair);
                    p.Box(36, 18, 4, 28, pal.Metal);
                    p.Box(37, 44, 3, 6, pal.Accent);
                    break;
                case "C002":
                    p.Box(14, 8, 8, 10, pal.ClothDark);
                    p.Box(26, 8, 8, 10, pal.ClothDark);
                    p.Box(13, 16, 22, 10, pal.Cloth);
                    p.Box(16, 24, 16, 12, pal.ClothLite);
                    p.Box(10, 22, 6, 12, pal.Skin);
                    p.Box(32, 22, 6, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(18, 46, 12, 6, pal.Hair);
                    p.Box(6, 18, 10, 18, pal.Metal);
                    p.Box(8, 20, 6, 14, pal.Accent);
                    break;
                case "C003":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 46, 16, 8, pal.Hair);
                    p.Box(10, 18, 5, 24, pal.Hair);
                    p.Box(33, 18, 5, 24, pal.Hair);
                    p.Box(36, 16, 3, 30, pal.Metal);
                    p.Oval(38, 48, 4, 4, pal.Accent);
                    break;
                case "C004":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 22, 5, 10, pal.Skin);
                    p.Box(31, 22, 5, 10, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 46, 14, 7, pal.Hair);
                    p.Box(30, 20, 7, 26, pal.Hair);
                    p.Oval(10, 30, 5, 5, pal.Accent);
                    break;
                case "C005":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(17, 46, 14, 6, pal.Hair);
                    p.Oval(32, 54, 4, 4, pal.Hair);
                    p.Box(10, 28, 4, 16, pal.Accent);
                    p.Oval(10, 46, 4, 3, pal.Accent);
                    break;
                case "C006":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 46, 16, 8, pal.Hair);
                    p.Box(32, 20, 5, 22, pal.Hair);
                    p.Box(8, 18, 4, 24, pal.ClothDark);
                    p.Box(10, 18, 2, 24, pal.Metal);
                    break;
                case "C007":
                    p.Box(14, 8, 8, 10, pal.ClothDark);
                    p.Box(26, 8, 8, 10, pal.ClothDark);
                    p.Box(13, 16, 22, 10, pal.Cloth);
                    p.Box(16, 24, 16, 12, pal.Metal);
                    p.Box(10, 22, 6, 12, pal.Skin);
                    p.Box(32, 22, 6, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 46, 16, 8, pal.Hair);
                    p.Box(5, 16, 10, 22, pal.Metal);
                    p.Box(7, 18, 6, 18, pal.Accent);
                    break;
                case "C008":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 46, 16, 8, pal.Hair);
                    p.Oval(14, 54, 3, 3, pal.Accent);
                    p.Oval(10, 26, 6, 6, pal.Accent);
                    p.Box(9, 24, 8, 2, pal.Metal);
                    break;
                case "C009":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(16, 44, 16, 10, pal.Hair);
                    p.Oval(16, 52, 2, 2, pal.Accent);
                    p.Oval(36, 24, 5, 5, pal.Accent);
                    p.Box(35, 16, 3, 8, pal.Metal);
                    break;
                case "C010":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(15, 16, 18, 8, pal.Cloth);
                    p.Box(17, 22, 14, 12, pal.ClothDark);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(14, 44, 20, 12, pal.Hair);
                    p.Box(8, 26, 4, 16, pal.Metal);
                    p.Box(36, 26, 4, 16, pal.Metal);
                    break;
                case "C011":
                    p.Box(17, 8, 5, 10, pal.ClothDark);
                    p.Box(26, 8, 5, 10, pal.ClothDark);
                    p.Box(16, 16, 16, 8, pal.Cloth);
                    p.Box(18, 22, 12, 12, pal.ClothLite);
                    p.Box(12, 20, 5, 12, pal.Skin);
                    p.Box(31, 20, 5, 12, pal.Skin);
                    p.Oval(24, 42, 8, 8, pal.Skin);
                    p.Box(20, 46, 14, 10, pal.Hair);
                    p.Box(16, 44, 6, 8, pal.Hair);
                    p.Box(35, 20, 4, 22, pal.Metal);
                    break;
                default:
                    PaintAllyByRole(p, pal, def.Role);
                    break;
            }
        }

        static void PaintAllyByRole(Pix p, Pal pal, Role role)
        {
            p.Box(17, 8, 5, 10, pal.ClothDark);
            p.Box(26, 8, 5, 10, pal.ClothDark);
            p.Box(15, 16, 18, 8, pal.Cloth);
            p.Box(17, 22, 14, 12, pal.ClothLite);
            p.Box(12, 20, 5, 12, pal.Skin);
            p.Box(31, 20, 5, 12, pal.Skin);
            p.Oval(24, 42, 8, 8, pal.Skin);
            p.Box(16, 44, 16, 12, pal.Hair);
            switch (role)
            {
                case Role.Attacker:
                    p.Box(36, 16, 4, 28, pal.Metal);
                    p.Box(37, 42, 3, 6, pal.Accent);
                    break;
                case Role.Defender:
                    p.Box(6, 18, 8, 16, pal.Metal);
                    p.Box(7, 20, 6, 12, pal.ClothDark);
                    break;
                case Role.Debuffer:
                    p.Oval(10, 30, 5, 5, pal.Accent);
                    p.Oval(38, 30, 5, 5, pal.Metal);
                    break;
                case Role.Healer:
                    p.Box(36, 18, 3, 26, pal.Metal);
                    p.Oval(37, 46, 5, 5, pal.Accent);
                    break;
                default:
                    p.Box(8, 22, 4, 14, pal.Metal);
                    p.Oval(10, 38, 5, 6, pal.Accent);
                    break;
            }
        }

        static void PaintEnemy(Pix p, Pal pal, CharacterDef def)
        {
            p.Oval(24, 6, 11, 3, pal.Shadow);
            p.Box(16, 8, 7, 9, pal.ClothDark);
            p.Box(25, 8, 7, 9, pal.ClothDark);
            p.Box(14, 16, 20, 10, pal.Cloth);
            p.Box(16, 24, 16, 10, pal.ClothDark);
            p.Box(11, 20, 5, 10, pal.Skin);
            p.Box(32, 20, 5, 10, pal.Skin);
            p.Oval(24, 40, 8, 7, pal.Skin);
            p.Box(16, 42, 16, 8, pal.Hair);
            if (def.Role == Role.Defender)
                p.Box(6, 16, 10, 16, pal.Metal);
            else if (def.Role == Role.Attacker)
                p.Box(36, 16, 4, 22, pal.Metal);
            else
                p.Oval(38, 32, 5, 5, pal.Accent);
        }

        static void PaintBoss(Pix p, Pal pal)
        {
            p.Oval(24, 5, 14, 4, pal.Shadow);
            p.Box(12, 8, 10, 12, pal.ClothDark);
            p.Box(26, 8, 10, 12, pal.ClothDark);
            p.Box(10, 18, 28, 12, pal.Cloth);
            p.Box(14, 28, 20, 10, pal.ClothDark);
            p.Oval(24, 42, 10, 9, pal.Skin);
            p.Box(14, 46, 20, 8, pal.Hair);
            p.Box(10, 50, 6, 12, pal.Hair);
            p.Box(32, 50, 6, 12, pal.Hair);
            p.Oval(24, 30, 5, 5, pal.Accent);
            p.Box(6, 20, 8, 16, pal.Metal);
            PaintHe(p, 21, 28, pal.Accent);
        }

        // 核 — SNES boss mark. Never English "BOSS".
        static void PaintHe(Pix p, int x, int y, Color32 c)
        {
            p.Box(x + 1, y + 6, 5, 1, c);
            p.Set(x, y + 5, c);
            p.Set(x + 2, y + 5, c);
            p.Set(x + 4, y + 5, c);
            p.Set(x + 6, y + 5, c);
            p.Box(x, y + 4, 7, 1, c);
            p.Box(x + 1, y + 3, 1, 3, c);
            p.Box(x + 3, y + 2, 1, 3, c);
            p.Box(x + 5, y + 2, 1, 2, c);
            p.Set(x + 2, y + 2, c);
            p.Set(x + 4, y + 1, c);
            p.Set(x + 6, y + 1, c);
            p.Set(x, y + 1, c);
            p.Set(x + 2, y, c);
            p.Set(x + 4, y, c);
        }

        static void WalkPose(Pix p)
        {
            var src = (Color32[])p.D.Clone();
            for (int y = 0; y < 22; y++)
                for (int x = 0; x < W; x++)
                    p.Set(x, y, new Color32(0, 0, 0, 0));
            for (int y = 0; y < 22; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var c = src[y * W + x];
                    if (c.a < 8) continue;
                    var dx = x < 24 ? -1 : 1;
                    var dy = y < 11 ? 1 : 0;
                    p.Set(x + dx, y + dy, c);
                }
            }
        }

        static void PaintFace(Pix p, Pal pal, string id, bool enemy, bool boss)
        {
            var eyeY = enemy ? 41 : 43;
            var white = new Color32(255, 255, 255, 255);
            p.Box(20, eyeY, 3, 3, white);
            p.Box(26, eyeY, 3, 3, white);
            p.Box(21, eyeY, 2, 2, pal.Ink);
            p.Box(27, eyeY, 2, 2, pal.Ink);
            if (id == "C010" || boss)
            {
                p.Box(18, eyeY - 1, 12, 2, pal.Hair);
                p.Box(21, eyeY, 2, 2, pal.Accent);
                p.Box(26, eyeY, 2, 2, pal.Accent);
            }
            p.Box(22, eyeY - 4, 5, 2, pal.Ink);
        }

        struct Pal
        {
            public Color32 Skin, Hair, Cloth, ClothDark, ClothLite, Metal, Accent, Ink, Shadow;

            public static Pal From(CharacterDef def, string skinId)
            {
                var cloth = CharacterPresenter.SkinTint(def.Element, skinId);
                var pal = new Pal
                {
                    Skin = SkinOf(def.Id),
                    Hair = HairOf(def.Id),
                    Cloth = C(cloth),
                    ClothDark = C(Color.Lerp(cloth, Color.black, 0.45f)),
                    ClothLite = C(Color.Lerp(cloth, Color.white, 0.22f)),
                    Metal = new Color32(220, 210, 190, 255),
                    Accent = C(VisualTokens.Element(def.Element)),
                    Ink = new Color32(18, 10, 8, 255),
                    Shadow = new Color32(0, 0, 0, 140)
                };
                return pal;
            }

            static Color32 SkinOf(string id)
            {
                switch (id)
                {
                    case "C002": return new Color32(199, 148, 107, 255);
                    case "C004": return new Color32(237, 209, 194, 255);
                    case "C006": return new Color32(184, 138, 102, 255);
                    case "C009": return new Color32(230, 199, 184, 255);
                    case "C012": return new Color32(240, 230, 224, 255);
                    case "C014": return new Color32(230, 186, 158, 255);
                    case "C016": return new Color32(199, 168, 148, 255);
                    case "C019": return new Color32(186, 158, 122, 255);
                    case "C022": return new Color32(245, 224, 204, 255);
                    case "C024": return new Color32(168, 138, 148, 255);
                    case "EBOSS": return new Color32(158, 122, 133, 255);
                    default: return new Color32(245, 209, 178, 255);
                }
            }

            static Color32 HairOf(string id)
            {
                switch (id)
                {
                    case "C001": return new Color32(46, 20, 15, 255);
                    case "C002": return new Color32(107, 92, 82, 255);
                    case "C003": return new Color32(20, 56, 82, 255);
                    case "C004": return new Color32(26, 36, 71, 255);
                    case "C005": return new Color32(56, 97, 36, 255);
                    case "C006": return new Color32(41, 56, 26, 255);
                    case "C007": return new Color32(235, 224, 184, 255);
                    case "C008": return new Color32(250, 230, 158, 255);
                    case "C009": return new Color32(56, 31, 71, 255);
                    case "C010": return new Color32(20, 15, 26, 255);
                    case "C011": return new Color32(140, 31, 20, 255);
                    case "C012": return new Color32(184, 219, 235, 255);
                    case "C013": return new Color32(122, 46, 28, 255);
                    case "C014": return new Color32(92, 56, 48, 255);
                    case "C015": return new Color32(28, 82, 107, 255);
                    case "C016": return new Color32(48, 71, 92, 255);
                    case "C017": return new Color32(56, 71, 36, 255);
                    case "C018": return new Color32(36, 82, 41, 255);
                    case "C019": return new Color32(71, 107, 56, 255);
                    case "C020": return new Color32(219, 199, 122, 255);
                    case "C021": return new Color32(235, 214, 158, 255);
                    case "C022": return new Color32(245, 232, 194, 255);
                    case "C023": return new Color32(28, 15, 20, 255);
                    case "C024": return new Color32(36, 28, 41, 255);
                    case "C025": return new Color32(71, 36, 92, 255);
                    case "EBOSS": return new Color32(31, 15, 36, 255);
                    default: return new Color32(41, 31, 31, 255);
                }
            }

            static Color32 C(Color c)
            {
                return new Color32(
                    (byte)Mathf.RoundToInt(c.r * 255f),
                    (byte)Mathf.RoundToInt(c.g * 255f),
                    (byte)Mathf.RoundToInt(c.b * 255f),
                    255);
            }
        }

        sealed class Pix
        {
            public readonly Color32[] D = new Color32[W * H];

            public void Set(int x, int y, Color32 c)
            {
                if ((uint)x >= W || (uint)y >= H) return;
                D[y * W + x] = c;
            }

            public Color32 Get(int x, int y)
            {
                if ((uint)x >= W || (uint)y >= H) return new Color32(0, 0, 0, 0);
                return D[y * W + x];
            }

            public void Box(int x, int y, int w, int h, Color32 c)
            {
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                        Set(x + i, y + j, c);
            }

            public void Oval(int cx, int cy, int rx, int ry, Color32 c)
            {
                var rx2 = rx * rx;
                var ry2 = ry * ry;
                for (int j = -ry; j <= ry; j++)
                    for (int i = -rx; i <= rx; i++)
                        if (i * i * ry2 + j * j * rx2 <= rx2 * ry2)
                            Set(cx + i, cy + j, c);
            }

            public void Outline(Color32 ink)
            {
                var copy = (Color32[])D.Clone();
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        if (copy[y * W + x].a < 16) continue;
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                if (i == 0 && j == 0) continue;
                                var nx = x + i;
                                var ny = y + j;
                                if ((uint)nx >= W || (uint)ny >= H) continue;
                                if (copy[ny * W + nx].a < 16)
                                    Set(nx, ny, ink);
                            }
                        }
                    }
                }
            }
        }
    }
}
