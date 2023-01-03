// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	public sealed class AP2DView : APPainterViewport
	{
		[MenuItem( "Window/Amplify Painter/2D Painter " + VIEW2D, priority = 1009 )]
		public static AP2DView OpenAmplifyPainter2DViewWindow()
		{
			var window = OpenOrCloseWindow<AP2DView>( false, null );
			if( window )
				window.titleContent = new GUIContent( "2D Paint" );

			window.minSize = new Vector2( 300, 100 );

			window.autoRepaintOnSceneChange = true;
			window.m_is3DView = false;
			return window;
		}
	}
}
