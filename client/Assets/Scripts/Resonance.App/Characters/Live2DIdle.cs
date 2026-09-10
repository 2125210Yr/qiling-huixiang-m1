using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Lobby presenter: blink + tiny stance sway, and tap-zone reactions.
    /// Not Cubism Live2D — one texture, no mesh-cut. Idle must not stretch the body.
    /// </summary>
    public sealed class Live2DIdle : MaskableGraphic, IPointerDownHandler
    {
        Texture _idle;
        Texture _blink;
        string _id;
        float _phase;
        float _nextBlink = 2.6f;
        float _blinkLeft;
        float _react;
        int _zone;
        float _sayT;
        Text _say;
        Image _sayBg;
        Image _spark;
        Vector2 _sparkAt;
        Vector2 _basePos;
        bool _gotBase;

        public void Bind(Texture idle, Texture blink, float phase, string id, bool tappable)
        {
            if (idle == null && blink == null) return;
            _idle = idle;
            _blink = blink;
            _phase = phase;
            _id = id ?? "";
            _nextBlink = 2.1f + Mathf.Abs(phase) * 0.4f;
            raycastTarget = tappable;
            EnsureOverlay();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public override Texture mainTexture
        {
            get
            {
                if (_blinkLeft > 0f && _blink != null) return _blink;
                if (_idle != null) return _idle;
                return Texture2D.whiteTexture;
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!raycastTarget || e == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, e.position, e.pressEventCamera, out local))
                return;
            var rect = rectTransform.rect;
            if (rect.width < 1f || rect.height < 1f) return;
            var v = (local.y - rect.yMin) / rect.height;
            if (v > 0.72f) _zone = 1;
            else if (v > 0.50f) _zone = 2;
            else if (v > 0.30f) _zone = 3;
            else _zone = 4;
            _react = 0.70f;
            _blinkLeft = 0.12f;
            _nextBlink = 2.4f + Random.Range(0f, 1.6f);
            _sparkAt = local;
            Say(CharacterPresenter.TapLine(_id, _zone));
            SetMaterialDirty();
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _nextBlink -= dt;
            if (_blinkLeft > 0f)
            {
                _blinkLeft -= dt;
                if (_blinkLeft <= 0f) SetMaterialDirty();
            }
            else if (_nextBlink <= 0f)
            {
                _blinkLeft = 0.11f;
                _nextBlink = 2.8f + Random.Range(0f, 2.6f);
                SetMaterialDirty();
            }

            if (_react > 0f) _react = Mathf.Max(0f, _react - dt);
            if (_sayT > 0f)
            {
                _sayT -= dt;
                var a = _sayT > 0.25f ? 1f : Mathf.Clamp01(_sayT / 0.25f);
                SetSayAlpha(a);
            }

            if (!_gotBase)
            {
                _basePos = rectTransform.anchoredPosition;
                _gotBase = true;
            }

            var t = Time.unscaledTime + _phase;
            var idleRot = Mathf.Sin(t * 0.85f) * 0.55f;
            var idleY = Mathf.Sin(t * 1.05f) * 2.2f;
            var rot = idleRot;
            var pos = _basePos + new Vector2(0f, idleY);
            var sc = Vector3.one;

            if (_react > 0f)
            {
                var u = 1f - (_react / 0.70f);
                var punch = Mathf.Sin(u * Mathf.PI);
                if (_zone == 1)
                {
                    rot -= 8f * punch;
                    pos.y -= 6f * punch;
                }
                else if (_zone == 2)
                {
                    sc = Vector3.one * (1f + 0.035f * punch);
                    pos.y += 10f * punch;
                }
                else if (_zone == 3)
                {
                    rot += 11f * punch * (u < 0.45f ? 1f : -0.35f);
                    pos.x += 22f * punch;
                }
                else
                {
                    pos.y += 48f * punch;
                }
            }

            rectTransform.localEulerAngles = new Vector3(0f, 0f, rot);
            rectTransform.anchoredPosition = pos;
            rectTransform.localScale = sc;

            if (_spark != null)
            {
                _spark.rectTransform.anchoredPosition = _sparkAt;
                var sp = _react > 0.45f ? (_react - 0.45f) / 0.25f : 0f;
                var c = _spark.color;
                c.a = Mathf.Clamp01(sp) * 0.9f;
                _spark.color = c;
                _spark.rectTransform.localScale = Vector3.one * (0.7f + (1f - sp) * 0.8f);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var c = (Color32)color;
            var i0 = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin, 0f), c, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(r.xMax, r.yMin, 0f), c, new Vector2(1f, 0f));
            vh.AddVert(new Vector3(r.xMax, r.yMax, 0f), c, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(r.xMin, r.yMax, 0f), c, new Vector2(0f, 1f));
            vh.AddTriangle(i0, i0 + 1, i0 + 2);
            vh.AddTriangle(i0, i0 + 2, i0 + 3);
        }

        void EnsureOverlay()
        {
            if (_say != null) return;
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

            var spGo = new GameObject("spark", typeof(RectTransform), typeof(Image));
            spGo.transform.SetParent(transform, false);
            var spRt = spGo.GetComponent<RectTransform>();
            spRt.anchorMin = spRt.anchorMax = new Vector2(0.5f, 0f);
            spRt.pivot = new Vector2(0.5f, 0.5f);
            spRt.sizeDelta = new Vector2(72f, 72f);
            _spark = spGo.GetComponent<Image>();
            UiSprites.Apply(_spark, UiSprites.Spark());
            _spark.color = new Color(1f, 0.82f, 0.35f, 0f);
            _spark.raycastTarget = false;
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
