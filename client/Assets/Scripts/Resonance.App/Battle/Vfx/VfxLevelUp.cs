using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Gold level-up chip: 等级提升 + LV n. ~1.0s.
    /// Never LEVEL UP.
    /// </summary>
    public sealed class VfxLevelUp : MonoBehaviour
    {
        const float Life = 1.00f;
        const float Hold = 0.72f;
        const string Word = "等级提升";

        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color Ink = new Color(0.42f, 0.16f, 0.02f, 0.92f);
        static readonly Color ChipBg = new Color(0.05f, 0.04f, 0.06f, 0.90f);

        Image _flash;
        Transform _chip;
        Image _glow;
        Image _pill;
        Image _ring;
        Image _star;
        Text _ghost;
        Text _title;
        Text _lvGhost;
        Text _lv;
        float _age;
        bool _burst;

        public static void Play(Transform parent, int newLevel)
        {
            if (parent == null) return;

            var go = new GameObject("vfxLevelUp", typeof(RectTransform), typeof(VfxLevelUp));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxLevelUp>().Build(newLevel);
        }

        void Build(int newLevel)
        {
            var lvWord = "LV " + Mathf.Max(0, newLevel);

            _flash = Img(transform, "flash", UiSprites.Soft(), Color.clear,
                new Vector2(0.50f, 0.58f), new Vector2(720f, 720f));

            _chip = Root("chip", new Vector2(0.50f, 0.58f));
            _glow = Img(_chip, "glow", UiSprites.Soft(),
                new Color(Gold.r, Gold.g, Gold.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(560f, 220f));
            _pill = Img(_chip, "pill", UiSprites.Pill(), ChipBg,
                new Vector2(0.50f, 0.50f), new Vector2(420f, 128f));
            _ring = Img(_chip, "ring", UiSprites.Circle(),
                new Color(Gold.r, Gold.g, Gold.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(88f, 88f));
            _star = Img(_chip, "star", UiSprites.Star(),
                new Color(Hot.r, Hot.g, Hot.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(48f, 48f));
            _star.rectTransform.anchoredPosition = new Vector2(0f, 78f);

            _ghost = MkText(_chip, "ghost", Word, 52, Ink,
                new Vector2(0.50f, 0.50f), new Vector2(520f, 80f), new Vector2(5f, 18f));
            _title = MkText(_chip, "tx", Word, 52, Gold,
                new Vector2(0.50f, 0.50f), new Vector2(520f, 80f), new Vector2(0f, 22f));
            _lvGhost = MkText(_chip, "lvGhost", lvWord, 36, Ink,
                new Vector2(0.50f, 0.50f), new Vector2(280f, 52f), new Vector2(4f, -28f));
            _lv = MkText(_chip, "lv", lvWord, 36, Hot,
                new Vector2(0.50f, 0.50f), new Vector2(280f, 52f), new Vector2(0f, -24f));

            Fade(_ghost, 0f);
            Fade(_title, 0f);
            Fade(_lvGhost, 0f);
            Fade(_lv, 0f);
            SetA(_pill, 0f);
            _chip.localScale = Vector3.one * 0.50f;

            CanvasShake.Punch(10f, 0.16f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var fade = _age < Hold ? 1f : 1f - Mathf.Clamp01((_age - Hold) / (Life - Hold));

            if (!_burst && _age >= 0.02f)
            {
                _burst = true;
                for (int i = 0; i < 10; i++)
                    Spark.Spawn(_chip, (i & 1) == 0 ? Gold : Hot,
                        (i / 10f) * Mathf.PI * 2f, 86f, true);
                for (int i = 0; i < 4; i++)
                    Spark.Spawn(_chip, Hot, (i / 4f) * Mathf.PI * 2f + 0.30f, 52f, true, true);
            }

            TickFlash(fade);
            TickChip(fade);

            if (_age >= Life) Destroy(gameObject);
        }

        void TickFlash(float fade)
        {
            if (_flash == null) return;
            var peak = 1f - Mathf.Abs(Mathf.Clamp01(_age / 0.16f) - 0.40f) / 0.40f;
            peak = Mathf.Clamp01(peak);
            _flash.color = new Color(Gold.r, Gold.g, Gold.b, 0.48f * peak * fade);
            _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01(_age / 0.20f));
        }

        void TickChip(float fade)
        {
            if (_chip != null) _chip.localScale = Vector3.one * ScalePop(_age, 1.22f);
            var a = fade * Gate(_age);
            SetA(_glow, 0.55f * a);
            SetA(_pill, ChipBg.a * a);
            Fade(_ghost, a);
            Fade(_title, a);
            Fade(_lvGhost, a);
            Fade(_lv, a);

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 2.10f, 1f - (1f - Mathf.Clamp01(_age / Life)) * (1f - Mathf.Clamp01(_age / Life)));
                SetA(_ring, 0.70f * (1f - Mathf.Clamp01(_age / Life)) * a);
            }

            if (_star != null)
            {
                var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));
                float s;
                if (slam < 1f) s = Mathf.Lerp(0.35f, 1.22f, slam);
                else s = Mathf.Lerp(1.22f, 1f, Mathf.Clamp01((_age - 0.14f) / 0.16f));
                _star.transform.localScale = Vector3.one * s;
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-28f, 12f, slam));
                var sc = Color.Lerp(Gold, Hot, slam);
                sc.a = a;
                _star.color = sc;
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
