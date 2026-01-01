Shader "Hidden/FullScreen/A10_DreamLens"
{
    Properties
    {
        _CA_Strength("CA Strength", Range(0, 1)) = 0.12
        _DistortStrength("Distort Strength", Range(0, 1)) = 0.08
        _DistortSpeed("Distort Speed", Range(0, 2)) = 0.3
        _DistortScale("Distort Scale", Range(0.5, 4)) = 1.2
        _EdgeOnly("Edge Only", Range(0, 1)) = 0.7
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
                float _CA_Strength;
                float _DistortStrength;
                float _DistortSpeed;
                float _DistortScale;
                float _EdgeOnly;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centered = uv - 0.5;
                float radius = length(centered);
                float edge = lerp(1.0, saturate(radius * 2.0), _EdgeOnly);

                float2 wave = sin((uv * _DistortScale + _Time.y * _DistortSpeed) * 6.2831);
                float2 distort = wave * (_DistortStrength * 0.002);
                float2 baseUV = uv + distort;

                float2 caOffset = centered * (_CA_Strength * 0.004) * edge;

                float3 color;
                color.r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, baseUV + caOffset).r;
                color.g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, baseUV).g;
                color.b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, baseUV - caOffset).b;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
