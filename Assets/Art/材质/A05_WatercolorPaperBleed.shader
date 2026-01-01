Shader "Hidden/FullScreen/A05_WatercolorPaperBleed"
{
    Properties
    {
        _PaperTex("Paper Tex", 2D) = "white" {}
        _PaperStrength("Paper Strength", Range(0, 0.3)) = 0.12
        _PaperTexTiling("Paper Tex Tiling", Range(1, 8)) = 3.0
        _PaperUseTex("Paper Use Tex", Range(0, 1)) = 0
        _BleedRadius("Bleed Radius", Range(0, 2)) = 0.8
        _BleedStrength("Bleed Strength", Range(0, 1)) = 0.25
        _Blend("Blend", Range(0, 1)) = 0.9

        [Header(World Space Locking)]
        [Toggle] _PaperWorldLock("World Space Lock", Float) = 1
        _PaperWorldTiling("World Tiling", Float) = 0.5
        _PaperWorldOffset("World Offset", Vector) = (0, 0, 0, 0)
        // Helper properties (usually set by script)
        _PaperOrthoSize("Ortho Size (Script)", Float) = 5
        _PaperCamPos("Camera Pos (Script)", Vector) = (0, 0, 0, 0)

        [Header(Highlight Protection)]
        _HLProtect("Highlight Protection", Range(0, 1)) = 0.6
        _HLStart("Highlight Start", Range(0, 2)) = 0.8
        _HLEnd("Highlight End", Range(0, 2)) = 1.2
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

            TEXTURE2D(_PaperTex);
            SAMPLER(sampler_PaperTex);

            CBUFFER_START(UnityPerMaterial)
                float _PaperStrength;
                float _PaperTexTiling;
                float _PaperUseTex;
                float _BleedRadius;
                float _BleedStrength;
                float _Blend;
                
                float _PaperWorldLock;
                float _PaperWorldTiling;
                float4 _PaperWorldOffset;
                float _PaperOrthoSize;
                float4 _PaperCamPos;

                float _HLProtect;
                float _HLStart;
                float _HLEnd;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 baseColor = SampleColor(uv);

                float2 paperUV;
                if (_PaperWorldLock > 0.5)
                {
                    float aspect = _ScreenParams.x / _ScreenParams.y;
                    float2 worldSize = float2(2.0 * _PaperOrthoSize * aspect, 2.0 * _PaperOrthoSize);
                    float2 worldPos = _PaperCamPos.xy + (uv - 0.5) * worldSize;
                    paperUV = (worldPos + _PaperWorldOffset.xy) * _PaperWorldTiling;
                }
                else
                {
                    paperUV = uv * _PaperTexTiling;
                }

                float paperTex = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).r;
                float paperNoise = Hash21(paperUV * 8.0);
                float paperSample = lerp(paperNoise, paperTex, saturate(_PaperUseTex));

                // Highlight Protection
                float luma = dot(baseColor, float3(0.2126, 0.7152, 0.0722));
                float highlightMask = smoothstep(_HLStart, _HLEnd, luma);
                float paperStrengthFinal = _PaperStrength * (1.0 - highlightMask * _HLProtect);

                float paperFactor = 1.0 + (paperSample - 0.5) * 2.0 * paperStrengthFinal;

                float2 o = _BlitTexture_TexelSize.xy * _BleedRadius;
                float3 blur = baseColor * 0.4;
                blur += SampleColor(uv + float2(o.x, 0.0)) * 0.15;
                blur += SampleColor(uv - float2(o.x, 0.0)) * 0.15;
                blur += SampleColor(uv + float2(0.0, o.y)) * 0.15;
                blur += SampleColor(uv - float2(0.0, o.y)) * 0.15;

                float3 bleed = lerp(baseColor, blur, _BleedStrength);
                float3 papered = bleed * paperFactor;
                float3 result = lerp(baseColor, papered, _Blend);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
