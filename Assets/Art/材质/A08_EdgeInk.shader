Shader "Hidden/FullScreen/A08_EdgeInk"
{
    Properties
    {
        _EdgeWidth("Edge Width", Range(0.5, 2.0)) = 1.0
        _Threshold("Threshold", Range(0, 1)) = 0.18
        _InkColor("Ink Color", Color) = (0.02, 0.08, 0.10, 1)
        _InkStrength("Ink Strength", Range(0, 1)) = 0.55
        _OnlyDarken("Only Darken", Range(0, 1)) = 1
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
                float _EdgeWidth;
                float _Threshold;
                float4 _InkColor;
                float _InkStrength;
                float _OnlyDarken;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float SampleLuma(float2 uv)
            {
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
                return Luma(color);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 o = _BlitTexture_TexelSize.xy * _EdgeWidth;

                float lTL = SampleLuma(uv + float2(-o.x, o.y));
                float lTC = SampleLuma(uv + float2(0.0, o.y));
                float lTR = SampleLuma(uv + float2(o.x, o.y));
                float lML = SampleLuma(uv + float2(-o.x, 0.0));
                float lMR = SampleLuma(uv + float2(o.x, 0.0));
                float lBL = SampleLuma(uv + float2(-o.x, -o.y));
                float lBC = SampleLuma(uv + float2(0.0, -o.y));
                float lBR = SampleLuma(uv + float2(o.x, -o.y));

                float gx = -lTL - 2.0 * lML - lBL + lTR + 2.0 * lMR + lBR;
                float gy = -lTL - 2.0 * lTC - lTR + lBL + 2.0 * lBC + lBR;
                float edge = length(float2(gx, gy));
                edge = saturate((edge - _Threshold) / max(1.0 - _Threshold, 1e-5));

                float3 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
                float3 inked = lerp(baseColor, _InkColor.rgb, edge * _InkStrength);

                float onlyDarken = step(0.5, _OnlyDarken);
                float3 result = lerp(inked, min(baseColor, inked), onlyDarken);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
