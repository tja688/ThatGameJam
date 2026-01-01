Shader "Hidden/FullScreen/A02_SplitToningFilmic"
{
    Properties
    {
        _ShadowColor("Shadow Color", Color) = (0.10, 0.22, 0.28, 1)
        _HighlightColor("Highlight Color", Color) = (1.0, 0.72, 0.35, 1)
        _Balance("Balance", Range(0, 1)) = 0.45
        _Contrast("Contrast", Range(0.5, 1.5)) = 1.1
        _Lift("Lift", Range(-0.1, 0.2)) = 0.05
        _Saturation("Saturation", Range(0, 2)) = 1.05
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "FullScreenPass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float4 _HighlightColor;
                float _Balance;
                float _Contrast;
                float _Lift;
                float _Saturation;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 ACESFilm(float3 x)
            {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv).rgb;
                float luma = Luma(color);
                float highlightW = smoothstep(_Balance - 0.1, _Balance + 0.1, luma);
                float3 tint = lerp(_ShadowColor.rgb, _HighlightColor.rgb, highlightW);
                color *= tint;

                color += _Lift;
                color = (color - 0.5) * _Contrast + 0.5;
                color = max(color, 0.0);
                color = ACESFilm(color);

                float postLuma = Luma(color);
                color = lerp(float3(postLuma, postLuma, postLuma), color, _Saturation);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
