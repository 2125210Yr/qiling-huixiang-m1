using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Additive ice rim / drifting lamp on the still. Does not move pixels.
    /// </summary>
    public sealed class StillLight : MonoBehaviour
    {
        Material _mat;

        public static void Attach(RawImage still)
        {
            if (still == null || still.texture == null) return;
            if (still.transform.Find("stillLight") != null) return;
            var go = new GameObject("stillLight", typeof(RectTransform), typeof(RawImage), typeof(StillLight));
            go.transform.SetParent(still.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.texture = still.texture;
            img.color = Color.white;
            img.raycastTarget = false;
            go.GetComponent<StillLight>().Bind(img);
        }

        void Bind(RawImage img)
        {
            var sh = Resources.Load<Shader>("Shaders/StillLight");
            if (sh == null)
                sh = Shader.Find("Resonance/UI/StillLight");
            if (sh == null) return;
            _mat = new Material(sh);
            img.material = _mat;
        }

        void LateUpdate()
        {
            if (_mat == null) return;
            _mat.SetFloat("_TimeU", Time.unscaledTime);
            _mat.SetFloat("_Gain", 0.92f + 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.85f)));
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
