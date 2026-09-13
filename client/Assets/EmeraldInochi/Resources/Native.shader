Shader "EmeraldInochi/Native" {
 Properties { _Src("Source",Int)=1 _Dst("Destination",Int)=10 _SrcAlpha("Source Alpha",Int)=1 _DstAlpha("Destination Alpha",Int)=10 }
 SubShader {
  Cull Off ZWrite Off ZTest Always
  CGINCLUDE
  #include "UnityCG.cginc"
  struct Vertex { float2 position:POSITION; float2 uv:TEXCOORD0; };
  struct Interpolator { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float2 maskUV:TEXCOORD1; };
  float4x4 _NativeProjection,_NativeClip;
  sampler2D _NativeTexture,_NativeMask;
  float4 _NativeTint,_NativeScreen;
  float _NativeOpacity,_NativeClipped;
  Interpolator vert(Vertex v) {
   Interpolator o; o.position=mul(_NativeProjection,float4(v.position.x,-v.position.y,0,1));
   o.uv=v.uv; o.maskUV=float2(o.position.x*.5+.5,.5-o.position.y*.5);return o;
  }
  Interpolator composite(Vertex v) {
   Interpolator o; o.position=mul(_NativeClip,float4(v.position,0,1));
   o.uv=float2(o.position.x*.5+.5,.5-o.position.y*.5);o.maskUV=o.uv;return o;
  }
  float4 frag(Interpolator i):SV_Target {
   if(any(i.uv<0)||any(i.uv>1))return 0;
   float4 c=tex2D(_NativeTexture,i.uv);
   c.rgb=(1-(1-c.rgb)*(1-_NativeScreen.rgb*c.a))*_NativeTint.rgb;
   c*=_NativeOpacity;
   if(_NativeClipped>.5)c*=saturate(tex2D(_NativeMask,i.maskUV).r);
   return c;
  }
  float4 maskFrag(Interpolator i):SV_Target {
   if(any(i.uv<0)||any(i.uv>1))return 0;
   return tex2D(_NativeTexture,i.uv).a;
  }
  ENDCG
  Pass { Blend [_Src] [_Dst], [_SrcAlpha] [_DstAlpha]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   ENDCG
  }
  Pass { Blend One One
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment maskFrag
   ENDCG
  }
  Pass { Blend [_Src] [_Dst], [_SrcAlpha] [_DstAlpha]
   CGPROGRAM
   #pragma vertex composite
   #pragma fragment frag
   ENDCG
  }
 }
}
