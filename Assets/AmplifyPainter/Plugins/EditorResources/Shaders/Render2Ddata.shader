Shader "Hidden/Render2Ddata"
{
    Properties
    {
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
				float4 data0 : TEXCOORD0;
				float4 data1 : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
				o.data0 = v.vertex;
				o.data1 = float4( v.normal, 1 );

				v.vertex.xy = v.uv;
				v.vertex.z = 0;
				o.vertex = UnityObjectToClipPos( v.vertex );
                return o;
            }

            void frag (v2f i, 
				out half4 outPosition : SV_Target0,
				out half4 outNormal : SV_Target1
			)
            {
				outPosition = i.data0;
				outNormal = i.data1;
            }
            ENDCG
        }
    }
}
