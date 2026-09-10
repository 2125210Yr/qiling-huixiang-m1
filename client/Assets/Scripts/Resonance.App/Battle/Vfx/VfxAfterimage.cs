using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Slide/Drive dash ghosts along the lunge path. Soft silhouettes, 12fps, ~0.28s.
    /// Skill motion only — not a character mesh.
    /// </summary>
    public sealed class VfxAfterimage : MonoBehaviour
    {
        public const float FrameDuration = 1f / 12f;
        public const float Life = 0.28f;

        Image[] _ghosts;
        Vector2[] _pos;
        Vector2[] _size;
        Color[] _tint;
        float[] _peak;
        int[] _spawn;
        float _ang;
        float _age;
        int _frame = -1;

        public static void Play(Transform parent, Vector2 from01, Vector2 to01, Color color, int ghosts = 3)
        {
            if (parent == null) return;
            if (color.a < 0.05f) color.a = 1f;
            ghosts = Mathf.Clamp(ghosts, 3, 5);

            var go = new GameObject("afterimage", typeof(RectTransform), typeof(VfxAfterimage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var fx = go.GetComponent<VfxAfterimage>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(parent, from01, to01, color, ghosts);
            fx.ApplyFrame(0);
        }

        void Build(Transform parent, Vector2 from, Vector2 to, Color color, int n)
        {
            var canvas = CanvasPx(parent);
            var a = new Vector2(from.x * canvas.x, from.y * canvas.y);
            var b = new Vector2(to.x * canvas.x, to.y * canvas.y);
            var delta = b - a;
            if (delta.sqrMagnitude < 1f)
            {
                to = from + new Vector2(0f, 0.10f);
                b = new Vector2(to.x * canvas.x, to.y * canvas.y);
                delta = b - a;
            }
            _ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            _ghosts = new Image[n];
            _pos = new Vector2[n];
            _size = new Vector2[n];
            _tint = new Color[n];
            _peak = new float[n];
            _spawn = new int[n];

            for (int i = 0; i < n; i++)
            {
                var t = i / (n - 1f);
                _pos[i] = Vector2.Lerp(from, to, t);
                _spawn[i] = Mathf.Clamp(Mathf.FloorToInt(i * 2f / (n - 1f) + 0.5f), 0, 2);
                _peak[i] = Mathf.Lerp(0.32f, 0.90f, t);
                _size[i] = new Vector2(Mathf.Lerp(44f, 72f, t), Mathf.Lerp(20f, 36f, t));
                _tint[i] = Color.Lerp(color, Color.white, t * 0.40f);
                _ghosts[i] = Quad(transform, "ghost" + i, UiSprites.Soft(), Tint(_tint[i], 0f), _size[i].y);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age >= Life)
            {
                Destroy(gameObject);
                return;
            }
            var frame = Mathf.FloorToInt(_age / FrameDuration);
            if (frame == _frame) return;
            ApplyFrame(frame);
        }

        void ApplyFrame(int frame)
        {
            _frame = frame;
            if (_ghosts == null || _pos == null || _size == null || _tint == null || _peak == null || _spawn == null) return;
            var n = Mathf.Min(_ghosts.Length, Mathf.Min(_pos.Length, Mathf.Min(_size.Length, Mathf.Min(_tint.Length, Mathf.Min(_peak.Length, _spawn.Length)))));
            for (int i = 0; i < n; i++)
            {
                var spawnAt = _spawn[i] * FrameDuration;
                if (_age < spawnAt)
                {
                    Place(_ghosts[i], _pos[i], _size[i], _ang, Tint(_tint[i], 0f));
                    continue;
                }
                var span = Mathf.Max(FrameDuration, Life - spawnAt);
                var u = Mathf.Floor(Mathf.Clamp01((_age - spawnAt) / span) * 8f) / 8f;
                var s = Mathf.Lerp(1.05f, 0.70f, u);
                Place(_ghosts[i], _pos[i], _size[i] * s, _ang, Tint(_tint[i], _peak[i] * (1f - u)));
            }
        }

        static void Place(Image img, Vector2 anchor, Vector2 size, float ang, Color color)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, ang);
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
        }

        static Color Tint(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static Vector2 CanvasPx(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();
            if (rt == null) return new Vector2(756f, 1344f);
            var r = rt.rect;
            return new Vector2(Mathf.Max(360f, r.width), Mathf.Max(640f, r.height));
        }

        static Image Quad(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Soft());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
