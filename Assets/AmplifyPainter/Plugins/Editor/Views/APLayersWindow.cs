// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//#define USE_CHANNEL_GRID
//#define USE_CHANNEL_DROPDOWN_SELECTOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace AmplifyPainter
{
	public sealed class APLayersWindow : APParentWindow
	{
		private const string WindowLabel = "Layers";

		public delegate void OnAddChannelButton( APLayerType layerType );
		public event OnAddChannelButton OnAddChannelButtonEvent;
#if USE_CHANNEL_DROPDOWN_SELECTOR
		private PopupField<int> m_availableChannelsPopUp;
#endif
		private VisualElement m_availableChannelsToggles;
		private List<int> m_channelElements;
		private List<string> m_channelLabels;
		private List<string> m_channelIds;
		private string m_currentChannelPropertyId;

		private Dictionary<string, APLayersChannelView> m_overallChannelsUIDict;

		private VisualElement m_channelAnchor;
		public VisualElement m_reorderAnchor;
#if USE_CHANNEL_GRID
		private APChannelsGrid m_channelsGrid;
#endif
		private StyleBackground m_layerBackground;

		SerializedObject m_serializedObject;
		//private APChannelsGridIMGUI m_channelsGridIMGUI;
		//private Button m_IMGUITestbutton;
		[MenuItem( "Window/Amplify Painter/Layers " + LAYERS, priority = 1002 )]
		public static APLayersWindow OpenAmplifyPainterLayersWindow()
		{
			APLayersWindow window = OpenOrCloseWindow<APLayersWindow>( false, null );
			if( window )
				window.titleContent = new GUIContent( WindowLabel );
			window.minSize = new Vector2( 200, 150 );
			return window;
		}

		public static APLayersWindow ShowAmplifyPainterWindow()
		{
			APLayersWindow window = GetWindow<APLayersWindow>( false );
			window.titleContent = new GUIContent( WindowLabel );
			window.minSize = new Vector2( 200, 150 );
			return window;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			m_serializedObject = new SerializedObject( m_controller.ChannelsController );
			m_layerBackground = new StyleBackground( APResources.LayerBackgroundTexture );

			name = "Layers";
			VisualElement root = rootVisualElement;
			root.styleSheets.Add( APResources.MainStyleSheet );
			if( !EditorGUIUtility.isProSkin )
				root.styleSheets.Add( APResources.LightStyleSheet );

			//m_channelsGridIMGUI = new APChannelsGridIMGUI( this );
			//m_IMGUITestbutton = new Button( () => 
			//{
			//	Rect rect = new Rect( 0, m_IMGUITestbutton.layout.y + 2*m_IMGUITestbutton.layout.height, 0, 0);
			//	UnityEditor.PopupWindow.Show( rect, m_channelsGridIMGUI );
			//} );
			//root.Add( m_IMGUITestbutton );

			m_channelLabels = new List<string>() { string.Empty };
			m_channelIds = new List<string>() { string.Empty };
			m_channelElements = new List<int>() { 0 };
			m_overallChannelsUIDict = new Dictionary<string, APLayersChannelView>();

#if USE_CHANNEL_DROPDOWN_SELECTOR
			m_availableChannelsPopUp = new PopupField<int>( m_channelElements, 0, OnChannelSelected, OnChannelDrawn );
#endif

#if USE_CHANNEL_GRID
			m_channelsGrid = new APChannelsGrid( m_controller.ChannelsController );
			m_channelsGrid.OnSelectedElementOnGrid += OnLayerDataSelectedFromGrid;
			m_channelsGrid.OnSelectedChannelEvent += OnChannelSelectedFromGrid;
			root.Add( m_channelsGrid );
#endif
			Label selectedChannelLabel = new Label( "Selected Channel" );
			selectedChannelLabel.name = "LayersSelectedChannel";
			root.Add( selectedChannelLabel );
			//root.Add( m_availableChannelsPopUp );

			m_availableChannelsToggles = new VisualElement();
			m_availableChannelsToggles.name = "PerChannelElementsToggles";
			m_availableChannelsToggles.style.flexDirection = FlexDirection.Row;
			m_availableChannelsToggles.style.flexWrap = Wrap.Wrap;
			root.Add( m_availableChannelsToggles );

			m_channelAnchor = new VisualElement();
			m_channelAnchor.name = "LayerBox";
			root.Add( m_channelAnchor );
			m_reorderAnchor = new VisualElement();
			m_reorderAnchor.name = "ReorderSelector";
			root.Add( m_reorderAnchor );

			AddLayerButtons( root, OnMainAddLayerButton );

			m_controller.OnChannelsUpdated += OnChannelsUpdated;
			m_controller.OnUpdateLayerView += UpdateLayerView;
			m_controller.OnAddFirstLayer += OnAddFirstLayer;
			m_controller.OnUpdateInfoFromObjectEvent += GetInfoFromObject;

			m_controller.ChannelsController.OnChannelAdded += OnChannelAdded;
			m_controller.ChannelsController.OnChannelRemoved += OnChannelRemoved;
			m_controller.ChannelsController.OnChannelUpdated += OnChannelUpdated;
			m_controller.ChannelsController.OnLayersResized += OnLayersResized;

			//Sync with controller
			//SetNewChannels( m_controller.PropertiesContainer );
			SetNewChannels( m_controller.ChannelsController.AvailableChannels );
			UpdateAvailableChannels( m_controller.ChannelsController.AvailableChannels, m_controller.CurrentChannel, m_controller.ChannelsController.CurrentLayerIdx );

			//Bind can only be done after channel data has been synced
#if USE_CHANNEL_DROPDOWN_SELECTOR
			SerializedProperty channelsProperty = m_serializedObject.FindProperty( "m_currentChannel" );
			m_availableChannelsPopUp.BindProperty( channelsProperty );
#else
			OnChannelSelected( m_controller.ChannelsController.CurrentChannelIdx );
#endif
		}
#if USE_CHANNEL_GRID
		public void RefreshGrid()
		{
			m_channelsGrid.RefreshAll( m_controller.ChannelsController );
		}
#endif
		public override void OnDisable()
		{
			base.OnDisable();
#if USE_CHANNEL_GRID
			m_channelsGrid.OnSelectedElementOnGrid -= OnLayerDataSelectedFromGrid;
			m_channelsGrid.OnSelectedChannelEvent -= OnChannelSelectedFromGrid;
#endif

			m_controller.OnChannelsUpdated -= OnChannelsUpdated;
			m_controller.OnUpdateLayerView -= UpdateLayerView;
			m_controller.OnAddFirstLayer -= OnAddFirstLayer;
			m_controller.OnUpdateInfoFromObjectEvent -= GetInfoFromObject;

			m_controller.ChannelsController.OnChannelAdded -= OnChannelAdded;
			m_controller.ChannelsController.OnChannelRemoved -= OnChannelRemoved;
			m_controller.ChannelsController.OnChannelUpdated -= OnChannelUpdated;
			m_controller.ChannelsController.OnLayersResized -= OnLayersResized;
		}

		public void SetNewChannels( APTexturePropertyController propertyContainer )
		{
#if USE_CHANNEL_GRID
			m_channelsGrid.ClearAll();
#endif
			m_availableChannelsToggles.Clear();
			foreach( KeyValuePair<string, APLayersChannelView> kvp in m_overallChannelsUIDict )
			{
				kvp.Value.Destroy();
			}

			m_overallChannelsUIDict.Clear();

			m_channelLabels.Clear();
			m_channelIds.Clear();
			m_channelElements.Clear();
			AddDummyChannelInfo();

			m_channelAnchor.Clear();
#if USE_CHANNEL_DROPDOWN_SELECTOR
			m_availableChannelsPopUp.SetValueWithoutNotify( 0 );
#endif
			m_currentChannelPropertyId = string.Empty;

			//Register all possible available channels into dictionary
			for( int i = 0; i < propertyContainer.AvailableProperties.Count; i++ )
			{
				m_overallChannelsUIDict.Add( propertyContainer.AvailableProperties[ i ].Name, new APLayersChannelView( this, OnLayerReorder ) );
			}
		}

		public void SetNewChannels( List<APChannel> channels )
		{

			foreach( KeyValuePair<string, APLayersChannelView> kvp in m_overallChannelsUIDict )
			{
				kvp.Value.Destroy();
			}

			m_overallChannelsUIDict.Clear();

			m_channelLabels.Clear();
			m_channelIds.Clear();
			m_channelElements.Clear();
			m_availableChannelsToggles.Clear();

			AddDummyChannelInfo();

			m_channelAnchor.Clear();
#if USE_CHANNEL_DROPDOWN_SELECTOR
			m_availableChannelsPopUp.SetValueWithoutNotify( 0 );
#endif
			m_currentChannelPropertyId = string.Empty;

			//Register all possible available channels into dictionary
			for( int i = 0; i < channels.Count; i++ )
			{
				m_overallChannelsUIDict.Add( channels[ i ].UniqueId, new APLayersChannelView( this, OnLayerReorder ) );
				AddChannelToggle( channels[ i ] );
			}

		}

		public void UpdateAvailableChannels( List<APChannel> availableChannels, int currentSelectedChannel, int currentSelectedLayer )
		{
			m_channelLabels.Clear();
			m_channelIds.Clear();
			m_channelElements.Clear();
			if( availableChannels.Count > 0 )
			{
				for( int channelIdx = 0; channelIdx < availableChannels.Count; channelIdx++ )
				{
					( (ToolbarToggle)m_availableChannelsToggles[ channelIdx ] ).SetValueWithoutNotify( m_controller.ChannelsController.CurrentChannelIdx == channelIdx );
					m_overallChannelsUIDict[ availableChannels[ channelIdx ].UniqueId ].Reset();
					//m_overallChannelsUIDict[ availableChannels[ channelIdx ].UniqueId ].LinkToObject( new SerializedObject( availableChannels[ channelIdx ] ) );

					m_channelLabels.Add( availableChannels[ channelIdx ].FromProperty ? availableChannels[ channelIdx ].Property.Description : availableChannels[ channelIdx ].Name );
					m_channelIds.Add( availableChannels[ channelIdx ].UniqueId );
					m_channelElements.Add( channelIdx );

					for( int layerIdx = 0; layerIdx < availableChannels[ channelIdx ].Layers.Count; layerIdx++ )
					{
						APLayerElementView layerView = CreateNewLayerElement( availableChannels[ channelIdx ].Layers[ layerIdx ].LayerType, layerIdx, OnAddLayerButton, OnRemoveLayerButton, OnLayerDataChanged, OnLayerSelected, new SerializedObject( availableChannels[ channelIdx ].Layers[ layerIdx ] ) );

						//if( layerIdx == currentSelectedLayer )
						//{
						layerView.Selected.SetValueWithoutNotify( layerIdx == availableChannels[ channelIdx ].CurrentLayerIdx );
						//}

						layerView.Value.image = availableChannels[ channelIdx ].Layers[ layerIdx ].Value;
						m_overallChannelsUIDict[ availableChannels[ channelIdx ].UniqueId ].AddLayer( layerView );
						if( layerIdx == m_controller.ChannelsController.CurrentLayerIdx )
							layerView.SelectBackground();
					}
				}
#if USE_CHANNEL_DROPDOWN_SELECTOR
				int currChannelSelected = ( currentSelectedChannel > -1 ) ? currentSelectedChannel : Mathf.Clamp( m_availableChannelsPopUp.value, 0, availableChannels.Count - 1 );
				m_currentChannelPropertyId = m_channelIds.Count > 0 ? m_channelIds[ currChannelSelected ] : string.Empty;
				m_availableChannelsPopUp.SetValueWithoutNotify( currChannelSelected );
#else
				m_currentChannelPropertyId = m_channelIds[ m_controller.ChannelsController.CurrentChannelIdx ];
				OnChannelSelected( m_controller.ChannelsController.CurrentChannelIdx );
#endif
			}
			else
			{
				AddDummyChannelInfo();
				m_currentChannelPropertyId = string.Empty;
#if USE_CHANNEL_DROPDOWN_SELECTOR
				m_availableChannelsPopUp.SetValueWithoutNotify( 0 );
#endif
			}
		}

		public void OnAddFirstLayer( APChannel channel, APParentLayer layer )
		{
			bool selected = channel.CurrentLayerIdx > -1;
			APLayerElementView layerView = CreateNewLayerElement( layer.LayerType, 0, OnAddLayerButton, OnRemoveLayerButton, OnLayerDataChanged, OnLayerSelected, new SerializedObject( layer ) );
			layerView.Selected.SetValueWithoutNotify( selected );
			layerView.Value.image = layer.Value;
			//m_overallChannelsUIDict[ m_currentChannelPropertyId ].AddLayer( layerView );
			string id = m_channelIds[ channel.CurrentIdx ];
			m_overallChannelsUIDict[ id ].AddLayer( layerView );
#if USE_CHANNEL_GRID
			m_channelsGrid.AddLayer( channel , layer, selected );
#endif
		}

		void AddDummyChannelInfo()
		{
			m_channelLabels.Add( string.Empty );
			m_channelIds.Add( string.Empty );
			m_channelElements.Add( 0 );
		}

		APLayerElementView CreateNewLayerElement( APLayerType layerType, int id, APLayerElementView.AddLayerButtonClicked onAddLayer, APLayerElementView.RemoveLayerButtonClicked onRemoveLayer, APLayerElementView.LayerDataChanged onLayerDataChanged, EventCallback<ChangeEvent<bool>> toggleSelected, SerializedObject layerObject )
		{
			APLayerElementView element = new APLayerElementView( layerType, id, m_layerBackground, onAddLayer, onRemoveLayer, onLayerDataChanged, toggleSelected, layerObject );
			element.OnObjectsDroppedOnLayer += OnObjectsDroppedOnLayer;
			//element.style.flexShrink = 13;
			return element;
		}

		void OnObjectsDroppedOnLayer( int layerIdx, UnityEngine.Object[] objects )
		{
			bool resetBindings = false;
			if( m_controller.ChannelsController.DropObjectsOnLayer( layerIdx, ref resetBindings, objects ) )
			{
				m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ layerIdx ].Value.image = m_controller.ChannelsController.CurrentChannelData.Layers[ layerIdx ].Value;
				m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ layerIdx ].Value.MarkDirtyRepaint();
				if( resetBindings )
				{
					SerializedObject serializedObject = new SerializedObject( m_controller.ChannelsController.GetLayer( layerIdx ) );
					m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ layerIdx ].ReassignBindings( serializedObject );
				}
			}
		}

		void OnAddLayerButton( APLayerType layerType, int layerIdx )
		{
			if( string.IsNullOrEmpty( m_currentChannelPropertyId ) )
				return;
#if USE_CHANNEL_DROPDOWN_SELECTOR
			APParentLayer layer = m_controller.AddLayerToChannel( layerType, m_availableChannelsPopUp.value, layerIdx, false );
#else
			APParentLayer layer = m_controller.AddLayerToChannel( layerType, m_controller.ChannelsController.CurrentChannelIdx, layerIdx, false,true );
#endif
			if( layer != null )
			{
				if( layerIdx > m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers.Count )
					return;

				APLayerElementView layerView = CreateNewLayerElement( layerType, layerIdx, OnAddLayerButton, OnRemoveLayerButton, OnLayerDataChanged, OnLayerSelected, new SerializedObject( layer ) );


				m_overallChannelsUIDict[ m_currentChannelPropertyId ].InsertLayer( layerIdx, layerView );
				RefreshLayerIds();

				m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ layerIdx ].Value.image = layer.Value;

				if( m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers.Count == 1 )
				{
					layerView.Selected.SetValueWithoutNotify( true );
				}
				else if( APGeneralOptions.SelectLayerOnCreation )
				{
					layerView.Selected.value = true;
				}
			}
		}

		void OnLayerReorder( int oldIdx, int newIdx, bool switchMode )
		{
			RefreshLayerIds();
			m_controller.ChannelsController.ReorderLayers( oldIdx, newIdx, switchMode );
#if USE_CHANNEL_GRID
			m_channelsGrid.RefreshChannel( m_controller.ChannelsController.CurrentChannelData );
#endif
		}

#if USE_CHANNEL_GRID
		void OnChannelSelectedFromGrid( int channelIdx )
		{
			m_availableChannelsPopUp.value = channelIdx;
		}
#endif
		void OnLayerDataSelectedFromGrid( int channelIdx, int layerIdx )
		{
			if( channelIdx == m_controller.ChannelsController.CurrentChannelIdx )
				m_controller.ChannelsController.CurrentLayerIdx = layerIdx;

			string currentChannelId = m_channelIds[ channelIdx ];

			for( int i = 0; i < m_overallChannelsUIDict[ currentChannelId ].Layers.Count; i++ )
			{
				bool toggleValue = ( (int)m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.userData == layerIdx );
				m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.SetValueWithoutNotify( toggleValue );
			}

			for( int i = 0; i < m_overallChannelsUIDict[ currentChannelId ].Layers.Count; i++ )
			{
				m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].DeselectBackground();
				if( m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.value )
					m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].SelectBackground();
			}
			m_controller.ChannelsController.SetLayerOnChannel( channelIdx, layerIdx );
		}

		void OnLayerSelected( ChangeEvent<bool> eventCall )
		{
			Toggle toggle = eventCall.target as Toggle;
			if( !toggle.value )
			{
				toggle.SetValueWithoutNotify( true );
				return;
			}

			m_controller.ChannelsController.CurrentLayerIdx = toggle.value ? (int)toggle.userData : -1;
#if USE_CHANNEL_DROPDOWN_SELECTOR
			string currentChannelId = m_channelIds[ m_availableChannelsPopUp.value ];
#else
			string currentChannelId = m_channelIds[ m_controller.ChannelsController.CurrentChannelIdx ];
#endif
			for( int i = 0; i < m_overallChannelsUIDict[ currentChannelId ].Layers.Count; i++ )
			{
				if( m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.userData != toggle.userData )
				{
					m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.SetValueWithoutNotify( false );
				}
			}

			for( int i = 0; i < m_overallChannelsUIDict[ currentChannelId ].Layers.Count; i++ )
			{
				m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].DeselectBackground();
				if( m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.value )
					m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].SelectBackground();
			}
			m_controller.SetCurrentLayerAndUpdateMaterial( m_controller.ChannelsController.CurrentLayerIdx );
#if USE_CHANNEL_GRID
			m_channelsGrid.UpdateChannelSelection( m_controller.ChannelsController.CurrentChannelIdx, m_controller.ChannelsController.CurrentLayerIdx, toggle.value );
#endif
		}

		void RefreshLayerIds()
		{
			int fireLayerRefresh = -1;
			for( int i = 0; i < m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers.Count; i++ )
			{
				if( m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ i ].Selected.value && m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ i ].Id != i )
				{
					fireLayerRefresh = i;
				}
				m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ i ].Id = i;
			}

			if( fireLayerRefresh > -1 )
			{
				m_controller.SetCurrentLayerAndUpdateMaterial( fireLayerRefresh );
			}
		}

		void OnLayerDataChanged( int layerIdx, bool refreshValue )
		{
			if( refreshValue )
			{
				m_controller.ChannelsController.RefreshLayerOnChannel( layerIdx );
			}
			m_controller.ChannelsController.UpdateValue();
		}

		void OnRemoveLayerButton( int layerIdx )
		{
			if( layerIdx >= m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers.Count )
				return;

			m_overallChannelsUIDict[ m_currentChannelPropertyId ].RemoveLayer( layerIdx );

			RefreshLayerIds();
#if USE_CHANNEL_DROPDOWN_SELECTOR
			if( m_controller.RemoveLayerFromChannel( m_availableChannelsPopUp.value, layerIdx ) )
#else
			if( m_controller.RemoveLayerFromChannel( m_controller.ChannelsController.CurrentChannelIdx, layerIdx ) )
#endif
			{
				SetNewSelected( m_controller.ChannelsController.CurrentLayerIdx );
			}
			m_controller.ChannelsController.UpdateValue();
#if USE_CHANNEL_GRID
			m_channelsGrid.RefreshChannel( m_controller.ChannelsController.CurrentChannelData );
#endif
		}

		void OnMainAddLayerButton( APLayerType layerType )
		{
			OnAddLayerButton( layerType, m_controller.ChannelsController.CurrentLayerIdx );
		}

		void OnMainRemoveLayerButton()
		{
			// this action should be performed by a controller
			if( m_controller.HasChannels() && m_controller.HasLayerOnChannel() )
				OnRemoveLayerButton( m_controller.ChannelsController.CurrentLayerIdx );
		}

		public string OnChannelSelected( int value )
		{
			m_currentChannelPropertyId = m_channelIds[ value ];
			RefreshChannelUI();
			//m_controller.CurrentChannel = value;
			return m_channelLabels[ value ];
		}

		void RefreshChannelUI()
		{
			//PopUp UI initialization also calls this through the OnChannelSelected event
			if( m_channelAnchor != null && m_overallChannelsUIDict.ContainsKey( m_currentChannelPropertyId ) )
			{
				m_channelAnchor.Clear();
				m_channelAnchor.Add( m_overallChannelsUIDict[ m_currentChannelPropertyId ] );
			}
		}

		public void SetNewSelected( int newSelected )
		{
			m_controller.ChannelsController.CurrentLayerIdx = newSelected;
#if USE_CHANNEL_DROPDOWN_SELECTOR
			string currentChannelId = m_channelIds[ m_availableChannelsPopUp.value ];
#else
			string currentChannelId = m_channelIds[ m_controller.ChannelsController.CurrentChannelIdx ];
#endif
			for( int i = 0; i < m_overallChannelsUIDict[ currentChannelId ].Layers.Count; i++ )
			{
				m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].Selected.SetValueWithoutNotify( i == newSelected );
				if( i == m_controller.ChannelsController.CurrentLayerIdx )
					m_overallChannelsUIDict[ currentChannelId ].Layers[ i ].SelectBackground();
			}
		}

		public string OnChannelDrawn( int value )
		{
			return m_channelLabels[ value ];
		}

		public override void OnToolChangedReset()
		{
			int toggleCount = m_availableChannelsToggles.childCount;
			for( int i = 0; i < toggleCount; i++ )
			{
				( (ToolbarToggle)m_availableChannelsToggles[ i ] ).UnregisterValueChangedCallback( OnChannelToggleSelected );
			}
			m_availableChannelsToggles.Clear();

			foreach( KeyValuePair<string, APLayersChannelView> kvp in m_overallChannelsUIDict )
			{
				kvp.Value.Destroy();
			}

			m_overallChannelsUIDict.Clear();

			m_channelLabels.Clear();
			m_channelIds.Clear();
			m_channelElements.Clear();
			AddDummyChannelInfo();

			m_channelAnchor.Clear();
#if USE_CHANNEL_DROPDOWN_SELECTOR
			m_availableChannelsPopUp.SetValueWithoutNotify( 0 );
			m_availableChannelsPopUp.MarkDirtyRepaint();
#endif
			m_currentChannelPropertyId = string.Empty;
		}

		void OnChannelsUpdated()
		{
			UpdateAvailableChannels( m_controller.ChannelsController.AvailableChannels, -1, m_controller.ChannelsController.CurrentLayerIdx );
		}

		void OnChannelAdded( int idx, APChannel channel )
		{
			APLayersChannelView channelView = new APLayersChannelView( this, OnLayerReorder );
			if( m_overallChannelsUIDict.ContainsKey( channel.UniqueId ) )
			{
				m_overallChannelsUIDict[ channel.UniqueId ].Destroy();
				m_overallChannelsUIDict[ channel.UniqueId ] = channelView;
			}
			else
			{
				m_overallChannelsUIDict.Add( channel.UniqueId, channelView );
			}
#if USE_CHANNEL_GRID
			m_channelsGrid.AddChannel( channel );
#endif
			AddChannelToggle( channel );
		}

		void AddChannelToggle( APChannel channel )
		{
			ToolbarToggle channelToggle = new ToolbarToggle() { text = channel.Name };
			channelToggle.AddToClassList( "ComponentToggle" );
			channelToggle.Q<Label>().style.minWidth = 0;
			channelToggle.RegisterValueChangedCallback( OnChannelToggleSelected );
			m_availableChannelsToggles.Insert( channel.CurrentIdx, channelToggle );
		}

		void OnChannelToggleSelected( ChangeEvent<bool> changeEvent )
		{
			int channelIdx = -1;
			for( int i = 0; i < m_availableChannelsToggles.childCount; i++ )
			{
				if( m_availableChannelsToggles[ i ] != changeEvent.target )
				{
					( (ToolbarToggle)m_availableChannelsToggles[ i ] ).SetValueWithoutNotify( false );
				}
				else
				{
					( (ToolbarToggle)m_availableChannelsToggles[ i ] ).SetValueWithoutNotify( true );
					channelIdx = i;
				}
			}
			m_controller.ChannelsController.CurrentChannelIdx = channelIdx;
			OnChannelSelected( channelIdx );
		}

		void AddLayerButtons( VisualElement root, OnAddChannelButton addLayerEvt )
		{
			OnAddChannelButtonEvent += addLayerEvt;

			VisualElement buttonsAnchor = new VisualElement();
			buttonsAnchor.name = "LayerViewButtonAnchor";
			{
				Button mainRemoveButton = new Button( OnMainRemoveLayerButton );
				mainRemoveButton.AddToClassList( "LayerViewButton" );
				mainRemoveButton.RemoveFromClassList("unity-button");
				mainRemoveButton.tooltip = "Delete Current Layer";
				mainRemoveButton.style.backgroundImage = APResources.RemoveLayerIconTexture;
				buttonsAnchor.Add( mainRemoveButton );
			}

			{
				Button mainAddSMORTButton = new Button( OnAddTextureLayerButton );
				mainAddSMORTButton.AddToClassList( "LayerViewButton" );
				mainAddSMORTButton.RemoveFromClassList( "unity-button" );
				mainAddSMORTButton.tooltip = "Add Smart Fill Layer";
				mainAddSMORTButton.style.backgroundImage = APResources.AddSMORTLayerIconTexture;
				buttonsAnchor.Add( mainAddSMORTButton );
			}

			{
				Button mainAddButton = new Button( OnAddDefaultLayerButton );
				mainAddButton.AddToClassList( "LayerViewButton" );
				mainAddButton.RemoveFromClassList( "unity-button" );
				mainAddButton.tooltip = "Add New Layer";
				mainAddButton.style.backgroundImage = APResources.AddLayerIconTexture;
				buttonsAnchor.Add( mainAddButton );
			}
			root.Add( buttonsAnchor );
		}

		void OnAddDefaultLayerButton()
		{
			if( OnAddChannelButtonEvent != null )
			{
				OnAddChannelButtonEvent( APLayerType.Default );
			}
#if USE_CHANNEL_GRID
			m_channelsGrid.RefreshChannel( m_controller.ChannelsController.CurrentChannelData );
#endif
		}

		void OnAddTextureLayerButton()
		{
			if( OnAddChannelButtonEvent != null )
			{
				OnAddChannelButtonEvent( APLayerType.Texture2D );
			}
#if USE_CHANNEL_GRID
			m_channelsGrid.RefreshChannel( m_controller.ChannelsController.CurrentChannelData );
#endif
		}

		void OnChannelRemoved( int idx, APChannel channel )
		{
			if( m_overallChannelsUIDict.ContainsKey( channel.UniqueId ) )
			{
				m_overallChannelsUIDict[ channel.UniqueId ].Destroy();
			}
			m_overallChannelsUIDict.Remove( channel.UniqueId );
#if USE_CHANNEL_GRID
			m_channelsGrid.RemoveChannel( idx );
#endif
			m_availableChannelsToggles.RemoveAt( idx );
		}

		void OnLayersResized()
		{
			List<APChannel> availableChannels = m_controller.ChannelsController.AvailableChannels;
			for( int channelIdx = 0; channelIdx < availableChannels.Count; channelIdx++ )
			{
				int layerCount = availableChannels[ channelIdx ].Layers.Count;

				for( int layerIdx = 0; layerIdx < layerCount; layerIdx++ )
				{
					m_overallChannelsUIDict[ availableChannels[ channelIdx ].UniqueId ].Layers[ layerIdx ].Value.image = availableChannels[ channelIdx ].Layers[ layerIdx ].Value;
				}
			}
		}

		void OnChannelUpdated( int idx, APChannel channel )
		{
			m_channelLabels[ idx ] = channel.Name;
			m_channelIds[ idx ] = channel.UniqueId;
#if USE_CHANNEL_DROPDOWN_SELECTOR
			m_availableChannelsPopUp.SetValueWithoutNotify( m_availableChannelsPopUp.value );
			m_availableChannelsPopUp.MarkDirtyRepaint();
#endif
			m_controller.UpdateChannelOnMaterial( idx );
			( (ToolbarToggle)m_availableChannelsToggles[ idx ] ).text = channel.Name;
#if USE_CHANNEL_GRID
			m_channelsGrid.RefreshChannelLabel( idx, channel );
#endif
		}

		void UpdateLayerView()
		{
			CurrentLayerView.Value.MarkDirtyRepaint();
		}

		void GetInfoFromObject()
		{
			SetNewChannels( m_controller.PropertiesContainer );
			if( APGeneralOptions.AddFirstLayerOnSelection )
			{
				UpdateAvailableChannels( m_controller.ChannelsController.AvailableChannels, 0, 0 );
			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();

			OnAddChannelButtonEvent = null;

			m_layerBackground = null;
			m_channelElements.Clear();
			m_channelElements = null;
			m_channelLabels.Clear();
			m_channelLabels = null;
			m_channelIds.Clear();
			m_channelIds = null;
			m_channelAnchor = null;

			foreach( KeyValuePair<string, APLayersChannelView> kvp in m_overallChannelsUIDict )
			{
				kvp.Value.Destroy();
			}
			m_overallChannelsUIDict.Clear();
			m_overallChannelsUIDict = null;
			rootVisualElement.Clear();
#if USE_CHANNEL_GRID
			m_channelsGrid.ClearAll();
			m_channelsGrid = null;
#endif
		}

		public APLayerElementView CurrentLayerView
		{
			get
			{
				if( m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers != null && m_controller.ChannelsController.CurrentLayerIdx < m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers.Count )
					return m_overallChannelsUIDict[ m_currentChannelPropertyId ].Layers[ m_controller.ChannelsController.CurrentLayerIdx ];

				return null;
			}
		}
#if USE_CHANNEL_GRID
		public APChannelsGrid ChannelGrid{ get { return m_channelsGrid; } }
#endif
		public VisualElement AvailableChannelsToggles { get { return m_availableChannelsToggles; } }
	}
}
