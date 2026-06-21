Shader "Custom/Outline_URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        // ---- Pass 1: 描边（外扩） ----
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // 将顶点转换到裁剪空间
                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 法线转换到世界空间
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                // 法线转换到裁剪空间（用于偏移）
                float3 normalCS = TransformWorldToHClipDir(normalWS);
                
                // 顶点沿法线方向外扩
                positionCS.xy += normalize(normalCS.xy) * _OutlineWidth * 0.1;
                
                OUT.positionCS = positionCS;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ---- Pass 2: 本体渲染 ----
        Pass
        {
            Name "MAIN"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 col = tex2D(_MainTex, IN.uv) * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}