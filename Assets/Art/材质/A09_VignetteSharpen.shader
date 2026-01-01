Shader "Hidden/FullScreen/A09_VignetteSharpen"
{
    Properties
    {
        _VignetteStrength("Vignette Strength", Range(0, 0.8)) = 0.25
        _VignetteRoundness("Vignette Roundness", Range(0, 1)) = 0.6
        _SharpenAmount("Sharpen Amount", Range(0, 1)) = 0.18
        _SharpenRadius("Sharpen Radius", Range(0, 2)) = 0.8
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
                float _VignetteStrength;
                float _VignetteRoundness;
                float _SharpenAmount;
                float _SharpenRadius;
            CBUFFER_END

            float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 color = SampleColor(uv);

                float2 o = _BlitTexture_TexelSize.xy * _SharpenRadius;
                float3 blur = color * 0.4;
                blur += SampleColor(uv + float2(o.x, 0.0)) * 0.15;
                blur += SampleColor(uv - float2(o.x, 0.0)) * 0.15;
                blur += SampleColor(uv + float2(0.0, o.y)) * 0.15;
                blur += SampleColor(uv - float2(0.0, o.y)) * 0.15;

                float3 sharpened = color + (color - blur) * _SharpenAmount;

                float2 centered = uv - 0.5;
                float aspect = _BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w;
                float2 scaled = centered * float2(lerp(1.0, aspect, _VignetteRoundness), 1.0);
                float vignette = smoothstep(0.4, 0.9, length(scaled));

                float3 result = sharpened * (1.0 - vignette * _VignetteStrength);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
