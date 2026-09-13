using System;
using UnityEngine;

namespace EmeraldBunny
{
    [Serializable] public class LayerSource
    {
        public string id, label, region;
        public int x, y, width, height, order;
        public float defaultOpacity=1;
    }
    [Serializable] public class SourceDocument { public int width, height; public string sourceSha256; public LayerSource[] layers; }
    [Serializable] public class BoundPart { public string id, region; public SkinnedMeshRenderer renderer; }

    public static class BunnyDefinition
    {
        public const float PixelsPerUnit=100;
        public static Vector3 Point(float x,float y) => new Vector3((x-512)/100,(1536-y)/100,0);
        public static Vector2 Pixel(Vector3 v) => new Vector2(v.x*100+512,1536-v.y*100);
        public static readonly string[] Names={
            "Fixed","Pelvis","Chest","Neck","Head",
            "RearHip","RearKnee","RearAnkle","RearToes",
            "FrontHip","FrontKnee","FrontAnkle","FrontToes",
            "LeftShoulder","LeftElbow","LeftWrist","LeftPalm",
            "RightShoulder","RightElbow","RightWrist","RightPalm",
            "HairLeftRoot","HairLeftTip","HairRightRoot","HairRightTip","Forelock",
            "EarLeft","EarRightRoot","EarRightTip","EarringLeft","EarringRight","Ribbon",
            "LeftThumb","LeftIndex","LeftMiddle","RightThumb","RightIndex","RightMiddle","RightRing","RightLittle",
            "RearToe1","RearToe2","RearToe3","RearToe4","RearToe5",
            "FrontToe1","FrontToe2","FrontToe3","FrontToe4","FrontToe5","RearFoot","FrontFoot"
        };
        public static readonly Vector2[] Points={
            new Vector2(512,1536),new Vector2(474,704),new Vector2(440,485),new Vector2(413,367),new Vector2(428,296),
            new Vector2(525,742),new Vector2(605,813),new Vector2(605,1264),new Vector2(591,1424),
            new Vector2(431,715),new Vector2(673,678),new Vector2(848,1152),new Vector2(929,1320),
            new Vector2(278,393),new Vector2(131,512),new Vector2(250,521),new Vector2(279,579),
            new Vector2(569,477),new Vector2(632,570),new Vector2(616,612),new Vector2(605,645),
            new Vector2(349,216),new Vector2(334,300),new Vector2(520,231),new Vector2(520,352),new Vector2(476,182),
            new Vector2(446,153),new Vector2(526,161),new Vector2(609,116),new Vector2(364,276),new Vector2(476,360),new Vector2(438,384),
            new Vector2(249,556),new Vector2(285,573),new Vector2(275,588),new Vector2(580,614),new Vector2(605,628),new Vector2(618,634),new Vector2(630,637),new Vector2(641,636),
            new Vector2(548,1430),new Vector2(578,1430),new Vector2(600,1433),new Vector2(620,1436),new Vector2(640,1440),
            new Vector2(897,1308),new Vector2(913,1307),new Vector2(931,1306),new Vector2(949,1306),new Vector2(968,1306),
            new Vector2(605,1264),new Vector2(848,1152)
        };
        public static readonly int[] Parents={-1,0,1,2,3, 1,5,6,50, 1,9,10,51, 2,13,14,15, 2,17,18,19, 4,21,4,23,4, 4,4,27,4,4,3, 16,16,16,20,20,20,20,20, 8,8,8,8,8,12,12,12,12,12,7,11};
        public static float Smooth(float a,float b,float v) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
        public static float Along(Vector2 p,int a,int b) => Vector2.Dot(p-Points[a],Points[b]-Points[a])/(Points[b]-Points[a]).sqrMagnitude;
    }
}
