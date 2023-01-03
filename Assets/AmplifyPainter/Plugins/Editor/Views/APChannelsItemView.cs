// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AmplifyPainter
{
	public sealed class APChannelsItemView : VisualElement
	{
		public delegate void OnChannelInfoChanged( int id );
		public event OnChannelInfoChanged OnChannelInfoChangedEvent;
		public event OnChannelInfoChanged OnChannelRemovedEvent;

		private TextField m_name;
		private EnumField m_channelType;
		private PopupField<APTextureProperty> m_availablePropertiesPopUp;

		private SerializedProperty m_channelTypeProperty;
		private SerializedProperty m_linkedProperty;
		private SerializedProperty m_channelId;

		//private int m_id;

		public APChannelsItemView( SerializedObject serializedObject, int id, List<APTextureProperty> propertiesList, ShaderType shaderType  )
		{
			//Registar OnChannelInfoChangedEvent na view correspondente para que ela chame o
			//FireDataChangedNotification do channels controller

			bool enable = shaderType == ShaderType.Custom;
			//m_id = id;
			this.styleSheets.Add( APResources.MainStyleSheet );
			name = "ChannelItem";
			m_channelId = serializedObject.FindProperty( "CurrentIdx" );
			SerializedProperty nameProperty = serializedObject.FindProperty( "Name" );
			m_name = new TextField() { value = nameProperty.stringValue };
			m_name.BindProperty( nameProperty );
			m_name.RegisterValueChangedCallback( OnNameModified );
			m_name.name = "ChannelItemName";
			m_name.SetEnabled( enable );
			Add( m_name );

			m_channelTypeProperty = serializedObject.FindProperty( "Type" );
			m_channelType = new EnumField();
			m_channelType.name = "ChannelItemType";
			m_channelType.Init( (ChannelType)m_channelTypeProperty.enumValueIndex );
			m_channelType.RegisterValueChangedCallback( OnChannelTypeSelected );
			m_channelType.SetEnabled( enable );
			Add( m_channelType );

			m_linkedProperty = serializedObject.FindProperty( "LinkedProperty" );
			APTextureProperty linkedProperty = m_linkedProperty.objectReferenceValue as APTextureProperty;
			int propertyId = ( linkedProperty != null ) ? linkedProperty.Id + 1 : 0;
			m_availablePropertiesPopUp = new PopupField<APTextureProperty>( string.Empty, propertiesList, propertiesList[ propertyId ], OnPropertySelected, OnPropertyDrawn );
			m_availablePropertiesPopUp.userData = 0;
			m_availablePropertiesPopUp.name = "ChannelItemProperties";
			m_availablePropertiesPopUp.SetEnabled( enable );
			Add( m_availablePropertiesPopUp );
			Button deleteButton = new Button( OnRemoveChannel );
			deleteButton.name = "ChannelItemDeleteButton";
			deleteButton.text = "-";
			deleteButton.SetEnabled( enable );
			Add( deleteButton );
		}

		void OnRemoveChannel()
		{
			if( OnChannelRemovedEvent != null )
				OnChannelRemovedEvent( m_channelId.intValue );
		}

		string OnPropertySelected( APTextureProperty property )
		{
			if( m_availablePropertiesPopUp != null )
			{
				if( m_linkedProperty.objectReferenceValue != property && !property.IsChannel )
				{
					//Reset old texture property
					if( m_linkedProperty.objectReferenceValue != null )
					{
						APTextureProperty oldProperty = m_linkedProperty.objectReferenceValue as APTextureProperty;
						oldProperty.Reset();
					}

					//Set new texture property
					if( m_availablePropertiesPopUp.index > 0 )
					{
						property.IsChannel = true;
						m_linkedProperty.objectReferenceValue = property;
					}
					else
					{
						//Custom channel without texture property
						m_linkedProperty.objectReferenceValue = null;
					}

					m_channelTypeProperty.serializedObject.ApplyModifiedProperties();
					//Save current valid index so it can be reverted
					m_availablePropertiesPopUp.userData = m_availablePropertiesPopUp.index;

					if( OnChannelInfoChangedEvent != null )
						OnChannelInfoChangedEvent( m_channelId.intValue );
				}
				else
				{
					m_availablePropertiesPopUp.index = (int)m_availablePropertiesPopUp.userData;
					return m_availablePropertiesPopUp.value.Name;
				}
			}

			return property.Name;
		}

		void OnChannelTypeSelected( ChangeEvent<Enum> eventCall )
		{
			m_channelTypeProperty.enumValueIndex = Convert.ToInt32( eventCall.newValue );
			m_channelTypeProperty.serializedObject.ApplyModifiedProperties();
			if( OnChannelInfoChangedEvent != null )
				OnChannelInfoChangedEvent( m_channelId.intValue );
		}

		void OnNameModified( ChangeEvent<string> eventCall )
		{
			if( OnChannelInfoChangedEvent != null )
				OnChannelInfoChangedEvent( m_channelId.intValue );
		}

		public void Reset()
		{
			m_availablePropertiesPopUp.index = 0;
		}

		string OnPropertyDrawn( APTextureProperty property )
		{
			return property.Name;
		}

		public void Destroy()
		{
			m_channelId = null;
			OnChannelRemovedEvent = null;
			OnChannelInfoChangedEvent = null;

			m_channelType.UnregisterValueChangedCallback( OnChannelTypeSelected );
			m_name.UnregisterValueChangedCallback( OnNameModified );
			m_channelTypeProperty = null;
			if( m_linkedProperty != null && m_linkedProperty.objectReferenceValue != null )
			{
				( (APTextureProperty)m_linkedProperty.objectReferenceValue ).Reset();
			}

			m_linkedProperty = null;
			Clear();
		}

		public TextField Name { get { return m_name; } }
		public EnumField ChannelType { get { return m_channelType; } }
		public PopupField<APTextureProperty> AvailablePropertiesPopUp { get { return m_availablePropertiesPopUp; } }

	}
}
