// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	public sealed class AP3DView : APPainterViewport
	{
		[MenuItem( "Window/Amplify Painter/3D Painter " + VIEW3D, priority = 1008 )]
		public static AP3DView OpenAmplifyPainter3DViewWindow()
		{
			var window = OpenOrCloseWindow<AP3DView>( false, null );
			if( window )
				window.titleContent = new GUIContent( "3D Paint" );

			window.minSize = new Vector2( 300, 100 );

			window.autoRepaintOnSceneChange = true;

			return window;
		}
	}
}
