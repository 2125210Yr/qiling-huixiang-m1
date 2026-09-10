using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// One uncut still on a SkinnedMeshRenderer. Shoulder/wrist/ankle/head are Unity bones.
    /// </summary>
    public sealed class PuppetRig : MonoBehaviour, IPointerDownHandler
    {
        public const int WorldLayer = 9;
        [Tooltip("勾选后停止自动摆姿势，可在层级里点骨骼用 E 旋转")]
        public bool freezePose;
        const float Ppu = 100f;
        const int GridX = 129;
        const int GridY = 193;
        static readonly Vector3 Origin = new Vector3(-240f, 0f, 0f);

        const int BoneRoot = 0;
        const int BoneChest = 1;
        const int BoneWrist = 2;
        const int BoneAnkleR = 3;
        const int BoneAnkleL = 4;
        const int BoneHead = 5;
        const int BonePelvis = 6;
        const int BoneThighR = 7;
        const int BoneKneeR = 8;
        const int BoneArmL = 9;
        const int BoneArmR = 10;

        RenderTexture _rt;
        Camera _cam;
        Transform _world;
        Transform _view;
        Mesh _bodyMesh;
        SkinnedMeshRenderer _smr;
        Vector3 _breastLP;
        Vector3 _chestC;
        Vector3 _headC;
        Vector3 _attachL;
        Vector3 _attachR;
        Vector3 _idleHip;
        Transform _rootBone;
        Transform _chestBone;
        Transform _headBone;
        Transform _wrist;
        Transform _armL;
        Transform _armR;
        Transform _ankleL;
        Transform _ankleR;
        Material _mouthMat;
        Texture _mouth0, _mouth1, _mouth2;
        readonly List<Material> _mats = new List<Material>();
        float _tapOffset;
        float _tapVelocity;
        float _headU = 0.51f;
        float _headV = 0.82f;
        float _chestU = 0.50f;
        float _chestV = 0.70f;
        float _handU = 0.43f;
        float _handV = 0.55f;
        float _footRU = 0.46f;
        float _footRV = 0.20f;
        float _footLU = 0.54f;
        float _footLV = 0.23f;
        float _hitU0, _hitU1, _hitV0, _hitV1;
        Vector3 _handC;
        Vector3 _shoulderL;
        Vector3 _shoulderR;
        Vector3 _footRC;
        Vector3 _footLC;
        bool _hasHead;
        bool _hasHair;
        bool _hasHand;
        bool _hasFoot;
        float _idleT;
        PuppetMotion _motion = new PuppetMotion();
        Vector3[] _restVertices, _motionVertices;
        Vector2[] _chestInfluence, _chestOuter, _hairInfluence;
        PuppetSoftBust _softBust;
        float _canvasWidth, _canvasHeight;
        Texture2D _blinkTexture;
        Texture2D _chestBounce;
        Shader _faceShader;
        bool _hasLegRig;
        Transform _legPelvis, _thighR, _kneeR;
        Vector3 _hipRest, _kneeRest, _ankleRest;
        static readonly float[] LegRows = { 570,620,740,840,900,970,1070,1135,1180,1230,1270,1310,1340 };
        static readonly float[] LegLeft = { 460,465,490,535,530,503,475,458,440,413,420,458,470 };
        static readonly float[] LegRight = { 602,589,602,615,622,578,549,534,505,484,489,481,472 };

        public Transform WorldRoot { get { return _world; } }

        public void RenderNow()
        {
            if (_cam != null) _cam.Render();
        }

        public void RevealBones()
        {
            if (_world == null) return;
            _world.gameObject.hideFlags = HideFlags.None;
            foreach (var t in _world.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.None;
        }

        public static bool TryAttach(Transform host, string id)
        {
            if (host == null || string.IsNullOrEmpty(id)) return false;
            PuppetDto dto;
            if (!CharacterArt.TryPuppet(id, out dto) || dto == null || dto.slots == null) return false;
            var fx = host.GetComponent<PuppetRig>();
            if (fx == null) fx = host.gameObject.AddComponent<PuppetRig>();
            if (!fx.Bind(id, dto))
            {
                if (fx != null) Object.Destroy(fx);
                return false;
            }
            return true;
        }

        bool Bind(string id, PuppetDto dto)
        {
            Release(false);
            Texture2D body = null;
            for (int i = 0; i < dto.slots.Length; i++)
                if (dto.slots[i] != null && dto.slots[i].id == "body")
                    body = CharacterArt.LoadPuppetTex(id, dto.slots[i].tex);
            if (body == null) return false;
            _blinkTexture = CharacterArt.LoadPuppetTex(id, "PuppetMotion/blink");
            _chestBounce = CharacterArt.LoadPuppetTex(id, "PuppetMotion/chest_bounce");
            _faceShader = Resources.Load<Shader>("Art/Characters/" + id + "/PuppetMotion/PuppetFace");

            var w = dto.canvasW > 0 ? dto.canvasW : body.width;
            var h = dto.canvasH > 0 ? dto.canvasH : body.height;
            var bodyW = w / Ppu;
            var bodyH = h / Ppu;
            _canvasWidth = bodyW;
            _canvasHeight = bodyH;
            _motion = new PuppetMotion();
            _hasLegRig = id == "C001";
            _hipRest = Uv(.520f,.595f,bodyW,bodyH);
            _kneeRest = Uv(.581f,.421f,bodyW,bodyH);
            _ankleRest = Uv(.470f,.255f,bodyW,bodyH);
            _headU = dto.headU > 0.01f ? dto.headU : 0.51f;
            _headV = dto.headV > 0.01f ? dto.headV : 0.82f;
            _chestU = dto.chestU > 0.01f ? dto.chestU : 0.50f;
            _chestV = dto.chestV > 0.01f ? dto.chestV : 0.70f;
            _handU = dto.handRU > 0.01f ? dto.handRU : 0.43f;
            _handV = dto.handRV > 0.01f ? dto.handRV : 0.55f;
            _footRU = dto.footRU > 0.01f ? dto.footRU : 0.46f;
            _footRV = dto.footRV > 0.01f ? dto.footRV : 0.20f;
            _footLU = dto.footLU > 0.01f ? dto.footLU : 0.54f;
            _footLV = dto.footLV > 0.01f ? dto.footLV : 0.23f;
            _hitU0 = 0.40f;
            _hitU1 = 0.68f;
            _hitV0 = 0.64f;
            _hitV1 = 0.78f;
            _chestC = Uv(_chestU, _chestV, bodyW, bodyH);
            _breastLP = Uv(0.520f, 0.778f, bodyW, bodyH);
            _attachL = Uv(0.455f, 0.758f, bodyW, bodyH);
            _attachR = Uv(0.580f, 0.752f, bodyW, bodyH);
            _idleHip = Uv(0.515f, 0.455f, bodyW, bodyH);
            _headC = Uv(_headU, _headV, bodyW, bodyH);
            _handC = Uv(_handU, _handV, bodyW, bodyH);
            _shoulderL = Uv(0.395f, 0.752f, bodyW, bodyH);
            _shoulderR = Uv(0.638f, 0.738f, bodyW, bodyH);
            _footRC = Uv(_footRU, _footRV, bodyW, bodyH);
            _footLC = Uv(_footLU, _footLV, bodyW, bodyH);
            _hasHead = false;
            _hasHair = false;
            _hasHand = false;
            _hasFoot = false;
            _idleT = 0f;
            for (int i = 0; i < dto.slots.Length; i++)
            {
                if (dto.slots[i] == null) continue;
                var sid = dto.slots[i].id;
                if (sid == "head") _hasHead = true;
                if (sid == "hair_back" || sid == "hair_front" || sid == "hair_side") _hasHair = true;
                if (sid == "hand_r" || sid == "sword") _hasHand = true;
                if (sid == "foot_l" || sid == "foot_r") _hasFoot = true;
            }

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
            _view = view.transform;

            var worldGo = new GameObject("Puppet_" + id);
            worldGo.layer = WorldLayer;
            worldGo.hideFlags = HideFlags.DontSave;
            _world = worldGo.transform;
            _world.position = Origin;
            _world.rotation = Quaternion.identity;
            _world.localScale = Vector3.one;
            BuildBodyGrid(w, h, bodyW, bodyH);
            BindSkinnedBody(body, bodyW, bodyH);

            var camGo = new GameObject("PuppetCam");
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

        public void OnPointerDown(PointerEventData e) { Punch(e); }

        public bool CloseArms()
        {
            _motion.TapChest(0f);
            return false;
        }

        public bool LiftRightLeg() { return _hasLegRig && _motion.StartLegLift(); }

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
            if (_bodyMesh == null) return;
            if (_hasLegRig && u>.40f && u<.65f && v>.12f && v<.58f)
            {
                LiftRightLeg();
                return;
            }
            if (v > .76f && v < .92f && u > .42f && u < .64f)
            {
                _motion.Look((u - _headU) / .065f);
                _motion.BlinkNow();
                return;
            }
            if (u >= _hitU0 && u <= _hitU1 && v >= _hitV0 && v <= _hitV1)
            {
                var side = u < _chestU ? -1f : 1f;
                _motion.TapChest(side);
            }
        }

        void LateUpdate()
        {
            HideFromOthers();
            if (!freezePose)
            {
                _idleT += Time.unscaledDeltaTime;
                _motion.Step(Time.unscaledDeltaTime);
                ApplySecondaryMesh();
                var breath = Mathf.Sin(_idleT * 0.72f);
                AdvanceSpring(ref _tapOffset, ref _tapVelocity, Time.unscaledDeltaTime);
                var bodySway = _motion.BodySway;
                var hairDeg = -bodySway * 0.55f + _motion.Gaze * .45f;
                var wristDeg = Mathf.Sin(_idleT * 0.46f) * 0.55f;
                var footR = (0.50f + 0.50f * Mathf.Sin(_idleT * 0.44f)) * 1.8f;
                var footL = (0.50f + 0.50f * Mathf.Sin(_idleT * 0.44f + 2.1f)) * 1.5f;
                ApplyBones(breath, 0f, wristDeg, footR, footL, hairDeg);
                ApplyMouth(0.5f + 0.5f * breath);
            }
            if (_cam != null) _cam.Render();
        }

        void OnDrawGizmos()
        {
            if (_world == null) return;
            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.9f);
            foreach (var t in _world.GetComponentsInChildren<Transform>(true))
            {
                if (t == _world || !t.name.StartsWith("bone_")) continue;
                Gizmos.DrawWireSphere(t.position, 0.08f);
                if (t.parent != null && t.parent != _world)
                    Gizmos.DrawLine(t.parent.position, t.position);
            }
        }

        // Exact solution of a damped spring: consistent at different frame rates.
        static void AdvanceSpring(ref float position, ref float velocity, float dt)
        {
            if (dt <= 0f) return;
            const float damping = 4f;
            const float frequency = 9f;
            var decay = Mathf.Exp(-damping * dt);
            var c = Mathf.Cos(frequency * dt);
            var sn = Mathf.Sin(frequency * dt);
            var oldPosition = position;
            position = decay * (oldPosition * c + (velocity + damping * oldPosition) / frequency * sn);
            velocity = decay * (velocity * c - (damping * velocity +
                (frequency * frequency + damping * damping) * oldPosition) / frequency * sn);
        }

        void ApplyBones(float inhale, float swayDeg, float wristDeg, float footRDeg, float footLDeg, float hairDeg)
        {
            if (_chestBone != null)
            {
                _chestBone.localRotation = Quaternion.identity;
                _chestBone.localScale = Vector3.one;
            }
            if (_armL != null)
                _armL.localRotation = Quaternion.identity;
            if (_armR != null)
                _armR.localRotation = Quaternion.identity;
            if (_wrist != null)
                _wrist.localRotation = Quaternion.Euler(0f, 0f, wristDeg);
            if (_ankleR != null)
                _ankleR.localRotation = Quaternion.Euler(0f, 0f, footRDeg);
            if (_ankleL != null)
                _ankleL.localRotation = Quaternion.Euler(0f, 0f, footLDeg);
            if (_headBone != null)
                _headBone.localRotation = Quaternion.Euler(0f, 0f, hairDeg);
            ApplyLegPose(_motion.LegLift);
        }

        void ApplyLegPose(float lift)
        {
            if (!_hasLegRig || _thighR == null) return;
            lift = Mathf.Clamp01(lift);
            // The tiny pelvic follow does not move the support-foot/root bone.
            var follow = new Vector3(_canvasWidth*.001f, _canvasHeight*.0015f,0)*lift;
            _legPelvis.localPosition=follow;
            var hip=_hipRest+follow;
            var target=_ankleRest + new Vector3(-_canvasWidth*.010f,_canvasHeight*.016f,0)*lift;
            var upper=_kneeRest-_hipRest; var lower=_ankleRest-_kneeRest;
            var solved=PuppetLegIK.Solve(hip.x,hip.y,target.x,target.y,upper.magnitude,lower.magnitude,1);
            var knee=new Vector3((float)solved.KneeX,(float)solved.KneeY,0);
            var ankle=new Vector3((float)solved.AnkleX,(float)solved.AnkleY,0);
            var hipAngle=Vector2.SignedAngle(upper,knee-hip);
            var shinAngle=Vector2.SignedAngle(lower,ankle-knee);
            _thighR.localRotation=Quaternion.Euler(0,0,hipAngle);
            _kneeR.localRotation=Quaternion.Euler(0,0,shinAngle-hipAngle);
            // Keep the boot rigid and its original world orientation.
            _ankleR.localRotation=Quaternion.Euler(0,0,-shinAngle);
        }

        static float FrontLegWeight(float u,float v)
        {
            var x=u*1024f; var y=(1-v)*1536f;
            if(y<LegRows[0] || y>LegRows[LegRows.Length-1])return 0;
            int row=0;
            while(row<LegRows.Length-2 && y>LegRows[row+1])row++;
            var t=Mathf.InverseLerp(LegRows[row],LegRows[row+1],y);
            var left=Mathf.Lerp(LegLeft[row],LegLeft[row+1],t);
            var right=Mathf.Lerp(LegRight[row],LegRight[row+1],t);
            var leftWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(left-60,left,x));
            // Keep visible knee fabric fully on the moving leg. Fade into the
            // transparent margin, not across the trouser highlights/outer seam.
            var kneeBand=Mathf.SmoothStep(0,1,Mathf.InverseLerp(730,830,y))
                * (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1000,1120,y)));
            right += 40f*kneeBand;
            var feather=y>1170 ? 8f : Mathf.Lerp(38f,70f,kneeBand);
            var rightWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(right,right+feather,x));
            var top=Mathf.SmoothStep(0,1,Mathf.InverseLerp(570,730,y));
            var bottom=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1305,1340,y));
            // The other boot is an explicit exclusion, not another moving ankle.
            var support= y>1190 ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(490,501,x)) : 0;
            return leftWeight*rightWeight*top*bottom*(1-support);
        }

        static float LeftArmWeight(float u, float v)
        {
            // Deltoid to wrist as one piece. Feather only into the torso, never
            // across the mid-sleeve — that pinch is the rejected clasp.
            return SoftBox(u, v, 0.338f, 0.432f, 0.505f, 0.758f, 0.028f);
        }

        static float RightArmWeight(float u, float v)
        {
            return SoftBox(u, v, 0.608f, 0.698f, 0.528f, 0.752f, 0.030f);
        }

        void ApplyMouth(float breath01)
        {
            if (_mouthMat == null || _mouth0 == null) return;
            Texture tex = _mouth0;
            if (breath01 > 0.82f && _mouth2 != null) tex = _mouth2;
            else if (breath01 > 0.62f && _mouth1 != null) tex = _mouth1;
            _mouthMat.mainTexture = tex;
            if (_mouthMat.HasProperty("_BaseMap")) _mouthMat.SetTexture("_BaseMap", tex);
            if (_mouthMat.HasProperty("_MainTex")) _mouthMat.SetTexture("_MainTex", tex);
        }

        void BindSkinnedBody(Texture tex, float bodyW, float bodyH)
        {
            _rootBone = Bone(_world, "bone_root");
            _rootBone.localPosition = Vector3.zero;
            _chestBone = Bone(_world, "bone_chest");
            _chestBone.localPosition = _breastLP;
            _armL = Bone(_world, "bone_armL");
            _armL.localPosition = _shoulderL;
            _wrist = Bone(_armL, "bone_wrist");
            _wrist.localPosition = _handC - _shoulderL;
            _armR = Bone(_world, "bone_armR");
            _armR.localPosition = _shoulderR;
            _legPelvis = Bone(_world, "bone_pelvis");
            _thighR = Bone(_legPelvis, "bone_thighR");
            _thighR.localPosition = _hipRest;
            _kneeR = Bone(_thighR, "bone_kneeR");
            _kneeR.localPosition = _kneeRest-_hipRest;
            _ankleR = Bone(_hasLegRig ? _kneeR : _world, "bone_ankleR");
            _ankleR.localPosition = _hasLegRig ? _ankleRest-_kneeRest : _footRC;
            _ankleL = Bone(_world, "bone_ankleL");
            _ankleL.localPosition = _footLC;
            _headBone = Bone(_world, "bone_head");
            _headBone.localPosition = _headC;

            var go = new GameObject("body_mesh");
            go.layer = WorldLayer;
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_world, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.quality = SkinQuality.Bone4;
            smr.updateWhenOffscreen = true;
            smr.skinnedMotionVectors = false;
            smr.forceMatrixRecalculationPerRender = true;
            smr.shadowCastingMode = ShadowCastingMode.Off;
            smr.receiveShadows = false;
            smr.lightProbeUsage = LightProbeUsage.Off;
            smr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            smr.allowOcclusionWhenDynamic = false;
            smr.sortingOrder = 1;
            var mat = _blinkTexture != null && _faceShader != null ? new Material(_faceShader) : MakeMat(tex);
            if (mat != null && _blinkTexture != null && _faceShader != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BlinkTex", _blinkTexture);
                mat.SetFloat("_Blink", 0f);
                if (_chestBounce != null)
                {
                    mat.SetTexture("_ChestTex", _chestBounce);
                    mat.SetVector("_ChestRect", new Vector4(0.45703125f, 0.66927083f, 0.23046875f, 0.1171875f));
                    mat.SetFloat("_ChestL", 0f);
                    mat.SetFloat("_ChestR", 0f);
                }
            }
            if (mat == null)
            {
                Object.Destroy(go);
                return;
            }
            smr.sharedMaterial = mat;
            _mats.Add(mat);
            var bones = new[] { _rootBone, _chestBone, _wrist, _ankleR, _ankleL, _headBone, _legPelvis, _thighR, _kneeR, _armL, _armR };
            var bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                bind[i] = bones[i].worldToLocalMatrix * go.transform.localToWorldMatrix;
            _bodyMesh.bindposes = bind;
            smr.sharedMesh = _bodyMesh;
            smr.bones = bones;
            smr.rootBone = _rootBone;
            smr.localBounds = new Bounds(
                new Vector3(0f, bodyH * 0.5f, 0f),
                new Vector3(bodyW, bodyH, 0.4f));
            _smr = smr;
        }

        void BuildBodyGrid(int texW, int texH, float bodyW, float bodyH)
        {
            var n = GridX * GridY;
            var verts = new Vector3[n];
            var uv = new Vector2[n];
            var nrm = new Vector3[n];
            var bw = new BoneWeight[n];
            var blade = Uv(0.28f, 0.30f, bodyW, bodyH);
            int i = 0;
            for (int iy = 0; iy < GridY; iy++)
            {
                var ty = iy / (float)(GridY - 1);
                for (int ix = 0; ix < GridX; ix++)
                {
                    var tx = ix / (float)(GridX - 1);
                    var p = Uv(tx, 1f - ty, bodyW, bodyH);
                    verts[i] = p;
                    uv[i] = new Vector2(tx, 1f - ty);
                    nrm[i] = new Vector3(0f, 0f, -1f);
                    var u = uv[i].x;
                    var v = uv[i].y;
                    var wc = BustGate(u, v) * Mathf.Max(
                        SoftEllipse(u, v, 0.462f, 0.715f, 0.048f, 0.046f, 0.032f),
                        SoftEllipse(u, v, 0.575f, 0.710f, 0.082f, 0.046f, 0.032f));
                    var ww = 0f;
                    if (u < 0.50f && v < 0.62f && v > 0.10f)
                        ww = Mathf.Max(Falloff(p, _handC, 1.15f, 1.55f), Falloff(p, blade, 1.35f, 1.75f));
                    var war = 0f;
                    if (v < 0.30f && u > 0.36f && u < 0.51f)
                        war = Falloff(p, _footRC, 0.85f, 1.05f);
                    var wal = 0f;
                    if (v < 0.32f && u > 0.49f && u < 0.66f)
                        wal = Falloff(p, _footLC, 0.85f, 1.05f);
                    var wh = 0f;
                    var torso = SoftBox(u, v, 0.38f, 0.72f, 0.32f, 0.76f, 0.035f);
                    wh = Falloff(p, _headC, 4.2f, 5.5f) * 0.85f * (1f - torso);
                    var face = SoftBox(u, v, 0.44f, 0.60f, 0.79f, 0.90f, 0.025f);
                    wh = Mathf.Max(wh, Falloff(p, _headC, 1.1f, 0.9f) * face);
                    if (_hasLegRig) war=0f;
                    ww = Mathf.Clamp01(ww);
                    war = Mathf.Clamp01(war);
                    wal = Mathf.Clamp01(wal);
                    ww *= 1f - wc;
                    wh *= (1f - ww) * (1f - war) * (1f - wal) * (1f - wc);
                    var wRoot = 1f - Mathf.Clamp01(wc + ww + war + wal + wh);
                    bw[i] = PackWeights(wRoot, wc, ww, war, wal, wh);
                    if (_hasLegRig)
                    {
                        var leg=FrontLegWeight(u,v);
                        if(leg>0)
                        {
                            // Spread the hinge blend over the lower thigh and upper calf;
                            // a narrow horizontal band stretches the knee-back folds.
                            var thigh=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.49f,v));
                            var boot=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.235f,.280f,v));
                            bw[i]=new BoneWeight {
                                boneIndex0=BoneRoot,weight0=1-leg,
                                boneIndex1=BoneThighR,weight1=leg*thigh*(1-boot),
                                boneIndex2=BoneKneeR,weight2=leg*(1-thigh)*(1-boot),
                                boneIndex3=BoneAnkleR,weight3=leg*boot
                            };
                        }
                        var armL = LeftArmWeight(u, v) * (1f - wc);
                        var armR = RightArmWeight(u, v) * (1f - wc);
                        if (armL > 0.02f)
                        {
                            // Whole sleeve is one rigid piece on the shoulder bone.
                            // The wrist is parented to that shoulder, so the hand/sword
                            // follow the same rotation instead of pinching at the elbow.
                            var handness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.50f, v));
                            bw[i] = new BoneWeight
                            {
                                boneIndex0 = BoneRoot, weight0 = 1f - armL,
                                boneIndex1 = BoneArmL, weight1 = armL * (1f - handness),
                                boneIndex2 = BoneWrist, weight2 = armL * handness
                            };
                        }
                        else if (armR > 0.02f)
                        {
                            bw[i] = new BoneWeight
                            {
                                boneIndex0 = BoneRoot, weight0 = 1f - armR,
                                boneIndex1 = BoneArmR, weight1 = armR
                            };
                        }
                    }
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
                    tris[t++] = b;
                    tris[t++] = c;
                    tris[t++] = b;
                    tris[t++] = d;
                    tris[t++] = c;
                }
            }
            if (_hasLegRig)
            {
                PuppetBootTopology.Split(ref verts,ref uv,ref bw,ref tris);
                n=verts.Length;
                nrm=new Vector3[n];
                for(int k=0;k<n;k++)nrm[k]=new Vector3(0,0,-1);
            }
            if (_bodyMesh != null) Object.Destroy(_bodyMesh);
            _bodyMesh = new Mesh();
            if(n>65535)_bodyMesh.indexFormat=IndexFormat.UInt32;
            _bodyMesh.name = "puppet-body";
            _restVertices = verts;
            _motionVertices = (Vector3[])verts.Clone();
            _chestInfluence = new Vector2[n];
            _chestOuter = new Vector2[n];
            _hairInfluence = new Vector2[n];
            for (int k = 0; k < n; k++)
            {
                var u = uv[k].x; var v = uv[k].y;
                var gate = BustGate(u, v);
                var wl = SoftEllipse(u, v, 0.458f, 0.718f, 0.062f, 0.052f, 0.028f) * gate;
                var wr = SoftEllipse(u, v, 0.582f, 0.712f, 0.095f, 0.054f, 0.028f) * gate;
                _chestInfluence[k] = new Vector2(wl, wr);
                // Outer/lower tissue moves more; inner-upper attach stays. Not a fluid slab.
                _chestOuter[k] = new Vector2(
                    wl * Mathf.Clamp01(0.40f * Mathf.InverseLerp(0.505f, 0.428f, u)
                        + 0.60f * Mathf.InverseLerp(0.752f, 0.678f, v)),
                    wr * Mathf.Clamp01(0.40f * Mathf.InverseLerp(0.518f, 0.655f, u)
                        + 0.60f * Mathf.InverseLerp(0.752f, 0.678f, v)));
                var left = SoftBox(u,v,.09f,.33f,.30f,.69f,.06f);
                var right = SoftBox(u,v,.72f,.91f,.32f,.61f,.065f);
                var bladeU = Mathf.Lerp(.238f,.42f,Mathf.InverseLerp(.15f,.54f,v));
                var swordGuard = SoftBox(u,v,bladeU-.035f,bladeU+.035f,.13f,.57f,.025f);
                var tail = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.77f,.30f,v));
                _hairInfluence[k] = new Vector2(left*(1-swordGuard)*tail, right*tail);
            }
            _bodyMesh.MarkDynamic();
            _bodyMesh.vertices = verts;
            _bodyMesh.normals = nrm;
            _bodyMesh.uv = uv;
            _bodyMesh.triangles = tris;
            _bodyMesh.boneWeights = bw;
            _bodyMesh.RecalculateBounds();
            _softBust = new PuppetSoftBust();
            if (!_softBust.Bind(_restVertices, _chestInfluence, _chestOuter, _canvasWidth, _canvasHeight))
                _softBust = null;
        }

        void ApplySecondaryMesh()
        {
            if (_bodyMesh == null || _restVertices == null) return;
            for (int i=0; i<_restVertices.Length; i++)
            {
                var rest = _restVertices[i];
                var bodySway = _motion.BodySway;
                var p = RotZ(rest, _idleHip, bodySway);
                var chest = _chestInfluence[i];
                var hair = _hairInfluence[i];
                var wl = chest.x;
                var wr = chest.y;
                var oL = _chestOuter != null ? _chestOuter[i].x : wl;
                var oR = _chestOuter != null ? _chestOuter[i].y : wr;
                var bl = _motion.ChestLeft;
                var br = _motion.ChestRight;
                var wPendL = 0.25f * wl + 0.75f * oL;
                var wPendR = 0.25f * wr + 0.75f * oR;
                var aLPivot = RotZ(_attachL, _idleHip, bodySway);
                var aRPivot = RotZ(_attachR, _idleHip, bodySway);
                var u = rest.x / Mathf.Max(0.001f, _canvasWidth) + 0.5f;
                var v = rest.y / Mathf.Max(0.001f, _canvasHeight);
                var gap = SoftBox(u, v, 0.486f, 0.544f, 0.692f, 0.772f, 0.016f);
                var flesh = 1f - gap;
                const float springToDeg = 14f / 0.006f;
                if (wPendL > 0.001f)
                    p = Vector3.Lerp(p, RotZ(p, aLPivot, bl * springToDeg * flesh), wPendL);
                if (wPendR > 0.001f)
                    p = Vector3.Lerp(p, RotZ(p, aRPivot, br * springToDeg * flesh), wPendR);
                p.x += (hair.x * (_motion.HairLeft * .35f + _motion.HairLeftTip * .65f)
                    + hair.y * (_motion.HairRight * .35f + _motion.HairRightTip * .65f)) * _canvasWidth * .0032f;
                p.y += (hair.x * _motion.HairLeftTip + hair.y * _motion.HairRightTip) * _canvasHeight * .00035f;
                _motionVertices[i] = p;
            }
            _bodyMesh.vertices = _motionVertices;
            if (_smr != null && _smr.sharedMaterial != null)
            {
                var mat = _smr.sharedMaterial;
                mat.SetFloat("_Blink", _motion.Blink);
                if (mat.HasProperty("_ChestL"))
                {
                    mat.SetFloat("_ChestL", 0f);
                    mat.SetFloat("_ChestR", 0f);
                }
            }
        }

        static BoneWeight PackWeights(float wRoot, float wChest, float wWrist, float wAnkleR, float wAnkleL, float wHead)
        {
            var idx = new[] { BoneRoot, BoneChest, BoneWrist, BoneAnkleR, BoneAnkleL, BoneHead };
            var w = new[] { wRoot, wChest, wWrist, wAnkleR, wAnkleL, wHead };
            for (int k = 0; k < w.Length; k++)
                w[k] = float.IsNaN(w[k]) || float.IsInfinity(w[k]) ? 0f : Mathf.Clamp01(w[k]);
            for (int a = 0; a < idx.Length - 1; a++)
            {
                for (int b = a + 1; b < idx.Length; b++)
                {
                    if (w[b] <= w[a]) continue;
                    var ti = idx[a];
                    idx[a] = idx[b];
                    idx[b] = ti;
                    var tw = w[a];
                    w[a] = w[b];
                    w[b] = tw;
                }
            }
            var sum = w[0] + w[1] + w[2] + w[3];
            if (sum < 0.0001f)
                return new BoneWeight { boneIndex0 = BoneRoot, weight0 = 1f };
            var inv = 1f / sum;
            return new BoneWeight
            {
                boneIndex0 = idx[0],
                weight0 = w[0] * inv,
                boneIndex1 = idx[1],
                weight1 = w[1] * inv,
                boneIndex2 = idx[2],
                weight2 = w[2] * inv,
                boneIndex3 = idx[3],
                weight3 = w[3] * inv
            };
        }

        static Vector3 Uv(float u, float v, float bodyW, float bodyH)
        {
            return new Vector3((u - 0.5f) * bodyW, v * bodyH, 0f);
        }

        static Vector3 RotZ(Vector3 p, Vector3 c, float deg)
        {
            var d = p - c;
            var rad = deg * Mathf.Deg2Rad;
            var cs = Mathf.Cos(rad);
            var sn = Mathf.Sin(rad);
            return c + new Vector3(d.x * cs - d.y * sn, d.x * sn + d.y * cs, 0f);
        }

        static float Falloff(Vector3 p, Vector3 c, float rx, float ry)
        {
            var d = p - c;
            var nx = d.x / Mathf.Max(0.001f, rx);
            var ny = d.y / Mathf.Max(0.001f, ry);
            var r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 1f) return 0f;
            if (r <= 0.22f) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 1f, r));
        }

        static float SoftBox(float u, float v, float u0, float u1, float v0, float v1, float feather)
        {
            if (u < u0 - feather || u > u1 + feather || v < v0 - feather || v > v1 + feather)
                return 0f;
            var wu = 1f;
            if (u < u0) wu = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(u0 - feather, u0, u));
            else if (u > u1) wu = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(u1, u1 + feather, u));
            var wv = 1f;
            if (v < v0) wv = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(v0 - feather, v0, v));
            else if (v > v1) wv = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(v1, v1 + feather, v));
            return wu * wv;
        }

        static float BustGate(float u, float v)
        {
            // Pin waist, abdomen, neck and the left shoulder.
            if (v < 0.668f || v > 0.775f || u < 0.418f || u > 0.690f)
                return 0f;
            var g = 1f;
            if (u < 0.445f) g *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.418f, 0.445f, u));
            if (v < 0.688f) g *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.668f, 0.688f, v));
            if (v > 0.758f) g *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.758f, 0.775f, v));
            return g;
        }

        static float SoftEllipse(float u, float v, float cu, float cv, float ru, float rv, float feather)
        {
            var nx = (u - cu) / Mathf.Max(0.001f, ru);
            var ny = (v - cv) / Mathf.Max(0.001f, rv);
            var r = Mathf.Sqrt(nx * nx + ny * ny);
            var frel = feather / Mathf.Max(0.001f, Mathf.Min(ru, rv));
            if (r <= 1f) return 1f;
            if (r >= 1f + frel) return 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 1f + frel, r));
        }

        static Transform Bone(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.layer = WorldLayer;
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
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
            if (m.HasProperty("_CullMode")) m.SetFloat("_CullMode", 0f);
            m.SetInt("_Cull", 0);
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

        void OnDestroy() { Release(true); }

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
            if (_bodyMesh != null)
            {
                Object.Destroy(_bodyMesh);
                _bodyMesh = null;
            }
            for (int i = 0; i < _mats.Count; i++)
                if (_mats[i] != null) Object.Destroy(_mats[i]);
            _mats.Clear();
            _restVertices = _motionVertices = null;
            _chestInfluence = _chestOuter = _hairInfluence = null;
            _softBust = null;
            _smr = null;
            _rootBone = _chestBone = _headBone = null;
            _wrist = _armL = _armR = _ankleL = _ankleR = null;
            _legPelvis = _thighR = _kneeR = null;
            _mouthMat = null;
            _blinkTexture = null;
            _chestBounce = null;
            _faceShader = null;
            _tapOffset = _tapVelocity = 0f;
        }
    }

    [System.Serializable]
    public sealed class PuppetDto
    {
        public string id;
        public string skeleton;
        public int canvasW;
        public int canvasH;
        public float headU;
        public float headV;
        public float chestU;
        public float chestV;
        public float handRU;
        public float handRV;
        public float footRU;
        public float footRV;
        public float footLU;
        public float footLV;
        public PuppetSlotDto[] slots;
    }

    [System.Serializable]
    public sealed class PuppetSlotDto
    {
        public string id;
        public string tex;
        public int order;
        public bool mesh;
    }
}
