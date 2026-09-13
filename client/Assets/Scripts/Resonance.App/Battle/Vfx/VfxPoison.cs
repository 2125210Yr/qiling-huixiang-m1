using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 毒 at feet: green-purple puddle and rising bubbles. 0.7s.
    /// </summary>
    public sealed class VfxPoison : MonoBehaviour
    {
        const float Life = 0.70f;

        struct Bubble
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 From;
            public Vector2 To;
            public float Delay;
            public float BaseA;
            public float Sz0;
            public float Sz1;
            public float Pop;
        }

        Image _stain;
        Image _pool;
        Image _sheen;
        Image _ring;
        Image[] _specks;
        Vector2[] _speckPos;
        float[] _speckA;
        float[] _speckSpin;
        Bubble[] _bubbles;
        Text _tag;
        float _age;
        Vector2 _feet;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfxPoison", typeof(RectTransform), typeof(VfxPoison));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxPoison>().Build();
        }

        void Build()
        {
            _feet = new Vector2(0f, -36f);
            var wood = VisualTokens.ElemWood;
            var dark = VisualTokens.ElemDark;
            var lime = VisualTokens.SlideGreen;
            var stain = Color.Lerp(
                new Color(0.10f, 0.18f, 0.06f, 0.82f),
                new Color(0.16f, 0.06f, 0.20f, 0.82f),
                0.45f);
            var pool = Color.Lerp(wood, dark, 0.42f);
            pool.a = 0.92f;
            var venom = Color.Lerp(lime, dark, 0.38f);
            venom.a = 0.95f;
            var purple = new Color(dark.r, dark.g, dark.b, 0.88f);

            _stain = Child("stain", UiSprites.Soft(), stain, new Vector2(176f, 78f), _feet);
            _pool = Child("pool", UiSprites.Soft(), pool, new Vector2(148f, 62f), _feet + new Vector2(0f, 2f));
            _sheen = Child("sheen", UiSprites.Soft(), venom, new Vector2(58f, 28f), _feet + new Vector2(-10f, 8f));
            _ring = Child("ring", UiSprites.Circle(), purple, new Vector2(72f, 40f), _feet);

            const int nSpeck = 8;
            _specks = new Image[nSpeck];
            _speckPos = new Vector2[nSpeck];
            _speckA = new float[nSpeck];
            _speckSpin = new float[nSpeck];
            for (int i = 0; i < nSpeck; i++)
            {
                var pos = _feet + new Vector2(Random.Range(-52f, 52f), Random.Range(-12f, 10f));
                var sz = Random.Range(4f, 8f);
                var col = (i & 1) == 0
                    ? Color.Lerp(wood, lime, Random.Range(0.20f, 0.70f))
                    : Color.Lerp(dark, lime, Random.Range(0.10f, 0.45f));
                col.a = Random.Range(0.70f, 1f);
                var img = Child("speck", UiSprites.Circle(), col, new Vector2(sz, sz), pos);
                _specks[i] = img;
                _speckPos[i] = pos;
                _speckA[i] = col.a;
                _speckSpin[i] = Random.Range(-80f, 80f);
            }

            const int nBubble = 10;
            _bubbles = new Bubble[nBubble];
            for (int i = 0; i < nBubble; i++)
            {
                var from = _feet + new Vector2(Random.Range(-44f, 44f), Random.Range(-8f, 6f));
                var rise = Random.Range(38f, 78f);
                var sz = Random.Range(8f, 18f);
                var col = (i % 3) == 0
                    ? Color.Lerp(dark, lime, 0.28f)
                    : ((i & 1) == 0 ? Color.Lerp(wood, dark, 0.35f) : venom);
                col.a = 1f;
                var img = Child("bubble", UiSprites.Circle(), col, new Vector2(sz, sz), from);
                img.color = Color.clear;
                _bubbles[i] = new Bubble
                {
                    Rt = img.rectTransform,
                    Img = img,
                    From = from,
                    To = from + new Vector2(Random.Range(-8f, 8f), rise),
                    Delay = i * 0.042f,
                    BaseA = Random.Range(0.72f, 0.96f),
                    Sz0 = sz * 0.28f,
                    Sz1 = sz,
                    Pop = Random.Range(0.62f, 0.86f)
                };
            }

            _tag = MkText(transform, "Poison", 26, new Vector2(0f, -8f), new Vector2(80f, 40f),
                Color.Lerp(lime, dark, 0.22f));
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var ease = 1f - (1f - u) * (1f - u);
            var fade = u < 0.18f ? u / 0.18f : 1f - Mathf.Clamp01((u - 0.58f) / 0.42f);

            if (_stain != null)
            {
                var s = Mathf.Lerp(0.38f, 1.55f, ease);
                _stain.rectTransform.localScale = new Vector3(s, s * 0.72f, 1f);
                var c = _stain.color;
                c.a = 0.82f * (1f - u * u);
                _stain.color = c;
            }
            if (_pool != null)
            {
                var wobble = 1f + 0.04f * Mathf.Sin(_age * 14f);
                var s = Mathf.Lerp(0.42f, 1.48f, ease) * wobble;
                _pool.rectTransform.localScale = new Vector3(s, s * 0.78f, 1f);
                var c = _pool.color;
                c.a = 0.90f * (1f - u) * (1f - u * 0.22f);
                _pool.color = c;
            }
            if (_sheen != null)
            {
                _sheen.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.35f, Mathf.Clamp01(u * 2.2f));
                var c = _sheen.color;
                c.a = 0.88f * Mathf.Sin(Mathf.Clamp01(u * 1.35f) * Mathf.PI);
                _sheen.color = c;
            }
            if (_ring != null)
            {
                var s = Mathf.Lerp(0.50f, 2.05f, ease);
                _ring.rectTransform.localScale = new Vector3(s, s * 0.62f, 1f);
                var c = _ring.color;
                c.a = 0.78f * Mathf.Sin(Mathf.Clamp01(u * 1.20f) * Mathf.PI);
                _ring.color = c;
            }

            if (_specks != null)
            {
                for (int i = 0; i < _specks.Length; i++)
                {
                    var img = _specks[i];
                    if (img == null) continue;
                    var drift = new Vector2(Mathf.Sin(_age * 7f + i) * 4f, Mathf.Sin(_age * 5f + i * 1.7f) * 3f);
                    img.rectTransform.anchoredPosition = _speckPos[i] + drift;
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _speckSpin[i] * u);
                    var c = img.color;
                    c.a = _speckA[i] * fade;
                    img.color = c;
                }
            }

            if (_bubbles != null)
            {
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    var b = _bubbles[i];
                    if (b.Rt == null || b.Img == null) continue;
                    if (_age < b.Delay)
                    {
                        var hide = b.Img.color;
                        hide.a = 0f;
                        b.Img.color = hide;
                        continue;
                    }
                    var t = Mathf.Clamp01((_age - b.Delay) / Mathf.Max(0.05f, Life - b.Delay));
                    var tu = 1f - (1f - t) * (1f - t);
                    b.Rt.anchoredPosition = Vector2.LerpUnclamped(b.From, b.To, tu);
                    var popped = t >= b.Pop;
                    var sz = popped
                        ? Mathf.Lerp(b.Sz1, b.Sz1 * 1.55f, (t - b.Pop) / Mathf.Max(0.04f, 1f - b.Pop))
                        : Mathf.Lerp(b.Sz0, b.Sz1, tu);
                    b.Rt.sizeDelta = new Vector2(sz, sz);
                    var a = popped
                        ? b.BaseA * (1f - (t - b.Pop) / Mathf.Max(0.04f, 1f - b.Pop))
                        : b.BaseA * (t < 0.14f ? t / 0.14f : 1f);
                    var ic = b.Img.color;
                    ic.a = Mathf.Clamp01(a);
                    b.Img.color = ic;
                }
            }

            if (_tag != null)
            {
                var rise = 1f - (1f - u) * (1f - u);
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, -8f + 28f * rise);
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(1.18f, 0.92f, u);
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
            }

            if (_age >= Life) Destroy(gameObject);
        }

        Image Child(string name, Sprite sprite, Color color, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
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
