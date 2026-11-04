// Used only through Graphics.Blit by UIBlurBackdrop (screen snapshot -> small blurred texture).
Shader "Hidden/UI/BlurBackdrop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _FlipY;
        float2 _Direction; // (radius, 0) for the horizontal pass, (0, radius) for the vertical pass

        // 2x2 box downsample, optionally flipping the image vertically.
        fixed4 FragDownsample(v2f_img i) : SV_Target
        {
            float2 uv = i.uv;
            if (_FlipY > 0.5) uv.y = 1.0 - uv.y;

            float2 o = _MainTex_TexelSize.xy * 0.5;
            fixed4 c = tex2D(_MainTex, uv + float2(-o.x, -o.y));
            c += tex2D(_MainTex, uv + float2( o.x, -o.y));
            c += tex2D(_MainTex, uv + float2(-o.x,  o.y));
            c += tex2D(_MainTex, uv + float2( o.x,  o.y));
            return fixed4((c * 0.25).rgb, 1);
        }

        // Separable 9-tap gaussian using 5 bilinear taps.
        fixed4 FragBlur(v2f_img i) : SV_Target
        {
            float2 stepUv = _Direction * _MainTex_TexelSize.xy;
            fixed4 c = tex2D(_MainTex, i.uv) * 0.2270270270;
            c += (tex2D(_MainTex, i.uv + stepUv * 1.3846153846) + tex2D(_MainTex, i.uv - stepUv * 1.3846153846)) * 0.3162162162;
            c += (tex2D(_MainTex, i.uv + stepUv * 3.2307692308) + tex2D(_MainTex, i.uv - stepUv * 3.2307692308)) * 0.0702702703;
            return fixed4(c.rgb, 1);
        }
        ENDCG

        // 0: downsample (+ flip)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDownsample
            ENDCG
        }

        // 1: blur (direction from _Direction)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBlur
            ENDCG
        }
    }

    Fallback Off
}
