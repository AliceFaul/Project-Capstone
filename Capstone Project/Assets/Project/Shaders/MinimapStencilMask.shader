Shader "Mhieu/Minimap/StencilMask"
{
    // Writes stencil = 1 where a character is visible and nothing else (no color, no depth write).
    // Used as the "Override Material" of a URP Render Objects feature for the Player / Enemy / NPC layers.
    // The minimap overlay material then uses Stencil Comp = NotEqual, Stencil Ref = 1,
    // so the map (and its "hidden = brighter" effect) is not drawn over those characters.
    //
    // ZTest LEqual + Offset: only the parts of the character that are really visible get marked,
    // parts hidden behind a tent or a tree are not.
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "MinimapStencilMask"
            Tags { "LightMode"="UniversalForward" }

            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                // same transform chain as the Lit shader, so the depth matches the original draw
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
