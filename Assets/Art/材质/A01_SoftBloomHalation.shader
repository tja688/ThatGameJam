Shader "Hidden/FullScreen/A01_SoftBloomHalation"
{
    Properties
    {
        _Threshold("Threshold", Range(0, 2)) = 1.0
        _Knee("Knee", Range(0, 1)) = 0.5
        _Radius("Radius", Range(0, 3)) = 1.2
        _Intensity("Intensity", Range(0, 2)) = 0.6
        _HalationTint("Halation Tint", Color) = (1, 0.86, 0.72, 1)
        _HalationStrength("Halation Strength", Range(0, 1)) = 0.25
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
            float4 _BlitTexture_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _Threshold;
                float _Knee;
                float _Radius;
                float _Intensity;
                float4 _HalationTint;
                float _HalationStrength;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 BloomSource(float2 uv)
            {
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
                float luma = Luma(color);
                float knee = max(_Knee, 1e-5);
                float soft = saturate((luma - _Threshold + knee) / (2.0 * knee));
                float highlight = max(luma - _Threshold, 0.0);
                highlight = max(highlight, soft * soft * knee);
                return (luma > 1e-5) ? color * (highlight / luma) : 0.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
                float2 o = _BlitTexture_TexelSize.xy * _Radius;

                float3 bloom = BloomSource(uv) * 0.28;
                bloom += BloomSource(uv + float2(o.x, 0.0)) * 0.13;
                bloom += BloomSource(uv - float2(o.x, 0.0)) * 0.13;
                bloom += BloomSource(uv + float2(0.0, o.y)) * 0.13;
                bloom += BloomSource(uv - float2(0.0, o.y)) * 0.13;
                bloom += BloomSource(uv + o) * 0.05;
                bloom += BloomSource(uv - o) * 0.05;
                bloom += BloomSource(uv + float2(o.x, -o.y)) * 0.05;
                bloom += BloomSource(uv + float2(-o.x, o.y)) * 0.05;

                float3 halation = bloom * _HalationTint.rgb * _HalationStrength;
                float3 result = baseColor + bloom * _Intensity + halation;
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
