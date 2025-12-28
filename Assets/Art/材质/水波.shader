Shader "Custom/URP_ScrollingWater"
{
    Properties
    {
        [MainTexture] _BaseMap("水池贴图", 2D) = "white" {}
        _ScrollSpeed("水平流动速度", Float) = 0.2
        _Opacity("透明度", Range(0, 1)) = 1.0
    }

    SubShader
    {
        // URP 标签，声明为通用渲染管线
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            // 开启透明混合
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 包含 URP 核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            // 使用 CBUFFER 包裹属性，使其兼容 SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _ScrollSpeed;
                float _Opacity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // 顶点着色器
            Varyings vert(Attributes input)
            {
                Varyings output;
                // 将物体空间坐标转换到裁剪空间
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 转换 UV 并应用 Tiling/Offset
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // 片元着色器
            half4 frag(Varyings input) : SV_Target
            {
                // 核心逻辑：获取 URP 的内置时间 _Time.y (等同于 Time.time)
                // 只修改 UV 的 x 分量实现水平位移
                float2 scrollingUV = input.uv;
                scrollingUV.x += _Time.y * _ScrollSpeed;

                // 采样贴图颜色
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, scrollingUV);
                
                // 应用透明度
                color.a *= _Opacity;

                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}