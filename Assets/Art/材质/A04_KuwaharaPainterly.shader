Shader "Hidden/FullScreen/A04_KuwaharaPainterly"
{
    Properties
    {
        _KernelSize("Kernel Size", Range(3, 12)) = 6
        _EdgePreserve("Edge Preserve", Range(0, 1)) = 0.6
        _Blend("Blend", Range(0, 1)) = 0.85
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
                float _KernelSize;
                float _EdgePreserve;
                float _Blend;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;
            }

            void Quadrant(float2 uv, float2 o1, float2 o2, float2 o3, float2 o4, out float3 mean, out float variance)
            {
                float3 c1 = SampleColor(uv + o1);
                float3 c2 = SampleColor(uv + o2);
                float3 c3 = SampleColor(uv + o3);
                float3 c4 = SampleColor(uv + o4);

                mean = (c1 + c2 + c3 + c4) * 0.25;

                float l1 = Luma(c1);
                float l2 = Luma(c2);
                float l3 = Luma(c3);
                float l4 = Luma(c4);
                float meanL = (l1 + l2 + l3 + l4) * 0.25;
                variance = (l1 * l1 + l2 * l2 + l3 * l3 + l4 * l4) * 0.25 - meanL * meanL;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 baseColor = SampleColor(uv);

                float2 o = _BlitTexture_TexelSize.xy * _KernelSize;
                float2 h = o * 0.5;

                float3 mean0; float var0;
                float3 mean1; float var1;
                float3 mean2; float var2;
                float3 mean3; float var3;

                Quadrant(uv, float2(-o.x, -o.y), float2(-o.x, 0.0), float2(0.0, -o.y), float2(-h.x, -h.y), mean0, var0);
                Quadrant(uv, float2(o.x, -o.y), float2(o.x, 0.0), float2(0.0, -o.y), float2(h.x, -h.y), mean1, var1);
                Quadrant(uv, float2(-o.x, o.y), float2(-o.x, 0.0), float2(0.0, o.y), float2(-h.x, h.y), mean2, var2);
                Quadrant(uv, float2(o.x, o.y), float2(o.x, 0.0), float2(0.0, o.y), float2(h.x, h.y), mean3, var3);

                float3 filtered = mean0;
                float bestVar = var0;
                if (var1 < bestVar) { bestVar = var1; filtered = mean1; }
                if (var2 < bestVar) { bestVar = var2; filtered = mean2; }
                if (var3 < bestVar) { filtered = mean3; }

                float2 e = _BlitTexture_TexelSize.xy;
                float lL = Luma(SampleColor(uv - float2(e.x, 0.0)));
                float lR = Luma(SampleColor(uv + float2(e.x, 0.0)));
                float lU = Luma(SampleColor(uv + float2(0.0, e.y)));
                float lD = Luma(SampleColor(uv - float2(0.0, e.y)));
                float edge = saturate((abs(lL - lR) + abs(lU - lD)) * 2.0);

                float effect = _Blend * (1.0 - edge * _EdgePreserve);
                float3 result = lerp(baseColor, filtered, saturate(effect));
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
