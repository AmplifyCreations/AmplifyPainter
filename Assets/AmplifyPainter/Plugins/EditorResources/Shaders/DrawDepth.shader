Shader "Hidden/AmplifyPainter/DrawDepth" {
    Properties
    {
    }

    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
			uniform float4x4 _LightMatrix;

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
				float3 normal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex + v.normal * 0.005);
				o.normal = v.normal;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

				float3 wNormal = mul( unity_ObjectToWorld, i.normal ).xyz;
				float3 lightSpaceNorm = mul( _LightMatrix, wNormal );
				//float bias = 0.01*tan( acos( -lightSpaceNorm.z ) );//_MyBias

				float shadowCos = -lightSpaceNorm.z;
				float shadowSine = sqrt( 1 - shadowCos * shadowCos );
				float normalBias = 0.01 * shadowSine;
				

                // TODO: Understand why depth is reversed
                float depth = 1 - i.vertex.z;
				return float4( depth + normalBias, normalBias, 0, 0 );
                //return float4(depth, pow(depth, 2), 0.5, 0);
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
