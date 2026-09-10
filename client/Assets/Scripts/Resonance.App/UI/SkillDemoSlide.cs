using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 上滑技能演示（非实战）。9:16 安全循环条。
    /// 上滑→命中→数字，PlayOnce 播一次。只画 UI。
    /// SHOWTIME 可观察字是 <see cref="BattleCueCopy.SlideShowtime"/>（开演），
    /// 不是 Fever 条幅 <see cref="BattleCueCopy.FeverTime"/>。不是战斗坐标。
    /// </summary>
    public sealed class SkillDemoSlide : MonoBehaviour
    {
        public const float Aspect = 9f / 16f;
        const float Tick = 1f / 12f;
        const int SlashFrames = 5;
        const float HitAt = 3f * Tick;
        const float NumAt = 3.4f * Tick;
        const float PlayLife = 1.2f;
        const float TiltDeg = 22f;
        const float TwinOffsetDeg = -28f;

        static readonly Vector2 Caster01 = new Vector2(0.28f, 0.40f);
        static readonly Vector2 Dummy01 = new Vector2(0.72f, 0.42f);
        static readonly float[] SlashA = { 0.90f, 1f, 0.82f, 0.40f, 0.12f };
        static readonly float[] SlashSx = { 0.55f, 0.78f, 1.00f, 1.12f, 1.18f };
        static readonly float[] SlashSy = { 1.25f, 1.00f, 0.72f, 0.48f, 0.28f };

        RectTransform _stage;
        RectTransform _caster;
        RectTransform _dummy;
        RectTransform _mosaic;
        Image _glow;
        Image _platC;
        Image _platD;
        Image _bodyC;
        Image _bodyD;
        Image _headC;
        Image _blade;
        Image _halo;
        Image _main;
        Image _twin;
        Image _hitCore;
        Image _hitPlus;
        Image _hitCross;
        Image _hitRing;
        Image[] _sparks;
        Vector2[] _sparkDir;
        Text _caption;
        Text _showtime;
        Text _tag;
        Text _num;
        Text _numBack;
        Image _chip;
        Text _chipGlyph;
        Element _elem;
        string _skillName = "";
        Color _tint = VisualTokens.SlideGreen;
        float _scale = 1f;
        float _play;
        float _age;
        bool _didHit;
        bool _didNum;
        int _slashFrame = -1;
        int _amount = 1280;

        public static SkillDemoSlide Attach(RectTransform host, Element elem, string skillName)
        {
            if (host == null) return null;

            var fx = host.GetComponent<SkillDemoSlide>();
            if (fx == null) fx = host.GetComponentInChildren<SkillDemoSlide>(true);
            if (fx == null)
            {
                var go = new GameObject("skillDemoSlide", typeof(RectTransform), typeof(CanvasGroup), typeof(SkillDemoSlide));
                go.transform.SetParent(host, false);
                Stretch(go.GetComponent<RectTransform>());
                var cg = go.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                fx = go.GetComponent<SkillDemoSlide>();
                fx.Build();
            }

            fx.Bind(elem, skillName);
            return fx;
        }

        public void PlayOnce()
        {
            if (_stage == null) Build();
            if (_stage == null) return;
            _play = PlayLife;
            _age = 0f;
            _didHit = false;
            _didNum = false;
            _slashFrame = -1;
            HideShot();
            FitStage();
            ApplySlash(0);
            PunchName();
        }

        void Bind(Element elem, string skillName)
        {
            if (_stage == null) Build();
            _elem = elem;
            _skillName = skillName ?? "";
            _tint = VisualTokens.Element(elem);
            _amount = DemoAmount(_skillName);
            PaintIdle();
            if (_caption != null) _caption.text = _skillName;
            if (_chipGlyph != null) _chipGlyph.text = ElemGlyph(_elem);
        }

        void Build()
        {
            _stage = Well(transform, "stage");

            var bg = Pic(_stage, "bg", UiSprites.Pixel(), VisualTokens.BgVoid, new Vector2(0.5f, 0.5f), Vector2.zero);
            Stretch(bg.rectTransform);

            _mosaic = Pic(_stage, "strip", UiSprites.FloorMosaic(), new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0.5f, 0.42f), new Vector2(720f, 420f)).rectTransform;

            _glow = Pic(_stage, "glow", UiSprites.Soft(), Color.clear, new Vector2(0.50f, 0.40f), new Vector2(280f, 280f));

            Pic(_stage, "ground", UiSprites.Circle(), new Color(0.10f, 0.09f, 0.08f, 0.92f),
                new Vector2(0.50f, 0.22f), new Vector2(280f, 72f));

            _caster = Fighter(_stage, "caster", true);
            _dummy = Fighter(_stage, "dummy", false);

            var mid = Vector2.Lerp(Caster01, Dummy01, 0.5f);
            _halo = Pic(_stage, "halo", UiSprites.Slash(), Color.white, mid, new Vector2(160f, 64f));
            _main = Pic(_stage, "main", UiSprites.Slash(), _tint, mid, new Vector2(160f, 48f));
            _twin = Pic(_stage, "twin", UiSprites.Slash(), _tint, mid, new Vector2(120f, 28f));

            _hitCore = Pic(_stage, "hitCore", UiSprites.Soft(), _tint, Dummy01, new Vector2(96f, 96f));
            _hitPlus = Pic(_stage, "hitPlus", UiSprites.Plus(), Color.white, Dummy01, new Vector2(28f, 28f));
            _hitCross = Pic(_stage, "hitCross", UiSprites.Plus(), Color.white, Dummy01, new Vector2(22f, 22f));
            _hitCross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            _hitRing = Pic(_stage, "hitRing", UiSprites.Circle(), _tint, Dummy01, new Vector2(48f, 48f));

            _sparks = new Image[8];
            _sparkDir = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                var ang = (i / 8f) * Mathf.PI * 2f;
                _sparkDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                _sparks[i] = Pic(_stage, "spark", i % 2 == 0 ? UiSprites.Spark() : UiSprites.Pixel(),
                    _tint, Dummy01, new Vector2(10f, 10f));
            }

            _numBack = Txt(_stage, "numB", "1,280", 42, new Color(0.12f, 0.08f, 0.06f, 1f),
                Dummy01 + new Vector2(0.01f, 0.16f), new Vector2(220f, 64f));
            _num = Txt(_stage, "num", "1,280", 42, VisualTokens.TapWhite,
                Dummy01 + new Vector2(0f, 0.18f), new Vector2(220f, 64f));
            _chip = Pic(_num.rectTransform, "chip", UiSprites.Pixel(), _tint, new Vector2(1f, 0.15f), new Vector2(22f, 22f));
            _chipGlyph = Txt(_chip.rectTransform, "g", "火", 12, Color.white, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));

            _caption = Txt(_stage, "name", "", 22, VisualTokens.TextPrimary,
                new Vector2(0.50f, 0.91f), new Vector2(280f, 40f));
            _showtime = Txt(_stage, "showtime", BattleCueCopy.SlideShowtime, 20, VisualTokens.StarEvolved,
                new Vector2(0.50f, 0.845f), new Vector2(200f, 32f));
            _tag = Txt(_stage, "tag", BattleCueCopy.SlideSkill, 16, VisualTokens.SlideGreen,
                new Vector2(0.50f, 0.775f), new Vector2(160f, 32f));
            Txt(_stage, "demo", "演示", 13, VisualTokens.TextMuted,
                new Vector2(0.50f, 0.075f), new Vector2(120f, 24f));

            HideShot();
            FitStage();
        }

        RectTransform Fighter(Transform parent, string name, bool caster)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = caster ? Caster01 : Dummy01;
            rt.sizeDelta = new Vector2(96f, 160f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = new Vector3(caster ? 1f : -1f, 1f, 1f);

            var plat = Pic(rt, "plat", UiSprites.Circle(), new Color(0.12f, 0.11f, 0.10f, 0.90f),
                new Vector2(0.5f, 0.08f), new Vector2(78f, 22f));
            if (caster) _platC = plat;
            else _platD = plat;

            var legs = Pic(rt, "legs", UiSprites.Pixel(), Color.white, new Vector2(0.50f, 0.22f), new Vector2(36f, 28f));
            var body = Pic(rt, "body", UiSprites.Pixel(), Color.white, new Vector2(0.50f, 0.46f), new Vector2(44f, 48f));
            var head = Pic(rt, "head", UiSprites.Circle(), Color.white, new Vector2(0.50f, 0.74f), new Vector2(28f, 28f));
            if (caster)
            {
                _bodyC = body;
                _headC = head;
                _blade = Pic(rt, "blade", UiSprites.Pixel(), Color.white, new Vector2(0.92f, 0.52f), new Vector2(10f, 52f));
            }
            else
            {
                _bodyD = body;
                legs.color = new Color(0.22f, 0.20f, 0.22f, 1f);
                head.color = new Color(0.62f, 0.58f, 0.60f, 1f);
            }
            return rt;
        }

        void LateUpdate()
        {
            FitStage();
            LoopStrip();
            if (_play <= 0f)
            {
                IdlePose();
                return;
            }
            _age += Time.unscaledDeltaTime;
            TickPlay();
            if (_age >= _play)
            {
                _play = 0f;
                HideShot();
                RestFighters();
            }
        }

        void TickPlay()
        {
            var frame = Mathf.Clamp(Mathf.FloorToInt(_age / Tick), 0, SlashFrames);
            if (frame < SlashFrames)
            {
                if (frame != _slashFrame) ApplySlash(frame);
            }
            else if (_slashFrame >= 0) HideSlash();

            Lunge(_caster, Caster01, Dummy01, 28f * _scale, Mathf.Clamp01(_age / 0.42f));
            if (_age >= HitAt)
                Lunge(_dummy, Dummy01, Caster01, -16f * _scale, Mathf.Clamp01((_age - HitAt) / 0.28f));

            if (!_didHit && _age >= HitAt)
            {
                _didHit = true;
                ApplyHit(0f);
            }
            else if (_didHit)
                ApplyHit(Mathf.Clamp01((_age - HitAt) / 0.32f));

            if (!_didNum && _age >= NumAt)
            {
                _didNum = true;
                ShowNum(0f);
            }
            else if (_didNum)
                ShowNum(Mathf.Clamp01((_age - NumAt) / 0.70f));
        }

        void ApplySlash(int frame)
        {
            _slashFrame = frame;
            var dist = Mathf.Max(140f * _scale, Vector2.Distance(Px(Caster01), Px(Dummy01)) * 0.95f);
            var thick = 64f * _scale;
            var tilt = Ang() + TiltDeg;
            var mid = Vector2.Lerp(Caster01, Dummy01, 0.5f);
            var a = SlashA[frame];
            PlaceBlade(_halo, mid, dist, thick + 18f * _scale, tilt, Tint(Color.white, 0.95f * a),
                SlashSx[frame], SlashSy[frame]);
            PlaceBlade(_main, mid, dist, thick, tilt, Tint(_tint, a), SlashSx[frame], SlashSy[frame]);
            PlaceBlade(_twin, mid, dist * 0.72f, thick * 0.45f, tilt + TwinOffsetDeg, Tint(_tint, a),
                SlashSx[frame], SlashSy[frame]);
        }

        void ApplyHit(float u)
        {
            var pop = 1f - (1f - u) * (1f - u);
            var hot = Color.Lerp(Color.white, _tint, 0.32f);
            Place(_hitCore, Dummy01, Vector2.one * (150f * _scale), 0f, Tint(_tint, 0.92f * (1f - u)));
            if (_hitCore != null) _hitCore.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.40f, 1.55f, pop));
            Place(_hitPlus, Dummy01, Vector2.one * (36f * _scale), 0f, Tint(hot, 1f - Mathf.Clamp01(u / 0.42f)));
            Place(_hitCross, Dummy01, Vector2.one * (28f * _scale), 45f, Tint(Color.white, 1f - Mathf.Clamp01(u / 0.34f)));
            Place(_hitRing, Dummy01, Vector2.one * (72f * _scale), 0f, Tint(_tint, 0.72f * (1f - u)));
            if (_hitRing != null) _hitRing.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.45f, 2.05f, u));

            var rad = Mathf.Lerp(10f, 84f, u) * _scale;
            var w = _stage != null ? Mathf.Max(1f, _stage.rect.width) : 1f;
            var h = _stage != null ? Mathf.Max(1f, _stage.rect.height) : 1f;
            if (_sparks != null && _sparkDir != null)
            {
                for (int i = 0; i < _sparks.Length; i++)
                {
                    var img = _sparks[i];
                    if (img == null) continue;
                    var dir = i < _sparkDir.Length ? _sparkDir[i] : Vector2.zero;
                    var p = Dummy01 + new Vector2(dir.x * rad / w, dir.y * rad / h);
                    Place(img, p, Vector2.one * ((i % 2 == 0 ? 16f : 10f) * _scale), 0f,
                        Tint((i & 1) == 0 ? _tint : hot, 1f - u));
                }
            }
        }

        void ShowNum(float u)
        {
            var digits = OverlayDraw.Comma(_amount);
            if (_num != null) _num.text = digits;
            if (_numBack != null) _numBack.text = digits;
            var rise = 96f * _scale * u;
            var punch = u < 0.14f ? Mathf.Lerp(1.26f, 1.08f, u / 0.14f) : Mathf.Lerp(1.08f, 0.92f, (u - 0.14f) / 0.86f);
            var a = u < 0.46f ? 1f : 1f - (u - 0.46f) / 0.54f;
            a = Mathf.Clamp01(a);
            var basePos = Dummy01 + new Vector2(0f, 0.18f);
            var px = new Vector2(0f, rise);
            PlaceNum(_num, basePos, px, VisualTokens.TapWhite, a, punch);
            PlaceNum(_numBack, basePos, px + new Vector2(3f * _scale, -3f * _scale),
                new Color(0.12f, 0.08f, 0.06f, 1f), a, punch);
            if (_chip != null)
            {
                var c = _tint;
                c.a = a;
                _chip.color = c;
            }
            if (_chipGlyph != null)
                _chipGlyph.color = new Color(1f, 1f, 1f, a);
        }

        void LoopStrip()
        {
            if (_mosaic != null)
            {
                var shift = CombatFeel.Snap(Mathf.Repeat(Time.unscaledTime * 28f, 48f) - 24f);
                _mosaic.anchoredPosition = new Vector2(shift, 8f * _scale);
            }
            if (_glow != null)
            {
                var pulse = 0.18f + 0.10f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
                _glow.color = Tint(_tint, pulse);
            }
        }

        void IdlePose()
        {
            var beat = (int)CombatFeel.PixelBeat;
            var bob = (beat & 1) == 0 ? 0f : 4f * _scale;
            if (_caster != null)
                _caster.anchoredPosition = new Vector2(0f, CombatFeel.Snap(bob));
            if (_dummy != null)
                _dummy.anchoredPosition = new Vector2(0f, CombatFeel.Snap(-bob * 0.6f));
        }

        void PaintIdle()
        {
            if (_glow != null) _glow.color = Tint(_tint, 0.22f);
            if (_platC != null) _platC.color = new Color(_tint.r, _tint.g, _tint.b, 0.85f);
            if (_platD != null) _platD.color = new Color(0.28f, 0.16f, 0.16f, 0.90f);
            if (_bodyC != null) _bodyC.color = Color.Lerp(_tint, VisualTokens.TextPrimary, 0.18f);
            if (_headC != null) _headC.color = new Color(0.94f, 0.88f, 0.82f, 1f);
            if (_blade != null) _blade.color = Color.Lerp(_tint, Color.white, 0.35f);
            if (_bodyD != null) _bodyD.color = new Color(0.32f, 0.30f, 0.34f, 1f);
            if (_showtime != null)
            {
                _showtime.text = BattleCueCopy.SlideShowtime;
                _showtime.color = VisualTokens.StarEvolved;
            }
            if (_tag != null)
            {
                _tag.text = BattleCueCopy.SlideSkill;
                _tag.color = VisualTokens.SlideGreen;
            }
            if (_chipGlyph != null) _chipGlyph.text = ElemGlyph(_elem);
        }

        void PunchName()
        {
            if (_showtime != null)
            {
                _showtime.text = BattleCueCopy.SlideShowtime;
                _showtime.color = VisualTokens.StarEvolved;
                _showtime.rectTransform.localScale = Vector3.one * 1.12f;
            }
            if (_tag != null)
            {
                _tag.text = BattleCueCopy.SlideSkill;
                _tag.color = VisualTokens.SlideGreen;
                _tag.rectTransform.localScale = Vector3.one * 1.12f;
            }
            if (_caption != null)
            {
                _caption.text = _skillName ?? "";
                _caption.color = VisualTokens.TextPrimary;
            }
        }

        void HideShot()
        {
            HideSlash();
            Fade(_hitCore);
            Fade(_hitPlus);
            Fade(_hitCross);
            Fade(_hitRing);
            if (_sparks != null)
                for (int i = 0; i < _sparks.Length; i++) Fade(_sparks[i]);
            FadeTxt(_num);
            FadeTxt(_numBack);
            Fade(_chip);
            FadeTxt(_chipGlyph);
            if (_showtime != null) _showtime.rectTransform.localScale = Vector3.one;
            if (_tag != null) _tag.rectTransform.localScale = Vector3.one;
        }

        void HideSlash()
        {
            Fade(_halo);
            Fade(_main);
            Fade(_twin);
            RestBlade(_halo);
            RestBlade(_main);
            RestBlade(_twin);
            _slashFrame = -1;
        }

        void RestFighters()
        {
            if (_caster != null)
            {
                _caster.anchorMin = _caster.anchorMax = Caster01;
                _caster.anchoredPosition = Vector2.zero;
            }
            if (_dummy != null)
            {
                _dummy.anchorMin = _dummy.anchorMax = Dummy01;
                _dummy.anchoredPosition = Vector2.zero;
            }
        }

        void FitStage()
        {
            if (_stage == null) return;
            var host = (RectTransform)transform;
            var r = host.rect;
            var aw = Mathf.Max(8f, r.width);
            var ah = Mathf.Max(8f, r.height);
            float w, h;
            if (aw / ah > Aspect)
            {
                h = ah;
                w = ah * Aspect;
            }
            else
            {
                w = aw;
                h = aw / Aspect;
            }
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(Mathf.Round(w), Mathf.Round(h));
            _stage.anchoredPosition = Vector2.zero;
            _scale = Mathf.Clamp(Mathf.Min(w / 720f, h / 1280f), 0.22f, 1.15f);

            if (_caster != null) _caster.sizeDelta = new Vector2(96f * _scale, 160f * _scale);
            if (_dummy != null) _dummy.sizeDelta = new Vector2(88f * _scale, 148f * _scale);
            if (_glow != null) _glow.rectTransform.sizeDelta = new Vector2(280f * _scale, 280f * _scale);
            if (_mosaic != null) _mosaic.sizeDelta = new Vector2(Mathf.Max(w * 1.4f, 420f), Mathf.Max(h * 0.42f, 180f));
            if (_caption != null) _caption.fontSize = Mathf.Clamp(Mathf.RoundToInt(22f * _scale), 12, 28);
            if (_showtime != null) _showtime.fontSize = Mathf.Clamp(Mathf.RoundToInt(20f * _scale), 12, 24);
            if (_tag != null) _tag.fontSize = Mathf.Clamp(Mathf.RoundToInt(16f * _scale), 11, 20);
            if (_num != null) _num.fontSize = Mathf.Clamp(Mathf.RoundToInt(42f * _scale), 16, 48);
            if (_numBack != null) _numBack.fontSize = _num != null ? _num.fontSize : 42;
        }

        void Lunge(RectTransform rt, Vector2 from, Vector2 to, float px, float u)
        {
            if (rt == null || _stage == null) return;
            var k = u < 0.55f ? u / 0.55f : 1f - (u - 0.55f) / 0.45f;
            k = Mathf.Clamp01(k);
            var dir = (to - from).normalized;
            var w = Mathf.Max(1f, _stage.rect.width);
            var hgt = Mathf.Max(1f, _stage.rect.height);
            rt.anchorMin = rt.anchorMax = from + new Vector2(dir.x * px * k / w, dir.y * px * k / hgt);
            rt.anchoredPosition = Vector2.zero;
        }

        void PlaceNum(Text tx, Vector2 anchor, Vector2 pos, Color color, float a, float punch)
        {
            if (tx == null) return;
            var rt = tx.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(220f * _scale, 64f * _scale);
            rt.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            rt.localScale = Vector3.one * punch;
            color.a = a;
            tx.color = color;
        }

        static void Place(Image img, Vector2 anchor, Vector2 size, float ang, Color color)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, ang);
            img.color = color;
        }

        static void PlaceBlade(Image img, Vector2 mid, float dist, float thick, float ang, Color color, float sx, float sy)
        {
            Place(img, mid, new Vector2(dist, thick), ang, color);
            if (img != null)
                img.rectTransform.localScale = new Vector3(Quant(sx), Quant(sy), 1f);
        }

        static void RestBlade(Image img)
        {
            if (img != null) img.rectTransform.localScale = Vector3.one;
        }

        Vector2 Px(Vector2 a01)
        {
            if (_stage == null) return a01;
            var r = _stage.rect;
            return new Vector2(a01.x * r.width, a01.y * r.height);
        }

        float Ang()
        {
            var a = Px(Caster01);
            var b = Px(Dummy01);
            var d = b - a;
            return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }

        static float Quant(float v)
        {
            return Mathf.Round(v * 8f) / 8f;
        }

        static Color Tint(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static void Fade(Image img)
        {
            if (img == null) return;
            img.canvasRenderer.cullTransparentMesh = false;
            img.color = Color.clear;
        }

        static void FadeTxt(Text tx)
        {
            if (tx == null) return;
            tx.canvasRenderer.cullTransparentMesh = false;
            var c = tx.color;
            c.a = 0f;
            tx.color = c;
        }

        static int DemoAmount(string name)
        {
            var h = 1280;
            if (string.IsNullOrEmpty(name)) return h;
            for (int i = 0; i < name.Length; i++)
                h = h * 33 + name[i];
            return 860 + Mathf.Abs(h % 2400);
        }

        static string ElemGlyph(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "火";
                case Element.Water: return "水";
                case Element.Wood: return "木";
                case Element.Light: return "光";
                default: return "暗";
            }
        }

        static RectTransform Well(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 640f);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFill;
            img.raycastTarget = false;
            go.GetComponent<Mask>().showMaskGraphic = true;
            return rt;
        }

        static Image Pic(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            img.canvasRenderer.cullTransparentMesh = false;
            return img;
        }

        static Text Txt(Transform parent, string name, string s, int size, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = s ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            tx.canvasRenderer.cullTransparentMesh = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
