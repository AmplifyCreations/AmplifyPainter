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
	public sealed class APLayerOpsElement : VisualElement
	{
		public delegate void LayerOpsChanged();
		public event LayerOpsChanged OnLayerOpsChangedEvent;

		public VisualElement TilingAnchor;
		public Label TilingLabel;
		public FloatField TilingX;
		public FloatField TilingY;

		public VisualElement OffsetAnchor;
		public Label OffsetLabel;
		public FloatField OffsetX;
		public FloatField OffsetY;

		public APLayerOpsElement( SerializedObject obj, string propertyName )
		{
			name = "LayerOpsMain";
			SerializedProperty tilingXProperty = obj.FindProperty( propertyName + ".x" );
			SerializedProperty tilingYProperty = obj.FindProperty( propertyName + ".y" );

			SerializedProperty offsetXProperty = obj.FindProperty( propertyName + ".z" );
			SerializedProperty offsetYProperty = obj.FindProperty( propertyName + ".w" );

			TilingAnchor = new VisualElement();
			TilingAnchor.name = "LayerOpsAnchor";

			TilingLabel = new Label( "Tile" );
			TilingLabel.name = "LayerOpsLabel";
			TilingAnchor.Add( TilingLabel );

			TilingX = new FloatField();
			TilingX.name = "LayerOpsValue";
			TilingX.BindProperty( tilingXProperty );
			TilingX.RegisterValueChangedCallback( FireValueChanged );
			TilingAnchor.Add( TilingX );

			TilingY = new FloatField();
			TilingY.name = "LayerOpsValue";
			TilingY.BindProperty( tilingYProperty );
			TilingY.RegisterValueChangedCallback( FireValueChanged );
			TilingAnchor.Add( TilingY );

			OffsetAnchor = new VisualElement();
			OffsetAnchor.name = "LayerOpsAnchor";

			OffsetLabel = new Label( "Offset" );
			OffsetLabel.name = "LayerOpsLabel";
			OffsetAnchor.Add( OffsetLabel );

			OffsetX = new FloatField();
			OffsetX.name = "LayerOpsValue";
			OffsetX.BindProperty( offsetXProperty );
			OffsetX.RegisterValueChangedCallback( FireValueChanged );
			OffsetAnchor.Add( OffsetX );

			OffsetY = new FloatField();
			OffsetY.name = "LayerOpsValue";
			OffsetY.BindProperty( offsetYProperty );
			OffsetY.RegisterValueChangedCallback( FireValueChanged );
			OffsetAnchor.Add( OffsetY );

			Add( TilingAnchor );
			Add( OffsetAnchor );
		}

		public void ReassignBindings( SerializedObject obj, string propertyName )
		{
			SerializedProperty tilingXProperty = obj.FindProperty( propertyName + ".x" );
			TilingX.BindProperty( tilingXProperty );

			SerializedProperty tilingYProperty = obj.FindProperty( propertyName + ".y" );
			TilingY.BindProperty( tilingYProperty );

			SerializedProperty offsetXProperty = obj.FindProperty( propertyName + ".z" );
			OffsetX.BindProperty( offsetXProperty );

			SerializedProperty offsetYProperty = obj.FindProperty( propertyName + ".w" );
			OffsetY.BindProperty( offsetYProperty );
		}


		public void FireValueChanged( ChangeEvent<float> eventCall )
		{
			if( OnLayerOpsChangedEvent != null )
				OnLayerOpsChangedEvent();
		}

		public void Destroy()
		{
			TilingLabel.Clear();
			OffsetLabel.Clear();
			Clear();
			OnLayerOpsChangedEvent = null;
		}
	}

	public sealed class APLayerElementView : VisualElement
	{
		private const string NameProperty = "Name";
		private const string BlendModeProperty = "BlendMode";
		private const string OpacityProperty = "Opacity";
		private const string LayerOpsProperty = "LayerOpsParams";

		public delegate void AddLayerButtonClicked( APLayerType layerType, int layerIdx );
		public delegate void RemoveLayerButtonClicked( int layerIdx );
		public delegate void LayerDataChanged( int layerIdx , bool refreshValue );
		public delegate void ObjectsDroppedOnLayer( int layerIdx, UnityEngine.Object[] obj );

		public event AddLayerButtonClicked OnAddLayerButtonClickedEvent;
		public event RemoveLayerButtonClicked OnRemoveLayerButtonClickedEvent;
		public event LayerDataChanged OnLayerDataChanged;
		public event ObjectsDroppedOnLayer OnObjectsDroppedOnLayer;

		private TextField m_name;
		private Image m_value;
		private int m_id;
		private Toggle m_selected;
		private EnumField m_blendModeEnum;
		private SerializedProperty m_blendModeProperty;

		private VisualElement m_blendOpacityAnchor;
		private VisualElement m_layerOpacityGroup;
		//private Vector4Field m_layerOps;
		private APLayerOpsElement m_layerOpsEx;
		private FloatField m_opacityField;
		private Label m_label;

		public APLayerElementView( APLayerType layerType,
									int id,
									StyleBackground bgImage,
									AddLayerButtonClicked onAddLayer,
									RemoveLayerButtonClicked onRemoveLayer,
									LayerDataChanged onLayerDataUpdated,
									EventCallback<ChangeEvent<bool>> toggleSelected,
									SerializedObject serializedLayerObject )
		{
			this.AddToClassList( "LayerMainElement" );
			OnAddLayerButtonClickedEvent += onAddLayer;
			OnRemoveLayerButtonClickedEvent += onRemoveLayer;
			OnLayerDataChanged += onLayerDataUpdated;
			
			////// TOGGLE
			m_selected = new Toggle();
			m_selected.name = "LayerToggleSelection";
			m_selected.RegisterValueChangedCallback( toggleSelected );
			
			this.RegisterCallback<MouseDownEvent>( OnClickMain/*, TrickleDown.TrickleDown */);
			Add( m_selected );

			////// IMAGE
			m_value = new Image();
			m_value.name = "LayerImageValue";
			m_value.image = Texture2D.whiteTexture;
			m_value.style.backgroundImage = bgImage;
			m_value.scaleMode = ScaleMode.StretchToFill;

			switch( layerType )
			{
				case APLayerType.Default:
				break;
				case APLayerType.Shader:
				case APLayerType.Texture2D:
				{
					Image image = new Image();
					image.image = APResources.SMORTLayerIconTexture;
					image.name = "LayerImageValueIcon";
					m_value.Add( image );
				}
				break;
				default:
				break;
			}
			Add( m_value );

			////// NAME
			SerializedProperty layerName = serializedLayerObject.FindProperty( NameProperty );

			m_label = new Label( layerName.stringValue );
			m_label.name = "LayerNameTextfield";
			m_label.focusable = true;
			m_label.RegisterCallback<MouseDownEvent>( OnClick, TrickleDown.TrickleDown );

			m_name = new TextField() { value = layerName.stringValue };
			m_name.name = "LayerNameTextfield";
			m_name.RegisterCallback<BlurEvent>( SwitchToLabel );
			m_name.BindProperty( layerName );
			Add( m_label );
			if( layerType != APLayerType.Default )
			{
				//m_layerOps = new Vector4Field();
				//m_layerOps.name = "LayerOps";
				SerializedProperty layerOpProperty = serializedLayerObject.FindProperty( LayerOpsProperty );
				//m_layerOps.BindProperty( layerOpProperty );
				//m_layerOps.RegisterValueChangedCallback( OnLayerOpChanged );
				//Add( m_layerOps );

				m_layerOpsEx = new APLayerOpsElement( serializedLayerObject, LayerOpsProperty );
				m_layerOpsEx.OnLayerOpsChangedEvent += OnLayerOpExChanged;
				Add( m_layerOpsEx );
			}

			m_blendOpacityAnchor = new VisualElement();
			m_blendOpacityAnchor.name = "LayerBlendOpacityAnchor";
			m_blendModeProperty = serializedLayerObject.FindProperty( BlendModeProperty );

			m_blendModeEnum = new EnumField( string.Empty );
			m_blendModeEnum.name = "LayerBlendMode";
			m_blendModeEnum.Init( (APLayerBlendMode)m_blendModeProperty.enumValueIndex );
			m_blendModeEnum.RegisterValueChangedCallback( OnBlendModeChanged );
			m_blendOpacityAnchor.Add( m_blendModeEnum );

			SerializedProperty layerOpacity = serializedLayerObject.FindProperty( OpacityProperty );
			m_opacityField = new FloatField() { value = layerOpacity.floatValue };
			m_opacityField.BindProperty( layerOpacity );
			m_opacityField.RegisterValueChangedCallback( OnOpacityValueChanged );

			m_layerOpacityGroup = new VisualElement();
			m_layerOpacityGroup.name = "LayerOpacityGroup";
			m_layerOpacityGroup.AddToClassList( "slider-with-field" );
			m_layerOpacityGroup.Add( m_opacityField );
			m_blendOpacityAnchor.Add( m_layerOpacityGroup );
			Add( m_blendOpacityAnchor );
			Id = id;

			RegisterCallback<DragUpdatedEvent>( OnDragUpdate );
			RegisterCallback<DragPerformEvent>( OnDragPerform, TrickleDown.TrickleDown );
		}

		void SwitchToLabel(BlurEvent e)
		{
			m_label.text = m_name.text;
			int index = IndexOf( m_name );
			Insert( index, m_label );
			Remove( m_name );
			m_singleClick = true;
			m_label.Focus();
		}

		void OnClickMain( MouseDownEvent e )
		{
			m_selected.value = true;
		}

		bool m_singleClick = true;
		double m_lastTimeClicked = 0;
		void OnClick( MouseDownEvent e )
		{
			bool singleClick = true;
			if( m_singleClick && ( EditorApplication.timeSinceStartup - m_lastTimeClicked ) < 0.3f )
			{
				singleClick = false;
				int index = IndexOf( m_label );
				Insert( index, m_name );
				Remove( m_label );
				m_name.Focus();
				VisualElement ti = m_name.ElementAt( 0 );
				ti.Focus();
			}
			else
			{
				m_selected.value = true;
			}
			m_lastTimeClicked = EditorApplication.timeSinceStartup;
			m_singleClick = singleClick;
		}

		public void ReassignBindings( SerializedObject serializedLayerObject )
		{
			SerializedProperty layerName = serializedLayerObject.FindProperty( NameProperty );
			m_name.BindProperty( layerName );

			m_blendModeProperty = serializedLayerObject.FindProperty( BlendModeProperty );
			m_blendModeEnum.Init( (APLayerBlendMode)m_blendModeProperty.enumValueIndex );

			SerializedProperty layerOpacity = serializedLayerObject.FindProperty( OpacityProperty );
			m_opacityField.BindProperty( layerOpacity );

			SerializedProperty layerOpProperty = serializedLayerObject.FindProperty( LayerOpsProperty );
			//m_layerOps.BindProperty( layerOpProperty );

			m_layerOpsEx.ReassignBindings( serializedLayerObject, LayerOpsProperty );
		}

		public void SelectBackground( )
		{
			AddToClassList( "layer-selected" );
		}

		public void DeselectBackground()
		{
			RemoveFromClassList( "layer-selected" );
		}


		void OnDragUpdate( DragUpdatedEvent e )
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
		}

		void OnDragPerform( DragPerformEvent e )
		{
			if( OnObjectsDroppedOnLayer != null )
				OnObjectsDroppedOnLayer( m_id, DragAndDrop.objectReferences );
		}

		void OnLayerOpExChanged( )
		{
			if( OnLayerDataChanged != null )
				OnLayerDataChanged( Id, true );
		}

		void OnLayerOpChanged( ChangeEvent<Vector4> eventCall )
		{
			if( OnLayerDataChanged != null )
				OnLayerDataChanged(Id,true);
		}

		void OnOpacityValueChanged( ChangeEvent<float> eventCall )
		{
			if( eventCall.newValue < 0 || eventCall.newValue > 100 )
			{
				m_opacityField.SetValueWithoutNotify( Mathf.Clamp( eventCall.newValue, 0, 100 ));
			}

			if( OnLayerDataChanged != null )
				OnLayerDataChanged(Id,false);
		}

		void OnBlendModeChanged( ChangeEvent<Enum> eventCall )
		{
			m_blendModeProperty.enumValueIndex = Convert.ToInt32( eventCall.newValue );
			m_blendModeProperty.serializedObject.ApplyModifiedProperties();
			if( OnLayerDataChanged != null )
				OnLayerDataChanged(Id,false);
		}
		

		public void Destroy()
		{
			UnregisterCallback<DragUpdatedEvent>( OnDragUpdate );
			UnregisterCallback<DragPerformEvent>( OnDragPerform );
			if( m_layerOpsEx != null )
				m_layerOpsEx.Destroy();

			OnAddLayerButtonClickedEvent = null;
			OnRemoveLayerButtonClickedEvent = null;
			OnLayerDataChanged = null;
			OnObjectsDroppedOnLayer = null;
			m_value = null;
			m_selected = null;
			Clear();
		}

		void OnAddButtonClicked()
		{
			if( OnAddLayerButtonClickedEvent != null )
			{
				OnAddLayerButtonClickedEvent( APLayerType.Default, m_id );
			}
		}

		void OnRemoveButtonClicked()
		{
			if( OnRemoveLayerButtonClickedEvent != null )
			{
				OnRemoveLayerButtonClickedEvent( m_id );
			}
		}
		

		public Toggle Selected { get { return m_selected; } }
		public Image Value { get { return m_value; } set { m_value = value; } }
		public int Id { get { return m_id; } set { m_id = value; m_selected.userData = value; } }
	}
}
