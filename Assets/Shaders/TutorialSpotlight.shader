Shader "Farm2Shelf/UI/TutorialSpotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dim Color", Color) = (0.015, 0.02, 0.04, 0.84)
        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _HoleHalf ("Hole Half", Vector) = (0.1, 0.06, 0, 0)
        _Softness ("Softness", Float) = 0.016
        _Corner ("Corner", Float) = 0.014
        _RimWidth ("Rim Width", Float) = 0.007
        _RimColor ("Rim Color", Color) = (1, 0.88, 0.48, 0.42)
        _HoleEnabled ("Hole Enabled", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "TutorialSpotlight"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float4 _HoleCenter;
            float4 _HoleHalf;
            float _Softness;
            float _Corner;
            float _RimWidth;
            float4 _RimColor;
            float _HoleEnabled;
            sampler2D _MainTex;

            float SdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = _Color * i.color;
                if (_HoleEnabled < 0.5)
                {
                    return col;
                }

                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 p = i.uv - _HoleCenter.xy;
                p.x *= aspect;
                float2 halfSize = _HoleHalf.xy;
                halfSize.x *= aspect;
                float corner = max(_Corner * min(halfSize.x, halfSize.y) * 8.0, 0.004);
                float d = SdRoundBox(p, halfSize, corner);

                float soft = max(_Softness, 0.004);
                float holeMask = smoothstep(-soft, soft, d);
                col.a *= holeMask;

                float rim = smoothstep(_RimWidth * 2.2, 0.0, abs(d));
                rim *= holeMask;
                col.rgb = lerp(col.rgb, _RimColor.rgb, rim * _RimColor.a);
                col.a = max(col.a, rim * _RimColor.a * 0.35);
                return col;
            }
            ENDCG
        }
    }
}
