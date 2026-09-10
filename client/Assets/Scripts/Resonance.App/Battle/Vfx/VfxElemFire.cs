using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Fire-element skill FX: orange-red rings and ember sparks, UGUI Images.
    /// </summary>
    public sealed class VfxElemFire : MonoBehaviour
    {
        Image _glow;
        Image _hot;
        float _glowA;
        float _hotA;
        Ring[] _rings;
        Ember[] _embers;
        float _life;
        float _age;

        struct Ring
        {
            public RectTransform Rt;
            public Image Img;
            public float Delay;
            public float EndScale;
            public float StartA;
            public float Spin;
        }

        struct Ember
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Dir;
            public float Delay;
            public float Spin;
            public float StartA;
            public float StartSize;
        }

        public static void Play(Transform parent, Vector2 anchor, bool fever)
        {
            if (parent == null) return;
            var size = fever ? 360f : 240f;
            var go = new GameObject("vfxFire", typeof(RectTransform), typeof(VfxElemFire));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxElemFire>().Build(fever, size);
        }

        void Build(bool fever, float size)
        {
            _life = fever ? 0.70f : 0.50f;
            var fire = VisualTokens.ElemFire;
            var ember = VisualTokens.Ember;
            var hot = Color.Lerp(ember, VisualTokens.YellowValue, 0.42f);
            if (fever) hot = Color.Lerp(hot, VisualTokens.FeverGold, 0.55f);

            _glow = Pic(transform, "glow", UiSprites.Soft(), WithA(Color.Lerp(fire, ember, 0.35f), fever ? 0.62f : 0.48f), size);
            _hot = Pic(transform, "hot", UiSprites.Soft(), WithA(hot, 0.95f), size * (fever ? 0.38f : 0.32f));
            _glowA = _glow.color.a;
            _hotA = _hot.color.a;

            var nRing = fever ? 6 : 4;
            _rings = new Ring[nRing];
            for (int i = 0; i < nRing; i++)
            {
                var mix = i / Mathf.Max(1f, nRing - 1f);
                var col = Color.Lerp(fire, ember, 0.25f + mix * 0.65f);
                if (fever && i == nRing - 1)
                    col = Color.Lerp(col, VisualTokens.FeverGold, 0.55f);
                var img = Pic(transform, "ring" + i, UiSprites.Circle(), WithA(col, 0.82f - mix * 0.22f), size * 0.52f);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = (i & 1) == 0;
                img.fillAmount = 0.72f + mix * 0.22f;
                _rings[i] = new Ring
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Delay = i * (fever ? 0.055f : 0.048f),
                    EndScale = (fever ? 2.55f : 2.05f) + i * 0.18f,
                    StartA = img.color.a,
                    Spin = ((i & 1) == 0 ? -1f : 1f) * (70f + i * 28f)
                };
            }

            var nEmber = fever ? 20 : 14;
            _embers = new Ember[nEmber];
            for (int i = 0; i < nEmber; i++)
            {
                var ang = (i / (float)nEmber) * Mathf.PI * 2f + Random.Range(-0.22f, 0.22f);
                var dist = (fever ? 148f : 108f) * Random.Range(0.72f, 1.18f);
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) + 0.42f) * dist;
                var spark = (i % 3) == 0;
                var spr = spark ? UiSprites.Spark() : UiSprites.Circle();
                var sz = spark ? (fever ? 22f : 16f) : (fever ? 14f : 10f);
                sz *= Random.Range(0.85f, 1.2f);
                var tint = (i % 4) == 0
                    ? VisualTokens.StarEvolved
                    : Color.Lerp(ember, VisualTokens.YellowValue, Random.value * 0.7f);
                if (fever && (i % 5) == 0) tint = Color.Lerp(tint, VisualTokens.FeverGold, 0.5f);
                var img = Pic(transform, "ember" + i, spr, WithA(tint, 0.95f), sz);
                _embers[i] = new Ember
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Dir = dir,
                    Delay = Random.Range(0f, fever ? 0.10f : 0.06f),
                    Spin = Random.Range(-220f, 220f),
                    StartA = img.color.a,
                    StartSize = sz
                };
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var ease = 1f - (1f - u) * (1f - u);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.85f, ease);
                var c = _glow.color;
                c.a = _glowA * (1f - u);
                _glow.color = c;
            }
            if (_hot != null)
            {
                _hot.transform.localScale = Vector3.one * Mathf.Lerp(0.90f, 0.18f, u);
                var c = _hot.color;
                c.a = _hotA * (1f - u * u);
                _hot.color = c;
            }

            if (_rings != null)
            {
                for (int i = 0; i < _rings.Length; i++)
                {
                    var r = _rings[i];
                    if (r.Img == null || r.Rt == null) continue;
                    var t = Mathf.Clamp01((_age - r.Delay) / Mathf.Max(0.08f, _life - r.Delay));
                    var e = 1f - (1f - t) * (1f - t);
                    r.Rt.localScale = Vector3.one * Mathf.Lerp(0.20f, r.EndScale, e);
                    r.Rt.localEulerAngles = new Vector3(0f, 0f, r.Rt.localEulerAngles.z + r.Spin * Time.unscaledDeltaTime);
                    var c = r.Img.color;
                    c.a = r.StartA * (1f - t);
                    r.Img.color = c;
                }
            }

            if (_embers != null)
            {
                for (int i = 0; i < _embers.Length; i++)
                {
                    var e = _embers[i];
                    if (e.Img == null || e.Rt == null) continue;
                    var t = Mathf.Clamp01((_age - e.Delay) / Mathf.Max(0.08f, _life - e.Delay));
                    var fly = t * t * (3f - 2f * t);
                    e.Rt.anchoredPosition = e.Dir * fly;
                    var s = Mathf.Lerp(1.08f, 0.22f, t);
                    e.Rt.sizeDelta = new Vector2(e.StartSize, e.StartSize);
                    e.Rt.localScale = Vector3.one * s;
                    e.Rt.localEulerAngles = new Vector3(0f, 0f, e.Rt.localEulerAngles.z + e.Spin * Time.unscaledDeltaTime);
                    var c = e.Img.color;
                    c.a = e.StartA * (1f - t);
                    e.Img.color = c;
                }
            }

            if (_age >= _life) Destroy(gameObject);
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
    }
}
