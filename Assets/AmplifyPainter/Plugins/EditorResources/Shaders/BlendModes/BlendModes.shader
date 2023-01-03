Shader "Hidden/AmplifyPainter/BlendModes"
{
	Properties
	{
		_MainTex ("First Layer", 2D) = "white" {}
		_SecondLayer ("Second Layer", 2D) = "black" {}
		_Opacity ("Opacity" , Float) = 1
	}

	SubShader
	{
		CGINCLUDE
		//In[c] - bottomLayer
		//Layer[c] - topLayer

		//Top Layer
		sampler2D _MainTex;

		//Bottom Layer
		sampler2D _SecondLayer;

		//Layer Opacity
		float _Opacity;
		
		float _LastLayer;

		#include "UnityCG.cginc"
		#pragma vertex vert_img
		#pragma fragment frag
		#define FETCH_TEXTURES	float4 topLayer = tex2D (_MainTex, i.uv);\
								float4 bottomLayer = tex2D (_SecondLayer, i.uv);
		////#define APPLY_OPACITY(result) _Opacity * result + bottomLayer * (1 - topLayer.a*_Opacity)
		#define APPLY_OPACITY(result) lerp( bottomLayer, result,topLayer.a*_Opacity)
		
		inline float3 ColorBurn (float3 topLayer, float3 bottomLayer)
		{
			return 1.0 - ((1.0 - bottomLayer) / max( topLayer,0.00001 ));
		}

		inline float3 ColorDodge (float3 topLayer, float3 bottomLayer)
		{
			return bottomLayer / max(1.0 - topLayer, 0.00001);
		}

		inline float3 Darken (float3 topLayer, float3 bottomLayer)
		{
			return min (topLayer, bottomLayer);
		}

		inline float3 Divide (float3 topLayer, float3 bottomLayer)
		{
			return bottomLayer / max(topLayer, 0.00001);
		}

		inline float3 Difference (float3 topLayer, float3 bottomLayer)
		{
			return abs (topLayer - bottomLayer);
		}

		inline float3 Exclusion (float3 topLayer, float3 bottomLayer)
		{
			return 0.5 - 2.0 * (topLayer - 0.5) * (bottomLayer - 0.5);
		}

		inline float3 SoftLight (float3 topLayer, float3 bottomLayer)
		{
			return 2.0f*topLayer*bottomLayer + bottomLayer * bottomLayer*(1.0f - 2.0f*topLayer);
			//float3 multiply = bottomLayer.rgb * topLayer.rgb;
			//float3 screen = 1.0f - (1.0f - bottomLayer.rgb) * (1.0f - topLayer.rgb);
			//return (1.0f - bottomLayer.rgb) * multiply + bottomLayer.rgb * screen;
		}

		inline float3 HardLight (float3 topLayer, float3 bottomLayer)
		{
			return topLayer > 0.5 ? (1.0 - (1.0 - 2.0 * (topLayer - 0.5)) * (1.0 - bottomLayer)) : (2.0 * topLayer * bottomLayer);
		}

		inline float3 HardMix (float3 topLayer, float3 bottomLayer)
		{
			//return round (0.5 * (topLayer + bottomLayer));
			return floor(topLayer + bottomLayer);
		}

		inline float3 Lighten (float3 topLayer, float3 bottomLayer)
		{
			return max (topLayer, bottomLayer);
		}

		inline float3 LinearBurn (float3 topLayer, float3 bottomLayer)
		{
			return topLayer + bottomLayer - 1.0;
		}

		inline float3 LinearDodge (float3 topLayer, float3 bottomLayer)
		{
			return topLayer + bottomLayer;
		}

		inline float3 LinearLight (float3 topLayer, float3 bottomLayer)
		{
			return topLayer > 0.5 ? (bottomLayer + 2.0 * topLayer - 1.0) : (bottomLayer + 2.0 * (topLayer - 0.5));
		}

		inline float3 Overlay (float3 topLayer, float3 bottomLayer)
		{
			return bottomLayer > 0.5 ? (1.0 - 2.0 * (1.0 - bottomLayer)  * (1.0 - topLayer)) : (2.0 * bottomLayer * topLayer);
		}

		inline float3 PinLight (float3 topLayer, float3 bottomLayer)
		{
			return topLayer > 0.5 ? max (bottomLayer, 2.0 * (topLayer - 0.5)) : min (bottomLayer, 2.0 * topLayer);
		}

		inline float3 Subtract (float3 topLayer, float3 bottomLayer)
		{
			return bottomLayer - topLayer;
		}

		inline float3 Screen (float3 topLayer, float3 bottomLayer)
		{
			return 1.0 - (1.0 - topLayer) * (1.0 - bottomLayer);
		}

		inline float3 VividLight (float3 topLayer, float3 bottomLayer)
		{
			return topLayer > 0.5 ? (bottomLayer / max((1.0 - topLayer) * 2.0, 0.00001)) : (1.0 - (((1.0 - bottomLayer) * 0.5) / max(topLayer, 0.00001)));
		}

		inline float4 ApplyAlphaTransform( float4 topLayer, float4 bottomLayer )
		{
			float topLayerAlpha = topLayer.a * _Opacity;
			float alpha = topLayerAlpha * (1 - bottomLayer.a) + bottomLayer.a;
			//float3 color = lerp (bottomLayer.rgb * bottomLayer.a, topLayer.rgb, topLayerAlpha) / alpha;
			float3 color = (topLayer.rgb * topLayerAlpha * 1 + bottomLayer.rgb * bottomLayer.a * ( 1.0f - topLayerAlpha) ) / alpha;
			return float4( lerp( color, color*alpha, _LastLayer), alpha);
		}
		ENDCG


		Cull Off ZWrite Off ZTest Always

		//Normal
		Pass
		{
			Name "Normal"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		//Multiply
		Pass
		{
			Name "Multiply"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb *= bottomLayer.rgb;
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		///////////////////////////////////////////////////////////////
		Pass
		{
			Name "ColorBurn"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = ColorBurn (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "ColorDodge"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = ColorDodge(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Darken"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Darken(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Divide"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Divide (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Difference"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Difference(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Exclusion"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Exclusion( topLayer, bottomLayer );
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "SoftLight"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = SoftLight(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "HardLight"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = HardLight(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "HardMix"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = HardMix (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Lighten"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Lighten (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "LinearBurn"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = LinearBurn (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "LinearDodge"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = LinearDodge (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "LinearLight"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = LinearLight( topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}
					
		Pass
		{
			Name "Overlay"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Overlay(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "PinLight"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = PinLight(topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "Subtract"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = Subtract( topLayer , bottomLayer );
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
			}

			Pass
			{
				Name "Screen"
				CGPROGRAM
				float4 frag (v2f_img i) : SV_Target
				{
					FETCH_TEXTURES
					//bottomLayer.rgb *= bottomLayer.a;
				topLayer.rgb = Screen (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		Pass
		{
			Name "VividLight"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				FETCH_TEXTURES
				topLayer.rgb = VividLight (topLayer , bottomLayer);
				return ApplyAlphaTransform (topLayer, bottomLayer);
			}
			ENDCG
		}

		///////////////////////////////////////////////////////////////
		Pass
		{
			Name "OpacityMul"
			CGPROGRAM
			float4 frag (v2f_img i) : SV_Target
			{
				float4 topLayer = tex2D (_MainTex, i.uv);
				topLayer.rgb = lerp (_Opacity * topLayer.rgb, _Opacity * topLayer.rgb * topLayer.a, _LastLayer);
				//topLayer.a = lerp ( 1, topLayer.a, _LastLayer);
				return ApplyAlphaTransform (topLayer, float4(0,0,0,1));
				//return topLayer;
			}
			ENDCG
		}

	}
}
