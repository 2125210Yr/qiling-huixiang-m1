using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Slide SHOWTIME stamp. Red slash + SHOWTIME + skill name.
    /// Not Fever (狂热时间), not Drive skill-name cut, not battle-start 开战.
    /// Ragna GL supplementary identity (`IT'S SHOWTIME!!` / `SLIDE SKILL`);
    /// not ordinary 5v5 GT. ~1.2s. No portrait window, no memorial art.
    /// </summary>
    public sealed class VfxShowtime : MonoBehaviour
    {
        public const float Duration = 1.20f;

        const string TitleWord = "SHOWTIME";
        const string LocWord = "上滑";

        static readonly Color VeilCol = new Color(0.42f, 0.04f, 0.08f, 0.12f);
        static readonly Color WipeCol = new Color(0.85f, 0.08f, 0.12f, 0.18f);
        static readonly Color DotCol = new Color(0.06f, 0.00f, 0.02f, 0.10f);
        static readonly Color BandCol = new Color(0.90f, 0.10f, 0.14f, 0.72f);

        Image _veil;
        Image _dots;
        Image _wipe;
        Image _band;
        Image _star;
        Text _titleGhost;
        Text _title;
        Text _loc;
        Text _skill;
        float _age;

        public static bool AnyLive()
        {
            var live = Object.FindObjectsByType<VfxShowtime>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null && live[i].gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        public static void KillAll()
        {
            var live = Object.FindObjectsByType<VfxShowtime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] == null) continue;
                live[i].gameObject.SetActive(false);
                Object.Destroy(live[i].gameObject);
            }
        }

        public static void Play(Transform parent, Texture portrait, string skillName)
        {
            if (parent == null) return;
            // portrait is intentionally ignored: this stamp never shows art.

            var live = parent.GetComponentsInChildren<VfxShowtime>(true);
            for (int i = 0; i < live.Length; i++)
            {
                var fx = live[i];
                if (fx == null || fx.transform == parent) continue;
                fx.gameObject.SetActive(false);
                Object.Destroy(fx.gameObject);
            }

            var go = new GameObject("vfxShowtime", typeof(RectTransform), typeof(VfxShowtime));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxShowtime>().Build(skillName);
        }

        void Build(string skillName)
        {
            _veil = Full("veil", VeilCol);

            _dots = Full("dots", DotCol);
            UiSprites.Apply(_dots, UiSprites.Halftone());
            _dots.type = Image.Type.Tiled;

            _wipe = Full("wipe", Color.clear);
            UiSprites.Apply(_wipe, UiSprites.Pixel());
            _wipe.type = Image.Type.Filled;
            _wipe.fillMethod = Image.FillMethod.Horizontal;
            _wipe.fillOrigin = 0;
            _wipe.fillAmount = 0f;
            _wipe.color = WipeCol;

            _band = Img("band", UiSprites.Slash(), BandCol, new Vector2(0.50f, 0.56f), new Vector2(980f, 170f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, -14f);

            _star = Img("star", UiSprites.Star(), VisualTokens.StarEvolved,
                new Vector2(0.30f, 0.72f), new Vector2(56f, 56f));

            _titleGhost = MkText("titleGhost", TitleWord, 86, new Color(0.12f, 0.01f, 0.02f, 1f),
                new Vector2(0.50f, 0.57f), new Vector2(520f, 140f));
            _titleGhost.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            _titleGhost.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
            _titleGhost.transform.localScale = Vector3.one * 3.40f;

            _title = MkText("title", TitleWord, 86, Color.white,
                new Vector2(0.50f, 0.58f), new Vector2(520f, 140f));
            _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
            _title.transform.localScale = Vector3.one * 3.40f;

            _loc = MkText("loc", LocWord, 28, VisualTokens.StarEvolved,
                new Vector2(0.50f, 0.46f), new Vector2(240f, 40f));

            var skill = skillName ?? "";
            var skillLine = skill.Length == 0 ? "" : skill;
            _skill = MkText("skill", skillLine, 40, VisualTokens.YellowValue,
                new Vector2(0.50f, 0.38f), new Vector2(560f, 72f));
            _skill.rectTransform.localEulerAngles = new Vector3(0f, 0f, -6f);
            if (skillLine.Length == 0)
                _skill.enabled = false;

            CanvasShake.Punch(32f, 0.30f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var a = u < 0.08f ? u / 0.08f
                : u < 0.82f ? 1f
                : 1f - (u - 0.82f) / 0.18f;
            a = Mathf.Clamp01(a);

            SetA(_veil, VeilCol.a * a);
            SetA(_dots, DotCol.a * a);
            SetA(_wipe, WipeCol.a * a);
            if (_wipe != null)
                _wipe.fillAmount = u < 0.16f ? u / 0.16f : 1f;

            SetA(_band, BandCol.a * a);
            if (_band != null)
            {
                var bx = Mathf.Lerp(0.30f, 1.12f, Mathf.Clamp01(_age / 0.16f));
                _band.transform.localScale = new Vector3(bx, 1f, 1f);
            }

            var slamT = Mathf.Clamp01(_age / 0.14f);
            var slam = 1f - (1f - slamT) * (1f - slamT);
            var settleT = Mathf.Clamp01((_age - 0.14f) / 0.10f);
            var settle = 1f - (1f - settleT) * (1f - settleT);
            // Bigger slam: 3.4x dive, squash to 0.94 on impact, ease back to 1.0.
            var titleScale = Mathf.Lerp(Mathf.Lerp(3.40f, 0.94f, slam), 1f, settle);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * titleScale;
                Fade(_title, a);
            }
            if (_titleGhost != null)
            {
                _titleGhost.transform.localScale = Vector3.one * titleScale;
                Fade(_titleGhost, a);
            }

            SetA(_star, a);
            if (_star != null)
            {
                var spin = Mathf.Lerp(-24f, 8f, slam);
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);
                _star.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 1f, slam);
            }

            var skillU = Mathf.Clamp01((_age - 0.12f) / 0.16f);
            Fade(_loc, a * skillU);
            Fade(_skill, a * skillU);

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
            if (img == null || !img.enabled) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        static void Fade(Text tx, float a)
        {
            if (tx == null || !tx.enabled) return;
            var c = tx.color;
            c.a = Mathf.Clamp01(a);
            tx.color = c;
        }
    }
}
