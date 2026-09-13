using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Enemy drive warning: veil + slash + speed lines.
    /// Primary EN <c>WARNING!!</c> + <c>ENEMY DRIVE SKILL</c> (Ragna/contrast; ordinary inventory OK).
    /// </summary>
    public sealed class VfxWarning : MonoBehaviour
    {
        const float Life = 1.00f;
        const int EdgeN = 6;

        static readonly Color VeilCol = new Color(0.55f, 0.02f, 0.05f, 0.30f);
        static readonly Color WipeCol = new Color(0.90f, 0.10f, 0.12f, 0.55f);
        static readonly Color BandCol = new Color(0.85f, 0.08f, 0.12f, 0.95f);
        static readonly Color FlashCol = new Color(1f, 0.86f, 0.86f, 0.60f);
        static readonly Color EdgeCol = new Color(1f, 0.80f, 0.82f, 0.55f);

        Image _veil;
        Image _wipe;
        Image _flashA;
        Image _flashB;
        Image _band;
        Image _band2;
        Image[] _edge;
        float[] _edgeDir;
        Text _title;
        Text _sub;
        Text _skill;
        float _age;

        public static void Play(Transform parent, string enemySkillName)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxWarning>(true);
            for (int i = 0; i < live.Length; i++)
            {
                var fx = live[i];
                if (fx == null || fx.transform == parent) continue;
                fx.gameObject.SetActive(false);
                Object.Destroy(fx.gameObject);
            }

            var go = new GameObject("vfxWarn", typeof(RectTransform), typeof(VfxWarning));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxWarning>().Build(enemySkillName);
        }

        void Build(string enemySkillName)
        {
            _veil = Full("veil", VeilCol);

            // 细斩击扫过，取代满屏红场。
            _wipe = Img("wipe", UiSprites.Slash(), Color.clear, new Vector2(0.50f, 0.56f), new Vector2(1560f, 78f));
            _wipe.type = Image.Type.Filled;
            _wipe.fillMethod = Image.FillMethod.Horizontal;
            _wipe.fillOrigin = 0;
            _wipe.fillAmount = 0f;
            _wipe.color = WipeCol;
            _wipe.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);

            // 闪光改为两条细斩线，不再用 920×920 软圆盘。
            _flashA = Img("flashA", UiSprites.Slash(), FlashCol, new Vector2(0.50f, 0.56f), new Vector2(1180f, 30f));
            _flashA.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
            _flashB = Img("flashB", UiSprites.Slash(), new Color(1f, 0.72f, 0.72f, 0.45f),
                new Vector2(0.50f, 0.52f), new Vector2(880f, 18f));
            _flashB.rectTransform.localEulerAngles = new Vector3(0f, 0f, 14f);

            _band = Img("band", UiSprites.Slash(), BandCol, new Vector2(0.50f, 0.56f), new Vector2(1480f, 210f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
            _band2 = Img("band2", UiSprites.Slash(), new Color(0.42f, 0.01f, 0.04f, 0.88f),
                new Vector2(0.50f, 0.56f), new Vector2(1180f, 150f));
            _band2.rectTransform.localEulerAngles = new Vector3(0f, 0f, 14f);
            _band2.transform.localScale = new Vector3(0.36f, 1f, 1f);

            // 边缘速度线：左右细线向场内冲入，不用满屏光斑。
            _edge = new Image[EdgeN];
            _edgeDir = new float[EdgeN];
            for (int i = 0; i < EdgeN; i++)
            {
                var left = (i & 1) == 0;
                var y = 0.10f + 0.80f * (i / (float)(EdgeN - 1));
                var e = Img("edge" + i, UiSprites.Slash(), EdgeCol,
                    new Vector2(left ? 0.02f : 0.98f, y),
                    new Vector2(240f + 70f * (i % 3), 5f + 2f * (i % 2)));
                e.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);
                _edge[i] = e;
                _edgeDir[i] = left ? -1f : 1f;
            }

            _title = MkText("title", BattleCueCopy.EnemyWarn, 92, Color.white,
                new Vector2(0.50f, 0.58f), new Vector2(720f, 140f));
            _title.transform.localScale = Vector3.one * 1.85f;

            _sub = MkText("sub", BattleCueCopy.EnemyWarnSub, 38, VisualTokens.TapWhite,
                new Vector2(0.50f, 0.48f), new Vector2(640f, 56f));

            var skill = enemySkillName ?? "";
            if (skill.Length > 0)
            {
                _skill = MkText("skill", skill, 32, VisualTokens.YellowValue,
                    new Vector2(0.50f, 0.41f), new Vector2(720f, 48f));
            }

            CanvasShake.Punch(18f, 0.24f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Life);
            var a = u < 0.10f ? u / 0.10f
                : u < 0.78f ? 1f
                : 1f - (u - 0.78f) / 0.22f;
            a = Mathf.Clamp01(a);

            var pulse = 0.92f + 0.08f * Mathf.Sin(_age * 26f);
            SetA(_veil, VeilCol.a * a * pulse);
            SetA(_wipe, WipeCol.a * a);
            if (_wipe != null)
                _wipe.fillAmount = u < 0.16f ? u / 0.16f : 1f;

            var flashPeak = 1f - Mathf.Abs(Mathf.Clamp01(_age / 0.18f) - 0.35f) / 0.35f;
            flashPeak = Mathf.Clamp01(flashPeak);
            SetA(_flashA, FlashCol.a * flashPeak * a);
            if (_flashA != null)
                _flashA.transform.localScale = new Vector3(Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01(_age / 0.22f)), 1f, 1f);
            SetA(_flashB, 0.45f * flashPeak * a);
            if (_flashB != null)
                _flashB.transform.localScale = new Vector3(Mathf.Lerp(0.40f, 1.30f, Mathf.Clamp01(_age / 0.20f)), 1f, 1f);

            if (_edge != null)
            {
                var rush = Mathf.Clamp01(_age / 0.22f);
                var rushE = 1f - (1f - rush) * (1f - rush);
                for (int i = 0; i < _edge.Length; i++)
                {
                    var e = _edge[i];
                    if (e == null) continue;
                    var pos = e.rectTransform.anchoredPosition;
                    pos.x = _edgeDir[i] * Mathf.Lerp(420f, -30f, rushE);
                    e.rectTransform.anchoredPosition = pos;
                    SetA(e, EdgeCol.a * a * (0.55f + 0.45f * Mathf.Sin(_age * 34f + i * 1.7f)));
                }
            }

            SetA(_band, BandCol.a * a);
            SetA(_band2, 0.88f * a);
            if (_band != null)
            {
                var bx = Mathf.Lerp(0.42f, 1.08f, Mathf.Clamp01(_age / 0.16f));
                _band.transform.localScale = new Vector3(bx, 1f, 1f);
            }
            if (_band2 != null)
            {
                var bx2 = Mathf.Lerp(0.36f, 1.02f, Mathf.Clamp01((_age - 0.03f) / 0.16f));
                _band2.transform.localScale = new Vector3(bx2, 1f, 1f);
            }

            var slam = Mathf.Clamp01(_age / 0.14f);
            var ease = 1f - (1f - slam) * (1f - slam);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * Mathf.Lerp(1.85f, 1f, ease);
                Fade(_title, a);
            }
            Fade(_sub, a);
            Fade(_skill, a);

            if (_age >= Life) Destroy(gameObject);
        }

        Image Full(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Image Img(string name, Sprite sprite, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MkText(string name, string text, int size, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = text ?? "";
            tx.color = color;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3f, -3f);
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
