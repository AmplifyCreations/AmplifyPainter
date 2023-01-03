using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AmplifyPainter
{
	public sealed class APTextureExport : APParentWindow
	{
		[MenuItem( "Window/Amplify Painter/Texture Export "+ EXPORT, priority = 1010 )]
		public static APTextureExport OpenAmplifyPainterExportWindow()
		{
			var window = OpenOrCloseWindow<APTextureExport>( false, null );
			if( window )
				window.titleContent = new GUIContent( "Texture Export" );

			window.minSize = new Vector2( 300, 100 );
			return window;
		}

		ScrollView m_textureList;

		public override void OnEnable()
		{
			base.OnEnable();

			VisualElement root = rootVisualElement;
			m_textureList = new ScrollView() { };
			rootVisualElement.styleSheets.Add( APResources.MainStyleSheet );
			rootVisualElement.name = "ExportUI";

			VisualElement group = new VisualElement();
			group.style.flexDirection = FlexDirection.Row;
			TextField tf = new TextField();
			tf.style.flexGrow = 1;

			SerializedObject so = new SerializedObject( m_controller );
			tf.BindProperty( so.FindProperty( "ProjectPath" ) );

			Button browse = new Button( () => { ChangeProjectPath(); } );
			browse.text = "Browse";

			group.Add( browse );
			group.Add( tf );

			OnChannelsUpdated();

			m_controller.OnChannelsUpdated += OnChannelsUpdated;
			m_controller.OnUpdateInfoFromObjectEvent += OnChannelsUpdated;

			VisualElement footer = new VisualElement();
			footer.name = "Footer";
			Button but = new Button( () => { ExportAll(); } );
			but.text = "Export All";
			footer.Add( but );
			root.Add( group );
			root.Add( m_textureList );
			root.Add( footer );
		}

		public override void OnDisable()
		{
			base.OnDisable();

			m_controller.OnChannelsUpdated -= OnChannelsUpdated;
			m_controller.OnUpdateInfoFromObjectEvent -= OnChannelsUpdated;
		}

		void OnChannelsUpdated()
		{
			switch( m_controller.CurrentShaderType )
			{
				default:
				case ShaderType.Custom:
				{
					var channels = m_controller.ChannelsController.AvailableChannels;

					m_textureList.Clear();

					for( int i = 0; i < channels.Count; i++ )
					{
						VisualElement group = new VisualElement();
						group.style.flexDirection = FlexDirection.Row;
						TextField tf = new TextField() { value = channels[ i ].Property.FileName };
						Label label = new Label( GenerateFilename( channels[ i ].Property, channels[ i ].Property.FileName ) );
						tf.style.width = 150;

						object[] arguments = new object[] { label, channels[ i ].Property };
						tf.RegisterCallback<ChangeEvent<string>, object[]>( UpdateName, arguments );
						SerializedObject so = new SerializedObject( channels[ i ].Property );
						tf.BindProperty( so.FindProperty( "FileName" ) );
						group.Add( tf );
						label.style.flexGrow = 1;
						group.Add( label );

						var channelIndex = i;
						Button but = new Button( () => { ExportAsTexture( channelIndex ); } );
						but.text = "Export";
						group.Add( but );

						m_textureList.Add( group );
					}
				}
				break;
				case ShaderType.StandardMetallic:
				case ShaderType.StandardSpecular:
				{
					Dictionary<ShaderPropertyType, List<APChannelTemplateItem>> channels = APAvailableTemplatesContainer.AvailableTemplates[ APAvailableTemplatesContainer.ShaderTypeToKey[ m_controller.CurrentShaderType ] ].Dict;
					m_textureList.Clear();
					int index = 0;
					foreach( KeyValuePair<ShaderPropertyType, List<APChannelTemplateItem>> kvp in channels )
					{
						APTextureProperty property = m_controller.PropertiesContainer.FindProperty( kvp.Key );
						VisualElement group = new VisualElement();
						group.style.flexDirection = FlexDirection.Row;
						TextField tf = new TextField() { value = property.FileName };
						Label label = new Label( GenerateFilename( property, property.FileName ) );
						tf.style.width = 150;

						object[] arguments = new object[] { label, property };
						tf.RegisterCallback<ChangeEvent<string>, object[]>( UpdateName, arguments );
						SerializedObject so = new SerializedObject( property );
						tf.BindProperty( so.FindProperty( "FileName" ) );
						group.Add( tf );
						label.style.flexGrow = 1;
						group.Add( label );

						ShaderPropertyType propertyType = kvp.Key;
						Button but = new Button( () => { ExportTemplate( index, propertyType ); } );
						but.text = "Export";
						group.Add( but );

						m_textureList.Add( group );
						index++;
					}

				}
				break;
			}
		}

		void ExportAll()
		{
			switch( m_controller.CurrentShaderType )
			{
				default:
				case ShaderType.Custom:
				{
					var channels = m_controller.ChannelsController.AvailableChannels;
					for( int i = 0; i < channels.Count; i++ )
					{
						var channelIndex = i;
						ExportAsTexture( channelIndex, false );
					}
				}
				break;
				case ShaderType.StandardMetallic:
				case ShaderType.StandardSpecular:
				{
					Dictionary<ShaderPropertyType, List<APChannelTemplateItem>> items = APAvailableTemplatesContainer.AvailableTemplates[ APAvailableTemplatesContainer.ShaderTypeToKey[ m_controller.CurrentShaderType ] ].Dict;
					foreach( KeyValuePair<ShaderPropertyType, List<APChannelTemplateItem>> kvp in items )
					{
						if( kvp.Value.Count > 1 )
						{
							int passId = Mathf.Clamp( kvp.Value.Count, 2, 4 ) - 2;
							for( int i = 0; i < kvp.Value.Count; i++ )
							{
								APChannel channel = m_controller.ChannelsController.GetChannelByName( kvp.Value[ i ].Name );
								APResources.CreateChannelsMaterial.SetVector( APAvailableTemplatesContainer.MaskNames[ i ], kvp.Value[ i ].ColorMask );
								APResources.CreateChannelsMaterial.SetTexture( APAvailableTemplatesContainer.TexNames[ i ], channel.Value );
							}
							RenderTexture temp = RenderTexture.GetTemporary( m_controller.ChannelsController.DefaultWidth, m_controller.ChannelsController.DefaultHeight, 0, m_controller.ChannelsController.DefaultFormat );
							Graphics.Blit( null, temp, APResources.CreateChannelsMaterial, passId );
							ExportAsTexture( kvp.Value[ 0 ], temp );
							temp.Release();
							RenderTexture.ReleaseTemporary( temp );
						}
						else
						{
							APChannel channel = m_controller.ChannelsController.GetChannelByName( kvp.Value[ 0 ].Name );
							ExportAsTexture( kvp.Value[ 0 ], channel.Value );
						}
					}
				}
				break;
			}
		}

		void ChangeProjectPath()
		{
			if( string.IsNullOrEmpty( m_controller.ProjectPath ) )
				m_controller.ProjectPath = Application.dataPath;
			m_controller.ProjectPath = EditorUtility.OpenFolderPanel( "Folder Path", m_controller.ProjectPath, "" );
		}

		void UpdateName( ChangeEvent<string> e, object[] arguments )
		{
			Label label = arguments[ 0 ] as Label;
			APTextureProperty prop = arguments[ 1 ] as APTextureProperty;
			label.text = GenerateFilename( prop, e.newValue );
		}

		string GenerateFilename( APTextureProperty prop, string newValue )
		{
			string fileName = newValue;
			fileName = fileName.Replace( "$property", prop.Description );

			return fileName + ".png";
		}

		void ExportAsTexture( int index, bool prompt = true )
		{
			if( m_controller.ChannelsController.AvailableChannels.Count <= index || m_controller.ChannelsController.AvailableChannels[ index ] == null )
				return;

			var info = m_controller.ChannelsController.AvailableChannels[ index ].Property;
			var t = m_controller.ChannelsController.AvailableChannels[ index ].Value;

			string fileName = info.FileName;
			fileName = fileName.Replace( "$property", info.Description );

			string path = m_controller.ProjectPath;
			if( prompt )
				path = EditorUtility.SaveFilePanel( "Save texture as PNG", m_controller.ProjectPath, fileName + ".png", "png" );
			else
				path += "/" + GenerateFilename( info, fileName );

			Debug.Log( path );

			if( string.IsNullOrEmpty( path ) )
				return;

			Texture2D outfile = new Texture2D( t.width, t.height, TextureFormat.ARGB32, true );

			RenderTexture temp = RenderTexture.active;
			RenderTexture.active = t;
			outfile.ReadPixels( new Rect( 0, 0, t.width, t.height ), 0, 0 );
			RenderTexture.active = temp;

			var pngData = outfile.EncodeToPNG();
			System.IO.File.WriteAllBytes( path, pngData );

			AssetDatabase.Refresh();
		}

		void ExportAsTexture( APChannelTemplateItem item, RenderTexture rt, bool prompt = false )
		{
			APTextureProperty info = m_controller.PropertiesContainer.FindProperty( item.Property );
			string fileName = info.FileName;
			fileName = fileName.Replace( "$property", info.Description );

			string path = m_controller.ProjectPath.Length > 0 ? m_controller.ProjectPath : Application.dataPath;
			if( prompt )
				path = EditorUtility.SaveFilePanel( "Save texture as PNG", m_controller.ProjectPath, fileName + ".png", "png" );
			else
				path += "/" + GenerateFilename( info, fileName );

			Debug.Log( path );

			if( string.IsNullOrEmpty( path ) )
				return;

			Texture2D outfile = new Texture2D( rt.width, rt.height, TextureFormat.ARGB32, true );

			RenderTexture temp = RenderTexture.active;
			RenderTexture.active = rt;
			outfile.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), 0, 0 );
			RenderTexture.active = temp;

			var pngData = outfile.EncodeToPNG();
			System.IO.File.WriteAllBytes( path, pngData );

			AssetDatabase.Refresh();
		}

		void ExportTemplate( int index, ShaderPropertyType property, bool prompt = false )
		{
			APTextureProperty info = m_controller.PropertiesContainer.FindProperty( property );

			bool releaseRT = true;
			RenderTexture rt = null;
			Dictionary<ShaderPropertyType, List<APChannelTemplateItem>> items = APAvailableTemplatesContainer.AvailableTemplates[ APAvailableTemplatesContainer.ShaderTypeToKey[ m_controller.CurrentShaderType ] ].Dict;
			if( items[ property ].Count > 1 )
			{
				int passId = Mathf.Clamp( items[ property ].Count, 2, 4 ) - 2;
				for( int i = 0; i < items[ property ].Count; i++ )
				{
					APChannel channel = m_controller.ChannelsController.GetChannelByName( items[ property ][ i ].Name );
					APResources.CreateChannelsMaterial.SetVector( APAvailableTemplatesContainer.MaskNames[ i ], items[ property ][ i ].ColorMask );
					APResources.CreateChannelsMaterial.SetTexture( APAvailableTemplatesContainer.TexNames[ i ], channel.Value );
				}
				rt = RenderTexture.GetTemporary( m_controller.ChannelsController.DefaultWidth, m_controller.ChannelsController.DefaultHeight, 0, m_controller.ChannelsController.DefaultFormat );
				Graphics.Blit( null, rt, APResources.CreateChannelsMaterial, passId );

			}
			else
			{
				APChannel channel = m_controller.ChannelsController.GetChannelByName( items[ property ][ 0 ].Name );
				releaseRT = false;
			}


			string fileName = info.FileName;
			fileName = fileName.Replace( "$property", info.Description );

			string path = m_controller.ProjectPath.Length > 0 ? m_controller.ProjectPath : Application.dataPath;
			if( prompt )
				path = EditorUtility.SaveFilePanel( "Save texture as PNG", m_controller.ProjectPath, fileName + ".png", "png" );
			else
				path += "/" + GenerateFilename( info, fileName );


			if( string.IsNullOrEmpty( path ) )
				return;

			Texture2D outfile = new Texture2D( rt.width, rt.height, TextureFormat.ARGB32, true );

			RenderTexture temp = RenderTexture.active;
			RenderTexture.active = rt;
			outfile.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), 0, 0 );
			RenderTexture.active = temp;

			var pngData = outfile.EncodeToPNG();
			System.IO.File.WriteAllBytes( path, pngData );

			AssetDatabase.Refresh();
			if( releaseRT )
			{
				RenderTexture.ReleaseTemporary( rt );
			}
		}
	}
}
