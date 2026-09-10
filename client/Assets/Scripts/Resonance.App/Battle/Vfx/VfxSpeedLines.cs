using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Fever speed-line overlay: white/gold radial + horizontal slash streaks.
    /// Smooth unscaledTime sine sweep. Show replaces any live instance. No English FEVER TIME.
    /// </summary>
    public sealed class VfxSpeedLines : MonoBehaviour
    {
        const int HorizN = 16;
        const int RadialN = 0;
        const float CenterY = 0.88f;

        static readonly Color Gold = VisualTokens.FeverGold;

        static VfxSpeedLines _live;

        Image[] _horiz;
        Image[] _radial;
        float[] _horizY;
        float[] _horizW;
        float[] _radialBase;
        float _left;

        public static void Show(Transform parent, float seconds)
        {
            if (parent == null) return;
            if (seconds <= 0f)
            {
                Hide();
                return;
            }

            Wipe(parent);
            var go = new GameObject("speedLines", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxSpeedLines));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            var fx = go.GetComponent<VfxSpeedLines>();
            fx.Build();
            _live = fx;
            fx.Begin(seconds);
        }

        public static void Hide()
        {
            if (_live == null) return;
            var go = _live.gameObject;
            _live = null;
            if (go != null) Object.DestroyImmediate(go);
        }

        static void Wipe(Transform parent)
        {
            Hide();
            if (parent == null) return;
            var found = parent.GetComponentsInChildren<VfxSpeedLines>(true);
            for (int i = 0; i < found.Length; i++)
            {
                var fx = found[i];
                if (fx == null) continue;
                Object.DestroyImmediate(fx.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_live == this) _live = null;
        }

        void Build()
        {
            var root = transform;
            _horiz = new Image[HorizN];
            _horizY = new float[HorizN];
            _horizW = new float[HorizN];
            for (int i = 0; i < HorizN; i++)
            {
                var y = i < HorizN / 2
                    ? 0.12f + (i / (float)Mathf.Max(1, HorizN / 2)) * 0.10f
                    : 0.86f + ((i - HorizN / 2) / (float)Mathf.Max(1, HorizN / 2)) * 0.08f;
                var w = i % 2 == 0 ? 240f : 160f;
                var h = i % 3 == 0 ? 12f : 8f;
                var tilt = (i % 3) == 1;
                var col = (i & 1) == 0 ? Color.white : Gold;
                col.a = 0f;
                _horiz[i] = MkImg(root, "h" + i, new Vector2(0.50f, y), new Vector2(w, h), tilt ? -8f : 0f,
                    col, UiSprites.Slash());
                _horizY[i] = y;
                _horizW[i] = w;
            }

            _radial = new Image[RadialN];
            _radialBase = new float[RadialN];
            for (int i = 0; i < RadialN; i++)
            {
                var len = 220f + (i % 4) * 40f;
                var thick = (i & 1) == 0 ? 8f : 6f;
                var ang = i * (360f / RadialN);
                var col = (i & 1) == 0 ? Color.white : Gold;
                col.a = 0f;
                var img = MkImg(root, "r" + i, new Vector2(0.50f, CenterY), new Vector2(len, thick), ang,
                    col, UiSprites.Slash());
                var rt = img.rectTransform;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                _radial[i] = img;
                _radialBase[i] = len;
            }
        }

        void Begin(float seconds)
        {
            _left = Mathf.Max(0.05f, seconds);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Paint(Time.unscaledTime);
        }

        void Update()
        {
            _left -= Time.unscaledDeltaTime;
            if (_left <= 0f)
            {
                Hide();
                return;
            }
            Paint(Time.unscaledTime);
        }

        void Paint(float t)
        {
            var fade = _left < 0.12f ? Mathf.Clamp01(_left / 0.12f) : 1f;
            if (_horiz != null)
            {
                for (int i = 0; i < _horiz.Length; i++)
                {
                    var img = _horiz[i];
                    if (img == null) continue;
                    var ph = t * 5.2f + i * 1.37f;
                    var wave = 0.5f + 0.5f * Mathf.Sin(ph);
                    var c = (i & 1) == 0 ? Color.white : Gold;
                    img.color = new Color(c.r, c.g, c.b, (0.10f + 0.16f * wave) * fade);
                    var rt = img.rectTransform;
                    var x = 0.06f + (0.5f + 0.5f * Mathf.Sin(t * 1.7f + i * 2.03f)) * 0.86f;
                    rt.anchorMin = rt.anchorMax = new Vector2(x, _horizY[i]);
                    var thick = 8f + 4f * (0.5f + 0.5f * Mathf.Sin(ph * 0.7f));
                    rt.sizeDelta = new Vector2(_horizW[i] * (0.85f + 0.30f * wave), thick);
                }
            }
            if (_radial == null) return;
            for (int i = 0; i < _radial.Length; i++)
            {
                var img = _radial[i];
                if (img == null) continue;
                var pulse = 0.5f + 0.5f * Mathf.Sin(t * 6.0f + i * 1.05f);
                var len = _radialBase[i] + pulse * 64f;
                img.rectTransform.sizeDelta = new Vector2(len, 6f + 4f * pulse);
                var c = (i & 1) == 0 ? Color.white : Gold;
                img.color = new Color(c.r, c.g, c.b, (0.14f + pulse * 0.42f) * fade);
            }
        }

        static Image MkImg(Transform parent, string name, Vector2 anchor, Vector2 size, float rot, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, rot);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();
        }
    }
}
