Shader "Hidden/MissileCamera/MotionBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.25
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
                float k = saturate(_Intensity) * 0.012;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.4;
                c += tex2D(_MainTex, i.uv + float2(k, 0)) * 0.2;
                c += tex2D(_MainTex, i.uv - float2(k, 0)) * 0.2;
                c += tex2D(_MainTex, i.uv + float2(0, k)) * 0.1;
                c += tex2D(_MainTex, i.uv - float2(0, k)) * 0.1;
                return c;
            }
            ENDCG
        }
    }
}
