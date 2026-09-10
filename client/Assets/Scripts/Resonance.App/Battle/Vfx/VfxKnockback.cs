using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Knockback spray on the target: shock ring plus dust/streaks away from the attacker.
    /// Heavy (Drive/Slide) 0.40s, more streaks. Light (tap) 0.22s. Printed / additive luxury UI overlay.
    /// </summary>
    public static class VfxKnockback
    {
        const float LightLife = 0.22f;
        const float HeavyLife = 0.40f;

        public static void Play(Transform parent, Vector2 from01, Vector2 to01, Color color, bool heavy)
        {
            if (parent == null) return;

            color = new Color(color.r, color.g, color.b, 1f);
            var hot = Color.Lerp(Color.white, color, heavy ? 0.22f : 0.38f);
            var dustCol = Color.Lerp(color, new Color(0.55f, 0.48f, 0.42f, 1f), 0.42f);
            var life = heavy ? HeavyLife : LightLife;

            var canvas = CanvasPx(parent);
            var a = new Vector2(from01.x * canvas.x, from01.y * canvas.y);
            var b = new Vector2(to01.x * canvas.x, to01.y * canvas.y);
            var delta = b - a;
            if (delta.sqrMagnitude < 16f) delta = new Vector2(48f, 0f);
            var dir = delta.normalized;
            var ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var perp = new Vector2(-dir.y, dir.x);

            var go = new GameObject("knockback", typeof(RectTransform), typeof(Player));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = to01;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();

            var puff = Pic(go.transform, "puff", UiSprites.Soft(), color, heavy ? 88f : 64f);
            var ring = Pic(go.transform, "ring", UiSprites.Soft(), hot, heavy ? 56f : 42f);
            var chip = Pic(go.transform, "chip", UiSprites.Spark(), Color.white, heavy ? 22f : 16f);
            if (chip != null) chip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            var nSlash = heavy ? 7 : 4;
            var slashes = new Image[nSlash];
            var slashMax = new float[nSlash];
            var slashThick = new float[nSlash];
            var spread = heavy ? 40f : 26f;
            for (int i = 0; i < nSlash; i++)
            {
                var t = nSlash == 1 ? 0f : i / (nSlash - 1f) * 2f - 1f;
                var deg = ang + t * spread + Random.Range(-5f, 5f);
                var main = i == nSlash / 2;
                slashMax[i] = (heavy ? 118f : 74f) * Random.Range(main ? 0.92f : 0.62f, main ? 1.18f : 1.02f);
                slashThick[i] = main ? (heavy ? 16f : 12f) : (heavy ? 11f : 8f);
                slashes[i] = Bar(go.transform, "slash", UiSprites.Slash(),
                    main ? hot : color, deg, 22f, slashThick[i]);
            }

            var nLine = heavy ? 5 : 3;
            var lines = new Image[nLine];
            var lineMax = new float[nLine];
            for (int i = 0; i < nLine; i++)
            {
                var t = nLine == 1 ? 0f : i / (nLine - 1f) * 2f - 1f;
                var deg = ang + t * (spread * 0.55f) + Random.Range(-4f, 4f);
                lineMax[i] = (heavy ? 148f : 92f) * Random.Range(0.78f, 1.12f);
                lines[i] = Bar(go.transform, "line", UiSprites.Slash(),
                    (i & 1) == 0 ? hot : color, deg, 16f, (i & 1) == 0 ? 6f : 4f);
            }

            var nCloud = heavy ? 6 : 4;
            var clouds = new Image[nCloud];
            var cloudFrom = new Vector2[nCloud];
            var cloudTo = new Vector2[nCloud];
            var cloudFall = new float[nCloud];
            var cloudDelay = new float[nCloud];
            var cone = heavy ? 0.62f : 0.42f;
            for (int i = 0; i < nCloud; i++)
            {
                var side = (i & 1) == 0 ? 1f : -1f;
                var fan = ((i + 0.5f) / nCloud) * cone * side;
                var rad = ang * Mathf.Deg2Rad + fan + Random.Range(-0.08f, 0.08f);
                var shot = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                var reach = (heavy ? 108f : 68f) * Random.Range(0.70f, 1.14f);
                cloudFrom[i] = Snap(dir * Random.Range(6f, 16f) + perp * side * Random.Range(2f, 10f));
                cloudTo[i] = shot * reach;
                cloudFall[i] = Random.Range(10f, heavy ? 28f : 18f);
                cloudDelay[i] = i * (heavy ? 0.028f : 0.016f);
                clouds[i] = Pic(go.transform, "cloud", UiSprites.Soft(),
                    (i & 1) == 0 ? dustCol : color, Random.Range(heavy ? 20f : 14f, heavy ? 34f : 24f));
            }

            var nGrit = heavy ? 10 : 6;
            var grit = new Image[nGrit];
            var gritFrom = new Vector2[nGrit];
            var gritTo = new Vector2[nGrit];
            var gritDelay = new float[nGrit];
            for (int i = 0; i < nGrit; i++)
            {
                var side = (i & 1) == 0 ? 1f : -1f;
                var fan = ((i + 0.35f) / nGrit) * cone * 0.85f * side;
                var rad = ang * Mathf.Deg2Rad + fan + Random.Range(-0.10f, 0.10f);
                var shot = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                var reach = (heavy ? 142f : 86f) * Random.Range(0.68f, 1.16f);
                gritFrom[i] = Snap(dir * Random.Range(4f, 12f));
                gritTo[i] = shot * reach;
                gritDelay[i] = (i % 4) * (heavy ? 0.018f : 0.012f);
                grit[i] = Pic(go.transform, "grit",
                    (i % 3) == 0 ? UiSprites.Spark() : UiSprites.Circle(),
                    (i % 3) == 0 ? hot : dustCol, i % 3 == 0 ? 12f : 8f);
            }

            var player = go.GetComponent<Player>();
            if (player == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            player.Bind(
                puff, ring, chip,
                slashes, slashMax, slashThick,
                lines, lineMax,
                clouds, cloudFrom, cloudTo, cloudFall, cloudDelay,
                grit, gritFrom, gritTo, gritDelay,
                dir, life, heavy);
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

        static Image Bar(Transform parent, string name, Sprite sprite, Color color, float angDeg, float len, float thick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(len, thick);
            rt.localEulerAngles = new Vector3(0f, 0f, angDeg);
            var img = go.GetComponent<Image>();
            Paint(img, sprite, color);
            return img;
        }

        static void Paint(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
        }

        static Vector2 CanvasPx(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();
            if (rt == null) return new Vector2(756f, 1344f);
            var r = rt.rect;
            return new Vector2(Mathf.Max(360f, r.width), Mathf.Max(640f, r.height));
        }

        static Vector2 Snap(Vector2 p)
        {
            return p;
        }

        static float Quant(float v)
        {
            return v;
        }

        static Color Fade(Color rgb, float a)
        {
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a));
        }

        sealed class Player : MonoBehaviour
        {
            Image _puff;
            Image _ring;
            Image _chip;
            Image[] _slashes;
            Image[] _lines;
            Image[] _clouds;
            Image[] _grit;
            float[] _slashMax;
            float[] _slashThick;
            float[] _lineMax;
            Vector2[] _cloudFrom;
            Vector2[] _cloudTo;
            float[] _cloudFall;
            float[] _cloudDelay;
            Vector2[] _gritFrom;
            Vector2[] _gritTo;
            float[] _gritDelay;
            Color _puffC;
            Color _ringC;
            Color _chipC;
            Color[] _slashC;
            Color[] _lineC;
            Color[] _cloudC;
            Color[] _gritC;
            Vector2 _dir;
            float _life = LightLife;
            float _age;
            bool _heavy;

            public void Bind(
                Image puff, Image ring, Image chip,
                Image[] slashes, float[] slashMax, float[] slashThick,
                Image[] lines, float[] lineMax,
                Image[] clouds, Vector2[] cloudFrom, Vector2[] cloudTo, float[] cloudFall, float[] cloudDelay,
                Image[] grit, Vector2[] gritFrom, Vector2[] gritTo, float[] gritDelay,
                Vector2 dir, float life, bool heavy)
            {
                _puff = puff;
                _ring = ring;
                _chip = chip;
                _slashes = slashes;
                _slashMax = slashMax;
                _slashThick = slashThick;
                _lines = lines;
                _lineMax = lineMax;
                _clouds = clouds;
                _cloudFrom = cloudFrom;
                _cloudTo = cloudTo;
                _cloudFall = cloudFall;
                _cloudDelay = cloudDelay;
                _grit = grit;
                _gritFrom = gritFrom;
                _gritTo = gritTo;
                _gritDelay = gritDelay;
                _dir = dir;
                _life = life;
                _heavy = heavy;
                _puffC = puff != null ? puff.color : Color.clear;
                _ringC = ring != null ? ring.color : Color.clear;
                _chipC = chip != null ? chip.color : Color.clear;
                _slashC = CopyColors(slashes);
                _lineC = CopyColors(lines);
                _cloudC = CopyColors(clouds);
                _gritC = CopyColors(grit);
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

                if (_puff != null)
                {
                    _puff.rectTransform.anchoredPosition = Snap(_dir * (8f + 18f * pop));
                    _puff.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.55f, _heavy ? 1.55f : 1.28f, pop));
                    _puff.color = Fade(_puffC, 0.88f * (1f - u));
                }

                if (_ring != null)
                {
                    _ring.rectTransform.anchoredPosition = Snap(_dir * (6f * pop));
                    _ring.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.38f, _heavy ? 2.35f : 1.85f, u));
                    var ra = 0.72f * (1f - u);
                    if (u < 0.10f) ra *= u / 0.10f;
                    _ring.color = Fade(_ringC, ra);
                }

                if (_chip != null)
                {
                    _chip.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(1.15f, 0.28f, u));
                    _chip.color = Fade(_chipC, 1f - Mathf.Clamp01(u / 0.38f));
                }

                if (_slashes != null && _slashMax != null && _slashThick != null && _slashC != null)
                {
                    var grow = Mathf.Sqrt(u);
                    var n = Mathf.Min(_slashes.Length, Mathf.Min(_slashMax.Length, Mathf.Min(_slashThick.Length, _slashC.Length)));
                    for (int i = 0; i < n; i++)
                    {
                        var img = _slashes[i];
                        if (img == null) continue;
                        var len = Mathf.Lerp(18f, _slashMax[i], grow);
                        var thick = Mathf.Lerp(_slashThick[i], _slashThick[i] * 0.35f, u);
                        img.rectTransform.sizeDelta = new Vector2(len, Mathf.Max(2f, thick));
                        var a = 0.92f * (1f - u);
                        if (u < 0.12f) a *= u / 0.12f;
                        img.color = Fade(_slashC[i], a);
                    }
                }

                if (_lines != null && _lineMax != null && _lineC != null)
                {
                    var grow = Mathf.Clamp01(u * 1.25f);
                    grow = 1f - (1f - grow) * (1f - grow);
                    var n = Mathf.Min(_lines.Length, Mathf.Min(_lineMax.Length, _lineC.Length));
                    for (int i = 0; i < n; i++)
                    {
                        var img = _lines[i];
                        if (img == null) continue;
                        var len = Mathf.Lerp(12f, _lineMax[i], grow);
                        var rt = img.rectTransform;
                        rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
                        var a = 0.86f * (1f - u);
                        if (u < 0.08f) a *= u / 0.08f;
                        img.color = Fade(_lineC[i], a);
                    }
                }

                if (_clouds != null && _cloudFrom != null && _cloudTo != null && _cloudFall != null && _cloudDelay != null && _cloudC != null)
                {
                    var cs = Quant(Mathf.Lerp(0.92f, 1.35f, pop));
                    var n = Mathf.Min(_clouds.Length, Mathf.Min(_cloudFrom.Length, Mathf.Min(_cloudTo.Length, Mathf.Min(_cloudFall.Length, Mathf.Min(_cloudDelay.Length, _cloudC.Length)))));
                    for (int i = 0; i < n; i++)
                    {
                        var img = _clouds[i];
                        if (img == null) continue;
                        var t = Mathf.Clamp01((u - _cloudDelay[i]) / Mathf.Max(0.18f, 1f - _cloudDelay[i]));
                        var k = 1f - (1f - t) * (1f - t);
                        var pos = _cloudFrom[i] + _cloudTo[i] * k + new Vector2(0f, -_cloudFall[i] * t * t);
                        img.rectTransform.anchoredPosition = Snap(pos);
                        img.rectTransform.localScale = Vector3.one * cs;
                        var a = 0.78f * (1f - t);
                        if (t < 0.14f) a *= t / 0.14f;
                        img.color = Fade(_cloudC[i], a);
                    }
                }

                if (_grit == null || _gritFrom == null || _gritTo == null || _gritDelay == null || _gritC == null) return;
                var gs = Quant(Mathf.Lerp(1.05f, 0.40f, u));
                var gn = Mathf.Min(_grit.Length, Mathf.Min(_gritFrom.Length, Mathf.Min(_gritTo.Length, Mathf.Min(_gritDelay.Length, _gritC.Length))));
                for (int i = 0; i < gn; i++)
                {
                    var img = _grit[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - _gritDelay[i]) / Mathf.Max(0.16f, 1f - _gritDelay[i]));
                    var k = 1f - (1f - t) * (1f - t);
                    img.rectTransform.anchoredPosition = Snap(_gritFrom[i] + _gritTo[i] * k);
                    img.rectTransform.localScale = Vector3.one * gs;
                    img.color = Fade(_gritC[i], 1f - t);
                }
            }
        }
    }
}
