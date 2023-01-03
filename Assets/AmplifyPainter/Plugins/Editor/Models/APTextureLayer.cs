// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APTextureLayer : APSMORTLayer
	{
		public Texture2D TextureValue;

		override protected void OnEnable()
		{
			base.OnEnable();
			if( TextureValue != null )
			{
				APResources.RegisterExternalResource( TextureValue, this );
			}
		}

		public void Init(int width, int height, RenderTextureFormat format )
		{
			BaseInit();
			SMORTInit( width, height, format );
			IsEditable = false;
			TextureValue = Texture2D.blackTexture;
		}

		public void Init( Texture2D value, RenderTextureFormat format )
		{
			BaseInit();
			SMORTInit( value.width, value.height, format );
			IsEditable = false;
			TextureValue = value;
		}

		public override APDropResult OnDroppedObjects( UnityEngine.Object[] objects )
		{
			Texture2D tex = objects[ 0 ] as Texture2D;
			if( tex != null )
			{
				if( TextureValue != null )
				{
					APResources.ReleaseExternalResource( TextureValue, this );
				}

				APResources.RegisterExternalResource( tex, this );
				TextureValue = tex;
				RefreshValue();
				return APDropResult.Success;
			}

			if( objects[ 0 ] is Material )
				return APDropResult.SwitchToMaterial;

			if( objects[ 0 ] is Shader )
				return APDropResult.SwitchToShader;

			return APDropResult.Fail;
		}

		override protected void OnDestroy()
		{
			base.OnDestroy();
			APResources.ReleaseExternalResource( TextureValue, this );
			TextureValue = null;
		}

		public override APLayerType LayerType { get { return APLayerType.Texture2D; } }
		//public override Texture Value { get { return m_value; } }
		public override void RefreshValue()
		{
			RenderTexture buffer = RenderTexture.active;
			RenderTexture.active = null;
			LayerOpsMat.SetVector( LayerOpParamsId, LayerOpsParams );
			Graphics.Blit( TextureValue, FinalValue, LayerOpsMat );
			RenderTexture.active = buffer;
		}
	}
}
