using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// C001 cut-part idle: full-canvas PNG parts hung on bone pivots. Not Cubism.
    /// </summary>
    public sealed class CutoutRig : MonoBehaviour, IPointerDownHandler
    {
        public const int WorldLayer = 9;
        const float Ppu = 100f;
        const float ReactDur = 0.42f;
        const float HairBackAmp = 3.4f;
        const float HairBackAmp2 = 0.75f;
        const float HairFrontAmp = 3.0f;
        const float HairFrontAmp2 = 0.65f;
        const float HairTipAmp = 3.8f;
        const float HairTipAmp2 = 0.70f;
        const float SwordAmp = 0.90f;
        const float SwordAmp2 = 0.18f;
        const float PunchHair = 4.2f;
        const float PunchSword = 2.4f;

        static readonly Vector3 Origin = new Vector3(-240f, 0f, 0f);

        RenderTexture _rt;
        Camera _cam;
        Transform _world;
        Transform _view;
        Transform _hairBack;
        Transform _hairFront;
        Transform _hairTip;
        Transform _sword;
        Transform _bodyRoot;
        Texture _bodyIdle;
        Texture _bodyBlink;
        Material _bodyMat;
        Mesh _mesh;
        readonly List<Material> _mats = new List<Material>();
        float _phase = 0.85f;
        float _nextBlink = 2.6f;
        float _blinkLeft;
        float _react;
        int _zone;

        public static bool TryAttach(Transform host, string id)
        {
            if (host == null || id != "C001") return false;
            Texture2D body, blink, hairFront, hairBack, hairTip, sword;
            string folder;
            if (!TryLoad(id, out body, out blink, out hairFront, out hairBack, out hairTip, out sword, out folder))
                return false;
            var fx = host.GetComponent<CutoutRig>();
            if (fx == null) fx = host.gameObject.AddComponent<CutoutRig>();
            fx.Bind(body, blink, hairBack, hairFront, hairTip, sword, folder, id);
            return true;
        }

        static bool TryLoad(string id, out Texture2D body, out Texture2D blink,
            out Texture2D hairFront, out Texture2D hairBack, out Texture2D hairTip,
            out Texture2D sword, out string folder)
        {
            if (CharacterArt.TrySpineLayers(id, out body, out blink, out hairFront, out hairBack, out hairTip, out sword))
            {
                folder = "SpineLayers";
                return true;
            }
            if (CharacterArt.TryPreviewLayers(id, out body, out blink, out hairFront, out hairBack, out hairTip, out sword))
            {
                folder = "PreviewLayers";
                return true;
            }
            hairTip = null;
            if (CharacterArt.TryV3Layers(id, out body, out blink, out hairFront, out hairBack, out sword)
                && body != null)
            {
                folder = "V3Layers";
                return true;
            }
            folder = null;
            return false;
        }

        void Bind(Texture2D body, Texture2D blink, Texture2D hairBack, Texture2D hairFront,
            Texture2D hairTip, Texture2D sword, string folder, string id)
        {
            if (body == null) return;
            Release(false);
            _bodyIdle = body;
            _bodyBlink = blink;
            _phase = 0.85f;
            _zone = 0;
            _react = 0f;
            var w = body.width;
            var h = body.height;
            var bodyW = w / Ppu;
            var bodyH = h / Ppu;

            Vector2 pBack, pFront, pTip, pSword;
            ReadPivots(id, folder, out pBack, out pFront, out pTip, out pSword);

            var rtW = w;
            var rtH = h;
            const int maxRt = 2048;
            if (rtH > maxRt || rtW > maxRt)
            {
                var s = maxRt / (float)Mathf.Max(rtW, rtH);
                rtW = Mathf.Max(8, Mathf.RoundToInt(rtW * s));
                rtH = Mathf.Max(8, Mathf.RoundToInt(rtH * s));
            }
            _rt = new RenderTexture(rtW, rtH, 16, RenderTextureFormat.ARGB32);
            _rt.filterMode = FilterMode.Bilinear;
            _rt.antiAliasing = 1;
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
            view.AddComponent<ViewTap>().Bind(this);
            _view = view.transform;

            var worldGo = new GameObject("CutoutRig_C001");
            worldGo.layer = WorldLayer;
            worldGo.hideFlags = HideFlags.DontSave;
            _world = worldGo.transform;
            _world.position = Origin;

            _mesh = MakeQuad(bodyW, bodyH);

            if (hairBack != null)
            {
                _hairBack = Bone(_world, "hairBack", PivotPos(pBack, bodyW, bodyH));
                Quad(_hairBack, "hairBack", Origin, hairBack, 0, 4f, out _);
            }
            _bodyRoot = Bone(_world, "body", Origin);
            Quad(_bodyRoot, "body", Origin, body, 1, 2f, out _bodyMat);
            if (sword != null)
            {
                _sword = Bone(_world, "sword", PivotPos(pSword, bodyW, bodyH));
                Quad(_sword, "sword", Origin, sword, 2, 0f, out _);
            }
            if (hairTip != null)
            {
                _hairTip = Bone(_world, "hairTip", PivotPos(pTip, bodyW, bodyH));
                Quad(_hairTip, "hairTip", Origin, hairTip, 3, -2f, out _);
            }
            if (hairFront != null)
            {
                _hairFront = Bone(_world, "hairFront", PivotPos(pFront, bodyW, bodyH));
                Quad(_hairFront, "hairFront", Origin, hairFront, 4, -4f, out _);
            }

            var camGo = new GameObject("CutoutRigCam");
            camGo.hideFlags = HideFlags.DontSave;
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = bodyH * 0.50f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << WorldLayer;
            _cam.targetTexture = _rt;
            _cam.depth = -50;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.useOcclusionCulling = false;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 40f;
            _cam.enabled = false;
            camGo.transform.position = Origin + new Vector3(0f, bodyH * 0.50f, -12f);
            AttachUrp(_cam);
            HideFromOthers();
            _cam.Render();
        }

        public void OnPointerDown(PointerEventData e)
        {
            Punch(e);
        }

        public void Punch(PointerEventData e)
        {
            if (e == null) return;
            var area = _view != null ? (RectTransform)_view : (RectTransform)transform;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out local))
                return;
            var r = area.rect;
            if (r.height < 1f) return;
            var u = (local.x - r.xMin) / r.width;
            var v = (local.y - r.yMin) / r.height;
            _zone = v > 0.70f ? 1 : v > 0.45f ? 2 : 3;
            if (u < 0.40f && v > 0.25f && v < 0.55f) _zone = 5;
            _react = ReactDur;
            if (_bodyBlink != null)
            {
                _blinkLeft = 0.12f;
                ApplyBody(_bodyBlink);
            }
        }

        void LateUpdate()
        {
            HideFromOthers();
            var dt = Time.unscaledDeltaTime;
            if (_bodyBlink != null)
            {
                _nextBlink -= dt;
                if (_blinkLeft > 0f)
                {
                    _blinkLeft -= dt;
                    if (_blinkLeft <= 0f) ApplyBody(_bodyIdle);
                }
                else if (_nextBlink <= 0f)
                {
                    _blinkLeft = 0.11f;
                    _nextBlink = 2.7f + Random.Range(0f, 2.0f);
                    ApplyBody(_bodyBlink);
                }
            }
            if (_react > 0f) _react = Mathf.Max(0f, _react - dt);

            var t = Time.unscaledTime + _phase;
            var punch = _react > 0f ? Mathf.Sin((1f - _react / ReactDur) * Mathf.PI) : 0f;
            var head = _zone == 1 ? punch : 0f;
            var chest = _zone == 2 ? punch : 0f;
            var blade = _zone == 5 ? punch : 0f;

            var hairB = Mathf.Sin(t * 1.12f) * HairBackAmp
                        + Mathf.Sin(t * 2.25f + 0.40f) * HairBackAmp2
                        + head * PunchHair;
            var hairF = Mathf.Sin(t * 1.20f + 0.72f) * HairFrontAmp
                        + Mathf.Sin(t * 2.45f + 0.90f) * HairFrontAmp2
                        + head * PunchHair * 0.65f
                        + chest * 1.6f;
            var hairT = Mathf.Sin(t * 1.12f - 0.48f) * HairTipAmp
                        + Mathf.Sin(t * 2.32f - 0.20f) * HairTipAmp2
                        + head * PunchHair * 1.05f;
            var sword = Mathf.Sin(t * 0.86f + 0.35f) * SwordAmp
                        + Mathf.Sin(t * 1.85f) * SwordAmp2
                        + blade * PunchSword;

            if (_hairBack != null) _hairBack.localEulerAngles = new Vector3(0f, 0f, hairB);
            if (_hairFront != null) _hairFront.localEulerAngles = new Vector3(0f, 0f, hairF);
            if (_hairTip != null) _hairTip.localEulerAngles = new Vector3(0f, 0f, hairT);
            if (_sword != null) _sword.localEulerAngles = new Vector3(0f, 0f, sword);
            if (_bodyRoot != null)
            {
                _bodyRoot.localEulerAngles = Vector3.zero;
                _bodyRoot.localScale = Vector3.one;
            }
            if (_cam != null) _cam.Render();
        }

        void ApplyBody(Texture tex)
        {
            if (_bodyMat == null || tex == null) return;
            _bodyMat.mainTexture = tex;
            if (_bodyMat.HasProperty("_BaseMap")) _bodyMat.SetTexture("_BaseMap", tex);
            if (_bodyMat.HasProperty("_MainTex")) _bodyMat.SetTexture("_MainTex", tex);
        }

        void Quad(Transform parent, string name, Vector3 worldPos, Texture tex, int order, float z, out Material mat)
        {
            var go = new GameObject(name);
            go.layer = WorldLayer;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos + new Vector3(0f, 0f, z);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.allowOcclusionWhenDynamic = false;
            mr.sortingOrder = order;
            mat = MakeMat(tex);
            if (mat == null)
            {
                Destroy(go);
                return;
            }
            mr.sharedMaterial = mat;
            _mats.Add(mat);
        }

        static Vector3 PivotPos(Vector2 uv, float bodyW, float bodyH)
        {
            return Origin + new Vector3((uv.x - 0.5f) * bodyW, uv.y * bodyH, 0f);
        }

        static Mesh MakeQuad(float bodyW, float bodyH)
        {
            var mesh = new Mesh();
            mesh.name = "cutout-plate";
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
            var t = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (t == null) return;
            var data = cam.GetComponent(t);
            if (data == null) data = cam.gameObject.AddComponent(t);
            var shadows = t.GetProperty("renderShadows");
            if (shadows != null && shadows.CanWrite) shadows.SetValue(data, false, null);
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
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
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

        void HideFromOthers()
        {
            var mask = ~(1 << WorldLayer);
            var cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null || c == _cam) continue;
                c.cullingMask &= mask;
            }
        }

        static void ReadPivots(string id, string folder,
            out Vector2 hairBack, out Vector2 hairFront, out Vector2 hairTip, out Vector2 sword)
        {
            hairBack = new Vector2(0.48f, 0.78f);
            hairFront = new Vector2(0.52f, 0.78f);
            hairTip = new Vector2(0.46f, 0.76f);
            sword = new Vector2(0.32f, 0.46f);
            if (folder == "PreviewLayers")
            {
                hairBack = new Vector2(0.271f, 0.717f);
                hairFront = new Vector2(0.753f, 0.66f);
                hairTip = new Vector2(0.336f, 0.686f);
                sword = new Vector2(0.30f, 0.44f);
            }
            else if (folder == "V3Layers")
            {
                hairBack = new Vector2(0.547f, 0.844f);
                hairFront = new Vector2(0.685f, 0.705f);
                hairTip = hairBack;
                sword = new Vector2(0.36f, 0.46f);
            }

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(folder)) return;
            var ta = Resources.Load<TextAsset>("Art/Characters/" + id + "/" + folder + "/pivots");
            if (ta == null || string.IsNullOrEmpty(ta.text)) return;
            var dto = JsonUtility.FromJson<PivotFile>(ta.text);
            if (dto == null) return;
            Vector2 v;
            if (TryXY(dto.hairBack, out v)) hairBack = v;
            if (TryXY(dto.hairFront, out v)) hairFront = v;
            if (TryXY(dto.hairTip, out v)) hairTip = v;
            if (TryXY(dto.sword, out v)) sword = v;
        }

        static bool TryXY(float[] a, out Vector2 v)
        {
            v = default;
            if (a == null || a.Length < 2) return false;
            if (a[0] < 0f || a[0] > 1f || a[1] < 0f || a[1] > 1f) return false;
            v = new Vector2(a[0], a[1]);
            return true;
        }

        void Release(bool fromDestroy)
        {
            if (_view != null)
            {
                var raw = _view.GetComponent<RawImage>();
                if (raw != null) raw.texture = null;
            }
            if (_cam != null)
            {
                _cam.targetTexture = null;
                _cam.enabled = false;
                Destroy(_cam.gameObject);
                _cam = null;
            }
            if (_world != null)
            {
                Destroy(_world.gameObject);
                _world = null;
            }
            _hairBack = _hairFront = _hairTip = _sword = _bodyRoot = null;
            _bodyMat = null;
            for (int i = 0; i < _mats.Count; i++)
                if (_mats[i] != null) Destroy(_mats[i]);
            _mats.Clear();
            if (_mesh != null)
            {
                Destroy(_mesh);
                _mesh = null;
            }
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }
            if (!fromDestroy && _view != null)
            {
                Destroy(_view.gameObject);
                _view = null;
            }
            else
                _view = null;
        }

        void OnDestroy()
        {
            Release(true);
        }

        [System.Serializable]
        class PivotFile
        {
            public float[] hairBack;
            public float[] hairFront;
            public float[] hairTip;
            public float[] sword;
        }

        sealed class ViewTap : MonoBehaviour, IPointerDownHandler
        {
            CutoutRig _host;
            public void Bind(CutoutRig host) { _host = host; }
            public void OnPointerDown(PointerEventData e)
            {
                if (_host != null) _host.Punch(e);
            }
        }
    }
}
