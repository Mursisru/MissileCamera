Shader "Hidden/MissileCamera/InfraredBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure EV", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _HighlightCompress ("Highlight Compress", Float) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "InfraredBlit"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Exposure;
            float _Contrast;
            float _HighlightCompress;

            static const half3 Rec709 = half3(0.2126h, 0.7152h, 0.0722h);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 linRgb = tex2D(_MainTex, i.uv).rgb;
                half lum = max(dot(linRgb, Rec709), 0.0h);

                lum *= exp2(_Exposure);

                half k = max(_HighlightCompress, 0.0h);
                lum = lum / (1.0h + lum * k);

                half c = max(_Contrast, 0.01h);
                lum = saturate((lum - 0.5h) * c + 0.5h);

                return fixed4(lum, lum, lum, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
