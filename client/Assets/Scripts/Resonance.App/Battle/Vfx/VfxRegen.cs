using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 再生: smooth green medical cross and rising cross motes on an ally.
    /// </summary>
    public sealed class VfxRegen : MonoBehaviour
    {
        static readonly Color Ink = new Color(0.45f, 1f, 0.55f, 1f);
        static readonly Color GlowCol = new Color(0.32f, 0.92f, 0.48f, 0.42f);

        const float Life = 0.60f;

        Image _glow;
        Image _cross;
        Image[] _motes;
        Vector2[] _from;
        Vector2[] _to;
        float[] _spin;
        float[] _delay;
        Text _tag;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01)
        {
            if (parent == null) return;
            var go = new GameObject("vfx再生", typeof(RectTransform), typeof(VfxRegen));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxRegen>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build();
        }

        void Build()
        {
            _glow = Child(transform, "glow", UiSprites.Soft(), GlowCol, 96f);

            _cross = Child(transform, "cross", UiSprites.Plus(), Ink, 52f);

            const int n = 6;
            _motes = new Image[n];
            _from = new Vector2[n];
            _to = new Vector2[n];
            _spin = new float[n];
            _delay = new float[n];
            for (int i = 0; i < n; i++)
            {
                var x = Random.Range(-32f, 32f);
                var y = Random.Range(-8f, 12f);
                _from[i] = new Vector2(x, y);
                _to[i] = new Vector2(x + Random.Range(-8f, 8f), y + Random.Range(52f, 92f));
                _spin[i] = Random.Range(-40f, 40f);
                _delay[i] = i * 0.028f;
                var sz = Random.Range(14f, 22f);
                var img = Child(transform, "mote" + i, UiSprites.Plus(), Ink, sz);
                if (img == null) continue;
                img.rectTransform.anchoredPosition = _from[i];
                img.canvasRenderer.cullTransparentMesh = false;
                img.color = Color.clear;
                _motes[i] = img;
            }

            _tag = MkText(transform, "Regen", 22, new Vector2(0f, 22f), new Vector2(160f, 36f), Ink);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var rise = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.50f) / 0.50f);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.50f, 1.28f, rise);
                var c = GlowCol;
                c.a = 0.42f * fade;
                _glow.color = c;
            }

            var crossScale = Mathf.Lerp(0.55f, 1.12f, rise);
            if (_cross != null)
            {
                _cross.transform.localScale = Vector3.one * crossScale;
                var c = Ink;
                c.a = fade;
                _cross.color = c;
            }

            if (_motes != null && _from != null && _to != null && _spin != null && _delay != null)
            {
                var n = Mathf.Min(_motes.Length, Mathf.Min(_from.Length, Mathf.Min(_to.Length, Mathf.Min(_spin.Length, _delay.Length))));
                for (int i = 0; i < n; i++)
                {
                    var img = _motes[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - _delay[i]) / 0.78f);
                    var ease = 1f - (1f - t) * (1f - t);
                    img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(_from[i], _to[i], ease);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _spin[i] * ease);
                    var a = t < 0.12f ? t / 0.12f : 1f - Mathf.Clamp01((t - 0.48f) / 0.52f);
                    var c = Ink;
                    c.a = a;
                    img.color = c;
                    img.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.10f, 1f - t);
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 22f + 14f * rise);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            Paint(img, sprite, color);
            return img;
        }

        static void Paint(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
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
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
            return tx;
        }
    }
}
