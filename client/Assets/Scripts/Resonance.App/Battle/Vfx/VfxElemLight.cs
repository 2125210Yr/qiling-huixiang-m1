using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Light skill FX: gold burst + sparkles.
    /// </summary>
    public sealed class VfxElemLight : MonoBehaviour
    {
        Image _core;
        Image _ring;
        Image _flash;
        Image _star;
        Image[] _rays;
        float _life = 0.38f;
        float _age;
        bool _fever;

        public static void Play(Transform parent, Vector2 anchor, bool fever)
        {
            if (parent == null) return;

            var size = fever ? 380f : 260f;
            var go = new GameObject("vfxLight", typeof(RectTransform), typeof(VfxElemLight));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();

            var gold = VisualTokens.ElemLight;
            var hot = VisualTokens.FeverGold;
            var white = VisualTokens.TapWhite;

            var core = Child(go.transform, "core", UiSprites.Soft(), fever ? hot : gold, size);
            var ring = Child(go.transform, "ring", UiSprites.Circle(), new Color(1f, 0.94f, 0.62f, 0.95f), size * 0.52f);
            var flash = Child(go.transform, "flash", UiSprites.Soft(), new Color(white.r, white.g, white.b, 0.92f), size * 0.30f);

            var rayN = fever ? 8 : 6;
            var rays = new Image[rayN];
            for (int i = 0; i < rayN; i++)
            {
                var ray = Child(go.transform, "ray" + i, UiSprites.Slash(),
                    i % 2 == 0 ? hot : gold, size * (fever ? 1.18f : 0.92f));
                var rrt = ray.rectTransform;
                rrt.sizeDelta = new Vector2(size * (fever ? 1.22f : 0.96f), fever ? 22f : 14f);
                rrt.localEulerAngles = new Vector3(0f, 0f, i * (180f / rayN) + 8f);
                var c = ray.color;
                c.a = 0.88f;
                ray.color = c;
                rays[i] = ray;
            }

            var star = Child(go.transform, "star", UiSprites.Star(),
                fever ? hot : white, size * (fever ? 0.60f : 0.46f));

            var fx = go.GetComponent<VfxElemLight>();
            fx._core = core;
            fx._ring = ring;
            fx._flash = flash;
            fx._star = star;
            fx._rays = rays;
            fx._life = fever ? 0.64f : 0.46f;
            fx._fever = fever;

            var n = fever ? 16 : 9;
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
                var c = i % 3 == 0 ? white : (i % 2 == 0 ? hot : gold);
                Spark.Spawn(go.transform, c, ang, fever ? 168f : 110f);
            }
            if (fever)
            {
                for (int i = 0; i < 6; i++)
                {
                    var ang = (i / 6f) * Mathf.PI * 2f + 0.42f;
                    Spark.Spawn(go.transform, white, ang, 168f, false);
                }
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var ease = 1f - (1f - u) * (1f - u);
            transform.localScale = Vector3.one * Mathf.Lerp(0.32f, _fever ? 2.35f : 1.75f, ease);
            var a = 1f - u;

            if (_core != null)
            {
                var c = _core.color;
                c.a = 0.92f * a;
                _core.color = c;
            }
            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * (0.55f + u * 1.85f);
                var c = _ring.color;
                c.a = 0.72f * a;
                _ring.color = c;
            }
            if (_flash != null)
            {
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.20f, u);
                var c = _flash.color;
                c.a = 0.95f * (1f - u * u);
                _flash.color = c;
            }
            if (_star != null)
            {
                _star.transform.localScale = Vector3.one * Mathf.Lerp(0.30f, _fever ? 1.55f : 1.15f, ease);
                _star.transform.localEulerAngles = new Vector3(0f, 0f, ease * 95f);
                var c = _star.color;
                c.a = 0.98f * (1f - u * u);
                _star.color = c;
            }
            if (_rays != null)
            {
                var sx = Mathf.Lerp(0.55f, 1.22f, ease);
                var sy = Mathf.Lerp(1.20f, 0.22f, u);
                for (int i = 0; i < _rays.Length; i++)
                {
                    var ray = _rays[i];
                    if (ray == null) continue;
                    ray.transform.localScale = new Vector3(sx, sy, 1f);
                    var c = ray.color;
                    c.a = 0.88f * a;
                    ray.color = c;
                }
            }

            if (_age >= _life) Destroy(gameObject);
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
    }
}
