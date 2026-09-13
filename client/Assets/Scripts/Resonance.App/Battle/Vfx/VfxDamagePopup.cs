using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Stacked hit numbers on the 1080x1920 overlay. Words are WeakPoint / CRIT.
    /// WeakPoint EN matches primary Robin ~t68.
    /// </summary>
    public sealed class VfxDamagePopup : MonoBehaviour
    {
        const float Life = 0.70f;

        static readonly Color HitInk = VisualTokens.TapWhite;
        static readonly Color WeakBack = new Color(0.62f, 0.22f, 0.02f, 1f);
        static readonly Color WeakMid = VisualTokens.OrangeLeader;
        static readonly Color WeakFront = VisualTokens.Ember;
        static readonly Color CritBack = new Color(0.42f, 0.02f, 0.04f, 1f);
        static readonly Color CritFront = VisualTokens.StarEvolved;
        static readonly Vector2[] WeakShift =
        {
            new Vector2(-7f, 5f),
            new Vector2(6f, -4f),
            new Vector2(0f, 0f)
        };

        Graphic[] _gfx;
        Color[] _base;
        Vector2 _from;
        float _life = Life;
        float _pop = 1.28f;
        float _rise = 88f;
        float _fadeFrom = 0.48f;

        public static void Spawn(Transform parent, Vector2 anchor01, int amount, bool weak, bool crit, Resonance.Battle.Element elem)
        {
            if (parent == null) return;

            var go = new GameObject("dmgPop", typeof(RectTransform), typeof(VfxDamagePopup));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 160f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var fx = go.GetComponent<VfxDamagePopup>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(Mathf.Max(0, amount), weak, crit, elem);
            HitChainProbe.NumberShown();
        }

        void Build(int amount, bool weak, bool crit, Resonance.Battle.Element elem)
        {
            _from = new Vector2(Random.Range(-26f, 26f), Random.Range(-6f, 8f));
            _pop = crit ? 1.55f : weak ? 1.42f : 1.26f;
            _rise = crit || weak ? 72f : 96f;
            _fadeFrom = crit || weak ? 0.58f : 0.46f;
            _life = Life;

            var digits = FormatAmount(amount);
            var numSize = crit ? 78 : weak ? 64 : 54;
            var numColor = crit ? CritFront : weak ? WeakFront : HitInk;
            var numY = (weak || crit) ? -18f : 0f;
            var numW = Mathf.Max(128f, digits.Length * numSize * 0.62f + 18f);

            var gfx = new Graphic[16];
            var n = 0;

            if (weak)
            {
                var wordY = crit ? 62f : 44f;
                var cols = new[] { WeakBack, WeakMid, WeakFront };
                for (int i = 0; i < WeakShift.Length; i++)
                    gfx[n++] = MkText("weak" + i, BattleCueCopy.WeakPoint, 32, cols[i],
                        new Vector2(0.5f, 0.5f), new Vector2(220f, 48f),
                        WeakShift[i] + new Vector2(0f, wordY), 4f);
            }

            if (crit)
            {
                var wordY = weak ? 28f : 44f;
                gfx[n++] = MkText("critB", "CRIT", 36, CritBack,
                    new Vector2(0.5f, 0.5f), new Vector2(220f, 52f),
                    new Vector2(5f, wordY - 4f), 4f);
                gfx[n++] = MkText("critF", "CRIT", 36, CritFront,
                    new Vector2(0.5f, 0.5f), new Vector2(220f, 52f),
                    new Vector2(0f, wordY), 4f);
            }

            gfx[n++] = MkText("numB", digits, numSize, Darken(numColor),
                new Vector2(0.5f, 0.5f), new Vector2(numW, 104f),
                new Vector2(5f, numY - 5f), 5f);
            var num = MkText("num", digits, numSize, numColor,
                new Vector2(0.5f, 0.5f), new Vector2(numW, 104f),
                new Vector2(0f, numY), 5f);
            gfx[n++] = num;
            n = AddChip(gfx, n, num.rectTransform, elem);

            _gfx = new Graphic[n];
            _base = new Color[n];
            for (int i = 0; i < n; i++)
            {
                _gfx[i] = gfx[i];
                _base[i] = gfx[i] != null ? gfx[i].color : Color.white;
            }

            ((RectTransform)transform).anchoredPosition = _from;
            transform.localScale = Vector3.one * _pop;
        }

        void Update()
        {
            _life -= Time.unscaledDeltaTime;
            var u = 1f - Mathf.Clamp01(_life / Life);
            var punch = u < 0.14f
                ? Mathf.Lerp(_pop, 1.08f, u / 0.14f)
                : Mathf.Lerp(1.08f, 0.92f, (u - 0.14f) / 0.86f);
            transform.localScale = Vector3.one * punch;
            ((RectTransform)transform).anchoredPosition = _from + new Vector2(_from.x * 0.08f, _rise * u);

            var a = u < _fadeFrom ? 1f : 1f - (u - _fadeFrom) / Mathf.Max(0.01f, 1f - _fadeFrom);
            a = Mathf.Clamp01(a);
            if (_gfx != null && _base != null)
            {
                var n = Mathf.Min(_gfx.Length, _base.Length);
                for (int i = 0; i < n; i++)
                {
                    var g = _gfx[i];
                    if (g == null) continue;
                    var c = _base[i];
                    c.a *= a;
                    g.color = c;
                }
            }

            if (_life <= 0f) Destroy(gameObject);
        }

        int AddChip(Graphic[] gfx, int n, RectTransform number, Resonance.Battle.Element elem)
        {
            if (gfx == null || number == null || n + 3 > gfx.Length) return n;
            var tint = VisualTokens.Element(elem);
            var go = new GameObject("elem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(number, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(32f, 32f);
            rt.anchoredPosition = new Vector2(8f, 12f);

            var rim = go.GetComponent<Image>();
            UiSprites.Apply(rim, UiSprites.Circle());
            if (rim != null)
            {
                rim.color = new Color(0.05f, 0.05f, 0.05f, 1f);
                rim.raycastTarget = false;
            }

            var fillGo = new GameObject("fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(-3f, -3f);
            var fill = fillGo.GetComponent<Image>();
            UiSprites.Apply(fill, UiSprites.Circle());
            if (fill != null)
            {
                fill.color = tint;
                fill.raycastTarget = false;
            }

            var glyph = MkText("g", ElemGlyph(elem), 17, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(30f, 30f), Vector2.zero, 1f);
            if (glyph != null)
            {
                glyph.transform.SetParent(go.transform, false);
                var grt = glyph.rectTransform;
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = Vector2.zero;
                grt.offsetMax = Vector2.zero;
                glyph.color = new Color(1f, 1f, 1f, 0.95f);
            }

            gfx[n++] = rim;
            gfx[n++] = fill;
            gfx[n++] = glyph;
            return n;
        }

        Text MkText(string name, string s, int size, Color color, Vector2 anchor, Vector2 dim, Vector2 pos, float outline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = s ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(outline, -outline);
            return tx;
        }

        static string FormatAmount(int amount)
        {
            var raw = amount.ToString();
            if (raw.Length <= 3) return raw;
            var buf = new char[raw.Length + (raw.Length - 1) / 3];
            var j = buf.Length - 1;
            var n = 0;
            for (int i = raw.Length - 1; i >= 0; i--)
            {
                if (n == 3)
                {
                    buf[j--] = ',';
                    n = 0;
                }
                buf[j--] = raw[i];
                n++;
            }
            return new string(buf);
        }

        static string ElemGlyph(Resonance.Battle.Element e)
        {
            // Primary EN: element pip letters (not CN 火/水).
            switch (e)
            {
                case Resonance.Battle.Element.Fire: return "F";
                case Resonance.Battle.Element.Water: return "W";
                case Resonance.Battle.Element.Wood: return "G";
                case Resonance.Battle.Element.Light: return "L";
                default: return "D";
            }
        }

        static Color Darken(Color c)
        {
            return new Color(c.r * 0.35f, c.g * 0.28f, c.b * 0.28f, 1f);
        }

    }
}
