using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Generic loc stamp (battle-start 开战, etc.).
    /// Slide / Drive / QTE / Fever / Warn titles no-op here — those modules own the look.
    /// </summary>
    public sealed class VfxWordStamp : MonoBehaviour
    {
        Image _veil;
        Image _dots;
        Image _glow;
        Image _band;
        Image _bandInner;
        Text _ghost;
        Text _title;
        Text _sub;
        Color _ink;
        float _life = 1.1f;
        float _age;

        public static void Play(Transform parent, string title, string sub, Color ink, float life)
        {
            if (parent == null || string.IsNullOrEmpty(title)) return;
            // Distinctive cues are owned by event modules. A generic slash stamp
            // would collapse Slide / Drive / QTE / Fever into one look.
            if (OwnedByChannelCue(title)) return;

            var live = parent.GetComponentsInChildren<VfxWordStamp>(true);
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] == null || live[i].transform == parent) continue;
                Object.Destroy(live[i].gameObject);
            }

            var go = new GameObject("wordStamp", typeof(RectTransform), typeof(CanvasGroup), typeof(VfxWordStamp));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            go.GetComponent<VfxWordStamp>().Build(title, sub, ink, life);
        }

        static bool OwnedByChannelCue(string title)
        {
            return title == "上滑" || title == "SHOWTIME" || title == "IT'S SHOWTIME!!"
                || title == "SLIDE SKILL" || title == BattleCueCopy.SlideSkillEn
                || title == "驱动" || title == "就绪？" || title == "驱动就绪"
                || title == BattleCueCopy.DriveCast || title == BattleCueCopy.DriveReady
                || title == BattleCueCopy.DriveReadyPortrait || title == BattleCueCopy.DriveSelect
                || title == "完美" || title == "优秀" || title == "好" || title == "失误"
                || title == BattleCueCopy.QtePerfect || title == BattleCueCopy.QteGreat
                || title == BattleCueCopy.QteGood || title == BattleCueCopy.QteBad
                || title == "狂热时间" || title == BattleCueCopy.FeverTimeEn
                || title == "警告" || title == BattleCueCopy.EnemyWarn
                || title == "驱动选择" || title == BattleCueCopy.BattleStart
                || title == BattleCueCopy.DriveCrushEn;
        }

        void Build(string title, string sub, Color ink, float life)
        {
            _ink = ink.a > 0.05f ? ink : Color.white;
            _life = Mathf.Max(0.45f, life);

            var wash = new Color(_ink.r * 0.55f, _ink.g * 0.12f, _ink.b * 0.10f, 0.11f);
            _veil = Full("veil", wash);
            _dots = Full("dots", new Color(0.08f, 0.02f, 0.01f, 0.18f));
            UiSprites.Apply(_dots, UiSprites.Soft());

            _glow = Img("glow", UiSprites.Soft(), new Color(_ink.r, _ink.g, _ink.b, 0.20f),
                new Vector2(0.50f, 0.58f), new Vector2(920f, 420f));
            _band = Img("band", UiSprites.Slash(), new Color(_ink.r, _ink.g, _ink.b, 0.96f),
                new Vector2(0.50f, 0.58f), new Vector2(1480f, 272f));
            _band.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);
            _bandInner = Img("bandIn", UiSprites.Slash(), new Color(1f, 1f, 1f, 0.62f),
                new Vector2(0.50f, 0.58f), new Vector2(1300f, 118f));
            _bandInner.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);

            _ghost = MkText("ghost", title, 96, new Color(0.06f, 0.01f, 0.01f, 0.85f),
                new Vector2(0.50f, 0.57f), new Vector2(980f, 160f));
            _ghost.rectTransform.anchoredPosition = new Vector2(10f, -10f);
            _ghost.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);

            _title = MkText("title", title, 96, Color.white,
                new Vector2(0.50f, 0.58f), new Vector2(980f, 160f));
            _title.fontStyle = FontStyle.Bold;
            _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            var ol2 = _title.gameObject.AddComponent<Outline>();
            ol2.effectColor = Color.black;
            ol2.effectDistance = new Vector2(3.4f, -3.4f);
            var ol3 = _title.gameObject.AddComponent<Outline>();
            ol3.effectColor = new Color(_ink.r * 0.55f, _ink.g * 0.55f, _ink.b * 0.55f, 0.90f);
            ol3.effectDistance = new Vector2(1.4f, 1.4f);
            var sh = _title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(_ink.r, _ink.g, _ink.b, 0.85f);
            sh.effectDistance = new Vector2(0f, -5f);

            if (!string.IsNullOrEmpty(sub))
            {
                _sub = MkText("sub", sub, 32, VisualTokens.YellowValue,
                    new Vector2(0.50f, 0.48f), new Vector2(880f, 56f));
            }

            _glow.transform.localScale = Vector3.one * 0.55f;
            _band.transform.localScale = new Vector3(0.42f, 1.15f, 1f);
            _title.transform.localScale = Vector3.one * 1.85f;

            CanvasShake.Punch(14f, 0.20f);
            for (int i = 0; i < 10; i++)
            {
                var ang = (i / 10f) * Mathf.PI * 2f;
                Spark.Spawn(transform, Color.Lerp(_ink, Color.white, 0.35f), ang, 160f, false);
            }
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / _life);
            var a = u < 0.10f ? u / 0.10f
                : u < 0.78f ? 1f
                : 1f - (u - 0.78f) / 0.22f;
            a = Mathf.Clamp01(a);

            SetA(_veil, 0.11f * a);
            SetA(_dots, 0.12f * a);
            SetA(_glow, 0.20f * a);
            SetA(_band, 0.96f * a);
            SetA(_bandInner, 0.58f * a);

            var slam = Mathf.Clamp01(_age / 0.14f);
            var ease = 1f - (1f - slam) * (1f - slam);
            if (_band != null)
                _band.transform.localScale = new Vector3(Mathf.Lerp(0.42f, 1.08f, ease), 1f, 1f);
            if (_bandInner != null)
                _bandInner.transform.localScale = new Vector3(Mathf.Lerp(0.38f, 1.02f, ease), 1f, 1f);
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.18f, ease);
            if (_title != null)
            {
                _title.transform.localScale = Vector3.one * Mathf.Lerp(1.85f, 1f, ease);
                Fade(_title, a);
            }
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one * Mathf.Lerp(1.85f, 1f, ease);
                Fade(_ghost, a * 0.85f);
            }
            Fade(_sub, a);

            if (_age >= _life) Destroy(gameObject);
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
            ol.effectDistance = new Vector2(6.5f, -6.5f);
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
