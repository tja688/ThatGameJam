Shader "Hidden/FullScreen/A06_GradientMap"
{
    Properties
    {
        _GradientTex("Gradient Tex", 2D) = "white" {}
        _UseGradientTex("Use Gradient Tex", Range(0, 1)) = 0
        _Strength("Strength", Range(0, 1)) = 0.75
        _PreserveSaturation("Preserve Saturation", Range(0, 1)) = 0.35
        _ColorA("Shadow Color", Color) = (0.10, 0.22, 0.28, 1)
        _ColorB("Mid Color", Color) = (0.55, 0.62, 0.70, 1)
        _ColorC("Highlight Color", Color) = (1.0, 0.72, 0.35, 1)
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

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);

            CBUFFER_START(UnityPerMaterial)
                float _UseGradientTex;
                float _Strength;
                float _PreserveSaturation;
                float4 _ColorA;
                float4 _ColorB;
                float4 _ColorC;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 ProceduralGradient(float luma)
            {
                float t = saturate(luma * 2.0);
                float3 low = lerp(_ColorA.rgb, _ColorB.rgb, t);
                float t2 = saturate((luma - 0.5) * 2.0);
                float3 high = lerp(_ColorB.rgb, _ColorC.rgb, t2);
                float useHigh = step(0.5, luma);
                return lerp(low, high, useHigh);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv).rgb;
                float luma = Luma(color);

                float3 mapped = ProceduralGradient(luma);
                float3 texMapped = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, float2(luma, 0.5)).rgb;
                mapped = lerp(mapped, texMapped, saturate(_UseGradientTex));

                float3 chroma = color - luma;
                mapped = saturate(mapped + chroma * _PreserveSaturation);

                float3 result = lerp(color, mapped, _Strength);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
