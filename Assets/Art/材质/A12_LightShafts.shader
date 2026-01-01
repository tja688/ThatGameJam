Shader "Hidden/FullScreen/A12_LightShafts"
{
    Properties
    {
        _Threshold("Threshold", Range(0, 2)) = 1.1
        _Steps("Steps", Range(4, 24)) = 12
        _ScatterStrength("Scatter Strength", Range(0, 1)) = 0.18
        _Decay("Decay", Range(0, 1)) = 0.85
        _Tint("Tint", Color) = (1.0, 0.82, 0.65, 1)
        _LightCenter("Light Center", Vector) = (0.5, 0.5, 0, 0)
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
                float _Threshold;
                float _Steps;
                float _ScatterStrength;
                float _Decay;
                float4 _Tint;
                float4 _LightCenter;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 ExtractHighlight(float3 color)
            {
                float luma = Luma(color);
                float h = max(luma - _Threshold, 0.0);
                return (luma > 1e-5) ? color * (h / luma) : 0.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;

                int steps = (int)_Steps;
                steps = min(max(steps, 1), 24);
                float invSteps = 1.0 / max((float)steps, 1.0);

                float2 center = saturate(_LightCenter.xy);
                float2 stepUV = (uv - center) * invSteps;

                float3 accum = 0.0;
                float weight = 1.0;
                float2 sampleUV = uv;

                [loop]
                for (int i = 0; i < 24; i++)
                {
                    if (i >= steps)
                        break;

                    sampleUV -= stepUV;
                    float3 sampleColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, sampleUV).rgb;
                    accum += ExtractHighlight(sampleColor) * weight;
                    weight *= _Decay;
                }

                float3 shafts = accum * (_ScatterStrength * invSteps);
                float3 result = baseColor + shafts * _Tint.rgb;
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
