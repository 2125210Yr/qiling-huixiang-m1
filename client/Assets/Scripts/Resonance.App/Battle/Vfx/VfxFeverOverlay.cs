using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Additive fever overlay. Does not replace HUD. Visible label is 狂热时间 only.
    /// 细线计时轨 + 边缘速度线; 无 Round 底槽 / 无填充色块.
    /// </summary>
    public sealed class VfxFeverOverlay : MonoBehaviour
    {
        const int LineN = 22;
        const int ArcN = 9;

        static readonly Color Pink = new Color(0.95f, 0.38f, 0.82f, 1f);
        static readonly Color Magenta = new Color(0.78f, 0.22f, 0.74f, 1f);

        static VfxFeverOverlay _live;
        static Sprite _rainbow;

        Image _railT;
        Image _railB;
        Image _capL;
        Image _capR;
        Image _barFill;
        Text _label;
        Image[] _lines;
        float[] _linePhase;
        float[] _lineLen;
        Image[] _arc;
        float _life = 7f;
        float _left;
        float _pop;

        public static bool Active { get; private set; }

        public static void Show(Transform parent, float seconds)
        {
            if (parent == null) return;
            if (seconds <= 0f)
            {
                Hide();
                return;
            }
            Ensure(parent).Begin(seconds);
        }

        public static void Hide()
        {
            if (_live != null) _live.End();
            Active = false;
        }

        static VfxFeverOverlay Ensure(Transform parent)
        {
            if (_live != null)
            {
                if (_live.transform.parent != parent)
                    _live.transform.SetParent(parent, false);
                Stretch(_live.transform as RectTransform);
                return _live;
            }

            var existing = parent.GetComponentInChildren<VfxFeverOverlay>(true);
            if (existing != null)
            {
                _live = existing;
                existing.transform.SetParent(parent, false);
                Stretch(existing.transform as RectTransform);
                return existing;
            }

            var go = new GameObject("feverOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxFeverOverlay));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            var fx = go.GetComponent<VfxFeverOverlay>();
            fx.Build();
            _live = fx;
            return fx;
        }

        void OnDestroy()
        {
            if (_live == this)
            {
                _live = null;
                Active = false;
            }
        }

        void Build()
        {
            var root = transform;

            // 细线计时轨：上下发线 + 两端刻线，取代会读成盒子的 Round 底槽。
            _railT = MkImg(root, "railT", new Vector2(0.5f, 0.236f), new Vector2(656f, 2f), 0f,
                new Color(0.92f, 0.44f, 0.86f, 0.55f), UiSprites.Pixel());
            _railT.rectTransform.anchoredPosition = new Vector2(0f, 13f);
            _railB = MkImg(root, "railB", new Vector2(0.5f, 0.236f), new Vector2(656f, 2f), 0f,
                new Color(0.92f, 0.44f, 0.86f, 0.55f), UiSprites.Pixel());
            _railB.rectTransform.anchoredPosition = new Vector2(0f, -13f);
            _capL = MkImg(root, "capL", new Vector2(0.5f, 0.236f), new Vector2(2f, 30f), 0f,
                new Color(0.92f, 0.44f, 0.86f, 0.65f), UiSprites.Pixel());
            _capL.rectTransform.anchoredPosition = new Vector2(-328f, 0f);
            _capR = MkImg(root, "capR", new Vector2(0.5f, 0.236f), new Vector2(2f, 30f), 0f,
                new Color(0.92f, 0.44f, 0.86f, 0.65f), UiSprites.Pixel());
            _capR.rectTransform.anchoredPosition = new Vector2(328f, 0f);

            _barFill = MkImg(root, "barFill", new Vector2(0.5f, 0.236f), new Vector2(640f, 10f), 0f,
                Color.white, RainbowSprite());
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _barFill.fillAmount = 1f;

            _label = MkText(root, "label", "狂热时间", 40, new Vector2(0.5f, 0.318f), new Vector2(640f, 72f), Pink);

            _arc = new Image[0];

            // 边缘速度线：左右两侧细斜线，呼吸明灭 + 上下漂移。
            _lines = new Image[LineN];
            _linePhase = new float[LineN];
            _lineLen = new float[LineN];
            for (int i = 0; i < LineN; i++)
            {
                var left = (i & 1) == 0;
                var y = 0.07f + 0.86f * ((i * 0.618034f) % 1f);
                var img = MkImg(root, "ln" + i, new Vector2(left ? 0.012f : 0.988f, y),
                    new Vector2(5f, 80f), left ? -16f : 16f, Color.clear, UiSprites.Slash());
                _lines[i] = img;
                _linePhase[i] = i * 1.37f;
                _lineLen[i] = 56f + 118f * ((i * 0.754878f) % 1f);
            }

            gameObject.SetActive(false);
        }

        void Begin(float seconds)
        {
            _life = Mathf.Max(0.05f, seconds);
            _left = _life;
            _pop = 0.28f;
            Active = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_barFill != null) _barFill.fillAmount = 1f;
            if (_label != null)
            {
                _label.text = "狂热时间";
                _label.color = Pink;
            }
        }

        void End()
        {
            _left = 0f;
            Active = false;
            if (gameObject != null) gameObject.SetActive(false);
        }

        void Update()
        {
            if (!Active) return;
            var dt = Time.unscaledDeltaTime;
            _left -= dt;
            if (_left <= 0f)
            {
                Hide();
                return;
            }
            Paint();
        }

        void Paint()
        {
            var u = Mathf.Clamp01(_left / _life);
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8.2f);
            if (_barFill != null)
            {
                _barFill.fillAmount = u;
                _barFill.color = Color.Lerp(Color.white, new Color(1f, 0.82f, 1f, 1f), pulse * 0.35f);
            }
            var railA = 0.34f + 0.22f * pulse;
            if (_railT != null) { var c = _railT.color; c.a = railA; _railT.color = c; }
            if (_railB != null) { var c = _railB.color; c.a = railA; _railB.color = c; }
            if (_capL != null) { var c = _capL.color; c.a = railA + 0.14f; _capL.color = c; }
            if (_capR != null) { var c = _capR.color; c.a = railA + 0.14f; _capR.color = c; }
            if (_label != null)
            {
                var lc = Color.Lerp(Pink, Color.white, pulse * 0.30f);
                lc.a = 0.82f + 0.18f * pulse;
                _label.color = lc;
            }
            if (_arc != null)
            {
                for (int i = 0; i < _arc.Length; i++)
                {
                    var img = _arc[i];
                    if (img == null) continue;
                    var t = (i / (float)Mathf.Max(1, _arc.Length - 1) + Time.unscaledTime * 0.07f) % 1f;
                    var k = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5.6f + i * 0.38f));
                    var col = Rainbow(t);
                    col.a = (0.28f + 0.50f * k) * Mathf.Lerp(0.45f, 1f, u);
                    img.color = col;
                    img.rectTransform.localScale = Vector3.one * (0.92f + 0.14f * k);
                }
            }
            if (_lines == null) return;
            for (int i = 0; i < _lines.Length; i++)
            {
                var img = _lines[i];
                if (img == null) continue;
                var k = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 11f + _linePhase[i]));
                img.color = new Color(1f, 0.92f, 1f, (0.06f + 0.22f * k) * Mathf.Lerp(0.45f, 1f, u));
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, _lineLen[i] * (0.78f + 0.28f * k));
                var pos = rt.anchoredPosition;
                pos.y = Mathf.Sin(Time.unscaledTime * 2.4f + _linePhase[i]) * 24f;
                rt.anchoredPosition = pos;
            }
        }

        static Image MkImg(Transform parent, string name, Vector2 anchor, Vector2 size, float rot, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, rot);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 dim, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.color = color;
            tx.text = text;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0.18f, 0.02f, 0.16f, 0.95f);
            ol.effectDistance = new Vector2(3f, -3f);
            return tx;
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static Sprite RainbowSprite()
        {
            if (_rainbow != null) return _rainbow;
            const int w = 96;
            const int h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                var c = (Color32)Rainbow(x / (float)(w - 1));
                for (int y = 0; y < h; y++)
                {
                    var edge = Mathf.Clamp01(1f - Mathf.Abs((y + 0.5f) / h - 0.5f) * 2.2f);
                    var cc = c;
                    cc.a = (byte)Mathf.RoundToInt(c.a * edge);
                    px[y * w + x] = cc;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _rainbow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _rainbow;
        }

        static Color Rainbow(float t)
        {
            t = Mathf.Repeat(t, 1f);
            var hue = Mathf.Lerp(0.50f, 0.92f, t);
            var c = Color.HSVToRGB(hue, 0.62f, 1f);
            if (t > 0.72f) c = Color.Lerp(c, Magenta, (t - 0.72f) / 0.28f);
            return c;
        }
    }
}
