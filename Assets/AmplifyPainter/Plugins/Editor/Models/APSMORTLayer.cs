// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System;

namespace AmplifyPainter
{
	[Serializable]
	public class APSMORTLayer : APParentLayer
	{
		protected const string LayerOpParamsIdStr = "_TilingAndOffset";

		[SerializeField]
		public Vector4 LayerOpsParams = new Vector4( 1, 1, 0, 0 );

		public Material LayerOpsMat;
		public int LayerOpParamsId;
		public RenderTexture FinalValue;

		protected virtual void OnEnable()
		{
			if( LayerOpsMat == null )
			{
				LayerOpsMat = new Material( APResources.LayerOpsShader )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				LayerOpParamsId = Shader.PropertyToID( LayerOpParamsIdStr );
			}
		}

		protected virtual void OnDestroy()
		{
			DestroyImmediate( LayerOpsMat );
			LayerOpsMat = null;

			FinalValue.Release();
			DestroyImmediate( FinalValue );
			FinalValue = null;
		}

		protected void SMORTInit( int width, int height, RenderTextureFormat format )
		{
			FinalValue = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
		}

		public override Texture Value { get { return FinalValue; } }
	}
}
