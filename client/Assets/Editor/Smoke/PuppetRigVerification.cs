using System;
using System.IO;
using System.Reflection;
using Resonance.App;
using UnityEditor;
using UnityEngine;
public static class PuppetRigVerification
{
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
