Shader "Hidden/FullScreen/A07_PosterizeDither"
{
    Properties
    {
        _Levels("Levels", Range(2, 16)) = 8
        _DitherStrength("Dither Strength", Range(0, 1)) = 0.35
        _DitherScale("Dither Scale", Range(1, 4)) = 2
        _HighlightProtect("Highlight Protect", Range(0, 1)) = 0.6
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
                float _Levels;
                float _DitherStrength;
                float _DitherScale;
                float _HighlightProtect;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float Bayer4(int2 p)
            {
                int x = p.x & 3;
                int y = p.y & 3;
                float4 row = float4(0.0, 8.0, 2.0, 10.0);
                if (y == 1) row = float4(12.0, 4.0, 14.0, 6.0);
                if (y == 2) row = float4(3.0, 11.0, 1.0, 9.0);
                if (y == 3) row = float4(15.0, 7.0, 13.0, 5.0);
                float value = row.x;
                if (x == 1) value = row.y;
                if (x == 2) value = row.z;
                if (x == 3) value = row.w;
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
                float luma = Luma(color);

                float levels = max(_Levels, 2.0);
                float2 pixel = uv * _BlitTexture_TexelSize.zw * _DitherScale;
                int2 p = int2(floor(pixel));
                float threshold = (Bayer4(p) + 0.5) / 16.0;

                float ditherStrength = _DitherStrength * (1.0 - _HighlightProtect * luma);
                float dither = (threshold - 0.5) * ditherStrength;

                float3 quant = floor(color * (levels - 1.0) + dither) / (levels - 1.0);
                quant = saturate(quant);
                return half4(quant, 1.0);
            }
            ENDHLSL
        }
    }
}
