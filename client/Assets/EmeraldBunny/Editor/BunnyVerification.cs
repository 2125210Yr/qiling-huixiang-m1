using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmeraldBunny.Editor
{
    public static class BunnyVerification
    {
        [Serializable] public class Report
        {
            public string status;
            public int bones,layers,vertices,frames,triangles;
            public float maxWeightError,maxBindError,maxFixedMotion,maxLoopError,minAreaRatio=1,maxAreaRatio=1,maxMotionPixels,maxHandContactError;
            public string visualReview="PENDING";
        }
        static void Require(bool condition,string why){if(!condition)throw new Exception(why);}
        public static void Run()
        {
            Directory.CreateDirectory(BunnyBuilder.Output);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BunnyBuilder.AssetRoot+"/EmeraldBunny.prefab");
            Require(prefab!=null,"Serialized prefab has not been built");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var rig=((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<BunnyMotion>();rig.autoPlay=false;
            var preview=BunnyBuilder.CreateScene(rig);preview.showPanel=false;rig.ResetPose();
            var report=new Report{bones=rig.bones.Length,layers=rig.parts.Length};
            var native=new Mesh();var rests=new List<Vector3[]>();var triangles=new List<int[]>();
            foreach(var part in rig.parts)
            {
                var mesh=part.renderer.sharedMesh;var rest=mesh.vertices;report.vertices+=rest.Length;report.triangles+=mesh.triangles.Length/3;
                Require(mesh.bindposes.Length==report.bones,"Missing real bind poses: "+part.id);
                foreach(var w in mesh.boneWeights)report.maxWeightError=Mathf.Max(report.maxWeightError,Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1));
                part.renderer.BakeMesh(native);var baked=native.vertices;
                for(int i=0;i<rest.Length;i++)report.maxBindError=Mathf.Max(report.maxBindError,Vector3.Distance(rest[i],baked[i]));
                rests.Add(baked);triangles.Add(mesh.triangles);
            }
            Capture(preview.view,"rest.png");
            var idleStart=new List<Vector3[]>();rig.Evaluate(0);foreach(var part in rig.parts){part.renderer.BakeMesh(native);idleStart.Add(native.vertices);}
            for(int frame=0;frame<=160;frame++)
            {
                float t=frame/20f;rig.Evaluate(t);CheckFrame(rig,native,rests,triangles,report);
            }
            rig.Evaluate(8);for(int j=0;j<rig.parts.Length;j++)
            {
                rig.parts[j].renderer.BakeMesh(native);var vertices=native.vertices;
                for(int i=0;i<vertices.Length;i++)report.maxLoopError=Mathf.Max(report.maxLoopError,Vector3.Distance(idleStart[j][i],vertices[i]));
            }
            foreach(int gesture in new[]{1,2,3,4})for(int f=0;f<=(gesture==4?48:36);f++)
            {rig.Evaluate(f/10f,gesture,f/10f);CheckFrame(rig,native,rests,triangles,report);}
            rig.Evaluate(2.1f);Capture(preview.view,"blink.png");
            rig.Evaluate(4,1,1.5f);Capture(preview.view,"response.png");
            rig.Evaluate(4,2,1.6f);Capture(preview.view,"leg-ankle.png");
            preview.SetBackground(1);Capture(preview.view,"black-background.png");preview.SetBackground(2);Capture(preview.view,"color-background.png");preview.SetBackground(0);
            bool pass=report.bones>=30&&report.maxWeightError<.0001f&&report.maxBindError<.0001f&&report.minAreaRatio>0&&report.maxFixedMotion<.0001f&&report.maxLoopError<.0001f&&report.maxMotionPixels>2&&report.maxHandContactError<.01f;
            report.status=pass?"NUMERICAL_PASS":"FAIL";
            File.WriteAllText(Path.Combine(BunnyBuilder.Output,"native-validation.json"),JsonUtility.ToJson(report,true));
            UnityEngine.Object.DestroyImmediate(native);
            Require(pass,"Native bone verification failed; see native-validation.json");
            rig.ResetPose();Debug.Log("BUNNY_VERIFY_PASS "+JsonUtility.ToJson(report));
        }
        static void CheckFrame(BunnyMotion rig,Mesh scratch,List<Vector3[]> rests,List<int[]> triangles,Report report)
        {
            report.frames++;
            for(int j=0;j<rig.parts.Length;j++)
            {
                var part=rig.parts[j];part.renderer.BakeMesh(scratch);var v=scratch.vertices;var rest=rests[j];
                bool fixedPart=part.region.StartsWith("chair")||part.region=="background"||part.region=="repair_chair";
                for(int i=0;i<v.Length;i++)
                {
                    Require(float.IsFinite(v[i].x)&&float.IsFinite(v[i].y),"Nonfinite vertex "+part.id);
                    float distance=Vector3.Distance(v[i],rest[i]);report.maxMotionPixels=Mathf.Max(report.maxMotionPixels,distance*100);
                    if(fixedPart)report.maxFixedMotion=Mathf.Max(report.maxFixedMotion,distance);
                }
                var tr=triangles[j];
                Require(v.Length==rest.Length,"Bake vertex count changed for "+part.id+": "+rest.Length+" -> "+v.Length);
                for(int i=0;i<tr.Length;i+=3)
                {
                    Require(tr[i]<rest.Length&&tr[i+1]<rest.Length&&tr[i+2]<rest.Length,"Invalid stored triangle in "+part.id+"; vertices="+rest.Length+" triangle="+tr[i]+","+tr[i+1]+","+tr[i+2]);
                    int a=tr[i],b=tr[i+1],c=tr[i+2];float before=Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).z;
                    float after=Vector3.Cross(v[b]-v[a],v[c]-v[a]).z;float ratio=after/before;
                    report.minAreaRatio=Mathf.Min(report.minAreaRatio,ratio);report.maxAreaRatio=Mathf.Max(report.maxAreaRatio,ratio);
                }
            }
            report.maxHandContactError=Mathf.Max(report.maxHandContactError,Vector3.Distance(rig.bones[15].position,rig.transform.TransformPoint(BunnyDefinition.Point(250,521))));
        }
        public static void Capture(Camera camera,string name)
        {
            // Batch editor Camera.Render can reuse a stale GPU bone palette inside one
            // executeMethod call. Render Unity's own BakeMesh output for synchronous evidence.
            var skinned=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var snapshots=new List<GameObject>();var bakedMeshes=new List<Mesh>();var enabled=new List<SkinnedMeshRenderer>();
            foreach(var source in skinned)if(source.enabled)
            {
                var mesh=new Mesh();source.BakeMesh(mesh);var go=new GameObject("Native baked capture");
                go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);go.transform.localScale=source.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
                renderer.sortingOrder=source.sortingOrder;var property=new MaterialPropertyBlock();source.GetPropertyBlock(property);renderer.SetPropertyBlock(property);
                source.enabled=false;enabled.Add(source);snapshots.Add(go);bakedMeshes.Add(mesh);
            }
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1024,1536,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.orthographicSize=7.68f;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1024,1536,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1024,1536),0,0);image.Apply();
            var pixels=image.GetPixels32();int low=255,high=0;
            for(int i=0;i<pixels.Length;i+=97){low=Math.Min(low,pixels[i].r);high=Math.Max(high,pixels[i].r);}
            Require(high-low>60,"Camera produced a blank frame: "+name);
            File.WriteAllBytes(Path.Combine(BunnyBuilder.Output,name),image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;
            UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
            foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in bakedMeshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var source in enabled)source.enabled=true;
        }
        public static void RenderSequence()
        {
            Directory.CreateDirectory(Path.Combine(BunnyBuilder.Output,"frames"));
            EditorSceneManager.OpenScene(BunnyBuilder.AssetRoot+"/EmeraldBunnyPreview.unity");
            var rig=UnityEngine.Object.FindFirstObjectByType<BunnyMotion>();var preview=UnityEngine.Object.FindFirstObjectByType<BunnyPreview>();rig.autoPlay=false;preview.showPanel=false;
            for(int i=0;i<288;i++)
            {
                float t=i/24f;int action=t>=8?2:0;rig.Evaluate(t,action,t-8);
                Capture(preview.view,"frames/frame-"+i.ToString("D4")+".png");
            }
            Debug.Log("BUNNY_SEQUENCE_PASS");
        }
    }
}
