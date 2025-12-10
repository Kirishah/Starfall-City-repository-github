Shader "Custom/PhoneTopDiv"
{
  Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.2
        _Color ("Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
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
            float _Radius;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Only modify alpha in the top corners
                float alpha = 1.0;
                
                // Top-left corner
                if (i.uv.x < _Radius && i.uv.y > 1.0 - _Radius)
                {
                    float2 cornerCenter = float2(_Radius, 1.0 - _Radius);
                    float dist = distance(i.uv, cornerCenter);
                    if (dist > _Radius)
                        alpha = 0.0;
                }
                // Top-right corner  
                else if (i.uv.x > 1.0 - _Radius && i.uv.y > 1.0 - _Radius)
                {
                    float2 cornerCenter = float2(1.0 - _Radius, 1.0 - _Radius);
                    float dist = distance(i.uv, cornerCenter);
                    if (dist > _Radius)
                        alpha = 0.0;
                }
                
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
