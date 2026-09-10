using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Gold leader banner: 队长 + skill. ~0.9s. 细线印章框 + 斩击规线, 无填充名牌 / 无光洗.
    /// No English LEADER.
    /// </summary>
    public sealed class VfxLeaderBurst : MonoBehaviour
    {
        public const float Duration = 0.90f;
        const string TitleWord = "队长";

        static readonly Color Gold = VisualTokens.GoldTitle;
        static readonly Color Hot = VisualTokens.FeverGold;
        static readonly Color Metal = VisualTokens.GoldMetal;
        static readonly Color VeilCol = new Color(0.10f, 0.06f, 0.00f, 0.30f);
        static readonly Color WipeCol = new Color(0.82f, 0.62f, 0.08f, 0.50f);
        static readonly Color BandCol = new Color(0.82f, 0.64f, 0.05f, 0.88f);
        static readonly Color Ink = new Color(0.28f, 0.14f, 0.00f, 1f);

        Image _veil;
        Image _wipe;
        Image _frame;
        Image _band;
        Image _bandHot;
        Image _ruleT;
        Image _ruleB;
        Image _star;
        Image _star2;
        Text _titleGhost;
        Text _title;
        Text _skill;
        float _age;

        public static void Play(Transform parent, string skillName)
        {
            if (parent == null) return;

            var live = parent.GetComponentsInChildren<VfxLeaderBurst>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != null)
                    Object.Destroy(live[i].gameObject);
            }

            var go = new GameObject("vfxLeaderBurst", typeof(RectTransform), typeof(VfxLeaderBurst));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.GetComponent<VfxLeaderBurst>().Build(skillName);
        }

        void Build(string skillName)
        {
            _veil = Full("veil", VeilCol);

            // 细金斩扫过，取代满屏 Soft 抹场。
            _wipe = Img("wipe", UiSprites.Slash(), Color.clear, new Vector2(0.50f, 0.56f), new Vector2(1480f, 64f));
            _wipe.type = Image.Type.Filled;
            _wipe.fillMethod = Image.FillMethod.Horizontal;
            _wipe.fillOrigin = 0;
            _wipe.fillAmount = 0f;
            _wipe.color = WipeCol;
            _wipe.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);

            // 印章式细线框（透明内芯），取代 920×280 Soft 光洗。
            _frame = Img("frame", UiSprites.WireFrame(), new Color(Metal.r, Metal.g, Metal.b, 0.85f),
                new Vector2(0.50f, 0.53f), new Vector2(600f, 236f));
            _frame.transform.localScale = Vector3.one * 0.62f;

            _band = Img("band", UiSprites.Slash(), BandCol, new Vector2(0.50f, 0.56f),
                new Vector2(1280f, 168f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
            _band.transform.localScale = new Vector3(0.38f, 1f, 1f);

            _bandHot = Img("bandHot", UiSprites.Slash(), new Color(Hot.r, Hot.g, Hot.b, 0.72f),
                new Vector2(0.50f, 0.55f), new Vector2(980f, 52f));
            _bandHot.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
            _bandHot.transform.localScale = new Vector3(0.22f, 1f, 1f);

            // 技能名上下细金规线，取代填充 Pill 名牌。
            _ruleT = Img("ruleT", UiSprites.Slash(), Metal, new Vector2(0.50f, 0.48f), new Vector2(460f, 6f));
            _ruleT.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            _ruleT.transform.localScale = new Vector3(0.55f, 1f, 1f);
            _ruleB = Img("ruleB", UiSprites.Slash(), Metal, new Vector2(0.50f, 0.48f), new Vector2(460f, 6f));
            _ruleB.rectTransform.anchoredPosition = new Vector2(0f, -34f);
            _ruleB.transform.localScale = new Vector3(0.55f, 1f, 1f);

            _star = Img("star", UiSprites.Star(), Gold, new Vector2(0.28f, 0.62f), new Vector2(48f, 48f));
            _star2 = Img("star2", UiSprites.Star(), Hot, new Vector2(0.72f, 0.50f), new Vector2(36f, 36f));
            _star.transform.localScale = Vector3.one * 0.35f;
            _star2.transform.localScale = Vector3.one * 0.35f;

            _titleGhost = MkText("titleGhost", TitleWord, 72, Ink,
                new Vector2(0.50f, 0.58f), new Vector2(480f, 110f));
            _titleGhost.rectTransform.anchoredPosition = new Vector2(5f, -5f);

            _title = MkText("title", TitleWord, 72, Gold,
                new Vector2(0.50f, 0.58f), new Vector2(480f, 110f));
            _title.transform.localScale = Vector3.one * 1.75f;
            _titleGhost.transform.localScale = Vector3.one * 1.75f;

            var skill = skillName ?? "";
            _skill = MkText("skill", skill, 32, Hot,
                new Vector2(0.50f, 0.48f), new Vector2(640f, 48f));
            if (skill.Length == 0)
                _skill.enabled = false;

            _skill.transform.localScale = Vector3.one * 0.55f;

            CanvasShake.Punch(8f, 0.12f);
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Duration);
            var a = u < 0.08f ? u / 0.08f
                : u < 0.78f ? 1f
                : 1f - (u - 0.78f) / 0.22f;
            a = Mathf.Clamp01(a);

            SetA(_veil, VeilCol.a * a);
            SetA(_wipe, WipeCol.a * a);
            if (_wipe != null)
                _wipe.fillAmount = u < 0.14f ? u / 0.14f : 1f;

            var frameU = Mathf.Clamp01((_age - 0.04f) / 0.18f);
            float frameS;
            if (frameU <= 0f) frameS = 0.62f;
            else if (frameU < 0.55f) frameS = Mathf.Lerp(0.62f, 1.06f, frameU / 0.55f);
            else frameS = Mathf.Lerp(1.06f, 1f, (frameU - 0.55f) / 0.45f);
            if (_frame != null)
            {
                _frame.transform.localScale = Vector3.one * frameS;
                SetA(_frame, 0.85f * a * frameU);
            }

            var bx = Mathf.Lerp(0.38f, 1.06f, Mathf.Clamp01(_age / 0.16f));
            if (_band != null)
            {
                _band.transform.localScale = new Vector3(bx, 1f, 1f);
                SetA(_band, BandCol.a * a);
            }
            if (_bandHot != null)
            {
                _bandHot.transform.localScale = new Vector3(Mathf.Lerp(0.22f, 1.04f, Mathf.Clamp01(_age / 0.14f)), 1f, 1f);
                SetA(_bandHot, 0.72f * a);
            }

            var slam = 1f - (1f - Mathf.Clamp01(_age / 0.14f)) * (1f - Mathf.Clamp01(_age / 0.14f));
            var titleS = Mathf.Lerp(1.75f, 1f, slam);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * titleS;
                Fade(_title, a);
            }
            if (_titleGhost != null)
            {
                _titleGhost.transform.localScale = Vector3.one * titleS;
                Fade(_titleGhost, a);
            }

            float ruleS;
            var ruleU = Mathf.Clamp01((_age - 0.06f) / 0.16f);
            if (ruleU <= 0f) ruleS = 0.55f;
            else if (ruleU < 0.55f) ruleS = Mathf.Lerp(0.55f, 1.12f, ruleU / 0.55f);
            else ruleS = Mathf.Lerp(1.12f, 1f, (ruleU - 0.55f) / 0.45f);
            if (_ruleT != null)
            {
                _ruleT.transform.localScale = new Vector3(ruleS, 1f, 1f);
                SetA(_ruleT, 0.90f * a * ruleU);
            }
            if (_ruleB != null)
            {
                _ruleB.transform.localScale = new Vector3(ruleS, 1f, 1f);
                SetA(_ruleB, 0.90f * a * ruleU);
            }

            var skillU = Mathf.Clamp01((_age - 0.12f) / 0.16f);
            float skillS;
            if (skillU <= 0f) skillS = 0.55f;
            else if (skillU < 0.55f) skillS = Mathf.Lerp(0.55f, 1.14f, skillU / 0.55f);
            else skillS = Mathf.Lerp(1.14f, 1f, (skillU - 0.55f) / 0.45f);
            if (_skill != null && _skill.enabled)
            {
                _skill.transform.localScale = Vector3.one * skillS;
                Fade(_skill, a * skillU);
            }

            SetA(_star, a);
            SetA(_star2, a);
            if (_star != null)
            {
                _star.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-22f, 10f, slam));
                _star.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, slam);
            }
            if (_star2 != null)
            {
                _star2.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(18f, -8f, slam));
                _star2.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.88f, slam);
            }

            if (_age >= Duration) Destroy(gameObject);
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
            UiSprites.Apply(img, UiSprites.Pixel());
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
            if (tx == null || !tx.enabled) return;
            var c = tx.color;
            c.a = Mathf.Clamp01(a);
            tx.color = c;
        }
    }
}
