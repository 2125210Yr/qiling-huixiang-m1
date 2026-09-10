using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Gold invulnerability octagon/hex flash on a fighter. Word 无敌. 0.5s.
    /// No English Invincible.
    /// </summary>
    public sealed class VfxInvuln : MonoBehaviour
    {
        const float Life = 0.50f;
        const string Word = "无敌";

        static readonly Color Metal = VisualTokens.GoldMetal;
        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color Hot = Color.Lerp(VisualTokens.TapWhite, VisualTokens.FeverGold, 0.28f);

        Image _glow;
        Image _ring;
        Image _plate;
        Image _oct;
        Image _inner;
        Image _shine;
        Image[] _pips;
        Vector2[] _pipDir;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;

            var go = new GameObject("vfxInvuln", typeof(RectTransform), typeof(VfxInvuln));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxInvuln>().Build();
        }

        void Build()
        {
            _glow = Img("glow", UiSprites.Soft(), WithA(Gold, 0.52f), 176f);
            _ring = Img("ring", UiSprites.HexRing(), WithA(Metal, 0.92f), 108f);
            _plate = Img("plate", UiSprites.Hex(), WithA(Metal, 0.96f), 122f);
            _oct = Img("oct", UiSprites.Hex(), WithA(Gold, 0.78f), 122f);
            _oct.rectTransform.localEulerAngles = new Vector3(0f, 0f, 22.5f);
            _inner = Img("inner", UiSprites.Hex(), WithA(Hot, 0.74f), 72f);
            _shine = Img("shine", UiSprites.Soft(), WithA(Color.white, 0.82f), 48f);
            _tag = MkText("tag", Word, 28, Gold, new Vector2(180f, 40f), new Vector2(0f, 58f));

            _glow.transform.localScale = Vector3.one * 0.62f;
            _ring.transform.localScale = Vector3.one * 0.38f;
            _plate.transform.localScale = Vector3.one * 0.40f;
            _oct.transform.localScale = Vector3.one * 0.40f;
            _inner.transform.localScale = Vector3.one * 0.36f;
            _shine.transform.localScale = Vector3.one * 1.28f;
            _tag.transform.localScale = Vector3.one * 1.40f;

            const int n = 8;
            _pips = new Image[n];
            _pipDir = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + Mathf.PI / 8f;
                _pipDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var col = (i & 1) == 0 ? Metal : Gold;
                _pips[i] = Img("pip" + i, (i & 1) == 0 ? UiSprites.Star() : UiSprites.Circle(),
                    WithA(col, 0.95f), (i & 1) == 0 ? 13f : 9f);
                Spark.Spawn(transform, col, ang, 78f, false);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.20f)) * (1f - Mathf.Clamp01(u / 0.20f));
            var fade = u < 0.12f ? u / 0.12f
                : u < 0.58f ? 1f
                : 1f - (u - 0.58f) / 0.42f;
            fade = Mathf.Clamp01(fade);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.58f, u);
                SetA(_glow, 0.52f * fade);
            }

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.38f, 2.05f, 1f - (1f - u) * (1f - u));
                _ring.rectTransform.localEulerAngles = new Vector3(0f, 0f, u * 18f);
                SetA(_ring, 0.88f * (1f - u));
            }

            if (_plate != null)
            {
                _plate.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1.06f, slam);
                SetA(_plate, 0.96f * fade);
            }

            if (_oct != null)
            {
                _oct.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1.10f, slam);
                _oct.rectTransform.localEulerAngles = new Vector3(0f, 0f, 22.5f + u * 12f);
                SetA(_oct, 0.78f * fade);
            }

            if (_inner != null)
            {
                _inner.transform.localScale = Vector3.one * Mathf.Lerp(0.36f, 1.02f, slam);
                SetA(_inner, 0.74f * fade);
            }

            if (_shine != null)
            {
                _shine.transform.localScale = Vector3.one * Mathf.Lerp(1.28f, 0.18f, u);
                SetA(_shine, 0.85f * (1f - u * u));
            }

            if (_pips != null && _pipDir != null)
            {
                var rad = Mathf.Lerp(18f, 72f, 1f - (1f - u) * (1f - u));
                var ps = Mathf.Lerp(1f, 0.42f, u);
                var n = Mathf.Min(_pips.Length, _pipDir.Length);
                for (int i = 0; i < n; i++)
                {
                    var img = _pips[i];
                    if (img == null) continue;
                    img.rectTransform.anchoredPosition = _pipDir[i] * rad;
                    img.transform.localScale = Vector3.one * ps;
                    SetA(img, 0.95f * (1f - u));
                }
            }

            if (_tag != null)
            {
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(1.40f, 1f, slam);
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
