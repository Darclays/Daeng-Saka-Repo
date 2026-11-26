Shader "Daeng Saka/Launching/Pixel Atlas"
{
    Properties
    {
        [PerRendererData] _MainTex ("Atlas", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
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
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;

            struct VertexInput
            {
                float4 position : POSITION;
                fixed4 tint : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct FragmentInput
            {
                float4 position : SV_POSITION;
                fixed4 tint : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
            };

            FragmentInput Vertex(VertexInput input)
            {
                FragmentInput output;
                output.position = UnityObjectToClipPos(input.position);
                output.tint = input.tint;
                output.uv = input.uv;
                output.localPosition = input.position.xy;
                return output;
            }

            fixed4 Fragment(FragmentInput input) : SV_Target
            {
                // Pixel-center sampling at mip zero keeps the source import settings untouched.
                float2 uv = (floor(input.uv * _MainTex_TexelSize.zw) + .5) * _MainTex_TexelSize.xy;
                fixed4 pixel = tex2Dlod(_MainTex, float4(uv, 0, 0));
                float luma = dot(pixel.rgb, float3(.299, .587, .114));
                pixel.rgb = lerp(luma.xxx, pixel.rgb, input.tint.r);
                pixel.a *= input.tint.a;
                #ifdef UNITY_UI_CLIP_RECT
                    pixel.a *= UnityGet2DClipping(input.localPosition, _ClipRect);
                #endif
                return pixel;
            }
            ENDCG
        }
    }
}
