Shader "EmeraldInochi/Display" {
 Properties {_MainTex("Native surface",2D)="white"{}}
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Cull Off ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float4 frag(v2f i):SV_Target {
    float4 c=tex2D(_MainTex,i.uv);
    #ifndef UNITY_COLORSPACE_GAMMA
    c.rgb=GammaToLinearSpace(c.rgb/max(c.a,.0001))*c.a;
    #endif
    return c;
   }
   ENDCG
  }
 }
}
