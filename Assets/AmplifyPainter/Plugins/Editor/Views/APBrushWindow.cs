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
	public sealed class APBrushWindow : APParentWindow
	{
		[MenuItem( "Window/Amplify Painter/Brush " + BRUSH, priority = 1003 )]
		public static APBrushWindow OpenAmplifyPainterBrushesWindow()
		{
			var window = OpenOrCloseWindow<APBrushWindow>( false, null );
			if( window )
				window.titleContent = new GUIContent( "Brush" );
			window.minSize = new Vector2( 230, 220 );
			return window;
		}

		private SerializedObject m_serializedObject;
		private SerializedProperty m_methodProperty;
		private SerializedProperty m_alignmentProperty;
		//private SerializedProperty m_colorProperty;
		private ColorField m_colorField;
		private EnumField m_enumField;
		private EnumField m_alignmentField;
		private VisualElement m_spacingGroup;

		private List<VisualElement> m_perChannelElements = new List<VisualElement>();
		private VisualElement m_perChannelElementsToggles;
		private VisualElement m_perChannelElementsData;
		
		public override void OnEnable()
		{
			base.OnEnable();
		
			APBrush brush = m_controller.BrushController.CurrentBrush;
			m_serializedObject = new SerializedObject( brush );

			VisualElement root = rootVisualElement;
			root.name = "BrushUI";
			root.styleSheets.Add( APResources.MainStyleSheet );
			VisualElement miscData = new VisualElement();
			miscData.style.flexShrink = 0;

			//------------- SIZE -------------
			SerializedProperty sizeProperty = m_serializedObject.FindProperty( "Size" );
			SerializedProperty sizeToggleProp = sizeProperty.FindPropertyRelative( "PenActive" );
			sizeProperty = sizeProperty.FindPropertyRelative( "Value" );
			Slider sizeSlider = new Slider( "Size", 0, 3 ) { value = sizeProperty.floatValue };
			FloatField sfField = new FloatField() { value = sizeProperty.floatValue };
			sfField.BindProperty( sizeProperty );
			Toggle sizeToggle = new Toggle( "" );
			sizeToggle.BindProperty( sizeToggleProp );

			var mts = new FieldMouseDragger<float>( sfField );
			mts.SetDragZone( sizeSlider.labelElement );
			sizeSlider.labelElement.AddToClassList( "unity-base-field__label--with-dragger" );
			sizeSlider.BindProperty( sizeProperty );
			
			VisualElement sizeGroup = new VisualElement();
			sizeGroup.AddToClassList( "slider-with-field" );
			sizeGroup.Add( sizeSlider );
			sizeGroup.Add( sfField );
			sizeGroup.Add( sizeToggle );

			//------------- STRENGTH -------------
			SerializedProperty strProperty = m_serializedObject.FindProperty( "Strength" );
			SerializedProperty strToggleProp = strProperty.FindPropertyRelative( "PenActive" );
			strProperty = strProperty.FindPropertyRelative( "Value" );
			Slider strSlider = new Slider( "Opacity", 0, 1 ) { value = strProperty.floatValue };
			FloatField strField = new FloatField() { value = strProperty.floatValue };
			strField.BindProperty( strProperty );
			Toggle strToggle = new Toggle( "" );
			strToggle.BindProperty( strToggleProp );

			var mtst = new FieldMouseDragger<float>( strField );
			mtst.SetDragZone( strSlider.labelElement );
			strSlider.labelElement.AddToClassList( "unity-base-field__label--with-dragger" );
			strSlider.BindProperty( strProperty );

			VisualElement strGroup = new VisualElement();
			strGroup.AddToClassList( "slider-with-field" );
			strGroup.Add( strSlider );
			strGroup.Add( strField );
			strGroup.Add( strToggle );

			//------------- ROTATION -------------
			SerializedProperty rotProperty = m_serializedObject.FindProperty( "Rotation" );
			SerializedProperty rotToggleProp = rotProperty.FindPropertyRelative( "PenActive" );
			PropertyField rotSlider = new PropertyField( rotProperty, "Rotation" );
			rotSlider.BindProperty( rotProperty );
			rotSlider.style.flexGrow = 1;
			Toggle rotToggle = new Toggle( "" );
			rotToggle.BindProperty( rotToggleProp );

			VisualElement rotGroup = new VisualElement();
			rotGroup.style.flexDirection = FlexDirection.Row;
			rotGroup.Add( rotSlider );
			rotGroup.Add( rotToggle );
			//------------- SPACING METHOD -------------
			m_methodProperty = m_serializedObject.FindProperty( "Method" );
			m_enumField = new EnumField( "Method" );
			m_enumField.Init( (StrokeMethod)m_methodProperty.enumValueIndex );

			m_enumField.RegisterCallback<ChangeEvent<Enum>>( ( e ) =>
			{
				m_methodProperty.enumValueIndex = Convert.ToInt32( e.newValue );
				m_methodProperty.serializedObject.ApplyModifiedProperties();
				RegisterSpacing( miscData );
				//AmplifyPainterWindow.Instance.CurrentBrush.Method = (StrokeMethod)Enum.Parse( typeof( StrokeMethod ), e.newValue.ToString() );
			} );

			SerializedProperty spaceProperty = m_serializedObject.FindProperty( "Spacing" );
			SerializedProperty spaceToggleProp = spaceProperty.FindPropertyRelative( "PenActive" );
			spaceProperty = spaceProperty.FindPropertyRelative( "Value" );
			Slider spaceSlider = new Slider( "Spacing", 0.02f, 3 ) { value = spaceProperty.floatValue };
			FloatField spField = new FloatField() { value = spaceProperty.floatValue };
			spField.BindProperty( spaceProperty );
			Toggle spaceToggle = new Toggle( "" );
			spaceToggle.BindProperty( spaceToggleProp );

			var mt = new FieldMouseDragger<float>( spField );
			mt.SetDragZone( spaceSlider.labelElement );
			spaceSlider.labelElement.AddToClassList( "unity-base-field__label--with-dragger" );
			spaceSlider.BindProperty( spaceProperty );

			m_spacingGroup = new VisualElement();
			m_spacingGroup.AddToClassList( "slider-with-field" );
			m_spacingGroup.Add( spaceSlider );
			m_spacingGroup.Add( spField );
			m_spacingGroup.Add( spaceToggle );

			//------------- BACKFACE CULLING -------------
			SerializedProperty cullProp = m_serializedObject.FindProperty( "BackCulling" );
			Toggle cullToggle = new Toggle( "Backface Culling" ) { value = cullProp.boolValue };
			cullToggle.BindProperty( cullProp );

			//------------- ALIGNMENT -------------
			m_alignmentProperty = m_serializedObject.FindProperty( "Alignment" );
			m_alignmentField = new EnumField( "Alignment" );
			m_alignmentField.Init( (ProjectionAlignment)m_alignmentProperty.enumValueIndex );
			m_alignmentField.RegisterCallback<ChangeEvent<Enum>>( ( e ) =>
			{
				DestroyImmediate( brush.BrushMaterial );
				m_alignmentProperty.enumValueIndex = Convert.ToInt32( e.newValue );
				m_alignmentProperty.serializedObject.ApplyModifiedProperties();
			} );

			//------------- MASK -------------
			SerializedProperty texProperty = m_serializedObject.FindProperty( "Mask" );
			ObjectField textureField = new ObjectField( "Mask" ) { objectType = typeof( Texture2D ), value = texProperty.objectReferenceValue };
			textureField.BindProperty( texProperty );

			//InspectorElement ins = new InspectorElement( m_serializedObject );
			//miscData.Add( ins );
			//miscData.Add( m_colorField );
			//miscData.Add( texField );
			miscData.Add( sizeGroup );
			miscData.Add( strGroup );
			miscData.Add( rotGroup );
			miscData.Add( m_enumField );
			RegisterSpacing( miscData );
			miscData.Add( cullToggle );
			miscData.Add( m_alignmentField );
			miscData.Add( textureField );


			Label perChannelLabel = new Label( "Per Channel Components" );
			perChannelLabel.name = "BrushPerChannelComponents";
			miscData.Add( perChannelLabel );

			// Components per channel
			ScrollView channelComponents = new ScrollView() { /*name = "BrushUI"*/ };
			m_perChannelElementsToggles = new VisualElement();
			m_perChannelElementsToggles.name = "PerChannelElementsToggles";
			m_perChannelElementsToggles.style.flexDirection = FlexDirection.Row;
			m_perChannelElementsToggles.style.flexWrap = Wrap.Wrap;
			m_perChannelElementsData = new VisualElement();
			m_perChannelElementsData.name = "PerChannelElementsData";
			BrushSelected( brush );
			channelComponents.Add( m_perChannelElementsToggles );
			channelComponents.Add( m_perChannelElementsData );
			
			root.Add( miscData );
			root.Add( channelComponents );

			m_controller.BrushController.OnChannelAddedEvent += OnControlChannelAdded;
			m_controller.BrushController.OnChannelRemovedEvent += OnControlChannelRemoved;
			m_controller.BrushController.OnChannelUpdatedEvent += OnControlnChannelUpdated;
			m_controller.BrushController.OnChannelsResetEvent += OnControlChannelsResetEvent;
		}

		private void OnControlChannelsResetEvent()
		{
			m_perChannelElementsToggles.Clear();
			m_perChannelElementsData.Clear();
			m_perChannelElements.Clear();
		}

		private void OnControlnChannelUpdated( int idx, APBrush brush )
		{
			( (ToolbarToggle)m_perChannelElementsToggles[ idx ] ).text = brush.ComponentsPerChannel[ idx ].ChannelId;
			( (Label)m_perChannelElements[ idx ][ 0 ] ).text = brush.ComponentsPerChannel[ idx ].ChannelId;
		}

		private void OnControlChannelRemoved( int idx, APBrush brush )
		{
			m_perChannelElementsToggles.RemoveAt( idx );
			if( m_perChannelElementsData.Contains( m_perChannelElements[ idx ] ))
				m_perChannelElementsData.Remove( m_perChannelElements[ idx ] );

			m_perChannelElements.RemoveAt( idx );
		}

		private void AddChannelComponent( int idx, APBrush brush )
		{
			SerializedObject channel = new SerializedObject( brush.ComponentsPerChannel[ idx ] );

			VisualElement colGroup = new VisualElement();
			colGroup.style.flexDirection = FlexDirection.Row;

			SerializedProperty colorProperty = channel.FindProperty( "Color" );
			ColorField colorField = new ColorField( /*"Color"*/ ) { value = colorProperty.colorValue, showAlpha = false };
			colorField.BindProperty( colorProperty );
			colorField.style.flexGrow = 0.5f;

			SerializedProperty texProperty = channel.FindProperty( "BrushTexture" );
			ObjectField textureField = new ObjectField( /*"Texture"*/ ) { value = texProperty.objectReferenceValue };
			textureField.objectType = typeof( Texture2D );
			textureField.BindProperty( texProperty );
			textureField.RegisterCallback<ChangeEvent<UnityEngine.Object>>( evt => {
				if( evt.previousValue != evt.newValue )
				{
					if( evt.newValue == null )
					{
						colGroup.Insert( 0, colorField );
					}
					else
					{
						if( colorField.parent != null )
							colorField.parent.Remove( colorField );
					}
				}
			} );
			textureField.style.flexGrow = 1f;

			if( texProperty.objectReferenceValue == null )
				colGroup.Add( colorField );

			colGroup.Add( textureField );
			m_perChannelElements[ idx ].Add( colGroup );
			if( brush.CanPaintOnChannel( idx ) )
				m_perChannelElementsData.Add( m_perChannelElements[ idx ] );
		}

		private void OnControlChannelAdded( int idx, APBrush brush )
		{
			string name = brush.ComponentsPerChannel[ idx ].ChannelId;
			ToolbarToggle channelToggle = new ToolbarToggle() { text = name };
			channelToggle.AddToClassList( "ComponentToggle" );
			channelToggle.Q<Label>().style.minWidth = 0;
			
			channelToggle.SetValueWithoutNotify( brush.CanPaintOnChannel( idx ) );

			channelToggle.RegisterCallback<MouseDownEvent>( OnComponentClick, TrickleDown.TrickleDown );
			channelToggle.RegisterValueChangedCallback( OnChannelSelected );
			m_perChannelElementsToggles.Add( channelToggle );

			m_perChannelElements.Insert( idx, new VisualElement() );
			Label brushChannelLabel = new Label( name );
			brushChannelLabel.name = "BrushChannelLabel";
			m_perChannelElements[ idx ].Add( brushChannelLabel );

			AddChannelComponent( idx, brush );
		}

		void BrushSelected( APBrush brush )
		{
			m_perChannelElementsToggles.Clear();
			for( int i = 0; i < brush.ComponentsPerChannel.Count; i++ )
			{
				//string name = brush.Special ? brush.ComponentsPerChannel[ i ].ChannelId : "Channel " + i;
				string name = brush.ComponentsPerChannel[ i ].ChannelId ;
				ToolbarToggle channelToggle = new ToolbarToggle() { text = name };
				channelToggle.AddToClassList( "ComponentToggle" );
				channelToggle.Q<Label>().style.minWidth = 0;
				
				channelToggle.SetValueWithoutNotify( brush.CanPaintOnChannel( i ) );
				channelToggle.RegisterCallback<MouseDownEvent>( OnComponentClick, TrickleDown.TrickleDown );
				channelToggle.RegisterValueChangedCallback( OnChannelSelected );
				m_perChannelElementsToggles.Add( channelToggle );
			}
			
			m_perChannelElementsData.Clear();
			m_perChannelElements.Clear();
			
			for( int i = 0; i < brush.ComponentsPerChannel.Count; i++ )
			{
				string name = brush.ComponentsPerChannel[ i ].ChannelId ;
				m_perChannelElements.Add( new VisualElement());
				Label brushChannelLabel = new Label( name );
				brushChannelLabel.name = "BrushChannelLabel";
				m_perChannelElements[ i ].Add( brushChannelLabel );

				AddChannelComponent( i, brush );
			}
		}

		void OnComponentClick( MouseDownEvent e )
		{
			ToolbarToggle toggle = e.target as ToolbarToggle;
			int toggleIdx = m_perChannelElementsToggles.IndexOf( toggle );

			bool toggleValue = toggle.value;

			bool allOn = false;
			if( e.altKey )
			{
				allOn = true;
				for( int i = 0; i < m_perChannelElementsToggles.childCount; i++ )
				{
					ToolbarToggle tog = m_perChannelElementsToggles.ElementAt( i ) as ToolbarToggle;
					if( !tog.value )
					{
						allOn = false;
						break;
					}
				}
			}

			if( (e.altKey && !toggleValue) || allOn )
			{
				if( !allOn )
				{
					if( !toggle.value )
					{
						toggle.SetValueWithoutNotify( true );
						m_controller.BrushController.CurrentBrush.SetAllowedChannel( toggleIdx );
						m_perChannelElementsData.Insert( Mathf.Clamp( toggleIdx, 0, m_perChannelElementsData.childCount ), m_perChannelElements[ toggleIdx ] );
					}
					else
					{
						toggle.SetValueWithoutNotify( false );
						m_controller.BrushController.CurrentBrush.SetLockedChannel( toggleIdx );
						if( m_perChannelElementsData.Contains( m_perChannelElements[ toggleIdx ] ))
							m_perChannelElementsData.Remove( m_perChannelElements[ toggleIdx ] );
					}
				}

				for( int i = 0; i < m_perChannelElementsToggles.childCount; i++ )
				{
					ToolbarToggle tog = m_perChannelElementsToggles.ElementAt( i ) as ToolbarToggle;
					if( i != toggleIdx )
					{
						if( tog.value )
						{
							tog.SetValueWithoutNotify( false );
							m_controller.BrushController.CurrentBrush.SetLockedChannel( i );
							if( m_perChannelElementsData.Contains( m_perChannelElements[ i ] ))
								m_perChannelElementsData.Remove( m_perChannelElements[ i ] );
						}
					}
				}
			}
			else if( e.altKey && toggleValue )
			{
				for( int i = 0; i < m_perChannelElementsToggles.childCount; i++ )
				{
					ToolbarToggle tog = m_perChannelElementsToggles.ElementAt( i ) as ToolbarToggle;
					if( !tog.value )
					{
						tog.SetValueWithoutNotify( true );
						m_controller.BrushController.CurrentBrush.SetAllowedChannel( i );
						m_perChannelElementsData.Insert( Mathf.Clamp( i, 0, m_perChannelElementsData.childCount ), m_perChannelElements[ i ] );
					}
				}
			}
			else
			{
				if( !toggle.value )
				{
					toggle.SetValueWithoutNotify( true );
					m_controller.BrushController.CurrentBrush.SetAllowedChannel( toggleIdx );
					m_perChannelElementsData.Insert( Mathf.Clamp( toggleIdx, 0, m_perChannelElementsData.childCount ), m_perChannelElements[ toggleIdx ] );
				}
				else
				{
					toggle.SetValueWithoutNotify( false );
					m_controller.BrushController.CurrentBrush.SetLockedChannel( toggleIdx );
					if( m_perChannelElementsData.Contains( m_perChannelElements[ toggleIdx ] ))
						m_perChannelElementsData.Remove( m_perChannelElements[ toggleIdx ] );
				}
			}
		}

		void OnChannelSelected( ChangeEvent<bool> eventCall )
		{
			ToolbarToggle toggle = eventCall.target as ToolbarToggle;
			int toggleIdx = m_perChannelElementsToggles.IndexOf( toggle );
			
			bool isolation = true;
			for( int i = 0; i < m_perChannelElementsToggles.childCount; i++ )
			{
				ToolbarToggle tog = m_perChannelElementsToggles.ElementAt( i ) as ToolbarToggle;
				if( i != toggleIdx )
				{
					if( tog.value )
					{
						isolation = false;
						tog.SetValueWithoutNotify( false );
						m_controller.BrushController.CurrentBrush.SetLockedChannel( i );
						if( m_perChannelElementsData.Contains( m_perChannelElements[ i ] ))
							m_perChannelElementsData.Remove( m_perChannelElements[ i ] );	
					}
				}
				else
				{
					if( toggle.value )
					{
						m_controller.BrushController.CurrentBrush.SetAllowedChannel( toggleIdx );
						m_perChannelElementsData.Insert( Mathf.Clamp( toggleIdx, 0, m_perChannelElementsData.childCount ), m_perChannelElements[ toggleIdx ] );
					}
					else
					{
						m_controller.BrushController.CurrentBrush.SetLockedChannel( toggleIdx );
						if( m_perChannelElementsData.Contains( m_perChannelElements[ toggleIdx ] ))
							m_perChannelElementsData.Remove( m_perChannelElements[ toggleIdx ] );
					}
				}
			}

			if( !isolation )
			{
				toggle.SetValueWithoutNotify( true );
				m_controller.BrushController.CurrentBrush.SetAllowedChannel( toggleIdx );
				m_perChannelElementsData.Insert( Mathf.Clamp( toggleIdx, 0, m_perChannelElementsData.childCount ), m_perChannelElements[ toggleIdx ] );
			}
		}

		public override void OnDisable()
		{
			base.OnDisable();
			m_controller.BrushController.OnChannelAddedEvent -= OnControlChannelAdded;
			m_controller.BrushController.OnChannelRemovedEvent -= OnControlChannelRemoved;
			m_controller.BrushController.OnChannelUpdatedEvent -= OnControlnChannelUpdated;
			//m_controller.BrushController.OnBrushAddedEvent -= OnBrushAdded;
		}

		//public void ShowColorPicker()
		//{
		//	ColorPickerEx.Show( ( x ) => { m_colorProperty.colorValue = x; m_colorProperty.serializedObject.ApplyModifiedProperties(); }, m_colorProperty.colorValue, false, false );
		//}

		private void RegisterSpacing( VisualElement root )
		{
			if( (StrokeMethod)m_methodProperty.enumValueIndex == StrokeMethod.Space )
			{
				// add
				if( !root.Contains( m_spacingGroup ) )
				{
					int index = root.IndexOf( m_enumField );
					root.Insert( index + 1, m_spacingGroup );
				}
			}
			else
			{
				// remove
				if( root.Contains( m_spacingGroup ) )
					root.Remove( m_spacingGroup );
			}
		}

		private void Update()
		{
			// create a condition here or else...
			m_enumField.SetValueWithoutNotify( (StrokeMethod)m_methodProperty.enumValueIndex );
			m_alignmentField.SetValueWithoutNotify( (ProjectionAlignment)m_alignmentProperty.enumValueIndex );
		}
	}
}
