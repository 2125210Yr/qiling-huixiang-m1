using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Caster-side skill fire: radial slash burst, expanding rings, star slam.
    /// TAP stays a portrait ring — no SHOWTIME cut-in. Auto stays quieter.
    /// </summary>
    public static class VfxSkillCast
    {
        public static void Play(Transform parent, Vector2 anchor, Color color, SkillType kind, bool fever)
        {
            if (parent == null) return;
            color = new Color(color.r, color.g, color.b, 1f);
            var hot = Color.Lerp(Color.white, color, fever ? 0.18f : 0.28f);
            var big = fever || kind == SkillType.Drive || kind == SkillType.Slide || kind == SkillType.Leader;
            var auto = !fever && kind == SkillType.Auto;
            var life = auto ? 0.28f : fever ? 0.62f : kind == SkillType.Drive || kind == SkillType.Leader ? 0.56f
                : kind == SkillType.Slide ? 0.50f : 0.42f;

            var go = new GameObject("skillCast", typeof(RectTransform), typeof(Player));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();

            var glowSz = auto ? 90f : fever ? 150f : kind == SkillType.Drive ? 140f : kind == SkillType.Slide ? 130f : 110f;
            var core = Pic(go.transform, "glow", UiSprites.Soft(), new Color(color.r, color.g, color.b, 0.55f), glowSz);
            var hotCore = Pic(go.transform, "hot", UiSprites.Soft(), new Color(hot.r, hot.g, hot.b, 0.70f), glowSz * 0.32f);
            var star = Pic(go.transform, "star", UiSprites.Star(), hot, auto ? 36f : fever ? 52f : 44f);
            var cross = Pic(go.transform, "cross", UiSprites.Star(), Color.white, auto ? 26f : 34f);
            if (cross != null) cross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            Image ring = null;
            Image rim = null;

            var nSlash = auto ? 5 : fever ? 12 : kind == SkillType.Drive || kind == SkillType.Leader ? 10
                : kind == SkillType.Slide ? 9 : 7;
            var slashes = new Image[nSlash];
            var slashMax = new float[nSlash];
            var slashCol = Color.Lerp(Color.white, color, 0.35f);
            var reach = auto ? 92f : fever ? 210f : kind == SkillType.Drive ? 188f : kind == SkillType.Slide ? 168f : 138f;
            for (int i = 0; i < nSlash; i++)
            {
                var ang = (i / (float)nSlash) * 360f + (i & 1) * 9f - 6f;
                slashMax[i] = reach * ((i & 1) == 0 ? 1.08f : 0.78f);
                var thick = auto ? 8f : (i & 1) == 0 ? (fever ? 18f : 14f) : 9f;
                slashes[i] = Bar(go.transform, (i & 1) == 0 ? slashCol : color, ang, 28f, thick);
            }

            Image[] extra = null;
            float[] extraMax = null;
            if (big)
            {
                extra = new Image[3];
                extraMax = new[] { reach * 1.22f, reach * 1.05f, reach * 0.72f };
                extra[0] = Bar(go.transform, new Color(1f, 1f, 1f, 0.95f), 28f, extraMax[0] * 0.45f, fever ? 22f : 16f);
                extra[1] = Bar(go.transform, color, -38f, extraMax[1] * 0.45f, fever ? 16f : 12f);
                extra[2] = Bar(go.transform, hot, 72f, extraMax[2] * 0.45f, 10f);
            }

            var nSpark = auto ? 6 : fever ? 18 : kind == SkillType.Drive ? 16 : kind == SkillType.Slide ? 14 : 12;
            var dist = auto ? 56f : fever ? 168f : kind == SkillType.Drive ? 148f : kind == SkillType.Slide ? 128f : 96f;
            for (int i = 0; i < nSpark; i++)
            {
                var ang = (i / (float)nSpark) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
                Spark.Spawn(go.transform, (i & 1) == 0 ? hot : color, ang, dist * Random.Range(0.78f, 1.12f), false, (i % 3) == 0);
            }

            var player = go.GetComponent<Player>();
            if (player == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            player.Bind(core, hotCore, star, cross, ring, rim, slashes, slashMax, extra, extraMax, life, big);

            var shake = auto ? 5f : fever ? 20f : kind == SkillType.Drive ? 22f : kind == SkillType.Slide ? 16f : 12f;
            CanvasShake.Punch(shake, auto ? 0.12f : 0.22f);
            if (!auto && kind != SkillType.Drive && !fever)
                CombatFeel.ScreenTint(parent, color, kind == SkillType.Slide ? 0.14f : 0.09f,
                    kind == SkillType.Slide ? 0.18f : 0.12f);
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
            Paint(img, sprite, color);
            return img;
        }

        static Image Bar(Transform parent, Color color, float angDeg, float len, float thick)
        {
            var go = new GameObject("ray", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(len, thick);
            rt.localEulerAngles = new Vector3(0f, 0f, angDeg);
            var img = go.GetComponent<Image>();
            Paint(img, UiSprites.Slash(), color);
            return img;
        }

        static void Paint(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Soft());
            img.color = color;
            img.raycastTarget = false;
        }

        static Color Fade(Color rgb, float a)
        {
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a));
        }

        sealed class Player : MonoBehaviour
        {
            Image _core;
            Image _hot;
            Image _star;
            Image _cross;
            Image _ring;
            Image _rim;
            Image[] _slashes;
            Image[] _extra;
            float[] _slashMax;
            float[] _extraMax;
            Color _coreC;
            Color _hotC;
            Color _starC;
            Color _crossC;
            Color _ringC;
            Color _rimC;
            Color[] _slashC;
            Color[] _extraC;
            float _life = 0.42f;
            float _age;
            bool _big;

            public void Bind(
                Image core, Image hot, Image star, Image cross, Image ring, Image rim,
                Image[] slashes, float[] slashMax, Image[] extra, float[] extraMax,
                float life, bool big)
            {
                _core = core;
                _hot = hot;
                _star = star;
                _cross = cross;
                _ring = ring;
                _rim = rim;
                _slashes = slashes;
                _slashMax = slashMax;
                _extra = extra;
                _extraMax = extraMax;
                _life = life;
                _big = big;
                _coreC = core != null ? core.color : Color.clear;
                _hotC = hot != null ? hot.color : Color.clear;
                _starC = star != null ? star.color : Color.clear;
                _crossC = cross != null ? cross.color : Color.clear;
                _ringC = ring != null ? ring.color : Color.clear;
                _rimC = rim != null ? rim.color : Color.clear;
                _slashC = Copy(slashes);
                _extraC = Copy(extra);
                Apply(0f);
            }

            static Color[] Copy(Image[] imgs)
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
                if (_age >= _life)
                {
                    Destroy(gameObject);
                    return;
                }
                Apply(Mathf.Clamp01(_age / _life));
            }

            void Apply(float u)
            {
                var pop = 1f - (1f - u) * (1f - u);
                if (_core != null)
                {
                    _core.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, _big ? 1.18f : 1.08f, pop);
                    _core.color = Fade(_coreC, 0.62f * (1f - u));
                }
                if (_hot != null)
                {
                    _hot.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.22f, u);
                    _hot.color = Fade(_hotC, 0.95f * (1f - u * u));
                }
                if (_star != null)
                {
                    _star.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.55f, 0.28f, u);
                    _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, u * 48f);
                    _star.color = Fade(_starC, 1f - Mathf.Clamp01(u / 0.48f));
                }
                if (_cross != null)
                {
                    _cross.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.90f, 0.22f, u);
                    _cross.color = Fade(_crossC, 1f - Mathf.Clamp01(u / 0.36f));
                }
                if (_ring != null)
                {
                    _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.32f, _big ? 2.70f : 2.15f, u);
                    _ring.color = Fade(_ringC, 0.78f * (1f - u));
                }
                if (_rim != null)
                {
                    _rim.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.22f, 1.85f, Mathf.Clamp01(u * 1.40f));
                    _rim.color = Fade(_rimC, 0.82f * Mathf.Clamp01(1f - u * 1.65f));
                }

                GrowBars(_slashes, _slashC, _slashMax, u, pop);
                GrowBars(_extra, _extraC, _extraMax, u, pop);
            }

            static void GrowBars(Image[] bars, Color[] cols, float[] max, float u, float pop)
            {
                if (bars == null || cols == null || max == null) return;
                var n = Mathf.Min(bars.Length, Mathf.Min(cols.Length, max.Length));
                var grow = Mathf.Sqrt(pop);
                for (int i = 0; i < n; i++)
                {
                    var img = bars[i];
                    if (img == null) continue;
                    var rt = img.rectTransform;
                    rt.sizeDelta = new Vector2(Mathf.Lerp(22f, max[i], grow), rt.sizeDelta.y);
                    var a = 0.92f * (1f - u);
                    if (u < 0.10f) a *= u / 0.10f;
                    img.color = Fade(cols[i], a);
                }
            }
        }
    }
}
