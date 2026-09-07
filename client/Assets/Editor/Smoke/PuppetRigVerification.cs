using System;
using System.IO;
using System.Reflection;
using Resonance.App;
using UnityEditor;
using UnityEngine;
public static class PuppetRigVerification
{
    public static void RunClasp()
    {
        var output=@"F:\天命之子\codex专区\puppet-rig-fix\clasp-preview";
        Directory.CreateDirectory(output);
        var host=new GameObject("ClaspVerification",typeof(RectTransform));
        if(!PuppetRig.TryAttach(host.transform,"C001"))throw new Exception("Bind failed");
        var rig=host.GetComponent<PuppetRig>();const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;
        var type=typeof(PuppetRig);var field=type.GetField("_motion",f);
        var motion=(PuppetMotion)field.GetValue(rig);var baseline=new PuppetMotion();
        var renderer=(SkinnedMeshRenderer)type.GetField("_smr",f).GetValue(rig);
        var camera=(Camera)type.GetField("_cam",f).GetValue(rig);
        var secondary=type.GetMethod("ApplySecondaryMesh",f);var apply=type.GetMethod("ApplyBones",f);
        var rest=renderer.sharedMesh.vertices;var uv=renderer.sharedMesh.uv;var triangles=renderer.sharedMesh.triangles;
        var baked=new Mesh();var reference=new Mesh();float minimum=1,clothMin=1,clothMax=1,pinError=0,maxMove=0;Vector2 minimumUv=Vector2.zero;
        for(int frame=0;frame<165;frame++)
        {
            if(frame==15 && (!rig.CloseArms() || rig.CloseArms()))throw new Exception("Clasp trigger failed");
            if(frame==48){motion.StartLegLift();baseline.StartLegLift();}
            motion.Step(1.0/30);baseline.Step(1.0/30);
            float t=(frame+1)/30f;type.GetField("_idleT",f).SetValue(rig,t);
            var args=new object[]{Mathf.Sin(t*.72f),0f,Mathf.Sin(t*.46f)*1.05f,(.5f+.5f*Mathf.Sin(t*.44f))*1.8f,(.5f+.5f*Mathf.Sin(t*.44f+2.1f))*1.5f,-Mathf.Sin(t*.52f)*1.15f*.40f};
            field.SetValue(rig,baseline);secondary.Invoke(rig,null);apply.Invoke(rig,args);renderer.BakeMesh(reference);var neutral=reference.vertices;
            field.SetValue(rig,motion);secondary.Invoke(rig,null);apply.Invoke(rig,args);renderer.BakeMesh(baked);var moved=baked.vertices;
            for(int k=0;k<uv.Length;k++)
            {
                float delta=Vector3.Distance(moved[k],neutral[k]);maxMove=Mathf.Max(maxMove,delta);
                if(uv[k].y>.78f || uv[k].y<.49f)pinError=Mathf.Max(pinError,delta);
            }
            for(int k=0;k<triangles.Length;k+=3)
            {
                int a=triangles[k],b=triangles[k+1],c=triangles[k+2];
                float area=Vector3.Cross(moved[b]-moved[a],moved[c]-moved[a]).z;
                float ratio=area/Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).z;
                if(float.IsNaN(ratio)||ratio<=0)throw new Exception("Fold at frame "+frame+" triangle "+k);
                minimum=Mathf.Min(minimum,ratio);
                var center=(uv[a]+uv[b]+uv[c])/3;
                if(center.x>.41f && center.x<.70f && center.y>.66f && center.y<.78f)
                {
                    float relative=area/Vector3.Cross(neutral[b]-neutral[a],neutral[c]-neutral[a]).z;
                    if(relative<clothMin){clothMin=relative;minimumUv=center;}clothMax=Mathf.Max(clothMax,relative);
                }
            }
            if(frame%2!=0)continue;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,"frame-"+(frame/2).ToString("D3")+".png"),image.EncodeToPNG());
            RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);
        }
        if(motion.ClaspActive || motion.ClaspAmount!=0 || motion.ArmClosure!=0)throw new Exception("Clasp did not return");
        if(pinError>.0001f || clothMin<.60f || clothMax>1.5f)throw new Exception("Cloth constraint: "+clothMin+".."+clothMax+" at="+minimumUv+" pin="+pinError);
        File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: 165 combined idle/clasp/leg poses; min triangle ratio="+minimum+"; relative cloth area="+clothMin+".."+clothMax+"; collar/lower-body differential="+pinError+"; max clasp displacement pixels="+(maxMove*100)+"; exact action return.");
        Debug.Log("PUPPET_CLASP_PASS");
    }

    public static void RunLeg()
    {
        var output=@"F:\天命之子\codex专区\puppet-rig-fix\leg-final-preview";
        Directory.CreateDirectory(output);
        var host=new GameObject("LegVerification",typeof(RectTransform));
        if(!PuppetRig.TryAttach(host.transform,"C001"))throw new Exception("Bind failed");
        var rig=host.GetComponent<PuppetRig>();const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var type=typeof(PuppetRig);var motion=(PuppetMotion)type.GetField("_motion",flags).GetValue(rig);
        var renderer=(SkinnedMeshRenderer)type.GetField("_smr",flags).GetValue(rig);
        var camera=(Camera)type.GetField("_cam",flags).GetValue(rig);
        var thigh=(Transform)type.GetField("_thighR",flags).GetValue(rig);
        var knee=(Transform)type.GetField("_kneeR",flags).GetValue(rig);
        var ankle=(Transform)type.GetField("_ankleR",flags).GetValue(rig);
        var support=(Transform)type.GetField("_ankleL",flags).GetValue(rig);
        var supportRest=support.position;var ankleRest=ankle.position;
        var upperLength=Vector3.Distance(thigh.position,knee.position);var lowerLength=Vector3.Distance(knee.position,ankle.position);
        var apply=type.GetMethod("ApplyBones",flags);var secondary=type.GetMethod("ApplySecondaryMesh",flags);
        var rest=renderer.sharedMesh.vertices;var tris=renderer.sharedMesh.triangles;var uv=renderer.sharedMesh.uv;
        int supportVertex=0;float nearest=float.MaxValue;
        for(int i=0;i<uv.Length;i++){var d=(uv[i]-new Vector2(.52f,.10f)).sqrMagnitude;if(d<nearest){nearest=d;supportVertex=i;}}
        float minimum=1,maxLift=0,maxBoneError=0,maxSupportError=0; Vector2 compressedUv=Vector2.zero;var baked=new Mesh();
        if(!rig.LiftRightLeg() || rig.LiftRightLeg())throw new Exception("Trigger/retrigger failed");
        for(int sample=0;sample<135;sample++)
        {
            motion.Step(1.0/30);secondary.Invoke(rig,null);
            apply.Invoke(rig,new object[]{motion.Breath,motion.Breath*.65f,0f,0f,0f,0f});
            maxBoneError=Mathf.Max(maxBoneError,Mathf.Abs(Vector3.Distance(thigh.position,knee.position)-upperLength),Mathf.Abs(Vector3.Distance(knee.position,ankle.position)-lowerLength));
            maxSupportError=Mathf.Max(maxSupportError,Vector3.Distance(support.position,supportRest));
            maxLift=Mathf.Max(maxLift,ankle.position.y-ankleRest.y);
            renderer.BakeMesh(baked);var verts=baked.vertices;
            maxSupportError=Mathf.Max(maxSupportError,Vector3.Distance(verts[supportVertex],rest[supportVertex]));
            for(int i=0;i<tris.Length;i+=3)
            {
                int a=tris[i],b=tris[i+1],c=tris[i+2];
                var ratio=Vector3.Cross(verts[b]-verts[a],verts[c]-verts[a]).z/Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).z;
                if(ratio<minimum){minimum=ratio;compressedUv=(uv[a]+uv[b]+uv[c])/3;}
                if(float.IsNaN(ratio)||ratio<=0)throw new Exception("Fold at sample "+sample+" triangle "+i+" ratio "+ratio);
            }
            if(sample%2!=0)continue;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,"frame-"+(sample/2).ToString("D3")+".png"),image.EncodeToPNG());
            RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);
        }
        if(maxBoneError>.0001f || maxSupportError>.0001f || maxLift<.2f)throw new Exception("Leg invariant failure");
        if(motion.LegActive || motion.LegLift!=0 || Vector3.Distance(ankle.position,ankleRest)>.0001f)throw new Exception("Rest pose not restored");
        File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: 135 poses; minimum triangle area="+minimum+" at UV "+compressedUv+"; bone error="+maxBoneError+"; support error="+maxSupportError+"; ankle lift pixels="+(maxLift*100)+"; exact return to rest.");
        Debug.Log("PUPPET_LEG_PASS");
    }

    public static void RunMotion()
    {
        var output = @"F:\天命之子\codex专区\puppet-rig-fix\motion-face-preview";
        Directory.CreateDirectory(output);
        var host = new GameObject("MotionVerification", typeof(RectTransform));
        if (!PuppetRig.TryAttach(host.transform,"C001")) throw new Exception("Bind failed");
        var rig=host.GetComponent<PuppetRig>();
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var type=typeof(PuppetRig);
        var motion=(PuppetMotion)type.GetField("_motion",flags).GetValue(rig);
        var secondary=type.GetMethod("ApplySecondaryMesh",flags);
        var apply=type.GetMethod("ApplyBones",flags);
        var renderer=(SkinnedMeshRenderer)type.GetField("_smr",flags).GetValue(rig);
        var camera=(Camera)type.GetField("_cam",flags).GetValue(rig);
        if(renderer.sharedMaterial.shader.name != "Resonance/PuppetFace") throw new Exception("Blink shader unavailable");
        var rest=renderer.sharedMesh.vertices;var triangles=renderer.sharedMesh.triangles;
        var baked=new Mesh();float minimum=1;
        for(int sample=0;sample<240;sample++)
        {
            if(sample==30)motion.TapChest(-1);
            if(sample==75)motion.TapChest(1);
            if(sample==110){motion.Look(1);motion.BlinkNow();}
            if(sample==155)motion.Look(-1);
            if(sample==195)motion.Look(0);
            motion.Step(1.0/30);
            secondary.Invoke(rig,null);
            apply.Invoke(rig,new object[]{motion.Breath,motion.Breath*.65f,0f,0f,0f,motion.Gaze*.45f});
            renderer.BakeMesh(baked);var verts=baked.vertices;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                var ratio=Vector3.Cross(verts[b]-verts[a],verts[c]-verts[a]).z / Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).z;
                if(float.IsNaN(ratio)||ratio<=0)throw new Exception("Fold at frame "+sample);
                minimum=Mathf.Min(minimum,ratio);
            }
            if(sample%2!=0 && sample!=113)continue;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,(sample==113 ? "blink-close" : "frame-"+(sample/2).ToString("D3"))+".png"),image.EncodeToPNG());
            RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);
        }
        File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: 240 actual skinned poses, no folds, minimum area ratio="+minimum+". Hair/chest/gaze only; blink shader active; no validated sword yet.");
        Debug.Log("PUPPET_MOTION_PASS "+minimum);
    }

    public static void Run()
    {
        var output = @"F:\天命之子\codex专区\puppet-rig-fix";
        var host = new GameObject("RigVerification", typeof(RectTransform));
        if (!PuppetRig.TryAttach(host.transform, "C001")) throw new Exception("Bind failed");
        var rig = host.GetComponent<PuppetRig>();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var apply = typeof(PuppetRig).GetMethod("ApplyBones", flags);
        var renderer = (SkinnedMeshRenderer)typeof(PuppetRig).GetField("_smr", flags).GetValue(rig);
        var camera = (Camera)typeof(PuppetRig).GetField("_cam", flags).GetValue(rig);
        if (renderer == null || camera == null) throw new Exception("Renderer missing");
        var baked = new Mesh();
        var original = renderer.sharedMesh.vertices;
        var triangles = renderer.sharedMesh.triangles;
        var minimumRatio = float.MaxValue;
        for (int sample = 0; sample <= 120; sample++)
        {
            var t = sample / 15f;
            var breath = Mathf.Sin(t * 1.05f);
            apply.Invoke(rig, new object[] { breath, .65f * breath + .6f * Mathf.Sin(t * 9f) * Mathf.Exp(-4*t), 0f, 0f, 0f, .45f * Mathf.Sin(t * .42f) });
            renderer.BakeMesh(baked);
            var vertices = baked.vertices;
            for (int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i], b=triangles[i+1], c=triangles[i+2];
                var rest=Vector3.Cross(original[b]-original[a],original[c]-original[a]).z;
                var moved=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).z;
                var ratio=moved/rest;
                if(float.IsNaN(ratio) || ratio<=0) throw new Exception("Flipped triangle at sample " + sample);
                minimumRatio=Mathf.Min(minimumRatio,ratio);
            }
            if (sample==0 || sample==24 || sample==72)
            {
                camera.Render();
                var previous=RenderTexture.active;
                RenderTexture.active=camera.targetTexture;
                var image=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,image.width,image.height),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(output,"unity-pose-"+sample+".png"),image.EncodeToPNG());
                RenderTexture.active=previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
        File.WriteAllText(Path.Combine(output,"unity-verification.txt"),"PASS: C001 binds; 121 sampled poses; no triangle flips. Minimum area ratio: " + minimumRatio + ". Captured poses 0,24,72. This does not certify layered animation or aesthetic quality.");
        Debug.Log("PUPPET_VERIFICATION_PASS minimum area ratio="+minimumRatio);
    }
}
