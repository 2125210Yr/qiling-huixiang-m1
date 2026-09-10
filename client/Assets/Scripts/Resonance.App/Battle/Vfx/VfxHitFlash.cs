using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Skill-colored hit burst on the target: soft core + star + ring + spark motes;
    /// fever goes bigger with more motes and radial slash speed lines.
    /// Printed / additive luxury UI overlay. Richer than CombatFeel.Impact; call separately.
    /// </summary>
    public static class VfxHitFlash
    {
        public static void Play(Transform parent, Vector2 anchor01, Color color, bool fever)
        {
            if (parent == null) return;

            color = new Color(color.r, color.g, color.b, 1f);
            var hot = Color.Lerp(Color.white, color, fever ? 0.18f : 0.32f);
            var life = fever ? 0.48f : 0.32f;

            var go = new GameObject("hitFlash", typeof(RectTransform), typeof(Player));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor01;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();

            var core = Pic(go.transform, "core", UiSprites.Soft(), color, fever ? 96f : 88f);
            var plus = Pic(go.transform, "plus", UiSprites.Star(), hot, fever ? 40f : 32f);
            var cross = Pic(go.transform, "cross", UiSprites.Star(), Color.white, fever ? 30f : 24f);
            if (cross != null) cross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            var ring = Pic(go.transform, "ring", UiSprites.Circle(), new Color(color.r, color.g, color.b, 0.55f), fever ? 56f : 48f);
            var rim = Pic(go.transform, "rim", UiSprites.Circle(), new Color(1f, 1f, 1f, 0.55f), fever ? 34f : 28f);

            var nDot = fever ? 14 : 10;
            var dots = new Image[nDot];
            var dotDir = new Vector2[nDot];
            for (int i = 0; i < nDot; i++)
            {
                var ang = (i / (float)nDot) * Mathf.PI * 2f;
                dotDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                dots[i] = Pic(go.transform, "dot", UiSprites.Circle(),
                    (i & 1) == 0 ? color : hot, i % 3 == 0 ? 12f : 8f);
            }

            var nSpark = fever ? 12 : 8;
            var sparks = new Image[nSpark];
            var sparkTo = new Vector2[nSpark];
            var reach = fever ? 132f : 84f;
            for (int i = 0; i < nSpark; i++)
            {
                var ang = (i / (float)nSpark) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
                sparkTo[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (reach * Random.Range(0.72f, 1.12f));
                var star = (i & 1) == 0;
                sparks[i] = Pic(go.transform, "spark",
                    star ? UiSprites.Spark() : UiSprites.Circle(),
                    star ? hot : color,
                    star ? (fever ? 26f : 20f) : (fever ? 14f : 10f));
            }

            Image[] lines = null;
            float[] lineMax = null;
            if (fever)
            {
                const int nLine = 10;
                lines = new Image[nLine];
                lineMax = new float[nLine];
                var lineCol = Color.Lerp(Color.white, color, 0.40f);
                for (int i = 0; i < nLine; i++)
                {
                    var ang = (i / (float)nLine) * 360f + Random.Range(-8f, 8f);
                    lineMax[i] = Random.Range(118f, 168f);
                    lines[i] = Bar(go.transform, lineCol, ang, 24f, (i & 1) == 0 ? 8f : 6f);
                }
            }

            var player = go.GetComponent<Player>();
            if (player == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            player.Bind(
                core, plus, cross, ring, rim,
                dots, dotDir, sparks, sparkTo, lines, lineMax,
                life, fever);
        }

        static Image Pic(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            Paint(img, sprite, color);
            return img;
        }

        static Image Bar(Transform parent, Color color, float angDeg, float len, float thick)
        {
            var go = new GameObject("line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(len, thick);
            rt.localEulerAngles = new Vector3(0f, 0f, angDeg);
            var img = go.GetComponent<Image>();
            Paint(img, UiSprites.Slash(), color);
            return img;
        }

        static void Paint(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(Mathf.Round(p.x * 0.5f) * 2f, Mathf.Round(p.y * 0.5f) * 2f);
        }

        static Color Fade(Color rgb, float a)
        {
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a));
        }

        sealed class Player : MonoBehaviour
        {
            Image _core;
            Image _plus;
            Image _cross;
            Image _ring;
            Image _rim;
            Image[] _dots;
            Image[] _sparks;
            Image[] _lines;
            Vector2[] _dotDir;
            Vector2[] _sparkTo;
            float[] _lineMax;
            Color _coreC;
            Color _plusC;
            Color _crossC;
            Color _ringC;
            Color _rimC;
            Color[] _dotC;
            Color[] _sparkC;
            Color[] _lineC;
            float _life = 0.32f;
            float _age;
            bool _fever;

            public void Bind(
                Image core, Image plus, Image cross, Image ring, Image rim,
                Image[] dots, Vector2[] dotDir, Image[] sparks, Vector2[] sparkTo,
                Image[] lines, float[] lineMax, float life, bool fever)
            {
                _core = core;
                _plus = plus;
                _cross = cross;
                _ring = ring;
                _rim = rim;
                _dots = dots;
                _dotDir = dotDir;
                _sparks = sparks;
                _sparkTo = sparkTo;
                _lines = lines;
                _lineMax = lineMax;
                _life = life;
                _fever = fever;
                _coreC = core != null ? core.color : Color.clear;
                _plusC = plus != null ? plus.color : Color.clear;
                _crossC = cross != null ? cross.color : Color.clear;
                _ringC = ring != null ? ring.color : Color.clear;
                _rimC = rim != null ? rim.color : Color.clear;
                _dotC = CopyColors(dots);
                _sparkC = CopyColors(sparks);
                _lineC = CopyColors(lines);
                Apply(0f);
            }

            static Color[] CopyColors(Image[] imgs)
            {
                if (imgs == null) return null;
                var c = new Color[imgs.Length];
                for (int i = 0; i < imgs.Length; i++)
                    c[i] = imgs[i] != null ? imgs[i].color : Color.clear;
                return c;
            }

            void Update()
            {
                _age += Time.unscaledDeltaTime;
                if (_age >= _life)
                {
                    Destroy(gameObject);
                    return;
                }
                Apply(Mathf.Clamp01(_age / _life));
            }

            void Apply(float u)
            {
                var pop = 1f - (1f - u) * (1f - u);

                if (_core != null)
                {
                    _core.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.40f, _fever ? 1.35f : 1.25f, pop);
                    _core.color = Fade(_coreC, 0.92f * (1f - u));
                }
                if (_plus != null)
                {
                    _plus.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.20f, 0.30f, u);
                    _plus.color = Fade(_plusC, 1f - Mathf.Clamp01(u / 0.42f));
                }
                if (_cross != null)
                {
                    _cross.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.95f, 0.25f, u);
                    _cross.color = Fade(_crossC, 1f - Mathf.Clamp01(u / 0.34f));
                }
                if (_ring != null)
                {
                    _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, _fever ? 1.55f : 1.40f, u);
                    _ring.color = Fade(_ringC, 0.72f * (1f - u));
                }
                if (_rim != null)
                {
                    _rim.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.30f, 1.70f, Mathf.Clamp01(u * 1.35f));
                    _rim.color = Fade(_rimC, 0.80f * Mathf.Clamp01(1f - u * 1.7f));
                }

                if (_dots != null && _dotC != null && _dotDir != null)
                {
                    var rad = Mathf.Lerp(14f, _fever ? 118f : 86f, u);
                    var ds = Mathf.Lerp(1f, 0.45f, u);
                    var n = Mathf.Min(_dots.Length, Mathf.Min(_dotC.Length, _dotDir.Length));
                    for (int i = 0; i < n; i++)
                    {
                        var img = _dots[i];
                        if (img == null) continue;
                        img.rectTransform.anchoredPosition = Snap(_dotDir[i] * rad);
                        img.rectTransform.localScale = Vector3.one * ds;
                        img.color = Fade(_dotC[i], 1f - u);
                    }
                }

                if (_sparks != null && _sparkC != null && _sparkTo != null)
                {
                    var k = pop;
                    var ss = Mathf.Lerp(1.05f, 0.30f, u);
                    var n = Mathf.Min(_sparks.Length, Mathf.Min(_sparkC.Length, _sparkTo.Length));
                    for (int i = 0; i < n; i++)
                    {
                        var img = _sparks[i];
                        if (img == null) continue;
                        img.rectTransform.anchoredPosition = Snap(_sparkTo[i] * k);
                        img.rectTransform.localScale = Vector3.one * ss;
                        img.color = Fade(_sparkC[i], 1f - u);
                    }
                }

                if (_lines == null || _lineC == null || _lineMax == null) return;
                var grow = Mathf.Sqrt(u);
                var ln = Mathf.Min(_lines.Length, Mathf.Min(_lineC.Length, _lineMax.Length));
                for (int i = 0; i < ln; i++)
                {
                    var img = _lines[i];
                    if (img == null) continue;
                    var len = Mathf.Lerp(20f, _lineMax[i], grow);
                    var rt = img.rectTransform;
                    rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
                    var a = 0.88f * (1f - u);
                    if (u < 0.12f) a *= u / 0.12f;
                    img.color = Fade(_lineC[i], a);
                }
            }
        }
    }
}
