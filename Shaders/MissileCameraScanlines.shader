Shader "Hidden/MissileCamera/Scanlines"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.28
        _LineDensity ("Line Density", Float) = 540
        _Opacity ("Opacity", Float) = 0.28
        _Fisheye ("Fisheye", Float) = 0.085
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
            float _LineDensity;
            float _Opacity;
            float _Fisheye;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float2 FisheyeUV(float2 uv, float amount)
            {
                float2 c = uv - 0.5;
                float r2 = dot(c, c);
                // Mild barrel: push corners out slightly.
                c *= 1.0 + amount * r2;
                return c + 0.5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float intensity = saturate(_Intensity);
                float2 uv = FisheyeUV(i.uv, _Fisheye * intensity);
                // Soft edge clamp after warp
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                fixed4 c = tex2D(_MainTex, uv);

                float density = max(_LineDensity, 160.0);
                // Rolling scan: slow vertical crawl + secondary fine field.
                float roll = _Time.y * 22.0;
                float line = frac(uv.y * density + roll * 0.015);
                float band = smoothstep(0.0, 0.35, line) * smoothstep(1.0, 0.65, line);
                float fine = sin((uv.y * density * 2.0 + roll) * 6.28318) * 0.5 + 0.5;
                float scan = lerp(1.0, 0.78 + band * 0.18 + fine * 0.04, saturate(_Opacity) * intensity);

                // Occasional brighter horizontal sweep (CRT beam feel).
                float beam = abs(frac(uv.y * 0.35 + _Time.y * 0.08) - 0.5);
                float sweep = smoothstep(0.02, 0.0, beam) * 0.07 * intensity;

                float2 d = uv - 0.5;
                float vig = 1.0 - saturate(dot(d, d) * 1.35) * (0.22 * intensity);

                float n = frac(sin(dot(uv * float2(12.9898, 78.233), float2(1.0, 1.0)) + _Time.y * 3.1) * 43758.5453);
                float grain = lerp(1.0, 0.94 + n * 0.12, 0.22 * intensity);

                c.rgb *= scan * vig * grain;
                c.rgb += sweep;
                return c;
            }
            ENDCG
        }
    }
}
