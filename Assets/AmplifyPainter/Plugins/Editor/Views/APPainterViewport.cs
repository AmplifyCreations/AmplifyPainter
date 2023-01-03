// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AmplifyPainter
{
	public struct PenCastHit
	{
		public Vector3 point;
		public Vector3 normal;
		public Vector3 tangent;
		public Vector2 texcoord;
		public Ray ray;
	}

	internal class PreviewScene : IDisposable
	{
		private readonly Scene m_scene;
		private readonly List<GameObject> m_gameObjects = new List<GameObject>();
		private readonly Camera m_camera;

		public PreviewScene( string sceneName )
		{
			m_scene = EditorSceneManager.NewPreviewScene();
			if( !m_scene.IsValid() )
				throw new InvalidOperationException( "Preview scene could not be created" );

			m_scene.name = sceneName;

			var camGO = EditorUtility.CreateGameObjectWithHideFlags( "Preview Scene Camera", HideFlags.HideAndDontSave, typeof( Camera ) );
			AddGameObject( camGO );
			m_camera = camGO.GetComponent<Camera>();
			camera.cameraType = CameraType.Preview;
			camera.enabled = false;
			camera.clearFlags = CameraClearFlags.Depth;
			camera.fieldOfView = 15;
			camera.farClipPlane = 10.0f;
			camera.nearClipPlane = 2.0f;
			camera.renderingPath = RenderingPath.Forward;
			camera.useOcclusionCulling = false;
			camera.scene = m_scene;
		}

		public Camera camera
		{
			get { return m_camera; }
		}

		public Scene scene
		{
			get { return m_scene; }
		}

		public void AddGameObject( GameObject go )
		{
			if( m_gameObjects.Contains( go ) )
				return;

			SceneManager.MoveGameObjectToScene( go, m_scene );
			m_gameObjects.Add( go );
		}

		public void AddManagedGO( GameObject go )
		{
			SceneManager.MoveGameObjectToScene( go, m_scene );
		}

		public void Dispose()
		{
			EditorSceneManager.ClosePreviewScene( m_scene );

			foreach( var go in m_gameObjects )
				UnityEngine.Object.DestroyImmediate( go );

			m_gameObjects.Clear();
		}
	}

	public class APPainterViewport : APParentWindow
	{
		public static readonly Vector3 View2DOffset = new Vector3( -0.5f, -0.5f, 0 );
		public static APPainterViewport Instance;

		[SerializeField]
		private Vector3 m_camPivot = Vector3.zero;

		[SerializeField]
		private Quaternion m_camRotation = Quaternion.identity;

		[SerializeField]
		private float m_camZoom = 3f;

		[SerializeField]
		private float m_camZoomOrtho = 0.5f + 0.05f;

		[SerializeField]
		private bool m_rebakeProbe = false;

		[SerializeField]
		private Cubemap m_customHDRI;

		[SerializeField]
		private Cubemap m_cubeMap;

		[SerializeField]
		private bool m_useAntialiasing = true;

		[SerializeField]
		protected bool m_is3DView = true;

		private Light m_mainLight;
		private GameObject m_obj;

		private Camera m_camera;
		public Camera Camera { get { return m_camera; } }

		private static Material s_proceduralSkybox = null;
		private static Material s_cubemapSkybox = null;

		public static event Action<APPainterViewport> duringSceneGui;

		//public Transform[] AllTransforms { get { return m_obj == null ? null : new Transform[] { m_obj.transform }; } }
		public Transform ObjTransform { get { return m_obj == null ? null : m_obj.transform; } }
		public GameObject Obj { get { return m_obj; } }

		private static Material s_guiTextureBlitSceneGUI;

		private static Material GUITextureBlitSceneGUIMaterial
		{
			get
			{
				if( !s_guiTextureBlitSceneGUI )
				{
					Shader shader = EditorGUIUtility.LoadRequired( "SceneView/GUITextureBlitSceneGUI.shader" ) as Shader;
					s_guiTextureBlitSceneGUI = new Material( shader )
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return s_guiTextureBlitSceneGUI;
			}
		}

		private PreviewScene m_PreviewScene;
		//public Color ambientColor { get; set; }

		private RenderTexture m_renderTexture;

		internal static class Styles
		{
			public static GUIContent sceneViewCameraContent = EditorGUIUtility.TrIconContent( "SceneViewCamera", "Display Options for this view" );
		}

		private Vector3 m_sunRotation = new Vector3( 50, -30, 0 );

		private static readonly Color s_backgroundColor = new Color( 0.2156863f, 0.2156863f, 0.2156863f );
		private static readonly Color s_squareColor = new Color( 0.35f, 0.35f, 0.35f );

		public override void OnEnable()
		{
			base.OnEnable();

			this.wantsMouseEnterLeaveWindow = true;

			EditorApplication.update -= UpdateRenderRepaint;
			EditorApplication.update += UpdateRenderRepaint;

			//Selection.selectionChanged += ChangeSelectedObject;
			m_controller.OnFinishedUpdateInfoFromObjectEvent += ChangeSelectedObject;
			Instance = this;
			EditorTool et = EditorToolsContextEx.GetCurrentTool();
			if( et is APEditorTool )
			{
				( et as APEditorTool ).Init( false );
			}
		}

		public override void OnDisable()
		{
			base.OnDisable();

			//Selection.selectionChanged -= ChangeSelectedObject;
			m_controller.OnFinishedUpdateInfoFromObjectEvent -= ChangeSelectedObject;
			EditorApplication.update -= UpdateRenderRepaint;
			if( m_PreviewScene != null )
			{
				m_PreviewScene.Dispose();
				m_PreviewScene = null;
			}

			if( m_renderTexture != null )
				DestroyImmediate( m_renderTexture );
			m_renderTexture = null;

			if( s_proceduralSkybox != null )
				DestroyImmediate( s_proceduralSkybox );
			s_proceduralSkybox = null;

			if( s_cubemapSkybox != null )
				DestroyImmediate( s_cubemapSkybox );
			s_cubemapSkybox = null;

			if( m_camera != null )
				DestroyImmediate( m_camera.gameObject );

			if( m_obj != null )
				DestroyImmediate( m_obj );

			if( m_mainLight != null )
				DestroyImmediate( m_mainLight.gameObject );

			if( m_copyMesh != null )
				DestroyImmediate( m_copyMesh );

			Instance = null;
		}

		private void OnFocus()
		{
			Instance = this;

			if( m_controller != null )
				m_controller.SetColliders();
		}

		private void OnLostFocus()
		{
			UnityEngine.Cursor.visible = true;
		}

		private void UpdateRenderRepaint()
		{
			Repaint();
		}

		private Mesh m_copyMesh;

		private void ChangeSelectedObject()
		{
			if( Selection.activeGameObject == null )
				return;

			if( m_obj != null )
				DestroyImmediate( m_obj );

			if( m_copyMesh != null )
				DestroyImmediate( m_copyMesh );

			var meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();
			var meshRenderer = Selection.activeGameObject.GetComponent<MeshRenderer>();

			if( meshFilter == null || meshRenderer == null )
				return;

			if( m_PreviewScene == null )
				return;

			if( m_is3DView )
			{
				m_obj = new GameObject( "Preview Object" );
				var mf = m_obj.AddComponent<MeshFilter>();
				mf.sharedMesh = meshFilter.sharedMesh;
				var mr = m_obj.AddComponent<MeshRenderer>();
				mr.sharedMaterials = meshRenderer.sharedMaterials;

				m_obj.transform.localPosition = Vector3.zero;
				m_obj.transform.localRotation = Quaternion.identity;
				m_obj.transform.localScale = Vector3.one;
				m_obj.hideFlags = HideFlags.HideAndDontSave;
				m_PreviewScene.AddGameObject( m_obj );
			}
			else
			{
				m_obj = new GameObject( "Preview Object" );
				var mf = m_obj.AddComponent<MeshFilter>();
				mf.sharedMesh = meshFilter.sharedMesh;
				var mr = m_obj.AddComponent<MeshRenderer>();
				mr.material = new Material( Shader.Find( "Hidden/FlatMesh" ) )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				var mc = m_obj.AddComponent<MeshCollider>();

				m_copyMesh = new Mesh();
				var mesh = meshFilter.sharedMesh;

				List<Vector3> allVerts = new List<Vector3>( mesh.vertices );
				List<Vector3> allNormals = new List<Vector3>( mesh.normals );
				List<Vector3> allUVs = new List<Vector3>( mesh.vertices.Length );

				mesh.GetUVs( 0, allUVs );
				m_copyMesh.vertices = allUVs.ToArray();
				m_copyMesh.name = "copy";
				m_copyMesh.normals = mesh.normals;
				m_copyMesh.tangents = mesh.tangents;
				m_copyMesh.uv = mesh.uv;
				m_copyMesh.SetUVs( 1, allVerts );
				m_copyMesh.SetUVs( 2, allNormals );
				m_copyMesh.triangles = mesh.triangles;
				m_copyMesh.hideFlags = HideFlags.HideAndDontSave;
				m_copyMesh.RecalculateBounds();
				m_copyMesh.UploadMeshData( false );
				mc.sharedMesh = m_copyMesh;

				m_obj.transform.localPosition = View2DOffset;
				m_obj.transform.localRotation = Quaternion.identity;
				m_obj.transform.localScale = Vector3.one;

				m_obj.hideFlags = HideFlags.HideAndDontSave;
				m_PreviewScene.AddGameObject( m_obj );
			}

			m_controller.SetColliders();
		}

		public override void OnToolChangedReset()
		{
			if( m_is3DView )
			{
				if( m_obj != null && Selection.activeGameObject != null )
				{
					MeshRenderer originalRenderer = Selection.activeGameObject.GetComponent<MeshRenderer>();
					MeshRenderer mr = m_obj.GetComponent<MeshRenderer>();
					if( mr != null && originalRenderer != null )
					{
						mr.sharedMaterials = originalRenderer.sharedMaterials;
					}
				}
			}
		}

		private void OnGUI()
		{
			Instance = this;

			if( Selection.activeGameObject == null )
				return;

			if( s_proceduralSkybox == null )
			{
				s_proceduralSkybox = new Material( Shader.Find( "Skybox/Procedural" ) )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			if( s_cubemapSkybox == null )
			{
				s_cubemapSkybox = new Material( Shader.Find( "Skybox/Cubemap" ) )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			if( m_PreviewScene == null )
			{
				m_PreviewScene = new PreviewScene( "Preview Scene" );

				GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags( "PreRenderLight", HideFlags.HideAndDontSave, typeof( Light ) );
				m_mainLight = lightGO.GetComponent<Light>();
				m_mainLight.type = LightType.Directional;
				m_mainLight.intensity = 1.0f;
				m_mainLight.enabled = false;
				m_mainLight.color = new Color( 0.769f, 0.769f, 0.769f, 1 );
				m_mainLight.transform.rotation = Quaternion.Euler( m_sunRotation );
				m_mainLight.shadows = LightShadows.Soft;
				m_PreviewScene.AddGameObject( lightGO );

				m_camera = m_PreviewScene.camera;
				m_camera.clearFlags = m_is3DView ? CameraClearFlags.Skybox : CameraClearFlags.Depth;
				m_camera.backgroundColor = new Color( 0.2156863f, 0.2156863f, 0.2156863f );
				m_camera.transform.rotation = m_camRotation;
				m_camera.transform.position = m_camPivot - m_camera.transform.forward * m_camZoom;
				m_camera.cameraType = CameraType.SceneView;
				m_camera.farClipPlane = 1000;
				m_camera.nearClipPlane = 0.01f;
				m_camera.orthographic = false;
				m_camera.orthographicSize = m_camZoomOrtho;
				m_camera.fieldOfView = 60;
				m_camera.orthographic = !m_is3DView;

				ChangeSelectedObject();

				m_cubeMap = new Cubemap( 128, m_camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR, TextureCreationFlags.MipChain )
				{
					hideFlags = HideFlags.HideAndDontSave
				};

				m_rebakeProbe = true;
			}

			BeginWindows();

			//// TOOLBAR START ////////////////////////////
			GUILayout.BeginHorizontal( EditorStyles.toolbar );
			{
				GUILayout.FlexibleSpace();
				if( DisplayOptionsButton( out Rect displayButton ) )
				{
					UnityEditor.PopupWindow.Show( displayButton, new DisplayOptions( this ) );
					GUIUtility.ExitGUI();
				}
			}
			GUILayout.EndHorizontal();
			//// TOOLBAR END ////////////////////////////

			if( m_obj == null )
				return;

			Event current = Event.current;
			Rect viewport = new Rect( 0, 0, this.position.width, this.position.height );
			Rect guiRect = new Rect( 0, 17, this.position.width, this.position.height - 17 );

			Rect windowSpaceCameraRect = guiRect;
			Rect groupSpaceCameraRect = new Rect( 0, 0, windowSpaceCameraRect.width, windowSpaceCameraRect.height );

			//// CONTROLS START ////////////////////////////
			bool cameraChanged = false;
			var zoom = m_is3DView ? m_camZoom : m_camZoomOrtho;
			var drag = current.delta;

			if( current.type == EventType.KeyDown && current.keyCode == KeyCode.F )
			{
				if( current.alt )
					m_camera.transform.rotation = Quaternion.identity;

				Bounds b = m_obj.GetComponent<MeshRenderer>().bounds;

				zoom = m_is3DView ? Mathf.Max( b.size.x, b.size.y, b.size.z ) + 0.1f : 0.5f + 0.05f;

				m_camPivot = m_is3DView ? b.center : Vector3.zero;
				cameraChanged = true;
			}

			if( current.type == EventType.MouseDrag )
			{
				// pan camera
				if( current.button == 2 || ( current.button == 0 && current.alt && current.control ) )
				{
					m_camera.transform.Translate( -drag.x * 0.002f * zoom, drag.y * 0.002f * zoom, 0, Space.Self );
					m_camPivot = m_camera.transform.position + m_camera.transform.forward * zoom;
				}

				// rotate camera
				if( current.alt && current.button == 0 && !current.control )
				{
					if( m_is3DView )
					{
						m_camera.transform.RotateAround( m_camPivot, Vector3.up, drag.x * 0.5f );
						m_camera.transform.RotateAround( m_camPivot, m_camera.transform.right, drag.y * 0.5f );
					}
					else
					{
						m_camera.transform.RotateAround( m_camPivot, m_camera.transform.forward, ( -drag.y - drag.x ) * 0.5f );
					}
				}

				// zoom camera
				if( current.alt && current.button == 1 )
				{
					zoom += ( -drag.y - drag.x ) * 0.01f;
					zoom = Mathf.Clamp( zoom, 0.05f, 5000 );
					cameraChanged = true;
				}

				// rotate sun
				if( current.shift && current.button == 1 )
				{
					m_sunRotation = m_mainLight.transform.eulerAngles;
					m_sunRotation.x -= drag.y;
					m_sunRotation.y += drag.x;
					m_mainLight.transform.rotation = Quaternion.Euler( m_sunRotation );
					m_rebakeProbe = true;
				}
			}

			// zoom camera scroll wheel
			if( current.isScrollWheel && !current.control )
			{
				float unclamped = drag.y * zoom * zoom * 0.01f;
				zoom += Mathf.Clamp( Mathf.Abs( unclamped ), 0.03f, 3 ) * Mathf.Sign( unclamped );
				zoom = Mathf.Clamp( zoom, 0.05f, 5000 );
				cameraChanged = true;
			}

			if( cameraChanged )
			{
				m_camera.transform.position = m_camPivot - m_camera.transform.forward * zoom;
				m_camera.orthographicSize = zoom;

				if( m_is3DView )
					m_camZoom = zoom;
				else
					m_camZoomOrtho = zoom;
			}
			m_camRotation = m_camera.transform.rotation;
			//// CONTROLS END ////////////////////////////

			GUI.BeginGroup( windowSpaceCameraRect );
			if( current.type == EventType.Repaint )
			{
				Texture defaultEnvTexture = ReflectionProbe.defaultTexture;

				if( Unsupported.SetOverrideLightingSettings( m_PreviewScene.scene ) )
				{
					RenderSettings.ambientMode = AmbientMode.Flat;
					//RenderSettings.ambientLight = ambientColor;

					RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
					RenderSettings.customReflection = defaultEnvTexture as Cubemap;
				}

				int rtWidth = (int)( windowSpaceCameraRect.width );
				int rtHeight = (int)( windowSpaceCameraRect.height );

				if( m_renderTexture == null )
				{
					GraphicsFormat format = m_camera.allowHDR ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
					m_renderTexture = new RenderTexture( 0, 0, 24, format )
					{
						name = "3D view RT",
						antiAliasing = m_useAntialiasing ? 8 : 1,
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				if( m_renderTexture.width != rtWidth || m_renderTexture.height != rtHeight )
				{
					m_renderTexture.Release();
					m_renderTexture.width = rtWidth;
					m_renderTexture.height = rtHeight;
					m_renderTexture.Create();
					m_camera.targetTexture = m_renderTexture;
				}

				m_mainLight.enabled = true;

				RenderSettings.sun = m_mainLight;
				if( m_customHDRI != null )
				{
					s_cubemapSkybox.SetTexture( "_Tex", m_customHDRI );
					RenderSettings.skybox = s_cubemapSkybox;
				}
				else
				{
					RenderSettings.skybox = s_proceduralSkybox;
				}
				RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
				RenderSettings.customReflection = null;

				if( m_rebakeProbe )
				{
					// turn it to skybox temporarily to bake it, this prevents issues when baking scenes
					RenderSettings.ambientMode = AmbientMode.Skybox;
					DynamicGI.UpdateEnvironment();

					// render skybox to cubemap
					m_obj.SetActive( false );
					var savePos = m_camera.transform.position;
					m_camera.transform.position = m_obj.transform.position;
					m_camera.RenderToCubemap( m_cubeMap );
					m_camera.transform.position = savePos;
					m_rebakeProbe = false;
					m_obj.SetActive( true );
				}

				SphericalHarmonicsL2 l2 = RenderSettings.ambientProbe * 1;
				RenderSettings.ambientProbe = l2;
				RenderSettings.ambientMode = AmbientMode.Custom;

				RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
				RenderSettings.customReflection = m_cubeMap;

				//// RENDER START //////////////
				var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
				Unsupported.useScriptableRenderPipeline = true;
				if( m_is3DView )
				{
					m_camera.Render();
				}
				else
				{
					var temp = RenderTexture.active;
					RenderTexture.active = m_renderTexture;
					GL.Clear( true, true, s_backgroundColor );
					RenderTexture.active = temp;

					var mr = m_obj.GetComponent<MeshRenderer>();
					mr.sharedMaterial.SetTexture( "_MainTex", m_controller.CurrentTexture );
					Shader.EnableKeyword( "_FlatUV" );
					m_camera.Render();
					Shader.DisableKeyword( "_FlatUV" );
				}
				Unsupported.useScriptableRenderPipeline = oldAllowPipes;
				//// RENDER END //////////////

				Unsupported.RestoreOverrideLightingSettings();
				m_mainLight.enabled = false;

				Rect asd = new Rect( 0, 0, m_renderTexture.width, m_renderTexture.height );

				GUI.DrawTexture( asd, m_renderTexture, ScaleMode.StretchToFill, false );
			}
			UnityEngine.Camera.SetupCurrent( Camera );
			GUI.BeginClip( groupSpaceCameraRect, Vector2.zero, Vector2.zero, true );
			if( current.type == EventType.Repaint )
			{
				Graphics.SetRenderTarget( m_camera.targetTexture );
				GL.Clear( false, true, new Color( 0, 0, 0, 0 ) );
			}

			if( duringSceneGui != null )
			{
				if( duringSceneGui != null )
				{
					HandlesEx.ClearHandles();
					duringSceneGui( this );
				}
			}

			if( current.type == EventType.Repaint )
				Graphics.SetRenderTarget( null );
			GUI.EndClip();
			UnityEngine.Camera.SetupCurrent( null );

			GUI.EndGroup();

			GUI.BeginGroup( windowSpaceCameraRect );
			if( current.type == EventType.Repaint && m_camera.targetTexture != null )
			{
				Graphics.DrawTexture( groupSpaceCameraRect, m_camera.targetTexture, new Rect( 0, 0, 1, 1 ), 0, 0, 0, 0, GUI.color, GUITextureBlitSceneGUIMaterial );
			}
			GUI.EndGroup();

			EndWindows();
		}

		private bool DisplayOptionsButton( out Rect buttonRect )
		{
			buttonRect = GUILayoutUtility.GetRect( Styles.sceneViewCameraContent, EditorStyles.toolbarDropDown );
			if( EditorGUI.DropdownButton( buttonRect, Styles.sceneViewCameraContent, FocusType.Passive, EditorStyles.toolbarDropDown ) )
				return true;

			return false;
		}

		internal class DisplayOptions : PopupWindowContent
		{
			private readonly APPainterViewport m_view;

			public DisplayOptions( APPainterViewport view )
			{
				m_view = view;
			}

			public override Vector2 GetWindowSize()
			{
				return new Vector2( 200, EditorGUIUtility.singleLineHeight * ( 7 /*+ 6 + 4*/ ) );
			}

			public override void OnGUI( Rect rc )
			{
				EditorGUILayout.LabelField( "Display Options", EditorStyles.boldLabel );
				EditorGUILayout.Space();
				float cache = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 100;
				EditorGUI.BeginChangeCheck();
				m_view.m_customHDRI = EditorGUILayout.ObjectField( "Custom HDRI", m_view.m_customHDRI, typeof( Cubemap ), true ) as Cubemap;
				if( EditorGUI.EndChangeCheck() )
				{
					m_view.m_rebakeProbe = true;
				}

				EditorGUI.BeginChangeCheck();
				m_view.m_useAntialiasing = EditorGUILayout.Toggle( "Use Antialiasing", m_view.m_useAntialiasing );
				if( EditorGUI.EndChangeCheck() )
				{
					m_view.m_renderTexture.Release();
					if( m_view.m_renderTexture != null )
						DestroyImmediate( m_view.m_renderTexture );
					m_view.m_renderTexture = null;
				}

				//m_view.m_camera = EditorGUILayout.ObjectField( "", m_view.m_camera, typeof( Camera ), true ) as Camera;
				//m_view.m_mainLight = EditorGUILayout.ObjectField( "", m_view.m_mainLight, typeof( Light ), true ) as Light;
				//m_view.m_obj = EditorGUILayout.ObjectField( "", m_view.m_obj, typeof( GameObject ), true ) as GameObject;
				//m_proceduralSkybox = EditorGUILayout.ObjectField( "", m_proceduralSkybox, typeof( Material ), true ) as Material;
				//m_view.m_cubeMap = EditorGUILayout.ObjectField( "", m_view.m_cubeMap, typeof( Cubemap ), true ) as Cubemap;
				EditorGUIUtility.labelWidth = cache;
			}
		}

		private static Material s_quadMat;

		private void DrawSquare()
		{
			if( s_quadMat == null )
			{
				s_quadMat = new Material( Shader.Find( "Hidden/Internal-Colored" ) )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			GL.PushMatrix();
			s_quadMat.SetPass( 0 );
			GL.LoadProjectionMatrix( Camera.projectionMatrix );
			GL.MultMatrix( Camera.worldToCameraMatrix );

			GL.Begin( GL.TRIANGLE_STRIP );
			GL.Color( s_squareColor );
			GL.Vertex3( -0.5f, -0.5f, 0 );
			GL.Vertex3( -0.5f, 0.5f, 0 );
			GL.Vertex3( 0.5f, -0.5f, 0 );
			GL.Vertex3( 0.5f, 0.5f, 0 );

			GL.End();
			GL.PopMatrix();
		}

		public bool Pencast( Vector2 mousePosition, out PenCastHit penhit, float maxDistance = float.MaxValue )
		{
			penhit = new PenCastHit();

			MeshCollider mc = m_obj.GetComponent<MeshCollider>();

			Vector2 screenPixelPos = mousePosition;
			// half pixel off seems to help to align the circle to the actual painting, maybe there's some pixel precision in some other areas
			screenPixelPos.y = m_camera.pixelRect.height - mousePosition.y + 0.5f;
			screenPixelPos.x -= 0.5f;

			Ray ray = m_camera.ScreenPointToRay( screenPixelPos );
			Vector3 origin = ray.origin;
			origin.z = -10;
			ray.origin = origin;

			penhit.ray = ray;

			if( mc.Raycast( ray, out RaycastHit hit, maxDistance ) )
			{
				Transform tr = m_obj.transform;
				penhit.point = tr.TransformPoint( hit.BariTextCoord( 1 ) );
				penhit.normal = tr.TransformDirection( hit.BariTextCoord( 2 ) );
				penhit.tangent = tr.TransformDirection( hit.BariTangent() );
				penhit.texcoord = hit.BariTextCoord( 0 );
				//DebugPointer( penhit.point, penhit.tangent );

				return true;
			}
			return false;
		}

		public static void DebugPointer( Vector3 point, Vector3 normal )
		{
			GameObject go = GameObject.Find( "pointer" );
			if( go == null )
			{
				go = new GameObject( "pointer"/*, typeof( gizmoDraw )*/ );
			}
			go.transform.position = point;
			go.transform.up = normal;
		}
	}
}
