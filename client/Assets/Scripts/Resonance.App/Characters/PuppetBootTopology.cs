using System.Collections.Generic;
using UnityEngine;
namespace Resonance.App
{
    // Split topology through transparent space between the boots. No pixel cutting,
    // repainting, or reuse of the old incomplete boot alpha masks.
    internal static class PuppetBootTopology
    {
        struct Vertex { public Vector3 P; public Vector2 U; public BoneWeight W; }
        static readonly Vector2[] Boundary = {
            new Vector2(350,1184),new Vector2(508,1184),new Vector2(494,1232),
            new Vector2(484,1264),new Vector2(472,1296),new Vector2(459,1320),new Vector2(350,1320)
        };
        static Vector2 Pixel(Vertex v) { return new Vector2(v.U.x*1024,(1-v.U.y)*1536); }
        static float Side(Vector2 p,Vector2 a,Vector2 b) { return (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x); }
        static BoneWeight Mix(BoneWeight a,BoneWeight b,float t)
        {
            var w=new float[9];
            w[a.boneIndex0]+=a.weight0*(1-t);w[a.boneIndex1]+=a.weight1*(1-t);
            w[a.boneIndex2]+=a.weight2*(1-t);w[a.boneIndex3]+=a.weight3*(1-t);
            w[b.boneIndex0]+=b.weight0*t;w[b.boneIndex1]+=b.weight1*t;
            w[b.boneIndex2]+=b.weight2*t;w[b.boneIndex3]+=b.weight3*t;
            var ids=new int[]{0,1,2,3,4,5,6,7,8};
            for(int i=0;i<4;i++)for(int j=i+1;j<9;j++)if(w[ids[j]]>w[ids[i]]){var n=ids[i];ids[i]=ids[j];ids[j]=n;}
            var sum=w[ids[0]]+w[ids[1]]+w[ids[2]]+w[ids[3]];
            if(sum<1e-8f)return new BoneWeight{boneIndex0=0,weight0=1};
            return new BoneWeight{boneIndex0=ids[0],weight0=w[ids[0]]/sum,boneIndex1=ids[1],weight1=w[ids[1]]/sum,boneIndex2=ids[2],weight2=w[ids[2]]/sum,boneIndex3=ids[3],weight3=w[ids[3]]/sum};
        }
        static List<Vertex> Clip(List<Vertex> input,Vector2 a,Vector2 b,bool inside)
        {
            var output=new List<Vertex>();if(input.Count==0)return output;
            var previous=input[input.Count-1];var dp=Side(Pixel(previous),a,b);var pin=inside ? dp>=0 : dp<=0;
            foreach(var current in input)
            {
                var dc=Side(Pixel(current),a,b);var cin=inside ? dc>=0 : dc<=0;
                if(cin!=pin)
                {
                    var t=dp/(dp-dc);
                    output.Add(new Vertex{P=Vector3.Lerp(previous.P,current.P,t),U=Vector2.Lerp(previous.U,current.U,t),W=Mix(previous.W,current.W,t)});
                }
                if(cin)output.Add(current);
                previous=current;dp=dc;pin=cin;
            }
            return output;
        }
        static void Append(List<Vertex> polygon,int bone,List<Vector3> p,List<Vector2> u,List<BoneWeight> w,List<int> triangles)
        {
            if(polygon.Count<3)return;
            for(int i=1;i<polygon.Count-1;i++)
            {
                var a=polygon[0];var b=polygon[i];var c=polygon[i+1];
                if(Vector3.Cross(b.P-a.P,c.P-a.P).sqrMagnitude<1e-16f)continue;
                foreach(var v in new[]{a,b,c})
                {
                    triangles.Add(p.Count);p.Add(v.P);u.Add(v.U);
                    w.Add(bone<0 ? v.W : new BoneWeight{boneIndex0=bone,weight0=1});
                }
            }
        }
        public static void Split(ref Vector3[] vertices,ref Vector2[] uv,ref BoneWeight[] weights,ref int[] triangles)
        {
            var p=new List<Vector3>(vertices);var u=new List<Vector2>(uv);var w=new List<BoneWeight>(weights);var result=new List<int>();
            for(int i=0;i<triangles.Length;i+=3)
            {
                var polygon=new List<Vertex>();float minY=float.MaxValue,maxY=float.MinValue,minX=float.MaxValue;
                for(int k=0;k<3;k++)
                {
                    int n=triangles[i+k];var v=new Vertex{P=vertices[n],U=uv[n],W=weights[n]};polygon.Add(v);
                    var px=Pixel(v);minY=Mathf.Min(minY,px.y);maxY=Mathf.Max(maxY,px.y);minX=Mathf.Min(minX,px.x);
                }
                if(maxY<1184 || minX>510 || minY>1320)
                {
                    for(int k=0;k<3;k++)
                    {
                        int n=triangles[i+k];
                        if(minY>=1184)w[n]=new BoneWeight{boneIndex0=0,weight0=1};
                        result.Add(n);
                    }
                    continue;
                }
                for(int edge=0;edge<Boundary.Length && polygon.Count>0;edge++)
                {
                    var a=Boundary[edge];var b=Boundary[(edge+1)%Boundary.Length];
                    var outside=Clip(polygon,a,b,false);
                    // Above the cuff keep the calf-to-ankle blend. Other exterior
                    // pieces are static support shoe or transparent background.
                    Append(outside,edge==0 ? -1 : 0,p,u,w,result);
                    polygon=Clip(polygon,a,b,true);
                }
                Append(polygon,3,p,u,w,result);
            }
            vertices=p.ToArray();uv=u.ToArray();weights=w.ToArray();triangles=result.ToArray();
        }
    }
}
