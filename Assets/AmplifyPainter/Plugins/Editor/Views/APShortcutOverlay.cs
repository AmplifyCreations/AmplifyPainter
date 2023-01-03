// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	public enum APMouseActions
	{
		LeftMouseButton = 0,
		RightMouseButton,
		MiddleMouseButton,
		MouseWheelUp,
		MouseWheelDown,
		LeftMouseDrag,
		RightMouseDrag,
		UpMouseDrag,
		DownMouseDrag,
		HorizontalDrag,
		VerticalDrag
	}

	public class APShortcutItem
	{
		//public KeyCode Modifier;
		//public KeyCode Key;
		//public int MouseId;
		//public string Description;
		public string Label;
		string GetModifierStr( KeyCode modifier )
		{
			string modifierStr = string.Empty;
			if( modifier == KeyCode.LeftControl || modifier == KeyCode.RightControl )
			{
				modifierStr = "Ctrl";
			}
			else if( modifier == KeyCode.LeftShift || modifier == KeyCode.RightShift )
			{
				modifierStr = "Shift";
			}
			else if( modifier == KeyCode.LeftAlt || modifier == KeyCode.RightAlt )
			{
				modifierStr = "Alt";
			}
			return modifierStr;
		}

		public APShortcutItem( KeyCode modifier, KeyCode key, string description )
		{
			string modifierStr = GetModifierStr( modifier );
			Label = string.Format( "{0} + {1}: {2}", modifierStr, key, description );
		}

		public APShortcutItem( KeyCode key, string description )
		{
			Label = string.Format( "{0}: {1}", key, description );
		}

		public APShortcutItem( KeyCode modifier, APMouseActions mouseAction, string description )
		{
			string modifierStr = GetModifierStr( modifier );
			Label = string.Format( "{0} + {1}: {2}", modifierStr, mouseAction.ToString(), description );
		}

		public APShortcutItem( KeyCode modifier, APMouseActions mouseModifier, APMouseActions mouseAction, string description )
		{
			string modifierStr = GetModifierStr( modifier );
			Label = string.Format( "{0} + {1} + {2}: {3}", modifierStr, mouseModifier.ToString(), mouseAction.ToString(), description );
		}
	}

	public class APShortcutOverlay
	{
		private Color m_shortcutBackgroundColor = new Color( 0.8f, 0.8f, 0.8f, 0.8f );
		private Color m_shortcutTextColor = new Color( 1f, 1f, 1f, 0.8f );

		public Dictionary<KeyCode, APShortcutItem[]> Shortcuts = new Dictionary<KeyCode, APShortcutItem[]>()
		{
			{
				KeyCode.LeftShift,new APShortcutItem[]
				{
					new APShortcutItem(KeyCode.LeftShift,KeyCode.Alpha1,"Open Channels Window"),
					new APShortcutItem(KeyCode.LeftShift,KeyCode.Alpha2,"Open Layers Window"),
					new APShortcutItem(KeyCode.LeftShift,KeyCode.Alpha3,"Open Brush Window"),
					new APShortcutItem(KeyCode.LeftShift,KeyCode.Alpha4,"Open all windows"),
					new APShortcutItem(KeyCode.LeftShift,KeyCode.V,"Cicle Channels Backward")
				}
			},
			{
				KeyCode.LeftControl,new APShortcutItem[]
				{
					new APShortcutItem(KeyCode.LeftControl,APMouseActions.MouseWheelUp,"Increase Brush Size"),
					new APShortcutItem(KeyCode.LeftControl,APMouseActions.MouseWheelDown,"Decrease Brush Size"),
					new APShortcutItem(KeyCode.LeftControl,APMouseActions.LeftMouseButton,APMouseActions.HorizontalDrag,"Change Brush Opacity"),
					new APShortcutItem(KeyCode.LeftControl,APMouseActions.LeftMouseButton,APMouseActions.VerticalDrag,"Change Brush Rotation")
				}
			},
			{
				KeyCode.None,new APShortcutItem[]
				{
					new APShortcutItem(KeyCode.I,"Shows this window"),
					new APShortcutItem(KeyCode.C,"Open Color Picker"),
					new APShortcutItem(KeyCode.V,"Cicle Channels Forward"),
					new APShortcutItem(KeyCode.B,"Toggle between Brush and Eraser tool"),
					new APShortcutItem(KeyCode.LeftShift,"Window related tools"),
					new APShortcutItem(KeyCode.LeftControl,"Brush related tools"),
				}
			}
		};

		public void Show( KeyCode modifier )
		{
			if( EditorWindow.focusedWindow == null || ( EditorWindow.focusedWindow.GetType() != typeof( SceneView ) && EditorWindow.focusedWindow.GetType() != typeof( AP3DView ) && EditorWindow.focusedWindow.GetType() != typeof( AP2DView ) ) || !SceneView.mouseOverWindow )
			{
				return;
			}

			Color buffer = GUI.color;
			//Handles.BeginGUI();

			GUILayout.BeginArea( new Rect( 35, 32, 400, 450 ) );
			Rect rect = EditorGUILayout.BeginVertical();
			GUI.color = m_shortcutBackgroundColor;
			GUI.Box( rect, GUIContent.none, GUI.skin.button );

			GUI.color = m_shortcutTextColor;

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label( "Shortcuts" );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			for( int i = 0; i < Shortcuts[ modifier ].Length; i++ )
			{
				GUILayout.Label( Shortcuts[ modifier ][ i ].Label );
			}

			EditorGUILayout.EndVertical();

			GUILayout.EndArea();
			//Handles.EndGUI();
			GUI.color = buffer;
		}
	}
}
