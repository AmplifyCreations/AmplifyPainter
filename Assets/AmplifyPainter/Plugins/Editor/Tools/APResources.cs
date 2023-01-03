// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmplifyPainter
{
	public class APResources
	{
		private const string CopyColorGUID = "4615d25c770b36c40884dace98a14be7";
		private static Material m_copyColorMaterial;
		public static Material CopyColorMaterial
		{
			get
			{
				if( m_copyColorMaterial == null )
				{
					m_copyColorMaterial = new Material( AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( CopyColorGUID ) ) )
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return m_copyColorMaterial;
			}
		}
		public static int CopyColorProperty;

		private const string CreateChannelsGUID = "e004138b99bb74941b295c009c751c47";
		private static Material m_createChannelsMaterial;
		public static Material CreateChannelsMaterial
		{
			get
			{
				if( m_createChannelsMaterial == null )
				{
					m_createChannelsMaterial = new Material( AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( CreateChannelsGUID ) ) )
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return m_createChannelsMaterial;
			}
		}

		private const string LayerOpsGUID = "661c47a806c4c5642b6a3b5e549fd5f0";
		public static Shader LayerOpsShader;

		private const string SurfaceShaderGUID = "a3366369ea41959429c0797204299d90";
		public static Shader SurfaceShader;

		private const string SurfaceShaderSpecGUID = "897aace79b7e0944b9b89cf88da1b460";
		public static Shader SurfaceShaderSpec;

		private const string ToolIconGUID = "b6947cb1272934c46bb245f3ab912a18";
		public static Texture2D ToolIconTexture;

		private const string MainStylesGUID = "95cd2ebe25dcb0e479a216794926e098";
		public static StyleSheet MainStyleSheet;

		private const string LightStylesGUID = "2f8150d0a0177ec4e8e39f52b08dac6e";
		public static StyleSheet LightStyleSheet;

		private const string LayerBackgroundGUID = "913241a6102a2eb4b9c4e5db6698c5a0";
		public static Texture2D LayerBackgroundTexture;

		private const string AddLayerIconGUID = "c3d4a07d944b9ad46a6714889c94e35f";
		public static Texture2D AddLayerIconTexture;

		private const string AddSMORTLayerIconGUID = "8d5ead94c9921074b8c7859f2a9f5a56";
		public static Texture2D AddSMORTLayerIconTexture;

		private const string SMORTLayerIconGUID = "9a2360477cc5ade4aba6f0d46d44d4ff";
		public static Texture2D SMORTLayerIconTexture;

		private const string RemoveLayerIconGUID = "5139d06ba2755c348829204a9cb61b67";
		public static Texture2D RemoveLayerIconTexture;

		private const string KnobBackGUID = "f7a55f35b6302e947bf346892863aab4";
		public static Texture2D KnobBackTexture;

		private const string KnobGUID = "e8fdc53a030f028448a9820bdf7fbbd6";
		public static Texture2D KnobTexture;

		private const string DilateGUID = "11eb4d139f8fbf541805efbfc11e2f03";
		public static Shader DilateShader;

		private const string DotGUID = "dedd80c4bdf822b4aa4cd6fc41ba8945";
		public static Texture2D DotTexture;

		private const string BlendModesGUID = "3dad30fc0eb08c540baf092e05800857";
		public static Material BlendModesMaterial;

		private const string GuassianTextureGUID = "154fc3615bb5caa4f9ead237c2cb370c";
		public static Texture2D GuassianTexture;

		private const string BrushIconGUID = "921624a4e974ec845b10cc41b2112425";
		public static GUIContent BrushIcon;

		private const string EraserIconGUID = "2161f95126cb4d9469ad5dcc797a0b97";
		public static GUIContent EraserIcon;

		private const string View2DWindowIconGUID = "b411e9021fa97f54bb30c05568300c6f";
		public static GUIContent View2DWindowIcon;

		private const string View3DWindowIconGUID = "f51ddc2bc551d3248a07a478630af9f8";
		public static GUIContent View3DWindowIcon;

		private const string BrushesWindowIconGUID = "0869e40ad6613584aa4695c620792637";
		public static GUIContent BrushesWindowIcon;

		private const string ChannelsWindowIconGUID = "b5b6017b79699f249814fdc4ceb43954";
		public static GUIContent ChannelsWindowIcon;

		private const string LayersWindowIconGUID = "6cb892cdfff309e43b66027e69670919";
		public static GUIContent LayersWindowIcon;

		private const string TextureExportWindowIconGUID = "98b3ce412e40cf04b8b30ebc2ad60abe";
		public static GUIContent TextureExportWindowIcon;

		private static bool m_initialized = false;

		private static Dictionary<string, List<APParentLayer>> m_externalResources = new Dictionary<string, List<APParentLayer>>();
		private static List<string> m_GUIDs = new List<string>()
		{
			ToolIconGUID,
			MainStylesGUID ,
			LayerBackgroundGUID,
			AddLayerIconGUID,
			AddSMORTLayerIconGUID,
			RemoveLayerIconGUID,
			KnobBackGUID,
			KnobGUID,
			DilateGUID ,
			DotGUID,
			BlendModesGUID
		};

		public static bool CheckNativeResources( string assetPath )
		{
			string guid = AssetDatabase.AssetPathToGUID( assetPath );
			bool foundResource = m_GUIDs.Contains( guid );
			m_initialized = !foundResource;
			return foundResource;
		}

		public static void LoadResources()
		{
			if( m_initialized && MainStyleSheet != null )
				return;

			ToolIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( ToolIconGUID ) );
			MainStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>( AssetDatabase.GUIDToAssetPath( MainStylesGUID ) );
			LightStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>( AssetDatabase.GUIDToAssetPath( LightStylesGUID ) );
			LayerBackgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( LayerBackgroundGUID ) );
			AddLayerIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( AddLayerIconGUID ) );
			AddSMORTLayerIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( AddSMORTLayerIconGUID ) );
			SMORTLayerIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( SMORTLayerIconGUID ) );
			RemoveLayerIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( RemoveLayerIconGUID ) );
			KnobBackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( KnobBackGUID ) );
			KnobTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( KnobGUID ) );
			DilateShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( DilateGUID ) );
			DotTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( DotGUID ) );
			BlendModesMaterial = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( BlendModesGUID ) );
			SurfaceShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( SurfaceShaderGUID ) );
			SurfaceShaderSpec = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( SurfaceShaderSpecGUID ) );
			LayerOpsShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( LayerOpsGUID ) );
			GuassianTexture = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( GuassianTextureGUID ) );
			BrushIcon = new GUIContent( string.Empty,AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( BrushIconGUID ) ) , "Select painting tool");
			EraserIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( EraserIconGUID ) ),"Select eraser tool" );
			View2DWindowIcon = new GUIContent( string.Empty,AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( View2DWindowIconGUID ) ),"Open 2D View window" );
			View3DWindowIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( View3DWindowIconGUID ) ), "Open 3D View window" );
			BrushesWindowIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( BrushesWindowIconGUID ) ), "Open Brushes window" );
			ChannelsWindowIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( ChannelsWindowIconGUID ) ), "Open Channels window" );
			LayersWindowIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( LayersWindowIconGUID ) ) ,"Open Layers window" );
			TextureExportWindowIcon = new GUIContent( string.Empty, AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( TextureExportWindowIconGUID ) ) , "Open Texture Export window" );
			CopyColorProperty = Shader.PropertyToID( "_Color" );

			m_initialized = true;
		}

		public static void UnloadResources()
		{
			Resources.UnloadAsset( ToolIconTexture );
			ToolIconTexture = null;

			Resources.UnloadAsset( MainStyleSheet );
			MainStyleSheet = null;

			Resources.UnloadAsset( LightStyleSheet );
			LightStyleSheet = null;

			Resources.UnloadAsset( LayerBackgroundTexture );
			LayerBackgroundTexture = null;

			Resources.UnloadAsset( AddLayerIconTexture );
			AddLayerIconTexture = null;

			Resources.UnloadAsset( AddSMORTLayerIconTexture );
			AddSMORTLayerIconTexture = null;

			Resources.UnloadAsset( SMORTLayerIconTexture );
			SMORTLayerIconTexture = null;

			Resources.UnloadAsset( RemoveLayerIconTexture );
			RemoveLayerIconTexture = null;

			Resources.UnloadAsset( KnobBackTexture );
			KnobBackTexture = null;

			Resources.UnloadAsset( KnobTexture );
			KnobTexture = null;

			Resources.UnloadAsset( DilateShader );
			DilateShader = null;

			Resources.UnloadAsset( DotTexture );
			DotTexture = null;

			Resources.UnloadAsset( BlendModesMaterial );
			BlendModesMaterial = null;

			Resources.UnloadAsset( SurfaceShader );
			SurfaceShader = null;

			Resources.UnloadAsset( SurfaceShaderSpec );
			SurfaceShaderSpec = null;

			GameObject.DestroyImmediate( m_copyColorMaterial );
			m_copyColorMaterial = null;

			Resources.UnloadAsset( LayerOpsShader );
			LayerOpsShader = null;

			Resources.UnloadAsset( GuassianTexture );
			GuassianTexture = null;

			Resources.UnloadAsset( BrushIcon.image );
			BrushIcon = null;

			Resources.UnloadAsset(EraserIcon.image );
			EraserIcon = null;

			Resources.UnloadAsset(View2DWindowIcon.image );
			View2DWindowIcon = null;

			Resources.UnloadAsset(View3DWindowIcon.image );
			View3DWindowIcon = null;

			Resources.UnloadAsset(BrushesWindowIcon.image );
			BrushesWindowIcon = null;

			Resources.UnloadAsset( ChannelsWindowIcon.image );
			ChannelsWindowIcon = null;

			Resources.UnloadAsset( LayersWindowIcon.image );
			LayersWindowIcon = null;

			Resources.UnloadAsset( TextureExportWindowIcon.image );
			TextureExportWindowIcon = null;
			
			GameObject.DestroyImmediate( m_copyColorMaterial );
			m_copyColorMaterial = null;
		}

		public static void RegisterExternalResource( UnityEngine.Object resource, APParentLayer layer )
		{
			string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( resource ) );
			if( m_externalResources.ContainsKey( guid ) )
			{
				m_externalResources[ guid ].Add( layer );
			}
			else
			{
				m_externalResources.Add( guid, new List<APParentLayer> { layer } );
			}
		}

		public static void ReleaseExternalResource( UnityEngine.Object resource, APParentLayer layer )
		{
			string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( resource ) );
			if( m_externalResources.ContainsKey( guid ) )
			{
				m_externalResources[ guid ].Remove( layer );
			}
		}

		public static bool CheckExternalResource( string assetPath )
		{
			string guid = AssetDatabase.AssetPathToGUID( assetPath );
			if( m_externalResources.ContainsKey( guid ) && m_externalResources[ guid ].Count > 0 )
			{
				for( int i = 0; i < m_externalResources[ guid ].Count; i++ )
				{
					m_externalResources[ guid ][ i ].RefreshValue();
				}
				return true;
			}

			return false;
		}

	}
}
