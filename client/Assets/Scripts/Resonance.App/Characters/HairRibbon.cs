using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.App
{
    /// <summary>
    /// Hair strip deformed by a FK bone chain. Body is not skinned. Not moc.
    /// Vertices rotate around each ancestor bone; tip lags and swings more.
    /// </summary>
    sealed class HairRibbon
    {
        const int Cols = 40;
        const int Rows = 64;

        readonly Vector3[] _rest;
        readonly Vector3[] _skin;
        readonly Vector3[] _boneRest;
        readonly float[] _t;
        readonly float[] _chain;
        readonly float[] _amp;
        readonly Mesh _mesh;
        readonly MeshFilter _filter;
        readonly Material _mat;
        readonly float _lag;
        readonly float _freq;
        readonly float _phase;

        public HairRibbon(Transform world, Texture2D tex, Vector2[] uvPath, float[] amp,
            int order, float bodyW, float bodyH, float radius, float lag, float freq, float phase)
        {
            _amp = amp;
            _lag = lag;
            _freq = freq;
            _phase = phase;
            int n = amp.Length;
            _boneRest = new Vector3[n];
            for (int i = 0; i < n; i++)
                _boneRest[i] = UvToLocal(uvPath[Mathf.Min(i, uvPath.Length - 1)], bodyW, bodyH);

            var meshGo = new GameObject("hairMesh");
            meshGo.layer = Sprite2DStandee.WorldLayer;
            meshGo.transform.SetParent(world, false);
            _filter = meshGo.AddComponent<MeshFilter>();
            var mr = meshGo.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.sortingOrder = order;
            _mat = MakeMat(tex);
            mr.sharedMaterial = _mat;

            int vx = Cols + 1;
            int vy = Rows + 1;
            int nVert = vx * vy;
            _rest = new Vector3[nVert];
            _skin = new Vector3[nVert];
            _t = new float[nVert];
            _chain = new float[nVert];
            var uv = new Vector2[nVert];
            var tris = new int[Cols * Rows * 6];

            var pixels = ReadPixels(tex);
            int tw = tex != null ? tex.width : 0;
            int th = tex != null ? tex.height : 0;

            for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Cols; x++)
            {
                int i = y * vx + x;
                float u = x / (float)Cols;
                float v = y / (float)Rows;
                _rest[i] = UvToLocal(new Vector2(u, v), bodyW, bodyH);
                uv[i] = new Vector2(u, v);
                float a = SampleA(pixels, tw, th, u, v);
                if (a < 0.02f) a = 1f;
                float t01, dist;
                Project(new Vector2(u, v), uvPath, out t01, out dist);
                _t[i] = t01;
                float cover = Mathf.Clamp01(1f - dist / Mathf.Max(0.05f, radius));
                _chain[i] = cover;
            }

            int t = 0;
            for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
            {
                int i = y * vx + x;
                tris[t++] = i;
                tris[t++] = i + vx;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + vx;
                tris[t++] = i + vx + 1;
            }

            _mesh = new Mesh { name = "hair-ribbon" };
            _mesh.MarkDynamic();
            _mesh.vertices = _rest;
            _mesh.uv = uv;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
            _mesh.bounds = new Bounds(new Vector3(0f, bodyH * 0.5f, 0f), new Vector3(bodyW * 1.5f, bodyH * 1.5f, 1f));
            _filter.sharedMesh = _mesh;
        }

        public void Tick(float time, float punch)
        {
            if (_mesh == null || _amp == null || _amp.Length == 0) return;
            int n = _amp.Length;
            var angs = new float[n];
            var t0 = time * _freq + _phase;
            for (int i = 0; i < n; i++)
            {
                angs[i] = _amp[i] * Mathf.Sin(t0 - i * _lag)
                          + _amp[i] * 0.25f * Mathf.Sin(t0 * 2.05f - i * _lag * 0.65f)
                          + punch * (0.5f + i * 0.8f);
            }

            var piv = new Vector3[n];
            for (int i = 0; i < n; i++)
                piv[i] = _boneRest[i];
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    piv[j] = RotZ(piv[j], piv[i], angs[i]);

            int nv = _rest.Length;
            float nm1 = n - 1;
            for (int v = 0; v < nv; v++)
            {
                float c = _chain[v];
                if (c < 0.02f)
                {
                    _skin[v] = _rest[v];
                    continue;
                }
                var p = _rest[v];
                float u = _t[v] * nm1;
                for (int i = 0; i < n; i++)
                {
                    float inf = Mathf.Clamp01(u - (i - 0.35f));
                    if (inf < 0.01f) continue;
                    p = RotZ(p, piv[i], angs[i] * inf * c);
                }
                _skin[v] = p;
            }
            _mesh.SetVertices(_skin);
        }

        public void Dispose()
        {
            if (_mat != null) Object.Destroy(_mat);
            if (_mesh != null) Object.Destroy(_mesh);
            if (_filter != null) Object.Destroy(_filter.gameObject);
        }

        static Vector3 RotZ(Vector3 p, Vector3 pivot, float deg)
        {
            if (deg == 0f) return p;
            float r = deg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(r);
            float sn = Mathf.Sin(r);
            float dx = p.x - pivot.x;
            float dy = p.y - pivot.y;
            return new Vector3(pivot.x + dx * cs - dy * sn, pivot.y + dx * sn + dy * cs, p.z);
        }

        static Vector3 UvToLocal(Vector2 uv, float bodyW, float bodyH)
        {
            return new Vector3((uv.x - 0.5f) * bodyW, uv.y * bodyH, 0f);
        }

        static float SampleA(Color32[] px, int tw, int th, float u, float v)
        {
            if (px == null || tw < 2 || th < 2) return 1f;
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (tw - 1)), 0, tw - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (th - 1)), 0, th - 1);
            return px[y * tw + x].a / 255f;
        }

        static void Project(Vector2 p, Vector2[] path, out float t01, out float dist)
        {
            float best = float.MaxValue;
            float bestAlong = 0f;
            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
                total += Vector2.Distance(path[i], path[i + 1]);
            if (total < 1e-5f)
            {
                t01 = 0f;
                dist = Vector2.Distance(p, path[0]);
                return;
            }
            float acc = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var ab = b - a;
                float ab2 = ab.sqrMagnitude;
                float uu = ab2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab2);
                var q = a + ab * uu;
                float d = (p - q).magnitude;
                if (d < best)
                {
                    best = d;
                    bestAlong = acc + uu * Mathf.Sqrt(ab2);
                }
                acc += Mathf.Sqrt(ab2);
            }
            dist = best;
            t01 = Mathf.Clamp01(bestAlong / total);
        }

        static Color32[] ReadPixels(Texture2D tex)
        {
            if (tex == null) return null;
            try
            {
                if (tex.isReadable) return tex.GetPixels32();
            }
            catch (UnityException) { }
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                var tmp = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                tmp.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tmp.Apply(false, false);
                var px = tmp.GetPixels32();
                Object.Destroy(tmp);
                return px;
            }
            catch
            {
                return null;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
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
    }
}
