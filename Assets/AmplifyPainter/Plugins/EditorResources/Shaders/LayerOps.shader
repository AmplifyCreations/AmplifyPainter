Shader "Hidden/AmplifyPainter/LayerOps"
{
    Properties
    {
        [HideInInspecctor]_MainTex ("Texture", 2D) = "white" {}
		[HideInInspecctor]_TilingAndOffset("TilingAndOffset", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            uniform sampler2D _MainTex;
			uniform float4 _TilingAndOffset;

            float4 frag (v2f i) : SV_Target
            {
				float2 uvs = i.uv*_TilingAndOffset.xy + _TilingAndOffset.zw;
				return tex2D( _MainTex, uvs );
            }
            ENDCG
        }
    }
}
