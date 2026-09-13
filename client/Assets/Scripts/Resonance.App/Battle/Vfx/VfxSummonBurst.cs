using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Summon reveal: gold/void burst, SUMMON + name + red stars (1..5). ~1.2s.
    /// </summary>
    public sealed class VfxSummonBurst : MonoBehaviour
    {
        public const float Duration = 1.20f;

        const string TitleWord = "SUMMON";
        const int RayN = 8;

        static readonly Color VeilCol = new Color(0.02f, 0.02f, 0.03f, 0.90f);
        static readonly Color VoidCol = VisualTokens.BgVoid;
        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color GoldHot = VisualTokens.YellowValue;
        static readonly Color GoldMetal = VisualTokens.GoldMetal;
        static readonly Color GoldInk = new Color(0.28f, 0.14f, 0.02f, 1f);
        static readonly Color StarCol = VisualTokens.StarEvolved;

        Image _veil;
        Image _glow;
        Image _well;
        Image _ring;
        Image _hex;
        Image _core;
        Image[] _rays;
        Image[] _stars;
        Text _titleGhost;
        Text _title;
        Text _name;
        int _starN;
        float _age;
        bool _burst;

        public static void Play(Transform parent, string charName, int star)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxSummonBurst>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.Destroy(live[i].gameObject);
            }

            var go = new GameObject("vfxSummonBurst", typeof(RectTransform), typeof(VfxSummonBurst));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxSummonBurst>().Build(charName, star);
        }

        void Build(string charName, int star)
        {
            _starN = Mathf.Clamp(star, 1, 5);

            _veil = Full("veil", VeilCol);

            _glow = Img("glow", UiSprites.Soft(), new Color(Gold.r, Gold.g, Gold.b, 0.42f),
                new Vector2(0.50f, 0.54f), new Vector2(720f, 720f));
            _glow.transform.localScale = Vector3.one * 0.28f;

            _well = Img("well", UiSprites.Circle(), new Color(VoidCol.r, VoidCol.g, VoidCol.b, 0.96f),
                new Vector2(0.50f, 0.54f), new Vector2(280f, 280f));
            _well.transform.localScale = Vector3.one * 0.22f;

            _ring = Img("ring", UiSprites.HexRing(), GoldMetal,
                new Vector2(0.50f, 0.54f), new Vector2(240f, 240f));
            _ring.transform.localScale = Vector3.one * 0.18f;

            _hex = Img("hex", UiSprites.Hex(), Gold,
                new Vector2(0.50f, 0.54f), new Vector2(128f, 128f));
            _hex.transform.localScale = Vector3.one * 0.18f;

            _core = Img("core", UiSprites.Spark(), GoldHot,
                new Vector2(0.50f, 0.54f), new Vector2(72f, 72f));
            _core.transform.localScale = Vector3.one * 0.18f;

            _rays = new Image[RayN];
            for (int i = 0; i < RayN; i++)
            {
                var ang = (i / (float)RayN) * 360f + 12f;
                var thick = 18f + (i % 3) * 8f;
                var len = 520f + (i % 4) * 80f;
                var col = (i & 1) == 0 ? Gold : GoldHot;
                col.a = 0.78f;
                var ray = Img("ray" + i, UiSprites.Slash(), col,
                    new Vector2(0.50f, 0.54f), new Vector2(len, thick));
                ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, ang);
                ray.rectTransform.pivot = new Vector2(0.12f, 0.5f);
                ray.transform.localScale = new Vector3(0.12f, 1.15f, 1f);
                _rays[i] = ray;
            }

            _titleGhost = MkText("titleGhost", TitleWord, 92, GoldInk,
                new Vector2(0.50f, 0.74f), new Vector2(640f, 140f));
            _titleGhost.rectTransform.anchoredPosition = new Vector2(8f, -8f);

            _title = MkText("title", TitleWord, 92, Gold,
                new Vector2(0.50f, 0.74f), new Vector2(640f, 140f));
            _title.transform.localScale = Vector3.one * 1.85f;

            var name = charName ?? "";
            _name = MkText("name", name, 40, VisualTokens.TapWhite,
                new Vector2(0.50f, 0.64f), new Vector2(720f, 72f));
            _name.transform.localScale = Vector3.one * 0.55f;
            if (name.Length == 0)
                _name.enabled = false;

            _stars = new Image[_starN];
            var span = (_starN - 1) * 0.055f;
            var left = 0.50f - span * 0.5f;
            for (int i = 0; i < _starN; i++)
            {
                var x = _starN == 1 ? 0.50f : left + i * 0.055f;
                var starImg = Img("star" + i, UiSprites.Star(), StarCol,
                    new Vector2(x, 0.36f), new Vector2(56f, 56f));
                starImg.transform.localScale = Vector3.one * 0.20f;
                _stars[i] = starImg;
            }

            CanvasShake.Punch(16f, 0.22f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var a = u < 0.08f ? u / 0.08f
                : u < 0.82f ? 1f
                : 1f - (u - 0.82f) / 0.18f;
            a = Mathf.Clamp01(a);

            if (!_burst && _age >= 0.06f)
            {
                _burst = true;
                for (int i = 0; i < 12; i++)
                {
                    var ang = (i / 12f) * Mathf.PI * 2f;
                    Spark.Spawn(transform, Gold, ang, 148f, true);
                }
                for (int i = 0; i < 6; i++)
                    Spark.Spawn(transform, Color.white, (i / 6f) * Mathf.PI * 2f + 0.21f, 88f, true);
            }

            SetA(_veil, VeilCol.a * a);

            var bloom = 1f - (1f - Mathf.Clamp01(_age / 0.22f)) * (1f - Mathf.Clamp01(_age / 0.22f));
            SetA(_glow, 0.42f * a);
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.18f, bloom);

            SetA(_well, 0.96f * a);
            if (_well != null)
                _well.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 1.04f, bloom);

            SetA(_ring, 0.95f * a);
            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 1.08f, bloom);
                _ring.rectTransform.localEulerAngles = new Vector3(0f, 0f, _age * 48f);
            }

            SetA(_hex, a);
            if (_hex != null)
            {
                _hex.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 1f, bloom);
                _hex.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_age * 32f);
            }

            SetA(_core, a);
            if (_core != null)
            {
                var pulse = 1f + 0.10f * Mathf.Sin(_age * 14f);
                _core.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 1f, bloom) * pulse;
            }

            var rayU = Mathf.Clamp01(_age / 0.18f);
            var rayEase = 1f - (1f - rayU) * (1f - rayU);
            if (_rays != null)
            {
                for (int i = 0; i < _rays.Length; i++)
                {
                    var ray = _rays[i];
                    if (ray == null) continue;
                    var delay = (i % 4) * 0.02f;
                    var ru = Mathf.Clamp01((_age - delay) / 0.18f);
                    var rx = Mathf.Lerp(0.12f, 1.06f, 1f - (1f - ru) * (1f - ru));
                    ray.transform.localScale = new Vector3(rx, 1.15f, 1f);
                    SetA(ray, 0.78f * a * rayEase);
                }
            }

            var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));
            var titleScale = Mathf.Lerp(1.85f, 1f, slam);
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

            var nameU = Mathf.Clamp01((_age - 0.12f) / 0.16f);
            float nameS;
            if (nameU <= 0f) nameS = 0.55f;
            else if (nameU < 0.55f) nameS = Mathf.Lerp(0.55f, 1.12f, nameU / 0.55f);
            else nameS = Mathf.Lerp(1.12f, 1f, (nameU - 0.55f) / 0.45f);
            if (_name != null)
            {
                _name.transform.localScale = Vector3.one * nameS;
                Fade(_name, a * nameU);
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    var star = _stars[i];
                    if (star == null) continue;
                    var t = Mathf.Clamp01((_age - 0.22f - i * 0.07f) / 0.14f);
                    float s;
                    if (t <= 0f) s = 0.20f;
                    else if (t < 0.55f) s = Mathf.Lerp(0.20f, 1.22f, t / 0.55f);
                    else s = Mathf.Lerp(1.22f, 1f, (t - 0.55f) / 0.45f);
                    star.transform.localScale = Vector3.one * s;
                    var spin = Mathf.Lerp(-28f, 8f, t);
                    star.rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);
                    SetA(star, a * t);
                }
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
