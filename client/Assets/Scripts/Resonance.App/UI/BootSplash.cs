using System;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 开机：虚空上金标题「契灵回响」。停两秒后 onDone。不用 Unity 闪屏，不画原作标。
    /// </summary>
    public static class BootSplash
    {
        const float HoldSeconds = 2f;

        public static void Draw(Transform parent, Action onDone)
        {
            if (parent == null) return;
            var root = new GameObject("BootSplash", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            var voidGo = new GameObject("void", typeof(RectTransform), typeof(Image));
            voidGo.transform.SetParent(root.transform, false);
            Stretch(voidGo.GetComponent<RectTransform>());
            var voidImg = voidGo.GetComponent<Image>();
            voidImg.sprite = UiSprites.Pixel();
            voidImg.type = Image.Type.Simple;
            voidImg.color = VisualTokens.BgVoid;
            voidImg.raycastTarget = true;
            voidImg.canvasRenderer.cullTransparentMesh = false;

            var titleGo = new GameObject("title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(root.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(1000f, 160f);
            titleRt.anchoredPosition = Vector2.zero;
            var tx = titleGo.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.fontSize = 64;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = VisualTokens.GoldTitle;
            tx.text = "契灵回响";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            var ol = titleGo.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3f, -3f);

            root.AddComponent<Hold>().Arm(HoldSeconds, onDone);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        sealed class Hold : MonoBehaviour
        {
            float _left;
            Action _done;
            bool _fired;

            public void Arm(float seconds, Action done)
            {
                _left = seconds;
                _done = done;
            }

            void Update()
            {
                if (_fired) return;
                _left -= Time.unscaledDeltaTime;
                if (_left > 0f) return;
                _fired = true;
                var cb = _done;
                _done = null;
                if (cb != null) cb();
            }
        }
    }
}
