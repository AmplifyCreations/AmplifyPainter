// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//#define DEBUG_RENDER_TEXTURE_MODE

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

using System.Collections.Generic;

namespace AmplifyPainter
{
	public enum WindowType
	{
		SceneView,
		Viewport3D,
		Viewport2D,
	}

	[EditorTool( "Amplify Painter" )]
	public sealed class APEditorTool : EditorTool
	{
		private const string PreviewStr = "Preview - ";
		private const string PreviewNoneStr = "<None>";

		[SerializeField]
		private AmplifyPainter m_controller;

		private readonly APShortcutOverlay m_shortcutInfo = new APShortcutOverlay();

		private bool m_showInfo = false;
		private bool m_pressedInfoButton = false;
		private Rect m_infoButtonArea = new Rect( 0, 0, 32, 32 );
		private Rect m_overallUIArea = new Rect( 35, 8, 128, 100 );
		private Rect m_brushIconArea = new Rect( 2, 32, 32, 32 );
		private Rect m_eraserIconArea = new Rect( 2, 66, 32, 32 );



		private GUIContent m_iconContent;

		private Ray m_ray;

		private string m_previousWindow = "";

		[SerializeField]
		private int m_previewChannelId = 0;

		private List<string> m_previewChannelLabels = new List<string>();
		private string[] m_previewChannelLabelsArr;

		private bool m_saveDetected = false;
		private WindowType m_windowType;

		[MenuItem( "Window/Amplify Painter/All " + APParentWindow.ALL, priority = 1004 )]
		public static void OpenAll()
		{
			APLayersWindow.ShowAmplifyPainterWindow();
			APChannelsWindow.ShowAmplifyPainterWindow();
		}

		private void OnEnable()
		{
			APResources.LoadResources();

			if( m_controller == null )
			{
				m_controller = CreateInstance<AmplifyPainter>();
				m_controller.Init();
			}

			m_iconContent = new GUIContent()
			{
				image = APResources.ToolIconTexture,
				text = "Amplify Painter",
				tooltip = "Amplify Painter: The most awesome painting tool on the Unity Asset Store"
			};

			//m_controller.Colliders = new List<MeshCollider>();

			EditorTools.activeToolChanged += OnToolChanged;
			
			
			if( EditorTools.IsActiveTool( this ) )
			{
				SceneView.duringSceneGui -= DuringSceneView;
				APPainterViewport.duringSceneGui -= DuringSceneView;

				SceneView.duringSceneGui += DuringSceneView;
				if( APPainterViewport.Instance != null )
					APPainterViewport.duringSceneGui += DuringSceneView;
				CreateCollider();
			}

			m_controller.ChannelsController.OnChannelAdded += OnPreviewChannelAdded;
			m_controller.ChannelsController.OnChannelRemoved += OnPreviewChannelRemoved;
			m_controller.ChannelsController.OnChannelUpdated += OnPreviewChannelUpdated;

			OnPreviewOnEnable();
		}
		
		

		private void OnDisable()
		{
			DeleteCollider();

			EditorTools.activeToolChanged -= OnToolChanged;

			SceneView.duringSceneGui -= DuringSceneView;
			APPainterViewport.duringSceneGui -= DuringSceneView;

			m_controller.ChannelsController.OnChannelAdded -= OnPreviewChannelAdded;
			m_controller.ChannelsController.OnChannelRemoved -= OnPreviewChannelRemoved;
			m_controller.ChannelsController.OnChannelUpdated -= OnPreviewChannelUpdated;
		}

		private void OnDestroy()
		{
			ScriptableObject.DestroyImmediate( m_controller );
			m_controller = null;

			APResources.UnloadResources();
		}

		private void CreateCollider()
		{
			Selection.selectionChanged += SetCollider;

			SetCollider();
		}

		private void DeleteCollider()
		{
			Selection.selectionChanged -= SetCollider;

			m_controller.DeleteColliders();
		}

		private void SetCollider()
		{
			m_controller.SetColliders();

			if( m_controller.Collider != null )
			{
				m_controller.GetInfoFromObject( m_controller.Collider.transform, true, OnPreviewReset );
			}
		}

		public void OnToolChanged()
		{
			Init( true );
		}

		public void Init( bool createCollider )
		{
			if( EditorTools.IsActiveTool( this ) )
			{
				SceneView.duringSceneGui -= DuringSceneView;
				APPainterViewport.duringSceneGui -= DuringSceneView;

				if( createCollider )
					CreateCollider();

				SceneView.duringSceneGui += DuringSceneView;
				if( APPainterViewport.Instance != null )
					APPainterViewport.duringSceneGui += DuringSceneView;

				m_controller.BrushController.HookTablet();

				if( SceneView.lastActiveSceneView != null )
				{
					SceneView.lastActiveSceneView.Focus();
				}
				else if( SceneView.sceneViews.Count > 0 )
				{
					( (SceneView)SceneView.sceneViews[ 0 ] ).Focus();
				}

			}
			else
			{
				m_controller.OnToolChangedReset();

				m_controller.BrushController.UnhookTablet();

				DeleteCollider();

				SceneView.duringSceneGui -= DuringSceneView;
				APPainterViewport.duringSceneGui -= DuringSceneView;
			}
		}


		public void SetSaveBehavior()
		{
			m_saveDetected = true;
			m_controller.SetOriginalMaterial();
		}

		public override void OnToolGUI( EditorWindow window )
		{
			//ShowSceneGUI();
			if( m_saveDetected )
			{
				m_saveDetected = false;
				m_controller.SetAPMaterial();
			}
		}

		
		public void OnPreviewChannelAdded( int idx, APChannel channel )
		{
			int arrIdx = channel.CurrentIdx + 1;
			m_previewChannelLabels.Insert( arrIdx, PreviewStr + channel.Name );
			m_previewChannelLabelsArr = m_previewChannelLabels.ToArray();
		}

		public void OnPreviewChannelRemoved( int idx, APChannel channel )
		{
			int arrIdx = idx + 1;
			m_previewChannelLabels.RemoveAt( arrIdx );
			m_previewChannelLabelsArr = m_previewChannelLabels.ToArray();
		}

		public void OnPreviewChannelUpdated( int idx, APChannel channel )
		{
			int arrIdx = channel.CurrentIdx + 1;
			m_previewChannelLabels[ arrIdx ] = PreviewStr + channel.Name;
			m_previewChannelLabelsArr[ arrIdx ] = m_previewChannelLabels[ arrIdx ];
		}

		public void OnPreviewReset()
		{
			m_previewChannelId = 0;
			m_previewChannelLabels.Clear();
			m_previewChannelLabels.Add( PreviewStr + PreviewNoneStr );
			m_previewChannelLabelsArr = m_previewChannelLabels.ToArray();

		}

		public void OnPreviewOnEnable()
		{
			m_previewChannelLabels.Clear();
			m_previewChannelLabels.Add( PreviewStr + PreviewNoneStr );
			List<APChannel> channels = m_controller.ChannelsController.AvailableChannels;
			for( int i = 0; i < channels.Count; i++ )
			{
				m_previewChannelLabels.Add( PreviewStr + channels[ i ].Name );
			}
			m_previewChannelLabelsArr = m_previewChannelLabels.ToArray();
			m_previewChannelId = Mathf.Clamp( m_previewChannelId, 0, m_previewChannelLabelsArr.Length ); ;
		}



		private void ShowShortcutUI()
		{
			Event current = Event.current;
			Handles.BeginGUI();

			if( current.control )
			{
				m_shortcutInfo.Show( KeyCode.LeftControl );
			}
			else if( current.shift )
			{
				m_shortcutInfo.Show( KeyCode.LeftShift );
			}
			else if( ( m_showInfo || m_pressedInfoButton ) && ( current.type == EventType.Repaint || current.type == EventType.Layout ) )
			{
				m_shortcutInfo.Show( KeyCode.None );
			}

			if( current.type == EventType.KeyDown && current.keyCode == KeyCode.I )
			{
				m_showInfo = true;
			}

			if( current.type == EventType.KeyUp && current.keyCode == KeyCode.I )
			{
				m_showInfo = false;
			}

			GUI.Label( m_infoButtonArea, APResources.ToolIconTexture );
			GUILayout.BeginArea( m_overallUIArea );
			m_previewChannelId = EditorGUILayout.Popup( m_previewChannelId, m_previewChannelLabelsArr );
			GUILayout.EndArea();
			Handles.EndGUI();

		}

		public static Event OverrideMouseScroll()
		{
			Event e = Event.current; // Grab the current event
			Event copy = new Event( e );
			if( e.type == EventType.ScrollWheel && e.control )
			{
				e.Use(); // We don't want to propagate the event
				return copy;
			}
			return e;
		}

		public static Event OverrideKey( KeyCode key )
		{
			Event e = Event.current; // Grab the current event
			Event copy = new Event( e );
			if( e.type == EventType.KeyDown && e.keyCode == key )
			{
				e.Use(); // We don't want to propagate the event
				return copy;
			}
			return e;
		}

		private void DuringSceneView( APPainterViewport editorWindow )
		{
			if( EditorWindow.focusedWindow != editorWindow )
				return;

			if( editorWindow is AP2DView )
				m_windowType = WindowType.Viewport2D;
			else
				m_windowType = WindowType.Viewport3D;


			EditorWindow window = editorWindow;
			Camera camera = editorWindow.Camera;

			DrawChannelPreview( window, camera );

			EditorWindowOnGUI( window, camera );
		}
		
		//bool m_stopChanged;
		private void DuringSceneView( SceneView sceneView )
		{
			// Code that forces antialiasing into scene view
			//if( !m_stopChanged )
			//{
			//	RenderTexture.active = null;
			//	SceneView.currentDrawingSceneView.camera.targetTexture.Release();
			//	SceneView.currentDrawingSceneView.camera.targetTexture.antiAliasing = 8;
			//	SceneView.currentDrawingSceneView.camera.targetTexture.Create();
			//	RenderTexture.active = SceneView.currentDrawingSceneView.camera.targetTexture;
			//	m_stopChanged = true;
			//}

			m_windowType = WindowType.SceneView;

			EditorWindow window = sceneView;
			Camera camera = sceneView.camera;

			if( EditorWindow.focusedWindow is APPainterViewport )
				return;

			DrawChannelPreview( window, camera );

			EditorWindowOnGUI( window, camera );
		}
		private const float ToolsToButtonSpacing = 42;
		private const float ButtonSpacing = 34;
		private void EditorWindowOnGUI( EditorWindow window, Camera camera )
		{
			m_controller.BrushController.Update();

			Event current = Event.current;

			Handles.BeginGUI();
			m_controller.CurrentTool = GUI.Toggle( m_brushIconArea, m_controller.CurrentTool == ToolType.Brush, APResources.BrushIcon, "Button" ) ? ToolType.Brush : ToolType.Eraser;
			m_controller.CurrentTool = GUI.Toggle( m_eraserIconArea, m_controller.CurrentTool == ToolType.Eraser, APResources.EraserIcon, "Button" ) ? ToolType.Eraser : ToolType.Brush;

			Rect newPos = m_eraserIconArea;
			newPos.y += ToolsToButtonSpacing;
			//Channels
			if( GUI.Button( newPos, APResources.ChannelsWindowIcon ) )
			{
				APChannelsWindow.OpenAmplifyPainterChannelsWindow();
			}
			newPos.y += ButtonSpacing;
			//Layers
			if( GUI.Button( newPos, APResources.LayersWindowIcon ) )
			{
				APLayersWindow.OpenAmplifyPainterLayersWindow();
			}
			newPos.y += ButtonSpacing;
			//Brush
			if( GUI.Button( newPos, APResources.BrushesWindowIcon ) )
			{
				APBrushWindow.OpenAmplifyPainterBrushesWindow();
			}
			newPos.y += ButtonSpacing;
			//3D View
			if( GUI.Button( newPos, APResources.View3DWindowIcon ) )
			{
				if( !( window is AP3DView ) )
				{
					AP3DView.OpenAmplifyPainter3DViewWindow();
				}
			}
			newPos.y += ButtonSpacing;
			//2D View
			if( GUI.Button( newPos, APResources.View2DWindowIcon ) )
			{
				if( !( window is AP2DView ) )
				{
					AP2DView.OpenAmplifyPainter2DViewWindow();
				}
			}
			newPos.y += ButtonSpacing;
			//Export
			if( GUI.Button( newPos, APResources.TextureExportWindowIcon ) )
			{
				APTextureExport.OpenAmplifyPainterExportWindow();
			}
			
			Handles.EndGUI();

			ShowShortcutUI();
			if( !m_controller.LockMouse )
			{
				Vector2 screenPixelPos = HandleUtility.GUIPointToScreenPixelCoordinate( current.mousePosition );
				m_ray = camera.ScreenPointToRay( screenPixelPos );
			}

			bool fakeMouse = false;
			if( EditorWindow.focusedWindow != null && m_previousWindow != EditorWindow.focusedWindow.GetType().ToString() )
			{
				m_controller.SetColliders();
				//if( m_controller.Collider != null )
				//	m_controller.CurrentObject = m_controller.Collider.transform;
			}

			if( m_previousWindow.Contains( "ColorPicker" ) && EditorWindow.focusedWindow == window )
			{
				m_previousWindow = "";
				fakeMouse = true;
				current.type = EventType.MouseDown;
			}

			// consumes scroll event ( but only if control is ON )
			if( window is SceneView )
				current = OverrideMouseScroll();
			if( window is APPainterViewport )
				current = OverrideKey( KeyCode.F );

			int controlID = GUIUtility.GetControlID( window.GetHashCode(), FocusType.Passive );
			switch( current.GetTypeForControl( controlID ) )
			{
				case EventType.KeyDown:
				{
					if( current.keyCode == KeyCode.B )
					{
						m_controller.CurrentTool = m_controller.CurrentTool == ToolType.Brush ? ToolType.Eraser : ToolType.Brush;
					}

					if( current.keyCode == KeyCode.C )
					{
						// open color picker
						m_controller.OpenColorPicker();
						UnityEngine.Cursor.visible = true;
					}

					if( current.keyCode == KeyCode.V && !current.shift )
					{
						if( m_previewChannelLabelsArr.Length > 0 )
						{
							m_previewChannelId = ( m_previewChannelId + 1 ) % m_previewChannelLabelsArr.Length;
						}
					}
					else if( current.keyCode == KeyCode.V && current.shift )
					{
						if( m_previewChannelLabelsArr.Length > 0 )
						{
							m_previewChannelId = ( m_previewChannelId > 0 ) ? ( m_previewChannelId - 1 ) : ( m_previewChannelLabelsArr.Length - 1 );
						}
					}
				}
				break;
				case EventType.KeyUp:
				{
					if( current.keyCode == KeyCode.LeftControl || current.keyCode == KeyCode.RightControl )
					{
						m_controller.UnlockMouseControls();
					}
				}
				break;
				case EventType.ScrollWheel:
				{
					// TODO: needs proper size adjustment
					if( current.control )
						m_controller.BrushController.AdjustSize( current.delta.y );
				}
				break;
				case EventType.Layout:
				{
					HandleUtility.AddDefaultControl( controlID );
				}
				break;
				case EventType.MouseDown:
				case EventType.MouseDrag:
				{
					if( fakeMouse )
						HandleUtility.AddDefaultControl( controlID );

					// there's a bug in the next line somehow. if you hold control while painting these controlID fail
					if( current.GetTypeForControl( controlID ) == EventType.MouseDrag && GUIUtility.hotControl != controlID )
						return;

					if( current.button != 0 || current.alt )
						return;

					if( HandleUtility.nearestControl != controlID )
						return;

					if( current.type == EventType.MouseDown )
					{
						GUIUtility.hotControl = controlID;
						m_pressedInfoButton = m_infoButtonArea.Contains( current.mousePosition );
					}

					// Handle Horizontal/Vertical Control - Opacity/Rotation
					bool inControl = m_controller.HandleOrthogonalControls( current );

					// Try Painting in this camera/window
					if( !inControl )
						m_controller.StartPainting( window, m_windowType, current, camera, m_ray );

					if( fakeMouse )
						current.type = EventType.Layout;
					else
						current.Use();
				}
				break;
				case EventType.MouseUp:
				{
					if( GUIUtility.hotControl != controlID )
						return;

					m_controller.StopPainting();

					// update again because of dilation
					for( int i = 0; i < m_controller.ChannelsController.ChannelCount; i++ )
					{
						if( m_controller.ChannelsController.AvailableChannels[ i ].ValidToDraw &&
							m_controller.BrushController.CanPaintOnChannel( m_controller.ChannelsController.AvailableChannels[ i ] ) )
						{
							m_controller.UpdateLayerValueOnChannel( i );
						}
					}

					m_controller.UnlockMouseControls();

					m_pressedInfoButton = false;

					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
				case EventType.Repaint:
				{
					// Draw preview texture
					//if( m_channelCycle && !( window is AP2DView ) )
					//	m_controller.DrawChannelPreview( camera );

					// TODO: put this code into controller (probably refactor it to make sure the view stuff remains here, ie, preview brush and hidding controller)
					if( !(ColorPickerEx.IsVisible || EditorGuiEx.ColorPickerID != 0) )
						DrawBrushPreview( window, m_windowType, current, camera, m_ray );

					HandleUtility.Repaint();
				}
				break;
			}

			if( EditorWindow.focusedWindow != null )
				m_previousWindow = EditorWindow.focusedWindow.GetType().ToString();
		}

		void DrawChannelPreview( EditorWindow window, Camera camera )
		{
			Event current = Event.current;

			int controlID = GUIUtility.GetControlID( window.GetHashCode(), FocusType.Passive );
			if( current.GetTypeForControl( controlID ) == EventType.Repaint )
			{
				if( m_previewChannelId != 0 )
					m_controller.DrawChannelPreview( camera, m_previewChannelId - 1, window, m_windowType );
			}
		}

		private void DrawBrushPreview( EditorWindow window, WindowType windowType, Event current, Camera camera, Ray ray )
		{
			bool canPreview = false;
			Vector3 tangent = Vector3.zero;
			Vector3 point, normal, wirepoint, wirenormal;
			Vector2 texcoord = Vector2.zero;
			point = normal = wirepoint = wirenormal = Vector3.zero;
			Rect noBorders = window.position;
			noBorders.y += 21 + 17;
			noBorders.height -= 17;

			bool mouseInside = noBorders.Contains( GUIUtility.GUIToScreenPoint( current.mousePosition ) );
			if( window is AP2DView )
			{
				if( mouseInside && ( window as AP2DView ).Pencast( current.mousePosition, out PenCastHit penHit ) )
				{
					point = penHit.point;
					normal = penHit.normal;
					tangent = penHit.tangent;
					texcoord = penHit.texcoord;
					canPreview = true;
					wirepoint = penHit.ray.origin;
					wirepoint.z = 0;
					wirenormal = Vector3.back;
				}
			}
			else
			{
				if( mouseInside && m_controller.Collider != null && m_controller.Collider.Raycast( ray, out RaycastHit hit, float.MaxValue ) )
				{
					point = hit.point;
					normal = hit.BariNormal();
					tangent = hit.BariTangent();
					texcoord = hit.BariTextCoord(0);
					canPreview = true;

					wirepoint = point;
					wirenormal = normal;
				}
			}

			if( canPreview )
			{
				if( !m_controller.IsPainting )
				{
					m_controller.DrawHandleProjectedTexture( point, normal, tangent, texcoord, camera, window, windowType );
				}

				// TODO: remove this controller code out of here
				if( m_controller.BrushController.CurrentBrush.Alignment == ProjectionAlignment.Camera )
					wirenormal = -camera.transform.forward;

				APHelpers.DrawWire( wirepoint, wirenormal, camera/*, window.position*/ );

				if( EditorWindow.focusedWindow == window )
					UnityEngine.Cursor.visible = false;
			}
			else
			{
				UnityEngine.Cursor.visible = true;
			}
		}

		public override GUIContent toolbarIcon { get { return m_iconContent; } }
		public AmplifyPainter Controller { get { return m_controller; } }
	}
}
