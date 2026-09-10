using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Printed / additive luxury 暴击 punch: star burst + spark motes + slash lines,
    /// stacked red word + large red number. Never Critical.
    /// </summary>
    public sealed class VfxCritical : MonoBehaviour
    {
        const float Life = 0.52f;
        const string Word = "暴击";

        static readonly Color Ink = VisualTokens.StarEvolved;
        static readonly Color Back = new Color(0.42f, 0.02f, 0.04f, 1f);
        static readonly Color Mid = VisualTokens.ElemFire;
        static readonly Color Hot = Color.Lerp(Color.white, VisualTokens.StarEvolved, 0.18f);

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
        Graphic[] _copyGfx;
        Color[] _copyC;
        RectTransform _copy;
        Vector2 _from;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01, int amount)
        {
            if (parent == null) return;

            var go = new GameObject("vfxCrit", typeof(RectTransform), typeof(VfxCritical));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxCritical>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(Mathf.Max(0, amount));
        }

        void Build(int amount)
        {
            _from = new Vector2(Random.Range(-18f, 18f), Random.Range(-4f, 8f));

            _core = Pic(transform, "core", UiSprites.Soft(), Ink, 186f);
            _plus = Pic(transform, "plus", UiSprites.Star(), Hot, 52f);
            _cross = Pic(transform, "cross", UiSprites.Star(), Color.white, 40f);
            if (_cross != null) _cross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            _ring = Pic(transform, "ring", UiSprites.Circle(), Ink, 84f);
            _rim = Pic(transform, "rim", UiSprites.Circle(), new Color(1f, 1f, 1f, 0.95f), 50f);

            const int nDot = 10;
            _dots = new Image[nDot];
            _dotDir = new Vector2[nDot];
            for (int i = 0; i < nDot; i++)
            {
                var ang = (i / (float)nDot) * Mathf.PI * 2f;
                _dotDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                _dots[i] = Pic(transform, "dot", UiSprites.Circle(),
                    (i & 1) == 0 ? Ink : Hot, i % 3 == 0 ? 12f : 8f);
            }

            const int nSpark = 8;
            _sparks = new Image[nSpark];
            _sparkTo = new Vector2[nSpark];
            for (int i = 0; i < nSpark; i++)
            {
                var ang = (i / (float)nSpark) * Mathf.PI * 2f + Random.Range(-0.16f, 0.16f);
                _sparkTo[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (96f * Random.Range(0.72f, 1.12f));
                var star = (i & 1) == 0;
                _sparks[i] = Pic(transform, "spark",
                    star ? UiSprites.Spark() : UiSprites.Circle(),
                    star ? Hot : Ink,
                    star ? 22f : 12f);
            }

            const int nLine = 6;
            _lines = new Image[nLine];
            _lineMax = new float[nLine];
            var lineCol = Color.Lerp(Color.white, Ink, 0.40f);
            for (int i = 0; i < nLine; i++)
            {
                var ang = (i / (float)nLine) * 360f + Random.Range(-8f, 8f);
                _lineMax[i] = Random.Range(88f, 132f);
                _lines[i] = Bar(transform, lineCol, ang, 20f, (i & 1) == 0 ? 8f : 6f);
            }

            var copyGo = new GameObject("copy", typeof(RectTransform));
            copyGo.transform.SetParent(transform, false);
            _copy = copyGo.GetComponent<RectTransform>();
            _copy.anchorMin = _copy.anchorMax = new Vector2(0.5f, 0.5f);
            _copy.pivot = new Vector2(0.5f, 0.5f);
            _copy.sizeDelta = Vector2.zero;
            _copy.anchoredPosition = Snap(_from);

            var digits = FormatAmount(amount);
            var numW = Mathf.Max(148f, digits.Length * 48f + 24f);
            var wordB = MkText(_copy, "wordB", Word, 44, Back,
                new Vector2(240f, 56f), new Vector2(6f, 40f), 3f);
            var wordM = MkText(_copy, "wordM", Word, 44, Mid,
                new Vector2(240f, 56f), new Vector2(-5f, 48f), 3f);
            var wordF = MkText(_copy, "wordF", Word, 44, Ink,
                new Vector2(240f, 56f), new Vector2(0f, 44f), 3f);
            var numB = MkText(_copy, "numB", digits, 78, Darken(Ink),
                new Vector2(numW, 96f), new Vector2(5f, -26f), 2f);
            var numF = MkText(_copy, "num", digits, 78, Ink,
                new Vector2(numW, 96f), new Vector2(0f, -22f), 3f);

            _copyGfx = new Graphic[] { wordB, wordM, wordF, numB, numF };
            _copyC = new Color[_copyGfx.Length];
            for (int i = 0; i < _copyGfx.Length; i++)
                _copyC[i] = _copyGfx[i] != null ? _copyGfx[i].color : Color.white;

            _coreC = _core != null ? _core.color : Color.clear;
            _plusC = _plus != null ? _plus.color : Color.clear;
            _crossC = _cross != null ? _cross.color : Color.clear;
            _ringC = _ring != null ? _ring.color : Color.clear;
            _rimC = _rim != null ? _rim.color : Color.clear;
            _dotC = CopyColors(_dots);
            _sparkC = CopyColors(_sparks);
            _lineC = CopyColors(_lines);

            CanvasShake.Punch(16f, 0.18f);
            Apply(0f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age >= Life)
            {
                Destroy(gameObject);
                return;
            }
            Apply(Mathf.Clamp01(_age / Life));
        }

        void Apply(float u)
        {
            var pop = 1f - (1f - u) * (1f - u);
            var punch = u < 0.17f
                ? Mathf.Lerp(1.70f, 1.08f, u / 0.17f)
                : Mathf.Lerp(1.08f, 0.92f, (u - 0.17f) / 0.83f);
            var fade = u < 0.58f ? 1f : 1f - (u - 0.58f) / 0.42f;
            fade = Mathf.Clamp01(fade);

            if (_copy != null)
            {
                _copy.localScale = Vector3.one * punch;
                _copy.anchoredPosition = Snap(_from + new Vector2(_from.x * 0.08f, 36f * u));
            }

            if (_copyGfx != null && _copyC != null)
            {
                var n = Mathf.Min(_copyGfx.Length, _copyC.Length);
                for (int i = 0; i < n; i++)
                {
                    var g = _copyGfx[i];
                    if (g == null) continue;
                    var c = _copyC[i];
                    c.a *= fade;
                    g.color = c;
                }
            }

            if (_core != null)
            {
                _core.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1.72f, pop);
                _core.color = Fade(_coreC, 0.92f * (1f - u));
            }
            if (_plus != null)
            {
                _plus.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.22f, 0.28f, u);
                _plus.color = Fade(_plusC, fade * (1f - Mathf.Clamp01(u / 0.42f)));
            }
            if (_cross != null)
            {
                _cross.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.95f, 0.22f, u);
                _cross.color = Fade(_crossC, fade * (1f - Mathf.Clamp01(u / 0.34f)));
            }
            if (_ring != null)
            {
                _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.42f, 2.20f, u);
                _ring.color = Fade(_ringC, 0.72f * (1f - u));
            }
            if (_rim != null)
            {
                _rim.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.78f, Mathf.Clamp01(u * 1.35f));
                _rim.color = Fade(_rimC, 0.80f * Mathf.Clamp01(1f - u * 1.7f));
            }

            if (_dots != null && _dotC != null && _dotDir != null)
            {
                var rad = Mathf.Lerp(12f, 92f, u);
                var ds = Mathf.Lerp(1f, 0.42f, u);
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
                var ss = Mathf.Lerp(1.08f, 0.28f, u);
                var n = Mathf.Min(_sparks.Length, Mathf.Min(_sparkC.Length, _sparkTo.Length));
                for (int i = 0; i < n; i++)
                {
                    var img = _sparks[i];
                    if (img == null) continue;
                    img.rectTransform.anchoredPosition = Snap(_sparkTo[i] * pop);
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
                var len = Mathf.Lerp(16f, _lineMax[i], grow);
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
                var a = 0.88f * (1f - u);
                if (u < 0.12f) a *= u / 0.12f;
                img.color = Fade(_lineC[i], a);
            }
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

        static Text MkText(Transform parent, string name, string s, int size, Color color, Vector2 dim, Vector2 pos, float outline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = s ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(outline, -outline);
            return tx;
        }

        static Color[] CopyColors(Image[] imgs)
        {
            if (imgs == null) return null;
            var c = new Color[imgs.Length];
            for (int i = 0; i < imgs.Length; i++)
                c[i] = imgs[i] != null ? imgs[i].color : Color.clear;
            return c;
        }

        static string FormatAmount(int amount)
        {
            var raw = amount.ToString();
            if (raw.Length <= 3) return raw;
            var buf = new char[raw.Length + (raw.Length - 1) / 3];
            var j = buf.Length - 1;
            var n = 0;
            for (int i = raw.Length - 1; i >= 0; i--)
            {
                if (n == 3)
                {
                    buf[j--] = ',';
                    n = 0;
                }
                buf[j--] = raw[i];
                n++;
            }
            return new string(buf);
        }

        static Color Darken(Color c)
        {
            return new Color(c.r * 0.35f, c.g * 0.28f, c.b * 0.28f, 1f);
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(CombatFeel.Snap(p.x), CombatFeel.Snap(p.y));
        }

        static Color Fade(Color rgb, float a)
        {
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a));
        }
    }
}
