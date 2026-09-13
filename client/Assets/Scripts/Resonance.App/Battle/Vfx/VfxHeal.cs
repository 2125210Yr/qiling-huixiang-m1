using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Heal/regen: green cross motes rising from an ally.
    /// Optional floating +N. Primary Robin ~t25 tag <c>Recovery</c>.
    /// </summary>
    public sealed class VfxHeal : MonoBehaviour
    {
        static readonly Color Heal = new Color(0.45f, 1f, 0.55f, 1f);
        static readonly Color Glow = new Color(0.32f, 0.92f, 0.48f, 0.42f);

        Image _glow;
        Image[] _motes;
        Vector2[] _from;
        Vector2[] _to;
        float[] _spin;
        float[] _delay;
        Text _tag;
        Text _amount;
        float _life = 0.90f;
        float _age;

        public static void Play(Transform parent, Vector2 anchor, int amount)
        {
            if (parent == null) return;
            var go = new GameObject("vfxHeal", typeof(RectTransform), typeof(VfxHeal));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxHeal>();
            if (fx == null)
            {
                Object.DestroyImmediate(go);
                return;
            }
            fx.Build(amount);
        }

        void Build(int amount)
        {
            _glow = Child(transform, "glow", UiSprites.Soft(), Glow, 110f);

            const int n = 8;
            _motes = new Image[n];
            _from = new Vector2[n];
            _to = new Vector2[n];
            _spin = new float[n];
            _delay = new float[n];
            for (int i = 0; i < n; i++)
            {
                var x = Random.Range(-38f, 38f);
                var y = Random.Range(-10f, 16f);
                _from[i] = new Vector2(x, y);
                _to[i] = new Vector2(x + Random.Range(-10f, 10f), y + Random.Range(78f, 132f));
                _spin[i] = Random.Range(-50f, 50f);
                _delay[i] = i * 0.035f;
                var sz = Random.Range(16f, 28f);
                var img = Child(transform, "mote" + i, CrossSprite(), Heal, sz);
                if (img == null) continue;
                img.rectTransform.anchoredPosition = _from[i];
                img.canvasRenderer.cullTransparentMesh = false;
                img.color = Color.clear;
                _motes[i] = img;
            }

            _tag = MkText(transform, "Recovery", 22, new Vector2(0f, 24f), new Vector2(180f, 36f), Heal);
            if (amount > 0)
                _amount = MkText(transform, "+" + amount, 36, new Vector2(0f, 56f), new Vector2(220f, 56f), Heal);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / _life);
            var rise = 1f - (1f - u) * (1f - u);
            var fade = u < 0.16f ? u / 0.16f : 1f - Mathf.Clamp01((u - 0.58f) / 0.42f);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.40f, rise);
                var c = Glow;
                c.a = 0.42f * fade;
                _glow.color = c;
            }

            if (_motes != null && _from != null && _to != null && _spin != null && _delay != null)
            {
                var n = Mathf.Min(_motes.Length, Mathf.Min(_from.Length, Mathf.Min(_to.Length, Mathf.Min(_spin.Length, _delay.Length))));
                for (int i = 0; i < n; i++)
                {
                    var img = _motes[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - _delay[i]) / 0.82f);
                    var ease = 1f - (1f - t) * (1f - t);
                    img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(_from[i], _to[i], ease);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _spin[i] * ease);
                    var a = t < 0.12f ? t / 0.12f : 1f - Mathf.Clamp01((t - 0.52f) / 0.48f);
                    var c = Heal;
                    c.a = a;
                    img.color = c;
                    img.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.12f, 1f - t);
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 24f + 16f * rise);
            }

            if (_amount != null)
            {
                var c = _amount.color;
                c.a = fade;
                _amount.color = c;
                ((RectTransform)_amount.transform).anchoredPosition = new Vector2(0f, 56f + 52f * rise);
                _amount.transform.localScale = Vector3.one * Mathf.Lerp(1.22f, 0.94f, u);
            }

            if (_age >= _life) Destroy(gameObject);
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
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (color.a <= 0f) img.canvasRenderer.cullTransparentMesh = false;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Sprite _crossSprite;

        /// <summary>Smooth anti-aliased medical cross; replaces the point-filtered pixel plus.</summary>
        static Sprite CrossSprite()
        {
            if (_crossSprite != null) return _crossSprite;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            var arm = s * 0.16f;
            var len = s * 0.46f;
            var rnd = s * 0.055f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = Mathf.Abs(x - c);
                    var dy = Mathf.Abs(y - c);
                    var sd = Mathf.Min(RboxSd(dx, dy, arm, len, rnd), RboxSd(dx, dy, len, arm, rnd));
                    var a = Mathf.Clamp01(0.6f - sd);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _crossSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _crossSprite;
        }

        static float RboxSd(float px, float py, float hx, float hy, float r)
        {
            var qx = px - (hx - r);
            var qy = py - (hy - r);
            var ax = Mathf.Max(qx, 0f);
            var ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
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
