using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Inventory tip ledger only. P0 t444/t445 is <c>TOTAL n DAMAGE</c> on the tutorial plate.
    /// Live Fever (t440/t442) is center <c>N COMBO</c> / <c>N DAMAGE</c>.
    /// Ordinary mid-fight frames have no left DPS/HEAL box — BattleHud keeps this hidden.
    /// </summary>
    public sealed class VfxDpsPanel : MonoBehaviour
    {
        const float AnchorX = 0.145f;
        const float AnchorY = 0.805f;
        const float PlateW = 236f;
        const float PlateH = 108f;

        static readonly Color Plate = new Color(0.06f, 0.045f, 0.03f, 0.88f);
        static readonly Color Tone = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.10f);
        static readonly Color Wire = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.92f);
        static readonly Color Gold = VisualTokens.GoldTitle;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color Mute = VisualTokens.TextStat;
        static readonly Color Ghost = new Color(0.12f, 0.06f, 0.02f, 0.90f);

        CanvasGroup _group;
        Text _dmg;
        Text _dps;
        Text _heal;
        Text _ghost;
        int _shownDealt = -1;
        float _pulse;

        public static void Draw(Transform parent, FightStats stats)
        {
            if (parent == null) return;
            var fx = Find(parent);
            if (fx == null)
            {
                var go = new GameObject("dpsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxDpsPanel));
                go.transform.SetParent(parent, false);
                fx = go.GetComponent<VfxDpsPanel>();
                fx.Build();
            }
            else if (fx.transform.parent != parent)
                fx.transform.SetParent(parent, false);
            fx.Apply(stats);
        }

        public static void Hide(Transform parent)
        {
            var fx = Find(parent);
            if (fx == null) return;
            if (fx._group != null) fx._group.alpha = 0f;
            fx.gameObject.SetActive(false);
        }

        void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(AnchorX, AnchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PlateW + 12f, PlateH + 8f);
            rt.anchoredPosition = Vector2.zero;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            Pic("plate", UiSprites.Pixel(), Plate, new Vector2(PlateW, PlateH), Vector2.zero);
            var tone = Pic("tone", UiSprites.Halftone(), Tone, new Vector2(PlateW, PlateH), Vector2.zero);
            tone.type = Image.Type.Tiled;
            tone.pixelsPerUnitMultiplier = 0.55f;
            Pic("wire", UiSprites.WireFrame(), Wire, new Vector2(PlateW + 8f, PlateH + 8f), Vector2.zero);
            Pic("slash", UiSprites.Slash(), new Color(Gold.r, Gold.g, Gold.b, 0.22f),
                new Vector2(88f, 28f), new Vector2(-58f, 36f));
            Pic("rule", UiSprites.Pixel(), new Color(Wire.r, Wire.g, Wire.b, 0.55f),
                new Vector2(120f, 1f), new Vector2(0f, 18f));

            _ghost = MkText("ghost", 20, Ghost, new Vector2(1f, 8f), new Vector2(210f, 28f));
            _dmg = MkText("dmg", 20, Hot, Vector2.zero, new Vector2(210f, 28f));
            _dps = MkText("dps", 16, Gold, new Vector2(0f, -26f), new Vector2(210f, 24f));
            _heal = MkText("heal", 15, Mute, new Vector2(0f, -48f), new Vector2(210f, 22f));
        }

        void Apply(FightStats stats)
        {
            if (_dmg == null) Build();
            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;
            transform.SetAsLastSibling();

            var dealt = stats != null ? Mathf.Max(0, stats.TotalDealt) : 0;
            var dps = stats != null ? Mathf.Max(0, stats.Dps) : 0;
            var heal = stats != null ? Mathf.Max(0, stats.TotalHeal) : 0;
            if (dealt != _shownDealt)
            {
                _shownDealt = dealt;
                _pulse = 1f;
            }

            var line = Comma(dealt) + " DAMAGE";
            Set(_dmg, line, Hot);
            Set(_ghost, line, Ghost);
            Set(_dps, "DPS  " + Comma(dps), Gold);
            if (heal > 0)
            {
                _heal.gameObject.SetActive(true);
                Set(_heal, "HEAL  " + Comma(heal), Mute);
            }
            else
            {
                Set(_heal, "", Mute);
                _heal.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (_pulse <= 0f) return;
            _pulse -= Time.unscaledDeltaTime * 4f;
            if (_pulse < 0f) _pulse = 0f;
            var s = 1f + 0.06f * _pulse;
            transform.localScale = new Vector3(s, s, 1f);
        }

        Image Pic(string name, Sprite sprite, Color color, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, int size, Color color, Vector2 pos, Vector2 dim)
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
            tx.alignment = TextAnchor.MiddleLeft;
            tx.color = color;
            tx.text = "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(1.5f, -1.5f);
            return tx;
        }

        static void Set(Text tx, string s, Color c)
        {
            if (tx == null) return;
            tx.text = s ?? "";
            tx.color = c;
        }

        static string Comma(int n)
        {
            if (n < 0) n = 0;
            return n.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture);
        }

        static VfxDpsPanel Find(Transform parent)
        {
            if (parent == null) return null;
            var t = parent.Find("dpsPanel");
            if (t != null)
            {
                var fx = t.GetComponent<VfxDpsPanel>();
                if (fx == null) fx = t.gameObject.AddComponent<VfxDpsPanel>();
                return fx;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i).GetComponent<VfxDpsPanel>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
