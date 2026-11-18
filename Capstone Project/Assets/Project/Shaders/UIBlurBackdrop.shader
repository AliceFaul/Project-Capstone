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
        float2 _FadeRange; // x = uv.x where the picture starts to fade out, y = uv.x where it is fully transparent

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

        // Keeps the blurred picture on the left and fades it to transparent toward the right (the live view shows through).
        fixed4 FragFade(v2f_img i) : SV_Target
        {
            fixed4 c = tex2D(_MainTex, i.uv);
            return fixed4(c.rgb, 1.0 - smoothstep(_FadeRange.x, _FadeRange.y, i.uv.x));
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

        // 2: horizontal alpha fade
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragFade
            ENDCG
        }
    }

    Fallback Off
}
