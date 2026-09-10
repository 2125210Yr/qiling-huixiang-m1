using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Full-field stage stamp: 完成 gold or 失败 red. ~1.1s then destroy.
    /// No English CLEAR / FAIL.
    /// </summary>
    public sealed class VfxStageClear : MonoBehaviour
    {
        public const float Duration = 1.10f;

        const string WinWord = "完成";
        const string FailWord = "失败";

        static readonly Color WinVeil = new Color(0.16f, 0.08f, 0.00f, 0.86f);
        static readonly Color FailVeil = new Color(0.42f, 0.04f, 0.08f, 0.88f);
        static readonly Color WinWipe = new Color(0.98f, 0.72f, 0.12f, 0.78f);
        static readonly Color FailWipe = new Color(0.85f, 0.08f, 0.12f, 0.86f);
        static readonly Color WinBand = new Color(0.98f, 0.72f, 0.10f, 0.82f);
        static readonly Color FailBand = new Color(0.90f, 0.10f, 0.14f, 0.82f);
        static readonly Color WinInk = new Color(0.28f, 0.12f, 0.00f, 1f);
        static readonly Color FailInk = new Color(0.28f, 0.02f, 0.04f, 1f);
        static readonly Color WinAccent = VisualTokens.FeverGold;
        static readonly Color FailAccent = VisualTokens.StarEvolved;

        Image _veil;
        Image _wipe;
        Image _flash;
        Image _glow;
        Image _band;
        Image _band2;
        Image _star;
        Text _ghost;
        Text _title;
        Color _veilCol;
        Color _wipeCol;
        Color _bandCol;
        Color _accent;
        float _age;
        bool _win;
        bool _burst;

        public static void Play(Transform parent, bool win)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxStageClear>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.Destroy(live[i].gameObject);
            }

            var go = new GameObject("vfxStageClear", typeof(RectTransform), typeof(VfxStageClear));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxStageClear>().Build(win);
        }

        void Build(bool win)
        {
            _win = win;
            _accent = win ? WinAccent : FailAccent;
            _veilCol = win ? WinVeil : FailVeil;
            _wipeCol = win ? WinWipe : FailWipe;
            _bandCol = win ? WinBand : FailBand;
            var word = win ? WinWord : FailWord;
            var ink = win ? WinInk : FailInk;
            var flashPx = win ? 980f : 860f;

            _veil = Full("veil", _veilCol);

            _wipe = Full("wipe", Color.clear);
            UiSprites.Apply(_wipe, UiSprites.Soft());
            _wipe.type = Image.Type.Filled;
            _wipe.fillMethod = Image.FillMethod.Horizontal;
            _wipe.fillOrigin = 0;
            _wipe.fillAmount = 0f;
            _wipe.color = _wipeCol;

            _flash = Img("flash", UiSprites.Soft(), Color.clear,
                new Vector2(0.50f, 0.56f), new Vector2(flashPx, flashPx));

            _glow = Img("glow", UiSprites.Soft(),
                new Color(_accent.r, _accent.g, _accent.b, 0.40f),
                new Vector2(0.50f, 0.56f), new Vector2(720f, 280f));
            _glow.transform.localScale = Vector3.one * 0.55f;

            _band = Img("band", UiSprites.Slash(), _bandCol,
                new Vector2(0.50f, 0.56f), new Vector2(1480f, 196f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, win ? -16f : -20f);
            _band.transform.localScale = new Vector3(0.32f, 1f, 1f);

            _band2 = Img("band2", UiSprites.Slash(),
                new Color(_veilCol.r, _veilCol.g, _veilCol.b, 0.90f),
                new Vector2(0.50f, 0.56f), new Vector2(1180f, 150f));
            _band2.rectTransform.localEulerAngles = new Vector3(0f, 0f, win ? 12f : 16f);
            _band2.transform.localScale = new Vector3(0.30f, 1f, 1f);

            if (win)
            {
                _star = Img("star", UiSprites.Star(), VisualTokens.StarEvolved,
                    new Vector2(0.62f, 0.68f), new Vector2(56f, 56f));
                _star.transform.localScale = Vector3.one * 0.40f;
            }

            _ghost = MkText("ghost", word, 96, ink,
                new Vector2(0.50f, 0.55f), new Vector2(720f, 160f));
            _ghost.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            _ghost.rectTransform.localEulerAngles = new Vector3(0f, 0f, -6f);

            _title = MkText("title", word, 96, _accent,
                new Vector2(0.50f, 0.56f), new Vector2(720f, 160f));
            _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, -6f);
            _title.transform.localScale = Vector3.one * 1.85f;
            _ghost.transform.localScale = Vector3.one * 1.85f;
            Fade(_ghost, 0f);
            Fade(_title, 0f);

            CanvasShake.Punch(win ? 22f : 16f, win ? 0.28f : 0.22f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var a = u < 0.08f ? u / 0.08f
                : u < 0.82f ? 1f
                : 1f - (u - 0.82f) / 0.18f;
            a = Mathf.Clamp01(a);

            if (!_burst && _age >= 0.04f)
            {
                _burst = true;
                var n = _win ? 12 : 8;
                var dist = _win ? 140f : 96f;
                for (int i = 0; i < n; i++)
                    Spark.Spawn(transform, (i & 1) == 0 ? _accent : Color.white,
                        (i / (float)n) * Mathf.PI * 2f, dist, true);
                if (_win)
                {
                    for (int i = 0; i < 6; i++)
                        Spark.Spawn(transform, VisualTokens.YellowValue,
                            (i / 6f) * Mathf.PI * 2f + 0.21f, 72f, true, true);
                }
            }

            SetA(_veil, _veilCol.a * a);
            SetA(_wipe, _wipeCol.a * a);
            if (_wipe != null)
                _wipe.fillAmount = u < 0.16f ? u / 0.16f : 1f;

            var flashPeak = 1f - Mathf.Abs(Mathf.Clamp01(_age / 0.16f) - 0.40f) / 0.40f;
            flashPeak = Mathf.Clamp01(flashPeak);
            SetA(_flash, (_win ? 0.72f : 0.50f) * flashPeak * a);
            if (_flash != null)
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01(_age / 0.20f));

            SetA(_glow, 0.40f * a);
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.18f, Mathf.Clamp01(_age / 0.18f));

            SetA(_band, _bandCol.a * a);
            SetA(_band2, 0.90f * a);
            if (_band != null)
            {
                var bx = Mathf.Lerp(0.32f, 1.08f, Mathf.Clamp01(_age / 0.16f));
                _band.transform.localScale = new Vector3(bx, 1f, 1f);
            }
            if (_band2 != null)
            {
                var bx2 = Mathf.Lerp(0.30f, 1.02f, Mathf.Clamp01((_age - 0.03f) / 0.16f));
                _band2.transform.localScale = new Vector3(bx2, 1f, 1f);
            }

            var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));
            var titleScale = Mathf.Lerp(1.85f, 1f, slam);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * titleScale;
                Fade(_title, a);
            }
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one * titleScale;
                Fade(_ghost, a);
            }

            if (_star != null)
            {
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-24f, 8f, slam));
                _star.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1f, slam);
                SetA(_star, a);
            }

            if (_age >= Duration) Destroy(gameObject);
        }

        Image Full(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Image Img(string name, Sprite sprite, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
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

        Text MkText(string name, string text, int size, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
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
            ol.effectDistance = new Vector2(4f, -4f);
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
