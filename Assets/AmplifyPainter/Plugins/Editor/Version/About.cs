// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	public sealed class About : EditorWindow
	{
		private const string AboutImageGUID = "7b01816c7bc52db4ab066bc87bd7b126";
		private Vector2 m_scrollPosition = Vector2.zero;
		private Texture2D m_aboutImage;

		[MenuItem( "Window/Amplify Painter/About...", false, 2001 )]
		static void Init()
		{
			About window = (About)GetWindow( typeof( About ), true, "About Amplify Painter" );
			window.minSize = new Vector2( 502, 290 );
			window.maxSize = new Vector2( 502, 290 );
			window.Show();
		}

		[MenuItem( "Window/Amplify Painter/Manual", false, 2000 )]
		static void OpenManual()
		{
			Application.OpenURL( "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Painter/Manual" );
		}

		private void OnEnable()
		{
			m_aboutImage = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( AboutImageGUID ) );
		}

		public void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView( m_scrollPosition );

			GUILayout.BeginVertical();

			GUILayout.Space( 10 );

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box( m_aboutImage, GUIStyle.none );

			if( Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) )
				Application.OpenURL( "http://www.amplify.pt" );

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUIStyle labelStyle = new GUIStyle( EditorStyles.label );
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.wordWrap = true;

			GUILayout.Label( "\nAmplify Painter " + VersionInfo.StaticToString(), labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.Label( "\nCopyright (c) Amplify Creations, Lda. All rights reserved.\n", labelStyle, GUILayout.ExpandWidth( true ) );

			GUILayout.EndVertical();

			GUILayout.EndScrollView();
		}
	}
}
