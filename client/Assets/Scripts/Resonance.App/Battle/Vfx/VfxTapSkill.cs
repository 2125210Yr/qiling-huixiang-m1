using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// TAP field punch: caster → target streak + impact ring. Smooth unscaledTime.
    /// No SHOWTIME, no Live2D.
    /// </summary>
    public sealed class VfxTapSkill : MonoBehaviour
    {
        const float Life = 0.40f;

        static readonly float[] KnuckleT = { -0.08f, 0.28f, 0.72f, 1f, 1f };
        static readonly float[] KnucklePx = { 48f, 78f, 108f, 136f, 52f };
        static readonly float[] KnuckleA = { 0.72f, 0.95f, 1f, 1f, 0.28f };
        static readonly float[] StreakA = { 0.20f, 0.92f, 0.95f, 0.48f, 0f };
        static readonly float[] FlashA = { 0f, 0.15f, 0.62f, 1f, 0.32f };
        static readonly float[] RingA = { 0f, 0.08f, 0.35f, 0.90f, 0.30f };

        Image _knuckle;
        Image _streak;
        Image _twin;
        Image _flash;
        Image _ring;
        Image[] _chips;
        Vector2 _from;
        Vector2 _to;
        Vector2 _canvas;
        float _ang;
        Color _color;
        float _age;
        bool _burst;

        public static void Play(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            if (parent == null) return;
            if (color.a < 0.05f) color.a = 1f;
            if ((to - from).sqrMagnitude < 0.0001f)
                to = from + new Vector2(0f, 0.10f);

            var go = new GameObject("tapSkill", typeof(RectTransform), typeof(VfxTapSkill));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                return;
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var fx = go.GetComponent<VfxTapSkill>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx._from = from;
            fx._to = to;
            fx._color = color;
            fx._canvas = CanvasPx(parent);
            var a = new Vector2(from.x * fx._canvas.x, from.y * fx._canvas.y);
            var b = new Vector2(to.x * fx._canvas.x, to.y * fx._canvas.y);
            var delta = b - a;
            fx._ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            fx.Build();
            fx.Apply(0f);
        }

        void Build()
        {
            var white = new Color(1f, 1f, 1f, 0.95f);
            _knuckle = Quad(transform, "knuckle", UiSprites.Soft(), _color, 48f);
            // No connecting streak — 06b still read as a slash when a long dash was drawn.
            // Isolated tap VFX is still missing in P0; punch + ring only.
            _streak = null;
            _twin = null;
            _flash = Quad(transform, "flash", UiSprites.Soft(), white, 72f);
            _ring = Quad(transform, "ring", UiSprites.Circle(), white, 40f);
            _chips = new Image[5];
            for (int i = 0; i < _chips.Length; i++)
                _chips[i] = Quad(transform, "chip" + i,
                    (i & 1) == 0 ? UiSprites.Spark() : UiSprites.Circle(), _color, 14f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age >= Life)
            {
                Destroy(gameObject);
                return;
            }
            Apply(Mathf.Clamp01(_age / Life));
        }

        void Apply(float u)
        {
            var t = Sample(KnuckleT, u);
            var pos = Vector2.LerpUnclamped(_from, _to, t);
            var knuckle = Sample(KnucklePx, u);
            Place(_knuckle, pos, new Vector2(knuckle * 1.18f, knuckle * 0.70f), _ang, Tint(_color, Sample(KnuckleA, u)));

            var mid = Vector2.Lerp(_from, pos, 0.55f);
            var a = new Vector2(_from.x * _canvas.x, _from.y * _canvas.y);
            var b = new Vector2(pos.x * _canvas.x, pos.y * _canvas.y);
            var dist = Mathf.Max(24f, (b - a).magnitude * 0.72f);
            // Soft dash only — keep thin so Tap never reads as Slide slash ribbon.
            var thick = Mathf.Lerp(14f, 8f, u);
            Place(_streak, mid, new Vector2(dist, thick), _ang, Tint(_color, Sample(StreakA, u) * 0.55f));
            Place(_twin, mid, new Vector2(dist * 0.55f, thick * 0.40f), _ang - 18f, Tint(_color, Sample(StreakA, u) * 0.35f));

            var burst = Color.Lerp(_color, Color.white, 0.55f);
            var flash = Mathf.Lerp(96f, 210f, u);
            Place(_flash, _to, new Vector2(flash, flash), 0f, Tint(burst, Sample(FlashA, u)));
            var ring = Mathf.Lerp(56f, 196f, u);
            Place(_ring, _to, new Vector2(ring, ring), 0f, Tint(Color.Lerp(_color, Color.white, 0.75f), Sample(RingA, u)));

            if (_chips != null)
            {
                var pop = 1f - (1f - u) * (1f - u);
                for (int i = 0; i < _chips.Length; i++)
                {
                    var ang = (_ang + 90f + i * 72f) * Mathf.Deg2Rad;
                    var off = Mathf.Lerp(8f, 46f, pop);
                    var chip = _to + new Vector2(
                        Mathf.Cos(ang) * off / Mathf.Max(1f, _canvas.x),
                        Mathf.Sin(ang) * off / Mathf.Max(1f, _canvas.y));
                    var sz = ((i & 1) == 0 ? 22f : 12f) * Mathf.Lerp(1.1f, 0.35f, u);
                    Place(_chips[i], chip, new Vector2(sz, sz), _ang + i * 36f,
                        Tint(_color, Mathf.Clamp01((u - 0.42f) / 0.58f) * (1f - u)));
                }
            }

            if (!_burst && u >= 0.58f)
            {
                _burst = true;
                var host = new GameObject("tapBurst", typeof(RectTransform));
                host.transform.SetParent(transform, false);
                var hrt = host.GetComponent<RectTransform>();
                hrt.anchorMin = hrt.anchorMax = _to;
                hrt.sizeDelta = Vector2.zero;
                hrt.anchoredPosition = Vector2.zero;
                for (int i = 0; i < 10; i++)
                    Spark.Spawn(host.transform, burst, (i / 10f) * Mathf.PI * 2f, 88f, false, (i % 3) == 0);
            }
        }

        static float Sample(float[] arr, float u)
        {
            if (arr == null || arr.Length == 0) return 0f;
            if (arr.Length == 1) return arr[0];
            var x = Mathf.Clamp01(u) * (arr.Length - 1);
            var i = Mathf.FloorToInt(x);
            var j = Mathf.Min(i + 1, arr.Length - 1);
            return Mathf.Lerp(arr[i], arr[j], x - i);
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
