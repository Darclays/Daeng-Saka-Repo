Shader "GamecommMentor/Daeng Saka/Battle Sprite Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Radius ("Blur in source pixels", Float) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Vertex
            {
                float4 position : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Radius;

            Varyings Vert(Vertex input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half4 SamplePremultiplied(float2 uv)
            {
                half4 texel = tex2D(_MainTex, uv);
                texel.rgb *= texel.a;
                return texel;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _Radius;
                float2 x = float2(offset.x, 0);
                float2 y = float2(0, offset.y);
                half4 result = SamplePremultiplied(input.uv) * 4;
                result += (SamplePremultiplied(input.uv - x) + SamplePremultiplied(input.uv + x)
                    + SamplePremultiplied(input.uv - y) + SamplePremultiplied(input.uv + y)) * 2;
                result += SamplePremultiplied(input.uv - x - y) + SamplePremultiplied(input.uv + x - y)
                    + SamplePremultiplied(input.uv - x + y) + SamplePremultiplied(input.uv + x + y);
                result *= 0.0625;
                result.rgb *= input.color.rgb * input.color.a;
                result.a *= input.color.a;
                return result;
            }
            ENDCG
        }
    }
}
