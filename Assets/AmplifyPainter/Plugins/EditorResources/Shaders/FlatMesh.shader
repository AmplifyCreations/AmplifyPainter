Shader "Hidden/FlatMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile _ _FlatUV
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
				float4 outPos :TEXCOORD1;
				float3 outNormal :TEXCOORD2;
				float4 teste : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.outPos = v.vertex;
				o.outNormal = v.normal;
				o.outNormal = UnityObjectToViewPos( v.vertex );
				o.teste = 0;

#if _FlatUV
				v.vertex.xy = v.uv;
				v.vertex.z = 0;
#endif
				o.vertex = UnityObjectToClipPos( v.vertex );
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return float4 (col.rgb * col.a, 1);
            }
            ENDCG
        }
    }
}
