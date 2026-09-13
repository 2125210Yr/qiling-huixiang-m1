using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Inochi2D.Internal;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace EmeraldInochi {
    // Animation curves feed official native parameters. NativeSurface only draws the result.
    public sealed class InochiStandee : MonoBehaviour, IPointerClickHandler {
        InPuppet puppet;
        NativeSurface surface;
        Material display;
        Dictionary<string,InParameter> parameters;
        float reaction=100;
        double enteredAt, reactionStarted=double.NegativeInfinity, footStarted=double.NegativeInfinity;
        bool ready, capture, footCapture, entryActionPending=true;
        string captureFolder;
        bool interactionCheck, interactionFinished;
        string interactionFolder;
        double interactionDeadline;
        readonly InteractionResult interactionResult=new();
        [Serializable] sealed class InteractionResult {
            public string status="RUNNING", error="", topHit="";
            public double durationSeconds;
            public float clickScreenX, clickScreenY;
            public bool autoLift, autoPeak, autoRest, clickLift, clickPeak, clickRest, ignoredMidcycle;
            public List<InteractionSample> samples=new();
        }
        [Serializable] sealed class InteractionSample {
            public string phase;
            public double timeSeconds, actionSeconds, footStarted, reactionStarted;
            public float lift, spread, expectedLift, expectedSpread;
        }
        public static GameObject Attach(Transform parent) {
            var holder=new GameObject("Emerald Bunny · Inochi",typeof(RectTransform));
            holder.transform.SetParent(parent,false);
            var rect=(RectTransform)holder.transform;
            rect.anchorMin=new Vector2(.02f,.105f);rect.anchorMax=new Vector2(.98f,.93f);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var go=new GameObject("Native surface",typeof(RectTransform),typeof(RawImage));
            go.transform.SetParent(holder.transform,false);
            var fit=go.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1024f/1536;
            var player=go.AddComponent<InochiStandee>();
            try {player.Initialize();return holder;}
            catch(Exception e) {
                Debug.LogError("EMERALD_LOAD_FAILED "+e);
                if(player.interactionCheck)player.InteractionFailed(e);
                else if(player.capture)player.CaptureFailed(e);
                player.ReleaseResources();Destroy(holder);return null;
            }
        }
        void Initialize() {
            string[] args=Environment.GetCommandLineArgs();
            footCapture=args.Contains("-emerald-foot-capture");
            capture=footCapture||args.Contains("-emerald-capture");
            interactionCheck=args.Contains("-emerald-foot-interaction-check");
            if(interactionCheck)interactionFolder=OptionValue(args,"-emerald-foot-interaction-check",Path.Combine(Application.persistentDataPath,"EmeraldFootInteraction"));
            if(interactionCheck&&capture)throw new ArgumentException("Interaction checks must run separately from deterministic capture");
            if(capture)captureFolder=OptionValue(args,footCapture?"-emerald-foot-capture":"-emerald-capture",Path.Combine(Application.persistentDataPath,footCapture?"EmeraldFootCapture":"EmeraldCapture"));
            byte[] modelBytes;
            string overridePath=capture?OptionValue(args,"-emerald-model",null):null;
            if(overridePath!=null) {
                if(!Path.IsPathRooted(overridePath))throw new ArgumentException("-emerald-model requires an absolute INX path");
                modelBytes=File.ReadAllBytes(overridePath);
                Debug.Log("EMERALD_CAPTURE_MODEL "+overridePath);
            } else {
                var model=Resources.Load<TextAsset>("Inochi/EmeraldBunny");
                if(model==null)throw new FileNotFoundException("Resources/Inochi/EmeraldBunny is missing");
                modelBytes=model.bytes;
            }
            using var bytes=new NativeArray<byte>(modelBytes,Allocator.Temp);
            puppet=InPuppet.LoadFromMemory(new NativeSlice<byte>(bytes));
            if(puppet.IsNull)throw new InvalidOperationException("Native model load returned null");
            parameters=puppet.Parameters.ToArray().ToDictionary(p=>p.Name);
            if((footCapture||interactionCheck)&&!parameters.ContainsKey("FootStretch"))throw new InvalidOperationException("Foot verification requires the native FootStretch parameter");
            if(footCapture||interactionCheck) {
                var foot=parameters["FootStretch"];
                if(foot.Dimensions!=2||foot.Min!=Vector2.zero||foot.Max!=Vector2.one)throw new InvalidOperationException("FootStretch must have two dimensions, both ranging from 0 to 1");
            }
            puppet.PhysicsEnabled=false;
            surface=new NativeSurface(puppet,1024,1536);
            display=new Material(Shader.Find("EmeraldInochi/Display"));
            var image=GetComponent<RawImage>();image.texture=surface.Output;image.material=display;
            enteredAt=Time.unscaledTimeAsDouble;
            if(interactionCheck)interactionDeadline=Time.realtimeSinceStartupAsDouble+20;
            ready=true;Pose(0,new FootStretchPose(0,0));puppet.Update(0);surface.Render();
            Debug.Log("EMERALD_LOADED parts="+puppet.Textures.Length+" vertices="+puppet.DrawList.VertexData.Length+" parameters="+parameters.Count);
            if(capture)StartCoroutine(Capture());
            else if(interactionCheck)StartCoroutine(InteractionCheck());
        }
        static string OptionValue(string[] args,string option,string fallback) {
            int index=Array.IndexOf(args,option);
            if(index<0)return fallback;
            if(index+1<args.Length&&!args[index+1].StartsWith("-",StringComparison.Ordinal))return args[index+1];
            if(fallback!=null)return fallback;
            throw new ArgumentException(option+" requires a value");
        }
        void Set(string name,float v){if(parameters.TryGetValue(name,out var p))p.Value=new Vector2(Mathf.Clamp(v,name=="Blink"?0:-1,1),0);}
        public void OnPointerClick(PointerEventData e) {
            if(!ready||capture)return;
            double time=Time.unscaledTimeAsDouble-enteredAt;
            if(time-footStarted<FootStretchTimeline.Duration)return;
            reactionStarted=time;footStarted=time;entryActionPending=false;
        }
        void Pose(float t,FootStretchPose foot) {
            float response=reaction<2.5f?Mathf.Sin(Mathf.PI*reaction/2.5f)*Mathf.Exp(-reaction*.45f):0;
            Set("HeadRoll",.48f*Mathf.Sin(t*.72f)+response*.45f);
            Set("HeadLook",.42f*Mathf.Sin(t*.41f+.5f)-response*.35f);
            Set("Breath",.7f*Mathf.Sin(t*1.18f));
            Set("HairFollow",.48f*Mathf.Sin((t-.28f)*.72f)+.15f*Mathf.Sin(t*1.18f-.6f));
            float phase=t%4.7f;float blink=phase>3.6f&&phase<3.88f?Mathf.Sin(Mathf.PI*(phase-3.6f)/.28f):0;
            Set("Blink",Mathf.SmoothStep(0,1,blink));
            if(parameters.TryGetValue("FootStretch",out var parameter))parameter.Value=new Vector2(foot.Lift,foot.Spread);
        }
        void LateUpdate() {
            if(!ready||capture)return;
            try {
                if(interactionCheck&&Time.realtimeSinceStartupAsDouble>interactionDeadline)throw new TimeoutException("Foot interaction check exceeded 20 seconds");
                double time=Time.unscaledTimeAsDouble-enteredAt;
                if(entryActionPending&&time>=FootStretchTimeline.EntryDelay) {footStarted=FootStretchTimeline.EntryDelay;entryActionPending=false;}
                reaction=(float)(time-reactionStarted);
                Pose((float)time,FootStretchTimeline.Evaluate(time-footStarted));
                puppet.Update(Mathf.Min(Time.unscaledDeltaTime,1f/30));surface.Render();
            } catch(Exception error) {
                if(!interactionCheck)throw;
                InteractionFailed(error);
            }
        }
        IEnumerator InteractionCheck() {
            var steps=InteractionSteps();
            try {
                while(!interactionFinished) {
                    bool advanced=false;object next=null;Exception failure=null;
                    try {advanced=steps.MoveNext();if(advanced)next=steps.Current;}
                    catch(Exception error) {failure=error;}
                    if(failure!=null) {InteractionFailed(failure);yield break;}
                    if(!advanced)break;
                    yield return next;
                }
            } finally {(steps as IDisposable)?.Dispose();}
            if(interactionFinished)yield break;
            interactionFinished=true;Application.Quit(0);
        }
        IEnumerator InteractionSteps() {
            Directory.CreateDirectory(interactionFolder);
            WriteInteractionResult();
            FootStretchTimeline.SelfCheck();
            // Observe values after the real LateUpdate and render; this code never calls Pose.
            while(Time.unscaledTimeAsDouble-enteredAt<5.5) {
                yield return new WaitForEndOfFrame();
                double time=Time.unscaledTimeAsDouble-enteredAt;
                var actual=ObserveInteraction("entry",FootStretchTimeline.EntryDelay);
                if(time>1.3&&time<2.05&&actual.x>0&&actual.x<1&&actual.y==0)interactionResult.autoLift=true;
                if(time>=3.1&&time<=3.65&&Near(actual,Vector2.one))interactionResult.autoPeak=true;
            }
            RequireInteraction(interactionResult.autoLift,"Entry auto action did not show a partial lift at the expected time");
            RequireInteraction(interactionResult.autoPeak,"Entry auto action did not reach the raised, spread pose at the expected time");
            RequireInteraction(footStarted==FootStretchTimeline.EntryDelay&&!entryActionPending,"Entry action did not start at the configured one-second delay");
            RequireInteraction(Near(ObserveInteraction("entry-rest",FootStretchTimeline.EntryDelay),Vector2.zero),"Entry auto action did not return to rest");
            interactionResult.autoRest=true;
            double clickTime=Time.unscaledTimeAsDouble-enteredAt;
            DispatchFootClick();
            RequireInteraction(footStarted==clickTime&&reactionStarted==clickTime,"Real pointer click did not start the foot and head response");
            bool checkedLift=false,checkedPeak=false;
            while(Time.unscaledTimeAsDouble-enteredAt-clickTime<4.5) {
                yield return new WaitForEndOfFrame();
                double actionTime=Time.unscaledTimeAsDouble-enteredAt-clickTime;
                var actual=ObserveInteraction("clicked",clickTime);
                if(!checkedLift&&actionTime>=.45) {
                    RequireInteraction(actual.x>0&&actual.x<1&&actual.y==0,"Pointer click did not produce a partial lift after 0.45 seconds");
                    checkedLift=true;interactionResult.clickLift=true;
                    double originalFootStart=footStarted,originalReactionStart=reactionStarted;
                    DispatchFootClick();
                    RequireInteraction(footStarted==originalFootStart&&reactionStarted==originalReactionStart,"Midcycle click restarted the foot action or head response");
                    interactionResult.ignoredMidcycle=true;
                }
                if(!checkedPeak&&actionTime>=2.3) {
                    RequireInteraction(Near(actual,Vector2.one),"Clicked action was not raised and spread at 2.3 seconds");
                    checkedPeak=true;interactionResult.clickPeak=true;
                }
            }
            RequireInteraction(checkedLift&&checkedPeak&&interactionResult.ignoredMidcycle,"Click verification missed an action checkpoint");
            RequireInteraction(Near(ObserveInteraction("clicked-rest",clickTime),Vector2.zero),"Clicked action did not return to rest after 4.5 seconds");
            interactionResult.clickRest=true;
            interactionResult.status="PASS";
            WriteInteractionResult();
            Debug.Log("EMERALD_FOOT_INTERACTION_PASS "+Path.Combine(interactionFolder,"result.json"));
        }
        Vector2 ObserveInteraction(string phase,double startTime) {
            if(Time.realtimeSinceStartupAsDouble>interactionDeadline)throw new TimeoutException("Foot interaction check exceeded 20 seconds");
            double time=Time.unscaledTimeAsDouble-enteredAt;
            var expected=FootStretchTimeline.Evaluate(time-startTime);
            var actual=parameters["FootStretch"].Value;
            interactionResult.samples.Add(new InteractionSample {
                phase=phase,timeSeconds=time,actionSeconds=time-startTime,
                footStarted=double.IsInfinity(footStarted)?-1:footStarted,
                reactionStarted=double.IsInfinity(reactionStarted)?-1:reactionStarted,
                lift=actual.x,spread=actual.y,expectedLift=expected.Lift,expectedSpread=expected.Spread
            });
            RequireInteraction(Near(actual,new Vector2(expected.Lift,expected.Spread)),"LateUpdate native pose differs from the expected "+phase+" timeline at "+time.ToString("F4",CultureInfo.InvariantCulture));
            return actual;
        }
        void DispatchFootClick() {
            var eventSystem=EventSystem.current;
            RequireInteraction(eventSystem!=null,"Lobby has no EventSystem for the foot click");
            var canvas=GetComponentInParent<Canvas>();
            RequireInteraction(canvas!=null,"Standee has no lobby Canvas");
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)transform;
            var uv=new Vector2(580f/1024,1-1430f/1536);
            var local=new Vector3(rect.rect.xMin+rect.rect.width*uv.x,rect.rect.yMin+rect.rect.height*uv.y,0);
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay&&camera==null)camera=Camera.main;
            var position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
            RequireInteraction(position.x>=0&&position.x<Screen.width&&position.y>=0&&position.y<Screen.height,"Foreground foot click lies outside the game screen");
            var pointer=new PointerEventData(eventSystem) {position=position,button=PointerEventData.InputButton.Left,clickCount=1};
            var hits=new List<RaycastResult>();eventSystem.RaycastAll(pointer,hits);
            RequireInteraction(hits.Count>0,"Foreground foot screen position has no UI raycast hit");
            var hit=hits[0];
            interactionResult.clickScreenX=position.x;interactionResult.clickScreenY=position.y;interactionResult.topHit=hit.gameObject.name;
            RequireInteraction(hit.gameObject.GetComponentInParent<InochiStandee>()==this,"Foreground foot is obstructed by the top UI raycast hit: "+hit.gameObject.name);
            pointer.pointerCurrentRaycast=hit;
            RequireInteraction(ExecuteEvents.Execute(hit.gameObject,pointer,ExecuteEvents.pointerClickHandler),"Top foot raycast hit did not receive pointerClickHandler");
        }
        static bool Near(Vector2 actual,Vector2 expected) {return Mathf.Abs(actual.x-expected.x)<=.0001f&&Mathf.Abs(actual.y-expected.y)<=.0001f;}
        static void RequireInteraction(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
        void WriteInteractionResult() {
            interactionResult.durationSeconds=Time.unscaledTimeAsDouble-enteredAt;
            File.WriteAllText(Path.Combine(interactionFolder,"result.json"),JsonUtility.ToJson(interactionResult,true));
        }
        void InteractionFailed(Exception error) {
            if(interactionFinished)return;
            interactionFinished=true;interactionResult.status="FAIL";interactionResult.error=error.ToString();
            Debug.LogError("EMERALD_FOOT_INTERACTION_FAILED "+error);
            try {
                if(!string.IsNullOrEmpty(interactionFolder)) {Directory.CreateDirectory(interactionFolder);WriteInteractionResult();}
            } catch(Exception writeError) {Debug.LogError("EMERALD_FOOT_INTERACTION_REPORT_FAILED "+writeError);}
            ReleaseResources();Application.Quit(1);
        }
        IEnumerator Capture() {
            // Iterator exceptions need to be observed explicitly; Unity otherwise logs and stops silently.
            var frames=CaptureFrames();
            try {
                while(true) {
                    bool advanced=false;object next=null;Exception failure=null;
                    try {advanced=frames.MoveNext();if(advanced)next=frames.Current;}
                    catch(Exception error) {failure=error;}
                    if(failure!=null) {CaptureFailed(failure);yield break;}
                    if(!advanced)break;
                    yield return next;
                }
            } finally {(frames as IDisposable)?.Dispose();}
            Application.Quit(0);
        }
        IEnumerator CaptureFrames() {
            string folder=captureFolder;
            Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.Combine(folder,"frames"));
            string completion=Path.Combine(folder,"completed.txt");
            string failure=Path.Combine(folder,"failed.txt");
            if(File.Exists(completion))File.Delete(completion);
            if(File.Exists(failure))File.Delete(failure);
            if(footCapture) {Directory.CreateDirectory(Path.Combine(folder,"surfaces"));FootStretchTimeline.SelfCheck();}
            using var csv=new StreamWriter(Path.Combine(folder,"params.csv"),false);
            csv.WriteLine("frame,time_seconds,action_seconds,FootStretch.x,FootStretch.y");
            yield return new WaitForSecondsRealtime(2);
            for(int frame=0;frame<180;frame++) {
                double time=frame/30.0;
                double actionTime=time-FootStretchTimeline.EntryDelay;
                var foot=footCapture?FootStretchTimeline.Evaluate(actionTime):new FootStretchPose(0,0);
                reaction=!footCapture&&frame>=120?(frame-120)/30f:100;
                Pose((float)time,foot);puppet.Update(1f/30);surface.Render();
                yield return new WaitForEndOfFrame();
                WriteImage(CaptureLobby(GetComponentInParent<Canvas>()),Path.Combine(folder,"frames",frame.ToString("D4")+".png"));
                if(footCapture||frame==0||frame==112||frame==145) {
                    string path=footCapture?Path.Combine(folder,"surfaces",frame.ToString("D4")+".png"):Path.Combine(folder,"surface-"+frame+".png");
                    WriteImage(CaptureSurface(),path);
                }
                Vector2 actual=parameters.TryGetValue("FootStretch",out var parameter)?parameter.Value:Vector2.zero;
                if(footCapture&&(Mathf.Abs(actual.x-foot.Lift)>.0001f||Mathf.Abs(actual.y-foot.Spread)>.0001f))throw new InvalidOperationException("Native FootStretch parameter differs from the requested pose at frame "+frame);
                csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4:F6}",frame,time,actionTime,actual.x,actual.y));
            }
            csv.Flush();
            File.WriteAllText(completion,footCapture?"180 actual lobby Canvas frames and 180 native surfaces at 30 fps; 1 second rest, 4.4 second FootStretch cycle, final rest. Official native core; native physics disabled.":"180 actual lobby frames; official native core; authored follow-through; native physics disabled.");
        }
        Texture2D CaptureSurface() {
            var old=RenderTexture.active;Texture2D texture=null;
            try {
                RenderTexture.active=surface.Output;
                texture=new Texture2D(surface.Output.width,surface.Output.height,TextureFormat.RGBA32,false,true);
                texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();return texture;
            } catch {if(texture!=null)Destroy(texture);throw;}
            finally {RenderTexture.active=old;}
        }
        static void WriteImage(Texture2D texture,string path) {
            try {
                var pixels=texture.GetPixels32();int visible=0;int stride=Math.Max(1,pixels.Length/4096);
                for(int i=0;i<pixels.Length;i+=stride) {
                    var color=pixels[i];
                    if(color.a>4&&(color.r>4||color.g>4||color.b>4))visible++;
                }
                if(visible<16)throw new InvalidOperationException("Capture is black or empty: "+path);
                File.WriteAllBytes(path,texture.EncodeToPNG());
            } finally {Destroy(texture);}
        }
        void CaptureFailed(Exception error) {
            Debug.LogError("EMERALD_CAPTURE_FAILED "+error);
            try {
                if(!string.IsNullOrEmpty(captureFolder)) {
                    Directory.CreateDirectory(captureFolder);
                    string completion=Path.Combine(captureFolder,"completed.txt");
                    if(File.Exists(completion))File.Delete(completion);
                    File.WriteAllText(Path.Combine(captureFolder,"failed.txt"),error.ToString());
                }
            } catch(Exception writeError) {Debug.LogError("EMERALD_CAPTURE_REPORT_FAILED "+writeError);}
            ReleaseResources();Application.Quit(1);
        }
        // Explicitly render the real lobby Canvas: hidden Windows players can return black backbuffers.
        public static Texture2D CaptureLobby(Canvas canvas) {
            var camera=Camera.main;
            if(canvas==null||camera==null)throw new InvalidOperationException("Capture requires the lobby Canvas and main camera");
            var target=new RenderTexture(756,1344,24,RenderTextureFormat.ARGB32);
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            Texture2D shot=null;
            try {
                if(!target.Create())throw new InvalidOperationException("Cannot create lobby capture render target");
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;
                shot=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                shot.ReadPixels(new Rect(0,0,target.width,target.height),0,0);shot.Apply();return shot;
            } catch {if(shot!=null)Destroy(shot);throw;
            } finally {
                camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
                RenderTexture.active=oldActive;target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
            }
        }
        void OnDestroy() {
            ReleaseResources();
        }
        void ReleaseResources() {
            ready=false;surface?.Dispose();surface=null;
            if(!puppet.IsNull){puppet.Free();puppet=default;}
            if(display!=null)Destroy(display);display=null;
        }
    }
}
