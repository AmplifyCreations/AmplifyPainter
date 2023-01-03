// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyPainter
{
	// Catch when scene is saved ( Ctr + S ) 
	public sealed class APSceneSaveCallback : UnityEditor.AssetModificationProcessor
	{
		private const string UnityStr = ".unity";

		static string[] OnWillSaveAssets( string[] paths )
		{
			bool canSave = false;

			if ( paths.Length == 0 )
			{
				canSave = true;
			}
			else
			{
				for ( int i = 0; i < paths.Length; i++ )
				{
					if ( paths[ i ].Contains( UnityStr ) )
					{
						canSave = true;
						break;
					}
				}
			}

			if( canSave )
			{
				EditorToolsContextEx.GetSingleton().SetSaveBehavior();
			}

			return paths;
		}
	}
}
