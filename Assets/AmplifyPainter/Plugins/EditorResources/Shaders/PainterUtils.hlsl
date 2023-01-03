#ifndef AP_UTILS_INCLUDED
#define AP_UTILS_INCLUDED

#include "UnityCG.cginc"

uniform float4x4 _BrushMatrix;
uniform float4 _BrushSettings;
uniform float4 _BrushProjSetting;
uniform sampler2D _BrushTex;
uniform float4 _BrushColor;
uniform float4 _MousePos;
uniform float _AP_BlendOP;
uniform sampler2D _BrushMask;
uniform sampler2D _LayerTex;
//uniform float4 _BrushTex_ST;
//uniform float4 _BrushTex_TexelSize;

float4 BrushProject( float4 vertex, float3 normal, float4 texcoord )
{
	float3 worldPos = mul( unity_ObjectToWorld, vertex ).xyz;
	float3 worldNormal = UnityObjectToWorldNormal( normal );

	float4 brushSpacePos = mul( _BrushMatrix, float4( worldPos, 1 ) );
	float3 brushSpaceNormal = normalize( mul( _BrushMatrix, worldNormal ) );

	// Brush Size
#if _UVmode
	float2 UVs = texcoord.xy - _MousePos.xy;
	UVs /= _BrushSettings.x;
#else
	float2 UVs = brushSpacePos.xy;
	UVs /= _BrushSettings.x;
#endif
	// Brush Rotation
	float cosUV = cos( _BrushSettings.z );
	float sinUV = sin( _BrushSettings.z );
	float2 rotator = mul( UVs, float2x2( cosUV, -sinUV, sinUV, cosUV ) );
	UVs = rotator;

	// Brush Wrap
#if _UVmode
	float depthMask = 1;// length( UVs * 2 - 1 );
#else
	float depthMask = brushSpacePos.z / _BrushSettings.x;
	depthMask = saturate( 1 - depthMask * depthMask );
	float wrapMask = lerp( 1, depthMask, _BrushProjSetting.y );
	UVs /= wrapMask;
#endif

	// Center and discard
	UVs = UVs * 0.5 + 0.5;
	if( UVs.x < 0 || UVs.x > 1 || UVs.y < 0 || UVs.y > 1 )
		discard;

	// Culling Mask
	float cullMask = -brushSpaceNormal.z;
	cullMask = cullMask * 1.5 + ( lerp( -0.5, 1.5, 0.5 ) );
	cullMask = smoothstep( 0.0, 1, cullMask );
	cullMask = lerp( 1, cullMask, _BrushProjSetting.x );
#if _UVmode
	cullMask = 1;
#endif


	#if _DefaultBrush
		float4 tex = float4( 1, 1, 1, saturate( 1 - saturate( smoothstep( 0, 1, length( rotator / depthMask ) ) ) ) );
	#else
		float4 tex = tex2D( _BrushTex, UVs );
		//#if _IsNormal
		//	tex.rgb = UnpackNormal(tex);
		//#endif
	#endif
	
	// Mask
	float mask = tex2D( _BrushMask, UVs ).r;

	// Layer value
	float4 layer = tex2D( _LayerTex, texcoord.xy );
	
	//change this later
	float colorMask = ( depthMask - 0.2 ) * 5;
	colorMask = saturate( colorMask );
	float3 brushColor = _BrushColor.rgb * tex.rgb;
	float brushAlpha = mask * colorMask * cullMask * _BrushSettings.y;
	
#ifdef AP_PREVIEW
	brushColor = lerp( brushColor, 0, _AP_BlendOP );
	return float4( brushColor, /*LinearToGammaSpaceExact*/(brushAlpha) );
#else
	float alpha = brushAlpha * ( 1 - layer.a ) + layer.a;
	float3 color = lerp( layer.rgb * layer.a, brushColor, brushAlpha ) / alpha;
	//float3 color = ( brushColor * brushAlpha * 1 + layer.rgb * layer.a * ( 1.0f - brushAlpha ) ) / alpha;
	
	float4 brush = float4( color, alpha );
	float4 eraser = float4( layer.rgb, saturate( layer.a - brushAlpha ));

	return lerp( brush, eraser, _AP_BlendOP );
#endif
}

#endif // AP_UTILS_INCLUDED
