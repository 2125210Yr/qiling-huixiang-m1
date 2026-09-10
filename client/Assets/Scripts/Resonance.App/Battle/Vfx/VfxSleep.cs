using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 睡眠: muted-blue 睡 bubbles rising above the head. 0.7s.
    /// No English Sleep.
    /// </summary>
    public sealed class VfxSleep : MonoBehaviour
    {
        const float Life = 0.70f;

        static readonly Color Ink = Color.Lerp(VisualTokens.ElemWater, VisualTokens.TextMuted, 0.58f);
        static readonly Color Deep = Color.Lerp(VisualTokens.IceDeep, VisualTokens.ElemWater, 0.28f);
        static readonly Color Mist = Color.Lerp(VisualTokens.IceShard, VisualTokens.TextMuted, 0.42f);
        static readonly Color GlowCol = new Color(0.36f, 0.48f, 0.64f, 0.36f);
        static readonly Vector2 Head = new Vector2(0f, 72f);

        struct Bubble
        {
            public RectTransform Rt;
            public Image Img;
            public Image Shine;
            public Text Glyph;
            public Vector2 From;
            public Vector2 To;
            public float Delay;
            public float BaseA;
            public float Sz0;
            public float Sz1;
            public float Pop;
            public float Wobble;
        }

        Image _glow;
        Image _haze;
        Bubble[] _bubbles;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfx睡眠", typeof(RectTransform), typeof(VfxSleep));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxSleep>().Build();
        }

        void Build()
        {
            var ink = Ink;
            ink.a = 1f;
            var deep = Deep;
            deep.a = 0.88f;
            var mist = Mist;
            mist.a = 0.55f;
            var glow = GlowCol;
            glow.a = 0.36f;

            _glow = Child(transform, "glow", UiSprites.Soft(), glow, 108f);
            _glow.rectTransform.anchoredPosition = Head;

            _haze = Child(transform, "haze", UiSprites.Soft(), new Color(deep.r, deep.g, deep.b, 0.42f), 1f);
            _haze.rectTransform.sizeDelta = new Vector2(92f, 48f);
            _haze.rectTransform.anchoredPosition = Head + new Vector2(8f, 4f);

            const int n = 6;
            _bubbles = new Bubble[n];
            for (int i = 0; i < n; i++)
            {
                var t = i / (float)Mathf.Max(1, n - 1);
                var sz = Mathf.Lerp(12f, 28f, t);
                var from = Head + new Vector2(-10f + i * 6f + Random.Range(-4f, 4f), Random.Range(-6f, 4f));
                var to = from + new Vector2(8f + t * 22f + Random.Range(-4f, 6f), 42f + t * 36f);
                var fill = (i % 3) == 0
                    ? Color.Lerp(ink, mist, 0.35f)
                    : ((i & 1) == 0 ? Color.Lerp(ink, deep, 0.22f) : Color.Lerp(mist, ink, 0.40f));
                fill.a = 0.92f;
                var img = Child(transform, "bubble" + i, UiSprites.Circle(), fill, sz);
                img.color = Color.clear;
                img.rectTransform.anchoredPosition = from;

                var shineCol = Color.Lerp(VisualTokens.IceCore, ink, 0.55f);
                shineCol.a = 0.70f;
                var shine = Child(img.transform, "shine", UiSprites.Soft(), shineCol, sz * 0.38f);
                shine.rectTransform.anchoredPosition = new Vector2(-sz * 0.16f, sz * 0.18f);
                shine.color = Color.clear;

                Text glyph = null;
                if (i >= 1)
                {
                    var gsz = Mathf.RoundToInt(Mathf.Lerp(12f, 20f, t));
                    glyph = MkText(img.transform, "睡", gsz, Vector2.zero, new Vector2(sz + 8f, sz + 8f), ink);
                    var gc = glyph.color;
                    gc.a = 0f;
                    glyph.color = gc;
                }

                _bubbles[i] = new Bubble
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Shine = shine,
                    Glyph = glyph,
                    From = from,
                    To = to,
                    Delay = i * 0.055f,
                    BaseA = Random.Range(0.70f, 0.94f),
                    Sz0 = sz * 0.28f,
                    Sz1 = sz,
                    Pop = Random.Range(0.68f, 0.88f),
                    Wobble = ((i & 1) == 0 ? 1f : -1f) * Random.Range(7f, 14f)
                };
            }

            _tag = MkText(transform, "睡眠", 22, Head + new Vector2(0f, 28f), new Vector2(180f, 36f), ink);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var ease = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.58f) / 0.42f);
            var bob = Mathf.Sin(_age * 6.4f) * 3.0f;

            if (_glow != null)
            {
                _glow.rectTransform.anchoredPosition = Head + new Vector2(0f, bob * 0.45f);
                _glow.transform.localScale = Vector3.one * (0.78f + 0.22f * (0.5f + 0.5f * Mathf.Sin(_age * 5.2f)));
                var c = GlowCol;
                c.a = GlowCol.a * fade;
                _glow.color = c;
            }

            if (_haze != null)
            {
                _haze.rectTransform.anchoredPosition = Head + new Vector2(8f + bob * 0.4f, 4f + 10f * ease);
                _haze.transform.localScale = new Vector3(
                    Mathf.Lerp(0.62f, 1.18f, ease),
                    Mathf.Lerp(0.70f, 1.05f, ease),
                    1f);
                var c = Deep;
                c.a = 0.42f * fade;
                _haze.color = c;
            }

            if (_bubbles != null)
            {
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    var b = _bubbles[i];
                    if (b.Rt == null || b.Img == null) continue;
                    if (_age < b.Delay)
                    {
                        Hide(b.Img);
                        Hide(b.Shine);
                        if (b.Glyph != null)
                        {
                            var gc = b.Glyph.color;
                            gc.a = 0f;
                            b.Glyph.color = gc;
                        }
                        continue;
                    }

                    var t = Mathf.Clamp01((_age - b.Delay) / Mathf.Max(0.05f, Life - b.Delay));
                    var tu = 1f - (1f - t) * (1f - t);
                    var sway = Mathf.Sin((_age + i) * 7.2f) * b.Wobble * tu;
                    b.Rt.anchoredPosition = Vector2.LerpUnclamped(b.From, b.To, tu) + new Vector2(sway, bob * 0.25f);
                    var popped = t >= b.Pop;
                    var sz = popped
                        ? Mathf.Lerp(b.Sz1, b.Sz1 * 1.45f, (t - b.Pop) / Mathf.Max(0.04f, 1f - b.Pop))
                        : Mathf.Lerp(b.Sz0, b.Sz1, tu);
                    b.Rt.sizeDelta = new Vector2(sz, sz);
                    var a = popped
                        ? b.BaseA * (1f - (t - b.Pop) / Mathf.Max(0.04f, 1f - b.Pop))
                        : b.BaseA * (t < 0.14f ? t / 0.14f : 1f);
                    a = Mathf.Clamp01(a);
                    var ic = b.Img.color;
                    ic.a = a;
                    b.Img.color = ic;

                    if (b.Shine != null)
                    {
                        b.Shine.rectTransform.anchoredPosition = new Vector2(-sz * 0.16f, sz * 0.18f);
                        b.Shine.rectTransform.sizeDelta = new Vector2(sz * 0.38f, sz * 0.38f);
                        var sc = b.Shine.color;
                        sc.a = a * 0.70f;
                        b.Shine.color = sc;
                    }

                    if (b.Glyph != null)
                    {
                        var gc = Ink;
                        gc.a = a;
                        b.Glyph.color = gc;
                        ((RectTransform)b.Glyph.transform).sizeDelta = new Vector2(sz + 8f, sz + 8f);
                    }
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = Head + new Vector2(0f, 28f + 16f * ease);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static void Hide(Image img)
        {
            if (img == null) return;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string text, int size, Vector2 pos, Vector2 dim, Color color)
        {
            var go = new GameObject("tx", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = text;
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }
    }
}
