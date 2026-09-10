using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 好感: rose/gold motes. Rank only scale-punches. ~0.7s.
    /// No English Affection.
    /// </summary>
    public sealed class VfxAffection : MonoBehaviour
    {
        const float Life = 0.70f;

        static readonly Color Rose = new Color(0.92f, 0.30f, 0.48f, 1f);
        static readonly Color Gold = VisualTokens.FeverGold;
        static readonly Color GlowCol = new Color(0.95f, 0.48f, 0.42f, 0.40f);

        Image _glow;
        Image _lobeL;
        Image _lobeR;
        Image _tip;
        Image[] _motes;
        Vector2[] _from;
        Vector2[] _to;
        float[] _spin;
        float[] _delay;
        Color[] _ink;
        Text _tag;
        float _punch = 1.12f;
        float _age;

        public static void Play(Transform parent, Vector2 anchor01, int rank)
        {
            if (parent == null) return;
            var go = new GameObject("vfx好感", typeof(RectTransform), typeof(VfxAffection));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(anchor01.x), Mathf.Clamp01(anchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxAffection>().Build(rank);
        }

        void Build(int rank)
        {
            _punch = 1.10f + 0.22f * Mathf.Clamp01(rank <= 6 ? rank / 5f : rank / 100f);
            transform.localScale = Vector3.one * _punch;

            _glow = Child(transform, "glow", UiSprites.Soft(), GlowCol, 108f);

            _lobeL = Child(transform, "lobeL", UiSprites.Circle(), Rose, 22f);
            _lobeL.rectTransform.anchoredPosition = new Vector2(-7f, 10f);
            _lobeR = Child(transform, "lobeR", UiSprites.Circle(), Rose, 22f);
            _lobeR.rectTransform.anchoredPosition = new Vector2(7f, 10f);
            _tip = Child(transform, "tip", UiSprites.Pixel(), Rose, 1f);
            _tip.rectTransform.sizeDelta = new Vector2(20f, 20f);
            _tip.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            _tip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            const int n = 8;
            _motes = new Image[n];
            _from = new Vector2[n];
            _to = new Vector2[n];
            _spin = new float[n];
            _delay = new float[n];
            _ink = new Color[n];
            for (int i = 0; i < n; i++)
            {
                var x = Random.Range(-36f, 36f);
                var y = Random.Range(-10f, 14f);
                _from[i] = new Vector2(x, y);
                _to[i] = new Vector2(x + Random.Range(-12f, 12f), y + Random.Range(64f, 118f));
                _spin[i] = Random.Range(-70f, 70f);
                _delay[i] = i * 0.032f;
                var gold = (i & 1) == 0;
                _ink[i] = gold ? Gold : Rose;
                var sz = gold ? Random.Range(14f, 22f) : Random.Range(12f, 20f);
                var spr = gold ? UiSprites.Star() : ((i % 3) == 1 ? UiSprites.Spark() : UiSprites.Soft());
                var img = Child(transform, "mote" + i, spr, _ink[i], sz);
                img.rectTransform.anchoredPosition = _from[i];
                img.color = Color.clear;
                _motes[i] = img;
            }

            _tag = MkText(transform, "好感", 22, new Vector2(0f, 26f), new Vector2(180f, 36f), Gold);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var rise = 1f - (1f - u) * (1f - u);
            var fade = u < 0.14f ? u / 0.14f : 1f - Mathf.Clamp01((u - 0.52f) / 0.48f);
            var slam = 1f - (1f - Mathf.Clamp01(u / 0.16f)) * (1f - Mathf.Clamp01(u / 0.16f));
            transform.localScale = Vector3.one * Mathf.Lerp(_punch, 1f, slam);

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.52f, 1.36f, rise);
                var c = GlowCol;
                c.a = 0.40f * fade;
                _glow.color = c;
            }

            var heartS = Mathf.Lerp(0.62f, 1.08f, slam);
            var bob = Mathf.Sin(_age * 11f) * 2.2f;
            SetHeart(_lobeL, new Vector2(-7f, 10f + bob * 0.35f), heartS, fade);
            SetHeart(_lobeR, new Vector2(7f, 10f + bob * 0.35f), heartS, fade);
            if (_tip != null)
            {
                _tip.rectTransform.anchoredPosition = new Vector2(0f, -2f + bob * 0.35f);
                _tip.transform.localScale = Vector3.one * heartS;
                var c = Rose;
                c.a = fade;
                _tip.color = c;
            }

            if (_motes != null)
            {
                for (int i = 0; i < _motes.Length; i++)
                {
                    var img = _motes[i];
                    if (img == null) continue;
                    var t = Mathf.Clamp01((u - _delay[i]) / 0.80f);
                    var ease = 1f - (1f - t) * (1f - t);
                    var sway = Mathf.Sin((_age + i) * 7.4f) * 6f * ease;
                    img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(_from[i], _to[i], ease) + new Vector2(sway, 0f);
                    img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _spin[i] * ease);
                    var a = t < 0.12f ? t / 0.12f : 1f - Mathf.Clamp01((t - 0.50f) / 0.50f);
                    var c = _ink[i];
                    c.a = a;
                    img.color = c;
                    img.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.14f, 1f - t);
                }
            }

            if (_tag != null)
            {
                var c = _tag.color;
                c.a = fade;
                _tag.color = c;
                ((RectTransform)_tag.transform).anchoredPosition = new Vector2(0f, 26f + 16f * rise);
                _tag.transform.localScale = Vector3.one * Mathf.Lerp(_punch, 1f, slam);
            }

            if (_age >= Life) Destroy(gameObject);
        }

        static void SetHeart(Image img, Vector2 pos, float scale, float a)
        {
            if (img == null) return;
            img.rectTransform.anchoredPosition = pos;
            img.transform.localScale = Vector3.one * scale;
            var c = Rose;
            c.a = a;
            img.color = c;
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
