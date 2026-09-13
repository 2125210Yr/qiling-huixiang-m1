using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Dedicated WeakPoint overlay: orange-yellow stereo word, bigger number below.
    /// Bigger and slower than VfxDamagePopup's inline WeakPoint.
    /// Primary Robin ~t68 EN <c>WeakPoint</c> (stacked).
    /// </summary>
    public sealed class VfxWeakPoint : MonoBehaviour
    {
        const float Life = 0.66f;
        const float FadeFrom = 0.60f;
        const float Pop = 1.68f;
        const float Rise = 120f;
        const int WordSize = 52;
        const int NumSize = 86;

        static readonly Color Back = VisualTokens.OrangeCancelBottom;
        static readonly Color Mid = VisualTokens.OrangeLeader;
        static readonly Color Front = VisualTokens.Ember;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color[] WordCol = { Back, Mid, Hot };
        static readonly Vector2[] WordShift =
        {
            new Vector2(-10f, 8f),
            new Vector2(9f, -6f),
            Vector2.zero
        };

        Graphic[] _gfx;
        Color[] _base;
        Image[] _sparks;
        Vector2[] _sparkTo;
        Color[] _sparkC;
        Vector2 _from;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01, int amount)
        {
            if (parent == null) return;

            var go = new GameObject("weakPoint", typeof(RectTransform), typeof(VfxWeakPoint));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 260f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var fx = go.GetComponent<VfxWeakPoint>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(Mathf.Max(0, amount));
        }

        void Build(int amount)
        {
            _from = new Vector2(Random.Range(-20f, 20f), Random.Range(-8f, 10f));
            var digits = FormatAmount(amount);
            var numW = Mathf.Max(180f, digits.Length * NumSize * 0.62f + 24f);

            var glow = Pic("glow", UiSprites.Soft(),
                new Color(Front.r, Front.g, Front.b, 0.42f),
                new Vector2(300f, 200f), new Vector2(0f, 10f));

            const int nSpark = 8;
            _sparks = new Image[nSpark];
            _sparkTo = new Vector2[nSpark];
            _sparkC = new Color[nSpark];
            for (int i = 0; i < nSpark; i++)
            {
                var ang = (i / (float)nSpark) * Mathf.PI * 2f + Random.Range(-0.16f, 0.16f);
                _sparkTo[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(52f, 96f);
                var kind = i % 3;
                var col = kind == 2 ? Front : Hot;
                var sz = kind == 0 ? 18f : kind == 1 ? 16f : 12f;
                _sparks[i] = Pic("spark" + i,
                    kind == 0 ? UiSprites.Spark() : kind == 1 ? UiSprites.Star() : UiSprites.Soft(),
                    col, new Vector2(sz, sz), Vector2.zero);
                _sparkC[i] = col;
            }

            const float wordY = 62f;
            var gfx = new Graphic[6];
            var n = 0;
            gfx[n++] = glow;
            for (int i = 0; i < WordShift.Length; i++)
                gfx[n++] = MkText("word" + i, BattleCueCopy.WeakPoint, WordSize, WordCol[i],
                    new Vector2(280f, 72f), WordShift[i] + new Vector2(0f, wordY), 4f);

            gfx[n++] = MkText("numB", digits, NumSize, Darken(Hot),
                new Vector2(numW, 118f), new Vector2(6f, -38f), 3f);
            gfx[n++] = MkText("num", digits, NumSize, Hot,
                new Vector2(numW, 118f), new Vector2(0f, -32f), 3f);

            _gfx = new Graphic[n];
            _base = new Color[n];
            for (int i = 0; i < n; i++)
            {
                _gfx[i] = gfx[i];
                _base[i] = gfx[i] != null ? gfx[i].color : Color.white;
            }

            ((RectTransform)transform).anchoredPosition = _from;
            transform.localScale = Vector3.one * Pop;
            CanvasShake.Punch(12f, 0.18f);
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

        void Apply(float raw)
        {
            var u = Mathf.Clamp01(raw);
            var punch = u < 0.25f
                ? Mathf.Lerp(Pop, 1.12f, u / 0.25f)
                : Mathf.Lerp(1.12f, 0.96f, (u - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * punch;
            ((RectTransform)transform).anchoredPosition = _from + new Vector2(_from.x * 0.08f, Rise * u);

            var a = u < FadeFrom ? 1f : 1f - (u - FadeFrom) / Mathf.Max(0.01f, 1f - FadeFrom);
            a = Mathf.Clamp01(a);
            if (_gfx != null && _base != null)
            {
                var n = Mathf.Min(_gfx.Length, _base.Length);
                for (int i = 0; i < n; i++)
                {
                    var g = _gfx[i];
                    if (g == null) continue;
                    var c = _base[i];
                    c.a *= a;
                    g.color = c;
                }
            }

            if (_sparks == null || _sparkTo == null || _sparkC == null) return;
            var k = 1f - (1f - u) * (1f - u);
            var ss = Mathf.Lerp(1.08f, 0.28f, u);
            var sa = 1f - Mathf.Clamp01(u / 0.48f);
            var sn = Mathf.Min(_sparks.Length, Mathf.Min(_sparkTo.Length, _sparkC.Length));
            for (int i = 0; i < sn; i++)
            {
                var img = _sparks[i];
                if (img == null) continue;
                img.rectTransform.anchoredPosition = _sparkTo[i] * k;
                img.rectTransform.localScale = Vector3.one * ss;
                var c = _sparkC[i];
                c.a = sa;
                img.color = c;
            }
        }

        Image Pic(string name, Sprite sprite, Color color, Vector2 dim, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Soft());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string s, int size, Color color, Vector2 dim, Vector2 pos, float outline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Bold;
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

    }
}
