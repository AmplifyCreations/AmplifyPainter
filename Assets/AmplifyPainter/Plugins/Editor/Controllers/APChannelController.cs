// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APChannelController : ScriptableObject
	{
		public static int LayerOpacityPass = (int)APLayerBlendMode.VividLight + 1;
		public static List<int> AvailableSizes = new List<int>{ 128, 256, 512, 1024, 2048, 4096 };

		public delegate void ControllerToViewChannelUpdate( int idx, APChannel channel );
		public event ControllerToViewChannelUpdate OnChannelAdded;
		public event ControllerToViewChannelUpdate OnChannelRemoved;
		public event ControllerToViewChannelUpdate OnChannelUpdated;
		public event Action OnLayersResized;

		private int m_bottomLayerTexId;
		private int m_layerOpacityId;

		[SerializeField]
		private List<APChannel> m_availableChannels;

		[SerializeField]
		private int m_defaultWidth = 512;

		[SerializeField]
		private int m_defaultHeight = 512;

		[SerializeField]
		private bool m_lockedRatio = true;

		[SerializeField]
		private RenderTextureFormat m_defaultFormat;

		[SerializeField]
		private int m_currentChannel = 0;

		[SerializeField]
		private uint m_uniqueIdGenerator = 0;

		[SerializeField]
		private int m_lastPassId = 0;
		public void Init( int defaultWidth, int defaultHeight, RenderTextureFormat defaultFormat )
		{
			m_defaultWidth = defaultWidth;
			m_defaultHeight = defaultHeight;
			m_defaultFormat = defaultFormat;
			m_availableChannels = new List<APChannel>();
			//m_bottomLayerTexId = Shader.PropertyToID( "_SecondLayer" );
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
			m_bottomLayerTexId = Shader.PropertyToID( "_SecondLayer" );
			m_layerOpacityId = Shader.PropertyToID( "_Opacity" );
			m_lastPassId = Shader.PropertyToID( "_LastLayer" );
		}
		#region Layers
		public int LayerCount()
		{
			return m_availableChannels[ m_currentChannel ].Layers.Count;
		}

		public int LayerCount( int channelIdx )
		{
			if( channelIdx >= m_availableChannels.Count )
				return 0;

			return m_availableChannels[ channelIdx ].Layers.Count;
		}

		public APParentLayer LayerFromChannel( int channelIdx, int layerIdx )
		{
			if( channelIdx >= m_availableChannels.Count )
				return null;

			return m_availableChannels[ channelIdx ].GetLayer( layerIdx );
		}

		public APParentLayer GetLayer( int layerIdx )
		{
			return m_availableChannels[ m_currentChannel ].GetLayer( layerIdx );
		}

		public APParentLayer AddLayerToChannel( APLayerType layerType, int channelIdx, int layerIdx, bool clearLayer )
		{
			if( channelIdx >= m_availableChannels.Count )
				return null;

			switch( layerType )
			{
				default:
				case APLayerType.Default:
				{
					Color defaultColor;
					if( clearLayer )
					{
						defaultColor = Color.clear;
					}
					else
					{
						if( m_availableChannels[ channelIdx ].Template != null )
						{
							defaultColor = m_availableChannels[ channelIdx ].Template.InitialColorValue;
						}
						else
						{
							defaultColor = m_availableChannels[ channelIdx ].Type == ChannelType.Normal ? new Color( 0.5f, 0.5f, 1.0f, 1.0f ) : Color.black;
						}
					}
					return m_availableChannels[ channelIdx ].AddLayer( layerIdx, defaultColor );
				}
				case APLayerType.Texture2D: return m_availableChannels[ channelIdx ].AddTextureLayer( layerIdx );
				case APLayerType.Shader: return m_availableChannels[ channelIdx ].AddShaderLayer( layerIdx );
			}
		}

		public void RemoveLayerFromChannel( int channelIdx, int layerIdx )
		{
			if( channelIdx >= m_availableChannels.Count )
				return;

			m_availableChannels[ channelIdx ].RemoveLayer( layerIdx );
		}

		public bool UpdateCurrentLayerOnChannel( int channelIdx, int oldLayerIdx )
		{
			if( m_availableChannels[ channelIdx ].Layers.Count == 0 )
				return false;

			if( oldLayerIdx < m_availableChannels[ channelIdx ].Layers.Count )
			{
				m_availableChannels[ channelIdx ].CurrentLayerIdx = oldLayerIdx;
			}
			else
			{
				m_availableChannels[ channelIdx ].CurrentLayerIdx = m_availableChannels[ channelIdx ].Layers.Count - 1;
			}

			return true;
		}

		public bool DropObjectsOnLayer( int layerIdx, ref bool reassignBindings, UnityEngine.Object[] objects )
		{
			if( layerIdx >= m_availableChannels[ m_currentChannel ].Layers.Count )
				return false;

			APDropResult dropResult = m_availableChannels[ m_currentChannel ].Layers[ layerIdx ].OnDroppedObjects( objects );
			switch( dropResult )
			{
				case APDropResult.Fail:
				default: return false;
				case APDropResult.Success:
				{
					reassignBindings = false;
				}
				break;
				case APDropResult.SwitchToTexture:
				{
					DestroyImmediate( m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] );
					APTextureLayer newLayer = CreateInstance<APTextureLayer>();
					newLayer.Init( objects[ 0 ] as Texture2D, m_defaultFormat );
					m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] = newLayer;
					reassignBindings = true;
				}
				break;
				case APDropResult.SwitchToShader:
				{
					DestroyImmediate( m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] );
					APShaderLayer newLayer = CreateInstance<APShaderLayer>();
					newLayer.Init( objects[ 0 ] as Shader, m_defaultFormat, DefaultWidth, DefaultHeight );
					m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] = newLayer;
					reassignBindings = true;
				}
				break;
				case APDropResult.SwitchToMaterial:
				{
					DestroyImmediate( m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] );
					APShaderLayer newLayer = CreateInstance<APShaderLayer>();
					newLayer.Init( objects[ 0 ] as Material, m_defaultFormat, DefaultWidth, DefaultHeight );
					m_availableChannels[ m_currentChannel ].Layers[ layerIdx ] = newLayer;
					reassignBindings = true;
				}
				break;
			}

			UpdateValue();
			return true;
		}

		#endregion

		#region Channels
		string UniqueId
		{
			get { return "APChannel" + m_uniqueIdGenerator++; }
		}

		public APChannel AddChannel( APTextureProperty property = null )
		{
			if( property != null && HasProperty( property ) )
				return null;

			APChannel channel = CreateInstance<APChannel>();
			if( property != null )
			{
				if( property.Value != null )
				{
					channel.Init( UniqueId, property, m_defaultFormat, property.Value.width, property.Value.height );
				}
				else
				{
					channel.Init( UniqueId, property, m_defaultFormat, DefaultWidth, DefaultHeight );
				}
			}
			else
			{
				channel.Init( UniqueId, "My new channel " + m_availableChannels.Count, m_defaultFormat, DefaultWidth, DefaultHeight );
			}
			channel.CurrentIdx = m_availableChannels.Count;
			m_availableChannels.Add( channel );
			//SortChannels();
			if( OnChannelAdded != null )
				OnChannelAdded( m_availableChannels.Count - 1, channel );

			return channel;
		}

		public APChannel AddChannel( APChannelTemplateItem item )
		{
			APChannel channel = CreateInstance<APChannel>();
			channel.Init( UniqueId, item.Name, m_defaultFormat, DefaultWidth, DefaultHeight, item.Type );
			channel.Template = item;
			channel.CurrentIdx = m_availableChannels.Count;
			m_availableChannels.Add( channel );
			if( OnChannelAdded != null )
				OnChannelAdded( m_availableChannels.Count - 1, channel );
			return channel;
		}

		public void RemoveChannel( APTextureProperty property )
		{
			for( int i = 0; i < m_availableChannels.Count; i++ )
			{
				if( m_availableChannels[ i ].Property == property )
				{
					APChannel channel = m_availableChannels[ i ];
					m_availableChannels.RemoveAt( i );

					GenerateChannelsIdx( i );

					if( OnChannelRemoved != null )
						OnChannelRemoved( i, channel );

					DestroyImmediate( channel );
					//SortChannels();
					return;
				}
			}
		}

		public bool RemoveChannel( int idx = -1 )
		{
			if( idx < 0 )
				idx = Mathf.Max( 0, m_availableChannels.Count - 1 );

			if( idx >= m_availableChannels.Count )
				return false;

			APChannel channel = m_availableChannels[ idx ];

			m_availableChannels.RemoveAt( idx );
			GenerateChannelsIdx( idx );

			if( OnChannelRemoved != null )
				OnChannelRemoved( idx, channel );

			DestroyImmediate( channel );
			//SortChannels();
			return true;
		}

		public void FireDataChangedNotification( int idx )
		{
			if( idx < m_availableChannels.Count && OnChannelUpdated != null )
			{
				OnChannelUpdated( idx, m_availableChannels[ idx ] );
			}
		}

		void GenerateChannelsIdx( int startIdx = 0 )
		{
			for( int i = startIdx; i < m_availableChannels.Count; i++ )
			{
				m_availableChannels[ i ].CurrentIdx = i;
			}
		}
		void SortChannels()
		{
			m_availableChannels.Sort( ( x, y ) => x.Property.Id.CompareTo( y.Property.Id ) );
		}

		public bool HasProperty( APTextureProperty property )
		{
			return m_availableChannels.Find( ( x ) => x.Property == property );
		}

		public APChannel GetChannelByName( string name )
		{
			return m_availableChannels.Find( ( x ) => x.Name.Equals( name ) );
		}

		public void ResetChannels()
		{
			m_currentChannel = 0;

			if( m_availableChannels != null )
			{
				for( int i = 0; i < m_availableChannels.Count; i++ )
				{
					DestroyImmediate( m_availableChannels[ i ] );
				}
				m_availableChannels.Clear();
			}
		}

		public void SetLayerOnChannel( int channelIdx, int layerIdx )
		{
			m_availableChannels[ channelIdx ].CurrentLayerIdx = layerIdx;
		}

		public void ReorderLayers( int oldIdx, int newIdx, bool switchMode )
		{
			m_availableChannels[ m_currentChannel ].ReorderLayers( oldIdx, newIdx, switchMode );
			UpdateValue();
		}

		public void DumpChannels()
		{
			string dump = "Current Channels\n";
			for( int i = 0; i < m_availableChannels.Count; i++ )
			{
				dump += string.Format( "{0}: {1}\n", i, m_availableChannels[ i ].Property.Name );
			}
			Debug.Log( dump );
		}
		#endregion

		public Texture2D GetDefaultValueFor( APLayerBlendMode mode )
		{
			switch( mode )
			{
				default:
				case APLayerBlendMode.Normal: return Texture2D.blackTexture;
				case APLayerBlendMode.Multiply: return Texture2D.whiteTexture;
				case APLayerBlendMode.ColorBurn: return Texture2D.whiteTexture;
				case APLayerBlendMode.ColorDodge: return Texture2D.whiteTexture;
				case APLayerBlendMode.Darken: return Texture2D.whiteTexture;
				case APLayerBlendMode.Divide: return Texture2D.whiteTexture;
				case APLayerBlendMode.Difference: return Texture2D.whiteTexture;
				case APLayerBlendMode.Exclusion: return Texture2D.whiteTexture;
				case APLayerBlendMode.SoftLight: return Texture2D.whiteTexture;
				case APLayerBlendMode.HardLight: return Texture2D.whiteTexture;
				case APLayerBlendMode.HardMix: return Texture2D.whiteTexture;
				case APLayerBlendMode.Lighten: return Texture2D.whiteTexture;
				case APLayerBlendMode.LinearBurn: return Texture2D.whiteTexture;
				case APLayerBlendMode.LinearDodge: return Texture2D.whiteTexture;
				case APLayerBlendMode.LinearLight: return Texture2D.whiteTexture;
				case APLayerBlendMode.Overlay: return Texture2D.whiteTexture;
				case APLayerBlendMode.PinLight: return Texture2D.whiteTexture;
				case APLayerBlendMode.Subtract: return Texture2D.whiteTexture;
				case APLayerBlendMode.Screen: return Texture2D.whiteTexture;
				case APLayerBlendMode.VividLight: return Texture2D.whiteTexture;
			}
		}

		public void RefreshLayerSize()
		{
			for( int channelIdx = 0; channelIdx < m_availableChannels.Count; channelIdx++ )
			{
				for( int layerIdx = 0; layerIdx < m_availableChannels[ channelIdx ].Layers.Count; layerIdx++ )
				{
					if( m_availableChannels[ channelIdx ].Layers[ layerIdx ].IsEditable )
					{
						m_availableChannels[ channelIdx ].Layers[ layerIdx ].SetSize( DefaultWidth, DefaultHeight );
					}
				}
				m_availableChannels[ channelIdx ].SetSize( DefaultWidth, DefaultHeight );
				UpdateValue( channelIdx );
			}

			if( OnLayersResized != null )
				OnLayersResized();
		}

		public void RefreshLayerOnChannel( int layerIdx, int currentChannel = -1 )
		{
			if( currentChannel < 0 )
				currentChannel = m_currentChannel;

			m_availableChannels[ currentChannel ].Layers[ layerIdx ].RefreshValue();
		}

		public bool UpdateValue( int currentChannel = -1 )
		{
			if( currentChannel < 0 )
				currentChannel = m_currentChannel;

			if( m_availableChannels.Count == 0 || currentChannel >= m_availableChannels.Count )
			{
				return false;
			}

			RenderTexture cache = RenderTexture.active;
			RenderTexture.active = null;

			List<APParentLayer> layers = m_availableChannels[ currentChannel ].Layers;
			int layerCount = layers.Count;
			if( layerCount == 0 )
			{
				Graphics.Blit( Texture2D.blackTexture, m_availableChannels[ currentChannel ].Value );
				RenderTexture.active = cache;
				return true;
			}

			if( layerCount == 1 )
			{
				APResources.BlendModesMaterial.SetFloat( m_lastPassId, 1.0f );
				float opacity = m_availableChannels[ currentChannel ].Layers[ 0 ].Opacity * 0.01f;
				APResources.BlendModesMaterial.SetFloat( m_layerOpacityId, opacity );
				Graphics.Blit( layers[ 0 ].Value, m_availableChannels[ currentChannel ].Value, APResources.BlendModesMaterial, LayerOpacityPass );
				RenderTexture.active = cache;

				return true;
			}

			RenderTexture tempRT00 = RenderTexture.GetTemporary( m_availableChannels[ currentChannel ].Width, m_availableChannels[ currentChannel ].Height, 0, m_availableChannels[ currentChannel ].Format );
			RenderTexture tempRT01 = RenderTexture.GetTemporary( m_availableChannels[ currentChannel ].Width, m_availableChannels[ currentChannel ].Height, 0, m_availableChannels[ currentChannel ].Format );
			int count = 0;
			Texture topLayer = null;
			RenderTexture bottomLayer = null;
			RenderTexture target = null;

			for( int i = layerCount - 1; i > -1; i--, count++ )
			{
				APParentLayer topLayerData = m_availableChannels[ currentChannel ].Layers[ i ];
				topLayer = topLayerData.Value;
				int passId = 0;
				if( count == 0 )
				{
					passId = LayerOpacityPass;
					target = tempRT00;
				}
				else if( count % 2 != 0 )
				{
					passId = (int)topLayerData.BlendMode;
					bottomLayer = tempRT00;
					target = tempRT01;
				}
				else
				{
					passId = (int)topLayerData.BlendMode;
					bottomLayer = tempRT01;
					target = tempRT00;
				}

				APResources.BlendModesMaterial.SetTexture( m_bottomLayerTexId, bottomLayer );
				APResources.BlendModesMaterial.SetFloat( m_layerOpacityId, topLayerData.Opacity * 0.01f );
				if( i == 0 )
				{
					APResources.BlendModesMaterial.SetFloat( m_lastPassId, 1.0f );
					Graphics.Blit( topLayer, m_availableChannels[ currentChannel ].Value, APResources.BlendModesMaterial, passId );
				}
				else
				{
					APResources.BlendModesMaterial.SetFloat( m_lastPassId, 0.0f );
					Graphics.Blit( topLayer, target, APResources.BlendModesMaterial, passId );
				}
			}

			//Graphics.Blit( target, m_availableChannels[ currentChannel ].Value );
			RenderTexture.ReleaseTemporary( tempRT00 );
			RenderTexture.ReleaseTemporary( tempRT01 );
			RenderTexture.active = cache;
			return true;
		}

		private void OnDisable()
		{
			OnLayersResized = null;
		}

		public void OnDestroy()
		{
			for( int i = 0; i < m_availableChannels.Count; i++ )
			{
				DestroyImmediate( m_availableChannels[ i ] );
			}

			m_availableChannels.Clear();
			m_availableChannels = null;
		}

		public void SetLastChannel()
		{
			m_currentChannel = m_availableChannels.Count - 1;
		}

		public void IncrementCurrentChannel()
		{
			if( m_availableChannels.Count > 0 )
			{
				m_currentChannel = ( m_currentChannel + 1 ) % m_availableChannels.Count;
			}
		}

		public void DecrementCurrentChannel()
		{
			if( m_availableChannels.Count > 0 )
			{
				m_currentChannel = ( m_currentChannel > 0 ) ? m_currentChannel - 1 : m_availableChannels.Count - 1;
			}
		}

		public bool IsLastChannel { get { return m_currentChannel == m_availableChannels.Count - 1; } }
		public bool IsFirstChannel { get { return m_currentChannel == 0; } }

		public bool Locked
		{
			get
			{
				return ( m_currentChannel >= m_availableChannels.Count ) ||
						( m_availableChannels[ m_currentChannel ].GetCurrentLayer() == null ) ||
						( !m_availableChannels[ m_currentChannel ].GetCurrentLayer().IsEditable );
			}
		}

		public int DefaultWidth { get { return m_defaultWidth; } }
		public int DefaultHeight { get { return m_lockedRatio ? m_defaultWidth : m_defaultHeight; } }

		public RenderTextureFormat DefaultFormat { get { return m_defaultFormat; } }

		public APParentLayer CurrentLayer { get { return m_availableChannels[ m_currentChannel ].GetCurrentLayer(); } }
		public APChannel CurrentChannelData { get { return m_availableChannels[ m_currentChannel ]; } }
		public List<APChannel> AvailableChannels { get { return m_availableChannels; } }
		public int ChannelCount { get { return m_availableChannels.Count; } }
		public int CurrentChannelIdx { get { return m_currentChannel; } set { m_currentChannel = value; } }
		public int CurrentLayerIdx
		{
			get
			{
				return m_currentChannel < m_availableChannels.Count ? m_availableChannels[ m_currentChannel ].CurrentLayerIdx : 0;
			}
			set
			{
				if( m_currentChannel < m_availableChannels.Count )
					m_availableChannels[ m_currentChannel ].CurrentLayerIdx = value;
			}
		}
	}
}
