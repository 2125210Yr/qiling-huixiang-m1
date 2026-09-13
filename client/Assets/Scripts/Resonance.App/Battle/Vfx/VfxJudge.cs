using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// QTE judge stamp. Slash band + misregistered type; PERFECT! / GREAT! / GOOD.
    /// Perfect prints DAMAGE n% only when <c>damageMul</c> is known.
    /// Ordinary-PVE mul is unseen — pass 0 and skip that row (Robin ND 150% stays inventory).
    /// Fever plate shows QTE <b>gain</b> % TO FEVER (primary Robin), not live gauge.
    /// Primary EN (Robin t55). No Soft disc. No Pill / Round card.
    /// </summary>
    public sealed class VfxJudge : MonoBehaviour
    {
        const float LifePerfect = 1.90f; // P1 ND Robin PERFECT first-on to first-off; ordinary PVE unseen
        const float FeverPlateLife = 2.50f; // P1 TO FEVER still on at window end; off not measured
        const float LifeGreat = 1.12f;
        const float LifeGood = 0.96f;
        const float GradeAt = 0.00f;
        const float DmgAt = 0.10f;
        const float FeverAtPerfect = 0.22f;
        const float FeverAtElse = 0.12f;
        const float FeverCountSec = 1.30f; // P1: 0% → settle (ease-in). 13% was mid-tween, not a formula.
        const float FlashA = 0.22f;

        static readonly Color PerfectCol = new Color(0.78f, 0.55f, 1f, 1f);
        static readonly Color PerfectInk = new Color(0.28f, 0.08f, 0.42f, 1f);
        static readonly Color GreatInk = new Color(0.04f, 0.26f, 0.08f, 1f);
        static readonly Color GoodInk = new Color(0.28f, 0.16f, 0.02f, 1f);
        static readonly Color DmgInk = new Color(0.18f, 0.16f, 0.14f, 1f);
        static readonly Color Plate = new Color(0.05f, 0.05f, 0.06f, 0.90f);
        static readonly Color TrackDim = new Color(0.14f, 0.22f, 0.12f, 0.80f);

        Image _flash;
        Transform _gradeRoot;
        Image _gradeBand;
        Image _gradeBandIn;
        Text _gradeGhost;
        Text _grade;
        Transform _dmgRoot;
        Image _dmgRule;
        Text _dmgLab;
        Text _dmgGhost;
        Text _dmg;
        Transform _feverRoot;
        Image _feverPlate;
        Image _feverWire;
        Image _feverTrack;
        Image _feverFill;
        Text _feverPct;
        Text _feverGhost;
        Text _feverLab;
        Color _accent;
        Color _ink;
        float _age;
        float _life = LifePerfect;
        float _feverAt;
        float _feverU;
        int _feverTarget;
        int _feverShown = -1;
        float _punch = 1.22f;
        int _kind;
        bool _didGrade;
        bool _didDmg;
        bool _didFever;

        public static bool AnyLive()
        {
            var live = Object.FindObjectsByType<VfxJudge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null && live[i].gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        public static void KillAll()
        {
            var live = Object.FindObjectsByType<VfxJudge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] == null) continue;
                live[i].gameObject.SetActive(false);
                Object.Destroy(live[i].gameObject);
            }
        }

        public static void Play(Transform parent, string grade, float damageMul, float feverPct)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxJudge>(true);
            for (int i = 0; i < live.Length; i++)
            {
                var fx = live[i];
                if (fx == null || fx.transform == parent) continue;
                fx.gameObject.SetActive(false);
                Object.Destroy(fx.gameObject);
            }

            var go = new GameObject("vfxJudge", typeof(RectTransform), typeof(VfxJudge));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            var stamp = go.GetComponent<VfxJudge>();
            stamp.Build(grade, damageMul, feverPct);
            CueTimingProbe.On("qte", stamp.GetInstanceID());
        }

        void OnDestroy()
        {
            CueTimingProbe.Off("qte", GetInstanceID());
        }

        void Build(string grade, float damageMul, float feverPct)
        {
            _kind = Kind(grade);
            _life = _kind == 2 ? FeverPlateLife : _kind == 1 ? LifeGreat : LifeGood;
            _feverAt = _kind == 2 ? FeverAtPerfect : FeverAtElse;
            _punch = _kind == 2 ? 1.28f : _kind == 1 ? 1.18f : 1.10f;
            _feverU = Mathf.Clamp01(feverPct / 100f);
            if (_kind == 2)
            {
                _accent = PerfectCol;
                _ink = PerfectInk;
            }
            else if (_kind == 1)
            {
                _accent = VisualTokens.SlideGreen;
                _ink = GreatInk;
            }
            else
            {
                _accent = VisualTokens.YellowValue;
                _ink = GoodInk;
            }

            _flash = Img(transform, "flash", UiSprites.Slash(), Color.clear,
                new Vector2(0.50f, 0.60f), new Vector2(_kind == 2 ? 1180f : 980f, _kind == 2 ? 32f : 26f));
            _flash.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
            _flash.type = Image.Type.Filled;
            _flash.fillMethod = Image.FillMethod.Horizontal;
            _flash.fillOrigin = (int)Image.OriginHorizontal.Left;
            _flash.fillAmount = 0f;

            var gradeY = _kind == 2 ? 0.68f : 0.62f;
            _gradeRoot = Root("grade", new Vector2(0.50f, gradeY));
            var bw = _kind == 2 ? 860f : _kind == 1 ? 740f : 620f;
            var bh = _kind == 2 ? 120f : _kind == 1 ? 100f : 86f;
            _gradeBand = Img(_gradeRoot, "band", UiSprites.Slash(),
                new Color(_accent.r, _accent.g, _accent.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(bw, bh));
            _gradeBand.rectTransform.localEulerAngles = new Vector3(0f, 0f, -14f);
            _gradeBandIn = Img(_gradeRoot, "bandIn", UiSprites.Slash(),
                new Color(1f, 1f, 1f, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(bw - 140f, _kind == 2 ? 44f : 36f));
            _gradeBandIn.rectTransform.localEulerAngles = new Vector3(0f, 0f, -14f);
            var gradeWord = _kind == 2 ? BattleCueCopy.QtePerfect : _kind == 1 ? BattleCueCopy.QteGreat : BattleCueCopy.QteGood;
            var gradeSize = _kind == 2 ? 86 : _kind == 1 ? 74 : 64;
            _gradeGhost = MkText(_gradeRoot, "ghost", gradeWord, gradeSize, _ink,
                new Vector2(0.50f, 0.50f), new Vector2(720f, 120f), new Vector2(8f, -8f));
            _gradeGhost.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            _grade = MkText(_gradeRoot, "tx", gradeWord, gradeSize, _accent,
                new Vector2(0.50f, 0.50f), new Vector2(720f, 120f), Vector2.zero);
            _grade.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            var stamp = _grade.gameObject.AddComponent<Shadow>();
            stamp.effectColor = new Color(_accent.r, _accent.g, _accent.b, 0.80f);
            stamp.effectDistance = new Vector2(0f, -4f);
            Fade(_gradeGhost, 0f);
            Fade(_grade, 0f);
            _gradeRoot.localScale = Vector3.one * 0.50f;

            if (_kind == 2 && damageMul > 0.05f)
            {
                var dmgPct = Mathf.Max(0, Mathf.RoundToInt(damageMul * 100f));
                _dmgRoot = Root("dmg", new Vector2(0.50f, 0.56f));
                _dmgLab = MkText(_dmgRoot, "lab", BattleCueCopy.QteDamageWord, 26, new Color(0.92f, 0.90f, 0.86f, 1f),
                    new Vector2(0.50f, 0.50f), new Vector2(280f, 40f), new Vector2(0f, 34f));
                var dmgWord = dmgPct + "%";
                _dmgGhost = MkText(_dmgRoot, "ghost", dmgWord, 58, DmgInk,
                    new Vector2(0.50f, 0.50f), new Vector2(360f, 80f), new Vector2(5f, -10f));
                _dmg = MkText(_dmgRoot, "tx", dmgWord, 58, VisualTokens.TapWhite,
                    new Vector2(0.50f, 0.50f), new Vector2(360f, 80f), new Vector2(0f, -6f));
                _dmgRule = Img(_dmgRoot, "rule", UiSprites.Dashed(),
                    new Color(VisualTokens.TapWhite.r, VisualTokens.TapWhite.g, VisualTokens.TapWhite.b, 0f),
                    new Vector2(0.50f, 0.50f), new Vector2(320f, 8f));
                _dmgRule.rectTransform.anchoredPosition = new Vector2(0f, -48f);
                _dmgRule.type = Image.Type.Tiled;
                Fade(_dmgLab, 0f);
                Fade(_dmgGhost, 0f);
                Fade(_dmg, 0f);
                _dmgRoot.localScale = Vector3.one * 0.50f;
            }

            var n = Mathf.Clamp(Mathf.RoundToInt(feverPct), 0, 100);
            _feverTarget = n;
            _feverShown = -1;
            _feverRoot = Root("fever", new Vector2(0.50f, 0.38f));
            _feverPlate = Img(_feverRoot, "plate", UiSprites.Pixel(),
                new Color(Plate.r, Plate.g, Plate.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(292f, 86f));
            _feverWire = Img(_feverRoot, "wire", UiSprites.WireFrame(),
                new Color(VisualTokens.SlideGreen.r, VisualTokens.SlideGreen.g, VisualTokens.SlideGreen.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(300f, 94f));
            _feverTrack = Img(_feverRoot, "track", UiSprites.Dashed(),
                new Color(TrackDim.r, TrackDim.g, TrackDim.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(248f, 8f));
            _feverTrack.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            _feverTrack.type = Image.Type.Tiled;
            _feverFill = Img(_feverRoot, "fill", UiSprites.Pixel(),
                new Color(VisualTokens.SlideGreen.r, VisualTokens.SlideGreen.g, VisualTokens.SlideGreen.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(248f, 5f));
            _feverFill.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            _feverFill.type = Image.Type.Filled;
            _feverFill.fillMethod = Image.FillMethod.Horizontal;
            _feverFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _feverFill.fillAmount = 0f;
            var feverWord = "0%";
            _feverGhost = MkText(_feverRoot, "ghost", feverWord, 48, GreatInk,
                new Vector2(0.50f, 0.50f), new Vector2(280f, 64f), new Vector2(4f, 14f));
            _feverPct = MkText(_feverRoot, "pct", feverWord, 48, VisualTokens.SlideGreen,
                new Vector2(0.50f, 0.50f), new Vector2(280f, 64f), new Vector2(0f, 18f));
            // Primary GT: "N% TO FEVER" under the gain number.
            _feverLab = MkText(_feverRoot, "lab", "TO FEVER", 22, VisualTokens.FeverGold,
                new Vector2(0.50f, 0.50f), new Vector2(220f, 32f), new Vector2(0f, -8f));
            Fade(_feverGhost, 0f);
            Fade(_feverPct, 0f);
            Fade(_feverLab, 0f);
            _feverRoot.localScale = Vector3.one * 0.50f;

            CanvasShake.Punch(_kind == 2 ? 22f : _kind == 1 ? 12f : 6f, _kind == 2 ? 0.28f : 0.18f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var gradeLife = _kind == 2 ? LifePerfect : _life;
            var gradeHold = gradeLife - 0.28f;
            var fade = _age < gradeHold ? 1f : 1f - Mathf.Clamp01((_age - gradeHold) / 0.28f);
            var feverHold = _life - 0.28f;
            var feverFade = _age < feverHold ? 1f : 1f - Mathf.Clamp01((_age - feverHold) / 0.28f);

            TickFlash(fade);
            TickGrade(fade);
            TickDmg(fade);
            TickFever(feverFade);

            if (_age >= _life) Destroy(gameObject);
        }

        void TickFlash(float fade)
        {
            if (_flash == null) return;
            var peak = 1f - Mathf.Abs(Mathf.Clamp01(_age / 0.16f) - 0.40f) / 0.40f;
            peak = Mathf.Clamp01(peak);
            var a = FlashA * peak * fade;
            var c = Color.Lerp(_accent, Color.white, 0.45f);
            _flash.color = new Color(c.r, c.g, c.b, a);
            _flash.fillAmount = Mathf.Clamp01(_age / 0.14f);
        }

        void TickGrade(float fade)
        {
            var t = _age - GradeAt;
            if (t >= 0f && !_didGrade)
            {
                _didGrade = true;
                Burst(_gradeRoot, _accent, _kind == 2 ? 10 : _kind == 1 ? 6 : 4, _kind == 2 ? 78f : 52f);
            }
            if (_gradeRoot != null) _gradeRoot.localScale = Vector3.one * ScalePop(t, _punch);
            var a = fade * Gate(t);
            SetA(_gradeBand, 0.90f * a);
            SetA(_gradeBandIn, 0.52f * a);
            Fade(_gradeGhost, a);
            Fade(_grade, a);
        }

        void TickDmg(float fade)
        {
            if (_dmgRoot == null) return;
            var t = _age - DmgAt;
            if (t >= 0f && !_didDmg)
            {
                _didDmg = true;
                Burst(_dmgRoot, VisualTokens.TapWhite, 6, 48f);
            }
            _dmgRoot.localScale = Vector3.one * ScalePop(t, 1.16f);
            var a = fade * Gate(t);
            SetA(_dmgRule, 0.72f * a);
            Fade(_dmgLab, a);
            Fade(_dmgGhost, a);
            Fade(_dmg, a);
        }

        void TickFever(float fade)
        {
            if (_feverRoot == null) return;
            var t = _age - _feverAt;
            if (t >= 0f && !_didFever)
            {
                _didFever = true;
                Burst(_feverRoot, VisualTokens.SlideGreen, 8, 64f);
            }
            _feverRoot.localScale = Vector3.one * ScalePop(t, 1.20f);
            var a = fade * Gate(t);
            SetA(_feverPlate, Plate.a * a);
            SetA(_feverWire, 0.88f * a);
            SetA(_feverTrack, TrackDim.a * a);
            var fillT = Mathf.Clamp01(t / FeverCountSec);
            var eased = fillT * fillT;
            if (_feverFill != null)
            {
                _feverFill.fillAmount = _feverU * eased;
                var fc = VisualTokens.SlideGreen;
                fc.a = a;
                _feverFill.color = fc;
            }
            var shown = t < 0f ? 0 : Mathf.RoundToInt(_feverTarget * eased);
            if (shown != _feverShown)
            {
                _feverShown = shown;
                var word = shown + "%";
                if (_feverPct != null) _feverPct.text = word;
                if (_feverGhost != null) _feverGhost.text = word;
            }
            Fade(_feverGhost, a);
            Fade(_feverPct, a);
            if (_feverLab != null)
            {
                var hue = CombatFeel.FeverHue(Time.unscaledTime * 0.35f);
                hue.a = a;
                _feverLab.color = hue;
            }
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

        static void Burst(Transform at, Color color, int n, float dist)
        {
            if (at == null) return;
            for (int i = 0; i < n; i++)
                Spark.Spawn(at, color, (i / (float)n) * Mathf.PI * 2f, dist, true);
        }

        static int Kind(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return 0;
            var g = grade.Trim();
            if (g.StartsWith("完美", System.StringComparison.Ordinal) ||
                g.StartsWith("PERFECT", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g, "perfect", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (g.StartsWith("优秀", System.StringComparison.Ordinal) ||
                g.StartsWith("GREAT", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g, "great", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            return 0;
        }

        Transform Root(string name, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return go.transform;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string name, string text, int size, Color color, Vector2 anchor, Vector2 dim, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = text ?? "";
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3f, -3f);
            return tx;
        }

        static void SetA(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        static void Fade(Text tx, float a)
        {
            if (tx == null) return;
            var c = tx.color;
            c.a = Mathf.Clamp01(a);
            tx.color = c;
        }
    }
}
