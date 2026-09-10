using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Up to 4 printed status tickets above a portrait: dark ink body,
    /// thin gold/element wire, 12px Chinese type with black outline.
    /// Chinese labels only. Empty/null Draw hides until the next Draw.
    /// </summary>
    public sealed class VfxStatusIcons : MonoBehaviour
    {
        const string ChildName = "statusIcons";
        const int Cap = 4;
        const float ChipH = 22f;
        const float Gap = 4f;
        const float Rise = 68f;
        const float FieldRise = 0f;
        const int FontPx = 14;

        static readonly Color InkDark = new Color(0.07f, 0.06f, 0.09f, 0.84f);
        static readonly Color Shade = new Color(0f, 0f, 0f, 0.32f);
        static readonly Color Paper = new Color(0.97f, 0.94f, 0.86f, 1f);

        struct Chip
        {
            public RectTransform Rt;
            public Image Shade;
            public Image Body;
            public Image WireT;
            public Image WireB;
            public Image WireL;
            public Image WireR;
            public Text Label;
        }

        CanvasGroup _group;
        Chip[] _chips;
        readonly string[] _live = new string[Cap];
        Vector2 _anchor;

        public static string[] Labels(UnitState u) => StatusChipText.Labels(u);

        public static void DrawField(Transform host, string[] labels, int key)
        {
            Draw(host, new Vector2(0.5f, 0.92f), labels, 100 + key, FieldRise);
        }

        public static void Draw(Transform parent, Vector2 portraitAnchor01, string[] labels, int slot = 0)
        {
            Draw(parent, portraitAnchor01, labels, slot, Rise);
        }

        public static void Draw(Transform parent, Vector2 portraitAnchor01, string[] labels, int slot, float risePx)
        {
            if (parent == null) return;
            if (slot < 0) slot = 0;

            var n = Collect(labels, Scratch);
            var name = ChildName + slot;
            var fx = Find(parent, name);
            if (n <= 0)
            {
                if (fx != null) fx.Hide();
                return;
            }

            if (fx == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(VfxStatusIcons));
                go.transform.SetParent(parent, false);
                fx = go.GetComponent<VfxStatusIcons>();
                fx.Build();
            }
            else if (fx.transform.parent != parent)
                fx.transform.SetParent(parent, false);

            fx.Apply(portraitAnchor01, Scratch, n, risePx);
        }

        static readonly string[] Scratch = new string[Cap];

        static VfxStatusIcons Find(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name)) return null;
            var t = parent.Find(name);
            if (t == null) return null;
            var fx = t.GetComponent<VfxStatusIcons>();
            if (fx == null) fx = t.gameObject.AddComponent<VfxStatusIcons>();
            return fx;
        }

        void Hide()
        {
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        void Build()
        {
            var rt = (RectTransform)transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 28f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _chips = new Chip[Cap];
            for (int i = 0; i < Cap; i++)
                _chips[i] = MakeChip(i);
        }

        void Apply(Vector2 portraitAnchor01, string[] labels, int n, float risePx)
        {
            if (_chips == null) Build();

            _anchor = new Vector2(Mathf.Clamp01(portraitAnchor01.x), Mathf.Clamp01(portraitAnchor01.y));
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = _anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Snap(new Vector2(0f, risePx));
            rt.SetAsLastSibling();

            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;

            var total = 0f;
            var widths = new float[Cap];
            for (int i = 0; i < n; i++)
            {
                _live[i] = labels[i];
                widths[i] = ChipW(labels[i]);
                total += widths[i];
                if (i > 0) total += Gap;
            }

            var x = Snap(new Vector2(-total * 0.5f, 0f)).x;
            for (int i = 0; i < Cap; i++)
            {
                if (i < n)
                {
                    x += widths[i] * 0.5f;
                    PaintChip(ref _chips[i], labels[i], widths[i], x);
                    x += widths[i] * 0.5f + Gap;
                }
                else
                {
                    _live[i] = null;
                    if (_chips[i].Rt != null)
                        _chips[i].Rt.gameObject.SetActive(false);
                }
            }
        }

        void PaintChip(ref Chip chip, string zh, float w, float x)
        {
            if (chip.Rt == null) return;
            chip.Rt.gameObject.SetActive(true);
            chip.Rt.anchoredPosition = Snap(new Vector2(x, 0f));
            chip.Rt.sizeDelta = new Vector2(w, ChipH);

            var wire = Mute(WireOf(zh));

            SetImg(chip.Shade, Shade, new Vector2(w, ChipH), new Vector2(0f, -1f));
            SetImg(chip.Body, InkDark, new Vector2(w, ChipH), Vector2.zero);
            SetImg(chip.WireT, wire, new Vector2(w, 1f), new Vector2(0f, ChipH * 0.5f - 0.5f));
            SetImg(chip.WireB, wire, new Vector2(w, 1f), new Vector2(0f, 0.5f - ChipH * 0.5f));
            SetImg(chip.WireL, wire, new Vector2(1f, ChipH - 2f), new Vector2(0.5f - w * 0.5f, 0f));
            SetImg(chip.WireR, wire, new Vector2(1f, ChipH - 2f), new Vector2(w * 0.5f - 0.5f, 0f));

            if (chip.Label != null)
            {
                chip.Label.text = zh;
                chip.Label.color = Paper;
                chip.Label.rectTransform.sizeDelta = new Vector2(w - 6f, ChipH);
                chip.Label.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        Chip MakeChip(int i)
        {
            var go = new GameObject("chip" + i, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, ChipH);
            rt.anchoredPosition = Vector2.zero;
            go.SetActive(false);

            return new Chip
            {
                Rt = rt,
                Shade = Pic(go.transform, "shade", Shade, new Vector2(48f, ChipH), new Vector2(0f, -1f)),
                Body = Pic(go.transform, "body", InkDark, new Vector2(48f, ChipH), Vector2.zero),
                WireT = Pic(go.transform, "wireT", Color.white, new Vector2(48f, 1f), new Vector2(0f, 7.5f)),
                WireB = Pic(go.transform, "wireB", Color.white, new Vector2(48f, 1f), new Vector2(0f, -7.5f)),
                WireL = Pic(go.transform, "wireL", Color.white, new Vector2(1f, ChipH - 2f), new Vector2(-23.5f, 0f)),
                WireR = Pic(go.transform, "wireR", Color.white, new Vector2(1f, ChipH - 2f), new Vector2(23.5f, 0f)),
                Label = MkText(go.transform, "tx", FontPx, Paper, Vector2.zero, new Vector2(42f, ChipH))
            };
        }

        static Image Pic(Transform parent, string name, Color color, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MkText(Transform parent, string name, int size, Color color, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Normal;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(1f, -1f);
            return tx;
        }

        static void SetImg(Image img, Color color, Vector2 size, Vector2 pos)
        {
            if (img == null) return;
            img.color = color;
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
        }

        static float ChipW(string zh)
        {
            var n = zh == null ? 1 : Mathf.Clamp(zh.Length, 1, 10);
            var w = 14f + n * 13f;
            return Mathf.Max(36f, Mathf.Round(w * 0.5f) * 2f);
        }

        static Color Mute(Color c)
        {
            var g = c.grayscale;
            return new Color(
                Mathf.Lerp(c.r, g, 0.15f) * 0.92f,
                Mathf.Lerp(c.g, g, 0.15f) * 0.92f,
                Mathf.Lerp(c.b, g, 0.15f) * 0.92f,
                0.95f);
        }

        static Color WireOf(string zh)
        {
            if (string.IsNullOrEmpty(zh)) return VisualTokens.GoldMetal;
            if (zh == VfxBuffFloat.Regen || zh == VfxBuffFloat.Lifesteal
                || zh == VfxBuffFloat.AtkStack || zh == VfxBuffFloat.DebuffBlast)
                return VfxBuffFloat.ColorOf(zh);
            if (Has(zh, "攻击") || Has(zh, "点按") || Has(zh, "上滑") || Has(zh, "驱动") || Has(zh, "充能") || Has(zh, "加速"))
                return VisualTokens.ElemWater;
            if (Has(zh, "防御") || Has(zh, "技防") || Has(zh, "弱防")) return VisualTokens.ElemDark;
            if (Has(zh, "回复") || Has(zh, "再生")) return VisualTokens.SlideGreen;
            if (Has(zh, "沉默")) return VisualTokens.ElemDark;
            if (Has(zh, "毒") || Has(zh, "中毒")) return VisualTokens.SlideGreen;
            if (Has(zh, "眩晕") || Has(zh, "晕眩")) return VisualTokens.FeverGold;
            if (Has(zh, "睡眠") || Has(zh, "冻结")) return VisualTokens.IceShard;
            if (Has(zh, "嘲讽") || Has(zh, "挑衅") || Has(zh, "流血") || Has(zh, "反击")) return VisualTokens.StarEvolved;
            if (Has(zh, "无敌") || Has(zh, "不死")) return VisualTokens.FeverGold;
            if (Has(zh, "净化")) return VisualTokens.TapWhite;
            if (Has(zh, "格挡") || Has(zh, "屏障")) return VisualTokens.GoldMetal;
            if (Has(zh, "灼烧") || Has(zh, "激怒") || Has(zh, "超载")) return VisualTokens.ElemFire;
            if (Has(zh, "石化")) return VisualTokens.IceShard;
            if (Has(zh, "失明")) return VisualTokens.TextMuted;
            if (Has(zh, "诅咒")) return VisualTokens.ElemDark;
            if (Has(zh, "禁疗")) return VisualTokens.OrangeCancel;
            return VisualTokens.GoldMetal;
        }

        static int Collect(string[] labels, string[] dst)
        {
            var n = 0;
            if (labels == null || dst == null) return 0;
            for (int i = 0; i < labels.Length && n < Cap; i++)
            {
                var zh = ToZh(labels[i]);
                if (zh.Length == 0) continue;
                var dup = false;
                for (int j = 0; j < n; j++)
                {
                    if (dst[j] == zh)
                    {
                        dup = true;
                        break;
                    }
                }
                if (dup) continue;
                dst[n++] = zh;
            }
            return n;
        }

        static string ToZh(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var s = raw.Trim();
            if (HasCjk(s)) return ClampReady(s);
            if (Is(s, VfxBuffFloat.Regen) || Is(s, "regen") || s == "回复") return VfxBuffFloat.Regen;
            if (Is(s, VfxBuffFloat.Lifesteal) || Is(s, "lifesteal") || Is(s, "vampirism")) return VfxBuffFloat.Lifesteal;
            if (s == VfxBuffFloat.AtkStack || s == "攻击力↑" || s == "呐喊") return VfxBuffFloat.AtkStack;
            if (s == VfxBuffFloat.DebuffBlast) return VfxBuffFloat.DebuffBlast;
            if (Has(s, "沉默") || Is(s, "silence")) return "沉默";
            if (Has(s, "毒") || Is(s, "poison")) return "毒";
            if (Has(s, "眩晕") || Has(s, "晕眩") || Is(s, "stun")) return "眩晕";
            if (Has(s, "睡眠") || Is(s, "sleep")) return "睡眠";
            if (Has(s, "嘲讽") || Has(s, "挑衅") || Is(s, "taunt")) return "嘲讽";
            if (Has(s, "流血") || Is(s, "bleed")) return "流血";
            if (Has(s, "无敌")) return "无敌";
            if (Has(s, "净化")) return "净化";
            if (Has(s, "格挡") || Has(s, "屏障")) return "格挡";
            if (Has(s, "反击")) return "反击";
            if (Has(s, "灼烧") || Is(s, "burn")) return "灼烧";
            if (Has(s, "石化") || Has(s, "冻结")) return "石化";
            if (Has(s, "失明")) return "失明";
            if (Has(s, "诅咒")) return "诅咒";
            if (Has(s, "禁疗") || Has(s, "禁止回复")) return "禁疗";
            return HarvestCjk(s);
        }

        static string HarvestCjk(string s)
        {
            var buf = new char[10];
            var n = 0;
            for (int i = 0; i < s.Length && n < 10; i++)
            {
                var c = s[i];
                if ((c >= 0x4E00 && c <= 0x9FFF) || c == '↑' || c == '↓' || c == 'Ⅱ'
                    || c == '×' || c == 's' || c == ' ' || (c >= '0' && c <= '9'))
                    buf[n++] = c;
            }
            return n > 0 ? new string(buf, 0, n).Trim() : "";
        }

        static bool HasCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] >= 0x4E00 && s[i] <= 0x9FFF) return true;
            return false;
        }

        static string ClampReady(string s)
        {
            return s.Length <= 10 ? s : s.Substring(0, 10);
        }

        static bool Has(string a, string b)
        {
            return a.IndexOf(b, System.StringComparison.Ordinal) >= 0;
        }

        static bool Is(string a, string b)
        {
            return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        }
    }
}
