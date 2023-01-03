// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyPainter
{
	public enum StrokeMethod
	{
		AirBrush = 0,
		Space = 1
	}

	public enum ProjectionAlignment
	{
		Camera = 0,
		TangentWrap = 1,
		TangentPlanar = 2,
		UV = 3
	}

	[Serializable]
	public class PenPressure
	{
		[SerializeField]
		public float Value;

		[SerializeField]
		public bool PenActive;

		public float InterpValue;
		public float InitialValue;
		public AnimationCurve RemapCurve;

		[SerializeField]
		protected float m_pressure;

		public PenPressure( float val )
		{
			Value = val;
			InterpValue = val;
			InitialValue = val;

			PenActive = false;
			RemapCurve = new AnimationCurve( new Keyframe( 0, 0 ), new Keyframe( 1, 1 ) );
		}

		public void UpdatePressure( float pressure )
		{
			m_pressure = pressure;
		}

		public float ShaderValue
		{
			get
			{
				if( !PenActive )
					return Value;

				return InterpValue; // returns pressure value
			}
		}

		public virtual float PressureValue { get { return Value * m_pressure; } }
	}

	[Serializable]
	public sealed class BrushSize : PenPressure
	{
		public BrushSize( float size ) : base( size ) { }
	}

	[Serializable]
	public sealed class BrushFlow : PenPressure
	{
		public BrushFlow( float strength ) : base( strength ) { }
	}

	[Serializable]
	public sealed class BrushRotation : PenPressure
	{
		public BrushRotation( float rotation ) : base( rotation ) { }

		public override float PressureValue { get { return Value - 360 * m_pressure; } }
	}

	[Serializable]
	public sealed class BrushSpacing : PenPressure
	{
		public BrushSpacing( float spacing ) : base( spacing ) { }

		public override float PressureValue { get { return Mathf.Max( 0.02f, Value * m_pressure ); } }
	}

	[Serializable]
	public sealed class APBrushComponentsPerChannel : ScriptableObject
	{
		public string ChannelId;

		[SerializeField]
		public Color Color;

		[SerializeField]
		public Texture2D BrushTexture;

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}
	}

	[Serializable]
	public sealed class APBrush : ScriptableObject
	{
		public string Name;
		//public bool Special;
		public List<APBrushComponentsPerChannel> ComponentsPerChannel;

		// Global, for when the no channels exist
		//[SerializeField]
		//private Color Color;

		// Global, for when the no channels exist
		//[SerializeField]
		//private Texture2D BrushTexture;

		[SerializeField]
		public BrushSize Size;

		[SerializeField]
		public BrushFlow Strength;

		[SerializeField]
		[Angle( 1, 0, 360 )]
		public BrushRotation Rotation;

		[SerializeField]
		public StrokeMethod Method;

		[SerializeField]
		public BrushSpacing Spacing;

		[SerializeField]
		public ProjectionAlignment Alignment;

		[SerializeField]
		public bool BackCulling;

		[SerializeField]
		public Texture2D Mask;

		//Bit array indicating which channels to apply brush
		public int Coverage = 1;//int.MaxValue;

		public int FirstAllowedChannel = 0;

		private static Material m_brushMaterial;

		
		public Material BrushMaterial
		{
			get
			{
				if( m_brushMaterial == null )
				{
					m_brushMaterial = new Material( Shader.Find( "Hidden/AmplifyPainter/BrushProjection" ) )
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return m_brushMaterial;
			}
		}
		// ENDTODO
		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		private void FindFirstAllowed()
		{
			for( int i = 0; i < ComponentsPerChannel.Count; i++ )
			{
				if( CanPaintOnChannel( i ) )
				{
					FirstAllowedChannel = i;
					return;
				}
			}
		}

		public bool CanPaintOnChannel( int idx )
		{
			idx = 1 << idx;
			return ( idx & Coverage ) > 0;
		}

		public void LockAllChannels()
		{
			Coverage = 0;
			FirstAllowedChannel = 0;
		}

		public void AllowAllChannels()
		{
			Coverage = int.MaxValue;
			FirstAllowedChannel = 0;
		}

		public void SetAllowedChannel( int idx )
		{
			idx = 1 << idx;
			Coverage = ( idx | Coverage );
			FindFirstAllowed();
		}

		public void SetLockedChannel( int idx )
		{
			idx = ~(1 << idx);
			Coverage = ( idx & Coverage );
			FindFirstAllowed();
		}

		public Color GetColor( int channelIdx )
		{
			if( ComponentsPerChannel.Count <= 0 )
				return Color.black;
			else
				return ComponentsPerChannel[ Mathf.Clamp( channelIdx, 0, ComponentsPerChannel.Count - 1 ) ].Color.linear;
		}

		public Texture2D GetTexture( int channelIdx )
		{
			if( ComponentsPerChannel.Count <= 0 )
				return null;
			else
				return ComponentsPerChannel[ Mathf.Clamp( channelIdx, 0, ComponentsPerChannel.Count - 1 ) ].BrushTexture;
		}

		public Color GetFirstAllowedColor()
		{
			if( ComponentsPerChannel.Count <= 0 )
				return Color.black;
			else
				return ComponentsPerChannel[ Mathf.Clamp( FirstAllowedChannel, 0, ComponentsPerChannel.Count - 1 ) ].Color;
		}

		public Texture2D GetFirstAllowedTexture()
		{
			if( ComponentsPerChannel.Count <= 0 )
				return null;
			else
				return ComponentsPerChannel[ Mathf.Clamp( FirstAllowedChannel, 0, ComponentsPerChannel.Count - 1 ) ].BrushTexture;
		}

	}
}
