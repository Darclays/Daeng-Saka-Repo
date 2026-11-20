Shader "Hidden/GamecommMentor/DaengSaka/DialogueWorldBlur"
{
    Properties
    {
        _Radius("Radius (pixels)", Range(0.5, 6)) = 2.6
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Dialogue World Blur"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment BlurFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
            CBUFFER_END

            half4 SampleWorld(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            half4 BlurFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 stepUV = _Radius * _BlitTexture_TexelSize.xy;
                float2 dx = float2(stepUV.x, 0.0);
                float2 dy = float2(0.0, stepUV.y);
                half4 center = SampleWorld(uv);
                half4 sides = SampleWorld(uv + dx) + SampleWorld(uv - dx)
                            + SampleWorld(uv + dy) + SampleWorld(uv - dy);
                half4 corners = SampleWorld(uv + dx + dy) + SampleWorld(uv + dx - dy)
                              + SampleWorld(uv - dx + dy) + SampleWorld(uv - dx - dy);
                return center * 0.25h + sides * 0.125h + corners * 0.0625h;
            }
            ENDHLSL
        }
    }
}
