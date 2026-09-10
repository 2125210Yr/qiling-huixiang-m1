using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Real ice particle field (Unity ParticleSystem → RT → UI).
    /// Motes stay small. Does not deform the still.
    /// </summary>
    public sealed class IceParticles : MonoBehaviour
    {
        public const int BackLayer = 7;
        public const int FrontLayer = 8;
        const float WorldX = 80f;
        const float ViewH = 16f;
        const float MaxOnScreen = 0.009f;

        RenderTexture _rt;
        Camera _cam;
        GameObject _world;
        int _layer;
        readonly List<Material> _mats = new List<Material>();

        public static void Attach(Transform parent, List<GameObject> built, bool front)
        {
            if (parent == null) return;
            var go = new GameObject(front ? "icePxFront" : "icePxBack",
                typeof(RectTransform), typeof(RawImage), typeof(IceParticles));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.raycastTarget = false;
            img.color = Color.white;
            go.GetComponent<IceParticles>().Build(img, front);
            if (built != null) built.Add(go);
        }

        void Build(RawImage img, bool front)
        {
            HideFromMain();
            _layer = front ? FrontLayer : BackLayer;
            var origin = new Vector3(front ? WorldX + 40f : WorldX, 0f, 0f);
            _rt = new RenderTexture(720, 1280, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            _rt.Create();
            img.texture = _rt;

            _world = new GameObject(front ? "IcePxWorldF" : "IcePxWorldB");
            _world.layer = _layer;
            _world.transform.position = origin;

            if (front)
                ForegroundIce.Emit(_world.transform, _layer, _mats);
            else
            {
                AddSystem(_world.transform, _layer, _mats, "dust", UiSprites.Soft(),
                    38f, 170, 5.0f, 9.0f, 0.016f, 0.032f, 0.18f, -0.36f, 0.014f);
                AddSystem(_world.transform, _layer, _mats, "flakes", UiSprites.IceFlake(),
                    4.5f, 22, 4.6f, 8.0f, 0.030f, 0.052f, 0.30f, -0.50f, 0.018f);
                AddSystem(_world.transform, _layer, _mats, "floor", UiSprites.IceSpark(),
                    9f, 40, 2.2f, 4.6f, 0.016f, 0.036f, 0.42f, -0.10f, 0.00f,
                    0.20f, -5.1f, 10f, 5.2f, true);
            }

            var camGo = new GameObject(front ? "IcePxCamF" : "IcePxCamB");
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = ViewH * 0.5f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << _layer;
            _cam.targetTexture = _rt;
            _cam.depth = front ? -40 : -41;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 24f;
            _cam.enabled = false;
            camGo.transform.position = origin + new Vector3(0f, 0f, -12f);
            camGo.transform.LookAt(origin);
            AttachUrp(_cam);
            _cam.Render();
        }

        public static void AddSystem(Transform parent, int layer, List<Material> mats,
            string name, Sprite spr,
            float rate, int max, float life0, float life1,
            float size0, float size1, float alpha, float fall, float grav,
            float spreadX = 0.26f, float yOffset = 0f, float boxX = 10f, float boxY = 18f,
            bool twinkle = false)
        {
            if (parent == null) return;
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var rend = go.GetComponent<ParticleSystemRenderer>();
            var mat = MakeMat(spr != null ? spr.texture : Texture2D.whiteTexture);
            if (mat == null)
            {
                Object.Destroy(go);
                return;
            }
            if (mats != null) mats.Add(mat);
            rend.sharedMaterial = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.alignment = ParticleSystemRenderSpace.View;
            rend.minParticleSize = 0.0012f;
            rend.maxParticleSize = Mathf.Min(MaxOnScreen, size1 / ViewH * 1.2f);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.allowRoll = !twinkle;
            rend.velocityScale = 0f;
            rend.lengthScale = 1f;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.duration = 6f;
            main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life0, life1);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.startSize = new ParticleSystem.MinMaxCurve(size0, size1);
            main.startSize3D = false;
            main.startColor = new Color(0.78f, 0.94f, 1f, alpha);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.2832f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.gravityModifier = grav;
            main.useUnscaledTime = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;
            if (twinkle)
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 7, 0, 1.6f) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(boxX, boxY, 0.12f);
            shape.randomDirectionAmount = 0.10f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-spreadX, spreadX);
            vel.y = new ParticleSystem.MinMaxCurve(fall - 0.18f, fall + 0.10f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.72f, 0.90f, 1f), 1f)
                },
                twinkle
                    ? new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.08f),
                        new GradientAlphaKey(0.35f, 0.45f),
                        new GradientAlphaKey(1f, 0.62f),
                        new GradientAlphaKey(0f, 1f)
                    }
                    : new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.14f),
                        new GradientAlphaKey(0.80f, 0.72f),
                        new GradientAlphaKey(0f, 1f)
                    });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, twinkle
                ? new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(0.55f, 0.40f),
                    new Keyframe(1f, 0.15f))
                : new AnimationCurve(
                    new Keyframe(0f, 0.70f),
                    new Keyframe(0.30f, 1f),
                    new Keyframe(1f, 0.75f)));

            var rot = ps.rotationOverLifetime;
            rot.enabled = !twinkle;
            rot.separateAxes = false;
            rot.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = twinkle ? 0.08f : 0.16f;
            noise.frequency = 0.36f;
            noise.scrollSpeed = 0.14f;
            noise.octaveCount = 1;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            ps.Play(true);
        }

        void LateUpdate()
        {
            if (_cam != null) _cam.Render();
        }

        static Material MakeMat(Texture tex)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Resources.Load<Shader>("Shaders/ParticleAdd")
                     ?? Shader.Find("Resonance/Particles/Additive")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default");
            if (sh == null) return null;
            var m = new Material(sh);
            if (tex != null)
            {
                m.mainTexture = tex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Tint")) m.SetColor("_Tint", Color.white);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.One);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
            }
            if (m.HasProperty("_Blend"))
            {
                m.SetFloat("_Blend", 2f);
                m.EnableKeyword("_BLENDMODE_ADD");
            }
            m.SetInt("_ZWrite", 0);
            m.renderQueue = 3000;
            return m;
        }

        static void HideFromMain()
        {
            var main = Camera.main;
            if (main != null)
                main.cullingMask &= ~((1 << BackLayer) | (1 << FrontLayer) | (1 << Sprite2DStandee.WorldLayer));
        }

        static void AttachUrp(Camera cam)
        {
            var t = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (t == null || cam.GetComponent(t) != null) return;
            cam.gameObject.AddComponent(t);
        }

        void OnDestroy()
        {
            if (_cam != null) Destroy(_cam.gameObject);
            if (_world != null) Destroy(_world.gameObject);
            for (int i = 0; i < _mats.Count; i++)
                if (_mats[i] != null) Destroy(_mats[i]);
            _mats.Clear();
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
        }
    }
}
