Shader "Custom/Charge Sweep Gold"
{
    Properties
    {
        // ── Base ──────────────────────────────────────────────
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor("Color", Color) = (1,1,1,1)

        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0.0

        [HDR] _EmissionColor("Base Emission", Color) = (0,0,0,0)

        // ── Sweep ─────────────────────────────────────────────
        [Header(Sweep)]
        _SweepProgress("Sweep Progress (0=feet, 1=head)", Range(0, 1)) = 0
        _SweepWidth("Sweep Band Width", Range(0.01, 0.5)) = 0.08
        _SweepSoftness("Sweep Edge Softness", Range(0.001, 0.2)) = 0.03

        // ── Gold Rim ──────────────────────────────────────────
        [Header(Gold Rim)]
        [HDR] _GoldColor("Gold Color", Color) = (1.0, 0.75, 0.15, 1)
        _GoldEmission("Gold Emission Intensity", Range(0, 10)) = 3
        _RimPower("Rim Power", Range(0.1, 8)) = 2.5
        _RimScale("Rim Scale", Range(0, 2)) = 1.0

        // ── Mesh Bounds (object-space Y) ──────────────────────
        [Header(Bounds)]
        _MeshMinY("Mesh Min Y (feet)", Float) = 0.0
        _MeshMaxY("Mesh Max Y (head)", Float) = 2.0

        // ── Blend state ───────────────────────────────────────
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

        // Obsolete
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1,1,1,1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF

            // URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ _LIGHT_LAYERS

            // ── Includes ──────────────────────────────────────
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            // ── Textures & Samplers ───────────────────────────
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            // ── Uniforms ──────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BumpMap_ST;
                float  _BumpScale;
                float  _Smoothness;
                float  _Metallic;
                float4 _EmissionColor;

                float  _SweepProgress;
                float  _SweepWidth;
                float  _SweepSoftness;
                float4 _GoldColor;
                float  _GoldEmission;
                float  _RimPower;
                float  _RimScale;
                float  _MeshMinY;
                float  _MeshMaxY;
                float  _Surface;
            CBUFFER_END

            // ── Vertex Input / Output ─────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float3 viewDirWS  : TEXCOORD4;
                float  objY       : TEXCOORD5;   // object-space Y (for sweep)
            #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                float2 lightmapUV : TEXCOORD6;
            #endif
            };

            // ── Helpers ───────────────────────────────────────
            float3 SafeNormalize(float3 v)
            {
                float lenSq = dot(v, v);
                return lenSq > 0.000001 ? v * rsqrt(lenSq) : float3(0, 1, 0);
            }

            // ── Vertex Shader ─────────────────────────────────
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = posInput.positionCS;
                output.positionWS = posInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.objY = input.positionOS.y;   // raw object-space Y

                // Normal
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.viewDirWS = GetWorldSpaceViewDir(posInput.positionWS);

            #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                output.lightmapUV = input.lightmapUV;
            #endif

                return output;
            }

            // ── Fragment Shader ───────────────────────────────
            float4 LitPassFragment(Varyings input) : SV_Target
            {
                // ── Sample textures ───────────────────────────
                float2 uv = input.uv;
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

            #ifdef _ALPHATEST_ON
                clip(albedo.a - 0.5);
            #endif

                // Normal
                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                float3 normalWS = SafeNormalize(
                    normalTS.x * input.tangentWS.xyz +
                    normalTS.y * bitangent +
                    normalTS.z * input.normalWS);

                float3 viewDirWS = SafeNormalize(input.viewDirWS);

                // ── Sweep: normalize object-space Y ───────────
                float range = _MeshMaxY - _MeshMinY;
                float normalizedHeight = (input.objY - _MeshMinY) / max(range, 0.001);

                // Gold band that sweeps upward
                // Leading edge of the sweep (top of band) at _SweepProgress
                // Band extends downward by _SweepWidth
                float bandTop    = _SweepProgress;
                float bandBottom = _SweepProgress - _SweepWidth;

                // Soft-step the band edges
                float inBand = smoothstep(bandBottom - _SweepSoftness, bandBottom, normalizedHeight)
                             * (1.0 - smoothstep(bandTop, bandTop + _SweepSoftness, normalizedHeight));

                // Fresnel rim at edges (strongest when surface faces away from camera)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower) * _RimScale;

                // Gold mask: band × fresnel at edges + band fill in center
                float goldMask = inBand * saturate(fresnel * 0.7 + 0.3);
                // Below the band (already swept area): subtle gold sheen, no fresnel pop
                float belowBand = step(normalizedHeight, bandBottom) * 0.15;
                goldMask = saturate(goldMask + belowBand);

                // ── Lighting ──────────────────────────────────
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
            #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, 0, normalWS);
            #else
                inputData.bakedGI = SampleSH(normalWS);
            #endif
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.specular = 0;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1;
                surfaceData.alpha = albedo.a;
                surfaceData.emission = _EmissionColor.rgb + _GoldColor.rgb * _GoldEmission * goldMask;

                float4 color = UniversalFragmentPBR(inputData, surfaceData);

                return color;
            }
            ENDHLSL
        }

        // ── Shadow Caster ─────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
            #ifdef _ALPHATEST_ON
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - 0.5);
            #endif
                return 0;
            }
            ENDHLSL
        }

        // ── Depth Only ────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
            #ifdef _ALPHATEST_ON
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - 0.5);
            #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
