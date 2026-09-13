# Builds the layered puppet assets out of reference.jpg.
#   reference.jpg  ->  assets/bg.png + assets/<layer>.png (all full-frame 546x292 RGBA)
# Segmentation = hand-authored envelope/limb capsules  x  colour classification.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

Add-Type -AssemblyName System.Drawing

$cs = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class Puppet
{
    static int W, H;
    static byte[] px;        // BGRA of reference
    static bool[] env;       // inside authored envelope
    static bool[] chr;       // binary character mask
    static byte[] alpha;     // feathered character alpha
    static List<double[]> forcedPolys = new List<double[]>();
    static List<double[]> forcedCaps = new List<double[]>();
    static List<double[]> layerPolys = new List<double[]>();
    static List<double[]> layerCaps = new List<double[]>();
    public static int Width { get { return W; } }
    public static int Height { get { return H; } }

    public static void Load(string path)
    {
        using (Bitmap src = new Bitmap(path))
        {
            W = src.Width; H = src.Height;
            using (Bitmap b = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(b)) { g.DrawImage(src, 0, 0, W, H); }
                BitmapData bd = b.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                px = new byte[W * H * 4];
                Marshal.Copy(bd.Scan0, px, 0, px.Length);
                b.UnlockBits(bd);
            }
        }
        forcedPolys.Clear(); forcedCaps.Clear();
    }

    // ---------- geometry helpers ----------
    static bool InPoly(double[] p, double x, double y)
    {
        bool inside = false;
        int n = p.Length / 2;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
            if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) inside = !inside;
        }
        return inside;
    }

    // caps = [x,y,r, x,y,r, ...]  -> union of capsules along the chain
    static bool InCaps(double[] c, double x, double y)
    {
        int n = c.Length / 3;
        for (int i = 0; i < n; i++)
        {
            double cx = c[i * 3], cy = c[i * 3 + 1], cr = c[i * 3 + 2];
            double dx = x - cx, dy = y - cy;
            if (dx * dx + dy * dy <= cr * cr) return true;
            if (i < n - 1)
            {
                double nx = c[(i + 1) * 3], ny = c[(i + 1) * 3 + 1], nr = c[(i + 1) * 3 + 2];
                double ax = nx - cx, ay = ny - cy;
                double len2 = ax * ax + ay * ay;
                if (len2 < 1e-9) continue;
                double t = (dx * ax + dy * ay) / len2;
                if (t < 0) t = 0; if (t > 1) t = 1;
                double qx = cx + ax * t, qy = cy + ay * t;
                double rr = cr + (nr - cr) * t;
                double ddx = x - qx, ddy = y - qy;
                if (ddx * ddx + ddy * ddy <= rr * rr) return true;
            }
        }
        return false;
    }

    public static void SetEnvelope(double[] poly)
    {
        env = new bool[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                env[y * W + x] = InPoly(poly, x + 0.5, y + 0.5);
    }
    public static void AddForcedPoly(double[] poly) { forcedPolys.Add(poly); }
    public static void AddForcedCaps(double[] caps) { forcedCaps.Add(caps); }

    // ---------- classification ----------
    static bool IsCharColour(int x, int y)
    {
        int i = (y * W + x) * 4;
        double b = px[i], g = px[i + 1], r = px[i + 2];
        double lum = 0.3 * r + 0.6 * g + 0.1 * b;
        if (r > 132 && g > 98) return true;                                  // lit skin
        if (r > 102 && g > 72 && b > 76) return true;                        // shadowed skin
        if (lum < 72 && b >= r - 16 && x >= 194 && x <= 290 && y <= 118) return true; // hair
        return false;
    }

    static bool[] Dilate(bool[] m, int rad)
    {
        bool[] o = new bool[W * H];
        int r2 = rad * rad;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (!m[y * W + x]) continue;
                for (int dy = -rad; dy <= rad; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= H) continue;
                    for (int dx = -rad; dx <= rad; dx++)
                    {
                        int xx = x + dx; if (xx < 0 || xx >= W) continue;
                        if (dx * dx + dy * dy <= r2) o[yy * W + xx] = true;
                    }
                }
            }
        return o;
    }
    static bool[] Erode(bool[] m, int rad)
    {
        bool[] inv = new bool[W * H];
        for (int i = 0; i < m.Length; i++) inv[i] = !m[i];
        bool[] d = Dilate(inv, rad);
        bool[] o = new bool[W * H];
        for (int i = 0; i < m.Length; i++) o[i] = !d[i];
        return o;
    }

    static void FillHoles(bool[] m)
    {
        bool[] seen = new bool[W * H];
        Stack<int> st = new Stack<int>();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (m[i] || seen[i]) continue;
                bool edge = (x == 0 || y == 0 || x == W - 1 || y == H - 1) || !env[i];
                if (edge) { seen[i] = true; st.Push(i); }
            }
        while (st.Count > 0)
        {
            int i = st.Pop();
            int x = i % W, y = i / W;
            int[] nx = { x - 1, x + 1, x, x };
            int[] ny = { y, y, y - 1, y + 1 };
            for (int k = 0; k < 4; k++)
            {
                if (nx[k] < 0 || ny[k] < 0 || nx[k] >= W || ny[k] >= H) continue;
                int j = ny[k] * W + nx[k];
                if (m[j] || seen[j]) continue;
                seen[j] = true; st.Push(j);
            }
        }
        for (int i = 0; i < m.Length; i++) if (!m[i] && !seen[i]) m[i] = true;
    }

    static void KeepLargest(bool[] m)
    {
        int[] lab = new int[W * H];
        int best = 0, bestSize = 0, cur = 0;
        Stack<int> st = new Stack<int>();
        for (int s = 0; s < m.Length; s++)
        {
            if (!m[s] || lab[s] != 0) continue;
            cur++; int size = 0;
            lab[s] = cur; st.Push(s);
            while (st.Count > 0)
            {
                int i = st.Pop(); size++;
                int x = i % W, y = i / W;
                int[] nx = { x - 1, x + 1, x, x };
                int[] ny = { y, y, y - 1, y + 1 };
                for (int k = 0; k < 4; k++)
                {
                    if (nx[k] < 0 || ny[k] < 0 || nx[k] >= W || ny[k] >= H) continue;
                    int j = ny[k] * W + nx[k];
                    if (!m[j] || lab[j] != 0) continue;
                    lab[j] = cur; st.Push(j);
                }
            }
            if (size > bestSize) { bestSize = size; best = cur; }
        }
        for (int i = 0; i < m.Length; i++) if (m[i] && lab[i] != best) m[i] = false;
    }

    static byte[] BoxBlur(byte[] a, int rad)
    {
        double[] t = new double[W * H];
        byte[] o = new byte[W * H];
        int win = rad * 2 + 1;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double s = 0; int n = 0;
                for (int dx = -rad; dx <= rad; dx++)
                {
                    int xx = x + dx; if (xx < 0 || xx >= W) continue;
                    s += a[y * W + xx]; n++;
                }
                t[y * W + x] = s / n;
            }
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double s = 0; int n = 0;
                for (int dy = -rad; dy <= rad; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= H) continue;
                    s += t[yy * W + x]; n++;
                }
                double v = s / n;
                o[y * W + x] = (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
            }
        return o;
    }

    public static void BuildMask()
    {
        chr = new bool[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                bool v = env[i] && IsCharColour(x, y);
                if (!v)
                {
                    for (int k = 0; k < forcedPolys.Count && !v; k++) if (InPoly(forcedPolys[k], x + 0.5, y + 0.5)) v = true;
                    for (int k = 0; k < forcedCaps.Count && !v; k++) if (InCaps(forcedCaps[k], x + 0.5, y + 0.5)) v = true;
                }
                chr[i] = v;
            }
        FillHoles(chr);
        chr = Erode(Dilate(chr, 2), 2);   // close notches
        FillHoles(chr);
        KeepLargest(chr);
        byte[] hard = new byte[W * H];
        for (int i = 0; i < chr.Length; i++) hard[i] = chr[i] ? (byte)255 : (byte)0;
        byte[] soft = BoxBlur(hard, 1);
        alpha = new byte[W * H];
        for (int i = 0; i < soft.Length; i++)
        {
            // keep the core solid, feather only the rim
            double v = soft[i] / 255.0;
            v = v * 1.35 - 0.14;
            if (v < 0) v = 0; if (v > 1) v = 1;
            alpha[i] = (byte)(v * 255);
        }
    }

    // ---------- layer export ----------
    public static void BeginLayer() { layerPolys.Clear(); layerCaps.Clear(); }
    public static void LayerPoly(double[] p) { layerPolys.Add(p); }
    public static void LayerCaps(double[] c) { layerCaps.Add(c); }

    public static void EndLayer(string file, int grow)
    {
        bool[] sel = new bool[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool v = false;
                for (int k = 0; k < layerPolys.Count && !v; k++) if (InPoly(layerPolys[k], x + 0.5, y + 0.5)) v = true;
                for (int k = 0; k < layerCaps.Count && !v; k++) if (InCaps(layerCaps[k], x + 0.5, y + 0.5)) v = true;
                sel[y * W + x] = v;
            }
        if (grow > 0) sel = Dilate(sel, grow);
        byte[] hard = new byte[W * H];
        for (int i = 0; i < sel.Length; i++) hard[i] = sel[i] ? (byte)255 : (byte)0;
        byte[] soft = BoxBlur(hard, 1);   // 1px soft interior cut, avoids aliased seams
        byte[] outp = new byte[W * H * 4];
        for (int i = 0; i < W * H; i++)
        {
            int a = alpha[i] * soft[i] / 255;
            outp[i * 4 + 3] = (byte)a;
            if (a == 0) continue;            // leave RGB at 0 so empty area compresses away
            outp[i * 4] = px[i * 4];
            outp[i * 4 + 1] = px[i * 4 + 1];
            outp[i * 4 + 2] = px[i * 4 + 2];
        }
        SavePng(outp, file);
    }

    static void SavePng(byte[] bgra, string file)
    {
        using (Bitmap b = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            BitmapData bd = b.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);
            b.UnlockBits(bd);
            b.Save(file, ImageFormat.Png);
        }
    }

    // ---------- background plate ----------
    public static void ExportBackground(string file, int grow)
    {
        bool[] hole = Dilate(chr, grow);
        byte[] outp = new byte[W * H * 4];
        Array.Copy(px, outp, px.Length);

        // horizontal span interpolation across the removed figure
        for (int y = 0; y < H; y++)
        {
            int x = 0;
            while (x < W)
            {
                if (!hole[y * W + x]) { x++; continue; }
                int s = x; while (x < W && hole[y * W + x]) x++;
                int e = x - 1;
                int li = s - 1, ri = e + 1;
                for (int c = 0; c < 3; c++)
                {
                    double lv, rv;
                    lv = (li >= 0) ? outp[(y * W + li) * 4 + c] : -1;
                    rv = (ri < W) ? outp[(y * W + ri) * 4 + c] : -1;
                    if (lv < 0) lv = rv; if (rv < 0) rv = lv;
                    if (lv < 0) { lv = 0; rv = 0; }
                    for (int k = s; k <= e; k++)
                    {
                        double t = (e - s) == 0 ? 0.5 : (double)(k - s) / (e - s);
                        double v = lv + (rv - lv) * t;
                        outp[(y * W + k) * 4 + c] = (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
                    }
                }
            }
        }

        // kill the interpolation streaks: heavy blur, but only inside the removed area
        for (int pass = 0; pass < 3; pass++)
        {
            byte[] tmp = (byte[])outp.Clone();
            int rad = 5;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!hole[y * W + x]) continue;
                    for (int c = 0; c < 3; c++)
                    {
                        double s = 0; int n = 0;
                        for (int dy = -rad; dy <= rad; dy++)
                        {
                            int yy = y + dy; if (yy < 0 || yy >= H) continue;
                            for (int dx = -rad; dx <= rad; dx++)
                            {
                                int xx = x + dx; if (xx < 0 || xx >= W) continue;
                                s += tmp[(yy * W + xx) * 4 + c]; n++;
                            }
                        }
                        outp[(y * W + x) * 4 + c] = (byte)(s / n);
                    }
                }
        }

        // grain so the reconstructed plate matches the compressed source
        Random rnd = new Random(7);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (!hole[y * W + x]) continue;
                int j = rnd.Next(-4, 5);
                for (int c = 0; c < 3; c++)
                {
                    int v = outp[(y * W + x) * 4 + c] + j;
                    outp[(y * W + x) * 4 + c] = (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
                }
            }

        // paint out the source video timecode (bottom-left) with the wall above it
        for (int y = 262; y < H; y++)
            for (int x = 0; x < 56; x++)
                for (int c = 0; c < 3; c++)
                    outp[(y * W + x) * 4 + c] = px[((y - 34) * W + x) * 4 + c];

        for (int i = 0; i < W * H; i++) outp[i * 4 + 3] = 255;
        SaveJpg(outp, file, 92L);
    }

    static void SaveJpg(byte[] bgra, string file, long quality)
    {
        using (Bitmap b = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            BitmapData bd = b.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);
            b.UnlockBits(bd);
            ImageCodecInfo enc = null;
            foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders()) if (c.FormatID == ImageFormat.Jpeg.Guid) enc = c;
            EncoderParameters ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            b.Save(file, enc, ep);
        }
    }

    public static void ExportDebug(string file, double[] envPoly)
    {
        byte[] outp = new byte[W * H * 4];
        for (int i = 0; i < W * H; i++)
        {
            int x = i % W, y = i / W;
            bool chk = (((x / 8) + (y / 8)) % 2) == 0;
            int bg = chk ? 60 : 96;
            double a = alpha[i] / 255.0;
            for (int c = 0; c < 3; c++)
                outp[i * 4 + c] = (byte)(px[i * 4 + c] * a + bg * (1 - a));
            outp[i * 4 + 3] = 255;
        }
        SavePng(outp, file);
    }
}
'@

Add-Type -TypeDefinition $cs -ReferencedAssemblies System.Drawing

$ref = Join-Path $root 'reference.jpg'
[Puppet]::Load($ref)

# ---------------------------------------------------------------- envelope
$envelope = @(
  204, 32, 208, 18, 220, 10, 240, 8, 258, 10, 272, 18, 280, 38, 285, 58, 287, 74,
  298, 78, 314, 90, 330, 100, 340, 114, 336, 130, 322, 140, 306, 146,
  302, 162, 308, 184, 308, 206, 304, 228, 296, 246,
  294, 270, 294, 292,
  240, 292, 244, 264, 242, 232, 240, 208,
  228, 206, 210, 204, 202, 196,
  194, 210, 180, 226, 168, 242, 156, 254, 142, 264, 122, 262, 114, 246, 126, 230, 136, 214, 142, 198,
  150, 178, 160, 154, 168, 130, 174, 106, 182, 90, 198, 82, 216, 90, 228, 110,
  228, 92, 224, 78, 214, 60, 206, 44
)
[Puppet]::SetEnvelope($envelope)

# dress / choker are red-on-red, force them in
[Puppet]::AddForcedPoly(@(242, 84, 258, 82, 274, 96, 292, 130, 300, 160, 304, 190, 300, 222, 292, 240, 268, 236, 252, 210, 246, 176, 240, 140, 238, 104))
[Puppet]::AddForcedPoly(@(240, 74, 264, 72, 266, 88, 240, 90))
# red toe-nails / anklet ribbon on the raised foot
[Puppet]::AddForcedCaps(@(150, 214, 7, 138, 234, 7, 128, 250, 7))
[Puppet]::BuildMask()

$dbg = Join-Path $root '_tools\debug_cutout.png'
[Puppet]::ExportDebug($dbg, $envelope)
[Puppet]::ExportBackground((Join-Path $assets 'bg.jpg'), 3)

# ---------------------------------------------------------------- layers
function L([string]$name, [double[][]]$polys, [double[][]]$caps, [int]$grow) {
  [Puppet]::BeginLayer()
  foreach ($p in $polys) { if ($p) { [Puppet]::LayerPoly($p) } }
  foreach ($c in $caps) { if ($c) { [Puppet]::LayerCaps($c) } }
  [Puppet]::EndLayer((Join-Path $assets "$name.png"), $grow)
  "  + $name"
}

# hair mass behind head/shoulders
L 'hair_back' @(, @(196, 40, 200, 20, 218, 8, 246, 6, 268, 12, 282, 30, 290, 60, 292, 96, 274, 112, 250, 116, 226, 112, 208, 96, 198, 70)) @() 2

# head = face + bangs + horns, hinged at the neck
L 'head' @(, @(206, 34, 212, 16, 226, 8, 246, 6, 264, 12, 276, 22, 282, 42, 284, 62, 278, 76, 268, 86, 248, 90, 232, 86, 218, 74, 208, 56)) @() 2

# eye band (drawn on top of head, nudged for eye tracking)
L 'eyes' @(, @(224, 44, 262, 42, 264, 66, 226, 68)) @() 0

# torso + dress + hips
L 'body' @(, @(222, 70, 248, 66, 274, 78, 292, 104, 302, 144, 308, 184, 306, 216, 296, 244, 262, 246, 240, 216, 228, 176, 216, 132, 214, 96)) @() 2

# skirt hem / slit that flutters
L 'dress_hem' @(, @(236, 178, 268, 172, 300, 180, 310, 210, 302, 244, 268, 250, 244, 236, 234, 208)) @() 2

# left arm (viewer right): shoulder -> elbow -> hand on hip
L 'arm' @() @(, @(272, 80, 15, 296, 84, 14, 318, 100, 15, 332, 118, 14, 314, 138, 14, 292, 146, 14)) 2

# raised leg: hip -> knee -> ankle -> toes
L 'leg_raised' @() @(, @(234, 158, 30, 214, 132, 30, 192, 108, 24, 176, 130, 22, 162, 156, 20, 150, 186, 18, 142, 214, 16, 132, 238, 14, 124, 252, 12)) 2

# standing leg
L 'leg_stand' @() @(, @(258, 186, 28, 262, 226, 24, 266, 262, 22, 268, 296, 22)) 2

"seg done $([Puppet]::Width)x$([Puppet]::Height)"
