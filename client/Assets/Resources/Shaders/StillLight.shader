Shader "Resonance/UI/StillLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TimeU ("Unscaled", Float) = 0
        _Gain ("Gain", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "StillLight"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _TimeU;
            float _Gain;
            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float blob(float2 uv, float2 c, float r)
            {
                float2 d = (uv - c) * float2(1.0, 1.78);
                return exp(-dot(d, d) / max(r * r, 1e-4));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a;
                if (mask < 0.04) return 0;
                float t = _TimeU;
                float2 c0 = float2(0.54 + 0.05 * sin(t * 0.33), 0.60 + 0.04 * sin(t * 0.27 + 0.6));
                float r0 = 0.20 + 0.03 * sin(t * 0.41);
                float u = 0.5 + 0.5 * sin(t * 0.48);
                float2 blade = lerp(float2(0.30, 0.44), float2(0.17, 0.11), 0.08 + u * 0.80);
                float g0 = blob(i.uv, c0, r0);
                float g1 = blob(i.uv, blade, 0.16);
                float2 rayP = i.uv - float2(0.62 + 0.03 * sin(t * 0.22), 0.82);
                float2 ray = float2(rayP.x * 3.2 + rayP.y * 0.45, rayP.y * 0.55);
                float gRay = exp(-dot(ray, ray) / 0.06) * (0.45 + 0.35 * sin(t * 0.62));
                float3 ice = float3(0.52, 0.82, 1.0);
                float3 rim = float3(0.42, 0.78, 1.0);
                float3 rgb = ice * g0 * 0.42 + rim * g1 * 1.05 + ice * gRay * 0.32;
                float a = saturate(g0 * 0.32 + g1 * 0.72 + gRay * 0.22) * mask * _Gain;
                return fixed4(rgb * i.color.rgb, a * i.color.a);
            }
            ENDCG
        }
    }
}
