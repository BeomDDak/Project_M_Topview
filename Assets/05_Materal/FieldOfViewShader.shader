Shader "Custom/FOVMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FOVMask ("FOV Mask", 2D) = "white" {}
        _DarknessStrength ("Darkness Strength", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _FOVMask;
            float _DarknessStrength;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_FOVMask, i.uv);
                
                // 마스크 값에 따라 어둡게 처리
                float darkness = 1 - (_DarknessStrength * (1 - mask.r));
                return col * darkness;
            }
            ENDCG
        }
    }
}