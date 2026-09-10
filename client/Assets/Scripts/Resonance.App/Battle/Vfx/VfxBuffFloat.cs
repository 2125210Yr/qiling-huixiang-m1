using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Buff 飘字 above a portrait. Words: 再生 / 吸血 / 攻击叠加 / 减益爆破.
    /// Printed ticket: dark ink body, thin element wire, 12px type with black outline.
    /// </summary>
    public sealed class VfxBuffFloat : MonoBehaviour
    {
        public const string Regen = "再生";
        public const string Lifesteal = "吸血";
        public const string AtkStack = "攻击叠加";
        public const string DebuffBlast = "减益爆破";

        static readonly Color RegenInk = new Color(0.45f, 1f, 0.55f, 1f);
        static readonly Color LifestealInk = new Color(0.92f, 0.18f, 0.28f, 1f);
        static readonly Color AtkStackInk = new Color(0.22f, 0.92f, 0.96f, 1f);
        static readonly Color BlastInk = new Color(0.86f, 0.38f, 1f, 1f);
        static readonly Color InkDark = new Color(0.07f, 0.06f, 0.09f, 0.86f);
        static readonly Color Paper = new Color(0.97f, 0.94f, 0.86f, 1f);

        enum Kind
        {
            Generic,
            Regen,
            Lifesteal,
            AtkStack,
            Blast
        }

        Kind _kind;
        Color _ink;
        Image _glow;
        Image _body;
        Image[] _wires;
        Image _barBg;
        Image _barFill;
        Image _ring;
        Image _flash;
        Image[] _motes;
        Vector2[] _from;
        Vector2[] _to;
        float[] _spin;
        Text _label;
        float _life = 0.86f;
        float _age;
        Vector2 _origin;
        float _rise = 52f;

        public static void Play(Transform parent, Vector2 anchor, string label, Color color)
        {
            if (parent == null) return;

            var kind = KindOf(label, out var zh);
            if (string.IsNullOrEmpty(zh)) return;
            if (color.a < 0.05f || Chroma(color) < 0.08f) color = ColorOfKind(kind);
            else color = new Color(color.r, color.g, color.b, 1f);

            var go = new GameObject("buffFloat", typeof(RectTransform), typeof(VfxBuffFloat));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor.x), Mathf.Clamp01(anchor.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 140f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            var fx = go.GetComponent<VfxBuffFloat>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(kind, zh, color);
        }

        public static Color ColorOf(string label)
        {
            return ColorOfKind(KindOf(label, out _));
        }

        void Build(Kind kind, string zh, Color ink)
        {
            _kind = kind;
            _ink = ink;
            _life = kind == Kind.Blast ? 0.72f : kind == Kind.AtkStack ? 0.96f : 0.88f;
            _rise = kind == Kind.Blast ? 36f : 56f;
            _origin = new Vector2(Random.Range(-16f, 16f), Random.Range(28f, 40f));

            _glow = Img("glow", UiSprites.Soft(), new Color(ink.r, ink.g, ink.b, 0.30f),
                kind == Kind.Blast ? 110f : 80f);
            _glow.rectTransform.anchoredPosition = _origin;

            var w = 18f + zh.Length * 14f;
            const float h = 22f;
            var wire = Mute(ink);

            _body = Img("body", UiSprites.Pixel(), InkDark, 0f);
            _body.rectTransform.sizeDelta = new Vector2(w, h);
            _body.rectTransform.anchoredPosition = _origin;

            _wires = new Image[4];
            _wires[0] = Wire("wireT", new Vector2(w, 1f), _origin + new Vector2(0f, h * 0.5f - 0.5f), wire);
            _wires[1] = Wire("wireB", new Vector2(w, 1f), _origin + new Vector2(0f, 0.5f - h * 0.5f), wire);
            _wires[2] = Wire("wireL", new Vector2(1f, h - 2f), _origin + new Vector2(0.5f - w * 0.5f, 0f), wire);
            _wires[3] = Wire("wireR", new Vector2(1f, h - 2f), _origin + new Vector2(w * 0.5f - 0.5f, 0f), wire);

            _label = MkText("label", zh, 12, Paper, new Vector2(w + 8f, h + 4f), _origin);

            if (kind == Kind.AtkStack)
            {
                var barPos = _origin + new Vector2(0f, -17f);
                _barBg = Img("barBg", UiSprites.Pixel(), new Color(0.04f, 0.05f, 0.07f, 0.88f), 0f);
                _barBg.rectTransform.sizeDelta = new Vector2(w, 7f);
                _barBg.rectTransform.anchoredPosition = barPos;
                _barFill = Img("barFill", UiSprites.Pixel(), AtkStackInk, 0f);
                _barFill.rectTransform.sizeDelta = new Vector2(w - 4f, 3f);
                _barFill.rectTransform.anchoredPosition = barPos;
                _barFill.type = Image.Type.Filled;
                _barFill.fillMethod = Image.FillMethod.Horizontal;
                _barFill.fillOrigin = 0;
                _barFill.fillAmount = 0f;
            }

            if (kind == Kind.Blast)
            {
                _flash = Img("flash", UiSprites.Soft(), new Color(1f, 0.86f, 1f, 0.90f), 56f);
                _flash.rectTransform.anchoredPosition = _origin;
                _ring = Img("ring", UiSprites.Circle(), new Color(ink.r, ink.g, ink.b, 0.92f), 40f);
                _ring.rectTransform.anchoredPosition = _origin;
                CanvasShake.Punch(10f, 0.16f);
                var n = 8;
                for (int i = 0; i < n; i++)
                {
                    var ang = (i / (float)n) * Mathf.PI * 2f;
                    Spark.Spawn(transform, i % 2 == 0 ? ink : Color.white, ang, 78f, false);
                }
            }

            if (kind == Kind.Regen || kind == Kind.Lifesteal)
            {
                var n = kind == Kind.Regen ? 6 : 5;
                _motes = new Image[n];
                _from = new Vector2[n];
                _to = new Vector2[n];
                _spin = new float[n];
                for (int i = 0; i < n; i++)
                {
                    var x = Random.Range(-26f, 26f);
                    var y = Random.Range(-8f, 10f);
                    _from[i] = _origin + new Vector2(x, y);
                    if (kind == Kind.Regen)
                        _to[i] = _from[i] + new Vector2(Random.Range(-8f, 8f), Random.Range(48f, 84f));
                    else
                        _to[i] = _from[i] + new Vector2(Random.Range(-20f, 20f), Random.Range(26f, 56f));
                    _spin[i] = Random.Range(-40f, 40f);
                    var sz = kind == Kind.Regen ? Random.Range(10f, 16f) : Random.Range(6f, 10f);
                    var spr = kind == Kind.Regen ? UiSprites.Plus() : UiSprites.Spark();
                    var img = Img("mote" + i, spr, ink, sz);
                    if (img == null) continue;
                    img.rectTransform.anchoredPosition = _from[i];
                    img.canvasRenderer.cullTransparentMesh = false;
                    img.color = Color.clear;
                    _motes[i] = img;
                }
            }

            ((RectTransform)transform).anchoredPosition = Snap(_origin * 0.15f);
            transform.localScale = Vector3.one * (kind == Kind.Blast ? 1.30f : 1.12f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.05f, _life));
            var pop = u < 0.14f
                ? Mathf.Lerp(_kind == Kind.Blast ? 1.30f : 1.12f, 1.02f, u / 0.14f)
                : Mathf.Lerp(1.02f, 0.96f, (u - 0.14f) / 0.86f);
            transform.localScale = Vector3.one * pop;
            ((RectTransform)transform).anchoredPosition = Snap(new Vector2(_origin.x * 0.12f, _rise * u));

            var fade = u < 0.58f ? 1f : 1f - (u - 0.58f) / 0.42f;
            fade = Mathf.Clamp01(fade);

            SetA(_glow, 0.30f * fade);
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, _kind == Kind.Blast ? 1.85f : 1.28f, u);
            SetA(_body, 0.86f * fade);
            if (_wires != null)
            {
                for (int i = 0; i < _wires.Length; i++)
                    SetA(_wires[i], 0.95f * fade);
            }
            Fade(_label, fade);

            if (_barFill != null)
            {
                _barFill.fillAmount = Mathf.Clamp01(u / 0.38f);
                SetA(_barBg, 0.88f * fade);
                SetA(_barFill, fade);
            }

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 2.40f, 1f - (1f - u) * (1f - u));
                SetA(_ring, 0.80f * (1f - u));
            }
            if (_flash != null)
            {
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.22f, u);
                SetA(_flash, 0.90f * (1f - u * u));
            }

            if (_motes != null && _from != null && _to != null && _spin != null)
            {
                var n = Mathf.Min(_motes.Length, Mathf.Min(_from.Length, Mathf.Min(_to.Length, _spin.Length)));
                for (int i = 0; i < n; i++)
                {
                    var img = _motes[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - i * 0.05f) / 0.78f);
                    var ease = 1f - (1f - t) * (1f - t);
                    img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(_from[i], _to[i], ease);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _spin[i] * ease);
                    var a = t < 0.12f ? t / 0.12f : 1f - Mathf.Clamp01((t - 0.50f) / 0.50f);
                    var c = _ink;
                    c.a = Mathf.Clamp01(a) * fade;
                    img.color = c;
                    img.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.10f, 1f - t);
                }
            }

            if (_age >= _life) Destroy(gameObject);
        }

        static Kind KindOf(string label, out string zh)
        {
            zh = label ?? "";
            if (zh.Length == 0) return Kind.Generic;
            if (Is(zh, Regen) || Is(zh, "regen") || zh == "回复")
            {
                zh = Regen;
                return Kind.Regen;
            }
            if (Is(zh, Lifesteal) || Is(zh, "vampirism") || Is(zh, "lifesteal"))
            {
                zh = Lifesteal;
                return Kind.Lifesteal;
            }
            if (zh == AtkStack || Is(zh, "atk stack") || Is(zh, "atkstack") || Is(zh, "attack stack")
                || zh == "攻击力↑" || zh == "呐喊")
            {
                zh = AtkStack;
                return Kind.AtkStack;
            }
            if (zh == DebuffBlast || Is(zh, "debuff blast") || Is(zh, "debuffblast"))
            {
                zh = DebuffBlast;
                return Kind.Blast;
            }
            if (Is(zh, "critical") || Is(zh, "crit") || zh == "暴击")
            {
                zh = "暴击";
                return Kind.Generic;
            }
            if (Is(zh, "weak") || Is(zh, "weakpoint") || Is(zh, "weak point") || zh == "弱点")
            {
                zh = "弱点";
                return Kind.Generic;
            }
            if (Is(zh, "heal") || zh == "恢复" || zh == "治疗")
            {
                zh = "恢复";
                return Kind.Generic;
            }
            if (!HasCjk(zh))
            {
                zh = "";
                return Kind.Generic;
            }
            return Kind.Generic;
        }

        static bool HasCjk(string s)
        {
            if (s == null) return false;
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            }
            return false;
        }

        static Color ColorOfKind(Kind kind)
        {
            switch (kind)
            {
                case Kind.Regen: return RegenInk;
                case Kind.Lifesteal: return LifestealInk;
                case Kind.AtkStack: return AtkStackInk;
                case Kind.Blast: return BlastInk;
                default: return VisualTokens.YellowValue;
            }
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

        static bool Is(string a, string b)
        {
            return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        static float Chroma(Color c)
        {
            var max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            var min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min;
        }

        static Vector2 Snap(Vector2 p)
        {
            return new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        }

        Image Wire(string name, Vector2 size, Vector2 pos, Color color)
        {
            var img = Img(name, UiSprites.Pixel(), color, 0f);
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            return img;
        }

        Image Img(string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 dim, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Normal;
            tx.text = text;
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(1f, -1f);
            return tx;
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
