using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Ragna top chrome: one arched million-scale boss HP bar plus 阶段.
    /// Overlay only — does not replace BattleHud. Copy is 阶段, never PHASE / HP.
    /// 印刷暗金:拱条细金线描边 + 阶段暗漆牌 chip;无 Soft 圆盘、无圆角金卡。
    /// </summary>
    public sealed class VfxRagnaBar : MonoBehaviour
    {
        const float ArchY = 0.978f;
        const float ArchDrop = 0.020f;
        const float ArchLeft = 0.04f;
        const float ArchRight = 0.96f;

        static readonly Color TroughCol = new Color(0.10f, 0.02f, 0.02f, 0.94f);
        static readonly Color FillCol = VisualTokens.StarEvolved;
        static readonly Color ShineCol = new Color(1f, 0.72f, 0.52f, 0.42f);
        static readonly Color Ink = new Color(0.22f, 0.04f, 0.04f, 0.92f);
        static readonly Color PhaseGold = VisualTokens.GoldTitle;
        static readonly Color Wire = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f);
        static readonly Color Plate = new Color(0.06f, 0.045f, 0.03f, 0.88f);
        static readonly Color Tone = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.10f);

        static Sprite _arch;

        CanvasGroup _group;
        Image _archWire;
        Image _chipWire;
        Image _trough;
        Image _fill;
        Image _shine;
        Image _edge;
        Text _hpGhost;
        Text _hp;
        Text _phaseGhost;
        Text _phase;
        Text _pct;
        float _shown = 1f;
        float _ratio = 1f;
        float _pulse;
        int _hpNow = -1;
        int _hpMax = -1;
        int _phaseN = -1;
        bool _inited;

        public int Hp => _hpNow;
        public int MaxHp => _hpMax;
        public int Phase => _phaseN;

        public static VfxRagnaBar Draw(Transform parent, int hp, int maxHp, int phase)
        {
            if (parent == null) return null;
            if (maxHp < 1)
            {
                var dead = Find(parent);
                if (dead != null) dead.Hide();
                return null;
            }

            var fx = Find(parent);
            if (fx == null)
            {
                var go = new GameObject("ragnaBar", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxRagnaBar));
                go.transform.SetParent(parent, false);
                fx = go.GetComponent<VfxRagnaBar>();
                fx.Build();
            }
            else if (fx.transform.parent != parent)
                fx.transform.SetParent(parent, false);

            fx.Apply(hp, maxHp, phase);
            return fx;
        }

        public void Hide()
        {
            _pulse = 0f;
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        void Build()
        {
            if (_fill != null) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var arch = ArchSprite();
            _archWire = Pic("archWire", arch, Wire, new Vector2(0.50f, ArchY), new Vector2(1090f, 80f));
            _trough = Pic("trough", arch, TroughCol, new Vector2(0.50f, ArchY), new Vector2(1080f, 72f));
            _fill = Pic("fill", arch, FillCol, new Vector2(0.50f, ArchY), new Vector2(1072f, 64f));
            FillBar(_fill);
            _shine = Pic("shine", arch, ShineCol, new Vector2(0.50f, ArchY + 0.004f), new Vector2(1072f, 28f));
            FillBar(_shine);
            _edge = Pic("edge", UiSprites.Circle(), Color.white, new Vector2(0.50f, ArchY), new Vector2(20f, 20f));

            _hpGhost = MkText("hpGhost", 28, Ink, new Vector2(0.50f, ArchY), new Vector2(4f, -4f), new Vector2(880f, 44f));
            _hp = MkText("hp", 28, VisualTokens.TapWhite, new Vector2(0.50f, ArchY), Vector2.zero, new Vector2(880f, 44f));
            Pic("chipPlate", UiSprites.Pixel(), Plate, new Vector2(0.50f, 0.948f), new Vector2(170f, 34f));
            var tone = Pic("chipTone", UiSprites.Halftone(), Tone, new Vector2(0.50f, 0.948f), new Vector2(170f, 34f));
            tone.type = Image.Type.Tiled;
            _chipWire = Pic("chipWire", UiSprites.WireFrame(), Wire, new Vector2(0.50f, 0.948f), new Vector2(178f, 42f));
            _phaseGhost = MkText("phaseGhost", 20, Ink, new Vector2(0.50f, 0.948f), new Vector2(2f, -2f), new Vector2(160f, 30f));
            _phase = MkText("phase", 20, PhaseGold, new Vector2(0.50f, 0.948f), Vector2.zero, new Vector2(160f, 30f));
            _phase.fontStyle = FontStyle.Normal;
            _phaseGhost.fontStyle = FontStyle.Normal;
            _pct = MkText("pct", 18, VisualTokens.TapWhite, new Vector2(0.50f, 0.922f), Vector2.zero, new Vector2(220f, 28f));
        }

        void Apply(int hp, int maxHp, int phase)
        {
            hp = Mathf.Max(0, hp);
            maxHp = Mathf.Max(0, maxHp);
            phase = Mathf.Max(1, phase);
            if (maxHp < 1)
            {
                Hide();
                return;
            }
            if (_fill == null) Build();
            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;

            var ratio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)hp / maxHp);
            if (_inited && hp < _hpNow) _pulse = 1f;
            var dirty = hp != _hpNow || maxHp != _hpMax || phase != _phaseN;
            _hpNow = hp;
            _hpMax = maxHp;
            _phaseN = phase;
            _ratio = ratio;
            if (!_inited)
            {
                _shown = ratio;
                _inited = true;
                PaintFill(_shown);
            }
            if (dirty) PaintCopy();
        }

        void PaintCopy()
        {
            var line = Comma(_hpNow) + " / " + Comma(_hpMax);
            Set(_hp, line, VisualTokens.TapWhite);
            Set(_hpGhost, line, Ink);
            var ph = "阶段 " + _phaseN;
            Set(_phase, ph, PhaseGold);
            Set(_phaseGhost, ph, Ink);
            var pct = _hpMax <= 0 ? 0 : Mathf.RoundToInt(100f * _ratio);
            Set(_pct, pct + "%", VisualTokens.TapWhite);
        }

        void LateUpdate()
        {
            if (!gameObject.activeSelf) return;
            var dt = Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, _ratio, dt * 1.65f);
            _pulse = Mathf.Max(0f, _pulse - dt * 5.4f);
            PaintFill(_shown);
        }

        void PaintFill(float u)
        {
            u = Mathf.Clamp01(u);
            var low = u < 0.22f;
            var flicker = low ? 0.40f + 0.60f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9.2f)) : 1f;
            var fill = Color.Lerp(FillCol, Color.white, _pulse * 0.72f);
            if (low) fill = Color.Lerp(fill, VisualTokens.Ember, 0.45f * flicker);

            if (_fill != null)
            {
                _fill.fillAmount = u;
                _fill.color = fill;
            }
            if (_shine != null)
            {
                _shine.fillAmount = u;
                var sc = ShineCol;
                sc.a = (0.22f + 0.38f * _pulse) * flicker;
                _shine.color = sc;
            }
            if (_archWire != null)
            {
                var w = Color.Lerp(Wire, VisualTokens.GoldSelect, 0.15f + 0.55f * _pulse);
                w.a = Wire.a * (low ? 0.55f + 0.45f * flicker : 1f);
                _archWire.color = w;
            }
            if (_chipWire != null)
                _chipWire.color = Color.Lerp(Wire, VisualTokens.GoldSelect, 0.45f * _pulse);
            if (_edge == null) return;

            var ert = _edge.rectTransform;
            ert.anchorMin = ert.anchorMax = EdgeAnchor(u);
            ert.anchoredPosition = Vector2.zero;
            var on = u > 0.02f && u < 0.985f;
            var ec = Color.Lerp(fill, Color.white, 0.55f + 0.45f * _pulse);
            ec.a = on ? 0.55f + 0.45f * _pulse : 0f;
            _edge.color = ec;
            ert.localScale = Vector3.one * (0.85f + 0.45f * _pulse);
        }

        static Vector2 EdgeAnchor(float u)
        {
            u = Mathf.Clamp01(u);
            var x = Mathf.Lerp(ArchLeft, ArchRight, u);
            var y = ArchY - ArchDrop * (1f - Mathf.Sin(u * Mathf.PI));
            return new Vector2(x, y);
        }

        static void FillBar(Image img)
        {
            if (img == null) return;
            if (img.sprite == null) UiSprites.Apply(img, UiSprites.Pixel());
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.preserveAspect = false;
        }

        Image Pic(string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 dim)
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
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2.5f, -2.5f);
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

        static Sprite ArchSprite()
        {
            if (_arch != null) return _arch;
            const int w = 512;
            const int h = 64;
            const float sag = 24f;
            const float thick = 28f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            var half = w * 0.5f;
            var rOuter = (sag * sag + half * half) / (2f * sag);
            var rInner = rOuter - thick;
            var cx = half;
            var cy = (h - 4f) - rOuter;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var dx = x + 0.5f - cx;
                    var dy = y + 0.5f - cy;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(rOuter - r + 1f) * Mathf.Clamp01(r - rInner + 1f);
                    var nx = Mathf.Abs(dx) / half;
                    if (nx > 0.96f) a *= Mathf.Clamp01((1f - nx) / 0.04f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _arch = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            _arch.name = "ragnaArch";
            return _arch;
        }

        static VfxRagnaBar Find(Transform parent)
        {
            var self = parent.GetComponent<VfxRagnaBar>();
            if (self != null) return self;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i).GetComponent<VfxRagnaBar>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
