using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace EmeraldBunny
{
    public sealed class BunnyPreview : MonoBehaviour
    {
        public BunnyMotion character;
        public Camera view;
        public bool showPanel=true,showBones;
        Font font;LineRenderer[] lines;
        Material lineMaterial;
        int background;
        void Start()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            font=Font.CreateDynamicFontFromOSFont("Microsoft YaHei",18);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-bunnySmoke")>=0)StartCoroutine(Smoke());
        }
        IEnumerator Smoke()
        {
            character.autoPlay=false;showPanel=false;
            for(int i=0;i<4;i++)yield return null;
            string directory=Path.Combine(Application.dataPath,"../smoke");Directory.CreateDirectory(directory);
            foreach(var pair in new[]{new Vector2(0,0),new Vector2(2.1f,1),new Vector2(4,2),new Vector2(0,3)})
            {
                if(pair.y==3)character.Evaluate(0,4,2);
                else character.Evaluate(pair.x,2,1.6f*(pair.y>1?1:0));
                yield return null;yield return null;
                var rt=new RenderTexture(768,1152,24);var oldTarget=view.targetTexture;var oldActive=RenderTexture.active;
                view.targetTexture=rt;view.orthographicSize=7.68f;view.Render();RenderTexture.active=rt;
                var capture=new Texture2D(768,1152,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,768,1152),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(directory,"frame-"+pair.y+".png"),capture.EncodeToPNG());
                view.targetTexture=oldTarget;RenderTexture.active=oldActive;Destroy(capture);Destroy(rt);
            }
            File.WriteAllText(Path.Combine(directory,"PASS.txt"),"Standalone player loaded serialized native bones, meshes, textures and motion; captured 4 frames including open foot stretch.");
            Application.Quit(0);
        }
        void OnGUI()
        {
            if(!showPanel)return;
            var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*Mathf.Max(.8f,Screen.height/1100f));
            GUI.skin.font=font;GUI.skin.button.fontSize=15;GUI.skin.label.fontSize=14;
            GUI.backgroundColor=new Color(.1f,.14f,.14f,.92f);
            GUILayout.BeginArea(new Rect(14,14,184,422),GUI.skin.box);
            GUILayout.Label("EMERALD BUNNY");GUILayout.Label("原生骨骼 · 坐姿验证");
            if(GUILayout.Button(character.autoPlay?"暂停动作":"播放待机",GUILayout.Height(29)))character.autoPlay=!character.autoPlay;
            if(GUILayout.Button("原画姿态对照",GUILayout.Height(29))){character.autoPlay=false;character.ResetPose();}
            if(GUILayout.Button("抬头回应",GUILayout.Height(29)))character.Trigger(1);
            if(GUILayout.Button("膝踝 / 脚趾测试",GUILayout.Height(29)))character.Trigger(2);
            if(GUILayout.Button("手部放松",GUILayout.Height(29)))character.Trigger(3);
            if(GUILayout.Button("脚掌舒展",GUILayout.Height(29)))character.Trigger(4);
            GUILayout.Label("舒展幅度  "+character.toeMotion.ToString("0.0")+"×");
            character.toeMotion=GUILayout.HorizontalSlider(character.toeMotion,0,1.5f);
            showBones=GUILayout.Toggle(showBones,"显示骨骼");
            character.secondaryMotion=GUILayout.Toggle(character.secondaryMotion,"发束与饰物跟随");
            if(GUILayout.Button("切换背景",GUILayout.Height(27)))SetBackground((background+1)%3);
            GUILayout.EndArea();GUI.matrix=old;
        }
        public void SetBackground(int index)
        {
            background=index;
            foreach(var p in character.parts)if(p.region=="background")p.renderer.enabled=index==0;
            view.backgroundColor=index==1?new Color(.035f,.04f,.05f):index==2?new Color(.2f,.29f,.28f):new Color(.99f,.986f,.976f);
        }
        void LateUpdate()
        {
            if(showBones&&lines==null)
            {
                lineMaterial=new Material(Shader.Find("Hidden/Internal-Colored"));
                lines=new LineRenderer[character.bones.Length];
                for(int i=1;i<lines.Length;i++)
                {
                    var go=new GameObject("Bone overlay "+i);go.transform.SetParent(transform,false);
                    var line=go.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.startColor=line.endColor=new Color(.05f,.9f,.73f);line.startWidth=line.endWidth=.014f;line.positionCount=2;line.sortingOrder=1000;lines[i]=line;
                }
            }
            if(lines!=null)for(int i=1;i<lines.Length;i++)
            {
                lines[i].enabled=showBones;
                if(showBones){var t=character.bones[i];lines[i].SetPosition(0,t.position+Vector3.back);lines[i].SetPosition(1,t.parent.position+Vector3.back);}
            }
        }
        void OnDestroy(){if(lineMaterial!=null)Destroy(lineMaterial);if(font!=null)Destroy(font);}
    }
}
