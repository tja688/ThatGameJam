Shader "Hidden/FullScreen/A11_FilmGrainDust"
{
    Properties
    {
        _GrainStrength("Grain Strength", Range(0, 0.6)) = 0.18
        _GrainSize("Grain Size", Range(0.5, 4)) = 1.6
        _LumaResponse("Luma Response", Range(0, 1)) = 0.7
        _DustStrength("Dust Strength", Range(0, 0.3)) = 0.06
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
                float _GrainStrength;
                float _GrainSize;
                float _LumaResponse;
                float _DustStrength;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.23));
                p += dot(p, p + 34.45);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;

                float2 pixelPos = uv * _BlitTexture_TexelSize.zw;
                float2 grainUV = pixelPos / max(_GrainSize, 0.001);
                float noise = Hash21(grainUV + _Time.y * 0.5);
                float grain = (noise - 0.5) * 2.0;

                float luma = Luma(color);
                float darkBoost = lerp(1.0, 1.0 + (1.0 - luma), _LumaResponse);
                float grainStrength = _GrainStrength * darkBoost;
                color += grain * grainStrength;

                float dustNoise = Hash21(grainUV * 0.2 + floor(_Time.y * 2.0));
                float dust = step(0.995, dustNoise) * _DustStrength;
                color += dust;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
