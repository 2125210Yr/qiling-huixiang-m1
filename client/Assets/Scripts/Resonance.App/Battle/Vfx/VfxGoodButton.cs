using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Drive QTE ticket. Printed dark plate + gold wire + slash timing. Label is 好.
    /// No English GOOD BUTTON. No Soft disc. No Circle body.
    /// </summary>
    public sealed class VfxGoodButton : MonoBehaviour
    {
        const float Cycle = 1.2f;
        const float WindowAt = 0.60f;
        const float WindowHalf = 0.08f;
        const float HitLen = 0.16f;
        const float PlateW = 280f;
        const float PlateH = 88f;
        const float SlashAng = -12f;
        static readonly Vector2 Anchor = new Vector2(0.5f, 0.268f);
        static readonly Vector2 SweepFrom = new Vector2(-118f, 22f);
        static readonly Vector2 SweepTo = new Vector2(118f, -18f);
        static readonly Color PlateCol = new Color(0.06f, 0.045f, 0.03f, 0.92f);
        static readonly Color ToneCol = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.10f);
        static readonly Color WireCol = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.92f);

        static VfxGoodButton _live;

        CanvasGroup _group;
        Image _plate;
        Image _tone;
        Image _wire;
        Image _hair;
        Image _track;
        Image _streak;
        Image _star;
        Image _flash;
        Image[] _pips;
        Text _label;
        Button _btn;
        System.Action<bool> _onPressed;
        float _age;
        float _hit;
        bool _open;
        bool _pressed;
        bool _hideQueued;

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
            rt.sizeDelta = new Vector2(PlateW, PlateH);
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

            _plate = Img("plate", UiSprites.Pixel(), PlateCol, new Vector2(PlateW, PlateH));
            _plate.raycastTarget = true;

            _tone = Img("tone", UiSprites.Halftone(), ToneCol, new Vector2(PlateW, PlateH));
            _tone.type = Image.Type.Tiled;
            _tone.pixelsPerUnitMultiplier = 0.55f;

            _wire = Img("wire", UiSprites.WireFrame(), WireCol, new Vector2(PlateW + 8f, PlateH + 8f));

            _hair = Img("hair", UiSprites.Pixel(), new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.70f),
                new Vector2(248f, 1.5f));
            _hair.rectTransform.anchoredPosition = new Vector2(0f, 28f);

            _track = Img("track", UiSprites.Slash(), new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.28f),
                new Vector2(252f, 18f));
            _track.rectTransform.localEulerAngles = new Vector3(0f, 0f, SlashAng);

            _streak = Img("streak", UiSprites.Slash(), new Color(VisualTokens.FeverGold.r, VisualTokens.FeverGold.g, VisualTokens.FeverGold.b, 0f),
                new Vector2(72f, 16f));
            _streak.rectTransform.localEulerAngles = new Vector3(0f, 0f, SlashAng);

            _star = Img("star", UiSprites.Star(), VisualTokens.FeverGold, new Vector2(28f, 28f));

            _flash = Img("flash", UiSprites.Slash(), new Color(1f, 0.94f, 0.72f, 0f), new Vector2(300f, 32f));
            _flash.rectTransform.localEulerAngles = new Vector3(0f, 0f, SlashAng);

            _label = MkText("label", "好", 52, Color.white, new Vector2(160f, 64f));

            _pips = new Image[7];
            for (int i = 0; i < _pips.Length; i++)
            {
                var u = _pips.Length <= 1 ? 0.5f : i / (float)(_pips.Length - 1);
                var pip = Img("pip" + i, UiSprites.Pixel(), Color.clear, new Vector2(8f, 2f));
                pip.rectTransform.anchoredPosition = Vector2.Lerp(SweepFrom, SweepTo, u);
                pip.rectTransform.localEulerAngles = new Vector3(0f, 0f, SlashAng);
                _pips[i] = pip;
            }

            _btn = GetComponent<Button>();
            _btn.transition = Selectable.Transition.None;
            _btn.navigation = new Navigation { mode = Navigation.Mode.None };
            _btn.targetGraphic = _plate;
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
                _label.text = "好";
                _label.color = Color.white;
            }
            SetA(_flash, 0f);
            SetA(_streak, 0f);
            SetA(_star, 0f);
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
            SetA(_flash, hit ? 0.95f : 0.42f);
            if (_flash != null) _flash.transform.localScale = Vector3.one * (hit ? 1.12f : 0.90f);
            if (hit)
            {
                for (int i = 0; i < 8; i++)
                    Spark.Spawn(transform, VisualTokens.FeverGold, (i / 8f) * Mathf.PI * 2f, 78f, true, true);
                for (int i = 0; i < 6; i++)
                    Spark.Spawn(transform, Color.white, (i / 6f) * Mathf.PI * 2f + 0.22f, 52f, true);
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
            return Mathf.Abs(Mathf.PingPong(_age, Cycle) - WindowAt) <= WindowHalf;
        }

        void PaintIdle(float t)
        {
            var glow = Mathf.Clamp01(1f - Mathf.Abs(t - WindowAt) / 0.40f);
            var gs = glow * glow * (3f - 2f * glow);
            transform.localScale = Vector3.one * (1f + 0.08f * gs);

            SetCol(_plate, PlateCol);
            SetA(_tone, 0.08f + 0.14f * gs);
            SetA(_wire, 0.72f + 0.28f * gs);
            SetCol(_wire, Color.Lerp(WireCol, VisualTokens.GoldMetal, gs * 0.55f));
            SetA(_hair, 0.40f + 0.50f * gs);
            SetA(_track, 0.18f + 0.48f * gs);

            var u = Mathf.Clamp01(Mathf.InverseLerp(WindowAt - 0.28f, WindowAt + 0.28f, t));
            var su = u * u * (3f - 2f * u);
            var pos = Vector2.Lerp(SweepFrom, SweepTo, su);
            var starOn = u > 0.02f && u < 0.98f;
            if (_star != null)
            {
                _star.rectTransform.anchoredPosition = pos;
                _star.transform.localScale = Vector3.one * (0.85f + 0.55f * gs);
                _star.transform.localEulerAngles = new Vector3(0f, 0f, su * 40f - 20f);
                SetCol(_star, Opaque(Color.Lerp(VisualTokens.FeverGold, Color.white, gs)));
                SetA(_star, starOn ? 0.30f + 0.70f * gs : 0f);
            }
            if (_streak != null)
            {
                _streak.rectTransform.anchoredPosition = pos;
                SetA(_streak, starOn ? 0.18f + 0.62f * gs : 0f);
            }

            if (_label != null)
            {
                _label.text = "好";
                _label.color = Color.white;
                _label.transform.localScale = Vector3.one * (1f + 0.08f * gs);
            }

            if (_pips == null) return;
            for (int i = 0; i < _pips.Length; i++)
            {
                var pip = _pips[i];
                if (pip == null) continue;
                var pu = _pips.Length <= 1 ? 0.5f : i / (float)(_pips.Length - 1);
                var near = 1f - Mathf.Clamp01(Mathf.Abs(pu - su) / 0.28f);
                var wave = 0.5f + 0.5f * Mathf.Sin(_age * 4.2f + i * (Mathf.PI * 2f / _pips.Length));
                var pc = VisualTokens.FeverGold;
                pc.a = Mathf.Clamp01((0.12f + 0.55f * glow) * (0.35f + 0.65f * wave) + 0.55f * near * gs);
                pip.color = pc;
            }
        }

        void TickHit()
        {
            var u = 1f - Mathf.Clamp01(_hit / HitLen);
            SetA(_flash, (1f - u) * 0.90f);
            if (_flash != null)
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 1.38f, u);
            transform.localScale = Vector3.one * Mathf.Lerp(1.10f, 0.92f, u);
            SetA(_plate, 0.92f - u * 0.35f);
            SetA(_wire, 1f - u);
            SetA(_tone, (1f - u) * 0.16f);
            SetA(_hair, (1f - u) * 0.70f);
            SetA(_track, (1f - u) * 0.55f);
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

        Image Img(string name, Sprite sprite, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
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
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3f, -3f);
            return tx;
        }

        static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
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
