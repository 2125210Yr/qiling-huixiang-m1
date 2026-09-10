using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Portrait-top chip when TAP charge fills. Yellow ring pulse + 点按已满.
    /// Never TAP FULL. Holds ~0.6s then fades.
    /// </summary>
    public sealed class VfxTapReady : MonoBehaviour
    {
        const string Word = "点按已满";
        const float Hold = 0.60f;
        const float FadeLen = 0.18f;
        const float Life = Hold + FadeLen;
        const float ChipY = 108f;
        const float PillW = 168f;
        const float PillH = 36f;
        const float RingPx = 64f;

        static readonly Color RingCol = VisualTokens.YellowValue;
        static readonly Color Ink = VisualTokens.TapWhite;
        static readonly Color PillCol = new Color(0.05f, 0.04f, 0.06f, 0.90f);

        Image _ring;
        Image _ring2;
        Image _pill;
        Text _ghost;
        Text _label;
        float _age;

        public static void Play(Transform parent, Vector2 portraitAnchor01)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxTapReady>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.DestroyImmediate(live[i].gameObject);
            }

            var go = new GameObject("tapReady", typeof(RectTransform), typeof(VfxTapReady));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                return;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(portraitAnchor01.x), Mathf.Clamp01(portraitAnchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 160f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            var fx = go.GetComponent<VfxTapReady>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx.Build();
        }

        void Build()
        {
            var chip = new Vector2(0f, ChipY);

            _ring = Img("ring", UiSprites.Circle(), RingCol, RingPx);
            if (_ring != null) _ring.rectTransform.anchoredPosition = chip;

            _ring2 = Img("ring2", UiSprites.Circle(), Ink, RingPx * 0.82f);
            if (_ring2 != null) _ring2.rectTransform.anchoredPosition = chip;

            _pill = Img("pill", UiSprites.Pill(), PillCol, 0f);
            if (_pill != null)
            {
                _pill.rectTransform.sizeDelta = new Vector2(PillW, PillH);
                _pill.rectTransform.anchoredPosition = chip;
            }

            _ghost = MkText("ghost", Word, 22, Darken(RingCol), new Vector2(PillW + 8f, 40f), chip + new Vector2(2f, -2f));
            _label = MkText("label", Word, 22, Ink, new Vector2(PillW + 8f, 40f), chip);

            transform.localScale = Vector3.one * 0.55f;

            for (int i = 0; i < 6; i++)
            {
                var col = (i & 1) == 0 ? RingCol : Ink;
                Spark.Spawn(transform, col, (i / 6f) * Mathf.PI * 2f, 44f, true);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var fade = _age < Hold ? 1f : 1f - Mathf.Clamp01((_age - Hold) / FadeLen);
            fade = Mathf.Clamp01(fade);

            float s;
            if (u < 0.16f) s = Mathf.Lerp(0.55f, 1.18f, u / 0.16f);
            else s = Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((u - 0.16f) / 0.20f));
            transform.localScale = Vector3.one * s;

            var ping = Mathf.Clamp01(_age / 0.48f);
            var pingEase = 1f - (1f - ping) * (1f - ping);
            if (_ring != null)
            {
                _ring.rectTransform.anchoredPosition = new Vector2(0f, ChipY);
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 2.05f, pingEase);
                SetA(_ring, 0.88f * (1f - ping) * fade);
            }

            var ping2 = Mathf.Clamp01((_age - 0.10f) / 0.48f);
            var ping2Ease = 1f - (1f - ping2) * (1f - ping2);
            if (_ring2 != null)
            {
                _ring2.rectTransform.anchoredPosition = new Vector2(0f, ChipY);
                _ring2.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.72f, ping2Ease);
                SetA(_ring2, 0.70f * (1f - ping2) * fade);
            }

            SetA(_pill, PillCol.a * fade);
            Fade(_ghost, fade);
            Fade(_label, fade);
            if (_ghost != null) _ghost.text = Word;
            if (_label != null) _label.text = Word;

            if (_age >= Life) Destroy(gameObject);
        }

        static Color Darken(Color c)
        {
            return new Color(c.r * 0.32f, c.g * 0.26f, c.b * 0.28f, 1f);
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
