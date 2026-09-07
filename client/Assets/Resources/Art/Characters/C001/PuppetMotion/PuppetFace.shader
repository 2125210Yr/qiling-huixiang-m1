Shader "Resonance/PuppetFace"
{
    Properties
    {
        _MainTex("Original", 2D) = "white" {}
        _BlinkTex("Closed eyes crop", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Blink("Closure", Range(0,1)) = 0
        _FaceRect("Face crop UV rectangle", Vector) = (0.45410156,0.77864583,0.146484375,0.07161458)
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
            sampler2D _MainTex, _BlinkTex;
            float4 _Color, _FaceRect;
            float _Blink;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float eye(float2 uv,float2 center,float2 radius)
            {
                float r=length((uv-center)/radius);
                return 1-smoothstep(.72,1,r);
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
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
