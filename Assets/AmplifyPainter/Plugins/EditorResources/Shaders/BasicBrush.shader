Shader "Hidden/AmplifyPainter/BasicBrush" {
    Properties
    {
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay+1000"}

        Pass
        {
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

			uniform float4 _BrushSettings;

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
				float4 test : TEXCOORD2;
				float angle : TEXCOORD3;
            };

			sampler2D _DotTexture;

            v2f vert(appdata v)
            {
                v2f o;
				//v.vertex.xyz *= _BrushSettings.x * 2;
				//v.vertex.xyz /= 0.5;

				float angle = ( v.vertex.z / UNITY_TWO_PI );
				v.vertex.z = 0;
				v.vertex.xy *= _BrushSettings.x;
                o.vertex = UnityObjectToClipPos(v.vertex /*+ v.normal * 0.005*/);

				o.angle = angle;

				o.normal = v.normal;
                o.uv = v.uv;
				o.test = mul( unity_WorldToObject, v.vertex.xyz );
                return o;
            }

			float stepAA( float A, float B ) {
				float o = B - A;
				return saturate( o / fwidth( o ) );
			}

            fixed4 frag(v2f i) : SV_Target
            {
				float teste = i.angle;
				return float4( tex2D( _DotTexture, float2( teste, 0 ) ).rrr,1);

				////////////////////////////////
				float2 baseUV = ( i.uv.xy * 2 - 1 );
				float c = ( atan2( baseUV.x, baseUV.y ) / UNITY_TWO_PI ) + 0.5;
				c *= 50;
				c = frac( c );
				c = step( c, 0.5 );
				////////////////////////////////
				float sdf = saturate( length( i.uv * 2 - 1 ) );
				float2 UVs = float2( i.uv.x, sdf );

				float2 a = UVs - 0.5;
				float2 b = abs( a );


				float2 dd = fwidth( UVs );

				float2 r = b / dd;
				float lineT = saturate( r.y - 0.01 );

				float4 wht = float4( c.xxx, 1 );
				float4 blk = float4( c.xxx, 0 );
				return lerp( wht, blk, lineT );
				////////////////////////////////
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
