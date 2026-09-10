using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 嘲讽: red mark over the head. 0.55s. No English Taunt.
    /// </summary>
    public sealed class VfxTaunt : MonoBehaviour
    {
        const float Life = 0.55f;

        static readonly Color Ink = VisualTokens.StarEvolved;
        static readonly Color Deep = new Color(0.38f, 0.04f, 0.06f, 0.92f);
        static readonly Color Hot = new Color(1f, 0.72f, 0.48f, 1f);
        static readonly Color GlowCol = new Color(1f, 0.22f, 0.10f, 0.42f);
        static readonly Color RingCol = new Color(1f, 0.28f, 0.12f, 0.88f);
        static readonly Vector2 Head = new Vector2(0f, 78f);

        Image _glow;
        Image _ring;
        Image _core;
        Image _barH;
        Image _barV;
        Image[] _veins;
        float[] _veinAng;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfxTaunt", typeof(RectTransform), typeof(VfxTaunt));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxTaunt>().Build();
        }

        void Build()
        {
            _glow = Child(transform, "glow", UiSprites.Soft(), GlowCol, 104f);
            _glow.rectTransform.anchoredPosition = Head;
            _glow.transform.localScale = Vector3.one * 0.52f;

            _ring = Child(transform, "ring", UiSprites.Circle(), RingCol, 58f);
            _ring.rectTransform.anchoredPosition = Head;
            _ring.transform.localScale = Vector3.one * 1.55f;

            _core = Child(transform, "core", UiSprites.Soft(), WithA(Hot, 0.96f), 26f);
            _core.rectTransform.anchoredPosition = Head;
            _core.transform.localScale = Vector3.one * 1.35f;

            _barH = Child(transform, "barH", UiSprites.Slash(), WithA(Ink, 0.98f), 1f);
            _barH.rectTransform.sizeDelta = new Vector2(52f, 7f);
            _barH.rectTransform.anchoredPosition = Head;
            _barH.transform.localScale = new Vector3(0.22f, 1.20f, 1f);

            _barV = Child(transform, "barV", UiSprites.Slash(), WithA(Hot, 0.98f), 1f);
            _barV.rectTransform.sizeDelta = new Vector2(7f, 52f);
            _barV.rectTransform.anchoredPosition = Head;
            _barV.transform.localScale = new Vector3(1.20f, 0.22f, 1f);

            const int n = 6;
            _veins = new Image[n];
            _veinAng = new float[n];
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * 360f + 18f;
                _veinAng[i] = ang;
                var len = (i & 1) == 0 ? 30f : 22f;
                var col = (i & 1) == 0 ? Ink : Color.Lerp(Ink, Deep, 0.35f);
                col.a = 0.98f;
                var img = Child(transform, "vein" + i, UiSprites.Slash(), col, 1f);
                img.rectTransform.sizeDelta = new Vector2(len, (i & 1) == 0 ? 7f : 5f);
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, ang);
                img.rectTransform.anchoredPosition = Head;
                img.transform.localScale = new Vector3(0.18f, 1.15f, 1f);
                _veins[i] = img;
            }

            _tag = MkText(transform, "嘲讽", 22, Head + new Vector2(0f, 30f), new Vector2(180f, 36f), Ink);
            _tag.transform.localScale = Vector3.one * 1.35f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.18f)) * (1f - Mathf.Clamp01(u / 0.18f));
            var ease = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.52f) / 0.48f);
            var bob = Mathf.Sin(_age * 14f) * 2.6f;
            var pulse = 0.5f + 0.5f * Mathf.Sin(_age * 18f);

            if (_glow != null)
            {
                _glow.rectTransform.anchoredPosition = Head + new Vector2(0f, bob * 0.35f);
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.52f, 1.42f, ease);
                SetA(_glow, GlowCol.a * fade * (0.82f + 0.18f * pulse));
            }

            if (_ring != null)
            {
                _ring.rectTransform.anchoredPosition = Head + new Vector2(0f, bob * 0.20f);
                var s = Mathf.Lerp(1.55f, 1.02f, slam) * (1f + 0.06f * pulse);
                _ring.rectTransform.localScale = new Vector3(s, s * 0.92f, 1f);
                SetA(_ring, RingCol.a * fade);
            }

            if (_core != null)
            {
                _core.rectTransform.anchoredPosition = Head;
                _core.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.72f, u);
                var c = Color.Lerp(Hot, Ink, slam);
                c.a = 0.96f * (1f - u * u);
                _core.color = c;
            }

            if (_barH != null)
            {
                _barH.rectTransform.anchoredPosition = Head;
                _barH.transform.localScale = new Vector3(Mathf.Lerp(0.22f, 1.04f, slam), 1f, 1f);
                SetA(_barH, 0.98f * fade);
            }
            if (_barV != null)
            {
                _barV.rectTransform.anchoredPosition = Head;
                _barV.transform.localScale = new Vector3(1f, Mathf.Lerp(0.22f, 1.04f, slam), 1f);
                SetA(_barV, 0.98f * fade);
            }

            if (_veins != null)
            {
                for (int i = 0; i < _veins.Length; i++)
                {
                    var img = _veins[i];
                    if (img == null) continue;
                    var rad = Mathf.Lerp(8f, 22f, slam);
                    var ang = (_veinAng[i] + _age * ((i & 1) == 0 ? 28f : -22f)) * Mathf.Deg2Rad;
                    img.rectTransform.anchoredPosition =
                        Head + new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad + bob * 0.15f);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _veinAng[i] + bob * 2f);
                    img.transform.localScale = new Vector3(Mathf.Lerp(0.18f, 1.08f, slam), 1f, 1f);
                    var c = img.color;
                    c.a = 0.98f * fade;
                    img.color = c;
                }
            }

            if (_tag != null)
            {
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, slam);
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = Head + new Vector2(0f, 30f + 16f * ease);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static void SetA(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
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
