using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 世界王: wide damage-total bar, not an HP kill. Two rows of ~20 round-head slot hints.
    /// Ranking scrape; no 擊滅 gold ring. Visible copy is 世界王 / 伤害.
    /// 印刷暗金:细金线框 + 暗漆牌 + 半调网点;无 Soft 圆盘、无圆角金卡。
    /// </summary>
    public sealed class VfxWorldBossBar : MonoBehaviour
    {
        public const int SlotHint = 20;

        const int Cols = 10;
        const float BarY = 0.978f;
        const float Catch = 3.2f;

        static readonly Color WellNight = new Color(0.06f, 0.05f, 0.04f, 0.94f);
        static readonly Color SlotNight = new Color(0.07f, 0.06f, 0.05f, 0.92f);
        static readonly Color GoldInk = new Color(0.36f, 0.16f, 0.02f, 0.92f);
        static readonly Color Wire = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f);
        static readonly Color Plate = new Color(0.06f, 0.045f, 0.03f, 0.88f);
        static readonly Color Tone = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.10f);

        static VfxWorldBossBar _live;

        CanvasGroup _group;
        Image _barRim;
        Image _barBg;
        Image _barFill;
        Text _title;
        Text _amountGhost;
        Text _amount;
        Text _tag;
        Text _pct;
        Image[] _slotRim;
        Image[] _slotWell;
        long _damage;
        long _goal;
        float _target;
        float _shown;
        float _pulse;
        bool _snap;

        public static bool Active { get; private set; }

        public static void Draw(Transform parent, long damage, long goal)
        {
            if (parent == null) return;
            Ensure(parent).Apply(damage, goal);
        }

        public static void Hide()
        {
            if (_live != null) _live.End();
            Active = false;
        }

        static VfxWorldBossBar Ensure(Transform parent)
        {
            if (_live != null)
            {
                if (_live.transform.parent != parent)
                    _live.transform.SetParent(parent, false);
                Stretch(_live.transform as RectTransform);
                return _live;
            }

            var existing = parent.GetComponentInChildren<VfxWorldBossBar>(true);
            if (existing != null)
            {
                _live = existing;
                existing.transform.SetParent(parent, false);
                Stretch(existing.transform as RectTransform);
                return existing;
            }

            var go = new GameObject("worldBossBar", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxWorldBossBar));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var fx = go.GetComponent<VfxWorldBossBar>();
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

        void Apply(long damage, long goal)
        {
            if (_barFill == null) Build();
            if (damage < 0L) damage = 0L;
            if (goal < 0L) goal = 0L;

            var rose = damage > _damage;
            _damage = damage;
            _goal = goal;
            _target = Ratio(damage, goal);

            Active = true;
            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;

            if (_snap)
            {
                _shown = _target;
                _snap = false;
            }
            else if (rose)
                _pulse = 1f;

            Paint(true);
        }

        void End()
        {
            Active = false;
            _pulse = 0f;
            _snap = true;
            if (_group != null) _group.alpha = 0f;
            if (gameObject != null) gameObject.SetActive(false);
        }

        void Build()
        {
            if (_barFill != null) return;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _barRim = MkImg(transform, "barRim", new Vector2(0.5f, BarY), new Vector2(1092f, 34f), 0f,
                Wire, UiSprites.WireFrame());
            _barBg = MkImg(transform, "barBg", new Vector2(0.5f, BarY), new Vector2(1068f, 18f), 0f,
                WellNight, UiSprites.Round());
            _barFill = MkImg(transform, "barFill", new Vector2(0.5f, BarY), new Vector2(1056f, 12f), 0f,
                VisualTokens.FeverGold, UiSprites.Round());
            if (_barFill != null && _barFill.sprite == null) UiSprites.Apply(_barFill, UiSprites.Pixel());
            if (_barFill != null)
            {
                _barFill.type = Image.Type.Filled;
                _barFill.fillMethod = Image.FillMethod.Horizontal;
                _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _barFill.fillAmount = 0f;
            }

            MkImg(transform, "titlePlate", new Vector2(0.12f, 0.948f), new Vector2(150f, 34f), 0f,
                Plate, UiSprites.Pixel());
            var titleTone = MkImg(transform, "titleTone", new Vector2(0.12f, 0.948f), new Vector2(150f, 34f), 0f,
                Tone, UiSprites.Halftone());
            if (titleTone != null) titleTone.type = Image.Type.Tiled;
            MkImg(transform, "titleWire", new Vector2(0.12f, 0.948f), new Vector2(158f, 42f), 0f,
                Wire, UiSprites.WireFrame());
            _title = MkText(transform, "title", "世界王", 20, new Vector2(0.12f, 0.948f), new Vector2(150f, 30f),
                VisualTokens.GoldTitle, TextAnchor.MiddleCenter);
            if (_title != null) _title.fontStyle = FontStyle.Normal;
            _amountGhost = MkText(transform, "amtGhost", "", 18, new Vector2(0.5f, BarY), new Vector2(720f, 24f),
                GoldInk, TextAnchor.MiddleCenter);
            _amount = MkText(transform, "amt", "", 18, new Vector2(0.5f, BarY), new Vector2(720f, 24f),
                VisualTokens.TapWhite, TextAnchor.MiddleCenter);
            _tag = MkText(transform, "tag", "伤害", 16, new Vector2(0.5f, 0.948f), new Vector2(160f, 28f),
                VisualTokens.YellowValue, TextAnchor.MiddleCenter);
            _pct = MkText(transform, "pct", "0%", 18, new Vector2(0.88f, 0.948f), new Vector2(160f, 32f),
                VisualTokens.FeverGold, TextAnchor.MiddleRight);

            _slotRim = new Image[SlotHint];
            _slotWell = new Image[SlotHint];
            for (int i = 0; i < SlotHint; i++)
            {
                int row = i / Cols;
                int col = i % Cols;
                var x = Mathf.Lerp(0.08f, 0.92f, col / (float)(Cols - 1));
                var y = (row == 0 ? 0.168f : 0.108f) + 0.018f * Mathf.Sin(col / (float)(Cols - 1) * Mathf.PI);
                var sz = row == 0 ? 44f : 52f;
                _slotRim[i] = MkImg(transform, "slotRim" + i, new Vector2(x, y), new Vector2(sz + 6f, sz + 6f), 0f,
                    Wire, UiSprites.Circle());
                _slotWell[i] = MkImg(transform, "slot" + i, new Vector2(x, y), new Vector2(sz, sz), 0f,
                    SlotNight, UiSprites.Circle());
            }

            _snap = true;
            _shown = 0f;
            _target = 0f;
        }

        void Update()
        {
            if (!Active) return;
            var dt = Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, _target, dt * Catch);
            if (_pulse > 0f)
                _pulse = Mathf.Max(0f, _pulse - dt * 6.4f);
            Paint(false);
        }

        void Paint(bool copy)
        {
            var t = Mathf.Clamp01(_shown);
            var pulse = _pulse * _pulse;
            var hot = Color.Lerp(VisualTokens.Ember, VisualTokens.FeverGold, t);

            if (_barFill != null)
            {
                _barFill.fillAmount = t;
                _barFill.color = hot;
            }
            if (_barRim != null)
                _barRim.color = Color.Lerp(Wire, VisualTokens.GoldSelect, 0.30f * t + 0.45f * pulse);

            if (copy)
            {
                var line = _goal <= 0L ? Comma(_damage) : Comma(_damage) + " / " + Comma(_goal);
                Set(_amountGhost, line, GoldInk);
                Set(_amount, line, VisualTokens.TapWhite);
                Set(_title, "世界王", VisualTokens.GoldTitle);
                Set(_tag, "伤害", VisualTokens.YellowValue);
            }

            var pct = Mathf.RoundToInt(100f * t);
            Set(_pct, pct + "%", Color.Lerp(VisualTokens.YellowValue, VisualTokens.FeverGold, t));

            if (_amount != null)
            {
                _amount.transform.localScale = Vector3.one * (1f + 0.10f * pulse);
                var ghost = _amountGhost != null ? _amountGhost.rectTransform : null;
                var rt = _amount.rectTransform;
                var bump = 6f * pulse;
                rt.anchoredPosition = new Vector2(0f, bump);
                if (ghost != null) ghost.anchoredPosition = new Vector2(3f, bump - 2f);
            }

            PaintSlots(t, pulse);
        }

        void PaintSlots(float t, float pulse)
        {
            if (_slotWell == null) return;
            var litN = t * SlotHint;
            for (int i = 0; i < SlotHint; i++)
            {
                var well = _slotWell[i];
                var rim = _slotRim != null && i < _slotRim.Length ? _slotRim[i] : null;
                if (well == null) continue;

                var u = Mathf.Clamp01(litN - i);
                var lit = u > 0.02f;
                var pop = (i == Mathf.Clamp(Mathf.FloorToInt(litN), 0, SlotHint - 1)) ? pulse : 0f;
                well.color = lit
                    ? Color.Lerp(SlotNight, HotGold(u), 0.55f + 0.35f * u)
                    : SlotNight;
                if (rim != null)
                    rim.color = lit
                        ? Color.Lerp(Wire, VisualTokens.GoldSelect, 0.40f + 0.40f * pop)
                        : new Color(Wire.r, Wire.g, Wire.b, 0.50f);

                var s = 1f + 0.08f * pop;
                well.transform.localScale = Vector3.one * s;
                if (rim != null) rim.transform.localScale = Vector3.one * s;
            }
        }

        static Color HotGold(float u)
        {
            return Color.Lerp(VisualTokens.Ember, VisualTokens.FeverGold, Mathf.Clamp01(u));
        }

        static float Ratio(long damage, long goal)
        {
            if (goal <= 0L) return 0f;
            var t = (float)((double)damage / (double)goal);
            if (float.IsNaN(t) || float.IsInfinity(t)) return 0f;
            return Mathf.Clamp01(t);
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

        static Text MkText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 dim, Color color, TextAnchor align)
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
            tx.alignment = align;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.color = color;
            tx.text = text ?? "";
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }

        static void Set(Text tx, string s, Color c)
        {
            if (tx == null) return;
            tx.text = s ?? "";
            tx.color = c;
        }

        static string Comma(long n)
        {
            if (n < 0L) n = 0L;
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

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
