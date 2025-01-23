Shader "Custom/AttackRangeStencil" 
{
    Properties 
    {
        _Color ("Overlay Color", Color) = (0,1,0,0.3)
        _MainTex ("Base Texture", 2D) = "white" {}
    }
    SubShader 
    {
        Tags 
        { 
            "Queue"="Transparent+200"
            "RenderType"="Transparent" 
        }
        
        Stencil 
        {
            Ref 1
            Comp Equal
        }
        
        Pass 
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                return lerp(col, _Color, _Color.a);
            }

            ENDCG
        }
    }
}