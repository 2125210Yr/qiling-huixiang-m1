using System;
using UnityEngine;
using static EmeraldBunny.BunnyDefinition;

namespace EmeraldBunny
{
    public sealed class BunnyMotion : MonoBehaviour
    {
        public Transform[] bones;
        public BoundPart[] parts;
        public Vector3[] restPositions;
        public bool autoPlay=true, breathing=true, secondaryMotion=true, blinking=true;
        [Range(0,1)] public float intensity=1;
        [Range(0,1.5f)] public float toeMotion=1;
        [HideInInspector] public float clock, actionTime, blink;
        [HideInInspector] public int action;
        MaterialPropertyBlock block;
        public void CacheRest()
        {
            restPositions=new Vector3[bones.Length];
            for(int i=0;i<bones.Length;i++)restPositions[i]=bones[i].localPosition;
        }
        public void ResetPose()
        {
            if(bones==null||restPositions==null)return;
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=restPositions[i];bones[i].localRotation=Quaternion.identity;bones[i].localScale=Vector3.one;}
            blink=0;
            foreach(var part in parts)
            {
                if(part.renderer.sharedMesh.blendShapeCount>0)part.renderer.SetBlendShapeWeight(0,0);
                Opacity(part.renderer,part.region=="blink_closed"?0:1);
            }
        }
        void Opacity(Renderer renderer,float value)
        {
            if(block==null)block=new MaterialPropertyBlock();
            block.Clear();block.SetFloat("_Opacity",value);renderer.SetPropertyBlock(block);
        }
        static float Wave(float t,float period,float delay=0)=>Mathf.Sin((t-delay)*Mathf.PI*2/period)-Mathf.Sin(-delay*Mathf.PI*2/period);
        static float Pulse(float t,float center)
        {
            float d=t-center;
            if(d<-.09f||d>.17f)return 0;
            return d<0?Smooth(-.09f,0,d):1-Smooth(0,.17f,d);
        }
        static float Envelope(float t,float length)=>t<=0||t>=length?0:Smooth(0,.75f,t)*(1-Smooth(length-.95f,length,t));
        void Rotate(int bone,float degrees)=>bones[bone].localRotation=Quaternion.Euler(0,0,degrees*intensity);
        void AnimateToes(float t,float leg,float toeGestureTime,bool toeGesture)
        {
            float idle=Mathf.Pow(Mathf.Sin(Mathf.PI*t/4),2);
            float lift=toeGesture?Smooth(0,1,toeGestureTime)*(1-Smooth(3.4f,4.8f,toeGestureTime)):0;
            float fan=toeGesture?Smooth(.8f,1.7f,toeGestureTime)*(1-Smooth(2.5f,3.3f,toeGestureTime)):0;
            float strength=toeMotion*intensity;
            // The foot lifts at the ankle. The calf has its own bone and stays supported.
            bones[50].localRotation=Quaternion.Euler(Mathf.Min(35,28*strength)*lift,0,0);
            bones[51].localRotation=Quaternion.Euler(Mathf.Min(35,30*strength)*lift,0,0);
            for(int i=40;i<50;i++)
            {
                int digit=(i-40)%5;
                float follow=1+.08f*Mathf.Sin(t*Mathf.PI/2-digit*.35f);
                float curl=Mathf.Clamp((8*idle*follow+14*leg)*(1-.035f*digit)*strength,0,25)*(1-lift);
                float spread=(digit-2)*Mathf.Min(11,8*strength)*fan;
                // Separate toe meshes retain foot weights at the roots; only the tips fan.
                bones[i].localPosition+=new Vector3((digit-2)*.015f*strength*fan,0,0);
                bones[i].localRotation=Quaternion.Euler(curl+Mathf.Min(5,3*strength)*lift,0,spread);
            }
        }
        // Analytic two-bone IK keeps the resting hand at its contact point while the torso breathes.
        void SolveArm(int first,Vector3 target)
        {
            Vector3 root=bones[first].position;
            float a=Vector3.Distance(bones[first].position,bones[first+1].position),b=Vector3.Distance(bones[first+1].position,bones[first+2].position);
            Vector2 d=target-root;float distance=Mathf.Clamp(d.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
            float along=(a*a-b*b+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector2 axis=d.normalized,normal=new Vector2(-axis.y,axis.x);
            Vector2 rest1=Points[first+1]-Points[first],rest2=Points[first+2]-Points[first];
            float sign=Mathf.Sign(-(rest2.x*rest1.y-rest2.y*rest1.x));
            Vector3 elbow=root+(Vector3)(axis*along+normal*height*sign);
            float angle=Vector2.SignedAngle(bones[first+1].position-root,elbow-root);
            bones[first].rotation=Quaternion.AngleAxis(angle,Vector3.forward)*bones[first].rotation;
            angle=Vector2.SignedAngle(bones[first+2].position-bones[first+1].position,target-bones[first+1].position);
            bones[first+1].rotation=Quaternion.AngleAxis(angle,Vector3.forward)*bones[first+1].rotation;
            bones[first+2].rotation=transform.rotation;
        }
        public void Evaluate(float time,int gesture=0,float gestureTime=0)
        {
            ResetPose();float t=Mathf.Repeat(time,8);
            float breathe=breathing?Wave(t,4):0;
            float response=gesture==1?Envelope(gestureTime,3):0;
            float leg=gesture==2?Envelope(gestureTime,3.6f):0;
            float hand=gesture==3?Envelope(gestureTime,3):0;
            bones[2].localPosition+=new Vector3(.004f*Wave(t,8),.025f*breathe,0)*intensity;
            Rotate(2,.24f*Wave(t,8)+.25f*response);
            Rotate(3,.14f*Wave(t,8,.2f));
            Rotate(4,.48f*Wave(t,8,.3f)-1.25f*response);
            Rotate(10,.18f*Wave(t,8,.4f)+.55f*leg);
            Rotate(11,.5f*Wave(t,8,.7f)+1.35f*leg);
            Rotate(12,.2f*Wave(t,4,.8f)+.55f*leg);
            Rotate(7,.15f*Wave(t,8,.5f));
            AnimateToes(t,leg,gestureTime,gesture==4);

            // Contact points are expressed in the character's frame, so moving the prefab is safe.
            SolveArm(13,transform.TransformPoint(Point(250,521)));
            Vector3 kneeDelta=bones[10].position-transform.TransformPoint(Point(673,678));
            SolveArm(17,transform.TransformPoint(Point(616,612))+kneeDelta);
            Rotate(16,.25f*Wave(t,8,.2f)+.45f*hand);
            Rotate(20,.18f*Wave(t,8,.4f)-.6f*hand);
            for(int i=32;i<40;i++)Rotate(i,.12f*Wave(t,4,.08f*(i-32))+.6f*hand);
            if(secondaryMotion)
            {
                Rotate(21,.5f*Wave(t,8,.25f)-.25f*response);
                Rotate(22,.85f*Wave(t,8,.5f)-.5f*response);
                Rotate(23,.45f*Wave(t,8,.3f)+.2f*response);
                Rotate(24,.85f*Wave(t,8,.6f)+.5f*response);
                Rotate(25,.3f*Wave(t,8,.25f));
                Rotate(26,.38f*Wave(t,8,.45f));
                Rotate(27,.38f*Wave(t,8,.3f));
                Rotate(28,.6f*Wave(t,8,.65f));
                Rotate(29,.8f*Wave(t,4,.25f));Rotate(30,.75f*Wave(t,4,.3f));Rotate(31,.4f*Wave(t,4,.4f));
            }
            blink=blinking?Mathf.Max(Pulse(t,2.1f),Pulse(t,5.7f)):0;
            foreach(var part in parts)
            {
                if(part.renderer.sharedMesh.blendShapeCount>0)part.renderer.SetBlendShapeWeight(0,blink*100);
                if(part.region.StartsWith("iris")||part.region.StartsWith("eyeL")||part.region.StartsWith("eyeR")||part.region=="repair_eyes")Opacity(part.renderer,1-blink);
                if(part.region=="blink_closed")Opacity(part.renderer,Smooth(.1f,.95f,blink));
            }
        }
        public void Trigger(int gesture){action=gesture;actionTime=0;autoPlay=true;}
        void Update()
        {
            if(!autoPlay)return;
            clock+=Time.deltaTime;if(action!=0){actionTime+=Time.deltaTime;if(actionTime>(action==4?4.8f:3.6f)){action=0;actionTime=0;}}
            Evaluate(clock,action,actionTime);
        }
    }
}
