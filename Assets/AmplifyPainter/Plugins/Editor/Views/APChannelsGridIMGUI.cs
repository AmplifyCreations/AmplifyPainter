// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyPainter
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public sealed class APChannelsGridIMGUI : PopupWindowContent
	{
		private readonly APLayersWindow m_view;

		public APChannelsGridIMGUI( APLayersWindow view )
		{
			m_view = view;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2( 500, EditorGUIUtility.singleLineHeight * ( 10 /*+ 6 + 4*/ ) );
		}

		override public void OnGUI( Rect rect )
		{
			APChannelController controller = m_view.Controller.ChannelsController;
			GUILayout.BeginHorizontal();
			for( int channelIdx = 0; channelIdx < controller.AvailableChannels.Count; channelIdx++ )
			{
				EditorGUILayout.BeginVertical();
				GUILayout.Button( controller.AvailableChannels[ channelIdx ].Name );
				for( int i = 0; i < controller.AvailableChannels[ channelIdx ].Layers.Count; i++ )
				{
					bool selected = false;
					selected = GUILayout.Toggle( selected, string.Empty );
				}
				EditorGUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}
	}
}
