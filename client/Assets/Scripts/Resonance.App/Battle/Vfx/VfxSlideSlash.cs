using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Slide slash: wide ribbon caster→target, +22° tilt, multi-blade + sparks.
    /// </summary>
    public sealed class VfxSlideSlash : MonoBehaviour
    {
        const float TiltDeg = 22f;
        const float TwinOffsetDeg = -28f;
        const float Life = 0.52f;
        const float FeverLife = 0.62f;

        Image[] _blades;
        float[] _alpha;
        float _age;
        float _life = Life;

        public static void Play(Transform parent, Vector2 from, Vector2 to, Color color, bool fever)
        {
            if (parent == null) return;
            if (color.a < 0.05f) color.a = 1f;
            if ((to - from).sqrMagnitude < 0.0001f)
                to = from + new Vector2(0f, 0.10f);

            var go = new GameObject("slideSlash", typeof(RectTransform), typeof(VfxSlideSlash));
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

            var canvas = CanvasPx(parent);
            var mid = (from + to) * 0.5f;
            var a = new Vector2(from.x * canvas.x, from.y * canvas.y);
            var b = new Vector2(to.x * canvas.x, to.y * canvas.y);
            var delta = b - a;
            var dist = Mathf.Max(180f, delta.magnitude * 1.05f);
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var thick = fever ? 118f : 88f;
            var tilt = ang + TiltDeg;
            var hot = Color.Lerp(color, Color.white, 0.45f);

            var halo = Blade(go.transform, mid, dist, thick + 24f, tilt, new Color(1f, 1f, 1f, 0.95f));
            var main = Blade(go.transform, mid, dist, thick, tilt, new Color(color.r, color.g, color.b, 1f));
            var twin = Blade(go.transform, mid, dist * 0.78f, thick * 0.48f, tilt + TwinOffsetDeg, color);
            var cross = Blade(go.transform, mid, dist * 0.62f, thick * 0.32f, tilt + 34f, hot);
            var sparkLine = Blade(go.transform, mid, dist * 0.42f, thick * 0.18f, tilt - 16f, new Color(1f, 0.92f, 0.55f, 1f));

            var sparkHost = new GameObject("sparks", typeof(RectTransform));
            sparkHost.transform.SetParent(go.transform, false);
            var srt = sparkHost.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = to;
            srt.sizeDelta = Vector2.zero;
            srt.anchoredPosition = Vector2.zero;
            var n = fever ? 16 : 12;
            for (int i = 0; i < n; i++)
                Spark.Spawn(sparkHost.transform, (i & 1) == 0 ? hot : color,
                    (i / (float)n) * Mathf.PI * 2f, fever ? 148f : 110f, false, (i % 3) == 0);

            var fx = go.GetComponent<VfxSlideSlash>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx._blades = new[] { halo, main, twin, cross, sparkLine };
            fx._alpha = new float[fx._blades.Length];
            for (int i = 0; i < fx._blades.Length; i++)
                fx._alpha[i] = fx._blades[i] != null ? fx._blades[i].color.a : 0f;
            fx._life = fever ? FeverLife : Life;
            CanvasShake.Punch(fever ? 18f : 12f, 0.22f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var sx = Mathf.Lerp(0.48f, 1.22f, u);
            var sy = Mathf.Lerp(1.35f, 0.22f, u);
            if (_blades != null)
            {
                for (int i = 0; i < _blades.Length; i++)
                {
                    var img = _blades[i];
                    if (img == null) continue;
                    img.transform.localScale = new Vector3(sx, sy, 1f);
                    if (_alpha == null || i >= _alpha.Length) continue;
                    var c = img.color;
                    c.a = _alpha[i] * (1f - u);
                    img.color = c;
                }
            }
            if (_age >= _life) Destroy(gameObject);
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

        static Image Blade(Transform parent, Vector2 mid, float dist, float thick, float ang, Color color)
        {
            if (parent == null) return null;
            var blade = new GameObject("blade", typeof(RectTransform), typeof(Image));
            blade.transform.SetParent(parent, false);
            var brt = blade.GetComponent<RectTransform>();
            var img = blade.GetComponent<Image>();
            if (brt == null || img == null)
            {
                Object.Destroy(blade);
                return null;
            }
            brt.anchorMin = brt.anchorMax = mid;
            brt.sizeDelta = new Vector2(dist, thick);
            brt.anchoredPosition = Vector2.zero;
            brt.localEulerAngles = new Vector3(0f, 0f, ang);
            UiSprites.Apply(img, UiSprites.Slash());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
