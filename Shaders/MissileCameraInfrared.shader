Shader "MissileCamera/Infrared"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Float) = 0
        _Contrast ("Contrast", Float) = 1.35
        _BlackPoint ("Black Point", Float) = 0.02
        _WhitePoint ("White Point", Float) = 0.85
        _RedWeight ("Red Weight", Float) = 0.55
        _GreenWeight ("Green Weight", Float) = 0.30
        _BlueWeight ("Blue Weight", Float) = 0.15
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"=""
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Infrared"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Exposure;
            float _Contrast;
            float _BlackPoint;
            float _WhitePoint;
            float _RedWeight;
            float _GreenWeight;
            float _BlueWeight;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.texcoord);
                half3 rgb = tex.rgb * i.color.rgb;

                half lum = dot(rgb, half3(_RedWeight, _GreenWeight, _BlueWeight));
                half range = max(_WhitePoint - _BlackPoint, 1e-4h);
                half contrastLum = saturate((lum - _BlackPoint) / range);
                contrastLum = saturate((contrastLum - 0.5h) * _Contrast + 0.5h);
                // Night boost — same spirit as TargetCam IR postExposure.
                contrastLum = saturate(contrastLum * exp2(_Exposure));

                return fixed4(contrastLum, contrastLum, contrastLum, tex.a * i.color.a);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
