Shader "UI/HeartFillHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Highlight)]
        _HighlightColor ("Highlight Color", Color) = (1,0.75,0.25,1)
        _HighlightWidth ("Core Width", Range(0.001, 0.1)) = 0.02
        _GlowWidth ("Glow Width", Range(0.001, 0.2)) = 0.06
        _CoreIntensity ("Intensity", Range(0, 5)) = 1.5

        [Header(Fill)]
        _FillAmount ("Fill Amount", Range(0,1)) = 1.0

        // Sprite UV rectangle inside texture:
        // x = minX
        // y = minY
        // z = maxX
        // w = maxY
        _SpriteUVMinMax ("Sprite UV Min Max", Vector) = (0,0,1,1)

        // Required by Unity UI masking.
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)

        // RectMask2D support.
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "HeartFill"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _TextureSampleAdd;

            float4 _ClipRect;

            float4 _HighlightColor;
            float _HighlightWidth;
            float _GlowWidth;
            float _CoreIntensity;

            float _FillAmount;
            float4 _SpriteUVMinMax;

            v2f vert(appdata_t v)
            {
                v2f OUT;

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);

                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.uv);

                fixed4 baseColor = tex * IN.color;

                // RectMask2D support.
                float clipAlpha =
                    UnityGet2DClipping(
                        IN.worldPosition.xy,
                        _ClipRect
                    );

                baseColor.a *= clipAlpha;

                // ----------------------------------------------------
                // Convert atlas UV -> local sprite UV (0 to 1).
                // ----------------------------------------------------

                float2 spriteSize =
                    max(
                        _SpriteUVMinMax.zw -
                        _SpriteUVMinMax.xy,
                        float2(0.00001, 0.00001)
                    );

                float2 localUV =
                    (IN.uv - _SpriteUVMinMax.xy) /
                    spriteSize;

                localUV = saturate(localUV);

                // ----------------------------------------------------
                // Heart fill direction:
                //
                // Bottom -> Top
                //
                // At fill = 1:
                // boundary = 1
                //
                // At fill = 0.5:
                // boundary = 0.5
                // ----------------------------------------------------

                float boundary = saturate(_FillAmount);

                // Distance from current pixel to the top edge
                // of the filled area.
                float distanceFromEdge =
                    boundary - localUV.y;

                // Only keep pixels INSIDE the filled region.
                float insideFill =
                    step(0.0, distanceFromEdge);

                // ----------------------------------------------------
                // Core highlight
                // ----------------------------------------------------

                float core =
                    1.0 -
                    smoothstep(
                        0.0,
                        _HighlightWidth,
                        abs(distanceFromEdge)
                    );

                // ----------------------------------------------------
                // Soft glow around the core.
                // ----------------------------------------------------

                float glow =
                    1.0 -
                    smoothstep(
                        0.0,
                        _GlowWidth,
                        abs(distanceFromEdge)
                    );

                core *= insideFill;
                glow *= insideFill;

                // Prevent glow from appearing in transparent parts
                // of the heart sprite.
                float spriteAlpha =
                    smoothstep(
                        0.01,
                        0.2,
                        tex.a
                    );

                core *= spriteAlpha;
                glow *= spriteAlpha;

                // ----------------------------------------------------
                // Final highlight
                // ----------------------------------------------------

                float3 highlight =
                    _HighlightColor.rgb *
                    (
                        core * _CoreIntensity +
                        glow * 0.35
                    ) *
                    _HighlightColor.a;

                baseColor.rgb =
                    saturate(
                        baseColor.rgb + highlight
                    );

                return baseColor;
            }

            ENDHLSL
        }
    }
}