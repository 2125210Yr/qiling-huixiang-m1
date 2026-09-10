using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Home/Inspect standee from Cubism-authored C001 plates.
    /// Chest tap warps vertices on the uncut body mesh (Cubism warp-style). Not moc3.
    /// </summary>
    public sealed class CubismPlateRig : MonoBehaviour, IPointerDownHandler
    {
        public const int WorldLayer = 9;
        const float Ppu = 100f;
        static readonly Vector3 Origin = new Vector3(-240f, 0f, 0f);

        // Photoshop bust bounds [427,370,603,540] on 1024×1536, origin top-left.
        const float ChestU = 515.5f / 1024f;
        const float ChestV = 1f - 455.5f / 1536f;
        const float HitU0 = 0.39f;
        const float HitU1 = 0.63f;
        const float HitV0 = 0.61f;
        const float HitV1 = 0.80f;

        const int GridX = 21;
        const int GridY = 31;
        const float ChestPx = 515.5f;
        const float ChestPy = 448f;
        const float ChestRxPx = 118f;
        const float ChestRyPx = 92f;
        const float JiggleHzY = 2.45f;
        const float JiggleHzX = 1.95f;
        const float JiggleDamp = 1.65f;
        const float JiggleMaxT = 2.15f;

        RenderTexture _rt;
        Camera _cam;
        Transform _world;
        Transform _view;
        Mesh _quad;
        Mesh _bodyMesh;
        Vector3[] _bodyRest;
        Vector3[] _bodyVerts;
        float[] _w;
        Vector3 _chestC;
        readonly List<Material> _mats = new List<Material>();
        float _jiggleT;
        bool _jiggling;
        float _side = 1f;

        public static bool TryAttach(Transform host, string id)
        {
            if (host == null || id != "C001") return false;
            CubismPlates p;
            if (!CharacterArt.TryCubismPlates(id, out p) || p.body == null) return false;
            var fx = host.GetComponent<CubismPlateRig>();
            if (fx == null) fx = host.gameObject.AddComponent<CubismPlateRig>();
            if (!fx.Bind(p))
            {
                if (fx != null) Object.Destroy(fx);
                return false;
            }
            return true;
        }

        bool Bind(CubismPlates p)
        {
            Release(false);
            var w = p.body.width;
            var h = p.body.height;
            var bodyW = w / Ppu;
            var bodyH = h / Ppu;

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
            raw.raycastTarget = false;
            var fit = view.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = w / (float)h;
            _view = view.transform;

            var worldGo = new GameObject("CubismPlates_C001");
            worldGo.layer = WorldLayer;
            worldGo.hideFlags = HideFlags.DontSave;
            _world = worldGo.transform;
            _world.position = Origin;
            _quad = MakeQuad(bodyW, bodyH);
            BuildBodyGrid(w, h, bodyW, bodyH);

            var root = Bone(_world, "root", Origin);
            int z = 0;
            AddPart(root, "hairBack", p.hairBack, _quad, z++, 4f);
            AddPart(root, "body", p.body, _bodyMesh, z++, 2f);
            AddPart(root, "hairSide", p.hairSide, _quad, z++, 0.5f);
            AddPart(root, "hairFront", p.hairFront, _quad, z++, 0f);
            AddPart(root, "sword", p.sword, _quad, z++, -0.5f);

            var camGo = new GameObject("CubismPlateCam");
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
            ApplyJiggle(-1f);
            _cam.Render();
            return true;
        }

        void AddPart(Transform parent, string name, Texture tex, Mesh mesh, int order, float z)
        {
            if (tex == null || mesh == null) return;
            var bone = Bone(parent, name, Origin);
            Material unused;
            Quad(bone, name + "_mesh", Origin, tex, mesh, order, z, out unused);
        }

        public void OnPointerDown(PointerEventData e)
        {
            Punch(e);
        }

        public void Punch(PointerEventData e)
        {
            if (e == null || _view == null) return;
            var area = (RectTransform)_view;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out local))
                return;
            var r = area.rect;
            if (r.height < 1f || r.width < 1f) return;
            var u = (local.x - r.xMin) / r.width;
            var v = (local.y - r.yMin) / r.height;
            if (u < HitU0 || u > HitU1 || v < HitV0 || v > HitV1) return;
            _side = u < ChestU ? -1f : 1f;
            _jiggleT = 0f;
            _jiggling = true;
        }

        void LateUpdate()
        {
            HideFromOthers();
            if (_jiggling)
            {
                _jiggleT += Time.unscaledDeltaTime;
                ApplyJiggle(_jiggleT);
            }
            if (_cam != null) _cam.Render();
        }

        void ApplyJiggle(float t)
        {
            if (_bodyMesh == null || _bodyRest == null || _bodyVerts == null) return;
            if (t < 0f || t >= JiggleMaxT)
            {
                _bodyMesh.vertices = _bodyRest;
                _jiggling = false;
                return;
            }

            var env = Mathf.Exp(-JiggleDamp * t);
            var yWave = env * Mathf.Cos(Mathf.PI * 2f * JiggleHzY * t);
            var xWave = env * Mathf.Sin(Mathf.PI * 2f * JiggleHzX * t);
            var y2 = env * Mathf.Cos(Mathf.PI * 2f * JiggleHzY * 1.35f * t + 0.7f) * 0.28f;
            yWave += y2;

            for (int i = 0; i < _bodyRest.Length; i++)
            {
                var rest = _bodyRest[i];
                var w = _w[i];
                if (w <= 0.001f)
                {
                    _bodyVerts[i] = rest;
                    continue;
                }
                _bodyVerts[i] = Vector3.Lerp(rest, Warp(rest, _chestC, yWave, xWave, _side), w);
            }
            _bodyMesh.vertices = _bodyVerts;
        }

        static Vector3 Warp(Vector3 p, Vector3 c, float yWave, float xWave, float side)
        {
            var d = p - c;
            var sx = 1f + 0.10f * yWave + 0.035f * xWave * side;
            var sy = 1f - 0.13f * yWave;
            d.x *= sx;
            d.y *= sy;
            d.y -= 0.22f * yWave;
            var ang = (5.5f * side * xWave + 1.8f * yWave * side) * Mathf.Deg2Rad;
            var cs = Mathf.Cos(ang);
            var sn = Mathf.Sin(ang);
            return c + new Vector3(d.x * cs - d.y * sn, d.x * sn + d.y * cs, 0f);
        }

        void BuildBodyGrid(int texW, int texH, float bodyW, float bodyH)
        {
            var n = GridX * GridY;
            _bodyRest = new Vector3[n];
            _bodyVerts = new Vector3[n];
            _w = new float[n];
            var uv = new Vector2[n];
            _chestC = Pix(ChestPx, ChestPy, texW, texH, bodyW, bodyH);
            var rx = ChestRxPx / Ppu;
            var ry = ChestRyPx / Ppu;
            int i = 0;
            for (int iy = 0; iy < GridY; iy++)
            {
                var ty = iy / (float)(GridY - 1);
                for (int ix = 0; ix < GridX; ix++)
                {
                    var tx = ix / (float)(GridX - 1);
                    var px = tx * (texW - 1);
                    var py = ty * (texH - 1);
                    var p = Pix(px, py, texW, texH, bodyW, bodyH);
                    _bodyRest[i] = p;
                    _bodyVerts[i] = p;
                    uv[i] = new Vector2(tx, 1f - ty);
                    _w[i] = Falloff(p, _chestC, rx, ry);
                    i++;
                }
            }

            var tris = new int[(GridX - 1) * (GridY - 1) * 6];
            var t = 0;
            for (int iy = 0; iy < GridY - 1; iy++)
            {
                for (int ix = 0; ix < GridX - 1; ix++)
                {
                    var a = iy * GridX + ix;
                    var b = a + 1;
                    var c = a + GridX;
                    var d = c + 1;
                    tris[t++] = a;
                    tris[t++] = c;
                    tris[t++] = b;
                    tris[t++] = b;
                    tris[t++] = c;
                    tris[t++] = d;
                }
            }

            if (_bodyMesh != null) Object.Destroy(_bodyMesh);
            _bodyMesh = new Mesh();
            _bodyMesh.name = "cubism-body";
            _bodyMesh.vertices = _bodyRest;
            _bodyMesh.uv = uv;
            _bodyMesh.triangles = tris;
            _bodyMesh.MarkDynamic();
            _bodyMesh.RecalculateNormals();
            _bodyMesh.RecalculateBounds();
        }

        static Vector3 Pix(float px, float py, int w, int h, float bodyW, float bodyH)
        {
            return new Vector3((px / w - 0.5f) * bodyW, (1f - py / h) * bodyH, 0f);
        }

        static float Falloff(Vector3 p, Vector3 c, float rx, float ry)
        {
            var d = p - c;
            var nx = d.x / Mathf.Max(0.001f, rx);
            var ny = d.y / Mathf.Max(0.001f, ry);
            var r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 1f) return 0f;
            if (r <= 0.20f) return 1f;
            return 1f - Mathf.SmoothStep(0.20f, 1f, r);
        }

        void Quad(Transform parent, string name, Vector3 worldPos, Texture tex, Mesh mesh, int order, float z, out Material mat)
        {
            var go = new GameObject(name);
            go.layer = WorldLayer;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos + new Vector3(0f, 0f, z);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
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

        static Mesh MakeQuad(float bodyW, float bodyH)
        {
            var mesh = new Mesh();
            mesh.name = "cubism-plate";
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

        void OnDestroy()
        {
            Release(true);
        }

        void Release(bool fromDestroy)
        {
            if (_cam != null)
            {
                _cam.targetTexture = null;
                if (_cam.gameObject != null) Object.Destroy(_cam.gameObject);
                _cam = null;
            }
            if (_world != null)
            {
                Object.Destroy(_world.gameObject);
                _world = null;
            }
            if (_view != null && fromDestroy == false)
            {
                Object.Destroy(_view.gameObject);
                _view = null;
            }
            if (_rt != null)
            {
                _rt.Release();
                Object.Destroy(_rt);
                _rt = null;
            }
            if (_quad != null)
            {
                Object.Destroy(_quad);
                _quad = null;
            }
            if (_bodyMesh != null)
            {
                Object.Destroy(_bodyMesh);
                _bodyMesh = null;
            }
            for (int i = 0; i < _mats.Count; i++)
                if (_mats[i] != null) Object.Destroy(_mats[i]);
            _mats.Clear();
            _bodyRest = null;
            _bodyVerts = null;
            _w = null;
            _jiggling = false;
        }
    }

    public struct CubismPlates
    {
        public Texture2D body;
        public Texture2D bust;
        public Texture2D hairBack;
        public Texture2D hairFront;
        public Texture2D hairSide;
        public Texture2D sword;
    }
}
