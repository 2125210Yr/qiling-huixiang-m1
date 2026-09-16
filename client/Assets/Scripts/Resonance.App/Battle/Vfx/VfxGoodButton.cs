using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Drive QTE coin. Primary P0 t382: gold rim + cream face + <c>PRESS BUTTON</c>.
    /// Tutorial finger / HERE IS A POINT stay off this widget.
    /// Timing window is engineering (pulse the rim); ordinary-PVE QTE still unseen.
    /// Read-only pulse query for natural-play; do not widen to hide a late tap.
    /// </summary>
    public sealed class VfxGoodButton : MonoBehaviour
    {
        public const float Cycle = VfxGoodWindow.Cycle;
        public const float WindowAt = VfxGoodWindow.WindowAt;
        public const float WindowHalf = VfxGoodWindow.WindowHalf;
        const float HitLen = 0.16f;
        const float CoinPx = 220f;
        static readonly Vector2 Anchor = new Vector2(0.5f, 0.268f);
        static readonly Color RimCol = new Color(0.93f, 0.62f, 0.08f, 1f);
        static readonly Color RimHot = new Color(1f, 0.78f, 0.18f, 1f);
        static readonly Color FaceCol = new Color(0.98f, 0.86f, 0.42f, 1f);
        static readonly Color FaceInner = new Color(0.99f, 0.92f, 0.62f, 1f);
        static readonly Color Ink = new Color(0.18f, 0.09f, 0.02f, 1f);
        static readonly string CoinLabel = BattleCueCopy.PressButton.Replace(' ', '\n');

        static VfxGoodButton _live;

        CanvasGroup _group;
        Image _rim;
        Image _face;
        Image _well;
        Image _flash;
        Text _label;
        Button _btn;
        System.Action<bool> _onPressed;
        float _age;
        float _hit;
        bool _open;
        bool _pressed;
        bool _hideQueued;

        public static VfxGoodButton Live => _live;

        /// <summary>Unscaled seconds since <see cref="Begin"/>.</summary>
        public float Age => _age;

        public bool IsInWindow => VfxGoodWindow.IsInWindow(_age);

        public bool IsEarlyInWindow => VfxGoodWindow.IsEarlyInWindow(_age);

        /// <summary>Seconds until the next early-in-window pulse. Zero if already ready.</summary>
        public float SecondsUntilWindow => VfxGoodWindow.SecondsUntilWindow(_age);

        public static void Show(Transform parent, System.Action<bool> onPressed)
        {
            if (parent == null) return;
            Ensure(parent).Begin(onPressed);
        }

        public static void Hide()
        {
            if (_live != null) _live.End();
        }

        static VfxGoodButton Ensure(Transform parent)
        {
            if (_live != null)
            {
                if (_live.transform.parent != parent)
                    _live.transform.SetParent(parent, false);
                Place(_live.transform as RectTransform);
                return _live;
            }

            var existing = parent.GetComponentInChildren<VfxGoodButton>(true);
            if (existing != null)
            {
                _live = existing;
                existing.transform.SetParent(parent, false);
                Place(existing.transform as RectTransform);
                return existing;
            }

            var go = new GameObject("vfxGood", typeof(RectTransform), typeof(CanvasGroup), typeof(Button), typeof(VfxGoodButton));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>());
            var fx = go.GetComponent<VfxGoodButton>();
            fx.Build();
            _live = fx;
            return fx;
        }

        static void Place(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = Anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CoinPx, CoinPx);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        void OnDestroy()
        {
            if (_btn != null) _btn.onClick.RemoveListener(OnClick);
            _onPressed = null;
            if (_live == this) _live = null;
        }

        void Build()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;

            _rim = Img("rim", RimCol, CoinPx);
            _face = Img("face", FaceCol, CoinPx * 0.86f);
            _well = Img("well", FaceInner, CoinPx * 0.62f);
            _flash = Img("flash", new Color(1f, 0.96f, 0.78f, 0f), CoinPx * 1.08f);

            _label = MkText("label", CoinLabel, 22, Ink, new Vector2(140f, 72f));

            _btn = GetComponent<Button>();
            _btn.transition = Selectable.Transition.None;
            _btn.navigation = new Navigation { mode = Navigation.Mode.None };
            _btn.targetGraphic = _face;
            // G2 N01/R01: the coin must be hittable by a real pointer raycast. Previously every child
            // had raycastTarget=false and the root had no Graphic, so only the fixture path could "press" it.
            _rim.raycastTarget = true;
            _face.raycastTarget = true;
            _btn.onClick.AddListener(OnClick);

            gameObject.SetActive(false);
        }

        void Begin(System.Action<bool> onPressed)
        {
            _hideQueued = false;
            ClearSparks();
            _onPressed = onPressed;
            _age = 0f;
            _hit = 0f;
            _open = true;
            _pressed = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            transform.localScale = Vector3.one;
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.interactable = true;
                _group.blocksRaycasts = true;
            }
            if (_btn != null) _btn.interactable = true;
            if (_label != null)
            {
                _label.text = CoinLabel;
                _label.color = Ink;
            }
            SetA(_flash, 0f);
            PaintIdle(0f);
        }

        void End()
        {
            _open = false;
            _pressed = true;
            _hit = 0f;
            _onPressed = null;
            if (_btn != null) _btn.interactable = false;
            if (_group != null)
            {
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
            _hideQueued = true;
        }

        void LateUpdate()
        {
            if (!_hideQueued) return;
            _hideQueued = false;
            ReleaseSparks();
            if (this == null) return;
            gameObject.SetActive(false);
        }

        void OnClick()
        {
            if (!_open || _pressed) return;
            _pressed = true;
            if (_btn != null) _btn.interactable = false;
            var hit = InWindow();
            _hit = HitLen;
            CanvasShake.Punch(hit ? 18f : 8f, hit ? 0.18f : 0.10f);
            transform.localScale = Vector3.one * (hit ? 1.10f : 0.96f);
            SetA(_flash, hit ? 0.85f : 0.35f);
            if (hit)
            {
                for (int i = 0; i < 8; i++)
                    Spark.Spawn(transform, VisualTokens.FeverGold, (i / 8f) * Mathf.PI * 2f, 78f, true, true);
            }
            var cb = _onPressed;
            _onPressed = null;
            if (cb != null) cb(hit);
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (_hit > 0f)
            {
                _hit -= dt;
                TickHit();
                if (_hit <= 0f) End();
                return;
            }
            if (!_open) return;
            _age += dt;
            PaintIdle(Mathf.PingPong(_age, Cycle));
        }

        bool InWindow()
        {
            return VfxGoodWindow.IsInWindow(_age);
        }

        void PaintIdle(float t)
        {
            var glow = Mathf.Clamp01(1f - Mathf.Abs(t - WindowAt) / 0.40f);
            var gs = glow * glow * (3f - 2f * glow);
            transform.localScale = Vector3.one * (1f + 0.06f * gs);

            SetCol(_rim, Color.Lerp(RimCol, RimHot, gs));
            SetA(_rim, 1f);
            SetCol(_face, FaceCol);
            SetA(_face, 1f);
            SetCol(_well, Color.Lerp(FaceInner, Color.white, gs * 0.35f));
            SetA(_well, 1f);
            if (_label != null)
            {
                _label.text = CoinLabel;
                _label.color = Ink;
                _label.transform.localScale = Vector3.one * (1f + 0.04f * gs);
            }
        }

        void TickHit()
        {
            var u = 1f - Mathf.Clamp01(_hit / HitLen);
            SetA(_flash, (1f - u) * 0.80f);
            transform.localScale = Vector3.one * Mathf.Lerp(1.10f, 0.92f, u);
            Fade(_label, 1f - u);
            if (_group != null) _group.alpha = 1f;
        }

        void ReleaseSparks()
        {
            var host = transform.parent;
            var sparks = GetComponentsInChildren<Spark>(true);
            for (int i = 0; i < sparks.Length; i++)
            {
                var s = sparks[i];
                if (s == null) continue;
                if (host != null) s.transform.SetParent(host, true);
                else Object.Destroy(s.gameObject);
            }
        }

        void ClearSparks()
        {
            var sparks = GetComponentsInChildren<Spark>(true);
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] != null) Object.Destroy(sparks[i].gameObject);
            }
        }

        Image Img(string name, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Circle());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
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
            ol.effectColor = new Color(1f, 0.92f, 0.55f, 0.85f);
            ol.effectDistance = new Vector2(1f, -1f);
            return tx;
        }

        static void SetCol(Image img, Color c)
        {
            if (img == null) return;
            img.color = c;
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
