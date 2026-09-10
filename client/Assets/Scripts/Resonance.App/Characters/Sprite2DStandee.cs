using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// C001 idle: still body + rigid hair locks. Roots stay. Tips sway. Not moc.
    /// </summary>
    public sealed class Sprite2DStandee : MonoBehaviour
    {
        public const int WorldLayer = 6;
        const float Ppu = 100f;

        RenderTexture _rt;
        Camera _cam;
        Transform _world;
        Transform _outerRoot;
        Transform _mintRoot;
        Transform _frontRoot;
        Texture _bodyIdle;
        Texture _bodyBlink;
        Material _bodyMat;
        Material _stageMat;
        Material _outerMat;
        Material _mintMat;
        Material _frontMat;
        float _phase;
        float _nextBlink = 2.6f;
        float _blinkLeft;
        float _react;
        int _zone;
        float _bodyH;
        float _bodyW;

        public static bool TryAttach(Transform host, string id)
        {
            if (host == null || id != "C001") return false;
            Texture2D body, blink, hairFront, hairBack, hairTip, sword;
            if (CharacterArt.TryPreviewLayers(id, out body, out blink, out hairFront, out hairBack, out hairTip)
                && body != null && (hairBack != null || hairFront != null || hairTip != null))
            {
                var fx = host.gameObject.GetComponent<Sprite2DStandee>();
                if (fx == null) fx = host.gameObject.AddComponent<Sprite2DStandee>();
                fx.BindLayers(body, blink, hairBack, hairFront,
                    new Vector2(0.27f, 0.72f), new Vector2(0.75f, 0.66f),
                    hairTip, new Vector2(0.34f, 0.69f));
                return true;
            }
            if (CharacterArt.TryV3Layers(id, out body, out blink, out hairFront, out hairBack, out sword)
                && body != null && (hairBack != null || hairFront != null))
            {
                var fx = host.gameObject.GetComponent<Sprite2DStandee>();
                if (fx == null) fx = host.gameObject.AddComponent<Sprite2DStandee>();
                fx.BindLayers(body, blink, hairBack, hairFront, new Vector2(0.547f, 0.844f), new Vector2(0.685f, 0.705f));
                return true;
            }
            return false;
        }

        public void BindLayers(Texture2D body, Texture2D blink, Texture2D hairBack, Texture2D hairFront)
        {
            BindLayers(body, blink, hairBack, hairFront, new Vector2(0.547f, 0.844f), new Vector2(0.685f, 0.705f), null, Vector2.zero);
        }

        public void BindLayers(Texture2D body, Texture2D blink, Texture2D hairBack, Texture2D hairFront,
            Vector2 hairBackPivot, Vector2 hairFrontPivot, Texture2D hairTip = null, Vector2 hairTipPivot = default,
            Texture2D sword = null)
        {
            if (body == null) return;
            _bodyIdle = body;
            _bodyBlink = blink;
            _phase = 0.21f;
            var w = body.width;
            var h = body.height;
            _bodyH = h / Ppu;
            _bodyW = w / Ppu;

            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
            _rt.Create();

            var view = new GameObject("view", typeof(RectTransform), typeof(RawImage));
            view.transform.SetParent(transform, false);
            var vrt = view.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            var raw = view.GetComponent<RawImage>();
            raw.texture = _rt;
            raw.color = Color.white;
            raw.raycastTarget = true;
            var fit = view.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = w / (float)h;
            view.AddComponent<StandeeTap>().Bind(this);

            var origin = new Vector3(-90f, 0f, 0f);
            var worldGo = new GameObject("Sprite2D_C001");
            worldGo.layer = WorldLayer;
            _world = worldGo.transform;
            _world.position = origin;

            var stage = CharacterArt.LoadStage("C001");
            if (stage != null)
                Quad(worldGo.transform, "stage", origin, stage, 0, out _stageMat);

            if (hairBack != null)
            {
                _outerRoot = Bone(worldGo.transform, "outerRoot",
                    origin + new Vector3((hairBackPivot.x - 0.5f) * _bodyW, hairBackPivot.y * _bodyH, 0f));
                Quad(_outerRoot, "hairOuter", origin, hairBack, 1, out _outerMat);
            }
            if (hairTip != null)
            {
                _mintRoot = Bone(worldGo.transform, "mintRoot",
                    origin + new Vector3((hairTipPivot.x - 0.5f) * _bodyW, hairTipPivot.y * _bodyH, 0f));
                Quad(_mintRoot, "hairMint", origin, hairTip, 2, out _mintMat);
            }
            if (hairFront != null)
            {
                _frontRoot = Bone(worldGo.transform, "frontRoot",
                    origin + new Vector3((hairFrontPivot.x - 0.5f) * _bodyW, hairFrontPivot.y * _bodyH, 0f));
                Quad(_frontRoot, "hairFront", origin, hairFront, 3, out _frontMat);
            }
            Quad(worldGo.transform, "body", origin, body, 6, out _bodyMat);

            var camGo = new GameObject("Sprite2DCam");
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = _bodyH * 0.50f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << WorldLayer;
            _cam.targetTexture = _rt;
            _cam.depth = -50;
            _cam.allowHDR = false;
            _cam.enabled = false;
            camGo.transform.position = origin + new Vector3(0f, _bodyH * 0.50f, -12f);
            AttachUrp(_cam);

            var main = Camera.main;
            if (main != null) main.cullingMask &= ~(1 << WorldLayer);

            _cam.Render();
        }

        public void Punch(PointerEventData e)
        {
            if (e == null) return;
            Vector2 local;
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local))
                return;
            var r = rt.rect;
            if (r.height < 1f) return;
            var u = (local.x - r.xMin) / r.width;
            var v = (local.y - r.yMin) / r.height;
            _zone = v > 0.70f ? 1 : v > 0.45f ? 2 : 3;
            if (u < 0.40f && v > 0.25f && v < 0.55f) _zone = 5;
            _react = 0.72f;
            _blinkLeft = 0.12f;
            ApplyBody(_bodyBlink);
        }

        void LateUpdate()
        {
            var dt = Time.unscaledDeltaTime;
            _nextBlink -= dt;
            if (_blinkLeft > 0f)
            {
                _blinkLeft -= dt;
                if (_blinkLeft <= 0f) ApplyBody(_bodyIdle);
            }
            else if (_nextBlink <= 0f && _bodyBlink != null)
            {
                _blinkLeft = 0.11f;
                _nextBlink = 2.7f + Random.Range(0f, 2.0f);
                ApplyBody(_bodyBlink);
            }
            if (_react > 0f) _react = Mathf.Max(0f, _react - dt);

            var t = Time.unscaledTime + _phase;
            var punch = _react > 0f ? Mathf.Sin((1f - _react / 0.72f) * Mathf.PI) : 0f;
            var head = _zone == 1 ? punch : 0f;
            var outer = Mathf.Sin(t * 0.62f) * 2.0f + Mathf.Sin(t * 1.35f) * 0.35f + head * 2.2f;
            var mint = Mathf.Sin(t * 0.62f - 0.55f) * 2.6f + Mathf.Sin(t * 1.48f + 0.2f) * 0.4f + head * 2.8f;
            var front = Mathf.Sin(t * 0.78f + 0.9f) * 2.2f + Mathf.Sin(t * 1.7f) * 0.35f + head * 1.6f;
            if (_outerRoot != null) _outerRoot.localEulerAngles = new Vector3(0f, 0f, outer);
            if (_mintRoot != null) _mintRoot.localEulerAngles = new Vector3(0f, 0f, mint);
            if (_frontRoot != null) _frontRoot.localEulerAngles = new Vector3(0f, 0f, -front);
            if (_cam != null) _cam.Render();
        }

        void ApplyBody(Texture tex)
        {
            if (_bodyMat == null || tex == null) return;
            _bodyMat.mainTexture = tex;
            if (_bodyMat.HasProperty("_BaseMap")) _bodyMat.SetTexture("_BaseMap", tex);
            if (_bodyMat.HasProperty("_MainTex")) _bodyMat.SetTexture("_MainTex", tex);
        }

        void Quad(Transform parent, string name, Vector3 worldPos, Texture tex, int order, out Material mat)
        {
            var go = new GameObject(name);
            go.layer = WorldLayer;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = MakeQuad(_bodyW, _bodyH);
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.sortingOrder = order;
            mat = MakeMat(tex);
            if (mat == null)
            {
                Destroy(go);
                return;
            }
            mr.sharedMaterial = mat;
        }

        static Mesh MakeQuad(float bodyW, float bodyH)
        {
            var mesh = new Mesh();
            mesh.name = "v3-plate";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f * bodyW, 0f, 0f),
                new Vector3(-0.5f * bodyW, bodyH, 0f),
                new Vector3( 0.5f * bodyW, 0f, 0f),
                new Vector3( 0.5f * bodyW, bodyH, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Transform Bone(Transform parent, string name, Vector3 world)
        {
            var go = new GameObject(name);
            go.layer = WorldLayer;
            go.transform.SetParent(parent, false);
            go.transform.position = world;
            return go.transform;
        }

        static void AttachUrp(Camera cam)
        {
            var t = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (t == null || cam.GetComponent(t) != null) return;
            cam.gameObject.AddComponent(t);
        }

        static Material MakeMat(Texture tex)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Transparent")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Texture");
            if (sh == null) return null;
            var m = new Material(sh);
            m.color = Color.white;
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
            }
            return m;
        }

        void OnDestroy()
        {
            if (_cam != null) Destroy(_cam.gameObject);
            if (_world != null) Destroy(_world.gameObject);
            if (_bodyMat != null) Destroy(_bodyMat);
            if (_stageMat != null) Destroy(_stageMat);
            if (_outerMat != null) Destroy(_outerMat);
            if (_mintMat != null) Destroy(_mintMat);
            if (_frontMat != null) Destroy(_frontMat);
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
        }

        sealed class StandeeTap : MonoBehaviour, IPointerDownHandler
        {
            Sprite2DStandee _host;
            public void Bind(Sprite2DStandee host) { _host = host; }
            public void OnPointerDown(PointerEventData e)
            {
                if (_host != null) _host.Punch(e);
            }
        }
    }
}
