using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Paper-doll lobby: body + hair + sword as separate images.
    /// Idle moves hair/sword only. Tap hits zones. Not Cubism.
    /// </summary>
    public sealed class LayeredPresenter : MonoBehaviour, IPointerDownHandler
    {
        string _id;
        float _phase;
        float _nextBlink = 2.4f;
        float _blinkLeft;
        float _react;
        int _zone;
        float _sayT;
        Texture _body;
        Texture _blink;
        RawImage _bodyImg;
        RectTransform _hairFront;
        RectTransform _hairBack;
        RectTransform _sword;
        RectTransform _root;
        Vector2 _basePos;
        bool _gotBase;
        Text _say;
        Image _sayBg;

        public void Bind(Texture body, Texture blink, Texture hairFront, Texture hairBack, Texture sword, string id)
        {
            if (body == null) return;
            _body = body;
            _blink = blink;
            _id = id ?? "";
            _phase = id != null ? (id.GetHashCode() & 255) * 0.11f : 0f;
            _root = (RectTransform)transform;
            _bodyImg = MakeLayer("body", body, new Vector2(0.50f, 0.00f), 0);
            if (hairBack != null)
                _hairBack = MakeLayer("hairBack", hairBack, new Vector2(0.30f, 0.72f), -1).rectTransform;
            if (sword != null)
                _sword = MakeLayer("sword", sword, new Vector2(0.40f, 0.48f), 1).rectTransform;
            if (hairFront != null)
                _hairFront = MakeLayer("hairFront", hairFront, new Vector2(0.52f, 0.80f), 2).rectTransform;
            var hit = new GameObject("hit", typeof(RectTransform), typeof(Image));
            hit.transform.SetParent(transform, false);
            Stretch(hit.GetComponent<RectTransform>());
            var img = hit.GetComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
            EnsureSay();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e == null || _root == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root, e.position, e.pressEventCamera, out local))
                return;
            var rect = _root.rect;
            if (rect.height < 1f) return;
            var u = (local.x - rect.xMin) / rect.width;
            var v = (local.y - rect.yMin) / rect.height;
            if (v > 0.72f) _zone = 1;
            else if (v > 0.50f) _zone = 2;
            else if (v > 0.30f) _zone = 3;
            else _zone = 4;
            if (u < 0.42f && v > 0.28f && v < 0.58f) _zone = 5;
            _react = 0.78f;
            _blinkLeft = 0.12f;
            _nextBlink = 2.2f + Random.Range(0f, 1.4f);
            Say(CharacterPresenter.TapLine(_id, _zone));
            ApplyBlink(true);
        }

        void Update()
        {
            if (_root == null) return;
            var dt = Time.unscaledDeltaTime;
            _nextBlink -= dt;
            if (_blinkLeft > 0f)
            {
                _blinkLeft -= dt;
                if (_blinkLeft <= 0f) ApplyBlink(false);
            }
            else if (_nextBlink <= 0f)
            {
                _blinkLeft = 0.11f;
                _nextBlink = 2.8f + Random.Range(0f, 2.4f);
                ApplyBlink(true);
            }

            if (_react > 0f) _react = Mathf.Max(0f, _react - dt);
            if (_sayT > 0f)
            {
                _sayT -= dt;
                SetSayAlpha(_sayT > 0.25f ? 1f : Mathf.Clamp01(_sayT / 0.25f));
            }

            if (!_gotBase)
            {
                _basePos = _root.anchoredPosition;
                _gotBase = true;
            }

            var t = Time.unscaledTime + _phase;
            var punch = 0f;
            if (_react > 0f)
                punch = Mathf.Sin((1f - _react / 0.78f) * Mathf.PI);

            var hairF = Mathf.Sin(t * 1.15f) * 4.4f;
            var hairB = Mathf.Sin(t * 0.78f + 0.9f) * 3.1f;
            var sword = Mathf.Sin(t * 0.92f + 0.4f) * 1.8f;
            if (_zone == 1) hairF += 14f * punch;
            if (_zone == 1) hairB += 8f * punch;
            if (_zone == 5) sword += 16f * punch;
            if (_hairFront != null) _hairFront.localEulerAngles = new Vector3(0f, 0f, hairF);
            if (_hairBack != null) _hairBack.localEulerAngles = new Vector3(0f, 0f, hairB);
            if (_sword != null) _sword.localEulerAngles = new Vector3(0f, 0f, sword);

            var bodyRot = 0f;
            var bodyPos = _basePos + new Vector2(0f, Mathf.Sin(t * 1.05f) * 2.0f);
            var bodySc = Vector3.one;
            if (_zone == 2)
            {
                bodySc = Vector3.one * (1f + 0.03f * punch);
                bodyPos.y += 8f * punch;
            }
            else if (_zone == 3)
            {
                bodyRot = 9f * punch * ((_react / 0.78f) > 0.5f ? 1f : -0.3f);
                bodyPos.x += 16f * punch;
            }
            else if (_zone == 4)
            {
                bodyPos.y += 46f * punch;
            }
            _root.localEulerAngles = new Vector3(0f, 0f, bodyRot);
            _root.anchoredPosition = bodyPos;
            _root.localScale = bodySc;
        }

        void ApplyBlink(bool on)
        {
            if (_bodyImg == null) return;
            if (on && _blink != null) _bodyImg.texture = _blink;
            else _bodyImg.texture = _body;
        }

        RawImage MakeLayer(string name, Texture tex, Vector2 pivot, int sibling)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            rt.pivot = pivot;
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = Color.white;
            img.raycastTarget = false;
            img.uvRect = new Rect(0f, 0f, 1f, 1f);
            if (sibling < 0) go.transform.SetAsFirstSibling();
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void EnsureSay()
        {
            var host = transform.parent != null ? transform.parent : transform;
            var bgGo = new GameObject("sayBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(host, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.28f, 0.72f);
            bgRt.sizeDelta = new Vector2(440f, 92f);
            _sayBg = bgGo.GetComponent<Image>();
            UiSprites.Apply(_sayBg, UiSprites.Round());
            _sayBg.color = new Color(0.05f, 0.04f, 0.04f, 0f);
            _sayBg.raycastTarget = false;
            var tGo = new GameObject("say", typeof(RectTransform), typeof(Text));
            tGo.transform.SetParent(bgGo.transform, false);
            var tRt = tGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(16f, 8f);
            tRt.offsetMax = new Vector2(-16f, -8f);
            _say = tGo.GetComponent<Text>();
            _say.font = CharacterPresenter.UiFont();
            _say.alignment = TextAnchor.MiddleCenter;
            _say.fontSize = 28;
            _say.color = new Color(1f, 1f, 1f, 0f);
            _say.raycastTarget = false;
            var ol = tGo.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2f, -2f);
        }

        void Say(string line)
        {
            if (_say == null) return;
            _say.text = line;
            _sayT = 1.85f;
            SetSayAlpha(1f);
        }

        void SetSayAlpha(float a)
        {
            if (_say != null)
            {
                var c = _say.color;
                c.a = a;
                _say.color = c;
            }
            if (_sayBg != null)
            {
                var c = _sayBg.color;
                c.a = 0.78f * a;
                _sayBg.color = c;
            }
        }
    }
}
