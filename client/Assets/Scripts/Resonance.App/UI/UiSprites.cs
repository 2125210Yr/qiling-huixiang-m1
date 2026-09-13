using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public static class UiSprites
    {
        static Sprite _pill;
        static Sprite _round;
        static Sprite _hex;
        static Sprite _circle;
        static Sprite _soft;
        static Sprite _slash;
        static Sprite _spark;
        static Sprite _pixel;
        static Sprite _plus;
        static Sprite _iceShard;
        static Sprite _iceCrystal;
        static Sprite _iceFlake;
        static Sprite _iceSpark;
        static Sprite _iceBokeh;
        static Sprite _mosaic;
        static Sprite _dashed;
        static Sprite _halftone;
        static Sprite _gear;
        static Sprite _hexRing;
        static Sprite _star;
        static Sprite _heart;
        static Sprite _navMark;
        static Sprite _eye;
        static Sprite _eyeSlash;
        static Sprite _confirmPill;
        static Sprite _cancelPill;
        static Sprite _inspectHanger;
        static Sprite _inspectBars;
        static Sprite _inspectSword;
        static Sprite _inspectShield;
        static Sprite _inspectRing;
        static Sprite _inspectCards;
        static Sprite _inspectDiag;

        public static Sprite Pill()
        {
            if (_pill != null) return _pill;
            _pill = RoundRect(128, 48, 24, new Vector4(24, 24, 24, 24));
            return _pill;
        }

        public static Sprite Round()
        {
            if (_round != null) return _round;
            _round = RoundRect(64, 64, 10, new Vector4(12, 12, 12, 12));
            return _round;
        }

        public static Sprite Hex()
        {
            if (_hex != null) return _hex;
            _hex = MakeHex(64);
            return _hex;
        }

        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            _circle = MakeCircle(64);
            return _circle;
        }

        public static Sprite Soft()
        {
            if (_soft != null) return _soft;
            _soft = MakeRadial(96);
            return _soft;
        }

        public static Sprite Slash()
        {
            if (_slash != null) return _slash;
            _slash = MakeSlash(160, 48);
            return _slash;
        }

        public static Sprite Spark()
        {
            if (_spark != null) return _spark;
            _spark = MakeSpark(32);
            return _spark;
        }

        public static Sprite Pixel()
        {
            if (_pixel != null) return _pixel;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var fill = new Color32(255, 255, 255, 255);
            var px = new Color32[64];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _pixel = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            return _pixel;
        }

        public static Sprite Plus()
        {
            if (_plus != null) return _plus;
            var s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var on = (x >= 6 && x <= 9) || (y >= 6 && y <= 9);
                    px[y * s + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _plus = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            return _plus;
        }

        public static Sprite IceShard()
        {
            if (_iceShard != null) return _iceShard;
            _iceShard = MakeIceShard(48, 96);
            return _iceShard;
        }

        public static Sprite IceCrystal()
        {
            if (_iceCrystal != null) return _iceCrystal;
            _iceCrystal = MakeHexGem(64);
            return _iceCrystal;
        }

        public static Sprite IceFlake()
        {
            if (_iceFlake != null) return _iceFlake;
            _iceFlake = MakeSnowflake(64);
            return _iceFlake;
        }

        public static Sprite IceSpark()
        {
            if (_iceSpark != null) return _iceSpark;
            _iceSpark = MakeSpark(48);
            return _iceSpark;
        }

        public static Sprite IceBokeh()
        {
            if (_iceBokeh != null) return _iceBokeh;
            _iceBokeh = MakeIceBokeh(128);
            return _iceBokeh;
        }

        public static Sprite FloorMosaic()
        {
            if (_mosaic != null) return _mosaic;
            _mosaic = MakeFloorMosaic(360, 640, 48f);
            return _mosaic;
        }

        public static Sprite MosaicDiamond() => FloorMosaic();

        public static Sprite Dashed()
        {
            if (_dashed != null) return _dashed;
            const int w = 64;
            const int h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[w * h];
            var on = WithAlpha(Color.white, 220);
            var off = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var dash = (x % 14) < 8 && y >= 3 && y <= 4;
                    px[y * w + x] = dash ? on : off;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _dashed = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _dashed;
        }

        /// <summary>平铺半调网点。印刷风底，不用 Pixel 灰块。</summary>
        public static Sprite Halftone()
        {
            if (_halftone != null) return _halftone;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[s * s];
            const float cell = 8f;
            for (int y = 0; y < s; y++)
            {
                var ox = ((int)(y / cell) & 1) * cell * 0.5f;
                for (int x = 0; x < s; x++)
                {
                    var lx = Mathf.Repeat(x - ox + cell * 0.5f, cell) - cell * 0.5f;
                    var ly = Mathf.Repeat(y + cell * 0.5f, cell) - cell * 0.5f;
                    var d = Mathf.Sqrt(lx * lx + ly * ly);
                    var a = Mathf.Clamp01(2.1f - d);
                    a *= a;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 180f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _halftone = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _halftone;
        }

        public static Sprite Gear()
        {
            if (_gear != null) return _gear;
            _gear = MakeGear(64);
            return _gear;
        }

        public static Sprite HexRing()
        {
            if (_hexRing != null) return _hexRing;
            _hexRing = MakeHexRing(64);
            return _hexRing;
        }

        public static Sprite Star()
        {
            if (_star != null) return _star;
            _star = MakeStar(32);
            return _star;
        }

        /// <summary>Top-bar crest / tip heart mark (primary GT heart-in-gear role).</summary>
        public static Sprite Heart()
        {
            if (_heart != null) return _heart;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            StampHeart(px, s, s, s / 2, s / 2 + 1, 11f, new Color32(255, 255, 255, 255));
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _heart = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _heart;
        }

        public static Sprite NavMark()
        {
            if (_navMark != null) return _navMark;
            _navMark = MakeNavMark(48, 16);
            return _navMark;
        }

        public static Sprite Eye()
        {
            if (_eye != null) return _eye;
            _eye = MakeEye(64, false);
            return _eye;
        }

        public static Sprite EyeSlash()
        {
            if (_eyeSlash != null) return _eyeSlash;
            _eyeSlash = MakeEye(64, true);
            return _eyeSlash;
        }

        public static Sprite InspectHanger()
        {
            if (_inspectHanger != null) return _inspectHanger;
            _inspectHanger = BakeInspect(64, PaintHanger);
            return _inspectHanger;
        }

        public static Sprite InspectBars()
        {
            if (_inspectBars != null) return _inspectBars;
            _inspectBars = BakeInspect(64, PaintBars);
            return _inspectBars;
        }

        public static Sprite InspectSword()
        {
            if (_inspectSword != null) return _inspectSword;
            _inspectSword = BakeInspect(64, PaintSword);
            return _inspectSword;
        }

        public static Sprite InspectShield()
        {
            if (_inspectShield != null) return _inspectShield;
            _inspectShield = BakeInspect(64, PaintShield);
            return _inspectShield;
        }

        public static Sprite InspectRing()
        {
            if (_inspectRing != null) return _inspectRing;
            _inspectRing = BakeInspect(64, PaintRing);
            return _inspectRing;
        }

        public static Sprite InspectCards()
        {
            if (_inspectCards != null) return _inspectCards;
            _inspectCards = BakeInspect(64, PaintCards);
            return _inspectCards;
        }

        public static Sprite InspectDiag()
        {
            if (_inspectDiag != null) return _inspectDiag;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var on = ((x + y) % 8) < 2;
                    px[y * s + x] = on ? new Color32(255, 255, 255, 220) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _inspectDiag = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _inspectDiag;
        }

        public static Sprite ConfirmPill()
        {
            if (_confirmPill != null) return _confirmPill;
            _confirmPill = StadiumPill(320, 84, VisualTokens.YellowConfirmTop, VisualTokens.YellowConfirmBottom);
            return _confirmPill;
        }

        public static Sprite CancelPill()
        {
            if (_cancelPill != null) return _cancelPill;
            _cancelPill = StadiumPill(320, 84, VisualTokens.OrangeCancel, VisualTokens.OrangeCancelBottom);
            return _cancelPill;
        }

        static Sprite _wire;

        /// <summary>细金线圆角框。印在面板内侧，不靠 Outline 组件。</summary>
        public static Sprite WireFrame()
        {
            if (_wire != null) return _wire;
            const int s = 64;
            const float rad = 12f;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var sd = RrSd(x + 0.5f, y + 0.5f, s, s, rad);
                    var a = Mathf.Clamp01(1.5f - Mathf.Abs(sd + 2.2f));
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _wire = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            return _wire;
        }

        static float RrSd(float x, float y, float w, float h, float r)
        {
            var qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
            var qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            var ax = Mathf.Max(qx, 0f);
            var ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        public static void Apply(Image img, Sprite sprite)
        {
            if (img == null) return;
            if (sprite == null) sprite = Pixel();
            if (sprite == null) return;
            img.sprite = sprite;
            img.type = sprite.border.sqrMagnitude > 0.1f ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = false;
        }

        static Sprite RoundRect(int w, int h, int radius, Vector4 border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var a = CornerAlpha(x, y, w, h, radius);
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        static float CornerAlpha(int x, int y, int w, int h, int r)
        {
            float cx = x + 0.5f;
            float cy = y + 0.5f;
            var px = cx < r ? r - cx : (cx > w - r ? cx - (w - r) : 0f);
            var py = cy < r ? r - cy : (cy > h - r ? cy - (h - r) : 0f);
            if (px == 0f && py == 0f) return 1f;
            var d = Mathf.Sqrt(px * px + py * py);
            return Mathf.Clamp01(r + 0.5f - d);
        }

        static Sprite _floor;
        static Sprite _floorIce;
        static Sprite[] _stamps;

        public static Sprite FloorChecker()
        {
            if (_floor != null) return _floor;
            const int w = 270;
            const int h = 480;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var dark = VisualTokens.FloorDark;
            var light = VisualTokens.FloorLight;
            for (int y = 0; y < h; y++)
            {
                var v = h <= 1 ? 0f : y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    var u = w <= 1 ? 0f : x / (float)(w - 1);
                    const float horizon = 0.64f;
                    Color col;
                    if (v > horizon)
                    {
                        var t = Mathf.InverseLerp(horizon, 1f, v);
                        col = Color.Lerp(VisualTokens.FloorDark, VisualTokens.BgVoid, t);
                    }
                    else
                    {
                        var depth = (horizon - v) / horizon;
                        var persp = 0.10f + depth * 1.85f;
                        var wx = (u - 0.5f) / persp * 7f;
                        var wz = (1f - depth) * 11f;
                        var a = ((Mathf.FloorToInt(wx) + Mathf.FloorToInt(wz)) & 1) == 0;
                        col = a ? dark : light;
                        var vig = Mathf.Clamp01(1.15f - Mathf.Abs(u - 0.5f) * 1.1f - (1f - depth) * 0.45f);
                        col *= vig;
                    }
                    var glow = Mathf.Exp(-((u - 0.5f) * (u - 0.5f) * 10f + (v - 0.30f) * (v - 0.30f) * 14f)) * 0.42f;
                    var wash = VisualTokens.UnderGlow;
                    col.r = Mathf.Min(1f, col.r + glow * wash.r);
                    col.g = Mathf.Min(1f, col.g + glow * wash.g);
                    col.b = Mathf.Min(1f, col.b + glow * wash.b);
                    pixels[y * w + x] = col;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            _floor = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _floor;
        }

        public static Sprite FloorCheckerIce()
        {
            if (_floorIce != null) return _floorIce;
            const int w = 270;
            const int h = 480;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var dark = VisualTokens.IceDeep;
            var light = Color.Lerp(VisualTokens.IceDeep, VisualTokens.IceShard, 0.45f);
            var voidCol = Color.Lerp(VisualTokens.BgVoid, VisualTokens.IceDeep, 0.35f);
            var auroraCol = VisualTokens.Aurora;
            var glowCol = VisualTokens.IceCore;
            for (int y = 0; y < h; y++)
            {
                var v = h <= 1 ? 0f : y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    var u = w <= 1 ? 0f : x / (float)(w - 1);
                    const float horizon = 0.64f;
                    Color col;
                    if (v > horizon)
                    {
                        var t = Mathf.InverseLerp(horizon, 1f, v);
                        col = Color.Lerp(VisualTokens.IceDeep, voidCol, t);
                        var aurora = Mathf.Exp(-((u - 0.62f) * (u - 0.62f) * 8f + (v - 0.86f) * (v - 0.86f) * 22f)) * 0.28f;
                        col.r = Mathf.Min(1f, col.r + aurora * auroraCol.r);
                        col.g = Mathf.Min(1f, col.g + aurora * auroraCol.g);
                        col.b = Mathf.Min(1f, col.b + aurora * auroraCol.b);
                    }
                    else
                    {
                        var depth = (horizon - v) / horizon;
                        var persp = 0.10f + depth * 1.85f;
                        var wx = (u - 0.5f) / persp * 7f;
                        var wz = (1f - depth) * 11f;
                        var a = ((Mathf.FloorToInt(wx) + Mathf.FloorToInt(wz)) & 1) == 0;
                        col = a ? dark : light;
                        var vig = Mathf.Clamp01(1.15f - Mathf.Abs(u - 0.5f) * 1.1f - (1f - depth) * 0.45f);
                        col *= vig;
                    }
                    var glow = Mathf.Exp(-((u - 0.5f) * (u - 0.5f) * 10f + (v - 0.30f) * (v - 0.30f) * 14f)) * 0.38f;
                    col.r = Mathf.Min(1f, col.r + glow * glowCol.r);
                    col.g = Mathf.Min(1f, col.g + glow * glowCol.g);
                    col.b = Mathf.Min(1f, col.b + glow * glowCol.b);
                    pixels[y * w + x] = col;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            _floorIce = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _floorIce;
        }

        /// <summary>
        /// 六页签图标：原创几何记号（门 / 焰 / 剑 / 书 / 章 / 径）。不描原作剪影。
        /// </summary>
        public static Sprite Stamp(int kind)
        {
            if (_stamps == null) _stamps = new Sprite[6];
            kind = Mathf.Clamp(kind, 0, 5);
            if (_stamps[kind] != null) return _stamps[kind];
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float a;
                    switch (kind)
                    {
                        case 0: a = TabGate(x, y); break;
                        case 1: a = TabFlame(x, y); break;
                        case 2: a = TabSword(x, y); break;
                        case 3: a = TabBook(x, y); break;
                        case 4: a = TabCrest(x, y); break;
                        default: a = TabPath(x, y); break;
                    }
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _stamps[kind] = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _stamps[kind];
        }

        static float SegA(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            var vx = bx - ax;
            var vy = by - ay;
            var c2 = vx * vx + vy * vy;
            var t = c2 < 0.0001f ? 0f : Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / c2);
            var dx = x - (ax + t * vx);
            var dy = y - (ay + t * vy);
            return Mathf.Clamp01(r + 1.1f - Mathf.Sqrt(dx * dx + dy * dy));
        }

        static float DiscA(float x, float y, float cx, float cy, float r)
        {
            var dx = x - cx;
            var dy = y - cy;
            return Mathf.Clamp01(r + 1.1f - Mathf.Sqrt(dx * dx + dy * dy));
        }

        static float PolyA(float x, float y, params float[] p)
        {
            var n = p.Length / 2;
            var inside = false;
            var best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var ax = p[i * 2];
                var ay = p[i * 2 + 1];
                var bx = p[((i + 1) % n) * 2];
                var by = p[((i + 1) % n) * 2 + 1];
                var vx = bx - ax;
                var vy = by - ay;
                var c2 = vx * vx + vy * vy;
                var t = c2 < 0.0001f ? 0f : Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / c2);
                var dx = x - (ax + t * vx);
                var dy = y - (ay + t * vy);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < best) best = d;
                if ((ay > y) != (by > y) && x < ax + (y - ay) / (by - ay) * (bx - ax))
                    inside = !inside;
            }
            var sd = inside ? best : -best;
            return Mathf.Clamp01(0.5f + sd / 1.3f);
        }

        static float TabGate(float x, float y)
        {
            var a = SegA(x, y, 14, 45, 50, 45, 3.0f);
            a = Mathf.Max(a, SegA(x, y, 14, 45, 10, 51, 2.4f));
            a = Mathf.Max(a, SegA(x, y, 50, 45, 54, 51, 2.4f));
            a = Mathf.Max(a, SegA(x, y, 20, 37, 44, 37, 2.0f));
            a = Mathf.Max(a, SegA(x, y, 21, 12, 21, 45, 2.8f));
            a = Mathf.Max(a, SegA(x, y, 43, 12, 43, 45, 2.8f));
            a = Mathf.Max(a, SegA(x, y, 14, 7, 50, 7, 2.0f));
            return a;
        }

        static float TabFlame(float x, float y)
        {
            var outer = Mathf.Max(DiscA(x, y, 32, 26, 14.5f), PolyA(x, y, 32, 57, 20, 31, 44, 31));
            var inner = Mathf.Max(DiscA(x, y, 32, 24, 7.6f), PolyA(x, y, 32, 45, 25.5f, 29, 38.5f, 29));
            return Mathf.Min(outer, 1f - inner);
        }

        static float TabSword(float x, float y)
        {
            var a = PolyA(x, y, 28.5f, 18, 35.5f, 18, 35.5f, 47, 32, 58, 28.5f, 47);
            a = Mathf.Max(a, SegA(x, y, 20, 14, 44, 14, 2.8f));
            a = Mathf.Max(a, SegA(x, y, 32, 5, 32, 13, 2.4f));
            a = Mathf.Max(a, DiscA(x, y, 32, 5, 2.8f));
            return a;
        }

        static float TabBook(float x, float y)
        {
            var pages = Mathf.Max(
                PolyA(x, y, 11, 15, 31, 13, 31, 45, 11, 47),
                PolyA(x, y, 33, 13, 53, 15, 53, 47, 33, 45));
            var cut = SegA(x, y, 15, 36, 27, 35, 1.1f);
            cut = Mathf.Max(cut, SegA(x, y, 15, 27, 27, 26, 1.1f));
            cut = Mathf.Max(cut, SegA(x, y, 37, 35, 49, 36, 1.1f));
            cut = Mathf.Max(cut, SegA(x, y, 37, 26, 49, 27, 1.1f));
            return Mathf.Min(pages, 1f - cut);
        }

        static float TabCrest(float x, float y)
        {
            var a = SegA(x, y, 17, 49, 47, 49, 2.6f);
            a = Mathf.Max(a, SegA(x, y, 47, 49, 47, 31, 2.6f));
            a = Mathf.Max(a, SegA(x, y, 47, 31, 32, 12, 2.6f));
            a = Mathf.Max(a, SegA(x, y, 32, 12, 17, 31, 2.6f));
            a = Mathf.Max(a, SegA(x, y, 17, 31, 17, 49, 2.6f));
            a = Mathf.Max(a, SegA(x, y, 23, 38, 41, 24, 3.0f));
            return a;
        }

        static float TabPath(float x, float y)
        {
            var a = SegA(x, y, 17, 7, 28, 53, 2.4f);
            a = Mathf.Max(a, SegA(x, y, 47, 7, 36, 53, 2.4f));
            a = Mathf.Max(a, DiscA(x, y, 32, 13, 2.6f));
            a = Mathf.Max(a, DiscA(x, y, 32, 26, 2.4f));
            a = Mathf.Max(a, DiscA(x, y, 32, 39, 2.2f));
            a = Mathf.Max(a, DiscA(x, y, 32, 52, 2.6f));
            return a;
        }

        static float HexDist(int x, int y, int s)
        {
            var cx = (s - 1) * 0.5f;
            var cy = (s - 1) * 0.5f;
            var nx = (x - cx) / (s * 0.46f);
            var ny = (y - cy) / (s * 0.46f);
            return Mathf.Abs(ny) + Mathf.Abs(nx) * 0.577f;
        }

        static Sprite MakeHex(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var d = HexDist(x, y, s);
                    var a = Mathf.Clamp01((1.02f - d) * 10f);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeHexRing(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var d = HexDist(x, y, s);
                    var a = Mathf.Clamp01((1.04f - d) * 14f) * Mathf.Clamp01((d - 0.86f) * 16f);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeStar(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var cx = (s - 1) * 0.5f;
            var cy = (s - 1) * 0.5f;
            var r = s * 0.46f;
            const float inner = 0.40f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r + 0.8f)
                    {
                        pixels[y * s + x] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    var ang = Mathf.Atan2(dx, dy);
                    if (ang < 0f) ang += Mathf.PI * 2f;
                    var slice = (Mathf.PI * 2f) / 5f;
                    var t = (ang % slice) / slice;
                    var edge = t < 0.5f
                        ? Mathf.Lerp(1f, inner, t * 2f)
                        : Mathf.Lerp(inner, 1f, (t - 0.5f) * 2f);
                    var a = Mathf.Clamp01(r * edge + 0.55f - dist);
                    a *= a;
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeNavMark(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var cy = (h - 1) * 0.42f;
            float[] dots = { 0.18f, 0.50f, 0.82f };
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var a = 0f;
                    for (int i = 0; i < dots.Length; i++)
                    {
                        var px = dots[i] * (w - 1);
                        var py = cy + Mathf.Abs(dots[i] - 0.5f) * h * 0.35f;
                        var d = Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
                        a = Mathf.Max(a, Mathf.Clamp01(2.4f - d));
                    }
                    a *= a;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCircle(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            var r = s * 0.48f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    var a = Mathf.Clamp01(r + 0.6f - d);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSlash(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var cx = (w - 1) * 0.5f;
            var cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var nx = (x - cx) / (w * 0.48f);
                    var ny = (y - cy) / (h * 0.38f);
                    var d = Mathf.Sqrt(nx * nx + ny * ny * 3.4f);
                    var tip = Mathf.Clamp01(1.15f - Mathf.Abs(nx));
                    var a = Mathf.Clamp01(1.05f - d) * tip;
                    a *= a;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeIceShard(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var cx = (w - 1) * 0.5f;
            var cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var nx = (x - cx) / (w * 0.42f);
                    var ny = (y - cy) / (h * 0.48f);
                    var body = 1f - (Mathf.Abs(nx) + Mathf.Abs(ny) * 0.72f);
                    var facet = 1f - Mathf.Abs(nx) * 1.8f - Mathf.Abs(ny + 0.15f) * 0.5f;
                    var core = 1f - Mathf.Abs(nx) * 6.5f - Mathf.Abs(ny) * 0.35f;
                    var a = Mathf.Max(0f, body);
                    a = Mathf.Max(a * 0.75f, Mathf.Max(0f, facet) * 0.9f);
                    a = Mathf.Max(a, Mathf.Max(0f, core));
                    a = Mathf.Clamp01(a);
                    a *= a;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeHexGem(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var cx = (s - 1) * 0.5f;
            var cy = (s - 1) * 0.5f;
            var r = s * 0.46f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var ang = Mathf.Atan2(dy, dx);
                    var fold = Mathf.Cos(ang - (Mathf.PI / 3f) * Mathf.Floor((ang + Mathf.PI) / (Mathf.PI / 3f)));
                    var hex = Mathf.Cos(Mathf.PI / 6f) / Mathf.Max(0.001f, fold);
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / (r * Mathf.Abs(hex) + 0.001f);
                    var edge = Mathf.Clamp01(1.12f - d);
                    var inner = Mathf.Clamp01(1.05f - d * 1.55f);
                    var glint = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(cx - r * 0.18f, cy - r * 0.22f)) / (r * 0.28f));
                    var a = Mathf.Max(edge * 0.55f, inner * 0.9f);
                    a = Mathf.Max(a, glint * 0.95f);
                    a = Mathf.Clamp01(a);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSnowflake(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = (x - c) / c;
                    var dy = (y - c) / c;
                    var ang = Mathf.Atan2(dy, dx);
                    var rad = Mathf.Sqrt(dx * dx + dy * dy);
                    var fold = Mathf.Abs(Mathf.Cos(ang * 3f));
                    var arm = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Cos(ang * 6f)) * 8f) * Mathf.Clamp01(1.05f - rad * 1.15f);
                    var branch = fold * Mathf.Clamp01(1f - Mathf.Abs(rad - 0.55f) * 7f) * 0.85f;
                    var hub = Mathf.Clamp01(1f - rad * 4.2f);
                    var a = Mathf.Max(arm, Mathf.Max(branch, hub));
                    a *= Mathf.Clamp01(1.08f - rad);
                    a = Mathf.Clamp01(a);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeIceBokeh(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var cx = (s - 1) * 0.5f;
            var cy = (s - 1) * 0.5f;
            var r = s * 0.46f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    var gauss = Mathf.Exp(-d * d * 2.4f);
                    var ang = Mathf.Atan2(dy, dx);
                    var hex = Mathf.Cos(Mathf.PI / 6f) / Mathf.Max(0.001f,
                        Mathf.Cos(ang - (Mathf.PI / 3f) * Mathf.Floor((ang + Mathf.PI) / (Mathf.PI / 3f))));
                    var hd = Mathf.Sqrt(dx * dx + dy * dy) / (r * Mathf.Abs(hex) + 0.001f);
                    var facet = Mathf.Clamp01(1.05f - hd) * 0.35f;
                    var a = Mathf.Clamp01(gauss * 0.90f + facet * gauss);
                    a *= a;
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSpark(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = Mathf.Abs(x - c) / c;
                    var dy = Mathf.Abs(y - c) / c;
                    var cross = Mathf.Min(dx, dy);
                    var arm = 1f - Mathf.Min(1f, Mathf.Min(dx, dy) * 6f + Mathf.Max(dx, dy) * 0.15f);
                    var diag = 1f - Mathf.Min(1f, Mathf.Abs(dx - dy) * 4.5f + (dx + dy) * 0.35f);
                    var a = Mathf.Clamp01(Mathf.Max(arm, diag * 0.85f) - cross * 0.2f);
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRadial(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var u = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (s * 0.5f);
                    var a = Mathf.Clamp01(1f - u);
                    a *= a;
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 体育场胶囊：柠檬渐变 + 烘焙厚深描边 + 顶部高光。描边印在 sprite 里，
        /// 不再依赖 Outline 组件的弱描边。
        /// </summary>
        static Sprite StadiumPill(int w, int h, Color top, Color bot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            var r = h * 0.5f;
            var ink = VisualTokens.TextOnYellow;
            const float stroke = 7f;
            for (int y = 0; y < h; y++)
            {
                var t = h <= 1 ? 0f : y / (float)(h - 1);
                var rgb = Color.Lerp(bot, top, t);
                var hi = Mathf.Clamp01((t - 0.58f) * 1.8f) * 0.30f;
                rgb = Color.Lerp(rgb, Color.white, hi * 0.35f);
                if (t < 0.22f) rgb *= 0.94f;
                var r8 = (byte)Mathf.RoundToInt(rgb.r * 255f);
                var g8 = (byte)Mathf.RoundToInt(rgb.g * 255f);
                var b8 = (byte)Mathf.RoundToInt(rgb.b * 255f);
                for (int x = 0; x < w; x++)
                {
                    var cx = Mathf.Clamp(x + 0.5f, r, w - r);
                    var dx = x + 0.5f - cx;
                    var dy = y + 0.5f - h * 0.5f;
                    var sd = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    var a = Mathf.Clamp01(0.5f - sd / 1.25f);
                    Color c = rgb;
                    if (sd > -stroke)
                    {
                        // 描边贴外缘：内侧 2px 过渡回填充色，外缘保持实心深边。
                        var rim = Mathf.Clamp01((sd + stroke) / 2f);
                        c = Color.Lerp(rgb, ink, rim);
                    }
                    pixels[y * w + x] = sd > -stroke
                        ? new Color32(
                            (byte)Mathf.RoundToInt(c.r * 255f),
                            (byte)Mathf.RoundToInt(c.g * 255f),
                            (byte)Mathf.RoundToInt(c.b * 255f),
                            (byte)Mathf.RoundToInt(a * 255f))
                        : new Color32(r8, g8, b8, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            var br = Mathf.RoundToInt(r);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(br, br, br, br));
        }

        static Sprite MakeGear(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            const int teeth = 6;
            var rHole = s * 0.12f;
            var rRim = s * 0.33f;
            var rTooth = s * 0.46f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - c;
                    var dy = y - c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var ang = Mathf.Atan2(dy, dx);
                    var tooth = Mathf.Cos(ang * teeth);
                    var blend = Mathf.SmoothStep(0.28f, 0.62f, tooth);
                    var rMax = Mathf.Lerp(rRim, rTooth, blend);
                    var body = r <= rMax && r >= rHole;
                    var a = 0f;
                    if (body)
                    {
                        var outer = Mathf.Clamp01((rMax + 0.55f - r) * 2.2f);
                        var inner = Mathf.Clamp01((r - rHole + 0.45f) * 2.4f);
                        a = Mathf.Min(outer, inner);
                    }
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeEye(int s, bool slash)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            var rx = s * 0.42f;
            var ry = s * 0.22f;
            var pr = s * 0.12f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var nx = (x - c) / rx;
                    var ny = (y - c) / ry;
                    var almond = nx * nx + ny * ny;
                    var lid = Mathf.Clamp01(1.08f - almond);
                    var ring = Mathf.Clamp01(1.02f - Mathf.Abs(1f - almond) * 7f);
                    var pupil = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / pr);
                    var a = Mathf.Max(ring, pupil * 0.95f) * lid;
                    if (slash)
                    {
                        var d = Mathf.Abs((x - c) * 0.72f + (y - c) * 0.72f);
                        var band = Mathf.Clamp01(1f - d / 3.2f) * Mathf.Clamp01(1.15f - Mathf.Abs(x - c) / (s * 0.42f));
                        a = Mathf.Max(a * 0.85f, band);
                    }
                    pixels[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeFloorMosaic(int w, int h, float cell)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            var dark = VisualTokens.BgVoid;
            var light = VisualTokens.BgMosaic;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var u = (x + y) / cell;
                    var v = (x - y) / cell;
                    var iu = Mathf.FloorToInt(u);
                    var iv = Mathf.FloorToInt(v);
                    var fu = u - iu;
                    var fv = v - iv;
                    var col = ((iu + iv) & 1) == 0 ? dark : light;
                    var edge = Mathf.Min(Mathf.Min(fu, fv), Mathf.Min(1f - fu, 1f - fv));
                    var bevel = (fv - fu) * 0.07f;
                    col.r = Mathf.Clamp01(col.r + bevel);
                    col.g = Mathf.Clamp01(col.g + bevel);
                    col.b = Mathf.Clamp01(col.b + bevel);
                    var line = Mathf.Clamp01(1f - edge * cell / 1.55f);
                    col = Color.Lerp(col, col * 0.52f, line * 0.5f);
                    px[y * w + x] = col;
                }
            }
            var gold = WithAlpha(VisualTokens.GoldMetal, 48);
            var ink = WithAlpha(VisualTokens.TextPrimary, 28);
            var dim = WithAlpha(VisualTokens.BgVoid, 36);
            int span = Mathf.CeilToInt((w + h) / cell) + 4;
            for (int iu = -span; iu <= span; iu++)
            {
                for (int iv = -span; iv <= span; iv++)
                {
                    var cx = Mathf.RoundToInt((iu + iv + 1) * cell * 0.5f);
                    var cy = Mathf.RoundToInt((iu - iv) * cell * 0.5f);
                    if (cx < -24 || cy < -24 || cx > w + 24 || cy > h + 24) continue;
                    var kind = (iu * 3 + iv * 5) & 3;
                    var mark = kind == 0 ? gold : ((iu + iv) & 1) == 0 ? ink : dim;
                    if (kind == 0) StampHeart(px, w, h, cx, cy, cell * 0.28f, mark);
                    else if (kind == 1) StampStar(px, w, h, cx, cy, cell * 0.26f, mark);
                    else if (kind == 2) StampHundred(px, w, h, cx, cy, mark);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Color32 WithAlpha(Color c, byte a)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(c.r * 255f),
                (byte)Mathf.RoundToInt(c.g * 255f),
                (byte)Mathf.RoundToInt(c.b * 255f),
                a);
        }

        static void Blend(Color32[] px, int w, int h, int x, int y, Color32 src)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h || src.a == 0) return;
            var i = y * w + x;
            var d = px[i];
            int a = src.a;
            int ia = 255 - a;
            px[i] = new Color32(
                (byte)((d.r * ia + src.r * a) / 255),
                (byte)((d.g * ia + src.g * a) / 255),
                (byte)((d.b * ia + src.b * a) / 255),
                255);
        }

        static void StampHeart(Color32[] px, int w, int h, int cx, int cy, float r, Color32 ink)
        {
            int rad = Mathf.CeilToInt(r + 2f);
            for (int y = cy - rad; y <= cy + rad; y++)
            {
                for (int x = cx - rad; x <= cx + rad; x++)
                {
                    var nx = (x - cx) / r;
                    var ny = (y - cy) / r;
                    ny += 0.12f;
                    var a = nx * nx + ny * ny - 0.55f;
                    var inside = a * a * a - nx * nx * ny * ny * ny < 0f;
                    nx *= 1.22f;
                    ny = (ny - 0.12f) * 1.22f + 0.12f;
                    a = nx * nx + ny * ny - 0.55f;
                    var inner = a * a * a - nx * nx * ny * ny * ny < 0f;
                    if (inside && !inner) Blend(px, w, h, x, y, ink);
                }
            }
        }

        static void StampStar(Color32[] px, int w, int h, int cx, int cy, float r, Color32 ink)
        {
            int rad = Mathf.CeilToInt(r + 2f);
            for (int y = cy - rad; y <= cy + rad; y++)
            {
                for (int x = cx - rad; x <= cx + rad; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var radp = Mathf.Sqrt(dx * dx + dy * dy);
                    var ang = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                    var k = Mathf.Pow(Mathf.Abs(Mathf.Cos(2.5f * ang)), 1.25f);
                    var rr = r * Mathf.Lerp(0.38f, 1f, k);
                    if (radp < rr && radp > rr * 0.72f) Blend(px, w, h, x, y, ink);
                }
            }
        }

        static readonly int[] DigitBits =
        {
            0x07, 0x05, 0x05, 0x05, 0x07,
            0x02, 0x02, 0x02, 0x02, 0x02
        };

        static void StampHundred(Color32[] px, int w, int h, int cx, int cy, Color32 ink)
        {
            const int scale = 2;
            const int gw = 3 * scale;
            const int gh = 5 * scale;
            const int gap = 2;
            int total = gw * 3 + gap * 2;
            int x0 = cx - total / 2;
            int y0 = cy - gh / 2;
            StampDigit(px, w, h, x0, y0, 1, scale, ink);
            StampDigit(px, w, h, x0 + gw + gap, y0, 0, scale, ink);
            StampDigit(px, w, h, x0 + (gw + gap) * 2, y0, 0, scale, ink);
        }

        static void StampDigit(Color32[] px, int w, int h, int x0, int y0, int digit, int scale, Color32 ink)
        {
            var baseRow = digit == 1 ? 5 : 0;
            for (int row = 0; row < 5; row++)
            {
                var bits = DigitBits[baseRow + row];
                for (int col = 0; col < 3; col++)
                {
                    if ((bits & (1 << (2 - col))) == 0) continue;
                    for (int oy = 0; oy < scale; oy++)
                        for (int ox = 0; ox < scale; ox++)
                            Blend(px, w, h, x0 + col * scale + ox, y0 + (4 - row) * scale + oy, ink);
                }
            }
        }

        static Sprite BakeInspect(int s, System.Action<Color32[], int> paint)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 0);
            if (paint != null) paint(px, s);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static void PlotInk(Color32[] px, int s, int x, int y, float a)
        {
            if ((uint)x >= (uint)s || (uint)y >= (uint)s || a <= 0f) return;
            var i = y * s + x;
            var na = (int)(Mathf.Clamp01(a) * 255f);
            if (na > px[i].a) px[i] = new Color32(255, 255, 255, (byte)na);
        }

        static void BrushInk(Color32[] px, int s, float x, float y, float r)
        {
            var x0 = Mathf.FloorToInt(x - r);
            var x1 = Mathf.CeilToInt(x + r);
            var y0 = Mathf.FloorToInt(y - r);
            var y1 = Mathf.CeilToInt(y + r);
            var rr = r + 0.35f;
            for (int iy = y0; iy <= y1; iy++)
            {
                for (int ix = x0; ix <= x1; ix++)
                {
                    var dx = ix + 0.5f - x;
                    var dy = iy + 0.5f - y;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    PlotInk(px, s, ix, iy, Mathf.Clamp01(rr - d));
                }
            }
        }

        static void StrokeInk(Color32[] px, int s, float x0, float y0, float x1, float y1, float r)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            var len = Mathf.Sqrt(dx * dx + dy * dy);
            var n = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            for (int i = 0; i <= n; i++)
            {
                var t = i / (float)n;
                BrushInk(px, s, x0 + dx * t, y0 + dy * t, r);
            }
        }

        static void FillDiscInk(Color32[] px, int s, float cx, float cy, float r)
        {
            BrushInk(px, s, cx, cy, r);
            var x0 = Mathf.FloorToInt(cx - r);
            var x1 = Mathf.CeilToInt(cx + r);
            var y0 = Mathf.FloorToInt(cy - r);
            var y1 = Mathf.CeilToInt(cy + r);
            var rr = r - 0.15f;
            for (int iy = y0; iy <= y1; iy++)
            {
                for (int ix = x0; ix <= x1; ix++)
                {
                    var dx = ix + 0.5f - cx;
                    var dy = iy + 0.5f - cy;
                    if (dx * dx + dy * dy <= rr * rr)
                        PlotInk(px, s, ix, iy, 1f);
                }
            }
        }

        static void RingInk(Color32[] px, int s, float cx, float cy, float r, float t)
        {
            var x0 = Mathf.FloorToInt(cx - r - t);
            var x1 = Mathf.CeilToInt(cx + r + t);
            var y0 = Mathf.FloorToInt(cy - r - t);
            var y1 = Mathf.CeilToInt(cy + r + t);
            var outer = r + t * 0.5f;
            var inner = r - t * 0.5f;
            for (int iy = y0; iy <= y1; iy++)
            {
                for (int ix = x0; ix <= x1; ix++)
                {
                    var dx = ix + 0.5f - cx;
                    var dy = iy + 0.5f - cy;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(outer + 0.45f - d) * Mathf.Clamp01(d - inner + 0.45f);
                    PlotInk(px, s, ix, iy, a);
                }
            }
        }

        static void RectStrokeInk(Color32[] px, int s, float x0, float y0, float x1, float y1, float t)
        {
            StrokeInk(px, s, x0, y0, x1, y0, t);
            StrokeInk(px, s, x1, y0, x1, y1, t);
            StrokeInk(px, s, x1, y1, x0, y1, t);
            StrokeInk(px, s, x0, y1, x0, y0, t);
        }

        static void PaintHanger(Color32[] px, int s)
        {
            RingInk(px, s, 32f, 54f, 4.2f, 2.1f);
            StrokeInk(px, s, 32f, 50f, 32f, 46f, 1.6f);
            StrokeInk(px, s, 14f, 45f, 50f, 45f, 1.8f);
            StrokeInk(px, s, 14f, 45f, 18f, 38f, 1.5f);
            StrokeInk(px, s, 50f, 45f, 46f, 38f, 1.5f);
            for (int y = 14; y <= 38; y++)
            {
                var t = (y - 14) / 24f;
                var inset = Mathf.Lerp(6f, 2f, t);
                var x0 = 16f + inset * 0.15f;
                var x1 = 48f - inset * 0.15f;
                if (y > 30)
                {
                    var v = (y - 30) / 8f;
                    var mid = 32f;
                    var cut = 5f * v;
                    StrokeInk(px, s, x0, y, mid - cut, y, 1.15f);
                    StrokeInk(px, s, mid + cut, y, x1, y, 1.15f);
                }
                else
                    StrokeInk(px, s, x0, y, x1, y, 1.05f);
            }
            StrokeInk(px, s, 16f, 14f, 22f, 20f, 1.4f);
            StrokeInk(px, s, 48f, 14f, 42f, 20f, 1.4f);
        }

        static void PaintBars(Color32[] px, int s)
        {
            float[] h = { 16f, 30f, 22f, 26f };
            for (int i = 0; i < 4; i++)
            {
                var x = 16f + i * 11f;
                StrokeInk(px, s, x, 14f, x, 14f + h[i], 3.1f);
            }
        }

        static void PaintSword(Color32[] px, int s)
        {
            StrokeInk(px, s, 16f, 18f, 50f, 50f, 2.4f);
            StrokeInk(px, s, 48f, 46f, 54f, 52f, 1.6f);
            StrokeInk(px, s, 12f, 22f, 22f, 12f, 2.0f);
            StrokeInk(px, s, 14f, 14f, 20f, 20f, 1.5f);
        }

        static void PaintShield(Color32[] px, int s)
        {
            RingInk(px, s, 32f, 32f, 18f, 2.4f);
            StrokeInk(px, s, 24f, 22f, 24f, 42f, 1.6f);
            StrokeInk(px, s, 32f, 20f, 32f, 44f, 1.6f);
            StrokeInk(px, s, 40f, 22f, 40f, 42f, 1.6f);
        }

        static void PaintRing(Color32[] px, int s)
        {
            RingInk(px, s, 32f, 30f, 16f, 2.6f);
            StrokeInk(px, s, 32f, 48f, 26f, 40f, 1.7f);
            StrokeInk(px, s, 32f, 48f, 38f, 40f, 1.7f);
            StrokeInk(px, s, 26f, 40f, 38f, 40f, 1.5f);
        }

        static void PaintCards(Color32[] px, int s)
        {
            RectStrokeInk(px, s, 14f, 16f, 40f, 46f, 1.6f);
            RectStrokeInk(px, s, 24f, 20f, 50f, 50f, 1.6f);
            FillDiscInk(px, s, 37f, 40f, 2.4f);
            StrokeInk(px, s, 32f, 30f, 42f, 30f, 1.2f);
        }
    }
}
