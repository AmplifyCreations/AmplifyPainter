// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyPainter
{
	public enum ChannelType
	{
		Color,
		Normal
	};

	[Serializable]
	public sealed class APChannel : ScriptableObject
	{
		//TODO: This needs to go to brushes data model
		//TODO: none of these should be here, I'll remove them later when we optimize the painting steps for multiple channels (I'll leave the m_ for now)
		[NonSerialized]
		public float BrushTraveledDistance;

		[NonSerialized]
		public Vector3 BrushPreviousPos;

		[NonSerialized]
		public bool m_holdPainting = false;
		[NonSerialized]
		public Vector3 m_initialPos = Vector3.zero;
		[NonSerialized]
		public Vector3 m_initialNormal = Vector3.zero;
		[NonSerialized]
		public Vector3 m_initialTangent = Vector3.zero;
		[NonSerialized]
		public Vector2 m_initialTexcoord = Vector3.zero;
		[NonSerialized]
		public Vector2 m_initialMousePos2D = Vector2.zero;

		//////////////////////////////////////////////////////////////////

		[SerializeField]
		public string Name;

		[SerializeField]
		public ChannelType Type;

		[SerializeField]
		public APTextureProperty LinkedProperty;

		//TODO: Check a way to make APChannelTemplateItem a SerializableObject (it's currently static)
		// So a duplicate is not created during hotcode reload
		public APChannelTemplateItem Template;

		public int Width;

		public int Height;

		public RenderTextureFormat Format;

		public RenderTexture Value;

		public List<APParentLayer> Layers;

		public APTextureProperty CustomProperty;

		public string InternalUniqueId;

		public int CurrentLayerIdx;

		public int CurrentIdx;

		//DEBUG: SHOULD BE REMOVED ON A LATER STAGE
		public bool FromProperty;

		public void Init( string uniqueId, string name, RenderTextureFormat format, int width, int height, ChannelType type = ChannelType.Color )
		{
			Name = name;
			FromProperty = false;
			InternalUniqueId = uniqueId;
			CustomProperty = CreateInstance<APTextureProperty>();
			CustomProperty.Init( 0, name, name, null, null );
			Type = type;
			Format = format;
			Width = width;
			Height = height;
			Layers = new List<APParentLayer>();
			Value = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			Value.Create();
		}

		public void Init( string uniqueId, APTextureProperty linkedProperty, RenderTextureFormat format, int width, int height )
		{
			Name = linkedProperty.Description;
			FromProperty = false;
			InternalUniqueId = uniqueId;
			LinkedProperty = linkedProperty;

			CustomProperty = CreateInstance<APTextureProperty>();
			CustomProperty.Init( linkedProperty.Id, linkedProperty.Name, linkedProperty.Description, null, null );

			Type = ChannelType.Color;
			Format = format;
			Width = width;
			Height = height;
			Layers = new List<APParentLayer>();
			Value = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			Value.Create();
		}

		public APParentLayer GetCurrentLayer()
		{
			if( CurrentLayerIdx < 0 || ( Layers != null && CurrentLayerIdx >= Layers.Count ) )
				return null;

			return Layers[ CurrentLayerIdx ];
		}

		public APParentLayer GetLayer( int idx )
		{
			if( idx >= Layers.Count )
				return null;

			return Layers[ idx ];
		}

		public APLayer AddLayer( int idx, Color defaultColor )
		{
			APLayer layer = CreateInstance<APLayer>();
			layer.Init( Type, Format, Width, Height, defaultColor );
			Layers.Insert( idx, layer );
			layer.Name += Layers.Count;
			return layer;
		}

		public APTextureLayer AddTextureLayer( int idx )
		{
			APTextureLayer layer = CreateInstance<APTextureLayer>();
			layer.Init( Width, Height, Format );
			Layers.Insert( idx, layer );
			layer.Name += Layers.Count;
			return layer;
		}

		public APShaderLayer AddShaderLayer( int idx )
		{
			APShaderLayer layer = CreateInstance<APShaderLayer>();
			layer.Init( (Shader)null, Format, Width, Height );
			Layers.Insert( idx, layer );
			layer.Name += Layers.Count;
			return layer;
		}

		public void RemoveLayer( int idx )
		{
			if( idx >= Layers.Count )
				return;

			APParentLayer layer = Layers[ idx ];
			Layers.RemoveAt( idx );
			DestroyImmediate( layer );
		}

		public void ReorderLayers( int oldIdx, int newIdx, bool switchMode )
		{
			if( oldIdx >= Layers.Count || newIdx >= Layers.Count )
				return;

			if( switchMode )
			{
				APParentLayer temp = Layers[ oldIdx ];
				Layers[ oldIdx ] = Layers[ newIdx ];
				Layers[ newIdx ] = temp;
			}
			else
			{
				APParentLayer temp = Layers[ oldIdx ];
				Layers.RemoveAt( oldIdx );
				Layers.Insert( newIdx, temp );
			}
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public void OnDestroy()
		{
			if( CustomProperty != null )
			{
				DestroyImmediate( CustomProperty );
				CustomProperty = null;
			}

			for( int i = 0; i < Layers.Count; i++ )
			{
				DestroyImmediate( Layers[ i ] );
			}
			Layers.Clear();

			Value.Release();
			DestroyImmediate( Value );
			Value = null;
			Template = null;
			LinkedProperty = null;
		}

		public void SetSize( int width, int height )
		{
			Width = width;
			Height = height;
			Value = Value.SetSize( width, height );
		}

		public APTextureProperty Property { get { return ( LinkedProperty != null ) ? LinkedProperty : CustomProperty; } set { LinkedProperty = value; } }
		//public ChannelType Type { get { return m_channelType; } set { m_channelType = value; } }
		//public int Width { get { return m_width; } }
		//public int Height { get { return m_height; } }
		//public RenderTextureFormat Format { get { return m_renderFormat; } }
		//public List<APParentLayer> Layers { get { return m_layers; } }
		//public RenderTexture Value { get { return m_value; } }
		public string UniqueId { get { return ( FromProperty ) ? LinkedProperty.Name : InternalUniqueId; } }
		//public bool FromProperty { get { return m_fromProperty; } }
		//public string Name { get { return m_name; } }

		public bool ValidToDraw { get { return Layers.Count > 0 && CurrentIdx >= 0; } }
	}
}
