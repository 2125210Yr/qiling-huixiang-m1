using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// M1-G2-DEV-OVERLAY：开发层标明 region=GL、当前 formula profile、UNKNOWN。
    /// 自举独立画布，不写入 BattleHud / GameRoot，不画进正式目标 HUD 美术。
    /// 不宣称 T26 / 验收。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class M1DevOverlay : MonoBehaviour
    {
        public const string RegionTag = "GL";
        public const string UnknownTag = "UNKNOWN";

        static readonly Color Ink = new Color(0.04f, 0.07f, 0.03f, 0.78f);
        static readonly Color Lime = new Color(0.55f, 1f, 0.38f, 1f);

        Text _tx;
        string _last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!WantOverlay()) return;
            if (FindFirstObjectByType<M1DevOverlay>() != null) return;
            var go = new GameObject("M1DevOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<M1DevOverlay>();
        }

        static bool WantOverlay()
        {
            return Debug.isDebugBuild;
        }

        void Awake()
        {
            if (!WantOverlay())
            {
                Destroy(gameObject);
                return;
            }
            Build();
        }

        void LateUpdate()
        {
            if (_tx == null) return;
            var line = Compose();
            if (line == _last) return;
            _last = line;
            _tx.text = line;
        }

        public static string Compose()
        {
            var profile = FormulaProfile.GL_UNKNOWN;
            var g = GameRoot.Live;
            if (g != null && g.Battle != null)
                profile = g.Battle.Profile;
            return "DEV  region=" + RegionTag + "  profile=" + profile + "  " + UnknownTag;
        }

        void Build()
        {
            var canvasGo = new GameObject("M1DevOverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = new GameObject("devBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(canvasGo.transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, 32f);
            var barImg = bar.GetComponent<Image>();
            UiSprites.Apply(barImg, UiSprites.Pixel());
            barImg.color = Ink;
            barImg.raycastTarget = false;

            var label = new GameObject("devLine", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(bar.transform, false);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(12f, 0f);
            labelRt.offsetMax = new Vector2(-12f, 0f);
            _tx = label.GetComponent<Text>();
            var font = CharacterPresenter.UiFont();
            if (font != null) _tx.font = font;
            _tx.fontSize = 12;
            _tx.color = Lime;
            _tx.alignment = TextAnchor.MiddleLeft;
            _tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tx.verticalOverflow = VerticalWrapMode.Truncate;
            _tx.raycastTarget = false;
            _tx.text = Compose();
            _last = _tx.text;
        }
    }
}
