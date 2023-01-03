Shader "Hidden/FastDilate"
{
	Properties
	{
		_MainTex( "", 2D ) = "white" {}
		_MaskTex( "", 2D ) = "black" {}
	}

	SubShader
	{
		ZTest Always Cull Off ZWrite Off Fog{ Mode off }

		Pass // 0
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag( v2f_img i ) : SV_target
			{
				return FastDilate( i );
			}
			ENDHLSL
		}

		Pass // 1
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#define _AP_ALPHA_DILATE

			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag( v2f_img i ) : SV_target
			{
				return FastDilate( i );
			}
			ENDHLSL
		}

		Pass // 2
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			
			#define _AP_USING_MASK
			
			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag( v2f_img i ) : SV_target
			{
				return FastDilate( i );
			}
			ENDHLSL
		}

		Pass // 3
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#define _AP_ALPHA_DILATE
			#define _AP_USING_MASK

			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag( v2f_img i ) : SV_target
			{
				return FastDilate( i );
			}
			ENDHLSL
		}

		Pass // 4
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag( v2f_img i ) : SV_target
			{
				float4 ref_main = tex2D( _MainTex, i.uv.xy );
				float ref_mask = tex2D( _MaskTex, i.uv.xy ).a;
				return float4( ref_main.rgb, ref_mask );
			}
			ENDHLSL
		}

		Pass // 5
		{
			HLSLPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "DilateUtils.hlsl"

			float4 frag(v2f_img i) : SV_target
			{
				return tex2D(_MainTex, i.uv.xy).r;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
