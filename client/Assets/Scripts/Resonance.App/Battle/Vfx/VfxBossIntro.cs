using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Boss-wave stamp. Primary P0 t100: <c>THE MASTER OF DESIRE</c> / <c>BOSS</c> /
    /// <c>WARNING</c> / role / name. No portrait window, no memorial art.
    /// </summary>
    public sealed class VfxBossIntro : MonoBehaviour
    {
        public const float Duration = 1.35f;

        const string TitleWord = "BOSS";

        Image _veil;
        Image _dots;
        Image _band;
        Image _bandInner;
        Image _warnPlate;
        Text _epithet;
        Text _ghost;
        Text _title;
        Text _warn;
        Text _role;
        Text _name;
        float _age;

        public static void Play(Transform parent, string bossName)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxBossIntro>(true);
            for (int i = 0; i < live.Length; i++)
            {
                var fx = live[i];
                if (fx == null || fx.transform == parent) continue;
                Object.Destroy(fx.gameObject);
            }

            var go = new GameObject("vfxBossIntro", typeof(RectTransform), typeof(VfxBossIntro));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxBossIntro>().Build(bossName);
        }

        void Build(string bossName)
        {
            _veil = Full("veil", new Color(0.04f, 0.01f, 0.00f, 0.16f));
            _dots = Full("dots", new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.07f));
            UiSprites.Apply(_dots, UiSprites.Halftone());
            _dots.type = Image.Type.Tiled;

            _band = Img("band", UiSprites.Slash(), new Color(VisualTokens.FeverGold.r, VisualTokens.FeverGold.g, VisualTokens.FeverGold.b, 0.88f),
                new Vector2(0.50f, 0.58f), new Vector2(1480f, 220f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);
            _bandInner = Img("bandIn", UiSprites.Slash(), new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0.50f, 0.58f), new Vector2(1260f, 96f));
            _bandInner.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);

            // P0 t100: small red epithet over BOSS.
            _epithet = MkText("epithet", BattleCueCopy.BossEpithetDesire, 18, Color.white,
                new Vector2(0.28f, 0.66f), new Vector2(420f, 36f));
            _epithet.fontStyle = FontStyle.Bold;
            _epithet.alignment = TextAnchor.MiddleLeft;
            var epPlate = Img("epithetPlate", UiSprites.Pixel(), new Color(0.72f, 0.08f, 0.10f, 0.92f),
                new Vector2(0.28f, 0.66f), new Vector2(360f, 28f));
            epPlate.transform.SetSiblingIndex(_epithet.transform.GetSiblingIndex());

            _ghost = MkText("ghost", TitleWord, 88, new Color(0.08f, 0.02f, 0.00f, 0.80f),
                new Vector2(0.50f, 0.57f), new Vector2(920f, 150f));
            _ghost.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            _ghost.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);

            _title = MkText("title", TitleWord, 88, Color.white,
                new Vector2(0.50f, 0.58f), new Vector2(920f, 150f));
            _title.fontStyle = FontStyle.Bold;
            _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            var ol = _title.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3.2f, -3.2f);

            // P0 t100 bottom-left warn stack: WARNING / role / name.
            _warnPlate = Img("warnPlate", UiSprites.Halftone(), new Color(0.55f, 0.04f, 0.06f, 0.78f),
                new Vector2(0.22f, 0.28f), new Vector2(360f, 140f));
            _warnPlate.type = Image.Type.Tiled;

            _warn = MkText("warn", BattleCueCopy.BossWarn, 26, Color.white,
                new Vector2(0.22f, 0.34f), new Vector2(320f, 36f));
            _warn.fontStyle = FontStyle.Bold;
            _warn.alignment = TextAnchor.MiddleLeft;

            var role = BattleCueCopy.BossRoleForName(bossName);
            _role = MkText("role", role ?? "", 20, Color.white,
                new Vector2(0.22f, 0.28f), new Vector2(320f, 28f));
            _role.alignment = TextAnchor.MiddleLeft;
            if (string.IsNullOrEmpty(role))
                _role.enabled = false;

            var name = bossName ?? "";
            _name = MkText("name", name, 34, Color.white,
                new Vector2(0.22f, 0.22f), new Vector2(360f, 44f));
            _name.fontStyle = FontStyle.Bold;
            _name.alignment = TextAnchor.MiddleLeft;
            if (name.Length == 0)
                _name.enabled = false;

            _band.transform.localScale = new Vector3(0.42f, 1.10f, 1f);
            _title.transform.localScale = Vector3.one * 1.70f;
            CanvasShake.Punch(10f, 0.16f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var a = u < 0.10f ? u / 0.10f
                : u < 0.72f ? 1f
                : 1f - (u - 0.72f) / 0.28f;
            a = Mathf.Clamp01(a);

            SetA(_veil, 0.16f * a);
            SetA(_dots, 0.07f * a);
            SetA(_band, 0.88f * a);
            SetA(_bandInner, 0.50f * a);
            SetA(_warnPlate, 0.78f * a);

            var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));
            if (_band != null)
                _band.transform.localScale = new Vector3(Mathf.Lerp(0.42f, 1.02f, slam), 1.10f, 1f);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * Mathf.Lerp(1.70f, 1f, slam);
                Fade(_title, a);
            }
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one * Mathf.Lerp(1.70f, 1f, slam);
                Fade(_ghost, a);
            }
            Fade(_epithet, a * Mathf.Clamp01((_age - 0.04f) / 0.10f));
            Fade(_warn, a * Mathf.Clamp01((_age - 0.08f) / 0.10f));
            Fade(_role, a * Mathf.Clamp01((_age - 0.10f) / 0.10f));
            Fade(_name, a * Mathf.Clamp01((_age - 0.12f) / 0.10f));

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
            ol.effectDistance = new Vector2(3f, -3f);
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
