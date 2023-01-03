#ifndef AP_DILATE_INCLUDED
#define AP_DILATE_INCLUDED

sampler2D _MainTex;
sampler2D _MaskTex;
float4 _MainTex_TexelSize;
float _DilateDistance = 1;

// requires v2f_img include
float4 FastDilate( v2f_img i )
{
	float4 ref_main = tex2Dlod( _MainTex, float4(i.uv.xy, 0, 0) );
	#if defined( _AP_USING_MASK )
		float ref_mask = tex2Dlod(_MaskTex, float4(i.uv.xy, 0, 0) ).r;
	#else
		float ref_mask = ref_main.a;
	#endif

	float4 result = 0;
	float2 offsets[ 4 ] =
	{
		float2(  0, -1 ),
		float2( -1,  0 ),
		float2( +1,  0 ),
		float2(  0, +1 ),
	};

	if( ref_mask == 0 )
	{
		float hits = 0;
		for( int tap = 0; tap < 4; tap++ )
		{
			float2 uv = i.uv.xy + offsets[ tap ] * _MainTex_TexelSize.xy * _DilateDistance;
			float4 main = tex2Dlod( _MainTex, float4( uv, 0, 0 ) );

			#if defined( _AP_USING_MASK )
				float mask = tex2Dlod(_MaskTex, float4(uv, 0, 0)).r;
			#else
				float mask = main.a;
			#endif

			if( mask != ref_mask )
			{
				result += main;
				hits++;
			}
		}

		if( hits > 0 )
		{
			#if defined( _AP_ALPHA_DILATE )
				result /= hits;
			#else
				result = float4( result.rgb / hits, ref_main.a );
			#endif
		}
		else
		{
			result = ref_main;
		}
	}
	else
	{
		result = ref_main;
	}

	return result;
}

#endif // AP_DILATE_INCLUDED
