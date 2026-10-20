Shader "Hidden/Mhieu/OutlineComposite"
{
    // Fullscreen pass. Reads the mask (_BlitTexture) and draws the outline as a ring of
    // _OutlineWidthPx pixels OUTSIDE the silhouette. Pixels inside the silhouette are left alone.
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _OutlineWidthPx;

        #define DIRS 12
        #define MAX_RINGS 8
        ENDHLSL

        // Pass 0: outline
        Pass
        {
            Name "OutlineComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 centre = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                // inside the silhouette: no outline here
                if (centre.a > 0.001) discard;

                float2 texel = rcp(_ScaledScreenParams.xy);
                int rings = clamp((int)ceil(_OutlineWidthPx), 1, MAX_RINGS);
                float stepPx = _OutlineWidthPx / rings;

                half4 best = half4(0, 0, 0, 0);

                [loop]
                for (int r = 1; r <= rings; r++)
                {
                    float radius = stepPx * r;

                    [unroll]
                    for (int d = 0; d < DIRS; d++)
                    {
                        // rotate every ring a bit so the samples do not line up in spokes
                        float ang = 6.2831853 * d / DIRS + r * 0.2618;
                        float2 offs = float2(cos(ang), sin(ang)) * radius * texel;

                        half4 s = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + offs);
                        if (s.a > best.a) best = s;
                    }
                }

                if (best.a <= 0.001) discard;

                return half4(best.rgb, best.a);
            }
            ENDHLSL
        }

        // Pass 1: debug, shows the raw mask
        Pass
        {
            Name "DebugMask"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDebug

            half4 FragDebug(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 m = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                if (m.a <= 0.001) discard;

                return half4(m.rgb, 0.6h);
            }
            ENDHLSL
        }
    }
}
