//UNITY_SHADER_NO_UPGRADE
// Capstone foliage toon trial v1.0 - Unity 6.3 / URP + Shader Graph 17.3.
// Called from the fragment stage of an Unlit graph with Keep Lighting Variants.
// The graph keeps the source RGB-mask color mix, vertex wind and alpha branches.
#ifndef CAPSTONE_FOLIAGE_TOON_LIGHTING_INCLUDED
#define CAPSTONE_FOLIAGE_TOON_LIGHTING_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
#endif

// Derived from the tested grass v1.2 fog/SH path. Decal/AO run before fog.
// In the companion graph, Default Decal Blending and Default SSAO must be OFF.
// The same operations are applied here so they do not tint/darken the fog later.
#if !defined(SHADERGRAPH_PREVIEW) && defined(_DBUFFER)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#endif

float3 CapstoneFoliageSafeNormalize(float3 v)
{
    return v * rsqrt(max(dot(v, v), 1e-8));
}

// Toon bands shape only the DIRECT light response. Neither ShadeBrightness
// nor the lower band may keep a surface lit when it faces away from the light
// or is fully shadowed. Ambient Fill is added separately by the caller.
float CapstoneFoliageDirectResponse(
    float NdotL, float Step, float Feather, float ShadeBrightness,
    float Shadow, float ShadowStrength)
{
    float ndotl = saturate(NdotL);
    float feather = max(Feather, 0.005);
    float halfFeather = feather * 0.5;
    float band = smoothstep(saturate(Step) - halfFeather,
                            saturate(Step) + halfFeather, ndotl);
    float brightness = lerp(saturate(ShadeBrightness), 1.0, band);

    // Fade the lower toon band to zero at grazing/back-facing light angles.
    // Using Feather keeps this transition soft without adding a new property.
    float facing = smoothstep(0.0, feather, ndotl);
    float visibility = lerp(1.0, saturate(Shadow), saturate(ShadowStrength));
    return brightness * facing * visibility;
}

// The same band response is used for punctual and extra directional lights.
// Range/spot attenuation, cookies and realtime shadows are supplied by URP.
#ifndef SHADERGRAPH_PREVIEW
float3 CapstoneFoliageEvaluateAdditionalLight(
    Light light, float3 normalWS, float Step, float Feather,
    float ShadeBrightness, float ShadowStrength)
{
    #ifdef _LIGHT_LAYERS
        if (!IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
            return float3(0.0, 0.0, 0.0);
    #endif

    float response = CapstoneFoliageDirectResponse(
        dot(normalWS, light.direction), Step, Feather, ShadeBrightness,
        light.shadowAttenuation, ShadowStrength);
    return max((float3)light.color, 0.0)
         * max(light.distanceAttenuation, 0.0) * response;
}

float3 CapstoneFoliageAdditionalLighting(
    float3 positionWS, float3 normalWS, float Step, float Feather,
    float ShadeBrightness, float ShadowStrength)
{
    float3 lighting = float3(0.0, 0.0, 0.0);
    #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
        // LIGHT_LOOP_BEGIN uses this exact local name in Forward+.
        InputData inputData = (InputData)0;
        inputData.positionWS = positionWS;
        inputData.normalWS = normalWS;
        inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);

        #if USE_CLUSTER_LIGHT_LOOP
            float3 ndc = ComputeNormalizedDeviceCoordinatesWithZ(
                positionWS, GetWorldToHClipMatrix());
            float4 pixelPositionCS = float4(
                ndc.xy * GetScaledScreenParams().xy, ndc.z, 1.0);
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(pixelPositionCS);

            // Forward+ stores additional directionals before punctual lights.
            UNITY_LOOP for (uint lightIndex = 0u;
                lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                ++lightIndex)
            {
                CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                Light light = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
                lighting += CapstoneFoliageEvaluateAdditionalLight(
                    light, normalWS, Step, Feather, ShadeBrightness, ShadowStrength);
            }
        #endif

        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
            lighting += CapstoneFoliageEvaluateAdditionalLight(
                light, normalWS, Step, Feather, ShadeBrightness, ShadowStrength);
        LIGHT_LOOP_END
    #endif
    return lighting;
}
#endif

void CapstoneFoliageToon_float(
    float3 Albedo,
    float3 PositionWS,
    float3 NormalWS,
    float Step,
    float Feather,
    float ShadeBrightness,
    float NormalUpBlend,
    float ShadowStrength,
    float AmbientFill,
    float LightMultiplier,
    float AdditionalLights,
    out float3 Color)
{
    // Keep the effective lighting normal in the upper hemisphere, then bias it
    // towards world up to soften striping between foliage cards. The tree
    // default is only 0.15 so the canopy keeps directional volume.
    // This affects surface shading only, not the original mesh or its wind.
    float3 n = NormalWS;
    if (dot(n, n) < 1e-8)
        n = float3(0.0, 1.0, 0.0);
    n = CapstoneFoliageSafeNormalize(n);
    n.y = abs(n.y);
    n = CapstoneFoliageSafeNormalize(lerp(n, float3(0.0, 1.0, 0.0),
                                      saturate(NormalUpBlend)));

    float3 lightDirection;
    float3 lightColor;
    float3 environmentLighting;
    float shadow;
    float distanceAttenuation;

#ifdef SHADERGRAPH_PREVIEW
    // A stable preview light. The Game View uses the actual URP main light.
    lightDirection = normalize(float3(0.35, 0.8, 0.45));
    lightColor = float3(1.0, 1.0, 1.0);
    environmentLighting = float3(0.0, 0.0, 0.0);
    shadow = 1.0;
    distanceAttenuation = 1.0;
#else
    // URP 17.3's helper handles shadow atlas, cascades and screen-space shadows.
    // This overload also applies shadow-distance fade and main-light cookies.
    // Baked occlusion is intentionally neutral for this realtime terrain trial.
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord, PositionWS, half4(1, 1, 1, 1));
    lightDirection = mainLight.direction;
    lightColor = max((float3)mainLight.color, 0.0);
    shadow = saturate((float)mainLight.shadowAttenuation);
    distanceAttenuation = max((float)mainLight.distanceAttenuation, 0.0);

    // SH irradiance supplied by Unity for this draw: scene ambient probe when
    // local probes are not used. No custom globals, scripts or fixed scene tint.
    // SampleSH also follows the project's linear/gamma color-space convention.
    environmentLighting = max((float3)SampleSH(n), 0.0);

    // Respect URP Rendering Layers when the feature is enabled.
    #ifdef _LIGHT_LAYERS
        if (!IsMatchingLightLayer(mainLight.layerMask, GetMeshRenderingLayer()))
            distanceAttenuation = 0.0;
    #endif
#endif

    float directResponse = CapstoneFoliageDirectResponse(
        dot(n, lightDirection), Step, Feather, ShadeBrightness,
        shadow, ShadowStrength);

    // The grass v1.1 directional/shadow fix is retained. Only direct light receives
    // the main light's shadow attenuation. Ambient Fill remains an optional
    // neutral artistic lift, now added ON TOP OF the scene's SH lighting.
    float3 directLighting = lightColor * distanceAttenuation * directResponse;
    float3 additionalLighting = float3(0.0, 0.0, 0.0);
#ifndef SHADERGRAPH_PREVIEW
    if (AdditionalLights > 0.0)
        additionalLighting = CapstoneFoliageAdditionalLighting(
            PositionWS, n, Step, Feather, ShadeBrightness, ShadowStrength)
            * max(AdditionalLights, 0.0);
#endif
    float3 illumination = directLighting + additionalLighting
                        + environmentLighting + max(AmbientFill, 0.0);
    Color = max(Albedo, 0.0) * illumination * max(LightMultiplier, 0.0);

    // The URP Shader Graph Unlit forward pass compiles fog variants, but does
    // not apply fog automatically. Mix it LAST: fog color must not be multiplied
    // by the foliage tint, LightMultiplier, decal tint or ambient occlusion.
    // Depth/shadow passes do not need color effects. Deferred fog, if applicable,
    // belongs to URP's deferred renderer rather than the material GBuffer write.
#if !defined(SHADERGRAPH_PREVIEW) && defined(SHADERPASS_UNLIT) && (SHADERPASS == SHADERPASS_UNLIT)
    #if defined(_DBUFFER) || (defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT))
        // Reconstruct raster pixel coordinates using URP's platform-aware NDC
        // helper; then use the same screen-UV conversion as UnlitPass.hlsl.
        float3 positionNDC = ComputeNormalizedDeviceCoordinatesWithZ(
            PositionWS, GetWorldToHClipMatrix());
        float4 pixelPositionCS = float4(
            positionNDC.xy * GetScaledScreenParams().xy, positionNDC.z, 1.0);
    #endif

    #if defined(_DBUFFER)
        half3 decalColor = (half3)Color;
        ApplyDecalToBaseColor(pixelPositionCS, decalColor);
        Color = (float3)decalColor;
    #endif

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
        float2 screenUV = GetNormalizedScreenSpaceUV(pixelPositionCS);
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);
        Color *= (float)aoFactor.directAmbientOcclusion;
    #endif

    // Use URP's per-fragment view depth (including the near-plane offset), as
    // Lit does. This supports Lighting-window Linear, Exponential and Exp2 fog,
    // perspective/orthographic cameras and URP 17.3 dynamic fog keywords.
    float fogFactor = InitializeInputDataFog(float4(PositionWS, 1.0), 0.0);
    Color = MixFog(Color, fogFactor);
#endif
}

// Also expose a half variant if a user later changes node precision.
// Keep the graph's Position and Custom Function nodes in Float for world coords.
void CapstoneFoliageToon_half(
    half3 Albedo, half3 PositionWS, half3 NormalWS,
    half Step, half Feather, half ShadeBrightness, half NormalUpBlend,
    half ShadowStrength, half AmbientFill, half LightMultiplier,
    half AdditionalLights, out half3 Color)
{
    float3 result;
    CapstoneFoliageToon_float(Albedo, PositionWS, NormalWS,
        Step, Feather, ShadeBrightness, NormalUpBlend,
        ShadowStrength, AmbientFill, LightMultiplier, AdditionalLights, result);
    Color = (half3)result;
}

#endif
