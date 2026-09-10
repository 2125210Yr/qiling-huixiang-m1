using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// DC-style free zoom on a still: wheel / pinch to scale, drag to pan.
    /// </summary>
    public sealed class StandeeZoom : MonoBehaviour, IScrollHandler, IDragHandler, IBeginDragHandler,
        IPointerClickHandler
    {
        const float Min = 1f;
        const float Max = 3.4f;
        RectTransform _target;
        RectTransform _view;
        float _scale = 1f;
        Vector2 _pinchPrev;
        bool _pinching;
        float _idleAt;
        bool _idleMotion = true;

        public void Bind(RectTransform target, bool idleMotion = true)
        {
            _target = target;
            _view = (RectTransform)transform;
            _idleMotion = idleMotion;
            if (!idleMotion)
            {
                _idleAt = float.PositiveInfinity;
                if (_target != null)
                {
                    _target.localScale = Vector3.one;
                    _target.anchoredPosition = Vector2.zero;
                }
            }
        }

        public void OnScroll(PointerEventData e)
        {
            if (e == null || _target == null) return;
            var dy = e.scrollDelta.y;
            var step = Mathf.Abs(dy) > 8f ? dy / 400f : dy * 0.10f;
            Busy();
            ZoomAt(e.position, 1f + step);
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (e == null || _target == null || _scale <= 1.02f) return;
            if (Input.touchCount >= 2) return;
            Busy();
            _target.anchoredPosition += e.delta;
            ClampPan();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e != null && e.clickCount >= 2) ResetZoom();
        }

        void LateUpdate()
        {
            if (_target == null) return;
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0).position;
                var b = Input.GetTouch(1).position;
                var now = b - a;
                var mid = (a + b) * 0.5f;
                if (_pinching && _pinchPrev.sqrMagnitude > 1f)
                    ZoomAt(mid, now.magnitude / _pinchPrev.magnitude);
                _pinchPrev = now;
                _pinching = true;
            }
            else
                _pinching = false;
            ApplyIdle();
        }

        void ApplyIdle()
        {
            if (_target == null || _view == null) return;
            if (!_idleMotion) return;
            if (_pinching || _scale > Min + 0.015f) return;
            if (Time.unscaledTime < _idleAt) return;
            var t = Time.unscaledTime;
            var push = 1f + 0.058f * (0.5f + 0.5f * Mathf.Sin(t * 0.41f));
            var view = _view.rect.size;
            var pan = new Vector2(
                Mathf.Sin(t * 0.27f) * view.x * 0.026f,
                Mathf.Sin(t * 0.23f + 0.85f) * view.y * 0.020f + view.y * 0.006f);
            _target.localScale = Vector3.one * push;
            _target.anchoredPosition = pan;
        }

        void Busy()
        {
            _idleAt = Time.unscaledTime + 0.45f;
        }

        void ZoomAt(Vector2 screen, float factor)
        {
            if (_target == null || _view == null) return;
            Busy();
            var next = Mathf.Clamp(_scale * factor, Min, Max);
            if (Mathf.Abs(next - _scale) < 0.0005f)
            {
                if (next <= Min + 0.001f) ResetZoom();
                return;
            }

            Camera cam = null;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector2 cursor;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view, screen, cam, out cursor);
            var from = cursor - _target.anchoredPosition;
            var k = next / _scale;
            _scale = next;
            _target.localScale = Vector3.one * _scale;
            _target.anchoredPosition = cursor - from * k;
            if (_scale <= Min + 0.001f) ResetZoom();
            else ClampPan();
        }

        void ClampPan()
        {
            if (_target == null || _view == null) return;
            var view = _view.rect.size;
            var size = _target.rect.size * _scale;
            var maxX = Mathf.Max(0f, (size.x - view.x * 0.35f) * 0.5f);
            var maxY = Mathf.Max(0f, (size.y - view.y * 0.35f) * 0.5f);
            var p = _target.anchoredPosition;
            p.x = Mathf.Clamp(p.x, -maxX, maxX);
            p.y = Mathf.Clamp(p.y, -maxY, maxY);
            _target.anchoredPosition = p;
        }

        void ResetZoom()
        {
            _scale = 1f;
            Busy();
            if (_target == null) return;
            _target.localScale = Vector3.one;
            _target.anchoredPosition = Vector2.zero;
        }
    }
}
