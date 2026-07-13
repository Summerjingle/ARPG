#ifndef LIT_PETRIFICATION_INPUT_INCLUDED
#define LIT_PETRIFICATION_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

// ── CBUFFER (original Lit uniforms + petrification) ────────────
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _DetailAlbedoMap_ST;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Smoothness;
    half _Metallic;
    half _BumpScale;
    half _Parallax;
    half _OcclusionStrength;
    half _ClearCoatMask;
    half _ClearCoatSmoothness;
    half _DetailAlbedoMapScale;
    half _DetailNormalMapScale;
    half _Surface;

    // ── Petrification ──────────────────────────────────────
    half _PetrificationProgress;
    half4 _StoneColor;
    half _StoneBumpScale;
    half _StoneOcclusionStrength;
    half _StoneSmoothness;
    half _StoneMetallic;
    half4 _StoneSpecColor;

    // ── Radial petrification (center → edge) ──────────────
    half4 _PetrificationCenter;       // world-space center (.xyz used, .w padding)
    half  _PetrificationRadius;       // max radius (world units)
    half  _PetrificationEdgeSoftness; // transition softness at the boundary
CBUFFER_END

// ── DOTS instancing (original Lit, stone props not instanced) ──
#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Parallax)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatMask)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatSmoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_SpecColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_BumpScale;
static float  unity_DOTS_Sampled_Parallax;
static float  unity_DOTS_Sampled_OcclusionStrength;
static float  unity_DOTS_Sampled_ClearCoatMask;
static float  unity_DOTS_Sampled_ClearCoatSmoothness;
static float  unity_DOTS_Sampled_DetailAlbedoMapScale;
static float  unity_DOTS_Sampled_DetailNormalMapScale;
static float  unity_DOTS_Sampled_Surface;

void SetupDOTSLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_SpecColor            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecColor);
    unity_DOTS_Sampled_EmissionColor        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
    unity_DOTS_Sampled_Cutoff               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff);
    unity_DOTS_Sampled_Smoothness           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_Metallic             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic);
    unity_DOTS_Sampled_BumpScale            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale);
    unity_DOTS_Sampled_Parallax             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Parallax);
    unity_DOTS_Sampled_OcclusionStrength    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    unity_DOTS_Sampled_ClearCoatMask        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ClearCoatMask);
    unity_DOTS_Sampled_ClearCoatSmoothness  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ClearCoatSmoothness);
    unity_DOTS_Sampled_DetailAlbedoMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoMapScale);
    unity_DOTS_Sampled_DetailNormalMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalMapScale);
    unity_DOTS_Sampled_Surface              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

#define _BaseColor              unity_DOTS_Sampled_BaseColor
#define _SpecColor              unity_DOTS_Sampled_SpecColor
#define _EmissionColor          unity_DOTS_Sampled_EmissionColor
#define _Cutoff                 unity_DOTS_Sampled_Cutoff
#define _Smoothness             unity_DOTS_Sampled_Smoothness
#define _Metallic               unity_DOTS_Sampled_Metallic
#define _BumpScale              unity_DOTS_Sampled_BumpScale
#define _Parallax               unity_DOTS_Sampled_Parallax
#define _OcclusionStrength      unity_DOTS_Sampled_OcclusionStrength
#define _ClearCoatMask          unity_DOTS_Sampled_ClearCoatMask
#define _ClearCoatSmoothness    unity_DOTS_Sampled_ClearCoatSmoothness
#define _DetailAlbedoMapScale   unity_DOTS_Sampled_DetailAlbedoMapScale
#define _DetailNormalMapScale   unity_DOTS_Sampled_DetailNormalMapScale
#define _Surface                unity_DOTS_Sampled_Surface

#endif


// ── Original texture declarations ──────────────────────────────
TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);

// ── Stone texture declarations ─────────────────────────────────
TEXTURE2D(_StoneBaseMap);       SAMPLER(sampler_StoneBaseMap);
TEXTURE2D(_StoneBumpMap);       SAMPLER(sampler_StoneBumpMap);
TEXTURE2D(_StoneOcclusionMap);  SAMPLER(sampler_StoneOcclusionMap);


// ── Original helpers (unchanged) ───────────────────────────────

#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = half4(SAMPLE_METALLICSPECULAR(uv));
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a *= _Smoothness;
    #endif
#else
    #if _SPECULAR_SETUP
        specGloss.rgb = _SpecColor.rgb;
    #else
        specGloss.rgb = _Metallic.rrr;
    #endif
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a = _Smoothness;
    #endif
#endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
    #ifdef _OCCLUSIONMAP
        half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
        return LerpWhiteTo(occ, _OcclusionStrength);
    #else
        return half(1.0);
    #endif
}

half2 SampleClearCoat(float2 uv)
{
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoatMaskSmoothness = half2(_ClearCoatMask, _ClearCoatSmoothness);
#if defined(_CLEARCOATMAP)
    clearCoatMaskSmoothness *= SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
#endif
    return clearCoatMaskSmoothness;
#else
    return half2(0.0, 1.0);
#endif
}

void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
#if defined(_PARALLAXMAP)
    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
#endif
}

half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
{
    return half(2.0) * detailAlbedo * scale - scale + half(1.0);
}

half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
{
#if defined(_DETAIL)
    half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;
#if defined(_DETAIL_SCALED)
    detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
#else
    detailAlbedo = half(2.0) * detailAlbedo;
#endif
    return albedo * LerpWhiteTo(detailAlbedo, detailMask);
#else
    return albedo;
#endif
}

half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
{
#if defined(_DETAIL)
#if BUMP_SCALE_NOT_SUPPORTED
    half3 detailNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv));
#else
    half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);
#endif
    detailNormalTS = normalize(detailNormalTS);
    return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
#else
    return normalTS;
#endif
}


// ── Compute per-pixel petrification progress ─────────────────
// "Growing stone sphere" model:
// A sphere of stone expands from the center as progress increases.
// At progress=0.5 the sphere covers 50% of maxRadius, etc.
// For de-petrification (1→0): the sphere shrinks back — edge recovers first.
half ComputePixelProgress(float3 positionWS)
{
    half globalProgress = saturate(_PetrificationProgress);

    // Radius ≤ 0 → uniform progress (no radial effect)
    if (_PetrificationRadius <= 0.0)
        return globalProgress;

    float dist  = distance(positionWS, _PetrificationCenter.xyz);
    float maxR  = max(_PetrificationRadius, 0.001);

    // The stone sphere radius grows linearly with progress
    float stoneRadius = globalProgress * maxR;

    // Soft transition band (fraction of max radius)
    float softness = max(_PetrificationEdgeSoftness * maxR, 0.001);

    // Inside stoneRadius → 1. Outside → 0. Soft border between.
    float pixelProgress = 1.0 - smoothstep(stoneRadius - softness, stoneRadius + softness, dist);

    return saturate(pixelProgress);
}

// ── 3-param version with world-space position (used by Forward pass) ─
inline void InitializeStandardLitSurfaceData(float2 uv, float3 positionWS, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    // ═══ Boss side (original Lit logic) ═══════════════════
    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);

    half3 bossAlbedo = albedoAlpha.rgb * _BaseColor.rgb;
    bossAlbedo = AlphaModulate(bossAlbedo, outSurfaceData.alpha);

#if _SPECULAR_SETUP
    half bossMetallic = half(1.0);
    half3 bossSpecular = specGloss.rgb;
#else
    half bossMetallic = specGloss.r;
    half3 bossSpecular = half3(0.0, 0.0, 0.0);
#endif
    half  bossSmoothness = specGloss.a;
    half3 bossNormalTS   = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    half  bossOcclusion  = SampleOcclusion(uv);
    half3 bossEmission   = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    // ═══ Stone side ═══════════════════════════════════════
    half4 stoneTex        = SAMPLE_TEXTURE2D(_StoneBaseMap, sampler_StoneBaseMap, uv);
    half3 stoneAlbedo     = stoneTex.rgb * _StoneColor.rgb;
    half  stoneSmoothness = stoneTex.a * _StoneSmoothness;
    half3 stoneNormalTS   = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_StoneBumpMap, sampler_StoneBumpMap, uv), _StoneBumpScale);
    half  stoneOcclusion  = SAMPLE_TEXTURE2D(_StoneOcclusionMap, sampler_StoneOcclusionMap, uv).r
                            * _StoneOcclusionStrength;

    // ═══ Per-pixel progress (radial if radius > 0) ════════
    half t = ComputePixelProgress(positionWS);

    // ═══ Lerp ═════════════════════════════════════════════
    outSurfaceData.albedo     = lerp(bossAlbedo,     stoneAlbedo,     t);
    outSurfaceData.metallic   = lerp(bossMetallic,   _StoneMetallic,  t);
    outSurfaceData.specular   = lerp(bossSpecular,   _StoneSpecColor.rgb, t);
    outSurfaceData.smoothness = lerp(bossSmoothness, stoneSmoothness, t);
    outSurfaceData.normalTS   = normalize(lerp(bossNormalTS, stoneNormalTS, t));
    outSurfaceData.occlusion  = lerp(bossOcclusion,  stoneOcclusion,  t);
    outSurfaceData.emission   = lerp(bossEmission,   half3(0, 0, 0),  t);

    // Clear coat — fades with stone
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = lerp(clearCoat.r, half(0.0), t);
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
#endif

    // Detail maps — fade with progress
#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo   = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
    outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask * (1.0 - t));
#endif
}

// ── 2-param overload: passes center as position → distance=0 → uniform progress ─
// Used by GBuffer, ShadowCaster, DepthOnly, DepthNormals, Meta passes.
inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    InitializeStandardLitSurfaceData(uv, _PetrificationCenter.xyz, outSurfaceData);
}

#endif
