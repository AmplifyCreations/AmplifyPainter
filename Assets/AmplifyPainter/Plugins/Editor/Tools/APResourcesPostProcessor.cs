// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;

namespace AmplifyPainter
{
	public sealed class APResourcesPostProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
			bool updateValue = false;
			bool updateResources = false;
			for( int i = 0; i < importedAssets.Length; i++ )
			{
				if( APResources.CheckExternalResource( importedAssets[ i ] ) )
				{
					updateValue = true;
				}
				updateResources = updateResources || APResources.CheckNativeResources( importedAssets[ i ] );
			}

			for( int i = 0; i < deletedAssets.Length; i++ )
			{
				if( APResources.CheckExternalResource( deletedAssets[ i ] ) )
				{
					updateValue = true;
				}
			}

			if( updateValue )
			{
				EditorToolsContextEx.GetSingleton().Controller.ChannelsController.UpdateValue();
			}

			if( updateResources )
			{
				APResources.LoadResources();
			}
		}
	}
}
