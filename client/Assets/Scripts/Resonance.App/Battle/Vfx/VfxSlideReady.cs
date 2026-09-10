using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Portrait ping when a slide skill is armed: expanding green ring + 上滑就绪.
    /// Never SLIDE SKILL READY. ~0.7s. Color is VisualTokens.SlideGreen.
    /// </summary>
    public sealed class VfxSlideReady : MonoBehaviour
    {
        public const float Duration = 0.70f;
        const string Word = "上滑就绪";

        static readonly Color Ink = new Color(0.04f, 0.26f, 0.08f, 1f);

        Image _glow;
        Image _core;
        Image _ring;
        Image _rim;
        Image[] _chevrons;
        Text _ghost;
        Text _label;
        float _age;

        public static void Play(Transform parent, Vector2 portraitAnchor01)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxSlideReady>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.DestroyImmediate(live[i].gameObject);
            }

            var go = new GameObject("slideReady", typeof(RectTransform), typeof(VfxSlideReady));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                return;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(portraitAnchor01.x), Mathf.Clamp01(portraitAnchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 280f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxSlideReady>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx.Build();
        }

        void Build()
        {
            var green = VisualTokens.SlideGreen;
            var hot = Color.Lerp(green, Color.white, 0.42f);

            _glow = Img("glow", UiSprites.Soft(), new Color(green.r, green.g, green.b, 0.52f), 176f);
            _core = Img("core", UiSprites.Soft(), new Color(hot.r, hot.g, hot.b, 0.78f), 56f);
            _ring = Img("ring", UiSprites.Circle(), new Color(green.r, green.g, green.b, 0.95f), 72f);
            _rim = Img("rim", UiSprites.Circle(), new Color(1f, 1f, 1f, 0.92f), 44f);

            _ghost = MkText("ghost", Word, 30, Ink, new Vector2(260f, 48f), new Vector2(3f, 10f));
            _label = MkText("label", Word, 30, green, new Vector2(260f, 48f), new Vector2(0f, 14f));

            _chevrons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var pip = Img("up" + i, UiSprites.Star(), green, 0f);
                if (pip == null) continue;
                pip.rectTransform.sizeDelta = new Vector2(16f - i * 2f, 16f - i * 2f);
                pip.rectTransform.anchoredPosition = new Vector2(0f, 36f + i * 14f);
                pip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                pip.color = Color.clear;
                _chevrons[i] = pip;
            }

            for (int i = 0; i < 8; i++)
            {
                var col = (i & 1) == 0 ? green : hot;
                Spark.Spawn(transform, col, (i / 8f) * Mathf.PI * 2f, 74f, false);
            }

            transform.localScale = Vector3.one * 1.22f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var fade = u < 0.12f ? u / 0.12f
                : u < 0.62f ? 1f
                : 1f - (u - 0.62f) / 0.38f;
            fade = Mathf.Clamp01(fade);

            var pop = u < 0.16f
                ? Mathf.Lerp(1.22f, 1.04f, u / 0.16f)
                : Mathf.Lerp(1.04f, 0.96f, (u - 0.16f) / 0.84f);
            transform.localScale = Vector3.one * pop;

            var ease = 1f - (1f - u) * (1f - u);
            SetA(_glow, 0.50f * fade);
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.48f, ease);

            SetA(_core, 0.72f * fade * (1f - u * 0.40f));
            if (_core != null)
                _core.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.28f, u);

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.38f, 2.18f, ease);
                SetA(_ring, 0.92f * (1f - u) * fade);
            }
            if (_rim != null)
            {
                _rim.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.72f, Mathf.Clamp01(u * 1.25f));
                SetA(_rim, 0.82f * Mathf.Clamp01(1f - u * 1.45f) * fade);
            }

            var rise = 22f * ease;
            if (_label != null)
                _label.rectTransform.anchoredPosition = new Vector2(0f, 14f + rise);
            if (_ghost != null)
                _ghost.rectTransform.anchoredPosition = new Vector2(3f, 10f + rise);
            Fade(_label, fade);
            Fade(_ghost, fade);
            if (_label != null) _label.text = Word;
            if (_ghost != null) _ghost.text = Word;

            if (_chevrons != null)
            {
                for (int i = 0; i < _chevrons.Length; i++)
                {
                    var img = _chevrons[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - i * 0.08f) / 0.55f);
                    var a = t < 0.18f ? t / 0.18f : 1f - Mathf.Clamp01((t - 0.42f) / 0.58f);
                    var c = VisualTokens.SlideGreen;
                    c.a = Mathf.Clamp01(a) * fade;
                    img.color = c;
                    img.rectTransform.anchoredPosition = new Vector2(0f, 36f + i * 14f + 28f * t);
                }
            }

            if (_age >= Duration) Destroy(gameObject);
        }

        Image Img(string name, Sprite sprite, Color color, float size)
        {
            if (transform == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            if (rt == null || img == null)
            {
                Object.Destroy(go);
                return null;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 dim, Vector2 pos)
        {
            if (transform == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var tx = go.GetComponent<Text>();
            if (rt == null || tx == null)
            {
                Object.Destroy(go);
                return null;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
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
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2f, -2f);
            }
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
