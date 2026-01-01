Shader "Hidden/FullScreen/A03_ScreenSpaceHaze"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.65, 0.75, 0.85, 1)
        _FogStartY("Fog Start Y", Range(0, 1)) = 0.25
        _FogEndY("Fog End Y", Range(0, 1)) = 0.95
        _FogDensity("Fog Density", Range(0, 2)) = 1.0
        _FogCurve("Fog Curve", Range(0.5, 4)) = 1.6
        _HighlightClear("Highlight Clear", Range(0, 1)) = 0.35
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
                float4 _FogColor;
                float _FogStartY;
                float _FogEndY;
                float _FogDensity;
                float _FogCurve;
                float _HighlightClear;
            CBUFFER_END

            float Luma(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).rgb;

                float startY = _FogStartY;
                float endY = _FogEndY;
                float rangeY = max(abs(endY - startY), 1e-5);
                float fogT = (endY >= startY) ? saturate((uv.y - startY) / rangeY)
                                              : saturate((startY - uv.y) / rangeY);
                fogT = pow(fogT, _FogCurve) * _FogDensity;
                fogT = saturate(fogT);

                float luma = Luma(color);
                fogT *= 1.0 - _HighlightClear * saturate(luma);

                float3 result = lerp(color, _FogColor.rgb, fogT);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
