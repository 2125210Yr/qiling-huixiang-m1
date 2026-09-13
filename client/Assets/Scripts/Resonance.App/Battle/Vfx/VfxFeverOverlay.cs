using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Additive fever overlay. Does not replace HUD.
    /// Live field (P0 t440/t442): rainbow arc + <c>FEVER TIME</c> + leftover <c>12.50</c>.
    /// Tip stamp stays <c>FEVER TIME!!</c>. No 狂热时间, no edge slash lines.
    /// </summary>
    public sealed class VfxFeverOverlay : MonoBehaviour
    {
        static readonly Color Magenta = new Color(0.78f, 0.22f, 0.74f, 1f);

        static VfxFeverOverlay _live;
        static Sprite _rainbow;

        Image _barFill;
        Text _label;
        Text _sub;
        float _life = 7f;
        float _left;

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
            // P0 t440: thin rainbow just above the party tray. Word sits on the bar.
            _barFill = MkImg(root, "barFill", new Vector2(0.5f, 0.218f), new Vector2(720f, 14f), 0f,
                Color.white, RainbowSprite());
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _barFill.fillAmount = 1f;

            _label = MkText(root, "label", BattleCueCopy.FeverTimeLive, 28, new Vector2(0.5f, 0.218f), new Vector2(640f, 40f), Color.white);
            _sub = MkText(root, "sub", BattleCueCopy.FeverWindowLeft(0f), 20, new Vector2(0.5f, 0.198f), new Vector2(240f, 32f), Color.white);

            gameObject.SetActive(false);
        }

        void Begin(float seconds)
        {
            _life = Mathf.Max(0.05f, seconds);
            _left = _life;
            Active = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_barFill != null) _barFill.fillAmount = 1f;
            if (_label != null)
            {
                _label.text = BattleCueCopy.FeverTimeLive;
                _label.color = Color.white;
            }
            if (_sub != null)
            {
                _sub.text = BattleCueCopy.FeverWindowLeft(_left);
                _sub.color = Color.white;
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
            if (_barFill != null)
            {
                _barFill.fillAmount = u;
                _barFill.color = Color.white;
            }
            if (_label != null)
            {
                _label.text = BattleCueCopy.FeverTimeLive;
                _label.color = Rainbow(Mathf.Repeat(Time.unscaledTime * 0.22f, 1f));
            }
            if (_sub != null)
            {
                _sub.text = BattleCueCopy.FeverWindowLeft(_left);
                _sub.color = Color.white;
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
