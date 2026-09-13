using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Tutorial tip plate. Primary P0 t442: yellow <c>TIP!</c> + title + body.
    /// Engineering chrome — not a full tip queue. Tip stamp <c>FEVER TIME!!</c>
    /// stays on tip art; live field uses <see cref="VfxFeverOverlay"/>.
    /// </summary>
    public sealed class VfxTipPlate : MonoBehaviour
    {
        const float LifeSec = 4.20f;
        const float RatesLifeSec = 5.40f;
        static readonly Color PlateCol = new Color(0.12f, 0.11f, 0.13f, 0.88f);
        static readonly Color WireCol = new Color(0.72f, 0.70f, 0.66f, 0.55f);
        static readonly Color TipGold = new Color(1f, 0.86f, 0.28f, 1f);
        static readonly Color TitleCol = new Color(1f, 0.42f, 0.28f, 1f);
        static readonly Color BodyCol = new Color(0.96f, 0.94f, 0.86f, 1f);

        static VfxTipPlate _live;

        CanvasGroup _group;
        Text _banner;
        Text _title;
        Text _body;
        Text _art;
        RectTransform _cardRt;
        float _life;
        bool _chainRates;
        bool _feverSession;
        bool _showArt;

        public static bool AnyLive()
        {
            return _live != null && _live._life > 0.02f;
        }

        /// <summary>True while a Fever tip plate (activate / rates) is on screen.</summary>
        public static bool FeverTipSession()
        {
            return AnyLive() && _live != null && _live._feverSession;
        }

        public static void ShowFeverActivate(Transform parent)
        {
            var fx = Ensure(parent);
            fx._feverSession = true;
            fx._chainRates = true;
            fx.Begin(BattleCueCopy.TipFeverActivate, BattleCueCopy.TipFeverWindowBody, LifeSec, tipArt: false);
        }

        public static void ShowFeverRates(Transform parent)
        {
            var fx = Ensure(parent);
            fx._feverSession = true;
            fx._chainRates = false;
            var body = BattleCueCopy.TipFeverRatesBody
                + "\n" + BattleCueCopy.TipFeverBarrageFooter
                + "\n" + BattleCueCopy.TipFeverChildNote;
            fx.Begin(BattleCueCopy.TipFeverBarrage, body, RatesLifeSec, tipArt: true);
        }

        public static void Show(Transform parent, string title, string body)
        {
            if (parent == null) return;
            var fx = Ensure(parent);
            fx._chainRates = false;
            fx.Begin(title, body, LifeSec, tipArt: false);
        }

        public static void Hide()
        {
            if (_live != null)
            {
                _live._chainRates = false;
                _live._feverSession = false;
                _live.End();
            }
        }

        static VfxTipPlate Ensure(Transform parent)
        {
            if (_live != null)
            {
                if (_live.transform.parent != parent)
                    _live.transform.SetParent(parent, false);
                Stretch(_live.transform as RectTransform);
                return _live;
            }

            var existing = parent.GetComponentInChildren<VfxTipPlate>(true);
            if (existing != null)
            {
                _live = existing;
                existing.transform.SetParent(parent, false);
                Stretch(existing.transform as RectTransform);
                return existing;
            }

            var go = new GameObject("tipPlate", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxTipPlate));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var fx = go.GetComponent<VfxTipPlate>();
            fx.Build();
            _live = fx;
            return fx;
        }

        void OnDestroy()
        {
            if (_live == this) _live = null;
        }

        void Build()
        {
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            _cardRt = card.GetComponent<RectTransform>();
            _cardRt.anchorMin = _cardRt.anchorMax = new Vector2(0.5f, 0.78f);
            _cardRt.pivot = new Vector2(0.5f, 0.5f);
            _cardRt.sizeDelta = new Vector2(620f, 168f);
            var bg = card.GetComponent<Image>();
            bg.color = PlateCol;
            bg.raycastTarget = false;

            var wire = new GameObject("Wire", typeof(RectTransform), typeof(Image));
            wire.transform.SetParent(card.transform, false);
            var wrt = wire.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero;
            wrt.anchorMax = Vector2.one;
            wrt.offsetMin = new Vector2(3f, 3f);
            wrt.offsetMax = new Vector2(-3f, -3f);
            var wimg = wire.GetComponent<Image>();
            wimg.color = WireCol;
            wimg.raycastTarget = false;

            _banner = MkText(card.transform, "Banner", BattleCueCopy.TipBanner, 22,
                TipGold, FontStyle.Bold, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.96f));
            _title = MkText(card.transform, "Title", "", 26,
                TitleCol, FontStyle.Bold, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.70f));
            _body = MkText(card.transform, "Body", "", 18,
                BodyCol, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.42f));
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            // P0 t444 tip field art: TOTAL / n / DAMAGE (not live combo).
            _art = MkText(card.transform, "Art", "", 20,
                new Color(1f, 0.92f, 0.35f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.62f, 0.08f), new Vector2(0.96f, 0.92f));
            _art.supportRichText = true;
            _art.gameObject.SetActive(false);
        }

        void Begin(string title, string body, float life, bool tipArt = false)
        {
            gameObject.SetActive(true);
            _showArt = tipArt;
            if (_banner != null) _banner.text = BattleCueCopy.TipBanner;
            if (_title != null) _title.text = title ?? "";
            if (_body != null) _body.text = body ?? "";
            if (_art != null)
            {
                _art.gameObject.SetActive(tipArt);
                if (tipArt)
                {
                    // P0 t445 tip art: FEVER TIME!! banner; TOTAL damage ledger above tip.
                    _art.text = BattleCueCopy.FeverTimeTipArt;
                    if (transform.parent != null)
                        CombatFeel.NamePopStack(transform.parent, new Vector2(0.50f, 0.90f),
                            BattleCueCopy.TipFeverTotalDamageStub, VisualTokens.TapWhite);
                }
            }
            if (_cardRt != null)
                _cardRt.sizeDelta = tipArt ? new Vector2(720f, 210f) : new Vector2(620f, 168f);
            if (_body != null)
            {
                var brt = _body.rectTransform;
                if (tipArt)
                {
                    brt.anchorMin = new Vector2(0.06f, 0.06f);
                    brt.anchorMax = new Vector2(0.60f, 0.42f);
                }
                else
                {
                    brt.anchorMin = new Vector2(0.06f, 0.06f);
                    brt.anchorMax = new Vector2(0.94f, 0.42f);
                }
            }
            _life = life > 0.05f ? life : LifeSec;
            if (_group != null) _group.alpha = 1f;
            transform.SetAsLastSibling();
        }

        void End()
        {
            _life = 0f;
            if (_group != null) _group.alpha = 0f;
        }

        void LateUpdate()
        {
            if (_life <= 0f)
            {
                if (_group != null && _group.alpha > 0f) _group.alpha = 0f;
                return;
            }

            _life -= Time.unscaledDeltaTime;
            if (_group == null) return;
            if (_life < 0.55f)
                _group.alpha = Mathf.Clamp01(_life / 0.55f);
            else
                _group.alpha = 1f;
            if (_life > 0f) return;

            if (_chainRates && _feverSession)
            {
                _chainRates = false;
                var body = BattleCueCopy.TipFeverRatesBody
                    + "\n" + BattleCueCopy.TipFeverBarrageFooter
                    + "\n" + BattleCueCopy.TipFeverChildNote;
                Begin(BattleCueCopy.TipFeverBarrage, body, RatesLifeSec, tipArt: true);
                return;
            }

            End();
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text MkText(Transform parent, string name, string copy, int size, Color col,
            FontStyle style, TextAnchor align, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tx.fontSize = size;
            tx.fontStyle = style;
            tx.alignment = align;
            tx.color = col;
            tx.text = copy ?? "";
            tx.raycastTarget = false;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.75f);
            ol.effectDistance = new Vector2(1.2f, -1.2f);
            return tx;
        }
    }
}
