Shader "Hidden/AmplifyPainter/MaskDilate"
{
	Properties
	{
	}
	
	SubShader
	{
		//Tags { "RenderType"="Opaque" "Queue"="Overlay"}
		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 clipPos : SV_POSITION;
				float4 vertex : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 UV0 : TEXCOORD2;
			};

			v2f vert ( appdata v )
			{
				v2f o;

				o.vertex = v.vertex;
				o.normal = v.normal;
				o.UV0 = v.texcoord;
				v.vertex.xy = v.texcoord.xy;
				v.vertex.z = 0;

				o.clipPos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return 1;
			}

			ENDCG
		}
	}
}

