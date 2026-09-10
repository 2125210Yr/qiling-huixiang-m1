using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Dark skill FX: purple claw marks over a void flash.
    /// No original logos.
    /// </summary>
    public sealed class VfxElemDark : MonoBehaviour
    {
        Image _void;
        Image _ring;
        Image _flash;
        Image[] _claws;
        float _life = 0.38f;
        float _age;
        bool _fever;

        public static void Play(Transform parent, Vector2 anchor, bool fever)
        {
            if (parent == null) return;

            var size = fever ? 380f : 260f;
            var go = new GameObject("vfxDark", typeof(RectTransform), typeof(VfxElemDark));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();

            var purple = VisualTokens.ElemDark;
            var hot = new Color(0.78f, 0.42f, 0.96f, 1f);
            var hole = new Color(0.06f, 0.02f, 0.10f, 0.94f);
            var rim = new Color(0.86f, 0.62f, 1f, 0.95f);

            var core = Child(go.transform, "void", UiSprites.Soft(), hole, size);
            var ring = Child(go.transform, "ring", UiSprites.Circle(), new Color(purple.r, purple.g, purple.b, 0.92f), size * 0.48f);
            var flash = Child(go.transform, "flash", UiSprites.Soft(), new Color(hot.r, hot.g, hot.b, 0.90f), size * 0.28f);

            var clawN = fever ? 6 : 4;
            var claws = new Image[clawN];
            var baseAng = -34f;
            var fan = fever ? 38f : 22f;
            var spread = fever ? 58f : 36f;
            for (int i = 0; i < clawN; i++)
            {
                var t = clawN == 1 ? 0.5f : i / (float)(clawN - 1);
                var ang = baseAng + (t - 0.5f) * fan;
                var off = (t - 0.5f) * spread;
                var rad = (ang + 90f) * Mathf.Deg2Rad;
                var claw = Child(go.transform, "claw" + i, UiSprites.Slash(),
                    i % 2 == 0 ? rim : purple, size * (fever ? 1.16f : 0.94f));
                var crt = claw.rectTransform;
                crt.sizeDelta = new Vector2(size * (fever ? 1.28f : 1.02f), fever ? 28f : 16f);
                crt.anchoredPosition = new Vector2(Mathf.Cos(rad) * off, Mathf.Sin(rad) * off);
                crt.localEulerAngles = new Vector3(0f, 0f, ang);
                var c = claw.color;
                c.a = 0.96f;
                claw.color = c;
                claws[i] = claw;
            }

            var fx = go.GetComponent<VfxElemDark>();
            fx._void = core;
            fx._ring = ring;
            fx._flash = flash;
            fx._claws = claws;
            fx._life = fever ? 0.64f : 0.46f;
            fx._fever = fever;

            var n = fever ? 14 : 8;
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
                var c = i % 3 == 0 ? rim : (i % 2 == 0 ? hot : purple);
                Spark.Spawn(go.transform, c, ang, fever ? 164f : 108f);
            }
            if (fever)
            {
                for (int i = 0; i < 5; i++)
                {
                    var ang = (i / 5f) * Mathf.PI * 2f + 0.38f;
                    Spark.Spawn(go.transform, hole, ang, 154f);
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

            if (_void != null)
            {
                _void.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.15f, ease);
                var c = _void.color;
                c.a = 0.94f * a;
                _void.color = c;
            }
            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * (0.50f + u * 1.95f);
                var c = _ring.color;
                c.a = 0.78f * a;
                _ring.color = c;
            }
            if (_flash != null)
            {
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(1.40f, 0.18f, u);
                var c = _flash.color;
                c.a = 0.92f * (1f - u * u);
                _flash.color = c;
            }
            if (_claws != null)
            {
                var sx = Mathf.Lerp(0.48f, 1.24f, ease);
                var sy = Mathf.Lerp(1.28f, 0.20f, u);
                for (int i = 0; i < _claws.Length; i++)
                {
                    var claw = _claws[i];
                    if (claw == null) continue;
                    claw.transform.localScale = new Vector3(sx, sy, 1f);
                    var c = claw.color;
                    c.a = 0.96f * a;
                    claw.color = c;
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
