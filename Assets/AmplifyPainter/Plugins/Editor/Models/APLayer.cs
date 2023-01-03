// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyPainter
{

	[Serializable]
	public sealed class APLayer : APParentLayer
	{
		public RenderTexture ParentValue;

		public void Init( ChannelType channelType, RenderTextureFormat format, int width, int height, Color defaultColor )
		{
			BaseInit();
			IsEditable = true;
			ParentValue = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			ParentValue.Create();
			RenderTexture buffer = RenderTexture.active;
			RenderTexture.active = null;
			//Texture2D defaultValue = null;
			//switch( channelType )
			//{
			//	default:
			//	case ChannelType.Color:defaultValue = Texture2D.blackTexture;break;
			//	case ChannelType.Normal:defaultValue = Texture2D.normalTexture;break;
			//}
			//Graphics.Blit( defaultValue, ParentValue );
			APResources.CopyColorMaterial.SetColor( APResources.CopyColorProperty, defaultColor );
			Graphics.Blit( null, ParentValue, APResources.CopyColorMaterial );
			RenderTexture.active = buffer;
		}

		public override APDropResult OnDroppedObjects( UnityEngine.Object[] objects )
		{
			for( int i = 0; i < objects.Length; i++ )
			{
				Texture2D tex = objects[ i ] as Texture2D;
				if( tex != null )
				{
					Graphics.Blit( tex, ParentValue );
					return APDropResult.Success;
				}
			}
			return APDropResult.Fail;
		}

		public void OnDestroy()
		{
			ParentValue.Release();
			DestroyImmediate( ParentValue );
			ParentValue = null;
		}

		public override Texture Value { get { return ParentValue; } }
		public override void SetSize( int width, int height )
		{
			ParentValue = ParentValue.SetSize( width, height );
		}
	}

}
