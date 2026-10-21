Shader "Hidden/Mhieu/OutlineMask"
{
    // Draws the outlined renderers into the mask texture (RGBA8):
    //   rgb = outline colour, a = coverage * strength
    // Blend is MAX so overlapping objects and the two passes never need a particular order.
    //
    // Pass 0 "Hidden"  : whole silhouette, no depth test, alpha = intensity * hiddenOpacity
    //                    (hiddenOpacity 0 = Visible Only, 1 = X Ray, in between = Ghost)
    // Pass 1 "Visible" : only the pixels that pass a depth test against the scene, alpha = intensity
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One
        BlendOp Max

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        // set per object through command buffer globals
        float4 _OutlineColorIntensity;   // rgb = colour, a = intensity (fade)
        float _OutlineHiddenOpacity;     // 0..1
        float _OutlineDepthBias;

        struct Attributes
        {
            float4 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Name "Hidden"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragHidden

            half4 FragHidden(Varyings IN) : SV_Target
            {
                return half4(_OutlineColorIntensity.rgb, _OutlineColorIntensity.a * _OutlineHiddenOpacity);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Visible"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragVisible

            half4 FragVisible(Varyings IN) : SV_Target
            {
                float2 uv = IN.positionCS.xy / _ScaledScreenParams.xy;
                float sceneDepth = SampleSceneDepth(uv);

                // keep the pixel only if it is on (or in front of) what the camera sees there
            #if UNITY_REVERSED_Z
                clip(IN.positionCS.z - sceneDepth + _OutlineDepthBias);
            #else
                clip(sceneDepth - IN.positionCS.z + _OutlineDepthBias);
            #endif

                return half4(_OutlineColorIntensity.rgb, _OutlineColorIntensity.a);
            }
            ENDHLSL
        }
    }
}
