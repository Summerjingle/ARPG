Shader "Custom/OutlineOnly_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        // ---- 单一 Pass：只画描边（背面外扩），不画本体 ----
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front          // 只渲染背面，外扩后形成描边
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 顶点 → 裁剪空间
                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 法线 → 世界空间 → 裁剪空间方向
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 normalCS = TransformWorldToHClipDir(normalWS);

                // 在裁剪空间沿法线方向外扩
                // 乘以 positionCS.w 让描边在屏幕上宽度一致（不受距离影响）
                float2 offset = normalize(normalCS.xy) * _OutlineWidth * positionCS.w;
                positionCS.xy += offset;

                OUT.positionCS = positionCS;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
