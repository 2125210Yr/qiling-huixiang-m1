using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// C001 home still: original torso + hair overlays. Not Cubism moc3.
    /// Arms/legs/sword stay on the still — See-through limb plates are a different pose.
    /// </summary>
    public sealed class LiveRig : MonoBehaviour, IPointerDownHandler
    {
        public const int WorldLayer = 9;
        const float Ppu = 100f;
        const float ReactDur = 0.48f;
        static readonly Vector3 Origin = new Vector3(-240f, 0f, 0f);

        RenderTexture _rt;
        Camera _cam;
        Transform _world;
        Transform _view;
        Transform _hairBack, _hairFront, _head, _torso;
        Transform _armL, _armR, _handL, _handR;
        Transform _legL, _legR, _footL, _footR, _sword;
        Texture _headIdle, _headBlink;
        Material _headMat;
        MeshRenderer _headMr;
        Mesh _mesh;
        readonly List<Material> _mats = new List<Material>();
        float _phase = 0.4f;
        float _nextBlink = 2.4f;
        float _blinkLeft;
        float _react;
        int _zone;

        public static bool TryAttach(Transform host, string id)
        {
            if (host == null || id != "C001") return false;
            LiveParts p;
            if (!CharacterArt.TryLiveLayers(id, out p)) return false;
            if (p.torso == null && p.head == null) return false;
            var fx = host.GetComponent<LiveRig>();
            if (fx == null) fx = host.gameObject.AddComponent<LiveRig>();
            if (!fx.Bind(p, id))
            {
                if (fx != null) Object.Destroy(fx);
                return false;
            }
            return true;
        }

        bool Bind(LiveParts p, string id)
        {
            var body = p.torso != null ? p.torso : p.head;
            if (body == null) return false;
            Release(false);
            _headIdle = p.head;
            _headBlink = p.headBlink;
            _phase = 0.4f;
            _zone = 0;
            _react = 0f;
            var w = body.width;
            var h = body.height;
            var bodyW = w / Ppu;
            var bodyH = h / Ppu;
            var piv = ReadPivots(id);

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

            var worldGo = new GameObject("LiveRig_C001");
            worldGo.layer = WorldLayer;
            worldGo.hideFlags = HideFlags.DontSave;
            _world = worldGo.transform;
            _world.position = Origin;
            _mesh = MakeQuad(bodyW, bodyH);

            var hip = Bone(_world, "hip", Origin);
            _torso = Bone(hip, "torso", PivotPos(piv.torso, bodyW, bodyH));
            _head = Bone(hip, "head", PivotPos(piv.head, bodyW, bodyH));
            _armL = Bone(hip, "armL", PivotPos(piv.armL, bodyW, bodyH));
            _armR = Bone(hip, "armR", PivotPos(piv.armR, bodyW, bodyH));
            _handL = Bone(_armL, "handL", PivotPos(piv.handL, bodyW, bodyH));
            _handR = Bone(_armR, "handR", PivotPos(piv.handR, bodyW, bodyH));
            _legL = Bone(hip, "legL", PivotPos(piv.legL, bodyW, bodyH));
            _legR = Bone(hip, "legR", PivotPos(piv.legR, bodyW, bodyH));
            _footL = Bone(_legL, "footL", PivotPos(piv.footL, bodyW, bodyH));
            _footR = Bone(_legR, "footR", PivotPos(piv.footR, bodyW, bodyH));
            _hairBack = Bone(hip, "hairBack", PivotPos(piv.hairBack, bodyW, bodyH));
            _hairFront = Bone(hip, "hairFront", PivotPos(piv.hairFront, bodyW, bodyH));
            _sword = Bone(_handR, "sword", PivotPos(piv.sword, bodyW, bodyH));

            int z = 0;
            // Original still owns face/arms/clothes. Only hair overlays move —
            // See-through limb plates were a different pose and drifted off the body.
            AddPart(_torso, p.torso, z++, 4f);
            AddPart(_hairBack, p.hairBack, z++, 0.5f);
            AddPart(_head, p.headBlink != null ? p.headBlink : p.head, z++, 0f, out _headMat);
            if (_head != null)
            {
                var mr = _head.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = false;
                    _headMr = mr;
                }
            }
            AddPart(_hairFront, p.hairFront, z++, -1f);

            var camGo = new GameObject("LiveRigCam");
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
            return true;
        }

        void AddPart(Transform bone, Texture tex, int order, float z)
        {
            Material unused;
            AddPart(bone, tex, order, z, out unused);
        }

        void AddPart(Transform bone, Texture tex, int order, float z, out Material mat)
        {
            mat = null;
            if (bone == null || tex == null) return;
            Quad(bone, bone.name + "_mesh", Origin, tex, order, z, out mat);
        }

        public void OnPointerDown(PointerEventData e) { Punch(e); }

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
            _zone = v > 0.70f ? 1 : v > 0.48f ? 2 : v > 0.28f ? 3 : 4;
            if (u < 0.42f && v > 0.22f && v < 0.55f) _zone = 5;
            _react = ReactDur;
            if (_headBlink != null)
            {
                _blinkLeft = 0.12f;
                ApplyHead(_headBlink);
            }
        }

        void LateUpdate()
        {
            HideFromOthers();
            var dt = Time.unscaledDeltaTime;
            if (_headBlink != null)
            {
                _nextBlink -= dt;
                if (_blinkLeft > 0f)
                {
                    _blinkLeft -= dt;
                    if (_blinkLeft <= 0f) ApplyHead(_headIdle);
                }
                else if (_nextBlink <= 0f)
                {
                    _blinkLeft = 0.11f;
                    _nextBlink = 2.5f + Random.Range(0f, 2.2f);
                    ApplyHead(_headBlink);
                }
            }
            if (_react > 0f) _react = Mathf.Max(0f, _react - dt);

            var t = Time.unscaledTime + _phase;
            var punch = _react > 0f ? Mathf.Sin((1f - _react / ReactDur) * Mathf.PI) : 0f;
            var headHit = _zone == 1 ? punch : 0f;
            var chestHit = _zone == 2 ? punch : 0f;
            var bladeHit = _zone == 5 ? punch : 0f;

            var breath = Mathf.Sin(t * 1.05f);
            var breath2 = Mathf.Sin(t * 2.10f + 0.4f);
            Spin(_hairBack, breath * 2.0f + breath2 * 0.40f + headHit * 0.8f);
            Spin(_hairFront, Mathf.Sin(t * 1.18f + 0.7f) * 1.4f + headHit * 0.4f);
            Spin(_head, 0f);
            Spin(_torso, 0f);
            if (_torso != null)
                _torso.localScale = Vector3.one;
            Spin(_armL, 0f);
            Spin(_armR, 0f);
            Spin(_handL, 0f);
            Spin(_handR, 0f);
            Spin(_legL, 0f);
            Spin(_legR, 0f);
            Spin(_footL, 0f);
            Spin(_footR, 0f);
            Spin(_sword, 0f);
            if (_cam != null) _cam.Render();
        }

        static void Spin(Transform b, float deg)
        {
            if (b != null) b.localEulerAngles = new Vector3(0f, 0f, deg);
        }

        void ApplyHead(Texture tex)
        {
            if (_headMr == null || _headMat == null) return;
            var blink = tex != null && tex == _headBlink;
            _headMr.enabled = blink;
            if (!blink || tex == null) return;
            _headMat.mainTexture = tex;
            if (_headMat.HasProperty("_BaseMap")) _headMat.SetTexture("_BaseMap", tex);
            if (_headMat.HasProperty("_MainTex")) _headMat.SetTexture("_MainTex", tex);
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
            mesh.name = "live-plate";
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

        static PivotDto ReadPivots(string id)
        {
            var d = new PivotDto();
            var ta = Resources.Load<TextAsset>("Art/Characters/" + id + "/LiveLayers/pivots");
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                var file = JsonUtility.FromJson<PivotFile>(ta.text);
                if (file != null)
                {
                    Take(file.hairBack, ref d.hairBack);
                    Take(file.hairFront, ref d.hairFront);
                    Take(file.head, ref d.head);
                    Take(file.torso, ref d.torso);
                    Take(file.armL, ref d.armL);
                    Take(file.armR, ref d.armR);
                    Take(file.handL, ref d.handL);
                    Take(file.handR, ref d.handR);
                    Take(file.legL, ref d.legL);
                    Take(file.legR, ref d.legR);
                    Take(file.footL, ref d.footL);
                    Take(file.footR, ref d.footR);
                    Take(file.sword, ref d.sword);
                }
            }
            return d;
        }

        static void Take(float[] a, ref Vector2 v)
        {
            if (a == null || a.Length < 2) return;
            if (a[0] < 0f || a[0] > 1f || a[1] < 0f || a[1] > 1f) return;
            v = new Vector2(a[0], a[1]);
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
            _hairBack = _hairFront = _head = _torso = null;
            _armL = _armR = _handL = _handR = null;
            _legL = _legR = _footL = _footR = _sword = null;
            _headMat = null;
            _headMr = null;
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

        void OnDestroy() { Release(true); }

        class PivotDto
        {
            public Vector2 hairBack = new Vector2(0.3731f, 0.8797f);
            public Vector2 hairFront = new Vector2(0.5926f, 0.8981f);
            public Vector2 head = new Vector2(0.5917f, 0.7803f);
            public Vector2 torso = new Vector2(0.6009f, 0.7054f);
            public Vector2 armL = new Vector2(0.7676f, 0.7560f);
            public Vector2 armR = new Vector2(0.4694f, 0.7647f);
            public Vector2 handL = new Vector2(0.7676f, 0.5827f);
            public Vector2 handR = new Vector2(0.4694f, 0.5646f);
            public Vector2 legL = new Vector2(0.6426f, 0.6355f);
            public Vector2 legR = new Vector2(0.6426f, 0.6355f);
            public Vector2 footL = new Vector2(0.5370f, 0.2366f);
            public Vector2 footR = new Vector2(0.5370f, 0.2366f);
            public Vector2 sword = new Vector2(0.3704f, 0.4888f);
        }

        [System.Serializable]
        class PivotFile
        {
            public float[] hairBack, hairFront, head, torso;
            public float[] armL, armR, handL, handR;
            public float[] legL, legR, footL, footR, sword;
        }

        sealed class ViewTap : MonoBehaviour, IPointerDownHandler
        {
            LiveRig _host;
            public void Bind(LiveRig host) { _host = host; }
            public void OnPointerDown(PointerEventData e)
            {
                if (_host != null) _host.Punch(e);
            }
        }
    }

    public struct LiveParts
    {
        public Texture2D hairBack, hairFront, head, headBlink, torso;
        public Texture2D armL, armR, handL, handR;
        public Texture2D legL, legR, footL, footR, sword;
    }
}
