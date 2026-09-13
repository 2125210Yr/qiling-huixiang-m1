using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 沉默: muted note and X over the mouth. 0.6s. No English Silence.
    /// </summary>
    public sealed class VfxSilence : MonoBehaviour
    {
        const float Life = 0.60f;

        static readonly Color Ink = VisualTokens.ElemDark;
        static readonly Color Hot = new Color(0.86f, 0.62f, 1f, 1f);
        static readonly Color Lip = new Color(0.22f, 0.08f, 0.28f, 0.96f);
        static readonly Vector2 Mouth = new Vector2(0f, 8f);
        static readonly Vector2 Note = new Vector2(20f, 40f);

        struct Mote
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 From;
            public Vector2 To;
            public float Delay;
            public float Spin;
            public Color Color;
            public float Sz;
        }

        Image _glow;
        Image _ring;
        Image _lips;
        Image _seal;
        Image _xA;
        Image _xB;
        Image _noteHead;
        Image _noteStem;
        Image _noteFlag;
        Image _muteSlash;
        Mote[] _motes;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;

            var go = new GameObject("vfxSilence", typeof(RectTransform), typeof(VfxSilence));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxSilence>().Build();
        }

        void Build()
        {
            _glow = Child("glow", UiSprites.Soft(), WithA(Ink, 0.42f), new Vector2(96f, 96f), Mouth);
            _ring = Child("ring", UiSprites.Circle(), WithA(Ink, 0.55f), new Vector2(58f, 58f), Mouth);

            _lips = Child("lips", UiSprites.Circle(), WithA(Lip, 0.96f), new Vector2(48f, 22f), Mouth);
            _seal = Child("seal", UiSprites.Soft(), WithA(Hot, 0.90f), new Vector2(30f, 4f), Mouth);

            _xA = Child("xA", UiSprites.Slash(), WithA(Hot, 0.98f), new Vector2(52f, 8f), Mouth);
            _xA.rectTransform.localEulerAngles = new Vector3(0f, 0f, 42f);
            _xB = Child("xB", UiSprites.Slash(), WithA(Ink, 0.98f), new Vector2(52f, 8f), Mouth);
            _xB.rectTransform.localEulerAngles = new Vector3(0f, 0f, -42f);

            _noteHead = Child("noteHead", UiSprites.Circle(), WithA(Ink, 1f), new Vector2(20f, 16f), Note);
            _noteHead.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
            _noteStem = Child("noteStem", UiSprites.Soft(), WithA(Ink, 1f), new Vector2(5f, 30f),
                Note + new Vector2(8f, 16f));
            _noteFlag = Child("noteFlag", UiSprites.Soft(), WithA(Hot, 1f), new Vector2(16f, 7f),
                Note + new Vector2(16f, 28f));
            _noteFlag.rectTransform.localEulerAngles = new Vector3(0f, 0f, -22f);

            _muteSlash = Child("mute", UiSprites.Slash(), WithA(Hot, 0.98f), new Vector2(58f, 12f),
                Note + new Vector2(6f, 12f));
            _muteSlash.rectTransform.localEulerAngles = new Vector3(0f, 0f, -38f);
            _muteSlash.rectTransform.localScale = new Vector3(0.12f, 1f, 1f);

            const int n = 5;
            _motes = new Mote[n];
            for (int i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + 0.35f;
                var from = Mouth + new Vector2(Mathf.Cos(ang) * 10f, Mathf.Sin(ang) * 6f);
                var to = Mouth + new Vector2(Mathf.Cos(ang) * 38f, Mathf.Sin(ang) * 28f + 8f);
                var sz = (i & 1) == 0 ? 10f : 7f;
                var col = (i & 1) == 0 ? Ink : Hot;
                col.a = 1f;
                var img = Child("mote" + i, UiSprites.Circle(),
                    col, new Vector2(sz, sz * 0.78f), from);
                img.color = Color.clear;
                _motes[i] = new Mote
                {
                    Rt = img.rectTransform,
                    Img = img,
                    From = from,
                    To = to,
                    Delay = 0.04f + i * 0.032f,
                    Spin = ((i & 1) == 0 ? 80f : -110f) + i * 12f,
                    Color = col,
                    Sz = sz
                };
            }

            _tag = MkText(transform, "Silence", 22, Mouth + new Vector2(0f, 36f), new Vector2(160f, 36f), Hot);

            _glow.transform.localScale = Vector3.one * 0.55f;
            _ring.transform.localScale = Vector3.one * 1.25f;
            _lips.transform.localScale = new Vector3(1.12f, 0.72f, 1f);
            _xA.transform.localScale = Vector3.one * 1.70f;
            _xB.transform.localScale = Vector3.one * 1.70f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var ease = 1f - (1f - u) * (1f - u);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.18f)) * (1f - Mathf.Clamp01(u / 0.18f));
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.52f) / 0.48f);
            var bob = Mathf.Sin(_age * 13f) * 2.4f;

            if (_glow != null)
            {
                _glow.rectTransform.anchoredPosition = Mouth;
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.38f, ease);
                SetA(_glow, 0.42f * fade);
            }

            if (_ring != null)
            {
                _ring.rectTransform.anchoredPosition = Mouth;
                var s = Mathf.Lerp(1.25f, 0.38f, ease);
                _ring.rectTransform.localScale = new Vector3(s, s * 0.82f, 1f);
                SetA(_ring, 0.55f * (1f - u) * fade);
            }

            if (_lips != null)
            {
                _lips.rectTransform.anchoredPosition = Mouth + new Vector2(0f, bob * 0.15f);
                _lips.rectTransform.localScale = new Vector3(
                    Mathf.Lerp(1.12f, 0.96f, slam),
                    Mathf.Lerp(0.72f, 0.42f, slam),
                    1f);
                SetA(_lips, 0.96f * fade);
            }

            if (_seal != null)
            {
                _seal.rectTransform.anchoredPosition = Mouth;
                _seal.rectTransform.localScale = new Vector3(Mathf.Lerp(0.35f, 1.05f, slam), 1f, 1f);
                SetA(_seal, 0.90f * fade);
            }

            if (_xA != null)
            {
                _xA.rectTransform.anchoredPosition = Mouth;
                _xA.transform.localScale = Vector3.one * Mathf.Lerp(1.70f, 1f, slam);
                _xA.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(58f, 42f, slam));
                SetA(_xA, 0.98f * fade);
            }
            if (_xB != null)
            {
                _xB.rectTransform.anchoredPosition = Mouth;
                _xB.transform.localScale = Vector3.one * Mathf.Lerp(1.70f, 1f, slam);
                _xB.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-58f, -42f, slam));
                SetA(_xB, 0.98f * fade);
            }

            var noteBob = new Vector2(0f, bob);
            if (_noteHead != null)
            {
                _noteHead.rectTransform.anchoredPosition = Note + noteBob;
                _noteHead.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f + bob * 2f);
                SetA(_noteHead, fade);
            }
            if (_noteStem != null)
            {
                _noteStem.rectTransform.anchoredPosition = Note + new Vector2(8f, 16f) + noteBob;
                SetA(_noteStem, fade);
            }
            if (_noteFlag != null)
            {
                _noteFlag.rectTransform.anchoredPosition = Note + new Vector2(16f, 28f) + noteBob;
                _noteFlag.rectTransform.localEulerAngles = new Vector3(0f, 0f, -22f + bob * 3f);
                SetA(_noteFlag, fade);
            }
            if (_muteSlash != null)
            {
                var sk = Mathf.Clamp01((_age - 0.05f) / 0.16f);
                var pop = 1f - (1f - sk) * (1f - sk);
                _muteSlash.rectTransform.anchoredPosition = Note + new Vector2(6f, 12f) + noteBob;
                _muteSlash.rectTransform.localScale = new Vector3(Mathf.Lerp(0.12f, 1.12f, pop), 1f, 1f);
                SetA(_muteSlash, 0.98f * fade * Mathf.Clamp01(sk / 0.20f));
            }

            if (_motes != null)
            {
                for (int i = 0; i < _motes.Length; i++)
                {
                    var m = _motes[i];
                    if (m.Rt == null || m.Img == null) continue;
                    if (_age < m.Delay)
                    {
                        SetA(m.Img, 0f);
                        continue;
                    }
                    var t = Mathf.Clamp01((_age - m.Delay) / Mathf.Max(0.08f, Life - m.Delay));
                    var tu = 1f - (1f - t) * (1f - t);
                    m.Rt.anchoredPosition = Vector2.LerpUnclamped(m.From, m.To, tu);
                    m.Rt.localEulerAngles = new Vector3(0f, 0f, m.Spin * tu);
                    var sz = Mathf.Lerp(m.Sz, m.Sz * 0.55f, tu);
                    m.Rt.sizeDelta = new Vector2(sz, sz * 0.78f);
                    var a = t < 0.12f ? t / 0.12f : 1f - Mathf.Clamp01((t - 0.42f) / 0.58f);
                    var c = m.Color;
                    c.a = m.Color.a * a;
                    m.Img.color = c;
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = Mouth + new Vector2(0f, 36f + 14f * ease);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static void SetA(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        Image Child(string name, Sprite sprite, Color color, Vector2 size, Vector2 pos)
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

        static Text MkText(Transform parent, string text, int size, Vector2 pos, Vector2 dim, Color color)
        {
            var go = new GameObject("tx", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = text;
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }
    }
}
