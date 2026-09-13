using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Bleed: red drip + short slash ticks. Primary EN <c>Bleed</c> (Hard r56); CN 流血 routes.
    /// 0.55s.
    /// </summary>
    public sealed class VfxBleed : MonoBehaviour
    {
        const float Life = 0.55f;

        static readonly Color Ink = VisualTokens.StarEvolved;
        static readonly Color Deep = new Color(0.38f, 0.04f, 0.06f, 0.92f);
        static readonly Color Hot = new Color(1f, 0.62f, 0.42f, 1f);

        struct Tick
        {
            public RectTransform Rt;
            public Image Img;
            public float Delay;
            public float StartA;
        }

        struct Drop
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Delay;
            public float Spin;
            public Color Color;
            public Vector2 Size0;
            public Vector2 Size1;
        }

        Image _glow;
        Image _stain;
        Image _core;
        Tick[] _ticks;
        Drop[] _drops;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;

            var go = new GameObject("vfxBleed", typeof(RectTransform), typeof(VfxBleed));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor01;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxBleed>().Build();
        }

        void Build()
        {
            _glow = Pic(transform, "glow", UiSprites.Soft(), WithA(Ink, 0.62f), 96f);
            _stain = Pic(transform, "stain", UiSprites.Soft(), WithA(Deep, 0.78f), 1f);
            _stain.rectTransform.sizeDelta = new Vector2(108f, 52f);
            _stain.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            _core = Pic(transform, "core", UiSprites.Soft(), WithA(Hot, 0.95f), 28f);

            var angs = new[] { -38f, -12f, 16f, 42f };
            _ticks = new Tick[angs.Length];
            for (int i = 0; i < angs.Length; i++)
            {
                var hot = (i & 1) == 0;
                var col = hot ? Hot : Ink;
                col.a = 0.96f;
                var img = Pic(transform, "tick" + i, UiSprites.Slash(), col, 1f);
                var len = 64f + i * 8f;
                var thick = (i & 1) == 0 ? 11f : 8f;
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(len, thick);
                rt.localEulerAngles = new Vector3(0f, 0f, angs[i]);
                rt.anchoredPosition = new Vector2((i - 1.5f) * 10f, 8f - i * 3f);
                rt.localScale = new Vector3(0.42f, 1.22f, 1f);
                _ticks[i] = new Tick
                {
                    Rt = rt,
                    Img = img,
                    Delay = i * 0.045f,
                    StartA = col.a
                };
            }

            const int nDrop = 7;
            _drops = new Drop[nDrop];
            for (int i = 0; i < nDrop; i++)
            {
                var tear = i % 3 != 1;
                var sz = Random.Range(8f, 16f);
                var col = (i & 1) == 0 ? Ink : Color.Lerp(Ink, Deep, 0.45f);
                if (i % 3 == 0) col = Color.Lerp(col, Hot, 0.35f);
                col.a = 1f;
                var img = Pic(transform, "drip" + i, UiSprites.Circle(), col, sz);
                var size0 = tear ? new Vector2(sz * 0.62f, sz * 1.15f) : new Vector2(sz * 0.72f, sz * 0.95f);
                var size1 = tear ? new Vector2(sz * 0.42f, sz * 1.85f) : new Vector2(sz * 0.50f, sz * 1.45f);
                img.rectTransform.sizeDelta = size0;
                var x = Random.Range(-28f, 28f);
                var y = Random.Range(6f, 22f);
                img.rectTransform.anchoredPosition = new Vector2(x, y);
                _drops[i] = new Drop
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Pos = new Vector2(x, y),
                    Vel = new Vector2(Random.Range(-22f, 22f), Random.Range(-8f, 28f)),
                    Delay = 0.04f + i * 0.038f,
                    Spin = Random.Range(-40f, 40f),
                    Color = col,
                    Size0 = size0,
                    Size1 = size1
                };
            }

            _tag = MkText(transform, "Bleed", 22, Ink, new Vector2(0f, 28f), new Vector2(160f, 36f));
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _age += dt;
            var u = Mathf.Clamp01(_age / Life);
            var ease = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.62f) / 0.38f);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.48f, 1.55f, ease);
                var c = Ink;
                c.a = 0.62f * fade;
                _glow.color = c;
            }
            if (_stain != null)
            {
                _stain.transform.localScale = new Vector3(
                    Mathf.Lerp(0.55f, 1.22f, ease),
                    Mathf.Lerp(0.70f, 1.08f, ease),
                    1f);
                var c = Deep;
                c.a = 0.78f * fade;
                _stain.color = c;
            }
            if (_core != null)
            {
                _core.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.18f, u);
                var c = Hot;
                c.a = 0.95f * (1f - u * u);
                _core.color = c;
            }

            if (_ticks != null)
            {
                for (int i = 0; i < _ticks.Length; i++)
                {
                    var t = _ticks[i];
                    if (t.Img == null || t.Rt == null) continue;
                    var k = Mathf.Clamp01((_age - t.Delay) / 0.28f);
                    var pop = 1f - (1f - k) * (1f - k);
                    t.Rt.localScale = new Vector3(
                        Mathf.Lerp(0.42f, 1.12f, pop),
                        Mathf.Lerp(1.22f, 0.22f, k),
                        1f);
                    var c = t.Img.color;
                    c.a = t.StartA * (k < 0.12f ? k / 0.12f : 1f - Mathf.Clamp01((k - 0.42f) / 0.58f));
                    t.Img.color = c;
                }
            }

            if (_drops != null)
            {
                for (int i = 0; i < _drops.Length; i++)
                {
                    var d = _drops[i];
                    if (d.Rt == null || d.Img == null || _age < d.Delay) continue;
                    d.Vel += new Vector2(0f, -640f * dt);
                    d.Pos += d.Vel * dt;
                    d.Rt.anchoredPosition = d.Pos;
                    var k = Mathf.Clamp01((_age - d.Delay) / Mathf.Max(0.08f, Life - d.Delay));
                    d.Rt.sizeDelta = Vector2.Lerp(d.Size0, d.Size1, k);
                    d.Rt.localEulerAngles = new Vector3(0f, 0f, d.Spin * k);
                    var c = d.Color;
                    c.a = d.Color.a * (k < 0.10f ? k / 0.10f : 1f - Mathf.Clamp01((k - 0.48f) / 0.52f));
                    d.Img.color = c;
                    _drops[i] = d;
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 28f + 18f * ease);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
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
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 dim)
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
