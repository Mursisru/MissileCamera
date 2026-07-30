Shader "Hidden/MissileCamera/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float k = 0.004 + saturate(_Intensity) * 0.01;
                fixed4 blur = tex2D(_MainTex, i.uv) * 0.4;
                blur += tex2D(_MainTex, i.uv + float2(k, 0)) * 0.15;
                blur += tex2D(_MainTex, i.uv - float2(k, 0)) * 0.15;
                blur += tex2D(_MainTex, i.uv + float2(0, k)) * 0.15;
                blur += tex2D(_MainTex, i.uv - float2(0, k)) * 0.15;
                float lum = dot(blur.rgb, fixed3(0.2126, 0.7152, 0.0722));
                float bloom = saturate((lum - 0.55) * 2.0) * saturate(_Intensity);
                c.rgb += blur.rgb * bloom;
                return c;
            }
            ENDCG
        }
    }
}
