using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Stun: gold stars orbit a faint ring above the head.
    /// Optional floating <c>Stun</c> (primary EN chips).
    /// </summary>
    public sealed class VfxStun : MonoBehaviour
    {
        const float Life = 0.80f;
        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color RingCol = new Color(1f, 0.90f, 0.45f, 0.36f);
        static readonly Color GlowCol = new Color(1f, 0.82f, 0.28f, 0.32f);
        static readonly Vector2 Head = new Vector2(0f, 78f);

        Image _glow;
        Image _ring;
        Image[] _stars;
        Color[] _starC;
        float[] _phase;
        float[] _spin;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfxStun", typeof(RectTransform), typeof(VfxStun));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor01;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxStun>().Build();
        }

        void Build()
        {
            _glow = Child(transform, "glow", UiSprites.Soft(), GlowCol, 96f);
            _glow.rectTransform.anchoredPosition = Head;

            _ring = Child(transform, "ring", UiSprites.Circle(), RingCol, 64f);
            _ring.rectTransform.anchoredPosition = Head;
            _ring.rectTransform.localScale = new Vector3(1.18f, 0.38f, 1f);

            const int n = 5;
            _stars = new Image[n];
            _starC = new Color[n];
            _phase = new float[n];
            _spin = new float[n];
            for (int i = 0; i < n; i++)
            {
                _phase[i] = (i / (float)n) * Mathf.PI * 2f;
                _spin[i] = ((i & 1) == 0 ? 240f : -200f) + i * 12f;
                var gold = (i & 1) == 0 ? Gold : Hot;
                _starC[i] = gold;
                var sz = i % 3 == 0 ? 24f : 17f;
                var img = Child(transform, "star" + i, UiSprites.Star(), gold, sz);
                img.rectTransform.anchoredPosition = Head;
                img.color = Color.clear;
                _stars[i] = img;
            }

            _tag = MkText(transform, "Stun", 22, Head + new Vector2(0f, 30f), new Vector2(180f, 36f), Gold);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var fade = u < 0.12f ? u / 0.12f : 1f - Mathf.Clamp01((u - 0.58f) / 0.42f);
            var bob = Mathf.Sin(_age * 9.5f) * 3.5f;

            if (_glow != null)
            {
                _glow.rectTransform.anchoredPosition = Head + new Vector2(0f, bob * 0.5f);
                _glow.transform.localScale = Vector3.one * (0.82f + 0.22f * (0.5f + 0.5f * Mathf.Sin(_age * 7.5f)));
                var c = GlowCol;
                c.a = GlowCol.a * fade;
                _glow.color = c;
            }

            if (_ring != null)
            {
                _ring.rectTransform.anchoredPosition = Head + new Vector2(0f, bob * 0.25f);
                _ring.rectTransform.localEulerAngles = new Vector3(0f, 0f, _age * 55f);
                var c = RingCol;
                c.a = RingCol.a * fade;
                _ring.color = c;
            }

            if (_stars != null)
            {
                const float rx = 36f;
                const float ry = 15f;
                for (int i = 0; i < _stars.Length; i++)
                {
                    var img = _stars[i];
                    if (img == null) continue;
                    var ang = _phase[i] + _age * 7.4f;
                    img.rectTransform.anchoredPosition =
                        Head + new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry + bob);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _spin[i] * _age);
                    var depth = 0.58f + 0.42f * (0.5f + 0.5f * Mathf.Sin(ang));
                    img.transform.localScale = Vector3.one * depth;
                    var twinkle = 0.70f + 0.30f * Mathf.Sin(_age * 16f + _phase[i]);
                    var c = _starC[i];
                    c.a = fade * twinkle;
                    img.color = c;
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = Head + new Vector2(0f, 30f + 16f * u);
            }

            if (_age >= Life) Destroy(gameObject);
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
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }
    }
}
