Shader "Resonance/PuppetFace"
{
    Properties
    {
        _MainTex("Original", 2D) = "white" {}
        _BlinkTex("Closed eyes crop", 2D) = "white" {}
        _ChestTex("Clothed chest bounce crop", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Blink("Closure", Range(0,1)) = 0
        _ChestL("Left chest key", Range(0,1)) = 0
        _ChestR("Right chest key", Range(0,1)) = 0
        _FaceRect("Face crop UV rectangle", Vector) = (0.45410156,0.77864583,0.146484375,0.07161458)
        _ChestRect("Chest crop UV rectangle", Vector) = (0.45703125,0.66927083,0.23046875,0.1171875)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _BlinkTex, _ChestTex;
            float4 _Color, _FaceRect, _ChestRect;
            float _Blink, _ChestL, _ChestR;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float eye(float2 uv,float2 center,float2 radius)
            {
                float r=length((uv-center)/radius);
                return 1-smoothstep(.72,1,r);
            }
            float globe(float2 uv,float2 center,float2 radius)
            {
                float r=length((uv-center)/radius);
                return 1-smoothstep(.55,1,r);
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                float2 chest=(i.uv-_ChestRect.xy)/_ChestRect.zw;
                float inChest=step(0,chest.x)*step(0,chest.y)*step(chest.x,1)*step(chest.y,1);
                float gL=globe(chest,float2(.271,.556),float2(.195,.289));
                float gR=globe(chest,float2(.712,.578),float2(.246,.300));
                float gap=smoothstep(.40,.47,chest.x)*(1-smoothstep(.53,.60,chest.x));
                float cm=saturate((gL*_ChestL+gR*_ChestR)*(1-gap)*inChest);
                fixed3 bounce=tex2D(_ChestTex,saturate(chest)).rgb;
                original.rgb=lerp(original.rgb,bounce,cm);
                float2 face=(i.uv-_FaceRect.xy)/_FaceRect.zw;
                float mask=max(eye(face,float2(.365,.636),float2(.165,.17)),
                               eye(face,float2(.625,.453),float2(.14,.16)));
                fixed3 closed=tex2D(_BlinkTex,saturate(face)).rgb;
                original.rgb=lerp(original.rgb,closed,saturate(mask*smoothstep(.2,.55,_Blink)));
                return original*_Color;
            }
            ENDCG
        }
    }
}
