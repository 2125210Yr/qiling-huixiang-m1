using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 休息: soft gold steam wisps rising from the body. ~0.8s.
    /// </summary>
    public sealed class VfxRestSteam : MonoBehaviour
    {
        const float Life = 0.80f;

        static readonly Color Ink = VisualTokens.GoldTitle;
        static readonly Color Warm = VisualTokens.FeverGold;
        static readonly Color GlowCol = new Color(
            VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.38f);
        static readonly Color Mist = Color.Lerp(VisualTokens.TapWhite, VisualTokens.GoldMetal, 0.42f);

        struct Wisp
        {
            public Image Img;
            public Vector2 From;
            public Vector2 To;
            public Vector2 Size0;
            public Vector2 Size1;
            public float Delay;
            public float Spin;
            public float Sway;
            public float BaseA;
            public Color Tint;
        }

        Image _glow;
        Image _haze;
        Wisp[] _wisps;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfx休息", typeof(RectTransform), typeof(VfxRestSteam));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxRestSteam>().Build();
        }

        void Build()
        {
            _glow = Child(transform, "glow", UiSprites.Soft(), GlowCol, new Vector2(108f, 96f));

            var hazeCol = Mist;
            hazeCol.a = 0.36f;
            _haze = Child(transform, "haze", UiSprites.Soft(), hazeCol, new Vector2(72f, 40f));
            _haze.rectTransform.anchoredPosition = new Vector2(4f, 6f);

            const int n = 7;
            _wisps = new Wisp[n];
            for (int i = 0; i < n; i++)
            {
                var t = i / (float)Mathf.Max(1, n - 1);
                var x = Mathf.Lerp(-28f, 28f, t) + Random.Range(-8f, 8f);
                var y = Random.Range(-10f, 14f);
                var from = new Vector2(x, y);
                var to = new Vector2(x + Random.Range(-16f, 16f), y + Random.Range(56f, 104f));
                var w0 = Mathf.Lerp(16f, 28f, t) + Random.Range(-3f, 3f);
                var h0 = w0 * Random.Range(1.35f, 1.85f);
                var tint = (i % 3) == 0 ? Warm : ((i & 1) == 0 ? Ink : Mist);
                tint.a = 1f;
                var img = Child(transform, "wisp" + i, UiSprites.Soft(), tint, new Vector2(w0, h0));
                img.rectTransform.anchoredPosition = from;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, Random.Range(-18f, 18f));
                img.color = Color.clear;

                _wisps[i] = new Wisp
                {
                    Img = img,
                    From = from,
                    To = to,
                    Size0 = new Vector2(w0 * 0.55f, h0 * 0.62f),
                    Size1 = new Vector2(w0 * Random.Range(1.25f, 1.70f), h0 * Random.Range(1.55f, 2.15f)),
                    Delay = i * 0.042f,
                    Spin = Random.Range(-22f, 22f),
                    Sway = ((i & 1) == 0 ? 1f : -1f) * Random.Range(8f, 16f),
                    BaseA = Random.Range(0.42f, 0.72f),
                    Tint = tint
                };
            }

            _tag = MkText(transform, "REST", 22, new Vector2(0f, 28f), new Vector2(80f, 36f), Ink);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var rise = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.52f) / 0.48f);
            var bob = Mathf.Sin(_age * 5.6f) * 2.4f;

            if (_glow != null)
            {
                _glow.rectTransform.anchoredPosition = new Vector2(0f, bob * 0.4f);
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.22f, rise);
                var c = GlowCol;
                c.a = 0.38f * fade;
                _glow.color = c;
            }

            if (_haze != null)
            {
                _haze.rectTransform.anchoredPosition = new Vector2(4f + bob * 0.55f, 6f + 28f * rise);
                _haze.transform.localScale = new Vector3(
                    Mathf.Lerp(0.70f, 1.28f, rise),
                    Mathf.Lerp(0.78f, 1.16f, rise),
                    1f);
                var c = Mist;
                c.a = 0.36f * fade;
                _haze.color = c;
            }

            if (_wisps != null)
            {
                for (int i = 0; i < _wisps.Length; i++)
                {
                    var w = _wisps[i];
                    if (w.Img == null) continue;
                    var t = Mathf.Clamp01((u - w.Delay) / 0.78f);
                    var ease = 1f - (1f - t) * (1f - t);
                    var sway = Mathf.Sin((_age + i * 0.37f) * 6.4f) * w.Sway * ease;
                    w.Img.rectTransform.anchoredPosition =
                        Vector2.LerpUnclamped(w.From, w.To, ease) + new Vector2(sway, bob * 0.2f);
                    w.Img.rectTransform.sizeDelta = Vector2.LerpUnclamped(w.Size0, w.Size1, ease);
                    w.Img.rectTransform.localEulerAngles = new Vector3(0f, 0f, w.Spin * ease);
                    var a = t < 0.16f ? t / 0.16f : 1f - Mathf.Clamp01((t - 0.46f) / 0.54f);
                    var c = w.Tint;
                    c.a = w.BaseA * a;
                    w.Img.color = c;
                }
            }

            if (_tag != null)
            {
                var c = Ink;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 28f + 18f * rise);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
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
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }
    }
}
