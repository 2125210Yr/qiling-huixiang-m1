using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Ordinary stage phase chip under arch. Copy is PHASE n/m (primary GT).
    /// 印刷暗金:暗漆底 + 半调网点 + 细金线框 + 错版墨影;无 Soft 圆盘、无圆角金卡。
    /// Persistent overlay. maxPhase below 1 hides. Does not touch BattleHud.
    /// </summary>
    public sealed class VfxPhaseBar : MonoBehaviour
    {
        const float AnchorY = 0.895f;
        const int PipCap = 6;
        const int LabSize = 15;
        const int NumSize = 21;
        const float SegW = 22f;
        const float SegH = 3f;
        const float SegGap = 8f;

        static readonly Color Gold = VisualTokens.GoldTitle;
        static readonly Color Hot = VisualTokens.YellowValue;
        static readonly Color Wire = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.92f);
        static readonly Color Plate = new Color(0.06f, 0.045f, 0.03f, 0.88f);
        static readonly Color Tone = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.10f);
        static readonly Color PipDim = new Color(0.24f, 0.19f, 0.12f, 0.85f);
        static readonly Color Ghost = new Color(0.12f, 0.06f, 0.02f, 0.90f);

        CanvasGroup _group;
        Image _wire;
        Text _ghost;
        Text _label;
        Image[] _segs;
        int _phase;
        int _max;
        float _pulse;

        public int Phase => _phase;
        public int MaxPhase => _max;
        public bool Visible => gameObject.activeSelf && _group != null && _group.alpha > 0.01f;

        public static void Draw(Transform parent, int phase, int maxPhase)
        {
            if (parent == null) return;
            if (maxPhase < 1)
            {
                var dead = Find(parent);
                if (dead != null) dead.gameObject.SetActive(false);
                return;
            }

            phase = Mathf.Clamp(phase, 1, maxPhase);

            var fx = Find(parent);
            if (fx == null)
            {
                var go = new GameObject("phaseBar", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxPhaseBar));
                go.transform.SetParent(parent, false);
                fx = go.GetComponent<VfxPhaseBar>();
                fx.Build();
            }
            else if (fx.transform.parent != parent)
                fx.transform.SetParent(parent, false);

            fx.Apply(phase, maxPhase);
        }

        void Build()
        {
            if (_label != null) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, AnchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 52f);
            rt.anchoredPosition = Vector2.zero;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            Pic("plate", UiSprites.Pixel(), Plate, new Vector2(340f, 40f), new Vector2(0f, 2f));
            var tone = Pic("tone", UiSprites.Halftone(), Tone, new Vector2(340f, 40f), new Vector2(0f, 2f));
            tone.type = Image.Type.Tiled;
            _wire = Pic("wire", UiSprites.WireFrame(), Wire, new Vector2(348f, 48f), new Vector2(0f, 2f));
            _ghost = MkText("ghost", LabSize, Ghost, new Vector2(2f, 6f), new Vector2(300f, 30f));
            _label = MkText("label", LabSize, Gold, new Vector2(0f, 8f), new Vector2(300f, 30f));
            Pic("rule", UiSprites.Pixel(), new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.55f),
                new Vector2(140f, 1f), new Vector2(0f, -4f));
        }

        void Apply(int phase, int maxPhase)
        {
            if (_label == null) Build();
            var changed = phase != _phase || maxPhase != _max;
            _phase = phase;
            _max = maxPhase;
            gameObject.SetActive(true);
            if (_group != null) _group.alpha = 1f;
            Paint();
            if (changed) _pulse = 1f;
        }

        void Paint()
        {
            var line = Rich(_phase, _max);
            Set(_label, line, Gold);
            Set(_ghost, line, Ghost);
            EnsureSegs(Mathf.Clamp(_max, 1, PipCap));
            if (_segs == null) return;
            var curI = Mathf.Clamp(_phase, 1, _segs.Length) - 1;
            for (int i = 0; i < _segs.Length; i++)
            {
                var img = _segs[i];
                if (img == null) continue;
                var done = i < _phase;
                img.color = i == curI ? Hot : done ? Wire : PipDim;
                img.transform.localScale = Vector3.one;
            }
        }

        void LateUpdate()
        {
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime * 6.4f);
                var k = _pulse * _pulse;
                transform.localScale = Vector3.one * (1f + 0.06f * k);
                if (_wire != null)
                    _wire.color = Color.Lerp(Wire, VisualTokens.GoldSelect, 0.65f * k);
            }
            else
            {
                transform.localScale = Vector3.one;
                if (_wire != null) _wire.color = Wire;
            }

            if (_segs == null || _phase < 1) return;
            var cur = Mathf.Clamp(_phase, 1, _segs.Length) - 1;
            var breath = 0.92f + 0.08f * Mathf.Sin(Time.unscaledTime * 5.6f);
            for (int i = 0; i < _segs.Length; i++)
            {
                var img = _segs[i];
                if (img == null) continue;
                img.transform.localScale = Vector3.one * (i == cur ? breath : 1f);
            }
        }

        void EnsureSegs(int n)
        {
            if (n < 1) n = 1;
            if (_segs != null && _segs.Length == n) return;
            if (_segs != null)
            {
                for (int i = 0; i < _segs.Length; i++)
                    if (_segs[i] != null) Destroy(_segs[i].gameObject);
            }

            _segs = new Image[n];
            var total = n * SegW + (n - 1) * SegGap;
            var x0 = -total * 0.5f + SegW * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var x = x0 + i * (SegW + SegGap);
                _segs[i] = Pic("seg" + i, UiSprites.Pixel(), PipDim, new Vector2(SegW, SegH), new Vector2(x, -13f));
            }
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
            tx.fontStyle = FontStyle.Normal;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.text = "";
            tx.supportRichText = true;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
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

        static string Rich(int phase, int maxPhase)
        {
            return "<size=" + LabSize + ">PHASE</size><size=" + NumSize + "> <b>" + phase + "/" + maxPhase + "</b></size>";
        }

        static VfxPhaseBar Find(Transform parent)
        {
            var self = parent.GetComponent<VfxPhaseBar>();
            if (self != null) return self;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i).GetComponent<VfxPhaseBar>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
