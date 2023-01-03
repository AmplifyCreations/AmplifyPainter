// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AmplifyPainter
{
	public sealed class APLayersChannelView : ScrollView
	{
		private List<APLayerElementView> m_layers;
		private APReorderableList m_layersAnchor;

		public APLayersChannelView( APLayersWindow parent,
									 APReorderableList.OnOrderChanged reoderLayerEvt )
		{
			
			this.name = "Layers";

			//Layers
			m_layersAnchor = new APReorderableList( parent, false );
			m_layers = new List<APLayerElementView>();
			m_layersAnchor.OnOrderChangedEvent += OnReorder;
			m_layersAnchor.OnOrderChangedEvent += reoderLayerEvt;
			Add( m_layersAnchor );
		}

		void OnReorder( int oldId, int newId, bool switchMode )
		{
			if( switchMode )
			{
				m_layers[ oldId ] = m_layersAnchor[ oldId ] as APLayerElementView;
				m_layers[ newId ] = m_layersAnchor[ newId ] as APLayerElementView;
			}
			else
			{
				m_layers.RemoveAt( oldId );
				m_layers.Insert( newId, m_layersAnchor[ newId ] as APLayerElementView );
			}
		}

		public void AddLayer( APLayerElementView layer )
		{
			m_layers.Add( layer );
			m_layersAnchor.AddItem( layer );
		}

		public void InsertLayer( int layerIdx, APLayerElementView layerData )
		{
			m_layers.Insert( layerIdx, layerData );
			m_layersAnchor.InsertItem( layerIdx, layerData );
		}

		public void RemoveLayer( int layerIdx )
		{
			m_layers[ layerIdx ].Destroy();
			m_layers.RemoveAt( layerIdx );
			m_layersAnchor.RemoveItemAt( layerIdx );
		}

		public void Destroy()
		{
			for( int i = 0; i < m_layers.Count; i++ )
			{
				m_layers[ i ].Destroy();
			}
			m_layers.Clear();
			m_layers = null;
			m_layersAnchor.Destroy();
			Clear();
		}

		public void Reset()
		{
			for( int i = 0; i < m_layers.Count; i++ )
			{
				m_layers[ i ].Destroy();
			}
			m_layers.Clear();
			m_layersAnchor.ClearItems();
		}

		public List<APLayerElementView> Layers { get { return m_layers; } }
	}
}
