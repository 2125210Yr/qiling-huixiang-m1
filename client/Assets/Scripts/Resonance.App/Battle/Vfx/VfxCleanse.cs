using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// White-gold cleanse burst. Word 净化. 0.45s.
    /// No English Cleanse.
    /// </summary>
    public sealed class VfxCleanse : MonoBehaviour
    {
        const float Life = 0.45f;
        const string Word = "净化";

        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color White = VisualTokens.TapWhite;
        static readonly Color Hot = Color.Lerp(VisualTokens.TapWhite, VisualTokens.FeverGold, 0.32f);

        Image _glow;
        Image _core;
        Image _ring;
        Image _rim;
        Image _plus;
        Image _cross;
        Image[] _rays;
        Image[] _pips;
        Vector2[] _pipDir;
        Image[] _motes;
        Vector2[] _moteFrom;
        Vector2[] _moteTo;
        float[] _moteSpin;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;

            var go = new GameObject("vfxCleanse", typeof(RectTransform), typeof(VfxCleanse));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxCleanse>().Build();
        }

        void Build()
        {
            _glow = Img("glow", UiSprites.Soft(), WithA(Gold, 0.58f), 168f);
            _core = Img("core", UiSprites.Soft(), WithA(White, 0.96f), 72f);
            _ring = Img("ring", UiSprites.Circle(), WithA(Gold, 0.90f), 88f);
            _rim = Img("rim", UiSprites.Circle(), WithA(White, 0.92f), 52f);
            _plus = Img("plus", UiSprites.Plus(), WithA(Gold, 1f), 48f);
            _cross = Img("cross", UiSprites.Plus(), WithA(White, 0.95f), 36f);
            _cross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            _tag = MkText("tag", Word, 28, Gold, new Vector2(180f, 40f), new Vector2(0f, 52f));

            _glow.transform.localScale = Vector3.one * 0.42f;
            _core.transform.localScale = Vector3.one * 1.35f;
            _ring.transform.localScale = Vector3.one * 0.32f;
            _rim.transform.localScale = Vector3.one * 0.22f;
            _plus.transform.localScale = Vector3.one * 1.55f;
            _cross.transform.localScale = Vector3.one * 1.40f;
            _tag.transform.localScale = Vector3.one * 1.38f;

            const int nRay = 6;
            _rays = new Image[nRay];
            for (int i = 0; i < nRay; i++)
            {
                var col = (i & 1) == 0 ? Gold : White;
                var ray = Img("ray" + i, UiSprites.Slash(), WithA(col, 0.88f), 0f);
                ray.rectTransform.sizeDelta = new Vector2(118f, (i & 1) == 0 ? 16f : 10f);
                ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, i * (180f / nRay) + 10f);
                ray.transform.localScale = new Vector3(0.42f, 1.18f, 1f);
                _rays[i] = ray;
            }

            const int nPip = 10;
            _pips = new Image[nPip];
            _pipDir = new Vector2[nPip];
            for (int i = 0; i < nPip; i++)
            {
                var ang = (i / (float)nPip) * Mathf.PI * 2f;
                _pipDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var col = (i & 1) == 0 ? White : Gold;
                _pips[i] = Img("pip" + i, i % 3 == 0 ? UiSprites.Spark() : UiSprites.Circle(),
                    WithA(col, 0.95f), i % 3 == 0 ? 18f : 9f);
            }

            const int nMote = 8;
            _motes = new Image[nMote];
            _moteFrom = new Vector2[nMote];
            _moteTo = new Vector2[nMote];
            _moteSpin = new float[nMote];
            for (int i = 0; i < nMote; i++)
            {
                var ang = (i / (float)nMote) * Mathf.PI * 2f + 0.22f;
                _moteFrom[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 8f;
                _moteTo[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(62f, 96f);
                _moteSpin[i] = ((i & 1) == 0 ? 90f : -120f) + i * 8f;
                var col = (i & 1) == 0 ? Gold : Hot;
                var sz = (i % 3) == 0 ? 16f : 10f;
                var img = Img("mote" + i, (i & 1) == 0 ? UiSprites.Star() : UiSprites.Spark(),
                    WithA(col, 1f), sz);
                img.rectTransform.anchoredPosition = _moteFrom[i];
                img.color = Color.clear;
                _motes[i] = img;
            }

            for (int i = 0; i < 8; i++)
            {
                var ang = (i / 8f) * Mathf.PI * 2f + 0.18f;
                Spark.Spawn(transform, (i & 1) == 0 ? Gold : White, ang, 86f, false, i % 4 == 0);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.18f)) * (1f - Mathf.Clamp01(u / 0.18f));
            var ease = 1f - (1f - u) * (1f - u);
            var fade = u < 0.12f ? u / 0.12f
                : u < 0.52f ? 1f
                : 1f - (u - 0.52f) / 0.48f;
            fade = Mathf.Clamp01(fade);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.72f, ease);
                SetA(_glow, 0.58f * fade);
            }

            if (_core != null)
            {
                _core.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.18f, u);
                SetA(_core, 0.96f * (1f - u * u));
            }

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.32f, 2.28f, ease);
                SetA(_ring, 0.88f * (1f - u));
            }

            if (_rim != null)
            {
                _rim.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 1.92f, Mathf.Clamp01(u * 1.35f));
                SetA(_rim, 0.82f * Mathf.Clamp01(1f - u * 1.6f));
            }

            if (_plus != null)
            {
                _plus.transform.localScale = Vector3.one * Mathf.Lerp(1.55f, 1.02f, slam);
                SetA(_plus, fade * (1f - Mathf.Clamp01(u / 0.72f)));
            }

            if (_cross != null)
            {
                _cross.transform.localScale = Vector3.one * Mathf.Lerp(1.40f, 0.28f, u);
                SetA(_cross, 0.95f * (1f - Mathf.Clamp01(u / 0.38f)));
            }

            if (_rays != null)
            {
                var sx = Mathf.Lerp(0.42f, 1.18f, slam);
                var sy = Mathf.Lerp(1.18f, 0.22f, u);
                for (int i = 0; i < _rays.Length; i++)
                {
                    var ray = _rays[i];
                    if (ray == null) continue;
                    ray.transform.localScale = new Vector3(sx, sy, 1f);
                    SetA(ray, 0.88f * (1f - u) * fade);
                }
            }

            if (_pips != null && _pipDir != null)
            {
                var rad = Mathf.Lerp(12f, 88f, ease);
                var ps = Mathf.Lerp(1f, 0.38f, u);
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

            if (_motes != null)
            {
                for (int i = 0; i < _motes.Length; i++)
                {
                    var img = _motes[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - i * 0.028f) / 0.82f);
                    var tu = 1f - (1f - t) * (1f - t);
                    img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(_moteFrom[i], _moteTo[i], tu);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _moteSpin[i] * tu);
                    var a = t < 0.10f ? t / 0.10f : 1f - Mathf.Clamp01((t - 0.42f) / 0.58f);
                    var c = (i & 1) == 0 ? Gold : Hot;
                    c.a = a * fade;
                    img.color = c;
                    img.transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.12f, 1f - t);
                }
            }

            if (_tag != null)
            {
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(1.38f, 1f, slam);
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 52f + 18f * ease);
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
