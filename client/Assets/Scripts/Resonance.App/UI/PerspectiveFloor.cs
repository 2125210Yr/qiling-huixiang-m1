using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 透视棋盘地暖:#101012/#2A2934 暗格,整体极轻暖呼吸。不碰立绘,不再是冰面。
    /// </summary>
    public sealed class PerspectiveFloor : MonoBehaviour
    {
        Image _img;
        float _phase;

        void Awake()
        {
            _phase = (GetInstanceID() & 255) * 0.037f;
            Bind();
        }

        void Start()
        {
            Bind();
        }

        void Bind()
        {
            if (_img == null) _img = GetComponent<Image>();
            if (_img == null) return;
            _img.material = null;
            UiSprites.Apply(_img, UiSprites.FloorChecker());
            _img.type = Image.Type.Simple;
            _img.preserveAspect = false;
            _img.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (_img == null) return;
            // 格子色不动,只在白与极浅暖之间缓慢浮动,像余烬烤着地板。
            var w = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.23f + _phase);
            _img.color = Color.Lerp(new Color(1f, 0.94f, 0.86f), Color.white, 0.45f + 0.55f * w);
        }
    }
}
