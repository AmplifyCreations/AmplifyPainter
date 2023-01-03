// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//#define DEBUG_RENDER_TEXTURE_MODE

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AmplifyPainter
{
	public enum ToolType
	{
		Brush,
		Eraser
	}

	[Serializable]
	public sealed class AmplifyPainter : ScriptableObject
	{
		private const string BrushMatrixPropertyStr = "_BrushMatrix";
		private const string BrushProjSettingPropertyStr = "_BrushProjSetting";
		private const string BrushSettingsPropertyStr = "_BrushSettings";
		private const string BrushTexPropertyStr = "_BrushTex";
		private const string BrushColorPropertyStr = "_BrushColor";
		private const string BrushMaskPropertyStr = "_BrushMask";
		private const string LayerTexPropertyStr = "_LayerTex";
		private const string BrushMousePosPropertyStr = "_MousePos";
		private const string MainTexPropertyStr = "_MainTex";
		private const string BrushBlendOpStr = "_AP_BlendOP";

		private const string IsNormalKeyword = "_IsNormal";
		private const string DefaultBrushKeyword = "_DefaultBrush";
		private const string FlatUVKeyword = "_FlatUV";
		private const string UVModeKeyword = "_UVmode";

		public delegate void ControllerToViewUpdate();
		public delegate void ControllerToViewLayerUpdate( APChannel channel, APParentLayer layer );

		public event ControllerToViewUpdate OnChannelsUpdated;
		public event ControllerToViewUpdate OnUpdateInfoFromObjectEvent;
		public event ControllerToViewUpdate OnFinishedUpdateInfoFromObjectEvent;
		public event ControllerToViewUpdate OnUpdateLayerView;
		public event ControllerToViewUpdate OnToolChangedResetEvent;
		public event ControllerToViewLayerUpdate OnAddFirstLayer;

		[SerializeField]
		private Transform m_currentObject;

		[SerializeField]
		private MeshRenderer m_currentMeshRenderer;

		[SerializeField]
		private Material m_originalMaterial;

		[SerializeField]
		private Material m_currentMaterial;

		[SerializeField]
		private ShaderType m_currentShaderType;

		[SerializeField]
		private MeshFilter m_currentMeshFilter;

		[SerializeField]
		private APTexturePropertyController m_propertiesController;

		[SerializeField]
		private APBrushController m_brushController;

		[SerializeField]
		private APChannelController m_channelsController;

		[SerializeField]
		public string ProjectPath = string.Empty;

		[SerializeField]
		public bool LockMouse; // Maybe this should be in VIEW instead?

		[SerializeField]
		public bool IsPainting;

		[SerializeField]
		private RenderTexture m_mask;

		[SerializeField]
		public ToolType CurrentTool;

		//private List<MeshCollider> m_colliders;

		[SerializeField]
		private MeshCollider m_collider;

		// No serialization needed fields below
		private bool m_verticalControl;
		private bool m_horizontalControl;

		private bool m_warpUp;
		private bool m_warpDown;
		private bool m_warpLeft;
		private bool m_warpRight;



		private Vector2 m_initialMousePos = Vector2.zero;

		// TODO: revert this later
		//private bool m_holdPainting = false;
		//private Vector3 m_initialPos = Vector3.zero;
		//private Vector3 m_initialNormal = Vector3.zero;
		//private Vector3 m_initialTangent = Vector3.zero;
		//private Vector2 m_initialMousePos2D = Vector2.zero;

		private static Material s_previewMaterial;

		private int BrushMatrixPropertyID;
		private int BrushProjSettingPropertyID;
		private int BrushSettingsPropertyID;
		private int BrushTexPropertyID;
		private int BrushColorPropertyID;
		private int BrushMaskPropertyID;
		private int LayerTexPropertyID;
		private int MainTexPropertyID;
		private int BrushMousePosPropertyID;
		private int BrushBlendOpID;

		private bool m_allowSceneCleanUp = true;

		public void Init()
		{
			m_propertiesController = CreateInstance<APTexturePropertyController>();
			m_propertiesController.Init();

			m_channelsController = CreateInstance<APChannelController>();
			m_channelsController.Init( 512, 512, RenderTextureFormat.ARGB32 );

			m_brushController = CreateInstance<APBrushController>();
			m_brushController.Init();

			// Default - No Channels
			{
				APBrush brush = CreateInstance<APBrush>();
				brush.Name = "Default";
				brush.Size = new BrushSize( 1f );
				brush.Strength = new BrushFlow( 1f );
				brush.Rotation = new BrushRotation( 0f );
				brush.ComponentsPerChannel = new List<APBrushComponentsPerChannel>();
				brush.Method = StrokeMethod.Space;
				brush.Spacing = new BrushSpacing( 0.2f );
				brush.Mask = APResources.GuassianTexture;
				brush.BackCulling = true;
				brush.Alignment = ProjectionAlignment.TangentWrap;

				m_brushController.AddBrush( brush );
				m_brushController.SelectBrush( 0, false );
			}
		}

		public void OnToolChangedReset()
		{
			ResetMaterialOnObject();

			m_currentObject = null;
			m_currentMeshRenderer = null;
			
#if !DEBUG_RENDER_TEXTURE_MODE
			ResetTexturesOnMaterial();

			m_propertiesController.ResetContainer();
#endif
			ResetChannels();

			if( OnToolChangedResetEvent != null )
			{
				OnToolChangedResetEvent();
			}
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
			EditorApplication.wantsToQuit += OnEditorQuit;
			EditorApplication.playModeStateChanged += PlayModeStateChanged;
			EditorSceneManager.activeSceneChangedInEditMode += ActiveSceneChangedInEditMode;
			BrushMatrixPropertyID = Shader.PropertyToID( BrushMatrixPropertyStr );
			BrushProjSettingPropertyID = Shader.PropertyToID( BrushProjSettingPropertyStr);
			BrushMousePosPropertyID = Shader.PropertyToID( BrushMousePosPropertyStr );
			BrushSettingsPropertyID = Shader.PropertyToID( BrushSettingsPropertyStr );
			BrushTexPropertyID = Shader.PropertyToID( BrushTexPropertyStr );
			BrushColorPropertyID = Shader.PropertyToID( BrushColorPropertyStr );
			BrushMaskPropertyID = Shader.PropertyToID( BrushMaskPropertyStr );
			LayerTexPropertyID = Shader.PropertyToID( LayerTexPropertyStr );
			MainTexPropertyID = Shader.PropertyToID( MainTexPropertyStr );
			BrushBlendOpID = Shader.PropertyToID( BrushBlendOpStr );
		}

		private void PlayModeStateChanged( PlayModeStateChange obj )
		{
			switch( obj )
			{
				case PlayModeStateChange.EnteredPlayMode:
				{
					m_allowSceneCleanUp = false;
				}
				break;
				case PlayModeStateChange.ExitingPlayMode:
				break;
				case PlayModeStateChange.ExitingEditMode:
				{
					if( m_currentMeshRenderer != null && m_originalMaterial != null )
					{
						m_currentMeshRenderer.sharedMaterial = m_originalMaterial;
					}
				}
				break;
				case PlayModeStateChange.EnteredEditMode:
				{
					if( m_currentMeshRenderer != null && m_currentMaterial != null )
					{
						m_currentMeshRenderer.sharedMaterial = m_currentMaterial;
					}
				}
				break;
			}
		}

		//This is also called when play mode is turned off so m_allowSceneCleanUp is set to false when 
		//detecting play mode changes
		//We don't want to reset tool when this happens
		private void ActiveSceneChangedInEditMode( Scene arg0, Scene arg1 )
		{
			if( m_allowSceneCleanUp )
			{
				OnToolChangedReset();
				DeleteColliders();
			}
			else
			{
				m_allowSceneCleanUp = true;
			}
		}

		private void OnDisable()
		{
			EditorApplication.wantsToQuit -= OnEditorQuit;
			EditorApplication.playModeStateChanged -= PlayModeStateChanged;
			EditorSceneManager.activeSceneChangedInEditMode -= ActiveSceneChangedInEditMode;
		}

		bool OnEditorQuit()
		{
			if( m_currentMeshRenderer != null && m_originalMaterial != null )
			{
				m_currentMeshRenderer.sharedMaterial = m_originalMaterial;
			}
			return true;
		}

		public void OnDestroy()
		{
			if( m_currentMeshRenderer != null && m_originalMaterial != null )
			{
				m_currentMeshRenderer.sharedMaterial = m_originalMaterial;
			}

			OnChannelsUpdated = null;
			OnToolChangedResetEvent = null;

			OnAddFirstLayer = null;

			OnUpdateInfoFromObjectEvent = null;
			OnFinishedUpdateInfoFromObjectEvent = null;
			OnUpdateLayerView = null;

			DestroyImmediate( m_channelsController );
			m_channelsController = null;

			DestroyImmediate( m_propertiesController );
			m_propertiesController = null;

			DestroyImmediate( m_brushController );
			m_brushController = null;
		}

		public void ResetChannels()
		{
			m_channelsController.ResetChannels();
			m_brushController.OnChannelsReset();
			if( m_mask != null )
				m_mask.Release();
			m_mask = null;
		}

		public APChannel AddChannel( APTextureProperty property = null )
		{
			APChannel channel = m_channelsController.AddChannel( property );
			m_brushController.AddChannelToBrush( channel );
			if( OnChannelsUpdated != null )
			{
				OnChannelsUpdated();
			}
			return channel;
		}

		public void AddChannel( APChannelTemplateItem item )
		{
			APChannel channel = m_channelsController.AddChannel( item );
			m_brushController.AddChannelToBrush( channel );
			if( OnChannelsUpdated != null )
			{
				OnChannelsUpdated();
			}
			if( !item.Enabled )
				channel.CurrentLayerIdx = -1;

			AddLayerToChannel( APLayerType.Default, channel.CurrentIdx, 0, true ,false);
		}

		public bool RemoveChannel( int idx = -1 )
		{
			bool success = m_channelsController.RemoveChannel( idx );
			if( success )
			{
				m_brushController.RemoveChannelFromBrush( idx );
			}
			if( OnChannelsUpdated != null )
			{
				OnChannelsUpdated();
			}
			return success;
		}

		public APParentLayer AddLayerToChannel( APLayerType layerType, int channelIdx, int layerIdx, bool fireNotificationToView , bool clearLayer )
		{
			if( channelIdx < 0 )
				channelIdx = m_channelsController.CurrentChannelIdx;

			APParentLayer layer = m_channelsController.AddLayerToChannel( layerType, channelIdx, layerIdx , clearLayer );
			if( m_channelsController.LayerCount( channelIdx ) == 1 )
			{
				// Need to copy single layer value into channel final value
				m_channelsController.UpdateValue( channelIdx );
				FeedChannelToMaterial( channelIdx );
			}

			if( fireNotificationToView )
			{
				if( OnAddFirstLayer != null )
				{
					OnAddFirstLayer( m_channelsController.AvailableChannels[ channelIdx ], layer );
				}
			}

			return layer;
		}

		public bool RemoveLayerFromChannel( int channelIdx, int layerIdx )
		{
			m_channelsController.RemoveLayerFromChannel( channelIdx, layerIdx );
			if( layerIdx == m_channelsController.CurrentLayerIdx )
			{
				return m_channelsController.UpdateCurrentLayerOnChannel( channelIdx, layerIdx );
			}
			return false;
		}

		public bool HasChannels()
		{
#if DEBUG_RENDER_TEXTURE_MODE
			return true;
#else
			return m_channelsController.ChannelCount > 0;
#endif
		}

		public bool HasLayerOnChannel( int channelIdx = -1 )
		{
#if DEBUG_RENDER_TEXTURE_MODE
			return true;
#else
			if( channelIdx < 0 )
				return m_channelsController.LayerCount() > 0;
			else
				return m_channelsController.AvailableChannels[ channelIdx ].Layers.Count > 0;
#endif
		}

		public void UpdateLayerValueOnChannel( int channelIdx = -1 )
		{
			if( m_channelsController.UpdateValue( channelIdx ) )
				FeedChannelToMaterial( channelIdx );
		}

		public bool IsLayerOnChannelLocked()
		{
#if DEBUG_RENDER_TEXTURE_MODE
			return false;
#else
			return ChannelsController.Locked;
#endif
		}

		public void FeedChannelToMaterial( int channelIdx = -1 )
		{
			APChannel channel = channelIdx < 0 ? m_channelsController.CurrentChannelData : m_channelsController.AvailableChannels[ channelIdx ];
			switch( m_currentShaderType )
			{
				default:
				case ShaderType.Custom:
				{
					if( m_propertiesController.CurrentMaterial != null )
					{
						m_propertiesController.CurrentMaterial.SetTexture( channel.Property.Name, channel.Value );
					}
				}
				break;
				case ShaderType.StandardMetallic:
				case ShaderType.StandardSpecular:
				{
					m_currentMaterial.SetTexture( channel.Template.PropertyName, channel.Value );
				}
				break;
			}
		}

		public void FeedAllChannelsToMaterial()
		{
			List<APChannel> availableChannels = m_channelsController.AvailableChannels;
			for( int i = 0; i < availableChannels.Count; i++ )
			{
				switch( m_currentShaderType )
				{
					default:
					case ShaderType.Custom:
					{
						if( m_propertiesController.CurrentMaterial != null )
						{
							m_propertiesController.CurrentMaterial.SetTexture( availableChannels[i].Property.Name, availableChannels[ i ].Value );
						}
					}
					break;
					case ShaderType.StandardMetallic:
					case ShaderType.StandardSpecular:
					{
						m_currentMaterial.SetTexture( availableChannels[ i ].Template.PropertyName, availableChannels[ i ].Value );
					}
					break;
				}
			}
		}

		public void UpdateChannelOnMaterial( int channelIdx )
		{
			APChannel channel = m_channelsController.AvailableChannels[ channelIdx ];
			string property = channel.Property.Name;
			if( m_propertiesController.CurrentMaterial != null && m_propertiesController.CurrentMaterial.HasProperty( property ) )
			{
				m_propertiesController.CurrentMaterial.SetTexture( property, channel.Value );
			}
			else
			{
				//TODO: Determine what to do when a custom channel is selected 
			}
		}

		public void ResetTexturesOnMaterial()
		{
			m_propertiesController.ResetTexturesOnMaterial();
		}

		public APChannelController ChannelsController { get { return m_channelsController; } }
		public APBrushController BrushController { get { return m_brushController; } }
		//public AmplifyPainterBrush CurrentBrush { get { return m_brushController.AvailableBrushes[ m_currentBrush ]; } }
		public APParentLayer CurrentLayer { get { return m_channelsController.CurrentLayer; } }

		public void SetCurrentLayerAndUpdateMaterial( int index )
		{
			m_channelsController.CurrentLayerIdx = index;
			FeedChannelToMaterial();
		}

		public int CurrentChannel { get { return m_channelsController.CurrentChannelIdx; } set { m_channelsController.CurrentChannelIdx = value; } }
		public APTexturePropertyController PropertiesContainer { get { return m_propertiesController; } }

		public Texture CurrentTexture
		{
			get
			{
#if DEBUG_RENDER_TEXTURE_MODE
				if( s_renderTex == null )
				{
					s_renderTex = AssetDatabase.LoadAssetAtPath<RenderTexture>( UnityEditor.AssetDatabase.GUIDToAssetPath( "5e505c3e72e91e546bcabcabd226c82c" ) );
				}

				return s_renderTex;
#else
				if( !HasChannels() )
					return null;

				APParentLayer layer = CurrentLayer;
				if( layer != null )
				{
					return layer.Value;
				}
				else
				{
					return null;
				}
#endif
			}
		}

#if DEBUG_RENDER_TEXTURE_MODE
		private static RenderTexture s_renderTex;
#endif

		public void DrawBrushToRenderTexture( APChannel channel, Vector3 center, Vector3 normal, Vector3 tangent, Vector2 texcoord, Camera camera, EditorWindow window, WindowType windowType )
		{
			if( m_currentMeshFilter == null )
				return;

			bool view2D = windowType == WindowType.Viewport2D;

			if( BrushController.CurrentBrush.Alignment == ProjectionAlignment.Camera && !view2D )
				normal = -camera.transform.forward;

			CommandBuffer cb = new CommandBuffer();
			cb.name = /*"Paint" +*/ channel.Name;

			CommandBuffer cbMask = new CommandBuffer();
			cbMask.name = /*"DilateMask" +*/ channel.Name;

#if DEBUG_RENDER_TEXTURE_MODE
			if( s_renderTex == null )
			{
				s_renderTex = AssetDatabase.LoadAssetAtPath<RenderTexture>( UnityEditor.AssetDatabase.GUIDToAssetPath( "5e505c3e72e91e546bcabcabd226c82c" ) );
			}

			cb.SetRenderTarget( s_renderTex );
#else
			APParentLayer layer = channel.GetCurrentLayer();
			if( layer != null )
			{
				cb.SetRenderTarget( layer.Value );
				if( m_channelsController.CurrentChannelData == channel && OnUpdateLayerView != null )
				{
					OnUpdateLayerView();
				}
			}
#endif

			bool renderMask = false;

			if( m_mask == null || !m_mask.IsCreated() || m_mask.width != layer.Value.width || m_mask.height != layer.Value.height )
			{
#if DEBUG_RENDER_TEXTURE_MODE
				m_mask = new RenderTexture( s_renderTex.width, s_renderTex.height, 0, RenderTextureFormat.R8 );
#else
				m_mask = new RenderTexture( layer.Value.width, layer.Value.height, 0, RenderTextureFormat.R8 );
#endif
				m_mask.Create();
				renderMask = true;
			}

			if( renderMask )
				cbMask.SetRenderTarget( m_mask );

			Transform obj = CurrentObject;
			if( windowType != WindowType.SceneView )
				obj = (window as APPainterViewport).ObjTransform;

			// Projection Matrix
			Quaternion rot = Quaternion.LookRotation( -obj.InverseTransformDirection( normal ), obj.InverseTransformDirection( view2D ? Vector3.Cross( -normal, tangent ) : camera.transform.up ) );
			Matrix4x4 m = Matrix4x4.TRS( obj.InverseTransformPoint( center ), rot, Vector3.one );
			cb.SetGlobalMatrix( BrushMatrixPropertyID, m.inverse );

			cb.SetGlobalVector( BrushProjSettingPropertyID, BrushController.GetProjectionSettingsVector() );
			cb.SetGlobalVector( BrushSettingsPropertyID, BrushController.GetBrushSettingsVector( true ) );
			if( CurrentTool == ToolType.Eraser )
				cb.SetGlobalFloat( BrushBlendOpID, 1 );
			else
				cb.SetGlobalFloat( BrushBlendOpID, 0 );

			if( BrushController.CurrentBrush.Alignment == ProjectionAlignment.UV )
			{
				cb.EnableShaderKeyword( UVModeKeyword );
				cb.SetGlobalVector( BrushMousePosPropertyID, texcoord );
			}
			else
			{
				cb.DisableShaderKeyword( UVModeKeyword );
			}

			RenderTexture layerTex = RenderTexture.GetTemporary( layer.Value.width, layer.Value.height, 0, layer.Value.graphicsFormat );
			Graphics.Blit( layer.Value, layerTex );
			cb.SetGlobalTexture( LayerTexPropertyID, layerTex );

			if( BrushController.CurrentBrush.GetTexture( channel.CurrentIdx ) != null )
			{
				switch( channel.Type )
				{
					case ChannelType.Color:
					cb.DisableShaderKeyword( IsNormalKeyword );
					break;
					case ChannelType.Normal:
					cb.EnableShaderKeyword( IsNormalKeyword );
					break;
				}
				cb.DisableShaderKeyword( DefaultBrushKeyword );
				cb.SetGlobalTexture( BrushTexPropertyID, BrushController.CurrentBrush.GetTexture( channel.CurrentIdx ) );
				cb.SetGlobalColor( BrushColorPropertyID, Color.white );
				//cb.SetGlobalVector( "_BrushTex_TexelSize", BrushController.GetTexelParamsVector() );
			}
			else
			{
				// TODO: remove keyword and use define
				cb.EnableShaderKeyword( DefaultBrushKeyword );
				cb.SetGlobalColor( BrushColorPropertyID, BrushController.CurrentBrush.GetColor( channel.CurrentIdx ) );
			}
			// TODO: remove keyword and use define
			cb.EnableShaderKeyword( FlatUVKeyword );
			BrushController.CurrentBrush.BrushMaterial.SetTexture( BrushMaskPropertyID, BrushController.CurrentBrush.Mask );
			// Looking at the object from its front
			Matrix4x4 V = Matrix4x4.LookAt( -Vector3.forward, Vector3.zero, Vector3.up ).inverse;

			// UV space ortho
			Matrix4x4 P = Matrix4x4.Ortho( 0, 1, 0, 1, 0, -1 );

			Matrix4x4 l2w = Matrix4x4.identity;

			cb.SetViewProjectionMatrices( V, P );
			cb.DrawMesh( m_currentMeshFilter.sharedMesh, l2w, BrushController.CurrentBrush.BrushMaterial, 0, 0 );

			if( renderMask )
			{
				Material maskMaterial = new Material( Shader.Find( "Hidden/AmplifyPainter/MaskDilate" ) )
				{
					hideFlags = HideFlags.HideAndDontSave 
				};
				cbMask.SetViewProjectionMatrices( V, P );
				cbMask.DrawMesh( m_currentMeshFilter.sharedMesh, l2w, maskMaterial, 0, 0 );
			}

			Graphics.ExecuteCommandBuffer( cb );
			cb.Release();
			cb = null;

			if( renderMask )
			{
				Graphics.ExecuteCommandBuffer( cbMask );
				cbMask.Release();
				cbMask = null;
				renderMask = false;
			}
			RenderTexture.ReleaseTemporary( layerTex );
			// disable keyword after use because CommandBuffer is not doing it by itself
			Shader.DisableKeyword( FlatUVKeyword );
		}

		public void DrawHandleProjectedTexture( Vector3 center, Vector3 normal, Vector3 tangent, Vector2 texcoord, Camera camera, EditorWindow window, WindowType windowType )
		{
			if( m_currentMeshFilter == null )
				return;

			bool view2D = windowType == WindowType.Viewport2D;

			if( BrushController.CurrentBrush.Alignment == ProjectionAlignment.Camera && !view2D )
				normal = -camera.transform.forward;

			if( BrushController.CurrentBrush.Alignment == ProjectionAlignment.UV )
			{
				Shader.EnableKeyword( UVModeKeyword );
				Shader.SetGlobalVector( BrushMousePosPropertyID, texcoord );
			}
			else
			{
				Shader.DisableKeyword( UVModeKeyword );
			}

			Quaternion rot = Quaternion.LookRotation( -normal, view2D ? Vector3.Cross( -normal, tangent ) : camera.transform.up );
			Matrix4x4 m = Matrix4x4.TRS( center, rot, Vector3.one );

			Shader.SetGlobalMatrix( BrushMatrixPropertyID, m.inverse );

			// Other settings
			Shader.SetGlobalVector( BrushProjSettingPropertyID, BrushController.GetProjectionSettingsVector() );
			Shader.SetGlobalVector( BrushSettingsPropertyID, BrushController.GetBrushSettingsVector( false ) );
			if( CurrentTool == ToolType.Eraser )
				Shader.SetGlobalFloat( BrushBlendOpID, 1 );
			else
				Shader.SetGlobalFloat( BrushBlendOpID, 0 );

			if( BrushController.CurrentBrush.GetFirstAllowedTexture() != null )
			{
				Shader.DisableKeyword( DefaultBrushKeyword );
				Shader.DisableKeyword( IsNormalKeyword );
				Shader.SetGlobalTexture( BrushTexPropertyID, BrushController.CurrentBrush.GetFirstAllowedTexture() );
				//Shader.SetGlobalVector( "_BrushTex_TexelSize", BrushController.GetTexelParamsVector() );
				Shader.SetGlobalColor( BrushColorPropertyID, Color.white );
			}
			else
			{
				// TODO: remove keyword and use define
				Shader.EnableKeyword( DefaultBrushKeyword );
				Shader.SetGlobalColor( BrushColorPropertyID, BrushController.CurrentBrush.GetFirstAllowedColor() );
			}

			BrushController.CurrentBrush.BrushMaterial.SetTexture( BrushMaskPropertyID, BrushController.CurrentBrush.Mask );

			if( view2D )
				Shader.EnableKeyword( FlatUVKeyword );
			else
				Shader.DisableKeyword( FlatUVKeyword );

			Transform obj = CurrentObject;
			if( windowType != WindowType.SceneView )
				obj = ( window as APPainterViewport ).ObjTransform;

			Matrix4x4 l2w = obj.localToWorldMatrix;

			// TODO: i don't like this, setupcamera shouldn't be a requirement, a bug might exist somewhere
			//Camera.SetupCurrent( camera );
			if( BrushController.CurrentBrush.BrushMaterial.SetPass( 1 ) )
				Graphics.DrawMeshNow( m_currentMeshFilter.sharedMesh, l2w, 0 );
			//Graphics.DrawMesh( m_currentMeshFilter.sharedMesh, l2w, BrushController.CurrentBrush.BrushMaterial, 0, camera, 0, null, false, false, false );
		}

		public bool HandleOrthogonalControls( Event current )
		{
			if( current.control && !IsPainting )
			{
				if( current.type == EventType.MouseDown )
				{
					m_initialMousePos = GUIUtility.GUIToScreenPoint( current.mousePosition );
				}

				if( current.delta.x * current.delta.x > current.delta.y * current.delta.y )
				{
					m_horizontalControl = true;
					m_verticalControl = false;
				}
				else if( current.delta.x * current.delta.x < current.delta.y * current.delta.y )
				{
					m_horizontalControl = false;
					m_verticalControl = true;
				}
				else
				{
					m_horizontalControl = false;
					m_verticalControl = false;
				}

				if( current.delta.magnitude > 0 )
				{
					if( m_warpDown )
						current.delta = new Vector2( current.delta.x, current.delta.y + Screen.height );
					else if( m_warpUp )
						current.delta = new Vector2( current.delta.x, current.delta.y - Screen.height );

					if( m_warpLeft )
						current.delta = new Vector2( current.delta.x + Screen.width, current.delta.y );
					else if( m_warpRight )
						current.delta = new Vector2( current.delta.x - Screen.width, current.delta.y );

					m_warpDown = false;
					m_warpUp = false;
					m_warpLeft = false;
					m_warpRight = false;

					Vector2 truePos = GUIUtility.GUIToScreenPoint( current.mousePosition );
					if( current.mousePosition.x > Screen.width )
					{
						APHelpers.SetCursorPos( (int)( truePos.x - Screen.width ), (int)( truePos.y ) );
						m_warpLeft = true;
					}
					if( current.mousePosition.x < 0 )
					{
						APHelpers.SetCursorPos( (int)( truePos.x + Screen.width ), (int)( truePos.y ) );
						m_warpRight = true;
					}

					if( current.mousePosition.y > Screen.height )
					{
						APHelpers.SetCursorPos( (int)( truePos.x ), (int)( truePos.y - Screen.height ) );
						m_warpDown = true;
					}
					else if( current.mousePosition.y < 0 )
					{
						APHelpers.SetCursorPos( (int)( truePos.x ), (int)( truePos.y + Screen.height ) );
						m_warpUp = true;
					}
				}

				float deltaMag = current.delta.magnitude;

				if( deltaMag > 0 && m_horizontalControl )
				{
					BrushController.CurrentBrush.Strength.Value += deltaMag * 0.005f * Mathf.Sign( current.delta.x );
					BrushController.CurrentBrush.Strength.Value = Mathf.Clamp01( BrushController.CurrentBrush.Strength.Value );
				}

				if( deltaMag > 0 && m_verticalControl )
				{
					BrushController.CurrentBrush.Rotation.Value += deltaMag * 0.005f * Mathf.Sign( current.delta.y ) * ( 180 / Mathf.PI );
				}

				if( deltaMag > 0 )
				{
					LockMouse = true;
				}
				current.Use();
				return true;
			}
			else
			{
				LockMouse = false;
				m_horizontalControl = false;
				m_verticalControl = false;
				return false;
			}
		}

		public void UnlockMouseControls()
		{
			if( LockMouse )
			{
				m_horizontalControl = false;
				m_verticalControl = false;
				LockMouse = false;
				APHelpers.SetCursorPos( (int)( m_initialMousePos.x ), (int)( m_initialMousePos.y ) );
			}
		}

		// TODO: this is here to remember me of changing how interpolation is calculated
		//float m_traveledDistance;
		//Vector3 m_previousPos;
		private void Paint( APChannel channel, Vector3 center, Vector3 normal, Vector3 tangent, Vector2 texcoord, Camera camera, Event current, EditorWindow window, WindowType windowType )
		{
			IsPainting = true;
			//Debug.Log( channel.Name );
			if( BrushController.CurrentBrush.Method == StrokeMethod.AirBrush )
				DrawBrushToRenderTexture( channel, center, normal, tangent, texcoord, camera, window, windowType );
			else
			{
				channel.BrushTraveledDistance += Vector3.Distance( channel.BrushPreviousPos, center );
				channel.BrushPreviousPos = center;
				//Debug.Log( channel.Name + " " + channel.BrushTraveledDistance );
				if( current.type == EventType.MouseDown || channel.m_holdPainting || channel.BrushTraveledDistance == 0 )
				{
					channel.m_holdPainting = false;
					channel.m_initialPos = center;
					channel.m_initialNormal = normal;
					channel.m_initialTangent = tangent;
					channel.m_initialTangent = texcoord;
					channel.m_initialMousePos2D = current.mousePosition;
					channel.BrushTraveledDistance = 0;
					BrushController.CalculatePressure();
					DrawBrushToRenderTexture( channel, center, normal, tangent, texcoord, camera, window, windowType );
				}
				else
				if( channel.BrushTraveledDistance >= BrushController.BrushDistance() )
				{
					//Debug.Log( channel.Name +" "+ channel.BrushPreviousPos );
					BrushController.StartPressureInterpolation();

					Vector3 truDist;
					Vector3 truNormal;
					Vector3 truTangent;
					Vector3 truTexcoord;
					Vector2 truMousePos;

					int i = 0;
					float accumDistance = 0;
					do
					{
						accumDistance += BrushController.BrushDistance();

						float interpolation = Mathf.InverseLerp( 0, channel.BrushTraveledDistance, accumDistance );

						// these need to be removed in favor of the ray cast (for now they serve as guesses)
						truDist = Vector3.LerpUnclamped( channel.m_initialPos, center, interpolation );
						truNormal = Vector3.LerpUnclamped( channel.m_initialNormal, normal, interpolation );
						truTangent = Vector3.LerpUnclamped( channel.m_initialTangent, tangent, interpolation );
						truMousePos = Vector2.LerpUnclamped( channel.m_initialMousePos2D, current.mousePosition, interpolation );
						truTexcoord = Vector2.LerpUnclamped( channel.m_initialTexcoord, texcoord, interpolation );

						Ray ray = new Ray( truDist + truNormal * 0.01f, -truNormal );
						if( windowType == WindowType.Viewport2D )
						{
							if( ( window as AP2DView ).Pencast( truMousePos, out PenCastHit penHit ) )
							{
								truDist = penHit.point;
								truNormal = penHit.normal;
								truTangent = penHit.tangent;
								truTexcoord = penHit.texcoord;
							}
						}
						else
						{
							if( Collider != null && Collider.Raycast( ray, out RaycastHit hit, 0.02f ) )
							{
								truDist = hit.point;
								truNormal = hit.BariNormal();
								truTangent = hit.BariTangent();
								truTexcoord = hit.BariTextCoord(0);
							}
						}
						
						BrushController.CalculatePressure( interpolation );
						DrawBrushToRenderTexture( channel, truDist, truNormal, truTangent, truTexcoord, camera, window, windowType );

						i++;
					} while( ( accumDistance + BrushController.BrushDistance() < channel.BrushTraveledDistance ) && ( i < 300 ) && ( BrushController.CurrentBrush.Size.InterpValue > 0.001 ) );

					channel.BrushTraveledDistance = 0;
					channel.m_initialPos = truDist;
					channel.BrushPreviousPos = truDist;
					channel.m_initialNormal = truNormal;
					channel.m_initialTangent = truTangent;
					channel.m_initialMousePos2D = truMousePos;
					channel.m_initialTexcoord = truTexcoord;

					BrushController.EndPressureInterpolation();
				}
			}
		}

		public void StartPainting( EditorWindow window, WindowType windowType, Event current, Camera camera, Ray ray )
		{
			bool canPaint = false;

			Vector3 point = Vector3.zero;
			Vector3 normal = Vector3.zero;
			Vector3 tangent = Vector3.zero;
			Vector2 texcoord = Vector2.zero;

			if( !IsLayerOnChannelLocked() )
			{
				if( windowType == WindowType.Viewport2D )
				{
					// Pencast
					if( ( window as AP2DView ).Pencast( current.mousePosition, out PenCastHit penHit ) )
					{
						point = penHit.point;
						normal = penHit.normal;
						tangent = penHit.tangent;
						texcoord = penHit.texcoord;
						canPaint = true;
						//DebugPointer( penHit.point, penHit.normal );
					}
				}
				else
				{
					// Raycast
					if( Collider != null && Collider.Raycast( ray, out RaycastHit hit, float.MaxValue ) )
					{
						point = hit.point;
						normal = hit.BariNormal();
						tangent = hit.BariTangent();
						texcoord = hit.BariTextCoord(0);
						canPaint = true;
						//DebugPointer( hit.point, hit.normal );
					}
				}
			}
			else
			{
				if( HasChannels() && !HasLayerOnChannel() )
				{
					if( EditorUtility.DisplayDialog( "New Layer", "Your current channel does not have any layers. Create one?", "Yes", "No" ) )
					{
						AddLayerToChannel( APLayerType.Default, -1, 0, true,true );
					}
				}
			}

			if( canPaint )
			{
				for( int i = 0; i < m_channelsController.ChannelCount; i++ )
				{
					if( m_channelsController.AvailableChannels[ i ].ValidToDraw &&
						m_brushController.CanPaintOnChannel( m_channelsController.AvailableChannels[ i ] ) )
					{
						Paint( m_channelsController.AvailableChannels[ i ], point, normal, tangent, texcoord, camera, current, window, windowType );
						UpdateLayerValueOnChannel( i );
					}
				}

			}
			else
			{
				// TODO: find better solution, painting should not be bound by failing to paint
				//HoldPainting();

				for( int i = 0; i < m_channelsController.ChannelCount; i++ )
				{
					if( m_channelsController.AvailableChannels[ i ].ValidToDraw &&
						m_brushController.CanPaintOnChannel( m_channelsController.AvailableChannels[ i ] ) )
					{
						HoldPainting( m_channelsController.AvailableChannels[ i ] );
					}
				}
			}
		}

		public void StopPainting()
		{
			if( IsPainting )
			{
				// Dilate result when painting is stopped
#if DEBUG_RENDER_TEXTURE_MODE
				s_renderTex.FastDilate( 32, m_mask ); // TODO: make the dilation editable?
#else
				for( int i = 0; i < m_channelsController.ChannelCount; i++ )
				{
					if( m_channelsController.AvailableChannels[ i ].ValidToDraw &&
						m_brushController.CanPaintOnChannel( m_channelsController.AvailableChannels[ i ] ) )
					{
						APParentLayer layer = m_channelsController.AvailableChannels[ i ].GetCurrentLayer();
						if( layer != null )
						{
							( (RenderTexture)layer.Value ).FastDilate( 32, m_mask ); // TODO: make the dilation editable?
						}
					}
				}
#endif
			}

			IsPainting = false;
		}

		public void HoldPainting( APChannel channel )
		{
			channel.m_holdPainting = true;
		}

		public void OpenColorPicker()
		{
			if( m_channelsController.ChannelCount > 0 )
			{
				int idx = m_channelsController.CurrentChannelIdx;
				ColorPickerEx.Show( ( x ) => { BrushController.CurrentBrush.ComponentsPerChannel[ idx ].Color = x; }, BrushController.CurrentBrush.ComponentsPerChannel[ idx ].Color, false, false );
			}
		}

		public void DrawChannelPreview( Camera camera, int channelIdx, EditorWindow window, WindowType windowType )
		{
			var channelTex = ChannelsController.AvailableChannels[channelIdx].Value;
			if( channelTex == null || !channelTex.IsCreated() )
				return;

			if( s_previewMaterial == null )
			{
				s_previewMaterial = new Material( Shader.Find( "Hidden/FlatMesh" ) )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			if( m_currentMaterial != null )
			{
				s_previewMaterial.renderQueue = m_currentMaterial.renderQueue + 1;
			}
			else
			{
				s_previewMaterial.renderQueue = m_propertiesController.CurrentMaterial.renderQueue + 1;
			}
			
			// TODO: Temporary solution, it shouldn't require setting keywords on and off
			if( windowType == WindowType.Viewport2D )
				Shader.EnableKeyword( FlatUVKeyword );
			else
				Shader.DisableKeyword( FlatUVKeyword );

			s_previewMaterial.SetTexture( MainTexPropertyID , channelTex );

			Transform obj = CurrentObject;
			if( windowType != WindowType.SceneView )
				obj = ( window as APPainterViewport ).ObjTransform;

			Matrix4x4 l2w = obj.localToWorldMatrix;

			Graphics.DrawMesh( m_currentMeshFilter.sharedMesh, l2w, s_previewMaterial, 0, camera, 0, null, false, false, false );
		}

		public void ResetMaterialOnObject()
		{
			if( m_currentObject != null && m_currentMeshRenderer != null && m_originalMaterial != null )
			{
				m_currentMeshRenderer.sharedMaterial = m_originalMaterial;
				EditorPrefs.DeleteKey( m_currentObject.GetInstanceID().ToString() );
			}

			m_originalMaterial = null;

			if( m_currentMaterial != null )
			{
				DestroyImmediate( m_currentMaterial );
				m_currentMaterial = null;
			}

			m_currentMeshFilter = null;
		}

		public void SetOriginalMaterial()
		{
			if( m_currentMeshRenderer != null && m_originalMaterial != null )
			{
				m_currentMeshRenderer.sharedMaterial = m_originalMaterial;
			}
		}

		public void SetAPMaterial()
		{
			if( m_currentMeshRenderer != null && m_currentMaterial != null )
			{
				m_currentMeshRenderer.sharedMaterial = m_currentMaterial;
			}
		}

		public void GetInfoFromObject( Transform newObject, bool refreshData, Action ActivatePreviewReset )
		{
			if( newObject == null )
				return;

			if( refreshData && m_currentObject != newObject )
			{
				ResetMaterialOnObject();

				m_currentMeshFilter = newObject.GetComponent<MeshFilter>();
				m_currentMeshRenderer = newObject.GetComponent<MeshRenderer>();
				if( m_currentMeshRenderer != null )
				{
					if( m_currentMeshRenderer.sharedMaterial == null )
					{
						string meshId = newObject.GetInstanceID().ToString();
						if( EditorPrefs.HasKey( meshId ) )
						{
							string matGUID = EditorPrefs.GetString( meshId );
							Material mat = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( matGUID ) );
							m_currentMeshRenderer.sharedMaterial = mat;
						}
					}

					m_currentObject = newObject;

#if !DEBUG_RENDER_TEXTURE_MODE
					PropertiesContainer.SetProperties( m_currentMeshRenderer.sharedMaterial );
#endif
					ActivatePreviewReset();
					ResetChannels();
					Shader shader = m_currentMeshRenderer.sharedMaterial.shader;
					m_currentShaderType = APAvailableTemplatesContainer.AvailableTemplates.ContainsKey( shader.name ) ?
											APAvailableTemplatesContainer.AvailableTemplates[ shader.name ].Type :
											ShaderType.Custom;
					if( OnUpdateInfoFromObjectEvent != null )
					{
						OnUpdateInfoFromObjectEvent();
					}

					if( m_currentShaderType != ShaderType.Custom )
					{
						m_originalMaterial = m_currentMeshRenderer.sharedMaterial;
						if( m_currentShaderType == ShaderType.StandardMetallic )
							m_currentMaterial = new Material( APResources.SurfaceShader );
						else
							m_currentMaterial = new Material( APResources.SurfaceShaderSpec );

#if UNITY_EDITOR
						m_currentMaterial.CopyPropertiesFrom( m_originalMaterial );
#endif
						
						if( m_currentShaderType != ShaderType.Custom )
						{
							if( m_originalMaterial.GetTexture( "_BumpMap" ) == null )
								m_currentMaterial.EnableKeyword("_NORMALMAP");
							if( m_originalMaterial.GetTexture( "_EmissionMap" ) == null )
							{
								m_currentMaterial.EnableKeyword( "_EMISSION" );
								m_currentMaterial.SetColor( "_EmissionColor", Color.white );
							}
						}
						m_currentMaterial.hideFlags = HideFlags.HideAndDontSave;
						m_currentMeshRenderer.sharedMaterial = m_currentMaterial;

						for( int i = 0; i < APAvailableTemplatesContainer.AvailableTemplates[ shader.name ].Channels.Count; i++ )
						{
							AddChannel( APAvailableTemplatesContainer.AvailableTemplates[ shader.name ].Channels[ i ] );
						}

					}
					else
					{
						if( APGeneralOptions.AddFirstChannelOnSelection )
						{
							APTextureProperty property = m_propertiesController.AvailableProperties.Count > 0 ? m_propertiesController.AvailableProperties[ 0 ] : null;
							APChannel channel = AddChannel( property );
							if( APGeneralOptions.AddFirstLayerOnSelection )
							{
								AddLayerToChannel( APLayerType.Default, 0, 0, true,true );
							}
						}
					}

					if( OnFinishedUpdateInfoFromObjectEvent != null )
					{
						OnFinishedUpdateInfoFromObjectEvent();
					}
				}
			}
		}

		public void DeleteColliders()
		{
			//if( m_colliders != null )
			//	foreach( Collider col in m_colliders )
			//		DestroyImmediate( col );

			if( m_collider != null )
				DestroyImmediate( m_collider );
		}

		public void SetColliders()
		{
			//Transform[] trs = Selection.transforms;

			//if( APPainterViewport.Instance != null )
			//	trs = APPainterViewport.Instance.AllTransforms;

			//if( trs == null )
			//	return;

			//DeleteColliders();

			//foreach( var item in trs )
			//{
			//	MeshFilter mf = item.GetComponent<MeshFilter>();
			//	if( mf == null )
			//		continue;

			//	MeshCollider mc = item.gameObject.AddComponent<MeshCollider>();
			//	mc.hideFlags = HideFlags.HideAndDontSave;
			//	mc.hideFlags |= HideFlags.HideInInspector;

			//	if( mf.sharedMesh != null )
			//	{
			//		mc.sharedMesh = mf.sharedMesh;
			//	}

			//	m_colliders.Add( mc );
			//}

			GameObject go = Selection.activeGameObject;

			if( APPainterViewport.Instance != null && EditorWindow.focusedWindow is APPainterViewport )
				go = APPainterViewport.Instance.Obj;

			if( go == null )
				return;

			DeleteColliders();

			MeshFilter mf = go.GetComponent<MeshFilter>();
			if( mf == null )
				return;

			MeshCollider mc = go.AddComponent<MeshCollider>();
			if( mc != null )
			{
				mc.hideFlags = HideFlags.HideAndDontSave;
				mc.hideFlags |= HideFlags.HideInInspector;

				if( mf.sharedMesh != null )
				{
					mc.sharedMesh = mf.sharedMesh;
				}

				m_collider = mc;
			}
		}

		//public List<MeshCollider> Colliders { get { return m_colliders; } set { m_colliders = value; } }
		public MeshCollider Collider { get { return m_collider; } set { m_collider = value; } }
		public Transform CurrentObject { get { return m_currentObject; } }
		public MeshRenderer CurrentMeshRenderer { get { return m_currentMeshRenderer; } }
		public ShaderType CurrentShaderType { get { return m_currentShaderType; } }
		public Material CurrentMaterial { get { return m_currentMaterial; } }
		public Material OriginalMaterial { get { return m_originalMaterial; } }
	}
}
