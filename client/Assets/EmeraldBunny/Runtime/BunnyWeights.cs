using UnityEngine;
using static EmeraldBunny.BunnyDefinition;

namespace EmeraldBunny
{
    // All parts of a limb use the same field in PSD coordinates. A seam cannot acquire
    // different weights merely because it belongs to a different texture layer.
    public static class BunnyWeights
    {
        static BoneWeight One(int a)=>new BoneWeight{boneIndex0=a,weight0=1};
        static BoneWeight Pair(int a,int b,float t)=>new BoneWeight{boneIndex0=a,weight0=1-t,boneIndex1=b,weight1=t};
        public static BoneWeight Mix(BoneWeight a,BoneWeight b,float t)
        {
            var w=new float[Names.Length];
            Add(w,a,1-t);Add(w,b,t);
            int[] index=new int[4];float[] value=new float[4];float sum=0;
            for(int k=0;k<4;k++){int best=0;for(int j=1;j<w.Length;j++)if(w[j]>w[best])best=j;index[k]=best;value[k]=w[best];sum+=w[best];w[best]=0;}
            return new BoneWeight{boneIndex0=index[0],weight0=value[0]/sum,boneIndex1=index[1],weight1=value[1]/sum,boneIndex2=index[2],weight2=value[2]/sum,boneIndex3=index[3],weight3=value[3]/sum};
        }
        static void Add(float[] w,BoneWeight b,float s){w[b.boneIndex0]+=b.weight0*s;w[b.boneIndex1]+=b.weight1*s;w[b.boneIndex2]+=b.weight2*s;w[b.boneIndex3]+=b.weight3*s;}
        static BoneWeight Torso(Vector2 p)
        {
            var w=Pair(1,2,1-Smooth(480,720,p.y));
            w=Mix(w,One(3),1-Smooth(350,420,p.y));
            return Mix(w,One(4),1-Smooth(328,380,p.y));
        }
        static BoneWeight Leg(Vector2 p,bool front)
        {
            int hip=front?9:5,knee=hip+1,ankle=hip+2,toes=hip+3;
            float thigh=Smooth(.45f,1.0f,Along(p,hip,knee));
            var w=Pair(hip,knee,thigh);
            float lower=Smooth(.45f,.99f,Along(p,knee,ankle));
            w=Mix(w,One(ankle),lower);
            w=Mix(w,One(front?51:50),Smooth(.03f,.58f,Along(p,ankle,toes)));
            float foot=Smooth(.45f,.99f,Along(p,ankle,toes));
            w=Mix(w,One(toes),foot);
            return w;
        }
        static BoneWeight Arm(Vector2 p,bool right)
        {
            int shoulder=right?17:13,elbow=shoulder+1,wrist=shoulder+2,palm=shoulder+3;
            var w=Mix(Torso(p),One(shoulder),Smooth(-.1f,.27f,Along(p,shoulder,elbow)));
            w=Mix(w,One(elbow),Smooth(.63f,1.04f,Along(p,shoulder,elbow)));
            float fore=Along(p,elbow,wrist);
            if(p.y>(right?545:470))w=Mix(w,One(wrist),Smooth(.5f,1f,fore));
            w=Mix(w,One(palm),Smooth(.15f,.88f,Along(p,wrist,palm))*Smooth(right?586:520,right?623:558,p.y));
            float fingers=right?Smooth(638,680,p.y):Smooth(580,634,p.y);
            if(fingers>0)
            {
                int start=right?35:32,count=right?5:3;
                float acc=0;var fw=One(palm);
                for(int i=0;i<count;i++)
                {
                    float dx=(p.x-Points[start+i].x)/13f,value=Mathf.Exp(-dx*dx);
                    if(acc+value>1e-7f)fw=Mix(fw,One(start+i),value/(acc+value));acc+=value;
                }
                if(acc>.01f)w=Mix(w,fw,fingers*.7f);
            }
            return w;
        }
        static BoneWeight SeatedLegs(Vector2 p,bool front)
        {
            if(p.y<750)return Leg(p,front);
            float edge=p.y<900?Mathf.Lerp(645,684,Mathf.InverseLerp(750,900,p.y)):
                p.y<1130?Mathf.Lerp(684,800,Mathf.InverseLerp(900,1130,p.y)):760;
            return Mix(Leg(p,false),Leg(p,true),Smooth(edge-4,edge+14,p.x));
        }
        public static BoneWeight At(string id,Vector2 p)
        {
            if(id.StartsWith("spread_"))
            {
                bool front=id.StartsWith("spread_front_");
                int digit=id[id.Length-1]-'0',bone=(front?45:40)+digit;
                return Mix(Leg(p,front),One(bone),Smooth(Points[bone].y-8,Points[bone].y+16,p.y)*.95f);
            }
            if(id=="background"||id.StartsWith("chair")||id=="repair_chair")return One(0);
            if(id=="rear_limb"||id.StartsWith("leg_rear")||id.StartsWith("foot_ground")||id.StartsWith("toeGround"))return SeatedLegs(p,false);
            if(id=="front_limb"||id.StartsWith("leg_front")||id.StartsWith("foot_air")||id.StartsWith("toeAir")||id=="repair_knee")return SeatedLegs(p,true);
            if(id.StartsWith("arm_left")||id=="hand_left"||id=="cuff_left"||id.StartsWith("fingerL"))return Arm(p,false);
            if(id.StartsWith("arm_right")||id=="hand_right"||id=="cuff_right"||id.StartsWith("fingerR"))return Arm(p,true);
            if(id.StartsWith("hair"))
            {
                if(id=="hair_ribbon"||id=="hair_gem"||id=="hair_braid"||id=="hair_sweep")return One(4);
                if(id=="hair_fringe")return Pair(4,25,Smooth(185,295,p.y)*.75f);
                if(p.x<375)return Mix(One(4),Pair(21,22,Smooth(240,330,p.y)),Smooth(204,250,p.y));
                if(p.x>483)return Mix(One(4),Pair(23,24,Smooth(265,390,p.y)),Smooth(205,285,p.y));
                return One(4);
            }
            if(id=="rabbit_left")return Pair(4,26,1-Smooth(70,151,p.y));
            if(id=="rabbit_right")return Mix(Pair(4,27,Smooth(517,570,p.x)),One(28),Smooth(583,670,p.x));
            if(id=="earring_left")return Pair(4,29,Smooth(276,313,p.y));
            if(id=="earring_right")return Pair(4,30,Smooth(360,395,p.y));
            if(id=="bow_tails")return Pair(3,31,Smooth(387,438,p.y));
            if(id.StartsWith("bow")||id.StartsWith("collar"))return Torso(p);
            if(id=="waist_chain")return One(1);
            if(id=="body"||id=="neck"||id.StartsWith("costume"))return Torso(p);
            return One(4);
        }
        public static bool Eye(string id)=>id.StartsWith("eye")&&!id.StartsWith("earring")||id.StartsWith("iris")||id.StartsWith("upperlid")||id.StartsWith("lowerlid");
    }
}
