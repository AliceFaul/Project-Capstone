// Shader don gian, khong can lighting, blend kieu Additive de trail phat sang dep
// Dung duoc ca Built-in Render Pipeline. Neu du an dung URP, xem huong dan URP ben duoi file README.
Shader "VFX/SwordTrailAdditive"
{
    Properties
    {
        _MainTex ("Texture (tuy chon, co the de trang)", 2D) = "white" {}
        _TintPower ("Do sang tong the (keo cao de an Bloom)", Range(0,8)) = 1.5
        _ScrollSpeed ("Toc do UV scroll doc trail (0 = dung yen)", Range(-5,5)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One // Additive blending -> trail sang ruc, phu hop kiem nang luong/phep thuat

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TintPower;
            float _ScrollSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.x += _Time.y * _ScrollSpeed; // Cuon texture doc theo chieu dai trail -> cam giac nang luong dang "chay"
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * i.color;
                col.rgb *= _TintPower;
                return col;
            }
            ENDCG
        }
    }
}
