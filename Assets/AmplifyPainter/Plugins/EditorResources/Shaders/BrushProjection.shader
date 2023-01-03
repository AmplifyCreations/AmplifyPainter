Shader "Hidden/AmplifyPainter/BrushProjection"
{
	Properties
	{
		//_BrushTex( "_BrushTex", 2D ) = "white" {}
		_BrushMask( "_BrushMask", 2D ) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Overlay"}

		Pass
		{
			Name "Project"

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _FlatUV
			#pragma multi_compile _ _UVmode
			#pragma multi_compile _ _DefaultBrush
			#pragma multi_compile _ _IsNormal
			#include "UnityCG.cginc"
			#include "PainterUtils.hlsl"

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
				float4 texcoord : TEXCOORD2;
			};

			v2f vert( appdata v )
			{
				v2f o;

				o.vertex = v.vertex;
				o.normal = v.normal;
				o.texcoord = v.texcoord;

				#if _FlatUV
					v.vertex.xy = v.texcoord.xy;
					v.vertex.z = 0;
					//o.vertex.z = 0;
				#endif

				o.clipPos = UnityObjectToClipPos( v.vertex );
				//o.clipPos.z = 0;
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				return BrushProject( i.vertex, i.normal, i.texcoord );
			}
			ENDCG
		}

		Pass
		{
			Name "Preview"
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _FlatUV
			#pragma multi_compile _ _UVmode
			#pragma multi_compile _ _DefaultBrush
			#pragma multi_compile _ _IsNormal
			#define AP_PREVIEW 1
			#include "UnityCG.cginc"
			#include "PainterUtils.hlsl"

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
				float4 texcoord : TEXCOORD2;
			};

			v2f vert( appdata v )
			{
				v2f o;

				o.vertex = v.vertex;
				o.normal = v.normal;
				o.texcoord = v.texcoord;

				#if _FlatUV
					v.vertex.xy = v.texcoord.xy;
					v.vertex.z = 0;
					//o.vertex.z = 0;
				#endif

				o.clipPos = UnityObjectToClipPos( v.vertex );
				//o.clipPos.z = 0;
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				return BrushProject( i.vertex, i.normal, i.texcoord );
			}
			ENDCG
		}
	}
}

