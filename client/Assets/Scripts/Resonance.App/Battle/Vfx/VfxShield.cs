using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Gold/metal hex shield flash on a fighter (block/guard). Word 格挡.
    /// </summary>
    public sealed class VfxShield : MonoBehaviour
    {
        const float Life = 0.35f;

        Image _glow;
        Image _ring;
        Image _plate;
        Image _inner;
        Image _shine;
        Text _tag;
        Color _metal;
        Color _hot;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01, Color color)
        {
            if (parent == null) return;
            if (color.a < 0.05f) color.a = 1f;

            var go = new GameObject("vfxShield", typeof(RectTransform), typeof(VfxShield));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxShield>().Build(color);
        }

        void Build(Color color)
        {
            var metal = VisualTokens.GoldMetal;
            _metal = Color.Lerp(metal, new Color(color.r, color.g, color.b, 1f), 0.28f);
            _hot = Color.Lerp(VisualTokens.TapWhite, metal, 0.32f);

            _glow = Img("glow", UiSprites.Soft(), WithA(_metal, 0.55f), 168f);
            _ring = Img("ring", UiSprites.Circle(), WithA(_metal, 0.88f), 96f);
            _plate = Img("plate", UiSprites.Hex(), WithA(_metal, 0.96f), 118f);
            _inner = Img("inner", UiSprites.Hex(), WithA(_hot, 0.72f), 72f);
            _shine = Img("shine", UiSprites.Soft(), WithA(Color.white, 0.80f), 48f);
            _tag = MkText("tag", "格挡", 28, _hot, new Vector2(180f, 40f), new Vector2(0f, 58f));

            _glow.transform.localScale = Vector3.one * 0.70f;
            _ring.transform.localScale = Vector3.one * 0.40f;
            _plate.transform.localScale = Vector3.one * 0.42f;
            _inner.transform.localScale = Vector3.one * 0.38f;
            _shine.transform.localScale = Vector3.one * 1.25f;
            _tag.transform.localScale = Vector3.one * 1.35f;

            const int n = 6;
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + 0.18f;
                Spark.Spawn(transform, i % 2 == 0 ? _metal : _hot, ang, 72f, true);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.22f)) * (1f - Mathf.Clamp01(u / 0.22f));
            var fade = u < 0.12f ? u / 0.12f
                : u < 0.55f ? 1f
                : 1f - (u - 0.55f) / 0.45f;
            fade = Mathf.Clamp01(fade);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.55f, u);
                SetA(_glow, 0.55f * fade);
            }

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 2.15f, 1f - (1f - u) * (1f - u));
                SetA(_ring, 0.80f * (1f - u));
            }

            if (_plate != null)
            {
                _plate.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.06f, slam);
                SetA(_plate, 0.96f * fade);
            }

            if (_inner != null)
            {
                _inner.transform.localScale = Vector3.one * Mathf.Lerp(0.38f, 1.02f, slam);
                SetA(_inner, 0.72f * fade);
            }

            if (_shine != null)
            {
                _shine.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 0.20f, u);
                SetA(_shine, 0.85f * (1f - u * u));
            }

            if (_tag != null)
            {
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, slam);
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 58f + 16f * u);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Color WithA(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        static void SetA(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        Image Img(string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
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

        Text MkText(string name, string text, int size, Color color, Vector2 dim, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.fontSize = size;
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
