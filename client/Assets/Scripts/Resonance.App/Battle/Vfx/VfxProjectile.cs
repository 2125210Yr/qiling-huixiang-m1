using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Ranged skill bead: fly from→to, then burst. UGUI Images.
    /// from/to are field anchors 0..1. onHit fires once at impact.
    /// </summary>
    public sealed class VfxProjectile : MonoBehaviour
    {
        const float Fly = 0.22f;
        const float BurstLife = 0.34f;
        const int TrailN = 8;
        const int SparkN = 14;

        Image _glow;
        Image _core;
        Image _hot;
        Image _streak;
        Image _flash;
        Image _ring;
        Image[] _trail;
        Image[] _sparks;
        Vector2[] _sparkDir;
        Vector2 _from;
        Vector2 _to;
        Vector2 _canvas;
        Color _color;
        System.Action _onHit;
        float _age;
        bool _hit;

        public static void Play(Transform parent, Vector2 from, Vector2 to, Color color, System.Action onHit)
        {
            if (parent == null)
            {
                if (onHit != null) onHit();
                return;
            }
            if (color.a < 0.05f) color.a = 1f;
            if ((to - from).sqrMagnitude < 0.0001f)
                to = from + new Vector2(0f, 0.10f);

            var go = new GameObject("vfxProj", typeof(RectTransform), typeof(VfxProjectile));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                if (onHit != null) onHit();
                return;
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var fx = go.GetComponent<VfxProjectile>();
            if (fx == null)
            {
                Object.Destroy(go);
                if (onHit != null) onHit();
                return;
            }
            fx._from = from;
            fx._to = to;
            fx._color = color;
            fx._onHit = onHit;
            fx._canvas = CanvasPx(parent);
            fx.Build();
        }

        void Build()
        {
            var hot = Color.Lerp(_color, Color.white, 0.55f);
            _glow = Quad(transform, "glow", UiSprites.Soft(), Tint(_color, 0.62f), 92f);
            _core = Quad(transform, "core", UiSprites.Circle(), Tint(_color, 1f), 44f);
            _hot = Quad(transform, "hot", UiSprites.Soft(), Tint(hot, 0.95f), 22f);
            _streak = Quad(transform, "streak", UiSprites.Slash(), Tint(_color, 0.88f), 16f);
            _flash = Quad(transform, "flash", UiSprites.Soft(), Tint(hot, 0f), 96f);
            _ring = Quad(transform, "ring", UiSprites.Circle(), Tint(Color.white, 0f), 56f);

            _trail = new Image[TrailN];
            for (int i = 0; i < TrailN; i++)
            {
                var sz = 28f - i * 2.6f;
                _trail[i] = Quad(transform, "trail" + i, UiSprites.Circle(), Tint(_color, 0.52f - i * 0.05f), sz);
            }

            _sparks = new Image[SparkN];
            _sparkDir = new Vector2[SparkN];
            for (int i = 0; i < SparkN; i++)
            {
                var ang = (i / (float)SparkN) * Mathf.PI * 2f;
                _sparkDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (118f + (i & 1) * 42f);
                var star = (i & 1) == 0;
                _sparks[i] = Quad(transform, "spark" + i,
                    star ? UiSprites.Spark() : UiSprites.Circle(),
                    Tint(star ? hot : _color, 0f),
                    star ? 28f : 14f);
            }

            var delta = Px(_to) - Px(_from);
            var ang0 = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Place(_glow, _from, new Vector2(92f, 92f), 0f, Tint(_color, 0.62f));
            Place(_core, _from, new Vector2(44f, 44f), ang0, Tint(_color, 1f));
            Place(_hot, _from, new Vector2(22f, 22f), 0f, Tint(Color.Lerp(_color, Color.white, 0.55f), 0.95f));
            Place(_streak, _from, new Vector2(16f, 16f), ang0, Tint(_color, 0f));
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age < Fly) TickFly();
            else TickBurst();
        }

        void TickFly()
        {
            var u = Mathf.Clamp01(_age / Fly);
            u = u * u * (3f - 2f * u);
            var pos = FlyPos(u);
            var delta = Px(pos) - Px(_from);
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var pulse = 1f + 0.08f * Mathf.Sin(u * Mathf.PI);

            Place(_glow, pos, new Vector2(92f, 92f) * pulse, 0f, Tint(_color, 0.62f));
            Place(_core, pos, new Vector2(44f, 44f) * pulse, ang, Tint(_color, 1f));
            Place(_hot, pos, new Vector2(22f, 22f) * pulse, 0f, Tint(Color.Lerp(_color, Color.white, 0.55f), 0.95f));

            var mid = Vector2.Lerp(_from, pos, 0.55f);
            var dist = Mathf.Max(24f, delta.magnitude * 0.72f);
            Place(_streak, mid, new Vector2(dist, 18f), ang, Tint(_color, 0.86f * (0.35f + 0.65f * u)));

            if (_trail != null)
            {
                var n = Mathf.Min(TrailN, _trail.Length);
                for (int i = 0; i < n; i++)
                {
                    var tu = Mathf.Clamp01(u - (i + 1) * 0.055f);
                    tu = tu * tu * (3f - 2f * tu);
                    var tp = FlyPos(tu);
                    var sz = 28f - i * 2.6f;
                    Place(_trail[i], tp, new Vector2(sz, sz), ang, Tint(_color, (0.50f - i * 0.05f) * u));
                }
            }
        }

        void TickBurst()
        {
            FireHit();
            var u = Mathf.Clamp01((_age - Fly) / BurstLife);
            var pop = 1f - (1f - u) * (1f - u);
            var fade = 1f - u;
            var hot = Color.Lerp(_color, Color.white, 0.55f);

            Place(_glow, _to, new Vector2(92f, 92f), 0f, Tint(_color, 0f));
            Place(_core, _to, new Vector2(44f, 44f), 0f, Tint(_color, 0f));
            Place(_hot, _to, new Vector2(22f, 22f), 0f, Tint(hot, 0f));
            Place(_streak, _to, new Vector2(16f, 16f), 0f, Tint(_color, 0f));
            if (_trail != null)
            {
                var n = Mathf.Min(TrailN, _trail.Length);
                for (int i = 0; i < n; i++)
                    Place(_trail[i], _to, new Vector2(8f, 8f), 0f, Tint(_color, 0f));
            }

            var flash = Mathf.Lerp(110f, 210f, pop);
            Place(_flash, _to, new Vector2(flash, flash), 0f, Tint(hot, 0.94f * fade));
            var ring = Mathf.Lerp(52f, 188f, u);
            Place(_ring, _to, new Vector2(ring, ring), 0f, Tint(Color.Lerp(_color, Color.white, 0.75f), 0.86f * fade));

            if (_sparks != null && _sparkDir != null)
            {
                var n = Mathf.Min(SparkN, Mathf.Min(_sparks.Length, _sparkDir.Length));
                for (int i = 0; i < n; i++)
                {
                    var img = _sparks[i];
                    if (img == null) continue;
                    var p = _to + new Vector2(
                        _sparkDir[i].x / Mathf.Max(1f, _canvas.x),
                        _sparkDir[i].y / Mathf.Max(1f, _canvas.y)) * pop;
                    var sz = ((i & 1) == 0 ? 28f : 14f) * Mathf.Lerp(1.08f, 0.28f, u);
                    Place(img, p, new Vector2(sz, sz), 0f, Tint((i & 1) == 0 ? hot : _color, fade));
                }
            }

            if (_age >= Fly + BurstLife) Destroy(gameObject);
        }

        Vector2 FlyPos(float u)
        {
            var pos = Vector2.LerpUnclamped(_from, _to, u);
            var d = _to - _from;
            var mag = d.magnitude;
            if (mag < 0.001f) return pos;
            var n = new Vector2(-d.y, d.x) / mag;
            if (n.y < 0f) n = -n;
            return pos + n * (Mathf.Sin(u * Mathf.PI) * 0.045f);
        }

        Vector2 Px(Vector2 anchor)
        {
            return new Vector2(anchor.x * _canvas.x, anchor.y * _canvas.y);
        }

        void FireHit()
        {
            if (_hit) return;
            _hit = true;
            var cb = _onHit;
            _onHit = null;
            if (cb != null) cb();
        }

        void OnDestroy()
        {
            FireHit();
        }

        static void Place(Image img, Vector2 anchor, Vector2 size, float ang, Color color)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, ang);
            img.color = color;
        }

        static Color Tint(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static Vector2 CanvasPx(Transform t)
        {
            if (t == null) return new Vector2(756f, 1344f);
            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();
            if (rt == null) return new Vector2(756f, 1344f);
            var r = rt.rect;
            return new Vector2(Mathf.Max(360f, r.width), Mathf.Max(640f, r.height));
        }

        static Image Quad(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            if (parent == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            if (rt == null || img == null)
            {
                Object.Destroy(go);
                return null;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
