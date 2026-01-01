Shader "Hidden/FullScreen/B01_PaperWashPastel"
{
    Properties
    {
        _Lift("Lift", Range(0, 0.25)) = 0.08
        _Contrast("Contrast", Range(0.6, 1.2)) = 0.9
        _Saturation("Saturation", Range(0, 1.2)) = 0.85
        _PaperTint("Paper Tint", Color) = (1.0, 0.96, 0.92, 1)
        _TintStrength("Tint Strength", Range(0, 1)) = 0.25
        _HighlightRolloff("Highlight Rolloff", Range(0, 1)) = 0.35
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
                float _Lift;
                float _Contrast;
                float _Saturation;
                float4 _PaperTint;
                float _TintStrength;
                float _HighlightRolloff;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv).rgb;

                float luma = Luma(color);
                color += (1.0 - luma) * _Lift;

                color = (color - 0.5) * _Contrast + 0.5;
                color = max(color, 0.0);

                color = color / (1.0 + color * _HighlightRolloff);

                float postLuma = Luma(color);
                color = lerp(float3(postLuma, postLuma, postLuma), color, _Saturation);

                color = lerp(color, color * _PaperTint.rgb, _TintStrength);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
