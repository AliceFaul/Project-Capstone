// Simple lit URP shader for the death / respawn dissolve (PlayerDissolve swaps the model to a material using it).
// The dissolve is procedural (3D noise mixed with a feet-to-head gradient), so no noise texture is needed.
//   _DissolveAmount: 0 = fully visible, 1 = completely gone. Animated by PlayerDissolve.
//   _DissolveBaseY / _DissolveHeight: world-space feet height and model height, set by PlayerDissolve.
Shader "Mhieu/Dissolve Lit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _DissolveAmount ("Dissolve Amount (0 visible, 1 gone)", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 4
        _HeightInfluence ("Height Influence (0 noise only, 1 bottom to top)", Range(0, 1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0.001, 0.3)) = 0.08
        [HDR] _EdgeColor ("Edge Color", Color) = (3, 1, 0.2, 1)

        _DissolveBaseY ("Dissolve Base Y (set by script)", Float) = 0
        _DissolveHeight ("Dissolve Height (set by script)", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _DissolveAmount;
            half _NoiseScale;
            half _HeightInfluence;
            half _EdgeWidth;
            half4 _EdgeColor;
            float _DissolveBaseY;
            float _DissolveHeight;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        float Hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.zyx + 31.32);
            return frac((p.x + p.y) * p.z);
        }

        float ValueNoise(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);

            float n000 = Hash31(i);
            float n100 = Hash31(i + float3(1, 0, 0));
            float n010 = Hash31(i + float3(0, 1, 0));
            float n110 = Hash31(i + float3(1, 1, 0));
            float n001 = Hash31(i + float3(0, 0, 1));
            float n101 = Hash31(i + float3(1, 0, 1));
            float n011 = Hash31(i + float3(0, 1, 1));
            float n111 = Hash31(i + float3(1, 1, 1));

            float nx00 = lerp(n000, n100, f.x);
            float nx10 = lerp(n010, n110, f.x);
            float nx01 = lerp(n001, n101, f.x);
            float nx11 = lerp(n011, n111, f.x);

            return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
        }

        // 0..1. A pixel disappears when its value is below the current threshold, so low values (feet, low noise) go first.
        float DissolveField(float3 positionWS)
        {
            float noise = ValueNoise(positionWS * _NoiseScale);
            float height = saturate((positionWS.y - _DissolveBaseY) / max(_DissolveHeight, 0.001));
            return lerp(noise, height, _HeightInfluence);
        }

        // From -edge (nothing removed, no glow) to 1 + edge (everything removed).
        float DissolveThreshold()
        {
            return lerp(-_EdgeWidth, 1.0 + _EdgeWidth, _DissolveAmount);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float field = DissolveField(input.positionWS);
                float threshold = DissolveThreshold();
                clip(field - threshold);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half3 direct = mainLight.color * (saturate(dot(normalWS, mainLight.direction)) * mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                half3 ambient = SampleSH(normalWS);
                half3 color = albedo.rgb * (direct + ambient);

                // Glowing rim right at the dissolve front.
                half edge = 1.0 - saturate((field - threshold) / max(_EdgeWidth, 0.0001));
                color = lerp(color, _EdgeColor.rgb, edge);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Set by URP when rendering a shadow map (used for the shadow bias).
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            ShadowVaryings vertShadow(ShadowAttributes input)
            {
                ShadowVaryings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                output.positionWS = positionWS;
                return output;
            }

            half4 fragShadow(ShadowVaryings input) : SV_Target
            {
                clip(DissolveField(input.positionWS) - DissolveThreshold());
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            DepthVaryings vertDepth(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half fragDepth(DepthVaryings input) : SV_Target
            {
                clip(DissolveField(input.positionWS) - DissolveThreshold());
                return 0;
            }
            ENDHLSL
        }
    }
}
