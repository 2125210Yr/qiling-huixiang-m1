using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// KO: fighter flashes, then collapses with a printed smash (soft shock + slash).
    /// Does not destroy fighterUi; battle code owns that GameObject.
    /// </summary>
    public sealed class VfxKo : MonoBehaviour
    {
        const float FlashLen = 0.22f;
        const float CollapseLen = 0.55f;
        const float Life = FlashLen + CollapseLen;

        Transform _fighter;
        RectTransform _pose;
        CanvasGroup _group;
        Graphic[] _gfx;
        Color[] _gfxColor;
        Vector3 _poseScale;
        Vector2 _posePos;
        Vector3 _poseEuler;
        Image _flash;
        Image _shock;
        Image _slash;
        Color _tint;
        float _face = 1f;
        float _groupA = 1f;
        float _age;
        bool _dust;
        bool _done;

        public static void Play(Transform fighterUi, Color tint)
        {
            if (fighterUi == null) return;
            if (tint.a < 0.05f) tint.a = 1f;

            var old = fighterUi.GetComponentsInChildren<VfxKo>(true);
            for (int i = 0; i < old.Length; i++)
            {
                var fx = old[i];
                if (fx == null) continue;
                fx.Restore();
                // Overlay host only — never Destroy the fighter GameObject.
                if (fx.transform != fighterUi)
                    Object.DestroyImmediate(fx.gameObject);
                else Object.DestroyImmediate(fx);
            }

            var go = new GameObject("vfxKo", typeof(RectTransform), typeof(VfxKo));
            go.transform.SetParent(fighterUi, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            var created = go.GetComponent<VfxKo>();
            if (created == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            created.Begin(fighterUi, tint);
        }

        void Begin(Transform fighter, Color tint)
        {
            if (fighter == null)
            {
                KillSelf();
                return;
            }
            _fighter = fighter;
            _tint = new Color(tint.r, tint.g, tint.b, 1f);

            _pose = FindPose(fighter);
            if (_pose != null)
            {
                _poseScale = _pose.localScale;
                _posePos = _pose.anchoredPosition;
                _poseEuler = _pose.localEulerAngles;
                _face = _poseScale.x < 0f ? -1f : 1f;
            }

            _group = fighter.GetComponent<CanvasGroup>();
            if (_group != null) _groupA = _group.alpha;

            var all = fighter.GetComponentsInChildren<Graphic>(true);
            var n = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (SkipSnap(all[i])) continue;
                n++;
            }
            _gfx = new Graphic[n];
            _gfxColor = new Color[n];
            n = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var g = all[i];
                if (SkipSnap(g)) continue;
                _gfx[n] = g;
                _gfxColor[n] = g.color;
                n++;
            }

            _flash = Pic(transform, "flash", UiSprites.Soft(), Color.clear);
            if (_flash != null)
            {
                var frt = _flash.rectTransform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = frt.offsetMax = Vector2.zero;
            }

            _shock = Pic(transform, "shock", UiSprites.Soft(), Color.clear);
            if (_shock != null)
            {
                var srt = _shock.rectTransform;
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.55f);
                srt.sizeDelta = new Vector2(150f, 150f);
                srt.anchoredPosition = Vector2.zero;
            }

            _slash = Pic(transform, "slash", UiSprites.Slash(), Color.clear);
            if (_slash != null)
            {
                var lrt = _slash.rectTransform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.55f);
                lrt.sizeDelta = new Vector2(280f, 84f);
                lrt.anchoredPosition = Vector2.zero;
                lrt.localEulerAngles = new Vector3(0f, 0f, _face * -18f);
            }

            CanvasShake.Punch(14f, 0.22f);
            Apply(0f);
        }

        void Update()
        {
            if (_done) return;
            if (_fighter == null)
            {
                KillSelf();
                return;
            }

            _age += Time.unscaledDeltaTime;
            if (_age >= Life)
            {
                Apply(1f);
                KillSelf();
                return;
            }

            Apply(Mathf.Clamp01(_age / Life));
        }

        void Apply(float u)
        {
            var flashEnd = FlashLen / Life;
            var inFlash = u < flashEnd;
            var fall = inFlash ? 0f : Mathf.Clamp01((u - flashEnd) / Mathf.Max(0.01f, 1f - flashEnd));
            var ease = fall * fall;
            var pulse = inFlash ? 0.40f + 0.60f * (0.5f + 0.5f * Mathf.Sin(_age * 38f)) : 0f;

            if (_flash != null)
            {
                var hot = Color.Lerp(Color.white, _tint, inFlash && u > flashEnd * 0.45f ? 0.62f : 0.12f);
                _flash.color = new Color(hot.r, hot.g, hot.b, pulse * 0.88f);
            }

            if (_shock != null)
            {
                if (inFlash)
                {
                    _shock.color = Color.clear;
                    _shock.transform.localScale = Vector3.one * 0.35f;
                }
                else
                {
                    var k = Mathf.Clamp01(fall / 0.55f);
                    _shock.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 2.55f, 1f - (1f - k) * (1f - k));
                    _shock.color = new Color(_tint.r, _tint.g, _tint.b, 0.80f * (1f - k));
                }
            }

            if (_slash != null)
            {
                if (inFlash)
                {
                    _slash.color = Color.clear;
                    _slash.transform.localScale = new Vector3(0.35f, 1.15f, 1f);
                }
                else
                {
                    var k = Mathf.Clamp01(fall / 0.42f);
                    _slash.transform.localScale = new Vector3(Mathf.Lerp(0.42f, 1.18f, k), 1f, 1f);
                    _slash.color = Color.Lerp(Color.white, _tint, 0.35f) * new Color(1f, 1f, 1f, 0.92f * (1f - k));
                }
            }

            if (_pose != null)
            {
                var ax = Mathf.Abs(_poseScale.x);
                var ay = Mathf.Abs(_poseScale.y);
                var sx = Mathf.Lerp(ax, ax * 0.82f, ease);
                var sy = Mathf.Lerp(ay, ay * 0.62f, ease);
                _pose.localScale = new Vector3(_face * sx, sy, _poseScale.z);
                _pose.anchoredPosition = _posePos + new Vector2(_face * 16f * ease, -40f * ease);
                _pose.localEulerAngles = _poseEuler + new Vector3(0f, 0f, _face * -26f * ease);
            }

            var fade = inFlash ? 1f : Mathf.Lerp(1f, 0.22f, ease);
            if (_group != null)
                _group.alpha = _groupA * fade;

            if (_gfx != null && _gfxColor != null)
            {
                var blink = Color.Lerp(Color.white, _tint, 0.40f);
                var n = Mathf.Min(_gfx.Length, _gfxColor.Length);
                for (int i = 0; i < n; i++)
                {
                    var g = _gfx[i];
                    if (g == null) continue;
                    var c = _gfxColor[i];
                    if (inFlash)
                    {
                        c = Color.Lerp(c, blink, pulse);
                        c.a = _gfxColor[i].a;
                    }
                    else if (_group == null)
                    {
                        c.a = _gfxColor[i].a * fade;
                    }
                    g.color = c;
                }
            }

            if (!inFlash && !_dust && _fighter != null)
            {
                _dust = true;
                const int n = 10;
                for (int i = 0; i < n; i++)
                {
                    var ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.20f, 0.20f);
                    Spark.Spawn(_fighter, (i & 1) == 0 ? _tint : Color.white, ang, 72f, false);
                }
            }
        }

        void Restore()
        {
            _done = true;
            if (_pose != null)
            {
                _pose.localScale = _poseScale;
                _pose.anchoredPosition = _posePos;
                _pose.localEulerAngles = _poseEuler;
            }
            if (_group != null) _group.alpha = _groupA;
            if (_gfx == null || _gfxColor == null) return;
            var n = Mathf.Min(_gfx.Length, _gfxColor.Length);
            for (int i = 0; i < n; i++)
            {
                if (_gfx[i] == null) continue;
                _gfx[i].color = _gfxColor[i];
            }
        }

        void KillSelf()
        {
            _done = true;
            if (_fighter != null && transform == _fighter) Destroy(this);
            else Destroy(gameObject);
        }

        static bool SkipSnap(Graphic g)
        {
            return g == null || g.GetComponentInParent<VfxKo>() != null;
        }

        static RectTransform FindPose(Transform fighter)
        {
            for (int i = 0; i < fighter.childCount; i++)
            {
                var c = fighter.GetChild(i);
                if (c.name == "Presenter" || c.name == "pixel")
                {
                    var rt = c as RectTransform;
                    if (rt != null) return rt;
                }
            }
            return fighter as RectTransform;
        }

        static Image Pic(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static float Quant(float v)
        {
            return Mathf.Round(v * 8f) / 8f;
        }

        static float Band(float a)
        {
            return Mathf.Round(Mathf.Clamp01(a) * 8f) / 8f;
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        }
    }
}
