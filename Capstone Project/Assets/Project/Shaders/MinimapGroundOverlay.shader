Shader "Mhieu/Minimap/GroundOverlay"
{
    // Every pixel of the quad has a world position wp. Its offset from the player, d = wp.xz - playerXZ,
    // is measured in "screen meters". The REAL world point that pixel represents is
    //     p = playerXZ + d / mapScale
    // so the miniature slides under the player and the icon (drawn at d = 0) always stays at the feet.
    //
    // Occlusion: no depth test. The scene depth is compared with the quad depth; when something is clearly
    // in front of the quad (a tent, a tree...) the map is still drawn but brighter / more opaque.
    // Needs "Depth Texture" enabled in the URP Asset.
    //
    // Global inputs:
    //   _MinimapVisibleMask : R8, 255 = visible map cell (MinimapExploredMask)
    //   _MinimapVisibleInfo : (cellSizeMeters, width, height, 0)
    //   _MinimapWorldRect   : (originX, originZ, sizeX, sizeZ) (MinimapMaskBaker) - only the origin is used
    //   _MinimapPlayerPos   : x,y = player world X,Z; z,w = facing X,Z (MinimapOverlayRenderer)
    //   _MinimapMiniParams  : x = mapScale, y = visibleRadius (MinimapOverlayRenderer)
    //
    // Width/size properties below are in SCREEN meters (quad meters), not real meters.
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.85, 0.92, 0.8, 0.4)
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (screen m)", Range(0.005, 0.2)) = 0.03
        _OutlineStrength ("Outline Strength", Range(0, 5)) = 1

        _Pattern ("Pattern", 2D) = "white" {}
        _PatternScale ("Pattern Scale", Float) = 5.6
        _PatternSpeed ("Pattern Speed (XY)", Vector) = (-0.07, -0.33, 0, 0)

        [Header(Shape)]
        _EdgeFade ("Edge Fade (fraction of radius)", Range(0.01, 1)) = 0.35

        [Header(Occlusion)]
        _OccludedBrightness ("Occluded Brightness x", Range(1, 4)) = 1.5
        _OccludedAlphaMul ("Occluded Opacity x", Range(1, 6)) = 2
        _OcclusionMinGap ("Occlusion Min Gap (m)", Range(0, 5)) = 1.25
        _OcclusionSoftness ("Occlusion Softness (m)", Range(0.05, 3)) = 0.75

        [Header(Debug)]
        [Enum(Off,0,Quad Circle,1,Visible Cells,2,Occlusion,3)] _DebugView ("Debug View", Float) = 0

        [Header(Player Icon)]
        [HDR] _IconColor ("Icon Color", Color) = (1, 0.85, 0.25, 1)
        _IconRadius ("Icon Ring Radius (screen m)", Range(0.1, 2)) = 0.4
        _IconThickness ("Icon Ring Thickness (screen m)", Range(0.02, 0.5)) = 0.1

        [Header(Stencil optional)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Float) = 8
        _StencilRef ("Stencil Ref", Float) = 0
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
            Cull Off

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // ----- globals (outside the material CBUFFER) -----
            TEXTURE2D(_MinimapVisibleMask);
            float4 _MinimapVisibleInfo;  // cellSize, width, height, 0
            float4 _MinimapWorldRect;    // originX, originZ, sizeX, sizeZ
            float4 _MinimapPlayerPos;    // posX, posZ, dirX, dirZ
            float4 _MinimapMiniParams;   // mapScale, visibleRadius, 0, 0

            // ----- material -----
            TEXTURE2D(_Pattern); SAMPLER(sampler_Pattern);

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineStrength;
                float4 _Pattern_ST;
                float _PatternScale;
                float4 _PatternSpeed;
                float _EdgeFade;
                float _OccludedBrightness;
                float _OccludedAlphaMul;
                float _OcclusionMinGap;
                float _OcclusionSoftness;
                float _DebugView;
                half4 _IconColor;
                float _IconRadius;
                float _IconThickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // 1 if the map cell is part of the visible map, else 0
            float Visible(int2 c)
            {
                if (c.x < 0 || c.y < 0 || c.x >= (int)_MinimapVisibleInfo.y || c.y >= (int)_MinimapVisibleInfo.z)
                    return 0.0;
                return step(0.5, LOAD_TEXTURE2D_LOD(_MinimapVisibleMask, c, 0).r);
            }

            // signed distance to a convex CCW triangle (negative inside)
            float SdTriangle(float2 p, float2 a, float2 b, float2 c)
            {
                float2 e0 = b - a;
                float2 e1 = c - b;
                float2 e2 = a - c;
                float2 n0 = normalize(float2(e0.y, -e0.x));
                float2 n1 = normalize(float2(e1.y, -e1.x));
                float2 n2 = normalize(float2(e2.y, -e2.x));
                return max(dot(n0, p - a), max(dot(n1, p - b), dot(n2, p - c)));
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

            // 0 = the quad point is visible, 1 = something clearly in front of it hides it
            float Occlusion(float4 positionCS, float3 positionWS)
            {
                float2 screenUV = positionCS.xy / _ScaledScreenParams.xy;
                float sceneEye = EyeDepthFromRaw(SampleSceneDepth(screenUV));
                float fragEye = -TransformWorldToView(positionWS).z;
                return smoothstep(_OcclusionMinGap, _OcclusionMinGap + _OcclusionSoftness, fragEye - sceneEye);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.positionWS;

                // ---- miniature space ----
                float2 playerXZ = _MinimapPlayerPos.xy;
                float2 d = wp.xz - playerXZ;                       // offset from the player, screen meters
                float scale = max(_MinimapMiniParams.x, 1e-4);
                float radius = max(_MinimapMiniParams.y, 0.1);
                float2 p = playerXZ + d / scale;                   // the REAL world point this pixel shows

                // ---- anything needing derivatives is computed BEFORE any clip/discard ----
                float fwQ = max(length(fwidth(d)), 1e-5);          // screen meters per pixel
                float2 patUV = d * (_PatternScale * 0.1) + _PatternSpeed.xy * _Time.y;
                float2 patDx = ddx(patUV);
                float2 patDy = ddy(patUV);

                // ---- circular edge fade ----
                float distQ = length(d);
                float rangeFade = 1.0 - smoothstep(radius * (1.0 - _EdgeFade), radius, distQ);
                float inCircle = step(distQ, radius);

                // debug 1: the circle the quad draws in
                if (_DebugView > 0.5 && _DebugView < 1.5) return half4(1, 0, 1, 0.4 * inCircle);

                // debug 3: occlusion, green = visible, red = hidden
                if (_DebugView > 2.5)
                {
                    float o = Occlusion(IN.positionCS, wp);
                    return half4(o, 1.0 - o, 0.0, 0.5 * inCircle);
                }

                // ---- player icon at d = 0: ring + arrow pointing to the facing direction ----
                float2 dir = _MinimapPlayerPos.zw;
                dir = (dot(dir, dir) > 1e-4) ? normalize(dir) : float2(0.0, 1.0);
                float2 right = float2(dir.y, -dir.x);
                float2 q = float2(dot(d, right), dot(d, dir));     // local space, +y = forward

                float R = _IconRadius;
                float ringSd = abs(length(q) - R) - _IconThickness * 0.5;
                float arrowSd = SdTriangle(q,
                                           float2(0.0, R * 2.3),
                                           float2(-R * 0.7, R * 1.45),
                                           float2( R * 0.7, R * 1.45));
                float iconMask = saturate(0.5 - min(ringSd, arrowSd) / fwQ);

                // ---- map cell (real world) ----
                float cell = max(_MinimapVisibleInfo.x, 1e-3);     // real meters
                float cellQ = cell * scale;                        // the same cell, in screen meters
                float2 g = (p - _MinimapWorldRect.xy) / cell;
                float2 gf = floor(g);
                int2 c = (int2)gf;
                float2 local = g - gf;                             // 0..1 inside the cell

                float visHere = Visible(c);

                // debug 2: raw mask data, red = visible map cell, blue = not visible
                if (_DebugView > 1.5) return half4(visHere, 0, 1.0 - visHere, 0.45 * inCircle);

                // nothing to draw here (neither map nor icon)
                clip(max(visHere * rangeFade, iconMask) - 0.01);

                // ---- distance (screen meters) to the nearest NOT-visible neighbour cell: inner outline ----
                float l  = Visible(c + int2(-1,  0));
                float r  = Visible(c + int2( 1,  0));
                float dn = Visible(c + int2( 0, -1));
                float u  = Visible(c + int2( 0,  1));
                float ld = Visible(c + int2(-1, -1));
                float rd = Visible(c + int2( 1, -1));
                float lu = Visible(c + int2(-1,  1));
                float ru = Visible(c + int2( 1,  1));

                float edgeDist = 1e5;
                if (l  < 0.5) edgeDist = min(edgeDist, local.x * cellQ);
                if (r  < 0.5) edgeDist = min(edgeDist, (1.0 - local.x) * cellQ);
                if (dn < 0.5) edgeDist = min(edgeDist, local.y * cellQ);
                if (u  < 0.5) edgeDist = min(edgeDist, (1.0 - local.y) * cellQ);

                // concave corners (both neighbours visible, diagonal not)
                if (ld < 0.5 && l > 0.5 && dn > 0.5) edgeDist = min(edgeDist, length(float2(local.x, local.y) * cellQ));
                if (rd < 0.5 && r > 0.5 && dn > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, local.y) * cellQ));
                if (lu < 0.5 && l > 0.5 && u  > 0.5) edgeDist = min(edgeDist, length(float2(local.x, 1.0 - local.y) * cellQ));
                if (ru < 0.5 && r > 0.5 && u  > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, 1.0 - local.y) * cellQ));

                float outlineMask = saturate((_OutlineWidth - edgeDist) / fwQ + 0.5);

                // ---- map layer ----
                half3 pattern = SAMPLE_TEXTURE2D_GRAD(_Pattern, sampler_Pattern, patUV, patDx, patDy).rgb;
                half3 outlineRgb = _OutlineColor.rgb * _OutlineStrength * pattern;
                half3 mapRgb = lerp(_FillColor.rgb, outlineRgb, outlineMask);
                half mapA = lerp(_FillColor.a, _OutlineColor.a, outlineMask) * rangeFade * visHere;

                // hidden by decor -> brighter and more opaque
                float occ = Occlusion(IN.positionCS, wp);
                mapRgb *= lerp(1.0, _OccludedBrightness, occ);
                mapA = saturate(mapA * lerp(1.0, _OccludedAlphaMul, occ));

                // ---- icon layer on top (never affected by occlusion or edge fade) ----
                half3 baseRgb = (visHere > 0.5) ? mapRgb : _IconColor.rgb;
                half3 rgb = lerp(baseRgb, _IconColor.rgb, iconMask);
                half a = lerp(mapA, _IconColor.a, iconMask);

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
