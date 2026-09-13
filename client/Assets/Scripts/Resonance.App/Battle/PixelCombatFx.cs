using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public enum CombatCut
    {
        Slide,
        Drive,
        Warn
    }

    /// <summary>
    /// Drive skill-name cut-in only. Slide SHOWTIME is VfxShowtime.
    /// QTE grades are VfxJudge. Fever banner is VfxFeverOverlay.
    /// Cut-ins follow cast events and never gate damage.
    /// </summary>
    public sealed class PixelCombatFx : MonoBehaviour
    {
        static readonly Color FeverPink = new Color(0.78f, 0.28f, 0.73f, 1f);

        Transform _root;
        Image _dim;
        Image _wipe;
        Image _slashWipe;
        Image _bolt;
        Image _dots;
        Image _cutin;
        Image _badgeDisc;
        Text _title;
        Text _badge;
        Text _skill;
        Text _combo;
        Text _comboDmg;
        Text _feverWord;
        Image[] _lines;
        Image[] _radial;
        Image[] _cutStars;
        float[] _linePhase;
        float _showT;
        float _showLife = 1.15f;
        float _comboPunch;
        CombatCut _kind;
        Color _accent = VisualTokens.DriveOrange;
        bool _fever;
        int _comboN;
        int _comboDmgN;
        System.Action _onShowDone;
        static Sprite _dotsSpr;

        public bool Showing => _showT > 0f;

        public static PixelCombatFx Ensure(Transform root)
        {
            if (root == null) return null;
            var existing = root.GetComponentInChildren<PixelCombatFx>(true);
            if (existing != null) return existing;
            var go = new GameObject("pixelFx", typeof(RectTransform), typeof(PixelCombatFx));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var fx = go.GetComponent<PixelCombatFx>();
            if (fx == null) { Object.Destroy(go); return null; }
            fx._root = go.transform;
            fx.Build();
            return fx;
        }

        void Build()
        {
            if (_root == null) _root = transform;
            _dim = Full("dim", new Color(0.42f, 0.04f, 0.08f, 0f));
            _dots = Full("dots", Color.clear);
            if (_dots != null)
            {
                UiSprites.Apply(_dots, Dots());
                _dots.type = Image.Type.Tiled;
            }

            _wipe = Full("wipe", new Color(0.85f, 0.08f, 0.12f, 0f));
            if (_wipe != null)
            {
                _wipe.type = Image.Type.Filled;
                _wipe.fillMethod = Image.FillMethod.Horizontal;
                _wipe.fillOrigin = 0;
                _wipe.fillAmount = 0f;
                // Overscan + slight diagonal so the fill edge sweeps like a red slash.
                var wrt = _wipe.rectTransform;
                wrt.offsetMin = new Vector2(-200f, -200f);
                wrt.offsetMax = new Vector2(200f, 200f);
                wrt.localEulerAngles = new Vector3(0f, 0f, -9f);
            }

            var slashGo = new GameObject("slashWipe", typeof(RectTransform), typeof(Image));
            slashGo.transform.SetParent(_root, false);
            var srt = slashGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.50f, 0.62f);
            srt.sizeDelta = new Vector2(1600f, 220f);
            srt.localEulerAngles = new Vector3(0f, 0f, -28f);
            _slashWipe = slashGo.GetComponent<Image>();
            UiSprites.Apply(_slashWipe, UiSprites.Slash());
            _slashWipe.color = Color.clear;
            _slashWipe.raycastTarget = false;

            _bolt = MkImg("bolt", new Vector2(0.48f, 0.60f), new Vector2(780f, 44f), Color.clear);
            if (_bolt != null)
            {
                UiSprites.Apply(_bolt, UiSprites.Slash());
                _bolt.rectTransform.localEulerAngles = new Vector3(0f, 0f, -42f);
            }

            var cutGo = new GameObject("cutin", typeof(RectTransform), typeof(Image));
            cutGo.transform.SetParent(_root, false);
            var crt = cutGo.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.30f, 0.58f);
            crt.sizeDelta = new Vector2(420f, 560f);
            _cutin = cutGo.GetComponent<Image>();
            UiSprites.Apply(_cutin, UiSprites.Pixel());
            _cutin.color = Color.clear;
            _cutin.preserveAspect = true;
            _cutin.raycastTarget = false;

            _badgeDisc = MkImg("badgeDisc", new Vector2(0.72f, 0.66f), new Vector2(88f, 88f), Color.clear);
            UiSprites.Apply(_badgeDisc, UiSprites.Circle());

            _title = MkText("title", 92, new Vector2(0.72f, 0.76f), new Vector2(560f, 132f), Color.white, 5f);
            _badge = MkText("badge", 26, new Vector2(0.72f, 0.66f), new Vector2(160f, 48f), VisualTokens.FeverGold);
            _skill = MkText("skill", 34, new Vector2(0.72f, 0.56f), new Vector2(520f, 64f), VisualTokens.YellowValue);
            _combo = MkText("combo", 40, new Vector2(0.50f, 0.88f), new Vector2(720f, 64f), VisualTokens.FeverGold);
            if (_combo != null) _combo.color = Color.clear;
            _comboDmg = MkText("comboDmg", 32, new Vector2(0.50f, 0.835f), new Vector2(720f, 48f), VisualTokens.YellowValue);
            if (_comboDmg != null) _comboDmg.color = Color.clear;
            _feverWord = MkText("feverWord", 28, new Vector2(0.50f, 0.248f), new Vector2(480f, 40f), FeverPink);
            if (_feverWord != null) _feverWord.color = Color.clear;

            _cutStars = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                var ang = i * 45f * Mathf.Deg2Rad;
                var st = MkImg("cstar" + i,
                    new Vector2(0.28f + Mathf.Cos(ang) * 0.16f, 0.62f + Mathf.Sin(ang) * 0.18f),
                    new Vector2(26f, 26f), Color.clear);
                UiSprites.Apply(st, UiSprites.Star());
                _cutStars[i] = st;
            }

            _lines = new Image[16];
            _linePhase = new float[16];
            for (int i = 0; i < 16; i++)
            {
                var go = new GameObject("line" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.08f + (i % 8) * 0.12f, 0.30f + (i / 8) * 0.34f);
                rt.sizeDelta = new Vector2(i % 2 == 0 ? 260f : 170f, i % 2 == 0 ? 20f : 16f);
                rt.localEulerAngles = new Vector3(0f, 0f, -36f);
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Slash());
                img.color = Color.clear;
                img.raycastTarget = false;
                _lines[i] = img;
                _linePhase[i] = i * 0.41f;
            }

            _radial = new Image[12];
            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("rad" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.50f, 0.54f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(300f, 18f);
                rt.localEulerAngles = new Vector3(0f, 0f, i * 30f);
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Slash());
                img.color = Color.clear;
                img.raycastTarget = false;
                _radial[i] = img;
            }

            HideCutin();
        }

        public void PlaySlide(Sprite portrait, string skill, float life, System.Action done)
        {
            var host = transform.parent != null ? transform.parent : transform;
            VfxShowtime.Play(host, portrait != null ? portrait.texture : null, skill);
            ArmTimer(CombatCut.Slide, VisualTokens.StarEvolved, life, done);
            HideCutin();
        }

        public void PlayDrive(Sprite portrait, string skill, float life, System.Action done)
        {
            VfxShowtime.KillAll();
            var head = string.IsNullOrEmpty(skill) ? BattleCueCopy.DriveCast : skill;
            Play(portrait, head, "", BattleCueCopy.DriveCast, VisualTokens.DriveOrange, life, CombatCut.Drive, done);
        }

        public void PlayWarning()
        {
            var host = transform.parent != null ? transform.parent : transform;
            VfxWarning.Play(host, "");
            ArmTimer(CombatCut.Warn, VisualTokens.StarEvolved, 0.95f, null);
            HideCutin();
        }

        public void Play(Sprite portrait, string title, string skill, string badge, Color accent, float life, CombatCut kind, System.Action done)
        {
            ArmTimer(kind, accent, life, done);
            if (kind != CombatCut.Drive)
            {
                HideCutin();
                return;
            }

            transform.SetAsLastSibling();
            SetText(_title, title, Color.white);
            SetText(_badge, badge, accent);
            SetText(_skill, skill, VisualTokens.YellowValue);
            if (_badgeDisc != null)
                _badgeDisc.color = new Color(accent.r, accent.g, accent.b, 0.92f);
            if (_cutin != null)
                _cutin.color = Color.clear;
            SetCutStars(true);
            PaintDrive();
        }

        void ArmTimer(CombatCut kind, Color accent, float life, System.Action done)
        {
            var prev = _onShowDone;
            _onShowDone = null;
            if (prev != null) prev();
            _kind = kind;
            _accent = accent;
            _showLife = Mathf.Max(0.35f, life);
            _showT = _showLife;
            _onShowDone = done;
        }

        public void HideNow()
        {
            _showT = 0f;
            HideCutin();
            var cb = _onShowDone;
            _onShowDone = null;
            if (cb != null) cb();
        }

        public void SetFever(bool on)
        {
            _fever = on;
            if (on)
            {
                transform.SetAsLastSibling();
                return;
            }
            _comboN = 0;
            _comboDmgN = 0;
            _comboPunch = 0f;
            if (_combo != null)
            {
                _combo.color = Color.clear;
                _combo.transform.localScale = Vector3.one;
            }
            if (_comboDmg != null)
            {
                _comboDmg.color = Color.clear;
                _comboDmg.transform.localScale = Vector3.one;
            }
            if (_feverWord != null) _feverWord.color = Color.clear;
            if (_dots != null && _showT <= 0f) _dots.color = Color.clear;
            ClearLines();
        }

        public void AddCombo(int dmg)
        {
            if (!_fever || dmg <= 0) return;
            _comboN++;
            _comboDmgN += dmg;
        }

        public void Tick()
        {
            var dt = Time.unscaledDeltaTime;
            if (_showT > 0f)
            {
                _showT -= dt;
                if (_kind == CombatCut.Drive)
                    PaintDrive();
                else
                    HideCutin();
                if (_showT <= 0f)
                {
                    _showT = 0f;
                    HideCutin();
                    var cb = _onShowDone;
                    _onShowDone = null;
                    if (cb != null) cb();
                }
            }

            if (_comboPunch > 0f)
            {
                _comboPunch -= dt * 3.5f;
                var p = 1f + Mathf.Max(0f, _comboPunch / 0.35f) * 0.28f;
                if (_combo != null) _combo.transform.localScale = Vector3.one * p;
                if (_comboDmg != null) _comboDmg.transform.localScale = Vector3.one * (p * 0.96f);
            }

            if (!_fever) return;
            AnimateFever();
        }

        void PaintDrive()
        {
            var u = 1f - Mathf.Clamp01(_showT / Mathf.Max(0.05f, _showLife));
            var a = u < 0.10f ? u / 0.10f
                : u > 0.78f ? (1f - u) / 0.22f
                : 1f;
            a = Mathf.Clamp01(a);
            PulseCutStars();
            if (_dim != null)
                _dim.color = new Color(0.28f, 0.10f, 0.02f, 0.14f * a);
            if (_wipe != null)
            {
                _wipe.color = Color.clear;
                _wipe.fillAmount = 0f;
            }
            if (_slashWipe != null) _slashWipe.color = Color.clear;
            if (_dots != null) _dots.color = Color.clear;
            if (_bolt != null)
            {
                UiSprites.Apply(_bolt, UiSprites.Slash());
                _bolt.color = new Color(_accent.r, _accent.g, _accent.b, 0.82f * a);
                _bolt.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
                var bx = Mathf.Lerp(0.42f, 1.08f, Mathf.Clamp01(u / 0.18f));
                _bolt.transform.localScale = new Vector3(bx, 1f, 1f);
            }
            if (_badgeDisc != null)
            {
                var c = _accent;
                c.a = 0.92f * a;
                _badgeDisc.color = c;
            }
            if (_cutin != null) _cutin.color = Color.clear;
            FadeText(_title, a);
            FadeText(_badge, a);
            FadeText(_skill, a);
            if (_cutStars != null)
            {
                for (int i = 0; i < _cutStars.Length; i++)
                {
                    if (_cutStars[i] == null) continue;
                    var c = _cutStars[i].color;
                    c.a *= a;
                    _cutStars[i].color = c;
                }
            }
        }

        void AnimateFever()
        {
            if (_feverWord != null) _feverWord.color = Color.clear;
            if (_showT <= 0f && _dots != null)
                _dots.color = Color.clear;
            if (_combo != null) _combo.color = Color.clear;
            if (_comboDmg != null) _comboDmg.color = Color.clear;
            ClearLines();
        }

        void ClearLines()
        {
            if (_lines != null)
                for (int i = 0; i < _lines.Length; i++)
                    if (_lines[i] != null) _lines[i].color = Color.clear;
            if (_radial != null)
                for (int i = 0; i < _radial.Length; i++)
                    if (_radial[i] != null) _radial[i].color = Color.clear;
        }

        void SetCutStars(bool on)
        {
            if (_cutStars == null) return;
            for (int i = 0; i < _cutStars.Length; i++)
            {
                if (_cutStars[i] == null) continue;
                _cutStars[i].color = on ? VisualTokens.FeverGold : Color.clear;
            }
        }

        void PulseCutStars()
        {
            if (_cutStars == null) return;
            var t = Time.unscaledTime;
            for (int i = 0; i < _cutStars.Length; i++)
            {
                if (_cutStars[i] == null) continue;
                var tw = 0.5f + 0.5f * Mathf.Sin(t * 4.2f + i * 1.3f);
                var c = Color.Lerp(new Color(1f, 0.82f, 0.25f), VisualTokens.FeverGold, tw);
                _cutStars[i].color = new Color(c.r, c.g, c.b, 0.30f + 0.65f * tw);
                _cutStars[i].transform.localScale = Vector3.one * (0.85f + 0.35f * tw);
            }
        }

        void PlaceCutin(Vector2 anchor, Vector2 size)
        {
            if (_cutin == null) return;
            var rt = _cutin.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
        }

        void FadeCutin(float a)
        {
            if (_dim != null)
            {
                var c = _dim.color;
            var baseA = _kind == CombatCut.Drive ? 0.08f : _kind == CombatCut.Warn ? 0.28f : 0.16f;
                c.a = baseA * a;
                _dim.color = c;
            }
            if (_wipe != null)
            {
                var c = _wipe.color;
                c.a = 0.70f * a;
                _wipe.color = c;
            }
            if (_slashWipe != null)
            {
                var c = _slashWipe.color;
                c.a = 0.70f * a;
                _slashWipe.color = c;
            }
            if (_bolt != null)
            {
                var c = _bolt.color;
                c.a = 0.70f * a;
                _bolt.color = c;
            }
            if (_dots != null)
            {
                var c = _dots.color;
                c.a = _kind == CombatCut.Slide ? 0.10f * a : 0f;
                _dots.color = c;
            }
            if (_cutin != null)
                _cutin.color = Color.clear;
            if (_badgeDisc != null)
            {
                var c = _badgeDisc.color;
                c.a = 0.92f * a;
                _badgeDisc.color = c;
            }
            if (_cutStars != null)
            {
                for (int i = 0; i < _cutStars.Length; i++)
                {
                    if (_cutStars[i] == null) continue;
                    var c = _cutStars[i].color;
                    c.a *= a;
                    _cutStars[i].color = c;
                }
            }
            FadeText(_title, a);
            FadeText(_badge, a);
            FadeText(_skill, a);
        }

        void HideCutin()
        {
            if (_dim != null) _dim.color = Color.clear;
            if (_wipe != null)
            {
                _wipe.color = Color.clear;
                _wipe.fillAmount = 0f;
            }
            if (_slashWipe != null) _slashWipe.color = Color.clear;
            if (_bolt != null) _bolt.color = Color.clear;
            if (_dots != null) _dots.color = Color.clear;
            if (_cutin != null) _cutin.color = Color.clear;
            if (_badgeDisc != null) _badgeDisc.color = Color.clear;
            SetCutStars(false);
            FadeText(_title, 0f);
            FadeText(_badge, 0f);
            FadeText(_skill, 0f);
        }

        static void SetText(Text tx, string s, Color c)
        {
            if (tx == null) return;
            tx.text = s ?? "";
            tx.color = c;
        }

        static void FadeText(Text tx, float a)
        {
            if (tx == null) return;
            var c = tx.color;
            c.a = a;
            tx.color = c;
        }

        Image Full(string name, Color color)
        {
            if (_root == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Image MkImg(string name, Vector2 anchor, Vector2 dim, Color color)
        {
            if (_root == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            var img = go.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, int size, Vector2 anchor, Vector2 dim, Color color, float outline = 3f)
        {
            if (_root == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            var tx = go.GetComponent<Text>();
            if (tx == null) return null;
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.clear;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(outline, -outline);
            }
            return tx;
        }

        static Sprite Dots()
        {
            if (_dotsSpr != null) return _dotsSpr;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            const float r = 13f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    var a = Mathf.Clamp01(r + 0.75f - d);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            // 64 texels at ppu 4 = 16-unit tile: a soft round halftone dot, not bitmap grain.
            _dotsSpr = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect, Vector4.zero);
            return _dotsSpr;
        }
    }
}
