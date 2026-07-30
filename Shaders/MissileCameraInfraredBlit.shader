Shader "Hidden/MissileCamera/InfraredBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure EV", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _HighlightCompress ("Highlight Compress", Float) = 0.35
        _Mode ("Vision Mode", Float) = 2
        _EdgeStrength ("Edge Strength", Float) = 2.5
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
            Name "VisionBlit"
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
            float4 _MainTex_TexelSize;
            float _Exposure;
            float _Contrast;
            float _HighlightCompress;
            float _Mode;
            float _EdgeStrength;

            static const half3 Rec709 = half3(0.2126h, 0.7152h, 0.0722h);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half SampleLum(float2 uv)
            {
                half3 linRgb = tex2D(_MainTex, uv).rgb;
                half lum = max(dot(linRgb, Rec709), 0.0h);
                lum *= exp2(_Exposure);
                half k = max(_HighlightCompress, 0.0h);
                lum = lum / (1.0h + lum * k);
                half c = max(_Contrast, 0.01h);
                return saturate((lum - 0.5h) * c + 0.5h);
            }

            half SobelEdge(float2 uv)
            {
                float2 t = _MainTex_TexelSize.xy;
                half tl = SampleLum(uv + float2(-t.x,  t.y));
                half  t0 = SampleLum(uv + float2( 0,    t.y));
                half tr = SampleLum(uv + float2( t.x,  t.y));
                half l  = SampleLum(uv + float2(-t.x,  0));
                half r  = SampleLum(uv + float2( t.x,  0));
                half bl = SampleLum(uv + float2(-t.x, -t.y));
                half  b = SampleLum(uv + float2( 0,   -t.y));
                half br = SampleLum(uv + float2( t.x, -t.y));
                half gx = -tl - 2.0h * l - bl + tr + 2.0h * r + br;
                half gy = -tl - 2.0h * t0 - tr + bl + 2.0h * b + br;
                return saturate(sqrt(gx * gx + gy * gy) * _EdgeStrength);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int mode = (int)(_Mode + 0.5h);
                half lum = SampleLum(i.uv);

                // 2 WhiteHot, 3 BlackHot, 4 WhiteContour, 5 BlackContour
                if (mode == 3)
                {
                    lum = 1.0h - lum;
                    return fixed4(lum, lum, lum, 1);
                }

                if (mode == 4)
                {
                    half e = SobelEdge(i.uv);
                    return fixed4(e, e, e, 1);
                }

                if (mode == 5)
                {
                    half e = SobelEdge(i.uv);
                    half v = 1.0h - e;
                    return fixed4(v, v, v, 1);
                }

                return fixed4(lum, lum, lum, 1);
            }
            ENDCG
        }
    }

    FallBack Off
}
