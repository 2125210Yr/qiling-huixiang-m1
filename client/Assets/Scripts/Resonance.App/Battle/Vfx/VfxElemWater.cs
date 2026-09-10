using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Water skill hit: cyan crescents and falling droplets at an anchor.
    /// </summary>
    public sealed class VfxElemWater : MonoBehaviour
    {
        struct Crescent
        {
            public RectTransform Rt;
            public Image Img;
            public float Ang0;
            public float Ang1;
            public float Len0;
            public float Len1;
            public float Thick0;
            public float Thick1;
            public Color Color;
            public float Delay;
        }

        struct Drop
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Pos;
            public Vector2 Vel;
            public Color Color;
            public float Delay;
            public float Spin;
        }

        Crescent[] _arcs;
        Drop[] _drops;
        Image _splash;
        Image _ring;
        Image _foam;
        float _life;
        float _age;
        bool _fever;

        public static void Play(Transform parent, Vector2 anchor, bool fever)
        {
            if (parent == null) return;
            var go = new GameObject("vfxWater", typeof(RectTransform), typeof(VfxElemWater));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxElemWater>().Build(fever);
        }

        void Build(bool fever)
        {
            _fever = fever;
            _life = fever ? 0.70f : 0.50f;

            var cyan = Color.Lerp(VisualTokens.ElemWater, VisualTokens.Aurora, 0.40f);
            var foam = VisualTokens.IceCore;
            var mist = VisualTokens.IceShard;
            var gold = VisualTokens.FeverGold;

            var splashSize = fever ? 340f : 240f;
            _splash = Piece("splash", UiSprites.Soft(),
                new Color(cyan.r, cyan.g, cyan.b, fever ? 0.88f : 0.72f), splashSize);
            _ring = Piece("ring", UiSprites.Circle(),
                new Color(foam.r, foam.g, foam.b, 0.92f), splashSize * 0.40f);
            _foam = Piece("foam", UiSprites.Soft(),
                new Color(mist.r, mist.g, mist.b, 0.55f), splashSize * 0.22f);

            var nArc = fever ? 8 : 5;
            _arcs = new Crescent[nArc];
            for (int i = 0; i < nArc; i++)
            {
                var t = i / (float)Mathf.Max(1, nArc - 1);
                var ang = -32f + t * 64f + (fever && (i & 1) == 1 ? 16f : 0f);
                var goldish = fever && i % 3 == 0;
                var col = goldish
                    ? Color.Lerp(cyan, gold, 0.42f)
                    : (i & 1) == 0 ? cyan : Color.Lerp(cyan, foam, 0.55f);
                col.a = 0.96f;
                var len = (fever ? 300f : 220f) * (0.78f + t * 0.32f);
                var thick = (fever ? 72f : 52f) * ((i & 1) == 0 ? 1f : 0.62f);
                var img = Piece("crescent" + i, UiSprites.Slash(), col, 1f);
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(len * 0.50f, thick);
                rt.localEulerAngles = new Vector3(0f, 0f, ang);
                var nrm = new Vector2(-Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad));
                rt.anchoredPosition = nrm * (fever ? 16f : 10f);
                _arcs[i] = new Crescent
                {
                    Rt = rt,
                    Img = img,
                    Ang0 = ang - 16f,
                    Ang1 = ang + 24f,
                    Len0 = len * 0.42f,
                    Len1 = len,
                    Thick0 = thick * 1.18f,
                    Thick1 = thick * 0.26f,
                    Color = col,
                    Delay = i * 0.032f
                };
            }

            var nDrop = fever ? 20 : 12;
            _drops = new Drop[nDrop];
            for (int i = 0; i < nDrop; i++)
            {
                var ang = (i / (float)nDrop) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
                var spd = (fever ? 220f : 148f) * Random.Range(0.55f, 1.12f);
                var sz = (fever ? 18f : 12f) * Random.Range(0.70f, 1.35f);
                var tear = i % 3 != 0;
                var col = (fever && i % 5 == 0)
                    ? Color.Lerp(cyan, gold, 0.35f)
                    : (i & 1) == 0 ? Color.Lerp(cyan, foam, 0.40f) : mist;
                col.a = 1f;
                var img = Piece("drop" + i, UiSprites.Circle(), col, sz);
                if (tear) img.rectTransform.sizeDelta = new Vector2(sz * 0.52f, sz * 1.38f);
                _drops[i] = new Drop
                {
                    Rt = img.rectTransform,
                    Img = img,
                    Pos = Vector2.zero,
                    Vel = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd * 0.52f + 78f),
                    Color = col,
                    Delay = Random.Range(0f, 0.08f),
                    Spin = Random.Range(-180f, 180f)
                };
            }
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _age += dt;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var fade = 1f - u;

            if (_splash != null)
            {
                var s = Mathf.Lerp(0.34f, _fever ? 2.20f : 1.65f, 1f - (1f - u) * (1f - u));
                _splash.transform.localScale = Vector3.one * s;
                var c = _splash.color;
                c.a = 0.74f * fade;
                _splash.color = c;
            }
            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * (0.42f + u * 2.15f);
                var c = _ring.color;
                c.a = 0.82f * fade * fade;
                _ring.color = c;
            }
            if (_foam != null)
            {
                _foam.transform.localScale = Vector3.one * (0.70f + u * 1.55f);
                var c = _foam.color;
                c.a = 0.50f * fade;
                _foam.color = c;
            }

            if (_arcs != null)
            {
                for (int i = 0; i < _arcs.Length; i++)
                {
                    var arc = _arcs[i];
                    if (arc.Rt == null || arc.Img == null) continue;
                    var k = Mathf.Clamp01((_age - arc.Delay) / Mathf.Max(0.05f, _life - arc.Delay));
                    var ease = k * k * (3f - 2f * k);
                    arc.Rt.sizeDelta = new Vector2(
                        Mathf.Lerp(arc.Len0, arc.Len1, ease),
                        Mathf.Lerp(arc.Thick0, arc.Thick1, ease));
                    arc.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(arc.Ang0, arc.Ang1, ease));
                    var c = arc.Color;
                    c.a = arc.Color.a * (1f - k * k);
                    arc.Img.color = c;
                }
            }

            if (_drops != null)
            {
                for (int i = 0; i < _drops.Length; i++)
                {
                    var d = _drops[i];
                    if (d.Rt == null || d.Img == null || _age < d.Delay) continue;
                    d.Vel += new Vector2(0f, -420f * dt);
                    d.Pos += d.Vel * dt;
                    d.Rt.anchoredPosition = d.Pos;
                    var k = Mathf.Clamp01((_age - d.Delay) / Mathf.Max(0.05f, _life - d.Delay));
                    d.Rt.localScale = new Vector3(1.05f - k * 0.35f, 1.08f - k * 0.58f, 1f);
                    d.Rt.localEulerAngles = new Vector3(0f, 0f, d.Spin * k);
                    var c = d.Color;
                    c.a = d.Color.a * (1f - k * k);
                    d.Img.color = c;
                    _drops[i] = d;
                }
            }

            if (_age >= _life) Destroy(gameObject);
        }

        Image Piece(string name, Sprite sprite, Color color, float size)
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
    }
}
