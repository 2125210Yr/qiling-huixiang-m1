using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Drive field crush only. QTE 完美/优秀 is VfxJudge.
    /// Not SHOWTIME, not Fever banner. 150% is not a PVE number.
    /// </summary>
    public sealed class VfxDriveCrush : MonoBehaviour
    {
        const float ReadyLen = 0.38f;
        const float CrushAt = 0.28f;
        const float CrushLen = 0.50f;

        Image _flash;
        Image _wipe;
        Image _core;
        Image _ring;
        Image _ring2;
        Image[] _blades;
        Text _ready;
        Text _crush;
        Color _color;
        bool _perfect;
        float _age;
        bool _didCrush;

        public static void Play(Transform parent, Vector2 center, Color color, bool perfect)
        {
            if (parent == null) return;
            if (color.a < 0.05f) color.a = 1f;
            center = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));

            var live = parent.GetComponentsInChildren<VfxDriveCrush>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.DestroyImmediate(live[i].gameObject);
            }

            var go = new GameObject("driveCrush", typeof(RectTransform), typeof(VfxDriveCrush));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Object.Destroy(go);
                return;
            }
            rt.anchorMin = rt.anchorMax = center;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
            var fx = go.GetComponent<VfxDriveCrush>();
            if (fx == null)
            {
                Object.Destroy(go);
                return;
            }
            fx.Build(color, perfect);
        }

        void Build(Color color, bool perfect)
        {
            _color = color;
            _perfect = perfect;

            _flash = Img("flash", UiSprites.Soft(), new Color(color.r, color.g, color.b, 0f), 180f);
            _wipe = Img("wipe", UiSprites.Slash(), new Color(color.r, color.g, color.b, 0f), 0f);
            if (_wipe != null)
            {
                _wipe.rectTransform.sizeDelta = new Vector2(680f, 86f);
                _wipe.type = Image.Type.Filled;
                _wipe.fillMethod = Image.FillMethod.Horizontal;
                _wipe.fillOrigin = 0;
                _wipe.fillAmount = 0f;
            }
            _core = Img("core", UiSprites.Soft(), new Color(1f, 1f, 1f, 0f), 96f);
            _ring = Img("ring", UiSprites.Slash(), new Color(1f, 1f, 1f, 0f), 0f);
            _ring2 = Img("ring2", UiSprites.Slash(), new Color(color.r, color.g, color.b, 0f), 0f);
            if (_ring != null) _ring.rectTransform.sizeDelta = new Vector2(280f, 18f);
            if (_ring2 != null) _ring2.rectTransform.sizeDelta = new Vector2(220f, 12f);

            _blades = new Image[3];
            var angs = new[] { -26f, 18f, 58f };
            for (int i = 0; i < 3; i++)
            {
                var blade = Img("blade" + i, UiSprites.Slash(), new Color(color.r, color.g, color.b, 0f), 0f);
                if (blade == null) continue;
                blade.rectTransform.sizeDelta = new Vector2(520f, perfect ? 78f : 56f);
                blade.rectTransform.localEulerAngles = new Vector3(0f, 0f, angs[i]);
                _blades[i] = blade;
            }

            _ready = MkText("ready", "准备", 64, Color.white, new Vector2(520f, 96f));
            _crush = MkText("crush", "碾压", 78, color, new Vector2(560f, 110f));
            Fade(_crush, 0f);
            if (_crush != null) _crush.transform.localScale = Vector3.one * 2.2f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            TickReady();
            TickCrush();
            if (_age >= CrushAt + CrushLen) Destroy(gameObject);
        }

        void TickReady()
        {
            var u = Mathf.Clamp01(_age / ReadyLen);
            var peak = 1f - Mathf.Abs(u - 0.18f) / 0.18f;
            peak = Mathf.Clamp01(peak);
            if (_flash != null)
            {
                SetA(_flash, 0.28f * peak);
                _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.70f, 1.12f, u);
            }

            if (_wipe != null)
            {
                _wipe.fillAmount = u < 0.22f ? u / 0.22f : 1f;
                SetA(_wipe, u < 0.72f ? 0.92f : 0.92f * (1f - (u - 0.72f) / 0.28f));
            }

            var slam = Mathf.Clamp01(_age / 0.16f);
            if (_ready != null)
            {
                _ready.text = "准备";
                _ready.transform.localScale = Vector3.one * Mathf.Lerp(1.55f, 1f, 1f - (1f - slam) * (1f - slam));
                Fade(_ready, u < 0.62f ? 1f : 1f - (u - 0.62f) / 0.38f);
            }
        }

        void TickCrush()
        {
            if (_age < CrushAt)
            {
                SetA(_core, 0f);
                SetA(_ring, 0f);
                SetA(_ring2, 0f);
                return;
            }

            if (!_didCrush)
            {
                _didCrush = true;
                CanvasShake.Punch(_perfect ? 28f : 18f, _perfect ? 0.34f : 0.24f);
                var n = _perfect ? 16 : 10;
                for (int i = 0; i < n; i++)
                {
                    var ang = (i / (float)n) * Mathf.PI * 2f;
                    Spark.Spawn(transform, _color, ang, _perfect ? 168f : 112f, false);
                }
                for (int i = 0; i < 6; i++)
                    Spark.Spawn(transform, Color.white, (i / 6f) * Mathf.PI * 2f + 0.21f, 90f, false);
            }

            var u = Mathf.Clamp01((_age - CrushAt) / CrushLen);
            var a = 1f - u * u;
            if (_core != null)
            {
                SetA(_core, 0.95f * a);
                _core.transform.localScale = Vector3.one * Mathf.Lerp(0.40f, _perfect ? 1.25f : 1.10f, 1f - (1f - u) * (1f - u));
            }
            if (_ring != null)
            {
                SetA(_ring, 0.80f * a);
                _ring.transform.localScale = new Vector3(Mathf.Lerp(0.55f, 1.15f, u), 1f, 1f);
            }
            if (_ring2 != null)
            {
                SetA(_ring2, 0.55f * a);
                _ring2.transform.localScale = new Vector3(Mathf.Lerp(0.65f, 1.20f, Mathf.Clamp01(u * 1.15f)), 1f, 1f);
            }

            var bx = Mathf.Lerp(0.45f, 1.18f, u);
            var by = Mathf.Lerp(1.20f, 0.22f, u);
            if (_blades != null)
            {
                for (int i = 0; i < _blades.Length; i++)
                {
                    var blade = _blades[i];
                    if (blade == null) continue;
                    blade.transform.localScale = new Vector3(bx, by, 1f);
                    SetA(blade, 0.95f * a);
                }
            }

            var slam = Mathf.Clamp01((_age - CrushAt) / 0.14f);
            if (_crush != null)
            {
                _crush.text = "碾压";
                _crush.transform.localScale = Vector3.one * Mathf.Lerp(2.15f, 1.02f, 1f - (1f - slam) * (1f - slam));
                Fade(_crush, u < 0.62f ? 1f : a);
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

        Text MkText(string name, string text, int size, Color color, Vector2 dim)
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
            rt.anchoredPosition = Vector2.zero;
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.fontSize = size;
            tx.text = text ?? "";
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(3f, -3f);
            }
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
