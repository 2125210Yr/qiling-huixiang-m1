using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmeraldBunny.Editor
{
    public static class BunnyStretchVerification
    {
        [Serializable] public class Report
        {
            public string status;
            public float rearFootLiftPixels,frontFootLiftPixels,rearToeSpreadPixels,frontToeSpreadPixels,returnPoseErrorPixels,minToeAreaRatio=1;
            public float closedToeSpreadPixels,nativeClipPoseErrorPixels;
            public float disabledToeTipMotionPixels;
            public int independentToeParts;
            public string worstToe;
            public float worstAmount,worstTime;
            public Vector2 worstPixel;
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BunnyBuilder.AssetRoot+"/EmeraldBunny.prefab");
            var rig=((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<BunnyMotion>();rig.autoPlay=false;
            Require(rig.bones.Length>=52,"Missing dedicated foot-lift bones: toe bending alone does not meet the stretch sequence.");
            var preview=BunnyBuilder.CreateScene(rig);preview.showPanel=false;
            var report=new Report();foreach(var part in rig.parts)if(part.region.StartsWith("spread_"))report.independentToeParts++;
            Require(report.independentToeParts==10,"Ten independently separated toe meshes are required for opening gaps.");
            rig.Evaluate(0);var rearRest=rig.bones[8].position;var frontRest=rig.bones[12].position;
            BunnyVerification.Capture(preview.view,"stretch-rest.png");
            rig.Evaluate(0,4,1.0f);
            report.rearFootLiftPixels=(rig.bones[8].position.y-rearRest.y)*100;
            report.frontFootLiftPixels=(rig.bones[12].position.y-frontRest.y)*100;
            BunnyVerification.Capture(preview.view,"stretch-lift.png");
            rig.Evaluate(0,4,2.0f);
            report.rearToeSpreadPixels=Spread(rig,40,44);
            report.frontToeSpreadPixels=Spread(rig,45,49);
            BunnyVerification.Capture(preview.view,"stretch-open.png");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(BunnyBuilder.AssetRoot+"/Generated/Animations/FootStretch.anim");
            Require(clip!=null&&Mathf.Abs(clip.length-4.8f)<.001f,"Missing complete native FootStretch animation clip.");
            var openPose=new Vector3[rig.bones.Length];for(int i=0;i<rig.bones.Length;i++)openPose[i]=rig.bones[i].position;
            rig.ResetPose();clip.SampleAnimation(rig.gameObject,2);
            for(int i=0;i<rig.bones.Length;i++)report.nativeClipPoseErrorPixels=Mathf.Max(report.nativeClipPoseErrorPixels,Vector3.Distance(openPose[i],rig.bones[i].position)*100);
            rig.Evaluate(0,4,3.4f);
            report.closedToeSpreadPixels=Mathf.Max(Mathf.Abs(Spread(rig,40,44)),Mathf.Abs(Spread(rig,45,49)));
            BunnyVerification.Capture(preview.view,"stretch-close.png");
            Require((rig.bones[8].position.y-rearRest.y)*100>10,"Foot must stay raised until the toes have closed.");
            rig.Evaluate(0,4,4.8f);
            for(int i=0;i<rig.bones.Length;i++)
            {
                var expected=rig.transform.TransformPoint(BunnyDefinition.Point(BunnyDefinition.Points[i].x,BunnyDefinition.Points[i].y));
                report.returnPoseErrorPixels=Mathf.Max(report.returnPoseErrorPixels,Vector3.Distance(rig.bones[i].position,expected)*100);
            }
            BunnyVerification.Capture(preview.view,"stretch-lowered.png");
            var baked=new Mesh();
            foreach(float amount in new[]{0f,1f,1.5f})for(int frame=0;frame<=96;frame++)
            {
                rig.toeMotion=amount;rig.Evaluate(0,4,frame/20f);
                if(amount==0)for(int digit=40;digit<50;digit++)
                {
                    var tip=new Vector3(0,-.25f,0);var p=BunnyDefinition.Points[digit];
                    var expected=rig.transform.TransformPoint(BunnyDefinition.Point(p.x,p.y)+tip);
                    report.disabledToeTipMotionPixels=Mathf.Max(report.disabledToeTipMotionPixels,Vector3.Distance(expected,rig.bones[digit].TransformPoint(tip))*100);
                }
                foreach(var part in rig.parts)if(part.region.StartsWith("spread_"))
                {
                    part.renderer.BakeMesh(baked);var v=baked.vertices;var rest=part.renderer.sharedMesh.vertices;var triangles=part.renderer.sharedMesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                        float ratio=Vector3.Cross(v[b]-v[a],v[c]-v[a]).z/Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).z;
                        if(ratio<report.minToeAreaRatio)
                        {
                            report.minToeAreaRatio=ratio;report.worstToe=part.id;report.worstAmount=amount;report.worstTime=frame/20f;
                            report.worstPixel=BunnyDefinition.Pixel((rest[a]+rest[b]+rest[c])/3);
                        }
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(baked);
            Require(report.disabledToeTipMotionPixels<.01f,"Zero amplitude must disable all toe motion.");
            bool pass=report.rearFootLiftPixels>10&&report.frontFootLiftPixels>10&&report.rearToeSpreadPixels>5&&report.frontToeSpreadPixels>5&&report.closedToeSpreadPixels<.1f&&report.nativeClipPoseErrorPixels<.1f&&report.returnPoseErrorPixels<.1f&&report.minToeAreaRatio>.05f;
            report.status=pass?"PASS":"FAIL";File.WriteAllText(Path.Combine(BunnyBuilder.Output,"stretch-validation.json"),JsonUtility.ToJson(report,true));
            Require(pass,"Foot stretch verification failed: "+JsonUtility.ToJson(report));Debug.Log("BUNNY_STRETCH_PASS "+JsonUtility.ToJson(report));
        }
        static float Spread(BunnyMotion rig,int first,int last)
        {
            var left=rig.bones[first];var right=rig.bones[last];
            var tip=new Vector3(0,-.25f,0);
            float actual=(right.TransformPoint(tip)-left.TransformPoint(tip)).x;
            var leftRest=left.parent.TransformPoint(rig.restPositions[first]+tip);
            var rightRest=right.parent.TransformPoint(rig.restPositions[last]+tip);
            return (actual-(rightRest-leftRest).x)*100;
        }
        public static void Sequence()
        {
            EditorSceneManager.OpenScene(BunnyBuilder.AssetRoot+"/EmeraldBunnyPreview.unity");
            var rig=UnityEngine.Object.FindFirstObjectByType<BunnyMotion>();var preview=UnityEngine.Object.FindFirstObjectByType<BunnyPreview>();rig.autoPlay=false;preview.showPanel=false;
            Directory.CreateDirectory(Path.Combine(BunnyBuilder.Output,"stretch-frames"));
            for(int frame=0;frame<120;frame++){rig.Evaluate(0,4,frame/24f);BunnyVerification.Capture(preview.view,"stretch-frames/frame-"+frame.ToString("D3")+".png");}
            Debug.Log("BUNNY_STRETCH_SEQUENCE_PASS");
        }
        public static void Release(){BunnyBuilder.All();Sequence();}
    }
}
