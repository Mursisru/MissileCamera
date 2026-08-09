Shader "Hidden/MissileCamera/Scanlines"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.45
        _LineDensity ("Line Density", Float) = 720
        _Opacity ("Opacity", Float) = 0.22
        _Fisheye ("Fisheye", Float) = 0.12
        _Scroll ("Scroll", Float) = 0
        _Noise ("Noise", Float) = 0.12
        _Vignette ("Vignette", Float) = 0.35
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
            float4 _MainTex_TexelSize;
            float _Intensity;
            float _LineDensity;
            float _Opacity;
            float _Fisheye;
            float _Scroll;
            float _Noise;
            float _Vignette;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float2 FisheyeUV(float2 uv, float amount)
            {
                float2 c = uv - 0.5;
                float r2 = dot(c, c);
                c *= 1.0 + amount * r2;
                return c + 0.5;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float intensity = saturate(_Intensity);
                float2 uv = FisheyeUV(i.uv, _Fisheye * intensity);
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                // Mild FLIR micro-jitter (sensor sync)
                float j = (_Scroll * 0.15);
                float2 juv = uv + float2(
                    (Hash21(float2(j, uv.y * 40.0)) - 0.5) * _MainTex_TexelSize.x * 0.6,
                    (Hash21(float2(uv.x * 40.0, j)) - 0.5) * _MainTex_TexelSize.y * 0.35);

                fixed4 c = tex2D(_MainTex, juv);

                // --- Realistic FLIR CRT: interlaced field + soft phosphor darkening ---
                float dens = max(_LineDensity, 240.0);
                float scroll = _Scroll; // driven from C# every frame (Blit _Time can freeze)
                float field = dens * 0.5;

                // Primary interlaced bars (odd/even fields crawl slowly)
                float y1 = juv.y * dens + scroll * 28.0;
                float lineA = abs(frac(y1) - 0.5) * 2.0;
                float slotA = smoothstep(0.15, 0.55, lineA);

                // Secondary finer aperture grille
                float y2 = juv.y * dens * 2.0 - scroll * 11.0;
                float lineB = abs(frac(y2) - 0.5) * 2.0;
                float slotB = smoothstep(0.35, 0.7, lineB);

                float mask = lerp(1.0, lerp(0.72, 1.0, slotA) * lerp(0.9, 1.0, slotB), saturate(_Opacity) * intensity);

                // Soft vertical refresh beam (rare, slow)
                float beamY = frac(juv.y * 0.55 + scroll * 0.045);
                float beam = smoothstep(0.03, 0.0, abs(beamY - 0.5)) * 0.045 * intensity;

                // Analog noise / AGC hiss
                float n = Hash21(juv * float2(1440.0, 900.0) + scroll * 60.0);
                float grain = lerp(1.0, 0.9 + n * 0.2, saturate(_Noise) * intensity);

                // Tube vignette + slight center bloom
                float2 d = juv - 0.5;
                float r2 = dot(d, d);
                float vig = 1.0 - saturate(r2 * 1.55) * (_Vignette * intensity);
                float bloom = 1.0 + (1.0 - saturate(r2 * 3.5)) * 0.035 * intensity;

                // Slight contrast punch like cooled FLIR CRT
                float3 rgb = c.rgb;
                rgb = (rgb - 0.5) * (1.0 + 0.08 * intensity) + 0.5;
                rgb *= mask * vig * grain * bloom;
                rgb += beam;

                return fixed4(saturate(rgb), c.a);
            }
            ENDCG
        }
    }
}
