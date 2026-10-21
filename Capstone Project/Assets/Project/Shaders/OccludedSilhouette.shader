Shader "Mhieu/Characters/OccludedSilhouette"
{
    // Glow / silhouette on a character where scenery (tent, tree, rock...) hides it.
    // Used as the "Override Material" of a URP Render Objects feature (one feature per layer / colour).
    //
    //  - "hidden" is decided from the depth texture: something must be at least _OcclusionMinGap meters
    //    closer to the camera than the character, so low grass does not trigger it.
    //  - Pixels that already carry stencil = _SelfStencilRef are skipped. The MinimapStencilMask feature writes
    //    that value on every VISIBLE pixel of the characters, which also removes the self-overlap
    //    artefact (an arm in front of the torso lighting up the torso).
    //
    // Render Objects settings: Event = BeforeRenderingTransparents (after the depth copy),
    // Queue = Opaque, Layer Mask = the character layer, Override Material = a material of this shader.
    // Needs "Depth Texture" enabled in the URP Asset. Use HDR colours + Bloom for a real glow.
    Properties
    {
        [HDR] _Color ("Silhouette Color", Color) = (0.25, 0.85, 1, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0.7, 1, 1, 1)
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.3
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimStrength ("Rim Strength", Range(0, 4)) = 1.5

        [Header(Occlusion)]
        _OcclusionMinGap ("Occlusion Min Gap (m)", Range(0, 5)) = 1
        _OcclusionSoftness ("Occlusion Softness (m)", Range(0.05, 3)) = 0.5

        [Header(Stencil)]
        _SelfStencilRef ("Self Stencil Ref", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OccludedSilhouette"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            Stencil
            {
                Ref [_SelfStencilRef]
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                float _FillAlpha;
                float _RimPower;
                float _RimStrength;
                float _OcclusionMinGap;
                float _OcclusionSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // raw depth texture value -> eye depth (meters along the view axis), perspective or orthographic
            float EyeDepthFromRaw(float raw)
            {
                if (unity_OrthoParams.w > 0.5)
                {
                #if UNITY_REVERSED_Z
                    return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * raw;
                #else
                    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * raw;
                #endif
                }
                return LinearEyeDepth(raw, _ZBufferParams);
            }

            // 0 = nothing in front of this pixel, 1 = something clearly in front of it
            float Occlusion(float4 positionCS, float3 positionWS)
            {
                float2 screenUV = positionCS.xy / _ScaledScreenParams.xy;
                float sceneEye = EyeDepthFromRaw(SampleSceneDepth(screenUV));
                float fragEye = -TransformWorldToView(positionWS).z;
                return smoothstep(_OcclusionMinGap, _OcclusionMinGap + _OcclusionSoftness, fragEye - sceneEye);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float occ = Occlusion(IN.positionCS, IN.positionWS);
                clip(occ - 0.01);

                float3 n = normalize(IN.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength;

                half3 rgb = _Color.rgb + _RimColor.rgb * rim;
                half a = saturate(_FillAlpha + rim) * occ * _Color.a;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
