// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyPainter
{
	public sealed class APChannelsGrid : ScrollView
	{
		public delegate void SelectedElementOnGrid( int channelIdx, int layerIdx );
		public event SelectedElementOnGrid OnSelectedElementOnGrid;

		public delegate void SelectedChannel( int channelIDX );
		public event SelectedChannel OnSelectedChannelEvent;

		private List<VisualElement> m_columns = new List<VisualElement>();
		private Foldout m_foldout;
		private VisualElement m_elementsAnchor;
		public APChannelsGrid( APChannelController channelController ) : base( ScrollViewMode.VerticalAndHorizontal )
		{
			this.styleSheets.Add( APResources.MainStyleSheet );
			name = "ChannelGrid";
			m_foldout = new Foldout();
			m_foldout.text = "Active Layers";
			m_elementsAnchor = new VisualElement();
			m_elementsAnchor.style.flexDirection = FlexDirection.Row;
			m_foldout.Add( m_elementsAnchor );
			Add( m_foldout );

			RefreshAll( channelController );
		}

		public void RefreshAll( APChannelController channelController )
		{
			m_elementsAnchor.Clear();
			if( m_columns != null )
			{
				for( int channelIdx = 0; channelIdx < m_columns.Count; channelIdx++ )
				{
					if( m_columns[ channelIdx ] != null )
					{
						m_columns[ channelIdx ].Clear();
						m_columns[ channelIdx ] = null;
					}
				}
				m_columns.Clear();
			}

			if( channelController.ChannelCount > 0 )
			{
				for( int channelIdx = 0; channelIdx < channelController.ChannelCount; channelIdx++ )
				{
					APChannel channel = channelController.AvailableChannels[ channelIdx ];
					VisualElement newChannel = new VisualElement();
					newChannel.name = "ChannelGridColumn";
					newChannel.userData = channelIdx;
					//int cidx = channelIdx;
					Button channelButton = new Button( () => { if( OnSelectedChannelEvent != null ) OnSelectedChannelEvent( channel.CurrentIdx ); } );
					channelButton.text = channel.Name;
					newChannel.Add( channelButton );
					for( int layerIdx = 0; layerIdx < channel.Layers.Count; layerIdx++ )
					{
						Toggle toggle = new Toggle();
						toggle.value = ( layerIdx == channel.CurrentLayerIdx );
						toggle.RegisterValueChangedCallback( OnLayerSelected );
						toggle.userData = layerIdx;
						newChannel.Add( toggle );
					}
					m_columns.Add( newChannel );
					m_elementsAnchor.Add( m_columns[ channelIdx ] );
				}
			}
		}

		public void ClearAll()
		{
			m_elementsAnchor.Clear();
			if( m_columns != null )
			{
				for( int channelIdx = 0; channelIdx < m_columns.Count; channelIdx++ )
				{
					if( m_columns[ channelIdx ] != null )
					{
						m_columns[ channelIdx ].Clear();
						m_columns[ channelIdx ] = null;
					}
				}
				m_columns.Clear();
			}
		}

		public void RefreshChannelLabel( int channelIdx, APChannel channel )
		{
			( (Button)m_columns[ channelIdx ][ 0 ] ).text = channel.Name;
		}

		public void AddChannel( APChannel channel )
		{
			VisualElement newChannel = new VisualElement();
			newChannel.name = "ChannelGridColumn";
			newChannel.userData = channel.CurrentIdx;
			Button channelButton = new Button(() => { if( OnSelectedChannelEvent != null ) OnSelectedChannelEvent( channel.CurrentIdx ); } );
			
			channelButton.text = channel.Name;
			newChannel.Add( channelButton );
			for( int layerIdx = 0; layerIdx < channel.Layers.Count; layerIdx++ )
			{
				Toggle toggle = new Toggle();
				toggle.value = ( layerIdx == channel.CurrentLayerIdx );
				toggle.RegisterValueChangedCallback( OnLayerSelected );
				toggle.userData = layerIdx;
				newChannel.Add( toggle );
			}

			m_columns.Insert( channel.CurrentIdx, newChannel );
			m_elementsAnchor.Insert( channel.CurrentIdx, m_columns[ channel.CurrentIdx ] );
		}

		public void AddLayer( APChannel channel , APParentLayer layer, bool selected )
		{
			Toggle toggle = new Toggle();
			toggle.value = selected;
			toggle.RegisterValueChangedCallback( OnLayerSelected );
			toggle.userData = m_columns[ channel.CurrentIdx ].childCount - 1;
			m_columns[ channel.CurrentIdx ].Add( toggle );
		}

		public void RemoveChannel( int channelIdx )
		{
			m_columns[ channelIdx ].Clear();
			m_columns.RemoveAt( channelIdx );
			m_elementsAnchor.RemoveAt( channelIdx );
			for( int i = channelIdx; i < m_columns.Count; i++ )
			{
				m_columns[ channelIdx ].userData = i;
			}
		}

		public void RefreshChannel( APChannel channel )
		{
			int cidx = channel.CurrentIdx;
			if( m_columns[ cidx ] != null )
			{
				m_columns[ cidx ].Clear();
			}
			Button channelButton = new Button( () => { if( OnSelectedChannelEvent != null ) OnSelectedChannelEvent( cidx ); } );
			channelButton.text = channel.Name;
			m_columns[ cidx ].Add( channelButton );
			for( int layerIdx = 0; layerIdx < channel.Layers.Count; layerIdx++ )
			{
				Toggle toggle = new Toggle();
				toggle.value = ( layerIdx == channel.CurrentLayerIdx );
				toggle.RegisterValueChangedCallback( OnLayerSelected );
				toggle.userData = layerIdx;
				m_columns[ cidx ].Add( toggle );
			}
		}

		void OnLayerSelected( ChangeEvent<bool> eventCall )
		{
			Toggle toggle = eventCall.target as Toggle;
			int layerIdx = (int)toggle.userData;
			int channelIdx = (int)toggle.parent.userData;
			int toggleIdx = layerIdx + 1;
			int childCount = toggle.parent.childCount;

			// Channel label is at 0
			// Toggles start at 1
			for( int i = 1; i < toggle.parent.childCount; i++ )
			{
				if( i != toggleIdx )
					( (Toggle)toggle.parent.ElementAt( i ) ).SetValueWithoutNotify( false );
			}

			if( OnSelectedElementOnGrid != null )
			{
				OnSelectedElementOnGrid( channelIdx, ( toggle.value ? layerIdx : -1 ) );
			}
		}

		public void UpdateChannelSelection( int channelIdx, int layerIdx, bool value )
		{
			int childCount = m_columns[ channelIdx ].childCount;
			layerIdx += 1;
			for( int i = 1; i < childCount; i++ )
			{
				( (Toggle)m_columns[ channelIdx ].ElementAt( i ) ).SetValueWithoutNotify( i == layerIdx ? value : false );
			}
		}
	}
}
