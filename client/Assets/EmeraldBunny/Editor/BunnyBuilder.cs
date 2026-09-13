using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static EmeraldBunny.BunnyDefinition;

namespace EmeraldBunny.Editor
{
    public sealed class BunnyTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/EmeraldBunny/Art/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        }
    }

    public static class BunnyBuilder
    {
        public const string AssetRoot="Assets/EmeraldBunny";
        public static string Root
        {
            get
            {
                string parent=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
                return Directory.Exists(Path.Combine(parent,"authoring"))?parent:Path.GetFullPath(Path.Combine(Application.dataPath,"../BunnyBuild"));
            }
        }
        public static string Output=>Path.Combine(Root,"verification");
        static T Save<T>(T asset,string path) where T:UnityEngine.Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old!=null)
            {
                // Rebuild native mesh buffers, rather than only copying serialization fields.
                if(old is Mesh destination && asset is Mesh source)
                {
                    destination.Clear();destination.ClearBlendShapes();destination.indexFormat=source.indexFormat;
                    destination.vertices=source.vertices;destination.uv=source.uv;destination.normals=source.normals;
                    destination.triangles=source.triangles;destination.boneWeights=source.boneWeights;destination.bindposes=source.bindposes;destination.bounds=source.bounds;
                    for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
                    {
                        var delta=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(shape,frame,delta,null,null);
                        destination.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),delta,null,null);
                    }
                }
                else EditorUtility.CopySerialized(asset,old);
                UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;
            }
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        public static BunnyMotion CreateCharacter()
        {
            foreach(var folder in new[]{"Generated","Generated/Meshes","Generated/Materials","Generated/Animations"})Directory.CreateDirectory(AssetRoot+"/"+folder);
            AssetDatabase.Refresh();
            var source=JsonUtility.FromJson<SourceDocument>(File.ReadAllText(AssetRoot+"/Art/layers.json"));
            var go=new GameObject("Emerald Bunny");var rig=go.AddComponent<BunnyMotion>();
            var skeleton=new GameObject("Skeleton").transform;skeleton.SetParent(go.transform,false);
            rig.bones=new Transform[Names.Length];
            for(int i=0;i<Names.Length;i++)
            {
                var bone=new GameObject(Names[i]).transform;
                bone.SetParent(skeleton,false);
                var p=Points[i];bone.position=Point(p.x,p.y);rig.bones[i]=bone;
            }
            for(int i=0;i<Names.Length;i++)rig.bones[i].SetParent(Parents[i]<0?skeleton:rig.bones[Parents[i]],true);
            var renderRoot=new GameObject("Artwork").transform;renderRoot.SetParent(go.transform,false);
            var parts=new List<BoundPart>();
            foreach(var row in source.layers)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetRoot+"/Art/"+row.id+".png");
                if(texture==null)throw new Exception("Missing texture "+row.id);
                var part=new GameObject(row.id);part.transform.SetParent(renderRoot,false);
                var renderer=part.AddComponent<SkinnedMeshRenderer>();
                renderer.bones=rig.bones;renderer.rootBone=rig.bones[0];renderer.updateWhenOffscreen=true;
                renderer.quality=SkinQuality.Bone4;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                renderer.sortingOrder=row.order;
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetRoot+"/Runtime/BunnySkin.shader");
                var material=new Material(shader){name=row.id,mainTexture=texture,renderQueue=3000+row.order};
                material.SetFloat("_Opacity",row.defaultOpacity);
                renderer.sharedMaterial=Save(material,AssetRoot+"/Generated/Materials/"+row.id+".mat");
                renderer.sharedMesh=Save(MakeMesh(row,texture,rig),AssetRoot+"/Generated/Meshes/"+row.id+".asset");
                renderer.localBounds=new Bounds(new Vector3(0,7.68f,0),new Vector3(13,19,4));
                parts.Add(new BoundPart{id=row.id,region=row.region,renderer=renderer});
            }
            rig.parts=parts.ToArray();rig.CacheRest();rig.ResetPose();return rig;
        }
        static Mesh MakeMesh(LayerSource row,Texture2D texture,BunnyMotion rig)
        {
            bool detail=BunnyWeights.Eye(row.region)||row.region.StartsWith("finger")||row.region.StartsWith("toe")||row.region.StartsWith("spread_")||row.region=="mouth";
            int step=row.region=="background"?160:row.region.StartsWith("chair")?20:detail?3:8;
            int startX=Mathf.FloorToInt(row.x/(float)step)*step,startY=Mathf.FloorToInt(row.y/(float)step)*step;
            int nx=Mathf.CeilToInt((row.x+row.width-startX)/(float)step),ny=Mathf.CeilToInt((row.y+row.height-startY)/(float)step);
            var pixels=texture.GetPixels32();
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var indices=new List<int>();
            var vertexMap=new Dictionary<int,int>();
            int Vertex(int gx,int gy)
            {
                int key=gy*(nx+1)+gx;if(vertexMap.TryGetValue(key,out var existing))return existing;
                var p=new Vector2(startX+gx*step,startY+gy*step);
                float u=(p.x-row.x)/row.width,v=(p.y-row.y)/row.height;
                int index=vertices.Count;var pos=Point(p.x,p.y);pos.z=-row.order*.0001f;
                vertices.Add(pos);uv.Add(new Vector2(u,1-v));weights.Add(BunnyWeights.At(row.region,p));vertexMap[key]=index;return index;
            }
            for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
            {
                int x0=Mathf.Max(0,startX+x*step-row.x-1),x1=Mathf.Min(row.width,startX+(x+1)*step-row.x+1);
                int y0=Mathf.Max(0,startY+y*step-row.y-1),y1=Mathf.Min(row.height,startY+(y+1)*step-row.y+1);
                bool occupied=false;
                for(int py=y0;py<y1&&!occupied;py++)for(int px=x0;px<x1;px++)if(pixels[(row.height-1-py)*row.width+px].a>0){occupied=true;break;}
                if(!occupied)continue;
                int a=Vertex(x,y),b=Vertex(x+1,y),c=Vertex(x,y+1),d=Vertex(x+1,y+1);
                indices.Add(a);indices.Add(c);indices.Add(b);indices.Add(b);indices.Add(c);indices.Add(d);
            }
            var mesh=new Mesh{name=row.id,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.boneWeights=weights.ToArray();
            var bind=new Matrix4x4[rig.bones.Length];for(int i=0;i<bind.Length;i++)bind[i]=rig.bones[i].worldToLocalMatrix*rig.transform.localToWorldMatrix;mesh.bindposes=bind;
            if(BunnyWeights.Eye(row.region))
            {
                var delta=new Vector3[vertices.Count];
                for(int i=0;i<delta.Length;i++)
                {
                    Vector2 p=Pixel(vertices[i]);bool right=p.x>460;
                    Vector2 center=right?new Vector2(486,272):new Vector2(428,240);
                    Vector2 normal=new Vector2(-.43f,1).normalized;
                    float distance=Vector2.Dot(p-center,normal);
                    Vector2 shift=normal*(-distance*.95f+1.0f);
                    delta[i]=new Vector3(shift.x/100,-shift.y/100,0);
                }
                mesh.AddBlendShapeFrame("Blink",100,delta,null,null);
            }
            mesh.RecalculateBounds();mesh.RecalculateNormals();return mesh;
        }
        public static BunnyPreview CreateScene(BunnyMotion rig)
        {
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=8.05f;camera.transform.position=new Vector3(0,7.68f,-20);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.99f,.986f,.976f);
            camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.allowHDR=false;camera.allowMSAA=false;
            var preview=new GameObject("Preview controls").AddComponent<BunnyPreview>();preview.character=rig;preview.view=camera;return preview;
        }
        [MenuItem("Emerald Bunny/Rebuild character and preview")]
        public static void Prepare()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            var rig=CreateCharacter();CreateScene(rig);
            PrefabUtility.SaveAsPrefabAsset(rig.gameObject,AssetRoot+"/EmeraldBunny.prefab");
            BakeClip(rig,"Idle",8,0);BakeClip(rig,"Response",3,1);BakeClip(rig,"LegAnkle",3.6f,2);BakeClip(rig,"HandRelax",3,3);BakeClip(rig,"FootStretch",4.8f,4);
            // Retain an earlier local prototype's clip as a compatible alias if present.
            if(AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetRoot+"/Generated/Animations/ToeFlex.anim")!=null)BakeClip(rig,"ToeFlex",4.8f,4);
            rig.ResetPose();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),AssetRoot+"/EmeraldBunnyPreview.unity");
            AssetDatabase.SaveAssets();Debug.Log("BUNNY_PREPARE_PASS");
        }
        static void BakeClip(BunnyMotion rig,string name,float duration,int action)
        {
            var clip=new AnimationClip{name=name,frameRate=30};
            int frames=Mathf.RoundToInt(duration*30);
            for(int bone=0;bone<rig.bones.Length;bone++)
            {
                var curves=new AnimationCurve[7];for(int c=0;c<7;c++)curves[c]=new AnimationCurve();
                for(int frame=0;frame<=frames;frame++)
                {
                    float t=frame/30f;rig.Evaluate(action==0?t:0,action,t);var b=rig.bones[bone];var p=b.localPosition;var q=b.localRotation;
                    var values=new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[c].AddKey(t,values[c]);
                }
                string path=AnimationUtility.CalculateTransformPath(rig.bones[bone],rig.transform);
                string[] props={"localPosition.x","localPosition.y","localPosition.z","localRotation.x","localRotation.y","localRotation.z","localRotation.w"};
                for(int c=0;c<7;c++)clip.SetCurve(path,typeof(Transform),props[c],curves[c]);
            }
            foreach(var part in rig.parts)if(part.renderer.sharedMesh.blendShapeCount>0)
            {
                var curve=new AnimationCurve();for(int frame=0;frame<=frames;frame++){float t=frame/30f;rig.Evaluate(action==0?t:0,action,t);curve.AddKey(t,rig.blink*100);}
                clip.SetCurve(AnimationUtility.CalculateTransformPath(part.renderer.transform,rig.transform),typeof(SkinnedMeshRenderer),"blendShape.Blink",curve);
            }
            foreach(var part in rig.parts)if(part.region.StartsWith("iris")||part.region.StartsWith("eyeL")||part.region.StartsWith("eyeR")||part.region=="repair_eyes"||part.region=="blink_closed")
            {
                var curve=new AnimationCurve();for(int frame=0;frame<=frames;frame++){float t=frame/30f;rig.Evaluate(action==0?t:0,action,t);curve.AddKey(t,part.region=="blink_closed"?Smooth(.1f,.95f,rig.blink):1-rig.blink);}
                clip.SetCurve(AnimationUtility.CalculateTransformPath(part.renderer.transform,rig.transform),typeof(SkinnedMeshRenderer),"material._Opacity",curve);
            }
            clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=action==0;AnimationUtility.SetAnimationClipSettings(clip,settings);
            Save(clip,AssetRoot+"/Generated/Animations/"+name+".anim");
        }
        public static void BuildPlayer()
        {
            PlayerSettings.productName="Emerald Bunny Native Rig";PlayerSettings.companyName="Character Studies";
            PlayerSettings.defaultScreenWidth=850;PlayerSettings.defaultScreenHeight=1200;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.Combine(Root,"player"));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{AssetRoot+"/EmeraldBunnyPreview.unity"},locationPathName=Path.Combine(Root,"player/EmeraldBunny.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Bunny player build failed");
            AssetDatabase.ExportPackage(AssetRoot,Path.Combine(Root,"EmeraldBunny-native-v0.1.1.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("BUNNY_BUILD_PASS");
        }
        public static void All(){Prepare();BunnyVerification.Run();BunnyStretchVerification.Run();BuildPlayer();}
        public static void PrepareAndVerify(){Prepare();BunnyVerification.Run();}
    }
}
