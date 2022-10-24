Shader "Unlit/ViewLinesLength"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OnColor ("Active Color", Color) = (0.83,0.46,0.25,1)
        _Threshold ("Threshold", Range(0,1)) = 0.07
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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
            float4 _MainTex_ST;

            fixed4 _OnColor;
            float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                float match = step(_Threshold, col.r);
                col = lerp(col, _OnColor, match);
                // apply fog
                return col;
            }
            ENDCG
        }
    }
}
