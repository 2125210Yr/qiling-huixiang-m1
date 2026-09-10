using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Gold loot chip at a 01-anchor. Copy is 获得 + itemName. Never GOT / LOOT.
    /// Pops ~0.7s then destroys itself.
    /// </summary>
    public sealed class VfxLootDrop : MonoBehaviour
    {
        const float Life = 0.70f;
        const float FadeFrom = 0.52f;
        const float Pop = 1.28f;
        const float Rise = 56f;
        const float PillH = 40f;
        const float GemPx = 28f;

        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color Ink = VisualTokens.TapWhite;
        static readonly Color PillCol = new Color(0.08f, 0.06f, 0.02f, 0.92f);
        static readonly Color RimCol = new Color(0.82f, 0.58f, 0.08f, 1f);

        Image _glow;
        Image _rim;
        Image _pill;
        Image _gem;
        Image _spark;
        Text _ghost;
        Text _label;
        Vector2 _from;
        float _life = Life;
        float _pillW;

        public static void Play(Transform parent, Vector2 anchor01, string itemName)
        {
            if (parent == null) return;

            var go = new GameObject("lootDrop", typeof(RectTransform), typeof(VfxLootDrop));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 140f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            go.GetComponent<VfxLootDrop>().Build(itemName);
        }

        void Build(string itemName)
        {
            var name = itemName ?? "";
            var word = name.Length == 0 ? "获得" : "获得  " + name;
            var n = word.Length;
            _pillW = Mathf.Clamp(72f + n * 24f, 160f, 400f);
            _from = new Vector2(Random.Range(-10f, 10f), Random.Range(-22f, -8f));
            _life = Life;

            _glow = Img("glow", UiSprites.Soft(), new Color(Gold.r, Gold.g, Gold.b, 0.48f), 0f);
            _glow.rectTransform.sizeDelta = new Vector2(_pillW + 72f, 96f);

            _rim = Img("rim", UiSprites.Pill(), RimCol, 0f);
            _rim.rectTransform.sizeDelta = new Vector2(_pillW + 8f, PillH + 8f);

            _pill = Img("pill", UiSprites.Pill(), PillCol, 0f);
            _pill.rectTransform.sizeDelta = new Vector2(_pillW, PillH);

            var gemX = -_pillW * 0.5f + 26f;
            _gem = Img("gem", UiSprites.Hex(), Hot, GemPx);
            _gem.rectTransform.anchoredPosition = new Vector2(gemX, 1f);

            _spark = Img("spark", UiSprites.Spark(), Gold, 16f);
            _spark.rectTransform.anchoredPosition = new Vector2(gemX, 1f);

            var textDim = new Vector2(_pillW - 28f, 40f);
            var textPos = new Vector2(14f, 0f);
            _ghost = MkText("ghost", word, 24, Darken(Hot), textDim, textPos + new Vector2(2f, -2f));
            _label = MkText("label", word, 24, Ink, textDim, textPos);

            ((RectTransform)transform).anchoredPosition = Snap(_from);
            transform.localScale = Vector3.one * Pop;

            for (int i = 0; i < 8; i++)
            {
                var col = (i & 1) == 0 ? Gold : Hot;
                Spark.Spawn(transform, col, (i / 8f) * Mathf.PI * 2f, 52f, true);
            }
        }

        void Update()
        {
            _life -= Time.unscaledDeltaTime;
            var u = 1f - Mathf.Clamp01(_life / Life);
            var punch = u < 0.14f
                ? Mathf.Lerp(Pop, 1.08f, u / 0.14f)
                : Mathf.Lerp(1.08f, 0.94f, (u - 0.14f) / 0.86f);
            transform.localScale = Vector3.one * punch;
            ((RectTransform)transform).anchoredPosition = Snap(_from + new Vector2(_from.x * 0.08f, Rise * u));

            var a = u < FadeFrom ? 1f : 1f - (u - FadeFrom) / Mathf.Max(0.01f, 1f - FadeFrom);
            a = Mathf.Clamp01(a);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.22f, u);
                SetA(_glow, 0.48f * a);
            }
            SetA(_rim, a);
            SetA(_pill, PillCol.a * a);
            SetA(_gem, a);
            if (_spark != null)
            {
                var spin = u * 220f;
                _spark.rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);
                _spark.transform.localScale = Vector3.one * (0.85f + 0.22f * Mathf.Sin(u * 18f));
                SetA(_spark, a);
            }
            Fade(_ghost, a);
            Fade(_label, a);

            if (_life <= 0f) Destroy(gameObject);
        }

        static Color Darken(Color c)
        {
            return new Color(c.r * 0.32f, c.g * 0.26f, c.b * 0.28f, 1f);
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        }

        Image Img(string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 dim, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
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
            ol.effectDistance = new Vector2(2f, -2f);
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
