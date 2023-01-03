Shader "Hidden/AmplifyPainter/CreateChannels"
{
	Properties
	{
		[HideInInspector]_ATex ("ATex", 2D) = "white" {}
		[HideInInspector]_BTex ("BTex", 2D) = "white" {}
		[HideInInspector]_CTex ("CTex", 2D) = "white" {}
		[HideInInspector]_DTex ("DTex", 2D) = "white" {}

		[HideInInspector]_AMask ("AMask", Vector) = (1,1,1,1)
		[HideInInspector]_BMask ("BMask", Vector) = (1,1,1,1)
		[HideInInspector]_CMask ("CMask", Vector) = (1,1,1,1)
		[HideInInspector]_DMask ("DMask", Vector) = (1,1,1,1)
	}

		SubShader
		{
			Cull Off ZWrite Off ZTest Always

			///////////////////////////////////////////////////////////////
			//2
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

				v2f vert (appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos (v.vertex);
					o.uv = v.uv;
					return o;
				}

				uniform sampler2D _ATex;
				uniform float4 _ATex_ST;
				uniform float4 _AMask;

				uniform sampler2D _BTex;
				uniform float4 _BTex_ST;
				uniform float4 _BMask;

				float4 frag (v2f i) : SV_Target
				{
					float4 AValue = tex2D (_ATex, i.uv);
					AValue.a = AValue.r;

					float4 BValue = tex2D (_BTex, i.uv);
					BValue.a = BValue.r;

					return AValue * _AMask + BValue * _BMask;
				}
				ENDCG
			}
			///////////////////////////////////////////////////////////////
			//3
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

				v2f vert (appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos (v.vertex);
					o.uv = v.uv;
					return o;
				}

				uniform sampler2D _ATex;
				uniform float4 _ATex_ST;
				uniform float4 _AMask;

				uniform sampler2D _BTex;
				uniform float4 _BTex_ST;
				uniform float4 _BMask;

				uniform sampler2D _CTex;
				uniform float4 _CTex_ST;
				uniform float4 _CMask;


				float4 frag (v2f i) : SV_Target
				{
					float4 AValue = tex2D (_ATex, i.uv);
					AValue.a = AValue.r;

					float4 BValue = tex2D (_BTex, i.uv);
					BValue.a = BValue.r;

					float4 CValue = tex2D (_CTex, i.uv);
					CValue.a = CValue.r;

					return AValue * _AMask + BValue * _BMask + CValue*_CMask;
				}
				ENDCG
			}
			
			///////////////////////////////////////////////////////////////
			//4
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

				v2f vert (appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos (v.vertex);
					o.uv = v.uv;
					return o;
				}

				uniform sampler2D _ATex;
				uniform float4 _ATex_ST;
				uniform float4 _AMask;

				uniform sampler2D _BTex;
				uniform float4 _BTex_ST;
				uniform float4 _BMask;

				uniform sampler2D _CTex;
				uniform float4 _CTex_ST;
				uniform float4 _CMask;

				uniform sampler2D _DTex;
				uniform float4 _DTex_ST;
				uniform float4 _DMask;

				float4 frag (v2f i) : SV_Target
				{
					float4 AValue = tex2D (_ATex, i.uv);
					AValue.a = AValue.r;
					
					float4 BValue = tex2D (_BTex, i.uv);
					BValue.a = BValue.r;

					float4 CValue = tex2D (_CTex, i.uv);
					CValue.a = CValue.r;

					float4 DValue = tex2D (_DTex, i.uv);
					DValue.a = DValue.r;

					return AValue * _AMask + BValue * _BMask + CValue * _CMask + DValue * _DMask;
				}
				ENDCG
			}
		}
}
