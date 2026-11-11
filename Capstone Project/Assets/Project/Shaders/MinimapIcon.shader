Shader "Mhieu/Minimap/Icon"
{
    // Minimap icon: unlit, alpha blended, ALWAYS on top (no depth test), drawn after the map overlay.
    // _MainTex / _UVRect / _Color are set per icon by MinimapIconLayer through a MaterialPropertyBlock.
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _UVRect ("UV Rect (xy offset, zw size)", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "MinimapIcon"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _UVRect;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = _UVRect.xy + IN.uv * _UVRect.zw;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * _Color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
