Shader "Mhieu/Minimap/GroundOverlay"
{
    // Renders on a box volume (Cube mesh, front faces culled). For every pixel inside the box it
    // reconstructs the world position from the camera depth texture, so the result "sticks" to
    // whatever surface is below, like a decal. Needs "Depth Texture" enabled in the URP Asset.
    //
    // Global inputs (set by MinimapMaskBaker / MinimapExploredMask):
    //   _MinimapWalkableMask, _MinimapExploredMask, _MinimapWorldRect, _MinimapMaskTexel
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.55, 1, 0.1, 0.16)
        [HDR] _OutlineColor ("Outline Color", Color) = (0.55, 1, 0.1, 1)
        _OutlineWidth ("Outline Width (m)", Range(0.01, 0.5)) = 0.07
        _OutlineStrength ("Outline Strength", Range(0, 5)) = 2.6

        _CellSize ("Cell Size (m, 0 = mask resolution)", Range(0, 2)) = 0.4

        _Pattern ("Pattern", 2D) = "white" {}
        _PatternScale ("Pattern Scale", Float) = 5.6
        _PatternSpeed ("Pattern Speed (XY)", Vector) = (-0.07, -0.33, 0, 0)

        _MinUpDot ("Min Up Dot (floor only)", Range(0, 1)) = 0.6
        _EdgeFade ("Box Edge Fade", Range(0.01, 0.5)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "MinimapGroundOverlay"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // ----- globals (outside the material CBUFFER) -----
            TEXTURE2D(_MinimapWalkableMask); SAMPLER(sampler_MinimapWalkableMask);
            TEXTURE2D(_MinimapExploredMask); SAMPLER(sampler_MinimapExploredMask);
            float4 _MinimapWorldRect;   // originX, originZ, sizeX, sizeZ
            float4 _MinimapMaskTexel;   // 1/w, 1/h, w, h

            // ----- material -----
            TEXTURE2D(_Pattern); SAMPLER(sampler_Pattern);

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineStrength;
                float _CellSize;
                float4 _Pattern_ST;
                float _PatternScale;
                float4 _PatternSpeed;
                float _MinUpDot;
                float _EdgeFade;
            CBUFFER_END

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

            float2 WorldToMaskUV(float2 xz)
            {
                return (xz - _MinimapWorldRect.xy) / _MinimapWorldRect.zw;
            }

            float SampleWalkable(float2 xz)
            {
                float2 uv = WorldToMaskUV(xz);
                if (any(uv < 0.0) || any(uv > 1.0)) return 0.0;
                return SAMPLE_TEXTURE2D_LOD(_MinimapWalkableMask, sampler_MinimapWalkableMask, uv, 0).r;
            }

            float SampleExplored(float2 xz)
            {
                float2 uv = WorldToMaskUV(xz);
                if (any(uv < 0.0) || any(uv > 1.0)) return 0.0;
                return SAMPLE_TEXTURE2D_LOD(_MinimapExploredMask, sampler_MinimapExploredMask, uv, 0).r;
            }

            // 1 if the whole cell counts as "map", 0 otherwise.
            float CellInside(float2 cellIdx, float cell, float mpt)
            {
                float2 center = (cellIdx + 0.5) * cell;

                float walk;
                if (cell > mpt * 1.5)
                {
                    // chunky cells: average 5 taps so thin corridors survive
                    float q = cell * 0.25;
                    walk  = SampleWalkable(center);
                    walk += SampleWalkable(center + float2( q,  q));
                    walk += SampleWalkable(center + float2(-q,  q));
                    walk += SampleWalkable(center + float2( q, -q));
                    walk += SampleWalkable(center + float2(-q, -q));
                    walk *= 0.2;
                }
                else
                {
                    walk = SampleWalkable(center);
                }

                float explored = SampleExplored(center);
                return step(0.5, walk) * step(0.5, explored);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // ---- world position from depth ----
                float2 screenUV = IN.positionCS.xy / _ScaledScreenParams.xy;
            #if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(screenUV);
            #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
            #endif
                float3 wp = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float2 p = wp.xz;

                // ---- anything needing derivatives is computed BEFORE any clip/discard ----
                float3 c = cross(ddy(wp), ddx(wp));
                float3 n = c / max(length(c), 1e-8);
                float fw = max(length(fwidth(p)), 1e-4);

                float2 patUV = p * (_PatternScale * 0.1) + _PatternSpeed.xy * _Time.y;
                half3 pattern = SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, patUV).rgb;

                // ---- inside the projector box? ----
                float3 lp = mul(unity_WorldToObject, float4(wp, 1.0)).xyz;
                clip(0.5 - abs(lp));

                // floor-like surfaces only (no stretching on walls)
                clip(abs(n.y) - _MinUpDot);

                // ---- cell grid ----
                float mpt = _MinimapWorldRect.z * _MinimapMaskTexel.x;
                float cell = max(_CellSize, mpt);
                float2 cellIdx = floor(p / cell);
                float2 local = p / cell - cellIdx;           // 0..1 inside the cell

                clip(CellInside(cellIdx, cell, mpt) - 0.5);  // outside the map -> nothing

                // ---- distance to the nearest border cell (inner outline) ----
                float l  = CellInside(cellIdx + float2(-1,  0), cell, mpt);
                float r  = CellInside(cellIdx + float2( 1,  0), cell, mpt);
                float d  = CellInside(cellIdx + float2( 0, -1), cell, mpt);
                float u  = CellInside(cellIdx + float2( 0,  1), cell, mpt);
                float ld = CellInside(cellIdx + float2(-1, -1), cell, mpt);
                float rd = CellInside(cellIdx + float2( 1, -1), cell, mpt);
                float lu = CellInside(cellIdx + float2(-1,  1), cell, mpt);
                float ru = CellInside(cellIdx + float2( 1,  1), cell, mpt);

                float edgeDist = 1e5;
                if (l < 0.5) edgeDist = min(edgeDist, local.x * cell);
                if (r < 0.5) edgeDist = min(edgeDist, (1.0 - local.x) * cell);
                if (d < 0.5) edgeDist = min(edgeDist, local.y * cell);
                if (u < 0.5) edgeDist = min(edgeDist, (1.0 - local.y) * cell);

                // concave corners (both neighbours inside, diagonal outside)
                if (ld < 0.5 && l > 0.5 && d > 0.5) edgeDist = min(edgeDist, length(float2(local.x, local.y) * cell));
                if (rd < 0.5 && r > 0.5 && d > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, local.y) * cell));
                if (lu < 0.5 && l > 0.5 && u > 0.5) edgeDist = min(edgeDist, length(float2(local.x, 1.0 - local.y) * cell));
                if (ru < 0.5 && r > 0.5 && u > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, 1.0 - local.y) * cell));

                float outlineMask = saturate((_OutlineWidth - edgeDist) / fw + 0.5);

                // ---- fade near the horizontal borders of the box ----
                float2 e = abs(lp.xz) * 2.0;
                float boxFade = 1.0 - smoothstep(1.0 - _EdgeFade * 2.0, 1.0, max(e.x, e.y));

                half3 outlineRgb = _OutlineColor.rgb * _OutlineStrength * pattern;
                half3 rgb = lerp(_FillColor.rgb, outlineRgb, outlineMask);
                half a = lerp(_FillColor.a, _OutlineColor.a, outlineMask) * boxFade;

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
