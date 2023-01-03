// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APChannelsWindow : APParentWindow
	{
		private const string WindowLabel = "Channels";

		//private List<Toggle> m_materialsTextureList = new List<Toggle>();
		//private ScrollView m_materialTexturesAnchor;
		private PopupField<int> m_widthField;
		private PopupField<int> m_heightField;

		private VisualElement m_heightFieldAnchor;
		private PopupField<int> m_heightFieldDummy;
		private Toggle m_lockSize;

		private ScrollView m_channelsAnchor;
		private List<APTextureProperty> m_texturePropertyOptions;

		private VisualElement m_channelsHeader;
		private Button m_addCustomChannelButton;

		[MenuItem( "Window/Amplify Painter/Channels " + CHANNELS, priority = 1001 )]
		public static APChannelsWindow OpenAmplifyPainterChannelsWindow()
		{
			APChannelsWindow window = OpenOrCloseWindow<APChannelsWindow>( false, null );
			if( window )
				window.titleContent = new GUIContent( WindowLabel );
			return window;
		}

		public static APChannelsWindow ShowAmplifyPainterWindow()
		{
			APChannelsWindow window = GetWindow<APChannelsWindow>( false );
			window.titleContent = new GUIContent( WindowLabel );
			return window;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			minSize = new Vector2( 200, 150 );

			m_controller.OnUpdateInfoFromObjectEvent += UpdateFromProperties;
			m_controller.ChannelsController.OnChannelAdded += OnAddChannelEvent;
			m_controller.ChannelsController.OnChannelRemoved += OnRemoveChannelEvent;

			VisualElement root = rootVisualElement;
			VisualElement miscData = new VisualElement() { name = "MainUI" };
			miscData.AddToClassList( "container" );

			miscData.styleSheets.Add( APResources.MainStyleSheet );

			// Layers Size

			SerializedObject channelsControllerObj = new SerializedObject( m_controller.ChannelsController );
			VisualElement layerSizeAnchor = new VisualElement();
			layerSizeAnchor.name = "LayerSizeAnchor";
			layerSizeAnchor.style.flexDirection = FlexDirection.Row;
			Label layerSizeLabel = new Label( "Size" );
			layerSizeLabel.name = "LayerSizeLabel";
			layerSizeAnchor.Add( layerSizeLabel );

			SerializedProperty widthProperty = channelsControllerObj.FindProperty( "m_defaultWidth" );
			m_widthField = new PopupField<int>( APChannelController.AvailableSizes, 0 );
			m_widthField.BindProperty( widthProperty );
			m_widthField.RegisterValueChangedCallback( OnSizeChanged );
			layerSizeAnchor.Add( m_widthField );

			m_lockSize = new Toggle();
			SerializedProperty m_lockedRatioProperty = channelsControllerObj.FindProperty( "m_lockedRatio" );
			m_lockSize.BindProperty( m_lockedRatioProperty );
			layerSizeAnchor.Add( m_lockSize );
			m_lockSize.RegisterValueChangedCallback( OnLockToogleSelected );

			m_heightFieldAnchor = new VisualElement();
			m_heightFieldAnchor.name = "LayerHeightAnchor";
			SerializedProperty heightProperty = channelsControllerObj.FindProperty( "m_defaultHeight" );
			m_heightField = new PopupField<int>( APChannelController.AvailableSizes, 0 );
			m_heightField.BindProperty( heightProperty );
			m_heightField.RegisterValueChangedCallback( OnSizeChanged );
			
			m_heightFieldDummy = new PopupField<int>( APChannelController.AvailableSizes, 0 );
			// This bind is on purpose, to show this widget when height is locked, thus its equal to width
			// It will always be disabled
			m_heightFieldDummy.BindProperty( widthProperty );
			m_heightFieldDummy.SetEnabled( false );

			m_heightFieldAnchor.Add( m_lockedRatioProperty.boolValue ? m_heightFieldDummy : m_heightField );
			layerSizeAnchor.Add( m_heightFieldAnchor );

			miscData.Add( layerSizeAnchor );

			//Material Textures
			//miscData.Add( new Label( "Material Textures" ) );
			//m_materialTexturesAnchor = new ScrollView();
			//m_materialTexturesAnchor.style.backgroundColor = new StyleColor( new Color( 0.3f, 0.3f, 0.3f, 1.0f ) );
			//miscData.Add( m_materialTexturesAnchor );

			//Custom Textures
			//miscData.Add( new Label( "Custom Textures" ) );
			m_channelsHeader = new VisualElement();
			m_channelsHeader.name = "ChannelItemHeader";
			m_channelsHeader.style.flexDirection = FlexDirection.Row;

			Label nameLabel = new Label( "Name" );
			nameLabel.name = "ChannelItemNameLabel";
			m_channelsHeader.Add( nameLabel );

			Label typeLabel = new Label( "Type" );
			typeLabel.name = "ChannelItemTypeLabel";
			m_channelsHeader.Add( typeLabel );

			Label propertiesLabel = new Label( "Properties" );
			propertiesLabel.name = "ChannelItemPropertiesLabel";
			m_channelsHeader.Add( propertiesLabel );

			m_addCustomChannelButton = new Button( OnButtonAddChannel );
			m_addCustomChannelButton.text = "+";
			m_addCustomChannelButton.name = "ChannelItemAddButton";
			m_channelsHeader.Add( m_addCustomChannelButton );

			//Button removeCustomChannelButton = new Button( OnRemoveChannel );
			//removeCustomChannelButton.text = "-";
			//m_channelsHeader.Add( removeCustomChannelButton );

			miscData.Add( m_channelsHeader );
			m_channelsAnchor = new ScrollView();

			miscData.Add( m_channelsAnchor );

			root.Add( miscData );

			APTextureProperty customProperty = CreateInstance<APTextureProperty>();
			customProperty.Name = "<Custom>";
			m_texturePropertyOptions = new List<APTextureProperty>() { customProperty };

			UpdateFromProperties();
			UpdateAvailableChannels();
		}

		void OnLockToogleSelected( ChangeEvent<bool> eventCall )
		{
			Toggle toggle = eventCall.target as Toggle;
			m_heightFieldAnchor.Clear();
			m_heightFieldAnchor.Add( toggle.value ? m_heightFieldDummy : m_heightField );
			// If width != height then a size change will happen with this toggle action
			if( m_heightField.value != m_widthField.value )
			{
				m_controller.ChannelsController.RefreshLayerSize();
				m_controller.FeedAllChannelsToMaterial();
			}
		}

		void OnSizeChanged( ChangeEvent<int> eventCall )
		{
			m_controller.ChannelsController.RefreshLayerSize();
			m_controller.FeedAllChannelsToMaterial();
		}

		public override void OnDisable()
		{
			base.OnDisable();
			m_controller.OnUpdateInfoFromObjectEvent -= UpdateFromProperties;
			m_controller.ChannelsController.OnChannelAdded -= OnAddChannelEvent;
			m_controller.ChannelsController.OnChannelRemoved -= OnRemoveChannelEvent;
		}



		public override void OnDestroy()
		{
			base.OnDestroy();
			if( m_texturePropertyOptions != null )
			{
				DestroyImmediate( m_texturePropertyOptions[ 0 ] );
				m_texturePropertyOptions.Clear();
				m_texturePropertyOptions = null;
			}
			//for( int i = 0; i < m_materialsTextureList.Count; i++ )
			//{
			//	m_materialsTextureList[ i ].UnregisterValueChangedCallback( OnToggleSelected );
			//}

			//m_materialTexturesAnchor.Clear();
			//m_materialTexturesAnchor = null;

			//m_materialsTextureList.Clear();
			//m_materialsTextureList = null;

			m_channelsAnchor.Clear();
			m_channelsAnchor = null;

			rootVisualElement.Clear();
		}

		void UpdateAvailableChannels()
		{
			List<APChannel> channels = m_controller.ChannelsController.AvailableChannels;
			for( int i = 0; i < channels.Count; i++ )
			{
				if( !channels[ i ].FromProperty )
				{
					APChannelsItemView channelItem = new APChannelsItemView( new SerializedObject( channels[ i ] ), m_channelsAnchor.childCount, m_texturePropertyOptions, m_controller.CurrentShaderType );
					channelItem.OnChannelInfoChangedEvent += OnChannelDataUpdated;
					channelItem.OnChannelRemovedEvent += OnButtonChannelRemoved;
					channelItem.Name.value = channels[ i ].Name;
					m_channelsAnchor.Add( channelItem );
				}
			}
		}

		void OnButtonAddChannel()
		{
			m_controller.AddChannel();
		}

		void OnAddChannelEvent( int idx, APChannel channel )
		{
			APChannelsItemView channelItem = new APChannelsItemView( new SerializedObject( channel ), m_channelsAnchor.childCount, m_texturePropertyOptions, m_controller.CurrentShaderType );
			channelItem.OnChannelInfoChangedEvent += OnChannelDataUpdated;
			channelItem.OnChannelRemovedEvent += OnButtonChannelRemoved;
			channelItem.Name.value = channel.Name;
			m_channelsAnchor.Add( channelItem );
		}


		//void OnRemoveChannel()
		//{
		//	if( m_controller.RemoveChannel() )
		//	{
		//		int index = m_channelsAnchor.childCount - 1;
		//		APChannelsItemView item = m_channelsAnchor.ElementAt( index ) as APChannelsItemView;
		//		item.Destroy();
		//		m_channelsAnchor.RemoveAt( index );
		//	}
		//}

		void OnButtonChannelRemoved( int idx )
		{
			m_controller.RemoveChannel( idx );
		}

		void OnRemoveChannelEvent( int idx, APChannel channel )
		{
			APChannelsItemView item = m_channelsAnchor.ElementAt( idx ) as APChannelsItemView;
			item.Destroy();
			m_channelsAnchor.RemoveAt( idx );
		}

		void OnChannelDataUpdated( int idx )
		{
			m_controller.ChannelsController.FireDataChangedNotification( idx );
			m_controller.BrushController.UpdateChannelInfoOnBrush( m_controller.ChannelsController.AvailableChannels[ idx ] );
		}

		//public void OnToggleSelected( ChangeEvent<bool> eventCall )
		//{
		//	Toggle toggle = eventCall.target as Toggle;
		//	m_controller.UpdateChannels( toggle.userData as APTextureProperty, toggle.value );
		//}

		public override void OnToolChangedReset()
		{
			//for( int i = 0; i < m_materialsTextureList.Count; i++ )
			//{
			//	m_materialsTextureList[ i ].UnregisterValueChangedCallback( OnToggleSelected );
			//}
			//m_materialsTextureList.Clear();
			//m_materialTexturesAnchor.Clear();
			foreach( APChannelsItemView channelItem in m_channelsAnchor.Children() )
			{
				channelItem.Destroy();
			}
			m_channelsAnchor.Clear();
		}

		void UpdateFromProperties()
		{
			m_addCustomChannelButton.SetEnabled( m_controller.CurrentShaderType == ShaderType.Custom );

			m_texturePropertyOptions.RemoveRange( 1, m_texturePropertyOptions.Count - 1 );
			m_texturePropertyOptions.AddRange( m_controller.PropertiesContainer.AvailableProperties );

			// Material Textures
			//for( int i = 0; i < m_materialsTextureList.Count; i++ )
			//{
			//	m_materialsTextureList[ i ].UnregisterValueChangedCallback( OnToggleSelected );
			//}
			//m_materialsTextureList.Clear();
			//m_materialTexturesAnchor.Clear();

			foreach( APChannelsItemView channelItem in m_channelsAnchor.Children() )
			{
				channelItem.Destroy();
			}
			m_channelsAnchor.Clear();



			//int propertyCount = m_controller.PropertiesContainer.AvailableProperties.Count;
			//for( int i = 0; i < propertyCount; i++ )
			//{
			//	Toggle toggle = new Toggle( m_controller.PropertiesContainer.AvailableProperties[ i ].Description );
			//	toggle.userData = m_controller.PropertiesContainer.AvailableProperties[ i ];
			//	toggle.SetValueWithoutNotify( m_controller.PropertiesContainer.AvailableProperties[ i ].IsChannel );
			//	toggle.RegisterValueChangedCallback( OnToggleSelected );

			//	VisualElement group = new VisualElement();
			//	group.style.flexDirection = FlexDirection.Row;
			//	toggle.style.flexGrow = 1;
			//	group.Add( toggle );

			//	m_materialTexturesAnchor.Add( group );
			//	m_materialsTextureList.Add( toggle );
			//}
		}

		//void GetInfoFromObject()
		//{
		//	UpdateFromProperties();
		//}
	}
}




