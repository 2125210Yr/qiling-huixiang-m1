using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Resonance.Battle;

namespace Resonance.App
{
    public sealed class CanvasShake : MonoBehaviour
    {
        public static CanvasShake Live { get; private set; }

        float _t;
        float _mag;

        void OnEnable() => Live = this;

        void OnDisable()
        {
            if (Live == this) Live = null;
        }

        public static void Punch(float mag, float dur = 0.20f)
        {
            if (Live == null) return;
            Live._mag = Mathf.Max(Live._mag, mag);
            Live._t = Mathf.Max(Live._t, dur);
        }

        void LateUpdate()
        {
            if (_t <= 0f)
            {
                transform.localPosition = Vector3.zero;
                _mag = 0f;
                return;
            }
            _t -= Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(_t / 0.20f);
            var m = _mag * k;
            var h = (int)CombatFeel.PixelBeat * 1103515245;
            transform.localPosition = new Vector3(
                CombatFeel.Snap((((h >> 16) & 7) - 3.5f) / 3.5f * m),
                CombatFeel.Snap((((h >> 20) & 7) - 3.5f) / 3.5f * m),
                0f);
        }
    }

    public static class CombatFeel
    {
        public static float PixelBeat => Mathf.Floor(Time.unscaledTime * 12f);

        public static float Snap(float v)
        {
            return Mathf.Round(v / 4f) * 4f;
        }

        public static float Step(float u, int frames)
        {
            if (frames <= 1) return Mathf.Clamp01(u);
            return Mathf.Floor(Mathf.Clamp01(u) * frames) / frames;
        }

        public static Color FeverHue(float t)
        {
            var u = Mathf.Repeat(t, 1f);
            if (u < 0.33f)
                return Color.Lerp(new Color(0.78f, 0.28f, 0.73f), VisualTokens.FeverGold, u / 0.33f);
            if (u < 0.66f)
                return Color.Lerp(VisualTokens.FeverGold, VisualTokens.ElemDark, (u - 0.33f) / 0.33f);
            return Color.Lerp(VisualTokens.ElemDark, new Color(0.78f, 0.28f, 0.73f), (u - 0.66f) / 0.34f);
        }

        public static void PlayCue(PresentationCue cue, Transform parent, BattleFighter focus)
        {
            if (parent == null) return;
            // Cast / Hit channel FX live in VfxRouter so Tap / Slide / Drive / Fever
            // stay distinct. This path only tints Fever. Never waits on animation.
            if (cue.Kind == PresentationCueKind.Fever)
                ScreenTint(parent, new Color(0.78f, 0.28f, 0.73f, 1f), 0.08f, 0.22f);
        }

        public static void Impact(Transform parent, Vector2 anchor, Color color, bool crit, bool fever)
        {
            Impact(parent, anchor, color, crit, fever, SkillType.Tap);
        }

        public static void Impact(Transform parent, Vector2 anchor, Color color, bool crit, bool fever, SkillType kind)
        {
            if (parent == null) return;
            var size = fever ? 168f : kind == SkillType.Drive ? 200f : kind == SkillType.Slide ? 170f : crit ? 150f : 110f;
            var go = new GameObject("impact", typeof(RectTransform), typeof(ImpactBurst));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();

            // No giant Soft disc over the enemy: small star flash + faint tight ring; sparks carry the hit.
            var flash = Child(go.transform, "flash", UiSprites.Star(), new Color(1f, 1f, 1f, 0.95f), size * 0.36f);
            var ring = Child(go.transform, "ring", UiSprites.Circle(), new Color(1f, 1f, 1f, 0.40f), size * 0.28f);
            var burst = go.GetComponent<ImpactBurst>();
            if (burst != null)
                burst.Bind(flash, ring, fever ? 0.52f : kind == SkillType.Drive ? 0.44f : 0.34f,
                    crit || fever || kind == SkillType.Drive, false);

            var n = fever ? 18 : kind == SkillType.Drive ? 14 : kind == SkillType.Slide ? 11 : crit ? 10 : 7;
            var dist = fever ? 148f : kind == SkillType.Drive ? 118f : kind == SkillType.Slide ? 96f : 64f;
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                Spark.Spawn(go.transform, color, ang, dist, false);
            }
            if (!fever && kind != SkillType.Drive) return;
            for (int i = 0; i < 8; i++)
            {
                var ang = (i / 8f) * Mathf.PI * 2f + 0.4f;
                Spark.Spawn(go.transform, Color.Lerp(color, Color.white, 0.4f), ang, dist * 0.72f, false, true);
            }
        }

        public static void Slash(Transform parent, Vector2 from, Vector2 to, Color color, SkillType kind, bool fever)
        {
            if (parent == null) return;
            var go = new GameObject("slash", typeof(RectTransform), typeof(SlashArc));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var canvas = CanvasPx(parent);
            var mid = (from + to) * 0.5f;
            var a = new Vector2(from.x * canvas.x, from.y * canvas.y);
            var b = new Vector2(to.x * canvas.x, to.y * canvas.y);
            var delta = b - a;
            var tap = !fever && (kind == SkillType.Tap || kind == SkillType.Auto);
            var dist = Mathf.Max(tap ? 96f : 140f, delta.magnitude * (tap ? 0.88f : 0.95f));
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var thick = fever ? 96f : kind == SkillType.Drive ? 88f : kind == SkillType.Slide ? 64f
                : kind == SkillType.Tap ? 36f : 24f;
            var tilt = kind == SkillType.Slide ? 22f : kind == SkillType.Drive ? -8f : tap ? -10f : -14f;

            Blade(go.transform, mid, dist, thick + (tap ? 10f : 18f), ang + tilt, new Color(1f, 1f, 1f, 0.95f), false);
            var img = Blade(go.transform, mid, dist, thick, ang + tilt, new Color(color.r, color.g, color.b, 1f), false);
            if (kind == SkillType.Tap || kind == SkillType.Slide)
                Blade(go.transform, mid, dist * 0.72f, thick * 0.42f, ang + tilt - 28f, color, false);
            if (kind == SkillType.Drive || fever)
            {
                Blade(go.transform, mid, dist * 0.80f, thick * 0.38f, ang + tilt + 26f, color, false);
                Blade(go.transform, mid, dist * 0.55f, thick * 0.22f, ang + tilt - 18f, new Color(1f, 0.92f, 0.45f, 1f), false);
            }
            var arc = go.GetComponent<SlashArc>();
            if (arc != null) arc.Bind(img, kind == SkillType.Drive || fever ? 0.42f : tap ? 0.24f : 0.28f);
        }

        public static void CastBurst(Transform parent, Vector2 anchor, Color color, SkillType kind, bool fever)
        {
            if (parent == null) return;
            if (!fever && kind == SkillType.Auto)
            {
                var go = new GameObject("autoburst", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = anchor;
                rt.sizeDelta = Vector2.zero;
                rt.SetAsLastSibling();
                Child(go.transform, "glow", UiSprites.Soft(), new Color(color.r, color.g, color.b, 0.70f), 96f);
                Child(go.transform, "ring", UiSprites.Circle(), new Color(1f, 1f, 1f, 0.80f), 48f);
                for (int i = 0; i < 6; i++)
                    Spark.Spawn(go.transform, color, (i / 6f) * Mathf.PI * 2f, 52f, false);
                Object.Destroy(go, 0.32f);
                return;
            }
            Impact(parent, anchor, color, kind == SkillType.Drive, fever, kind);
            var a = anchor + new Vector2(-0.08f, -0.04f);
            var b = anchor + new Vector2(0.10f, 0.12f);
            Slash(parent, a, b, color, kind, fever);
        }

        public static void HealBurst(Transform parent, Vector2 anchor, Color color)
        {
            if (parent == null) return;
            var go = new GameObject("heal", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            for (int i = 0; i < 6; i++)
            {
                var ang = (i / 6f) * Mathf.PI * 2f;
                Spark.Spawn(go.transform, color, ang, 70f, false, true);
            }
            Object.Destroy(go, 0.55f);
        }

        public static void NamePop(Transform parent, Vector2 anchor, string text, Color color)
        {
            if (parent == null || string.IsNullOrEmpty(text)) return;
            // Printed skill-name stamp: outlined type + thin slash underline, compact root.
            const float fs = 46f;
            float est = 0f;
            for (int i = 0; i < text.Length; i++)
                est += text[i] > 0x00FF ? fs : fs * 0.56f;
            var lineW = Mathf.Clamp(est * 0.94f, 84f, 400f);

            var go = new GameObject("skillname", typeof(RectTransform), typeof(NamePopFx));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor + new Vector2(0f, 0.10f);
            rt.sizeDelta = new Vector2(Mathf.Clamp(est + 48f, 132f, 440f), 64f);
            rt.SetAsLastSibling();

            var under = new GameObject("under", typeof(RectTransform), typeof(Image));
            under.transform.SetParent(go.transform, false);
            var urt = under.GetComponent<RectTransform>();
            urt.anchorMin = urt.anchorMax = new Vector2(0.5f, 0.5f);
            urt.sizeDelta = new Vector2(lineW + 10f, 9f);
            urt.anchoredPosition = new Vector2(0f, -23f);
            urt.localEulerAngles = new Vector3(0f, 0f, -5f);
            var uimg = under.GetComponent<Image>();
            if (uimg != null)
            {
                UiSprites.Apply(uimg, UiSprites.Slash());
                uimg.color = new Color(0f, 0f, 0f, 0.80f * color.a);
                uimg.raycastTarget = false;
            }

            var line = new GameObject("line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(go.transform, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(lineW, 5f);
            lrt.anchoredPosition = new Vector2(0f, -23f);
            lrt.localEulerAngles = new Vector3(0f, 0f, -5f);
            var limg = line.GetComponent<Image>();
            if (limg != null)
            {
                UiSprites.Apply(limg, UiSprites.Slash());
                limg.color = new Color(color.r, color.g, color.b, 0.95f * color.a);
                limg.raycastTarget = false;
            }

            var txGo = new GameObject("tx", typeof(RectTransform), typeof(Text), typeof(Outline));
            txGo.transform.SetParent(go.transform, false);
            var trt = txGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = new Vector2(0f, 7f);
            var tx = txGo.GetComponent<Text>();
            if (tx == null) { Object.Destroy(go); return; }
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.fontSize = (int)fs;
            tx.fontStyle = FontStyle.Bold;
            tx.text = text;
            tx.color = color;
            var ol = txGo.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2.2f, -2.2f);
            }
            var ol2 = txGo.AddComponent<Outline>();
            if (ol2 != null)
            {
                ol2.effectColor = Color.black;
                ol2.effectDistance = new Vector2(4.4f, -4.4f);
            }
            var pop = go.GetComponent<NamePopFx>();
            if (pop != null) pop.Bind(tx);
        }

        public static void ScreenTint(Transform parent, Color color, float alpha, float life)
        {
            if (parent == null) return;
            var go = new GameObject("tint", typeof(RectTransform), typeof(Image), typeof(TintFade));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Pixel());
                img.color = new Color(color.r, color.g, color.b, alpha);
                img.raycastTarget = false;
            }
            var fade = go.GetComponent<TintFade>();
            if (fade != null) fade.Bind(img, life);
        }

        public static void DriveCrush(Transform parent, Vector2 anchor)
        {
            if (parent == null) return;
            // No white-out tint; just a faint warm flash so the field stays readable.
            ScreenTint(parent, VisualTokens.DriveOrange, 0.14f, 0.16f);
            var a = anchor + new Vector2(-0.18f, -0.12f);
            var b = anchor + new Vector2(0.20f, 0.16f);
            Slash(parent, a, b, VisualTokens.FeverGold, SkillType.Drive, false);
            var go = new GameObject("crushStar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            for (int i = 0; i < 8; i++)
                Spark.Spawn(go.transform, VisualTokens.DriveOrange, (i / 8f) * Mathf.PI * 2f, 148f, false);
            for (int i = 0; i < 4; i++)
                Spark.Spawn(go.transform, VisualTokens.FeverGold, (i / 4f) * Mathf.PI * 2f + 0.35f, 92f, false, true);
            Object.Destroy(go, 0.46f);
        }

        public static void FeverRipple(Transform parent, Vector2 anchor)
        {
            if (parent == null) return;
            var go = new GameObject("feverRipple", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            for (int i = 0; i < 12; i++)
                Spark.Spawn(go.transform, FeverHue(i / 12f), (i / 12f) * Mathf.PI * 2f, 156f, false);
            for (int i = 0; i < 6; i++)
                Spark.Spawn(go.transform, VisualTokens.FeverGold, (i / 6f) * Mathf.PI * 2f + 0.2f, 72f, false, true);
            Object.Destroy(go, 0.50f);
        }

        public static void NamePopStack(Transform parent, Vector2 anchor, string text, Color color)
        {
            // One crisp pop + one faint misregistration ghost behind it.
            NamePop(parent, anchor + new Vector2(0.008f, 0.014f), text, new Color(color.r, color.g, color.b, 0.30f));
            NamePop(parent, anchor, text, color);
        }

        static Vector2 CanvasPx(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();
            if (rt == null) return new Vector2(756f, 1344f);
            var r = rt.rect;
            return new Vector2(Mathf.Max(360f, r.width), Mathf.Max(640f, r.height));
        }

        static Image Blade(Transform parent, Vector2 mid, float dist, float thick, float ang, Color color, bool pixel = false)
        {
            if (parent == null) return null;
            var blade = new GameObject("blade", typeof(RectTransform), typeof(Image));
            blade.transform.SetParent(parent, false);
            var brt = blade.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = mid;
            brt.sizeDelta = new Vector2(dist, thick);
            brt.anchoredPosition = Vector2.zero;
            brt.localEulerAngles = new Vector3(0f, 0f, ang);
            var img = blade.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, pixel ? UiSprites.Pixel() : UiSprites.Slash());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            if (parent == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }

    public sealed class ImpactBurst : MonoBehaviour
    {
        Image _core;
        Image _ring;
        float _coreA = 0.90f;
        float _ringA = 0.70f;
        float _life = 0.28f;
        float _age;
        bool _big;
        bool _pixel;

        public void Bind(Image core, Image ring, float life, bool big)
        {
            Bind(core, ring, life, big, false);
        }

        public void Bind(Image core, Image ring, float life, bool big, bool pixel)
        {
            _core = core;
            _ring = ring;
            _life = life;
            _big = big;
            _pixel = pixel;
            if (_core != null) _coreA = _core.color.a;
            if (_ring != null) _ringA = _ring.color.a;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / _life);
            var s = Mathf.Lerp(0.35f, _big ? 2.4f : 1.7f, 1f - (1f - u) * (1f - u));
            if (_pixel) s = 0.35f + CombatFeel.Step((s - 0.35f) / (_big ? 2.05f : 1.35f), 5) * (_big ? 2.05f : 1.35f);
            transform.localScale = Vector3.one * s;
            var a = _pixel ? 1f - CombatFeel.Step(u, 4) : 1f - u;
            if (_core != null)
            {
                var c = _core.color;
                c.a = _coreA * a;
                _core.color = c;
            }
            if (_ring != null)
            {
                var rs = 0.6f + (_pixel ? CombatFeel.Step(u, 5) : u) * 1.8f;
                _ring.transform.localScale = Vector3.one * rs;
                var c = _ring.color;
                c.a = _ringA * a;
                _ring.color = c;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }

    public sealed class SlashArc : MonoBehaviour
    {
        Image _blade;
        float _life = 0.22f;
        float _age;

        public void Bind(Image blade, float life)
        {
            _blade = blade;
            _life = life;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / _life);
            var sx = Mathf.Lerp(0.55f, 1.18f, u);
            var sy = Mathf.Lerp(1.25f, 0.28f, u);
            var imgs = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                if (img == null) continue;
                img.transform.localScale = new Vector3(sx, sy, 1f);
                var c = img.color;
                c.a = Mathf.Max(0.15f, c.a) * (1f - u);
                img.color = c;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }

    public sealed class Spark : MonoBehaviour
    {
        Vector2 _dir;
        float _spd;
        Image _img;
        float _life = 0.28f;
        bool _snap;

        public static void Spawn(Transform parent, Color color, float ang, float dist)
        {
            Spawn(parent, color, ang, dist, false, false);
        }

        public static void Spawn(Transform parent, Color color, float ang, float dist, bool pixel)
        {
            Spawn(parent, color, ang, dist, pixel, false);
        }

        public static void Spawn(Transform parent, Color color, float ang, float dist, bool pixel, bool plus)
        {
            if (parent == null) return;
            var go = new GameObject("spark", typeof(RectTransform), typeof(Image), typeof(Spark));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var sz = plus ? 28f : pixel ? 14f : 22f;
            rt.sizeDelta = new Vector2(sz, plus ? 28f : sz);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, plus ? UiSprites.Star() : UiSprites.Spark());
                img.color = color;
                img.raycastTarget = false;
            }
            var fx = go.GetComponent<Spark>();
            if (fx == null) return;
            fx._img = img;
            fx._dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
            fx._spd = 3.2f + Random.value * 1.6f;
            fx._snap = pixel;
        }

        void Update()
        {
            _life -= Time.unscaledDeltaTime * _spd * 0.35f;
            var u = 1f - Mathf.Clamp01(_life / 0.28f);
            var pos = _dir * u;
            if (_snap)
                pos = new Vector2(CombatFeel.Snap(pos.x), CombatFeel.Snap(pos.y));
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = pos;
            var s = 1.1f - (_snap ? CombatFeel.Step(u, 4) : u) * 0.7f;
            transform.localScale = Vector3.one * s;
            if (_img != null)
            {
                var c = _img.color;
                c.a = _snap ? 1f - CombatFeel.Step(u, 4) : 1f - u;
                _img.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public sealed class PulseGlow : MonoBehaviour
    {
        Image _img;
        Color _base;
        float _amp = 0.18f;
        float _spd = 2.4f;
        float _phase;

        public void Seed(float amp, float spd, float phase)
        {
            _amp = amp;
            _spd = spd;
            _phase = phase;
        }

        void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null) _base = _img.color;
        }

        void Update()
        {
            if (_img == null) return;
            var k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _spd + _phase);
            var c = _base;
            c.a = _base.a * (0.72f + _amp * k);
            _img.color = c;
            transform.localScale = Vector3.one * (1f + 0.06f * k);
        }
    }

    public sealed class TorchFlicker : MonoBehaviour
    {
        Image _img;
        Color _base;
        float _phase;

        void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null) _base = _img.color;
            _phase = Random.value * 8f;
        }

        void Update()
        {
            if (_img == null) return;
            var k = 0.75f + 0.25f * Mathf.PerlinNoise(Time.unscaledTime * 7.4f, _phase);
            _img.color = new Color(_base.r, _base.g, _base.b, _base.a * k);
            transform.localScale = new Vector3(0.92f + k * 0.12f, 0.85f + k * 0.28f, 1f);
        }
    }

    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 _base = Vector3.one;
        bool _held;

        void Awake() => _base = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            _held = true;
            transform.localScale = _base * 0.92f;
        }

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e)
        {
            if (_held) Release();
        }

        void Release()
        {
            _held = false;
            transform.localScale = _base;
        }
    }

    public sealed class NamePopFx : MonoBehaviour
    {
        Text _tx;
        float _txA = 1f;
        Image[] _imgs;
        float[] _imgA;
        float _life = 0.70f;

        public void Bind(Text tx)
        {
            _tx = tx;
            if (_tx != null) _txA = _tx.color.a;
            _imgs = GetComponentsInChildren<Image>(true);
            if (_imgs == null) return;
            _imgA = new float[_imgs.Length];
            for (int i = 0; i < _imgs.Length; i++)
                _imgA[i] = _imgs[i] != null ? _imgs[i].color.a : 0f;
        }

        void Update()
        {
            _life -= Time.unscaledDeltaTime;
            var u = 1f - Mathf.Clamp01(_life / 0.70f);
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = new Vector2(0f, 40f * u);
            transform.localScale = Vector3.one * Mathf.Lerp(1.55f, 0.96f, u);
            var fade = 1f - u * u;
            if (_tx != null)
            {
                var c = _tx.color;
                c.a = _txA * fade;
                _tx.color = c;
            }
            if (_imgs != null)
            {
                for (int i = 0; i < _imgs.Length; i++)
                {
                    var img = _imgs[i];
                    if (img == null) continue;
                    var c = img.color;
                    c.a = _imgA[i] * fade;
                    img.color = c;
                }
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public sealed class TintFade : MonoBehaviour
    {
        Image _img;
        float _life = 0.28f;
        float _age;
        float _a;

        public void Bind(Image img, float life)
        {
            _img = img;
            _life = life;
            _a = img != null ? img.color.a : 0.35f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            if (_img != null)
            {
                var c = _img.color;
                c.a = _a * (1f - u);
                _img.color = c;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
