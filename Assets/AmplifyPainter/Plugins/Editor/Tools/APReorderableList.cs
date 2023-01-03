// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APReorderableList : VisualElement
	{
		class APItemDragger : MouseManipulator
		{
			public delegate void OnMouseUpdate( VisualElement target );
			public event OnMouseUpdate OnStoppedMovingEvent;
			public event OnMouseUpdate OnMovingEvent;

			private Vector2 m_Start;
			private bool m_Active;
			private float m_initialY;
			private float m_deltaY;
			private float m_offsetY;
			private VisualElement m_parent;
			private VisualElement m_horizontalLine;
			private int m_index;
			private List<VisualElement> m_elements;

			public APItemDragger( VisualElement parent )
			{
				m_parent = parent;
				activators.Add( new ManipulatorActivationFilter { button = MouseButton.LeftMouse } );
				m_Active = false;
			}

			protected override void RegisterCallbacksOnTarget()
			{
				target.RegisterCallback<MouseDownEvent>( OnMouseDown );
				target.RegisterCallback<MouseMoveEvent>( OnMouseMove );
				target.RegisterCallback<MouseUpEvent>( OnMouseUp );
			}

			protected override void UnregisterCallbacksFromTarget()
			{
				target.UnregisterCallback<MouseDownEvent>( OnMouseDown );
				target.UnregisterCallback<MouseMoveEvent>( OnMouseMove );
				target.UnregisterCallback<MouseUpEvent>( OnMouseUp );
			}

			protected void OnMouseDown( MouseDownEvent e )
			{
				if( m_Active )
				{
					e.StopImmediatePropagation();
					return;
				}

				if( CanStartManipulation( e ) )
				{
					m_Start = e.localMousePosition;
					m_initialY = target.layout.y;
					APReorderableList.m_initialY = m_initialY;
					m_deltaY = e.localMousePosition.y - m_initialY;
					m_Active = true;
					target.CaptureMouse();
					e.StopPropagation();
					m_index = m_parent.IndexOf( target );
					m_elements = (List<VisualElement>)m_parent.Children();

					//tricking Visual Elements to place selected one on top of all
					//but without notifying visual element to calculate new layout
					m_elements.Remove( target );
					m_elements.Add( target );
				}
			}

			protected void OnMouseMove( MouseMoveEvent e )
			{
				if( !m_Active || !target.HasMouseCapture() )
					return;
				target.AddToClassList( "ondrag" );
				Vector2 diff = e.localMousePosition - m_Start;
				float min = -m_initialY;
				float max = m_parent.layout.height - m_initialY - target.layout.height;
				target.style.top = Mathf.Clamp( target.layout.y + diff.y - m_initialY, min, max );

				e.StopPropagation();

				if( OnMovingEvent != null )
					OnMovingEvent( target );
			}

			protected void OnMouseUp( MouseUpEvent e )
			{
				if( !m_Active || !target.HasMouseCapture() || !CanStopManipulation( e ) )
					return;

				target.RemoveFromClassList( "ondrag" );

				m_Active = false;
				target.ReleaseMouse();
				e.StopPropagation();

				//Restoring initial positions on trick, 
				//OnStoppedMovingEvent will then deal with the reordering
				m_elements.Remove( target );
				m_elements.Insert( m_index, target );

				if( OnStoppedMovingEvent != null )
					OnStoppedMovingEvent( target );
			}

			public void Destroy()
			{
				target = null;
				m_parent = null;
				m_elements = null;
			}
		}
		public delegate void OnOrderChanged( int oldId, int newId, bool switchMode );
		public event OnOrderChanged OnOrderChangedEvent;

		private Dictionary<int, APItemDragger> m_itemDraggers = new Dictionary<int, APItemDragger>();

		public static float m_initialY;
		private APLayersWindow m_parent;
		private bool m_switchNode = false;
		public APReorderableList( APLayersWindow parent, bool switchNode ) { m_parent = parent; m_switchNode = switchNode; name = "LayerList"; }

		public APReorderableList( APLayersWindow parent, bool switchNode, VisualElement[] elements )
		{
			m_parent = parent;
			m_switchNode = switchNode;
			name = "LayerList";
			for( int i = 0; i < elements.Length; i++ )
			{
				APItemDragger dragger = new APItemDragger( this );
				m_itemDraggers.Add( elements[ i ].GetHashCode(), dragger );
				dragger.OnStoppedMovingEvent += OnItemStoppedMoving;
				dragger.OnMovingEvent += OnItemMoving;
				elements[ i ].AddManipulator( dragger );
				Add( elements[ i ] );
			}
		}

		public void AddItem( VisualElement item )
		{
			APItemDragger dragger = new APItemDragger( this );
			m_itemDraggers.Add( item.GetHashCode(), dragger );
			dragger.OnStoppedMovingEvent += OnItemStoppedMoving;
			dragger.OnMovingEvent += OnItemMoving;
			item.AddManipulator( dragger );
			Add( item );
		}

		public void InsertItem( int index, VisualElement item )
		{
			APItemDragger dragger = new APItemDragger( this );
			m_itemDraggers.Add( item.GetHashCode(), dragger );
			dragger.OnStoppedMovingEvent += OnItemStoppedMoving;
			dragger.OnMovingEvent += OnItemMoving;
			item.AddManipulator( dragger );
			Insert( index, item );
		}

		public void RemoveItem( VisualElement item )
		{
			int hashCode = item.GetHashCode();
			if( m_itemDraggers.ContainsKey( hashCode ) )
			{
				m_itemDraggers[ hashCode ].Destroy();
				m_itemDraggers.Remove( hashCode );
			}
			Remove( item );
		}

		public void RemoveItemAt( int index )
		{
			VisualElement item = ElementAt( index );
			int hashCode = item.GetHashCode();
			if( m_itemDraggers.ContainsKey( hashCode ) )
			{
				m_itemDraggers[ hashCode ].Destroy();
				m_itemDraggers.Remove( hashCode );
			}
			RemoveAt( index );
		}

		void OnItemStoppedMoving( VisualElement target )
		{
			List<VisualElement> children = (List<VisualElement>)Children();

			int targetIdx = children.IndexOf( target );
			int newIdx = -1;
			VisualElement switchChild = null;
			float targetY = target.layout.y;

			if( m_initialY < targetY )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
				{
					if( children[ i ].layout.y <= targetY )
					{
						switchChild = children[ i ];
						newIdx = i;
						break;
					}
				}
			}
			else if( m_initialY > targetY )
			{
				for( int i = 0; i < children.Count; i++ )
				{
					if( children[ i ].layout.y >= targetY )
					{
						switchChild = children[ i ];
						newIdx = i;
						break;
					}
				}
			}

			if( newIdx > -1 )
			{
				if( m_switchNode )
				{
					Remove( target );
					Remove( switchChild );

					if( newIdx < targetIdx )
					{
						Insert( newIdx, target );
						Insert( targetIdx, switchChild );
					}
					else
					{
						Insert( targetIdx, switchChild );
						Insert( newIdx, target );
					}
				}
				else
				{
					Remove( target );
					Insert( newIdx, target );
				}
			}

			foreach( VisualElement child in Children() )
			{
				child.style.top = 0;
			}
			m_parent.m_reorderAnchor.style.top = 0;
			m_parent.m_reorderAnchor.style.width = 0;
			MarkDirtyRepaint();

			if( newIdx > -1 && OnOrderChangedEvent != null )
				OnOrderChangedEvent( targetIdx, newIdx, m_switchNode );
		}

		void OnItemMoving( VisualElement target )
		{
			// TODO: Find a way to get the marginBottom value instead of using the value directly
			int borderSize = 1;
			float truheight = target.layout.height + borderSize;
			float marker = (int)(target.layout.y / truheight ) * truheight + truheight + 16;
			bool closeMark = marker - m_initialY > 0 && marker - m_initialY < 100;

			//float gridAdjust = m_parent.ChannelGrid.layout.height;
			//float gridAdjust = 24;
			float gridAdjust = m_parent.AvailableChannelsToggles.layout.height + 6;
			if( target.layout.y == 0 && m_initialY != 0)
			{
				m_parent.m_reorderAnchor.style.top = gridAdjust + 16;
				m_parent.m_reorderAnchor.style.width = target.layout.width - borderSize;
			}
			else
			if( !closeMark )
			{
				m_parent.m_reorderAnchor.style.top = marker + gridAdjust;
				m_parent.m_reorderAnchor.style.width = target.layout.width - borderSize;
			}
			else
			{
				m_parent.m_reorderAnchor.style.top = 0;
				m_parent.m_reorderAnchor.style.width = 0;
			}
		}

		public void ClearItems()
		{
			foreach( KeyValuePair<int, APItemDragger> kvp in m_itemDraggers )
			{
				kvp.Value.Destroy();
			}
			m_itemDraggers.Clear();
			Clear();
		}

		public void Destroy()
		{
			ClearItems();
			m_itemDraggers = null;
			OnOrderChangedEvent = null;
		}
	}
}
