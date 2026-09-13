Shader "Resonance/UI/IceFloor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Horizon ("Horizon", Range(0.38, 0.62)) = 0.47
        _WarpAmp ("Warp Amp", Float) = 0.06
        _WarpSpeed ("Warp Speed", Float) = 0.78
        _WarpTime ("Warp Time", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "IceFloor"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 world : TEXCOORD1;
            };

            float4 _Color;
            float4 _ClipRect;
            float _Horizon;
            float _WarpAmp;
            float _WarpSpeed;
            float _WarpTime;
            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.world = v.vertex;
                return o;
            }

            float checker(float2 p)
            {
                float2 g = floor(p);
                float c = fmod(abs(g.x) + abs(g.y), 2.0);
                float2 w = max(fwidth(p), 1e-5);
                float2 f = frac(p);
                float2 edge = min(f, 1.0 - f) / w;
                float t = saturate(min(edge.x, edge.y));
                return lerp(0.5, c, smoothstep(0.0, 1.0, t));
            }

            float frostLine(float2 p)
            {
                float2 g = abs(frac(p) - 0.5);
                float2 w = max(fwidth(p) * 1.6, 1e-5);
                return saturate(1.0 - min(g.x / w.x, g.y / w.y));
            }

            float2 bend(float2 p, float t, float amp)
            {
                // Traveling waves: square edges become S-curves.
                float w1 = sin(p.y * 1.45 + t * 1.70);
                float w2 = sin(p.x * 1.20 + p.y * 0.55 + t * 1.35);
                float w3 = sin((p.x + p.y) * 0.90 - t * 0.95);
                float w4 = cos(p.x * 0.70 - p.y * 1.10 + t * 0.80);
                p.x += (w1 * 0.85 + w3 * 0.40) * amp;
                p.y += (w2 * 0.70 + w4 * 0.35) * amp;
                return p;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _WarpTime > 0.0 ? _WarpTime : _Time.y;
                float horizon = _Horizon;
                // Near-black DC void, ice only as a hint in the tiles.
                float3 iceDark = float3(0.032, 0.040, 0.056);
                float3 iceLite = float3(0.11, 0.145, 0.185);
                float3 voidCol = float3(0.006, 0.008, 0.014);
                float3 lamp = float3(0.22, 0.42, 0.52);
                float3 col = voidCol;

                float lampR = exp(-((uv.x - 0.50) * (uv.x - 0.50) * 16.0
                    + (uv.y - 0.44) * (uv.y - 0.44) * 22.0));
                col += lamp * lampR * 0.10;
                float vigSky = saturate(1.05 - abs(uv.x - 0.5) * 0.85 - uv.y * 0.22);
                col *= vigSky;

                if (uv.y < horizon)
                {
                    float d = saturate((horizon - uv.y) / max(horizon, 0.001));
                    float persp = 0.10 + d * 1.85;
                    float wx = (uv.x - 0.5) / persp * 6.5;
                    float wz = (1.0 - d) * 10.2;
                    float2 p = bend(float2(wx, wz), t * _WarpSpeed, _WarpAmp);
                    float ch = checker(p);
                    float3 floorCol = lerp(iceDark, iceLite, ch);
                    float frost = frostLine(p);
                    float glint = pow(saturate(sin(p.x * 3.1 + p.y * 2.0 - t * 1.35)), 28.0);
                    glint += pow(saturate(sin(p.x * 5.4 - p.y * 1.6 + t * 1.85)), 34.0) * 0.65;
                    floorCol += float3(0.38, 0.62, 0.82) * (frost * 0.08 + glint * 0.10) * d;
                    float vig = saturate(1.20 - abs(uv.x - 0.5) * 1.25 - (1.0 - d) * 0.55);
                    floorCol *= vig;
                    float join = saturate(d / 0.22);
                    col = lerp(col, floorCol, join);
                }

                return fixed4(col, 1) * i.color;
            }
            ENDCG
        }
    }
}
