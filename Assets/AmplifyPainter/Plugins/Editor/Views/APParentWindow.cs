// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	public class APParentWindow : SearchableEditorWindow
	{
		public const string CHANNELS = "_#1";
		public const string LAYERS = "_#2";
		public const string BRUSH = "_#3";
		public const string ALL = "_#4";
		public const string VIEW3D = "_#8";
		public const string VIEW2D = "_#9";
		public const string EXPORT = "_#0";

		[SerializeField]
		protected AmplifyPainter m_controller;

		public override void OnEnable()
		{
			base.OnEnable();

			APResources.LoadResources();

			APEditorTool instance = EditorToolsContextEx.GetSingleton();
			m_controller = instance.Controller;
			m_controller.OnToolChangedResetEvent += OnToolChangedReset;
		}

		public virtual void OnToolChangedReset() { }

		public override void OnDisable()
		{
			base.OnDisable();
			m_controller.OnToolChangedResetEvent -= OnToolChangedReset;
		}

		public virtual void OnDestroy()
		{
			m_controller = null;
		}

		private void OnFocus()
		{
			Cursor.visible = true;
		}

		public static T OpenOrCloseWindow<T>( bool utility, string title ) where T : EditorWindow
		{
			UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll( typeof( T ) );
			EditorWindow win = wins.Length > 0 ? (EditorWindow)( wins[ 0 ] ) : null;

			if( !win )
			{
				win = ScriptableObject.CreateInstance( typeof( T ) ) as EditorWindow;
				if( title != null )
					win.titleContent = new GUIContent( title );
				if( utility )
					win.ShowUtility();
				else
					win.Show();
			}
			else
			{
				win.Close();
			}

			return win as T;
		}

		public AmplifyPainter Controller { get { return m_controller; } }
	}
}
