using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Wood hit: green poison pool (潭) and leaf burst. Not realistic vines.
    /// </summary>
    public sealed class VfxElemWood : MonoBehaviour
    {
        struct Bit
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Vel;
            public float Spin;
            public float BaseA;
            public float W0, H0, W1, H1;
            public float Grav;
            public float Delay;
        }

        static Sprite _leaf;

        Image _stain;
        Image _pool;
        Image _ring;
        Image _core;
        Bit[] _bits;
        float _life;
        float _age;
        bool _fever;

        public static void Play(Transform parent, Vector2 anchor, bool fever)
        {
            if (parent == null) return;
            var go = new GameObject("vfxWood", typeof(RectTransform), typeof(VfxElemWood));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxElemWood>().Build(fever);
        }

        void Build(bool fever)
        {
            _fever = fever;
            _life = fever ? 0.68f : 0.48f;
            var size = fever ? 380f : 240f;
            var wood = VisualTokens.ElemWood;
            var lime = VisualTokens.SlideGreen;
            var stain = new Color(0.10f, 0.32f, 0.04f, 0.78f);
            var venom = new Color(0.52f, 0.86f, 0.12f, 1f);

            _stain = Child("stain", UiSprites.Soft(), stain, new Vector2(size * 1.22f, size * 0.62f));
            _pool = Child("pool", UiSprites.Soft(), new Color(wood.r, wood.g, wood.b, 0.90f),
                new Vector2(size * 1.05f, size * 0.68f));
            _ring = Child("ring", UiSprites.Circle(), new Color(lime.r, lime.g, lime.b, 0.92f),
                new Vector2(size * 0.46f, size * 0.30f));
            _core = Child("core", UiSprites.Soft(), new Color(0.78f, 1f, 0.42f, 0.95f),
                new Vector2(size * 0.36f, size * 0.36f));

            var nLeaf = fever ? 16 : 9;
            var nDrop = fever ? 12 : 6;
            var nMist = fever ? 6 : 3;
            var nLine = fever ? 10 : 0;
            _bits = new Bit[nLeaf + nDrop + nMist + nLine];
            var i = 0;

            for (int k = 0; k < nLeaf; k++, i++)
            {
                var ang = (k / (float)nLeaf) * Mathf.PI * 2f + Random.Range(-0.22f, 0.22f);
                var dist = (fever ? 156f : 108f) * Random.Range(0.72f, 1.18f);
                var w = Random.Range(22f, 38f) * (fever ? 1.15f : 1f);
                var h = w * Random.Range(1.45f, 1.85f);
                var col = Color.Lerp(wood, lime, Random.Range(0.12f, 0.70f));
                var img = Child("leaf", Leaf(), col, new Vector2(w, h));
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, ang * Mathf.Rad2Deg + 90f);
                _bits[i] = new Bit
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) + 0.38f) * dist,
                    Spin = Random.Range(-420f, 420f),
                    BaseA = Random.Range(0.82f, 1f),
                    W0 = w * 0.35f,
                    H0 = h * 0.35f,
                    W1 = w,
                    H1 = h,
                    Grav = 220f,
                    Delay = Random.Range(0f, 0.06f)
                };
            }

            for (int k = 0; k < nDrop; k++, i++)
            {
                var ang = (k / (float)nDrop) * Mathf.PI * 2f + 0.41f + Random.Range(-0.20f, 0.20f);
                var dist = (fever ? 96f : 64f) * Random.Range(0.60f, 1.10f);
                var sz = Random.Range(8f, 16f);
                var img = Child("drop", UiSprites.Circle(), Color.Lerp(venom, lime, Random.Range(0f, 0.45f)),
                    new Vector2(sz, sz * 1.35f));
                _bits[i] = new Bit
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Vel = new Vector2(Mathf.Cos(ang) * 0.70f, Mathf.Sin(ang) + 0.55f) * dist,
                    Spin = 0f,
                    BaseA = 0.95f,
                    W0 = sz * 0.40f,
                    H0 = sz * 0.55f,
                    W1 = sz,
                    H1 = sz * 1.35f,
                    Grav = 90f,
                    Delay = Random.Range(0f, 0.08f)
                };
            }

            for (int k = 0; k < nMist; k++, i++)
            {
                var ang = (k / (float)Mathf.Max(1, nMist)) * Mathf.PI * 2f;
                var sz = (fever ? 90f : 58f) * Random.Range(0.70f, 1.20f);
                var img = Child("mist", UiSprites.Soft(), new Color(wood.r, wood.g, 0.08f, 0.45f),
                    new Vector2(sz, sz * 0.72f));
                _bits[i] = new Bit
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.55f) * (fever ? 48f : 28f),
                    Spin = Random.Range(-20f, 20f),
                    BaseA = 0.42f,
                    W0 = sz * 0.40f,
                    H0 = sz * 0.28f,
                    W1 = sz * 1.55f,
                    H1 = sz * 1.05f,
                    Grav = 8f,
                    Delay = 0f
                };
            }

            for (int k = 0; k < nLine; k++, i++)
            {
                var ang = (k / (float)nLine) * 360f + Random.Range(-8f, 8f);
                var len = Random.Range(90f, 160f);
                var img = Child("line", UiSprites.Slash(), new Color(lime.r, lime.g, lime.b, 0.55f),
                    new Vector2(len, 10f));
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, ang);
                var rad = ang * Mathf.Deg2Rad;
                _bits[i] = new Bit
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Vel = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 180f,
                    Spin = 0f,
                    BaseA = 0.55f,
                    W0 = len * 0.35f,
                    H0 = 8f,
                    W1 = len * 1.25f,
                    H1 = 5f,
                    Grav = 0f,
                    Delay = 0f
                };
            }
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _age += dt;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var ease = 1f - (1f - u) * (1f - u);

            if (_stain != null)
            {
                var s = Mathf.Lerp(0.38f, _fever ? 2.15f : 1.70f, ease);
                _stain.transform.localScale = new Vector3(s, s * 0.72f, 1f);
                var c = _stain.color;
                c.a = 0.78f * (1f - u * u);
                _stain.color = c;
            }
            if (_pool != null)
            {
                var s = Mathf.Lerp(0.42f, _fever ? 2.35f : 1.85f, ease);
                _pool.transform.localScale = new Vector3(s, s * 0.78f, 1f);
                var c = _pool.color;
                c.a = 0.88f * (1f - u) * (1f - u * 0.28f);
                _pool.color = c;
            }
            if (_ring != null)
            {
                var s = Mathf.Lerp(0.55f, _fever ? 2.80f : 2.20f, ease);
                _ring.transform.localScale = new Vector3(s, s * 0.72f, 1f);
                var c = _ring.color;
                c.a = 0.78f * Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI);
                _ring.color = c;
            }
            if (_core != null)
            {
                _core.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.60f, Mathf.Clamp01(u * 2.4f));
                var c = _core.color;
                c.a = 0.95f * Mathf.Clamp01(1f - u * 2.6f);
                _core.color = c;
            }

            if (_bits != null)
            {
                for (int i = 0; i < _bits.Length; i++)
                {
                    var b = _bits[i];
                    if (b.Rt == null || b.Img == null) continue;
                    var t = Mathf.Clamp01((_age - b.Delay) / Mathf.Max(0.05f, _life - b.Delay));
                    if (_age < b.Delay)
                    {
                        var hide = b.Img.color;
                        hide.a = 0f;
                        b.Img.color = hide;
                        continue;
                    }
                    var tu = 1f - (1f - t) * (1f - t);
                    var pos = b.Vel * tu;
                    pos.y -= b.Grav * t * t * 0.5f;
                    b.Rt.anchoredPosition = pos;
                    if (b.Spin != 0f)
                        b.Rt.localEulerAngles = new Vector3(0f, 0f, b.Rt.localEulerAngles.z + b.Spin * dt);
                    b.Rt.sizeDelta = new Vector2(Mathf.Lerp(b.W0, b.W1, tu), Mathf.Lerp(b.H0, b.H1, tu));
                    var fade = t < 0.18f ? t / 0.18f : 1f - (t - 0.18f) / 0.82f;
                    var ic = b.Img.color;
                    ic.a = b.BaseA * Mathf.Clamp01(fade);
                    b.Img.color = ic;
                }
            }

            if (_age >= _life) Destroy(gameObject);
        }

        Image Child(string name, Sprite sprite, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Sprite Leaf()
        {
            if (_leaf != null) return _leaf;
            const int w = 32;
            const int h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            var cx = (w - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                var t = (y + 0.5f) / h;
                var half = (w * 0.46f) * Mathf.Sin(t * Mathf.PI) * (0.78f + 0.22f * t);
                for (int x = 0; x < w; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - cx);
                    var a = Mathf.Clamp01(half + 0.6f - dx);
                    var rib = Mathf.Clamp01(1.15f - dx * 1.7f) * Mathf.Clamp01(t * 1.4f) * 0.32f;
                    a = Mathf.Max(a * 0.92f, a * 0.80f + rib);
                    a *= a;
                    if (t < 0.08f) a *= t / 0.08f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _leaf = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.12f), 100f);
            return _leaf;
        }
    }
}
