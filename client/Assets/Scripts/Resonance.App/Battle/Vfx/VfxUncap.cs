using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Red star burst + UNCAP. Stars 1..6. ~0.9s.
    /// </summary>
    public sealed class VfxUncap : MonoBehaviour
    {
        const float Life = 0.90f;
        const float WordAt = 0.00f;
        const string Word = "UNCAP";

        static readonly Color Red = VisualTokens.StarEvolved;
        static readonly Color Hot = Color.Lerp(Color.white, VisualTokens.StarEvolved, 0.22f);
        static readonly Color Ink = new Color(0.28f, 0.04f, 0.02f, 1f);

        Transform _body;
        Image _flash;
        Image _glow;
        Image _ring;
        Image _wordGlow;
        Image[] _stars;
        Vector2[] _dir;
        float[] _dist;
        Text _ghost;
        Text _word;
        int _n;
        float _age;
        bool _didBurst;

        public static void Play(Transform parent, int stars)
        {
            if (parent == null) return;

            var go = new GameObject("vfxUncap", typeof(RectTransform), typeof(VfxUncap));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxUncap>().Build(stars);
        }

        void Build(int stars)
        {
            _n = Mathf.Clamp(stars, 1, 6);

            _flash = Img(transform, "flash", UiSprites.Soft(), Color.clear,
                new Vector2(0.50f, 0.56f), new Vector2(780f, 780f));

            _body = Root("body", new Vector2(0.50f, 0.56f));

            _glow = Img(_body, "glow", UiSprites.Soft(),
                new Color(Red.r, Red.g, Red.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(280f, 280f));
            _ring = Img(_body, "ring", UiSprites.Circle(),
                new Color(Red.r, Red.g, Red.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(96f, 96f));

            _stars = new Image[_n];
            _dir = new Vector2[_n];
            _dist = new float[_n];
            for (int i = 0; i < _n; i++)
            {
                var ang = (i / (float)_n) * Mathf.PI * 2f + Mathf.PI * 0.5f;
                _dir[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                _dist[i] = _n == 1 ? 70f : 88f + (i % 3) * 14f;
                var sz = _n == 1 ? 52f : (i & 1) == 0 ? 40f : 30f;
                var img = Img(_body, "star" + i, UiSprites.Star(),
                    new Color(Red.r, Red.g, Red.b, 0f),
                    new Vector2(0.50f, 0.50f), new Vector2(sz, sz));
                img.transform.localScale = Vector3.one * 0.35f;
                _stars[i] = img;
            }

            _wordGlow = Img(_body, "wordGlow", UiSprites.Soft(),
                new Color(Red.r, Red.g, Red.b, 0f),
                new Vector2(0.50f, 0.50f), new Vector2(520f, 160f));
            _ghost = MkText(_body, "ghost", Word, 78, Ink,
                new Vector2(0.50f, 0.50f), new Vector2(480f, 120f), new Vector2(6f, -5f));
            _word = MkText(_body, "tx", Word, 78, Red,
                new Vector2(0.50f, 0.50f), new Vector2(480f, 120f), Vector2.zero);
            Fade(_ghost, 0f);
            Fade(_word, 0f);
            _ghost.transform.localScale = Vector3.one * 0.50f;
            _word.transform.localScale = Vector3.one * 0.50f;

            CanvasShake.Punch(12f, 0.18f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var hold = Life - 0.28f;
            var fade = _age < hold ? 1f : 1f - Mathf.Clamp01((_age - hold) / 0.28f);

            TickFlash(fade);
            TickRing(fade);
            TickStars(fade);
            TickWord(fade);

            if (_age >= 0.02f && !_didBurst)
            {
                _didBurst = true;
                var at = _body != null ? _body : transform;
                for (int i = 0; i < 8; i++)
                    Spark.Spawn(at, (i & 1) == 0 ? Red : Hot,
                        (i / 8f) * Mathf.PI * 2f, 78f, true);
                for (int i = 0; i < 4; i++)
                    Spark.Spawn(at, Hot,
                        (i / 4f) * Mathf.PI * 2f + 0.40f, 52f, true, true);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        void TickFlash(float fade)
        {
            if (_flash == null) return;
            var peak = 1f - Mathf.Abs(Mathf.Clamp01(_age / 0.16f) - 0.40f) / 0.40f;
            peak = Mathf.Clamp01(peak);
            _flash.color = new Color(Hot.r, Hot.g, Hot.b, 0.62f * peak * fade);
            _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01(_age / 0.20f));
        }

        void TickRing(float fade)
        {
            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.38f, Mathf.Clamp01(_age / 0.22f));
                SetA(_glow, 0.55f * fade);
            }
            if (_ring == null) return;
            var u = Mathf.Clamp01(_age / 0.50f);
            var ease = 1f - (1f - u) * (1f - u);
            _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 2.35f, ease);
            SetA(_ring, 0.72f * (1f - u) * fade);
        }

        void TickStars(float fade)
        {
            if (_stars == null) return;
            for (int i = 0; i < _stars.Length; i++)
            {
                var img = _stars[i];
                if (img == null) continue;
                var t = _age - i * 0.04f;
                var u = Mathf.Clamp01(t / 0.36f);
                var ease = 1f - (1f - u) * (1f - u);
                img.rectTransform.anchoredPosition = _dir[i] * (_dist[i] * ease);
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, ((i & 1) == 0 ? 140f : -160f) * ease);
                float s;
                if (u < 0.45f) s = Mathf.Lerp(0.35f, 1.22f, u / 0.45f);
                else s = Mathf.Lerp(1.22f, 1f, (u - 0.45f) / 0.55f);
                img.transform.localScale = Vector3.one * s;
                var c = (i & 1) == 0 ? Red : Hot;
                c.a = fade * Gate(t);
                img.color = c;
            }
        }

        void TickWord(float fade)
        {
            var t = _age - WordAt;
            if (_word != null) _word.transform.localScale = Vector3.one * ScalePop(t, 1.22f);
            if (_ghost != null) _ghost.transform.localScale = Vector3.one * ScalePop(t, 1.22f);
            var a = fade * Gate(t);
            SetA(_wordGlow, 0.55f * a);
            Fade(_ghost, a);
            Fade(_word, a);
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
