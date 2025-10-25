Shader "Universal Render Pipeline/Custom/ExclamationMarkHighlight"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,0.8,0,1)
        _EmissionIntensity("Emission Intensity", Float) = 5.0
        _FresnelPower("Fresnel Power", Float) = 2.0
        _PulseSpeed("Pulse Speed", Float) = 1.5
        _PulseAmount("Pulse Amount", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _FresnelPower;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // 简化版本：直接使用内置函数
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                // 手动计算法线和视角方向
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // 采样纹理
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float4 finalColor = baseColor * _BaseColor;
                
                // 菲涅尔效应
                float fresnel = pow(1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _FresnelPower);
                
                // 脉冲效果
                float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * _PulseAmount + (1.0 - _PulseAmount);
                
                // 发光效果
                float3 emission = _EmissionColor.rgb * _EmissionIntensity * fresnel * pulse;
                finalColor.rgb += emission;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}