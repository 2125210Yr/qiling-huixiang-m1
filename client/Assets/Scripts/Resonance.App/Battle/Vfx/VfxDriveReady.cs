using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Portrait drive-ready mark: orange Slash wings + Star,
    /// word <see cref="BattleCueCopy.DriveReadyPortraitEn"/> (primary EN burst).
    /// Persistent tray uses <see cref="BattleCueCopy.DriveReadyPortraitLine"/> (EN+CN).
    /// ~0.8s.
    /// </summary>
    public sealed class VfxDriveReady : MonoBehaviour
    {
        const float Life = 0.80f;

        static readonly Color Orange = VisualTokens.DriveOrange;
        static readonly Color Ember = VisualTokens.Ember;
        static readonly Color Hot = new Color(1f, 0.92f, 0.62f, 1f);
        static readonly Color Ink = new Color(0.28f, 0.10f, 0.00f, 1f);

        Image _glow;
        Image _ring;
        Image _star;
        Image[] _wings;
        float[] _wingRestAng;
        float[] _wingFoldAng;
        Vector2[] _wingRest;
        Vector2[] _wingFold;
        Color[] _wingCol;
        Vector2[] _wingSize;
        Text _ghost;
        Text _label;
        float _age;
        bool _burst;

        public static void Play(Transform parent, Vector2 portraitAnchor01)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxDriveReady>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.DestroyImmediate(live[i].gameObject);
            }

            var go = new GameObject("driveReady", typeof(RectTransform), typeof(VfxDriveReady));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                return;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(
                Mathf.Clamp01(portraitAnchor01.x), Mathf.Clamp01(portraitAnchor01.y));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxDriveReady>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx.Build();
        }

        void Build()
        {
            _glow = Img("glow", UiSprites.Soft(), WithA(Ember, 0.55f), 168f);

            _wings = new Image[4];
            _wingRestAng = new[] { 38f, -38f, 18f, -18f };
            _wingFoldAng = new[] { 72f, -72f, 54f, -54f };
            _wingRest = new[]
            {
                new Vector2(-58f, 10f),
                new Vector2(58f, 10f),
                new Vector2(-34f, -4f),
                new Vector2(34f, -4f)
            };
            _wingFold = new[]
            {
                new Vector2(-10f, 6f),
                new Vector2(10f, 6f),
                new Vector2(-6f, 0f),
                new Vector2(6f, 0f)
            };
            _wingSize = new[]
            {
                new Vector2(168f, 50f),
                new Vector2(168f, 50f),
                new Vector2(112f, 34f),
                new Vector2(112f, 34f)
            };
            _wingCol = new[]
            {
                Orange,
                Orange,
                Color.Lerp(Ember, Hot, 0.42f),
                Color.Lerp(Ember, Hot, 0.42f)
            };

            for (int i = 0; i < 4; i++)
            {
                var img = Img("wing" + i, UiSprites.Slash(), WithA(_wingCol[i], 0f), 0f);
                if (img == null) continue;
                img.rectTransform.sizeDelta = _wingSize[i];
                img.rectTransform.anchoredPosition = _wingFold[i];
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, _wingFoldAng[i]);
                if ((i & 1) == 1)
                    img.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                _wings[i] = img;
            }

            _ring = Img("ring", UiSprites.Circle(), WithA(Orange, 0f), 72f);
            _star = Img("star", UiSprites.Star(), WithA(Orange, 0f), 44f);
            if (_star != null)
            {
                _star.transform.localScale = Vector3.one * 0.35f;
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, -28f);
            }

            _ghost = MkText("ghost", BattleCueCopy.DriveReadyPortraitEn, 28, Ink, new Vector2(240f, 44f), new Vector2(3f, 90f));
            _label = MkText("label", BattleCueCopy.DriveReadyPortraitEn, 28, Orange, new Vector2(240f, 44f), new Vector2(0f, 94f));
            Fade(_ghost, 0f);
            Fade(_label, 0f);
            if (_label != null) _label.transform.localScale = Vector3.one * 1.55f;
            if (_ghost != null) _ghost.transform.localScale = Vector3.one * 1.55f;

            CanvasShake.Punch(8f, 0.12f);
            Apply(0f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            if (!_burst && _age >= 0.04f)
            {
                _burst = true;
                for (int i = 0; i < 6; i++)
                    Spark.Spawn(transform, (i & 1) == 0 ? Orange : Ember,
                        (i / 6f) * Mathf.PI * 2f + 0.18f, 72f, true);
                for (int i = 0; i < 4; i++)
                    Spark.Spawn(transform, Hot, (i / 4f) * Mathf.PI * 2f, 44f, true, true);
            }

            Apply(Mathf.Clamp01(_age / Life));
        }

        void Apply(float u)
        {
            var fade = u < 0.62f ? 1f : 1f - (u - 0.62f) / 0.38f;
            fade = Mathf.Clamp01(fade);
            var spread = 1f - (1f - Mathf.Clamp01(_age / 0.16f)) * (1f - Mathf.Clamp01(_age / 0.16f));
            var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));

            if (_glow != null)
            {
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.35f, spread);
                SetA(_glow, 0.52f * fade);
            }

            if (_wings != null && _wingFold != null && _wingRest != null
                && _wingFoldAng != null && _wingRestAng != null && _wingCol != null)
            {
                for (int i = 0; i < _wings.Length; i++)
                {
                    var img = _wings[i];
                    if (img == null) continue;
                    if (i >= _wingFold.Length || i >= _wingRest.Length
                        || i >= _wingFoldAng.Length || i >= _wingRestAng.Length || i >= _wingCol.Length)
                        continue;
                    var rt = img.rectTransform;
                    rt.anchoredPosition = Vector2.LerpUnclamped(_wingFold[i], _wingRest[i], spread);
                    rt.localEulerAngles = new Vector3(0f, 0f,
                        Mathf.Lerp(_wingFoldAng[i], _wingRestAng[i], spread));
                    var sx = (i & 1) == 1 ? -1f : 1f;
                    var sy = Mathf.Lerp(1.18f, 0.92f, u);
                    rt.localScale = new Vector3(sx * Mathf.Lerp(0.35f, 1.05f, spread), sy, 1f);
                    var c = _wingCol[i];
                    c.a = (i < 2 ? 0.95f : 0.72f) * fade;
                    img.color = c;
                }
            }

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, 2.10f, 1f - (1f - u) * (1f - u));
                SetA(_ring, 0.70f * (1f - u) * fade);
            }

            if (_star != null)
            {
                float s;
                if (slam < 1f) s = Mathf.Lerp(0.35f, 1.22f, slam);
                else s = Mathf.Lerp(1.22f, 1f, Mathf.Clamp01((_age - 0.14f) / 0.16f));
                _star.transform.localScale = Vector3.one * s;
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-28f, 10f, slam));
                var sc = Color.Lerp(Orange, Hot, slam);
                sc.a = fade;
                _star.color = sc;
            }

            var wordS = Mathf.Lerp(1.55f, 1f, slam);
            var wordY = 94f + 10f * u;
            if (_label != null)
            {
                _label.text = BattleCueCopy.DriveReadyPortraitEn;
                _label.transform.localScale = Vector3.one * wordS;
                _label.rectTransform.anchoredPosition = new Vector2(0f, wordY);
                Fade(_label, fade);
            }
            if (_ghost != null)
            {
                _ghost.text = BattleCueCopy.DriveReadyPortraitEn;
                _ghost.transform.localScale = Vector3.one * wordS;
                _ghost.rectTransform.anchoredPosition = new Vector2(3f, wordY - 4f);
                Fade(_ghost, fade);
            }
        }

        Image Img(string name, Sprite sprite, Color color, float size)
        {
            if (transform == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            if (rt == null || img == null)
            {
                Object.Destroy(go);
                return null;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 dim, Vector2 pos)
        {
            if (transform == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var tx = go.GetComponent<Text>();
            if (rt == null || tx == null)
            {
                Object.Destroy(go);
                return null;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = text ?? "";
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2f, -2f);
            }
            return tx;
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

        static void Fade(Text tx, float a)
        {
            if (tx == null) return;
            var c = tx.color;
            c.a = Mathf.Clamp01(a);
            tx.color = c;
        }
    }
}
