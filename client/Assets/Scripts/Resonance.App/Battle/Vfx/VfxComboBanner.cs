using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Live Fever combo (P0 t440/t442): floating <c>N COMBO</c> + <c>N DAMAGE</c>.
    /// Tip field <c>TOTAL n DAMAGE</c> (t444/t445) is a different plate — do not use it here.
    /// No gold box. Hide is instant.
    /// </summary>
    public sealed class VfxComboBanner : MonoBehaviour
    {
        const float AnchorY = 0.92f;
        const int ComboNumSize = 56;
        const int ComboLabSize = 24;
        const int DmgNumSize = 44;
        const int DmgLabSize = 22;

        static readonly Color Num = Color.white;
        static readonly Color Ink = new Color(0.06f, 0.04f, 0.04f, 0.80f);
        const string LabHex = "#EB382E";

        CanvasGroup _group;
        Text _comboGhost;
        Text _combo;
        Text _dmgGhost;
        Text _dmg;
        float _pulse;
        int _comboN;
        int _totalN;

        public int Combo => _comboN;
        public int Total => _totalN;
        public bool Visible => gameObject.activeSelf && _group != null && _group.alpha > 0.01f;

        public static VfxComboBanner Show(Transform parent, int combo, int total)
        {
            if (parent == null) return null;
            var fx = Find(parent);
            if (fx == null)
            {
                var go = new GameObject("comboBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxComboBanner));
                go.transform.SetParent(parent, false);
                fx = go.GetComponent<VfxComboBanner>();
                fx.Build();
            }
            else if (fx.transform.parent != parent)
                fx.transform.SetParent(parent, false);

            fx.Apply(combo, total);
            return fx;
        }

        public void Hide()
        {
            _pulse = 0f;
            transform.localScale = Vector3.one;
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Pulse()
        {
            if (!gameObject.activeSelf) return;
            _pulse = 1f;
        }

        void Apply(int combo, int total)
        {
            _comboN = Mathf.Max(0, combo);
            _totalN = Mathf.Max(0, total);
            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;
            transform.SetAsLastSibling();
            Paint();
            Pulse();
        }

        void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, AnchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(980f, 168f);
            rt.anchoredPosition = Vector2.zero;

            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _comboGhost = MkText("comboGhost", ComboNumSize, Ink, new Vector2(4f, 28f), new Vector2(920f, 72f));
            _combo = MkText("combo", ComboNumSize, Num, new Vector2(0f, 34f), new Vector2(920f, 72f));
            _dmgGhost = MkText("dmgGhost", DmgNumSize, Ink, new Vector2(5f, -32f), new Vector2(940f, 64f));
            _dmg = MkText("dmg", DmgNumSize, Num, new Vector2(0f, -26f), new Vector2(940f, 64f));
        }

        void Paint()
        {
            var combo = Rich(_comboN.ToString(), BattleCueCopy.ComboLabel, ComboNumSize, ComboLabSize);
            // Live P0 t440/t442: N DAMAGE. Tip TOTAL n DAMAGE stays on the tutorial plate.
            var dmg = "<size=" + DmgNumSize + ">" + Comma(_totalN)
                + "</size><size=" + DmgLabSize + "><color=" + LabHex + "> DAMAGE</color></size>";
            Set(_combo, combo, Num);
            Set(_comboGhost, combo, Ink);
            Set(_dmg, dmg, Num);
            Set(_dmgGhost, dmg, Ink);
        }

        void LateUpdate()
        {
            if (_pulse <= 0f)
            {
                transform.localScale = Vector3.one;
                return;
            }

            _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime * 7.2f);
            var k = _pulse * _pulse;
            transform.localScale = Vector3.one * (1f + 0.16f * k);
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = new Vector2(0f, 10f * k);
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
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = "";
            tx.supportRichText = true;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3f, -3f);
            return tx;
        }

        static void Set(Text tx, string s, Color c)
        {
            if (tx == null) return;
            tx.text = s ?? "";
            tx.color = c;
        }

        static string Rich(string num, string label, int numSize, int labSize)
        {
            return "<size=" + numSize + ">" + num + "</size><size=" + labSize
                + "><color=" + LabHex + "> " + label + "</color></size>";
        }

        static string Comma(int n)
        {
            if (n < 0) n = 0;
            var raw = n.ToString();
            if (raw.Length <= 3) return raw;
            var buf = new char[raw.Length + (raw.Length - 1) / 3];
            var j = buf.Length - 1;
            var k = 0;
            for (int i = raw.Length - 1; i >= 0; i--)
            {
                if (k == 3)
                {
                    buf[j--] = ',';
                    k = 0;
                }
                buf[j--] = raw[i];
                k++;
            }
            return new string(buf);
        }

        static VfxComboBanner Find(Transform parent)
        {
            var self = parent.GetComponent<VfxComboBanner>();
            if (self != null) return self;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i).GetComponent<VfxComboBanner>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
