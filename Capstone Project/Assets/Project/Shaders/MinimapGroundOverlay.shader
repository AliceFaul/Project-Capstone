Shader "Mhieu/Minimap/GroundOverlay"
{
    // Renders on a box volume (Cube mesh, front faces culled). For every pixel inside the box it
    // reconstructs the world position from the camera depth texture, so the result "sticks" to
    // whatever surface is below, like a decal. Needs "Depth Texture" enabled in the URP Asset.
    //
    // Global inputs:
    //   _MinimapVisibleMask : R8, 255 = visible map cell (MinimapExploredMask)
    //   _MinimapVisibleInfo : (cellSizeMeters, width, height, 0)
    //   _MinimapWorldRect   : (originX, originZ, sizeX, sizeZ) (MinimapMaskBaker) - only the origin is used
    //   _MinimapPlayerPos   : x,y = player world X,Z; z,w = facing X,Z (MinimapOverlayProjector)
    //
    // v3: - a single coarse mask: 1 load per pixel outside the map, 9 loads inside (was dozens)
    //     - outline around the WHOLE visible shape (NavMesh borders and the explored frontier),
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.9, 0.95, 0.8, 0.28)
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (m)", Range(0.01, 0.5)) = 0.04
        _OutlineStrength ("Outline Strength", Range(0, 5)) = 1

        _Pattern ("Pattern", 2D) = "white" {}
        _PatternScale ("Pattern Scale", Float) = 5.6
        _PatternSpeed ("Pattern Speed (XY)", Vector) = (-0.07, -0.33, 0, 0)

        _MinUpDot ("Min Up Dot (0 = paint any surface)", Range(0, 1)) = 0.3
        _EdgeFade ("Circular Edge Fade", Range(0.01, 0.5)) = 0.1

        [Header(Debug)]
        [Enum(Off,0,Box Area,1,Visible Cells,2)] _DebugView ("Debug View", Float) = 0

        [Header(Player Icon)]
        [HDR] _IconColor ("Icon Color", Color) = (1, 0.85, 0.25, 1)
        _IconRadius ("Icon Ring Radius (m)", Range(0.1, 2)) = 0.45
        _IconThickness ("Icon Ring Thickness (m)", Range(0.02, 0.5)) = 0.1
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
            TEXTURE2D(_MinimapVisibleMask);
            float4 _MinimapVisibleInfo;  // cellSize, width, height, 0
            float4 _MinimapWorldRect;    // originX, originZ, sizeX, sizeZ
            float4 _MinimapPlayerPos;    // posX, posZ, dirX, dirZ

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
                float _MinUpDot;
                float _EdgeFade;
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
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
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
                float3 cr = cross(ddy(wp), ddx(wp));
                float3 n = cr / max(length(cr), 1e-8);
                float fw = max(length(fwidth(p)), 1e-4);

                float2 patUV = p * (_PatternScale * 0.1) + _PatternSpeed.xy * _Time.y;
                half3 pattern = SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, patUV).rgb;

                // ---- inside the projector box? ----
                float3 lp = mul(unity_WorldToObject, float4(wp, 1.0)).xyz;
                clip(0.5 - abs(lp));

                // floor-like surfaces only (set _MinUpDot to 0 to paint any surface)
                clip(abs(n.y) - _MinUpDot);

                // debug 1: paint everything the box + depth + floor test lets through (magenta)
                if (_DebugView > 0.5 && _DebugView < 1.5) return half4(1, 0, 1, 0.35);

                // ---- player icon: ring + arrow pointing to the facing direction ----
                float2 dir = _MinimapPlayerPos.zw;
                dir = (dot(dir, dir) > 1e-4) ? normalize(dir) : float2(0.0, 1.0);
                float2 right = float2(dir.y, -dir.x);
                float2 d = p - _MinimapPlayerPos.xy;
                float2 q = float2(dot(d, right), dot(d, dir));   // local space, +y = forward

                float R = _IconRadius;
                float ringSd = abs(length(q) - R) - _IconThickness * 0.5;
                float arrowSd = SdTriangle(q,
                                           float2(0.0, R * 2.3),
                                           float2(-R * 0.7, R * 1.45),
                                           float2( R * 0.7, R * 1.45));
                float iconMask = saturate(0.5 - min(ringSd, arrowSd) / fw);

                // ---- map cell ----
                float cell = max(_MinimapVisibleInfo.x, 1e-3);
                float2 g = (p - _MinimapWorldRect.xy) / cell;
                float2 gf = floor(g);
                int2 c = (int2)gf;
                float2 local = g - gf;                       // 0..1 inside the cell

                float visHere = Visible(c);

                // debug 2: raw mask data, red = visible map cell, blue = not visible
                if (_DebugView > 1.5) return half4(visHere, 0, 1.0 - visHere, 0.45);

                // nothing to draw here (neither map nor icon)
                clip(max(visHere, iconMask) - 0.01);

                // ---- distance to the nearest NOT-visible neighbour cell (inner outline) ----
                float l  = Visible(c + int2(-1,  0));
                float r  = Visible(c + int2( 1,  0));
                float dn = Visible(c + int2( 0, -1));
                float u  = Visible(c + int2( 0,  1));
                float ld = Visible(c + int2(-1, -1));
                float rd = Visible(c + int2( 1, -1));
                float lu = Visible(c + int2(-1,  1));
                float ru = Visible(c + int2( 1,  1));

                float edgeDist = 1e5;
                if (l  < 0.5) edgeDist = min(edgeDist, local.x * cell);
                if (r  < 0.5) edgeDist = min(edgeDist, (1.0 - local.x) * cell);
                if (dn < 0.5) edgeDist = min(edgeDist, local.y * cell);
                if (u  < 0.5) edgeDist = min(edgeDist, (1.0 - local.y) * cell);

                // concave corners (both neighbours visible, diagonal not)
                if (ld < 0.5 && l > 0.5 && dn > 0.5) edgeDist = min(edgeDist, length(float2(local.x, local.y) * cell));
                if (rd < 0.5 && r > 0.5 && dn > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, local.y) * cell));
                if (lu < 0.5 && l > 0.5 && u  > 0.5) edgeDist = min(edgeDist, length(float2(local.x, 1.0 - local.y) * cell));
                if (ru < 0.5 && r > 0.5 && u  > 0.5) edgeDist = min(edgeDist, length(float2(1.0 - local.x, 1.0 - local.y) * cell));

                float outlineMask = saturate((_OutlineWidth - edgeDist) / fw + 0.5);

                // ---- circular fade at the border of the box ----
                float rr = length(lp.xz) * 2.0;     // 0 at the center, 1 at the edge of the box
                float circleFade = 1.0 - smoothstep(1.0 - _EdgeFade * 2.0, 1.0, rr);

                // ---- map layer ----
                half3 outlineRgb = _OutlineColor.rgb * _OutlineStrength * pattern;
                half3 mapRgb = lerp(_FillColor.rgb, outlineRgb, outlineMask);
                half mapA = lerp(_FillColor.a, _OutlineColor.a, outlineMask) * circleFade * visHere;

                // ---- icon layer on top ----
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
