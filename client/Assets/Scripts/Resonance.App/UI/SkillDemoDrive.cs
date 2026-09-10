using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 驱动碾压演示（非实战判定）。9:16 安全。
    /// PlayOnce：就绪？ 斩→碾木桩→完美 / 伤害 150%。
    /// QTE 可观察字：PERFECT = <see cref="BattleCueCopy.QtePerfect"/>，
    /// GREAT = <see cref="BattleCueCopy.QteGreat"/>（本条只演完美）。不是战斗坐标。
    /// </summary>
    public sealed class SkillDemoDrive : MonoBehaviour
    {
        public const float Aspect = 9f / 16f;
        const float PlayLife = 1.80f;
        const float ReadyLen = 0.42f;
        const float SlashAt = 0.14f;
        const float SlashLen = 0.36f;
        const float CrushAt = 0.46f;
        const float CrushLen = 0.52f;
        const float PerfectAt = 0.90f;
        const float DmgAt = 1.04f;
        const float ChipLen = 0.76f;

        static readonly Vector2 Caster01 = new Vector2(0.28f, 0.40f);
        static readonly Vector2 Dummy01 = new Vector2(0.72f, 0.42f);
        static readonly Color PerfectCol = new Color(0.78f, 0.55f, 1f, 1f);
        static readonly Color PerfectInk = new Color(0.28f, 0.08f, 0.42f, 1f);
        static readonly Color DmgInk = new Color(0.18f, 0.16f, 0.14f, 1f);
        static readonly Color ChipBg = new Color(0.05f, 0.04f, 0.06f, 0.90f);
        static readonly float[] BladeAng = { -26f, 18f, 58f };

        RectTransform _stage;
        RectTransform _caster;
        RectTransform _dummy;
        RectTransform _mosaic;
        Image _glow;
        Image _dim;
        Image _platC;
        Image _platD;
        Image _bodyC;
        Image _bodyD;
        Image _headC;
        Image _blade;
        Image _flash;
        Image _wipe;
        Image _bolt;
        Image _core;
        Image _ring;
        Image _ring2;
        Image _hitPlus;
        Image _hitCross;
        Image[] _blades;
        Image[] _sparks;
        Vector2[] _sparkDir;
        Image _gradeGlow;
        Image _gradeBg;
        Text _gradeGhost;
        Text _grade;
        Image _dmgGlow;
        Image _dmgBg;
        Text _dmgGhost;
        Text _dmg;
        Text _ready;
        Text _caption;
        Text _tag;
        string _skillName = "";
        Color _tint = VisualTokens.DriveOrange;
        Color _drive = VisualTokens.DriveOrange;
        float _scale = 1f;
        float _play;
        float _age;

        public static SkillDemoDrive Attach(RectTransform host, Element elem, string skillName)
        {
            if (host == null) return null;

            var fx = host.GetComponent<SkillDemoDrive>();
            if (fx == null) fx = host.GetComponentInChildren<SkillDemoDrive>(true);
            if (fx == null)
            {
                var go = new GameObject("skillDemoDrive", typeof(RectTransform), typeof(CanvasGroup), typeof(SkillDemoDrive));
                go.transform.SetParent(host, false);
                Stretch(go.GetComponent<RectTransform>());
                var cg = go.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                fx = go.GetComponent<SkillDemoDrive>();
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
            HideShot();
            FitStage();
            PunchName();
        }

        void Bind(Element elem, string skillName)
        {
            if (_stage == null) Build();
            _skillName = skillName ?? "";
            _tint = VisualTokens.Element(elem);
            _drive = VisualTokens.DriveOrange;
            PaintIdle();
            if (_caption != null) _caption.text = _skillName;
        }

        void Build()
        {
            _stage = Well(transform, "stage");

            var bg = Pic(_stage, "bg", UiSprites.Pixel(), VisualTokens.BgVoid, new Vector2(0.5f, 0.5f), Vector2.zero);
            Stretch(bg.rectTransform);

            _mosaic = Pic(_stage, "strip", UiSprites.FloorMosaic(), new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0.5f, 0.42f), new Vector2(720f, 420f)).rectTransform;

            _dim = Pic(_stage, "dim", UiSprites.Pixel(), Color.clear, new Vector2(0.5f, 0.5f), Vector2.zero);
            Stretch(_dim.rectTransform);

            _glow = Pic(_stage, "glow", UiSprites.Soft(), Color.clear, new Vector2(0.50f, 0.40f), new Vector2(280f, 280f));

            Pic(_stage, "ground", UiSprites.Circle(), new Color(0.10f, 0.09f, 0.08f, 0.92f),
                new Vector2(0.50f, 0.22f), new Vector2(280f, 72f));

            _caster = Fighter(_stage, "caster", true);
            _dummy = Fighter(_stage, "dummy", false);

            _flash = Pic(_stage, "flash", UiSprites.Soft(), Color.clear, new Vector2(0.50f, 0.48f), new Vector2(320f, 320f));
            _wipe = Pic(_stage, "wipe", UiSprites.Slash(), Color.clear, Caster01, new Vector2(220f, 36f));
            _wipe.type = Image.Type.Filled;
            _wipe.fillMethod = Image.FillMethod.Horizontal;
            _wipe.fillOrigin = (int)Image.OriginHorizontal.Left;
            _wipe.fillAmount = 0f;
            _bolt = Pic(_stage, "bolt", UiSprites.Pixel(), Color.clear, new Vector2(0.50f, 0.42f), new Vector2(280f, 10f));

            _core = Pic(_stage, "core", UiSprites.Soft(), Color.clear, Dummy01, new Vector2(160f, 160f));
            _ring = Pic(_stage, "ring", UiSprites.Circle(), Color.clear, Dummy01, new Vector2(72f, 72f));
            _ring2 = Pic(_stage, "ring2", UiSprites.Circle(), Color.clear, Dummy01, new Vector2(96f, 96f));
            _hitPlus = Pic(_stage, "hitPlus", UiSprites.Plus(), Color.clear, Dummy01, new Vector2(36f, 36f));
            _hitCross = Pic(_stage, "hitCross", UiSprites.Plus(), Color.clear, Dummy01, new Vector2(28f, 28f));
            _hitCross.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            _blades = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var blade = Pic(_stage, "blade" + i, UiSprites.Slash(), Color.clear, Dummy01, new Vector2(220f, 32f));
                blade.rectTransform.localEulerAngles = new Vector3(0f, 0f, BladeAng[i]);
                _blades[i] = blade;
            }

            _sparks = new Image[10];
            _sparkDir = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                var ang = (i / 10f) * Mathf.PI * 2f;
                _sparkDir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                _sparks[i] = Pic(_stage, "spark", i % 2 == 0 ? UiSprites.Spark() : UiSprites.Pixel(),
                    Color.clear, Dummy01, new Vector2(12f, 12f));
            }

            _gradeGlow = Pic(_stage, "gradeGlow", UiSprites.Soft(), Color.clear,
                Dummy01 + new Vector2(0f, 0.26f), new Vector2(220f, 90f));
            _gradeBg = Pic(_stage, "gradeBg", UiSprites.Pill(), Color.clear,
                Dummy01 + new Vector2(0f, 0.26f), new Vector2(180f, 52f));
            _gradeGhost = Txt(_stage, "gradeGhost", BattleCueCopy.QtePerfect, 32, PerfectInk,
                Dummy01 + new Vector2(0f, 0.26f), new Vector2(200f, 52f), true);
            _grade = Txt(_stage, "grade", BattleCueCopy.QtePerfect, 32, PerfectCol,
                Dummy01 + new Vector2(0f, 0.26f), new Vector2(200f, 52f), true);

            _dmgGlow = Pic(_stage, "dmgGlow", UiSprites.Soft(), Color.clear,
                Dummy01 + new Vector2(0f, 0.12f), new Vector2(260f, 80f));
            _dmgBg = Pic(_stage, "dmgBg", UiSprites.Pill(), Color.clear,
                Dummy01 + new Vector2(0f, 0.12f), new Vector2(220f, 48f));
            _dmgGhost = Txt(_stage, "dmgGhost", BattleCueCopy.QtePerfectSub, 24, DmgInk,
                Dummy01 + new Vector2(0f, 0.12f), new Vector2(240f, 48f), true);
            _dmg = Txt(_stage, "dmg", BattleCueCopy.QtePerfectSub, 24, VisualTokens.TapWhite,
                Dummy01 + new Vector2(0f, 0.12f), new Vector2(240f, 48f), true);

            _ready = Txt(_stage, "ready", BattleCueCopy.DriveReady, 54, Color.white,
                new Vector2(0.50f, 0.58f), new Vector2(360f, 90f), true);

            _caption = Txt(_stage, "name", "", 22, VisualTokens.TextPrimary,
                new Vector2(0.50f, 0.90f), new Vector2(280f, 40f), false);
            _tag = Txt(_stage, "tag", BattleCueCopy.DriveCast, 16, VisualTokens.DriveOrange,
                new Vector2(0.50f, 0.82f), new Vector2(160f, 32f), false);
            Txt(_stage, "demo", "演示", 13, VisualTokens.TextMuted,
                new Vector2(0.50f, 0.075f), new Vector2(120f, 24f), false);

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
            TickReady();
            TickSlash();
            TickCrush();
            TickChips();

            var lungeU = Mathf.Clamp01((_age - SlashAt) / 0.40f);
            Lunge(_caster, Caster01, Dummy01, 26f * _scale, lungeU);
            if (_age >= CrushAt)
                Lunge(_dummy, Dummy01, Caster01, -20f * _scale, Mathf.Clamp01((_age - CrushAt) / 0.28f));
        }

        void TickReady()
        {
            var u = Mathf.Clamp01(_age / ReadyLen);
            var peak = 1f - Mathf.Abs(u - 0.18f) / 0.18f;
            peak = Mathf.Clamp01(peak);
            Place(_flash, new Vector2(0.50f, 0.48f), Vector2.one * (320f * _scale), 0f, Tint(_drive, 0.72f * peak));
            if (_flash != null)
                _flash.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.55f, 1.55f, u));

            if (_dim != null)
            {
                var da = u < 0.72f ? 0.42f : 0.42f * (1f - (u - 0.72f) / 0.28f);
                if (_age >= CrushAt) da = Mathf.Max(da, 0.28f * (1f - Mathf.Clamp01((_age - CrushAt) / CrushLen)));
                _dim.color = new Color(0.28f, 0.10f, 0.02f, da);
            }

            var slam = Mathf.Clamp01(_age / 0.16f);
            var readyA = u < 0.62f ? 1f : 1f - (u - 0.62f) / 0.38f;
            if (_age >= ReadyLen) readyA = 0f;
            PlaceTxt(_ready, new Vector2(0.50f, 0.58f), Vector2.zero,
                Color.white, readyA,
                Mathf.Lerp(1.55f, 1f, 1f - (1f - slam) * (1f - slam)),
                54, new Vector2(360f, 90f));
        }

        void TickSlash()
        {
            if (_age < SlashAt)
            {
                HideSlash();
                return;
            }

            var u = Mathf.Clamp01((_age - SlashAt) / SlashLen);
            var su = CombatFeel.Step(u, 6);
            var fill = u < 0.22f ? CombatFeel.Step(u / 0.22f, 4) : 1f;
            var a = u < 0.72f ? 0.95f : 0.95f * (1f - (u - 0.72f) / 0.28f);
            var mid = Vector2.Lerp(Caster01, Dummy01, 0.55f);
            var dist = Mathf.Max(90f * _scale, Vector2.Distance(Px(Caster01), Px(Dummy01)) * 1.12f);
            Place(_wipe, mid, new Vector2(dist, 38f * _scale), Ang(), Tint(Color.Lerp(_drive, VisualTokens.FeverGold, 0.45f), a));
            if (_wipe != null)
            {
                _wipe.fillAmount = fill;
                _wipe.rectTransform.localScale = new Vector3(Mathf.Lerp(0.35f, 1.18f, Mathf.Min(1f, su * 2f)), 1.15f - su * 0.30f, 1f);
            }

            var boltA = 0.95f * (1f - CombatFeel.Step(Mathf.InverseLerp(0.55f, 1f, u), 3));
            Place(_bolt, mid, new Vector2(dist * (0.55f + su * 0.55f), 12f * _scale), Ang(),
                Tint(VisualTokens.FeverGold, boltA));
        }

        void TickCrush()
        {
            if (_age < CrushAt)
            {
                HideCrush();
                return;
            }

            var u = Mathf.Clamp01((_age - CrushAt) / CrushLen);
            var a = 1f - u * u;
            var hot = Color.Lerp(Color.white, _tint, 0.28f);
            var crush = Color.Lerp(_tint, _drive, 0.55f);

            Place(_core, Dummy01, Vector2.one * (170f * _scale), 0f, Tint(crush, 0.95f * a));
            if (_core != null)
                _core.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.28f, 2.15f, 1f - (1f - u) * (1f - u)));
            Place(_ring, Dummy01, Vector2.one * (80f * _scale), 0f, Tint(Color.white, 0.80f * a));
            if (_ring != null)
                _ring.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.35f, 3.05f, u));
            Place(_ring2, Dummy01, Vector2.one * (96f * _scale), 0f, Tint(crush, 0.55f * a));
            if (_ring2 != null)
                _ring2.rectTransform.localScale = Vector3.one * Quant(Mathf.Lerp(0.55f, 3.40f, Mathf.Clamp01(u * 1.15f)));

            Place(_hitPlus, Dummy01, Vector2.one * (40f * _scale), 0f, Tint(hot, 1f - Mathf.Clamp01(u / 0.42f)));
            Place(_hitCross, Dummy01, Vector2.one * (30f * _scale), 45f, Tint(Color.white, 1f - Mathf.Clamp01(u / 0.34f)));

            var bx = Mathf.Lerp(0.45f, 1.18f, u);
            var by = Mathf.Lerp(1.20f, 0.22f, u);
            if (_blades != null)
            {
                for (int i = 0; i < _blades.Length; i++)
                {
                    var blade = _blades[i];
                    if (blade == null) continue;
                    Place(blade, Dummy01, new Vector2(240f * _scale, (i == 0 ? 36f : 28f) * _scale), BladeAng[i], Tint(crush, 0.95f * a));
                    blade.rectTransform.localScale = new Vector3(bx, by, 1f);
                }
            }

            var rad = Mathf.Lerp(12f, 110f, u) * _scale;
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
                    Place(img, p, Vector2.one * ((i % 2 == 0 ? 18f : 11f) * _scale), 0f,
                        Tint((i & 1) == 0 ? crush : hot, 1f - u));
                }
            }
        }

        void TickChips()
        {
            TickChip(_age - PerfectAt,
                _gradeGlow, _gradeBg, _gradeGhost, _grade,
                Dummy01 + new Vector2(0f, 0.26f), PerfectCol, 1.28f, 180f, 52f, 32);
            TickChip(_age - DmgAt,
                _dmgGlow, _dmgBg, _dmgGhost, _dmg,
                Dummy01 + new Vector2(0f, 0.12f), VisualTokens.TapWhite, 1.16f, 220f, 48f, 24);
        }

        void TickChip(float t, Image glow, Image bg, Text ghost, Text tx,
            Vector2 anchor, Color accent, float punch, float bw, float bh, int font)
        {
            if (t < 0f)
            {
                Fade(glow);
                Fade(bg);
                FadeTxt(ghost);
                FadeTxt(tx);
                return;
            }

            var u = Mathf.Clamp01(t / ChipLen);
            var fade = u < 0.72f ? Gate(t) : Gate(t) * (1f - (u - 0.72f) / 0.28f);
            fade = Mathf.Clamp01(fade);
            var s = ScalePop(t, punch);
            var rise = 18f * _scale * u;
            var pos = new Vector2(0f, rise);

            if (glow != null)
            {
                Place(glow, anchor, new Vector2((bw + 40f) * _scale, (bh + 36f) * _scale), 0f, Tint(accent, 0.50f * fade));
                glow.rectTransform.anchoredPosition = pos;
                glow.rectTransform.localScale = Vector3.one * (s * 1.12f);
            }
            if (bg != null)
            {
                Place(bg, anchor, new Vector2(bw * _scale, bh * _scale), 0f, Tint(ChipBg, 0.90f * fade));
                bg.rectTransform.anchoredPosition = pos;
                bg.rectTransform.localScale = Vector3.one * s;
            }
            var dim = new Vector2(bw + 20f, bh);
            var ink = ghost != null ? new Color(ghost.color.r, ghost.color.g, ghost.color.b, 1f) : accent;
            PlaceTxt(ghost, anchor, pos + new Vector2(3f * _scale, -3f * _scale), ink, fade, s, font, dim);
            PlaceTxt(tx, anchor, pos, accent, fade, s, font, dim);
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
                _glow.color = Tint(_drive, pulse);
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
            if (_glow != null) _glow.color = Tint(_drive, 0.22f);
            if (_platC != null) _platC.color = new Color(_drive.r, _drive.g, _drive.b, 0.85f);
            if (_platD != null) _platD.color = new Color(0.28f, 0.16f, 0.16f, 0.90f);
            if (_bodyC != null) _bodyC.color = Color.Lerp(_tint, VisualTokens.TextPrimary, 0.18f);
            if (_headC != null) _headC.color = new Color(0.94f, 0.88f, 0.82f, 1f);
            if (_blade != null) _blade.color = Color.Lerp(_drive, VisualTokens.FeverGold, 0.35f);
            if (_bodyD != null) _bodyD.color = new Color(0.32f, 0.30f, 0.34f, 1f);
            if (_tag != null)
            {
                _tag.text = BattleCueCopy.DriveCast;
                _tag.color = VisualTokens.DriveOrange;
            }
        }

        void PunchName()
        {
            if (_tag != null)
            {
                _tag.text = BattleCueCopy.DriveCast;
                _tag.color = VisualTokens.DriveOrange;
                _tag.rectTransform.localScale = Vector3.one * 1.12f;
            }
            if (_caption != null)
            {
                _caption.text = _skillName ?? "";
                _caption.color = VisualTokens.TextPrimary;
            }
            if (_ready != null)
            {
                _ready.text = BattleCueCopy.DriveReady;
                _ready.rectTransform.localScale = Vector3.one * 1.55f;
            }
            if (_grade != null) _grade.text = BattleCueCopy.QtePerfect;
            if (_gradeGhost != null) _gradeGhost.text = BattleCueCopy.QtePerfect;
            if (_dmg != null) _dmg.text = BattleCueCopy.QtePerfectSub;
            if (_dmgGhost != null) _dmgGhost.text = BattleCueCopy.QtePerfectSub;
        }

        void HideShot()
        {
            HideSlash();
            HideCrush();
            Fade(_flash);
            Fade(_dim);
            Fade(_gradeGlow);
            Fade(_gradeBg);
            FadeTxt(_gradeGhost);
            FadeTxt(_grade);
            Fade(_dmgGlow);
            Fade(_dmgBg);
            FadeTxt(_dmgGhost);
            FadeTxt(_dmg);
            FadeTxt(_ready);
            if (_tag != null) _tag.rectTransform.localScale = Vector3.one;
            if (_ready != null) _ready.rectTransform.localScale = Vector3.one;
            if (_flash != null) _flash.rectTransform.localScale = Vector3.one;
            if (_core != null) _core.rectTransform.localScale = Vector3.one;
            if (_ring != null) _ring.rectTransform.localScale = Vector3.one;
            if (_ring2 != null) _ring2.rectTransform.localScale = Vector3.one;
            ResetScale(_gradeGlow);
            ResetScale(_gradeBg);
            ResetScale(_dmgGlow);
            ResetScale(_dmgBg);
            if (_gradeGhost != null) _gradeGhost.rectTransform.localScale = Vector3.one;
            if (_grade != null) _grade.rectTransform.localScale = Vector3.one;
            if (_dmgGhost != null) _dmgGhost.rectTransform.localScale = Vector3.one;
            if (_dmg != null) _dmg.rectTransform.localScale = Vector3.one;
        }

        void HideSlash()
        {
            Fade(_wipe);
            Fade(_bolt);
            if (_wipe != null)
            {
                _wipe.fillAmount = 0f;
                _wipe.rectTransform.localScale = Vector3.one;
            }
        }

        void HideCrush()
        {
            Fade(_core);
            Fade(_ring);
            Fade(_ring2);
            Fade(_hitPlus);
            Fade(_hitCross);
            if (_blades != null)
            {
                for (int i = 0; i < _blades.Length; i++)
                {
                    Fade(_blades[i]);
                    if (_blades[i] != null) _blades[i].rectTransform.localScale = Vector3.one;
                }
            }
            if (_sparks != null)
                for (int i = 0; i < _sparks.Length; i++) Fade(_sparks[i]);
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
            if (_tag != null) _tag.fontSize = Mathf.Clamp(Mathf.RoundToInt(16f * _scale), 11, 20);
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

        void PlaceTxt(Text tx, Vector2 anchor, Vector2 pos, Color color, float a, float punch, int font, Vector2 dim)
        {
            if (tx == null) return;
            var rt = tx.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(Mathf.Round(dim.x * _scale), Mathf.Round(dim.y * _scale));
            rt.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            rt.localScale = Vector3.one * punch;
            color.a = a;
            tx.color = color;
            tx.fontSize = Mathf.Clamp(Mathf.RoundToInt(font * _scale), 11, 64);
        }

        static void ResetScale(Image img)
        {
            if (img != null) img.rectTransform.localScale = Vector3.one;
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

        static float ScalePop(float t, float punch)
        {
            if (t < 0f) return 0.50f;
            var u = Mathf.Clamp01(t / 0.36f);
            if (u < 0.45f) return Mathf.Lerp(0.50f, punch, u / 0.45f);
            return Mathf.Lerp(punch, 1f, (u - 0.45f) / 0.55f);
        }

        static float Gate(float t)
        {
            if (t < 0f) return 0f;
            return t < 0.08f ? t / 0.08f : 1f;
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

        static Text Txt(Transform parent, string name, string s, int size, Color color, Vector2 anchor, Vector2 dim, bool bold)
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
            tx.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
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
