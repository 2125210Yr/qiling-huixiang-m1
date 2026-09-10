using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Counter riposte: reverse slash defender→attacker + word 反击. 0.4s. UiSprites.Slash.
    /// Never Counter / Riposte.
    /// </summary>
    public static class VfxCounter
    {
        const float Life = 0.40f;
        const float TiltDeg = -22f;
        const float TwinOffsetDeg = 28f;
        const string Word = "反击";

        public static void Play(Transform parent, Vector2 from01, Vector2 to01, Color color)
        {
            if (parent == null) return;

            color = new Color(color.r, color.g, color.b, 1f);
            var hot = Color.Lerp(Color.white, color, 0.22f);
            var ink = new Color(color.r * 0.28f, color.g * 0.12f, color.b * 0.10f, 1f);

            var go = new GameObject("vfxCounter", typeof(RectTransform), typeof(Player));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var canvas = CanvasPx(parent);
            var a = new Vector2(from01.x * canvas.x, from01.y * canvas.y);
            var b = new Vector2(to01.x * canvas.x, to01.y * canvas.y);
            var delta = b - a;
            if (delta.sqrMagnitude < 16f) delta = new Vector2(48f, 0f);
            var dist = Mathf.Max(140f, delta.magnitude * 0.95f);
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var tilt = ang + TiltDeg;
            var mid = (from01 + to01) * 0.5f;

            var halo = Blade(go.transform, mid, dist, 82f, tilt, new Color(1f, 1f, 1f, 0.95f));
            var main = Blade(go.transform, mid, dist, 64f, tilt, color);
            var twin = Blade(go.transform, mid, dist * 0.72f, 28f, tilt + TwinOffsetDeg, hot);

            var flash = Pic(go.transform, "flash", UiSprites.Soft(), hot, 96f, to01);
            var ring = Pic(go.transform, "ring", UiSprites.Circle(), color, 64f, to01);
            var star = Pic(go.transform, "star", UiSprites.Star(), Color.white, 44f, to01);

            var copyGo = new GameObject("copy", typeof(RectTransform));
            copyGo.transform.SetParent(go.transform, false);
            var copy = copyGo.GetComponent<RectTransform>();
            copy.anchorMin = copy.anchorMax = from01;
            copy.pivot = new Vector2(0.5f, 0.5f);
            copy.sizeDelta = Vector2.zero;
            copy.anchoredPosition = Vector2.zero;

            var wordB = MkText(copy, "wordB", Word, 40, ink, new Vector2(220f, 56f), new Vector2(5f, 40f), 3f);
            var wordF = MkText(copy, "wordF", Word, 40, hot, new Vector2(220f, 56f), new Vector2(0f, 44f), 3f);

            go.GetComponent<Player>().Bind(
                new[] { halo, main, twin },
                flash, ring, star,
                copy, new Graphic[] { wordB, wordF });
        }

        static Vector2 CanvasPx(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();
            if (rt == null) return new Vector2(756f, 1344f);
            var r = rt.rect;
            return new Vector2(Mathf.Max(360f, r.width), Mathf.Max(640f, r.height));
        }

        static Image Blade(Transform parent, Vector2 mid, float dist, float thick, float ang, Color color)
        {
            var blade = new GameObject("blade", typeof(RectTransform), typeof(Image));
            blade.transform.SetParent(parent, false);
            var brt = blade.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = mid;
            brt.sizeDelta = new Vector2(dist, thick);
            brt.anchoredPosition = Vector2.zero;
            brt.localEulerAngles = new Vector3(0f, 0f, ang);
            var img = blade.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Slash());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Pic(Transform parent, string name, Sprite sprite, Color color, float size, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string name, string s, int size, Color color, Vector2 dim, Vector2 pos, float outline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = s ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(outline, -outline);
            return tx;
        }

        static Color Fade(Color rgb, float a)
        {
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a));
        }

        sealed class Player : MonoBehaviour
        {
            Image[] _blades;
            Image _flash;
            Image _ring;
            Image _star;
            RectTransform _copy;
            Graphic[] _copyGfx;
            Color[] _bladeC;
            Color _flashC;
            Color _ringC;
            Color _starC;
            Color[] _copyC;
            float _age;

            public void Bind(
                Image[] blades, Image flash, Image ring, Image star,
                RectTransform copy, Graphic[] copyGfx)
            {
                _blades = blades;
                _flash = flash;
                _ring = ring;
                _star = star;
                _copy = copy;
                _copyGfx = copyGfx;
                _bladeC = CopyColors(blades);
                _flashC = flash != null ? flash.color : Color.clear;
                _ringC = ring != null ? ring.color : Color.clear;
                _starC = star != null ? star.color : Color.clear;
                _copyC = new Color[copyGfx != null ? copyGfx.Length : 0];
                for (int i = 0; i < _copyC.Length; i++)
                    _copyC[i] = copyGfx[i] != null ? copyGfx[i].color : Color.clear;
                Apply(0f);
            }

            static Color[] CopyColors(Image[] imgs)
            {
                if (imgs == null) return null;
                var c = new Color[imgs.Length];
                for (int i = 0; i < imgs.Length; i++)
                    c[i] = imgs[i] != null ? imgs[i].color : Color.clear;
                return c;
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
                var pop = 1f - (1f - u) * (1f - u);
                var sx = Mathf.Lerp(0.55f, 1.18f, u);
                var sy = Mathf.Lerp(1.25f, 0.28f, u);
                if (_blades != null)
                {
                    for (int i = 0; i < _blades.Length; i++)
                    {
                        var img = _blades[i];
                        if (img == null) continue;
                        img.rectTransform.localScale = new Vector3(sx, sy, 1f);
                        img.color = Fade(_bladeC[i], _bladeC[i].a * (1f - u));
                    }
                }

                if (_flash != null)
                {
                    _flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1.70f, pop);
                    _flash.color = Fade(_flashC, 0.90f * (1f - u));
                }
                if (_ring != null)
                {
                    _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.42f, 2.10f, u);
                    _ring.color = Fade(_ringC, 0.72f * (1f - u));
                }
                if (_star != null)
                {
                    _star.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.20f, 0.28f, u);
                    _star.color = Fade(_starC, 1f - Mathf.Clamp01(u / 0.42f));
                }

                var punch = u < 0.17f
                    ? Mathf.Lerp(1.70f, 1.08f, u / 0.17f)
                    : Mathf.Lerp(1.08f, 0.92f, (u - 0.17f) / 0.83f);
                var fade = u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
                fade = Mathf.Clamp01(fade);

                if (_copy != null)
                {
                    _copy.localScale = Vector3.one * punch;
                    _copy.anchoredPosition = new Vector2(0f, 36f * u);
                }
                if (_copyGfx == null) return;
                for (int i = 0; i < _copyGfx.Length; i++)
                {
                    var g = _copyGfx[i];
                    if (g == null) continue;
                    var c = _copyC[i];
                    c.a *= fade;
                    g.color = c;
                }
            }
        }
    }
}
