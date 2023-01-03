// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APBrushController : ScriptableObject
	{
		public delegate void ControllerToViewChannelUpdate( int idx, APBrush brush );
		public event ControllerToViewChannelUpdate OnChannelAddedEvent;
		public event ControllerToViewChannelUpdate OnChannelRemovedEvent;
		public event ControllerToViewChannelUpdate OnChannelUpdatedEvent;
		public event Action OnChannelsResetEvent;

		public delegate void OnBrushAdded( APBrush brush );
		public event OnBrushAdded OnBrushAddedEvent;

		public delegate void BrushSelected( int brushIdx, APBrush brush );
		public BrushSelected OnBrushSelectedEvent;

		[SerializeField]
		private List<APBrush> m_availableBrushes;

		[SerializeField]
		private APTabletData m_currentTablet;

		[SerializeField]
		private int m_currentBrush = 0;

		private static Vector4 s_auxVec4 = Vector4.zero;

		public void Init()
		{
			m_availableBrushes = new List<APBrush>();
			m_currentTablet = CreateInstance<APTabletData>();
		}

		public void OnDestroy()
		{
			for( int i = 0; i < m_availableBrushes.Count; i++ )
			{
				DestroyImmediate( m_availableBrushes[ i ] );
			}
			m_availableBrushes.Clear();
			m_availableBrushes = null;

			DestroyImmediate( m_currentTablet );
			m_currentTablet = null;
		}

		//public APBrush AddBrush()
		//{
		//	APBrush brush = CreateInstance<APBrush>();
		//	brush.ComponentsPerChannel = new List<APBrushComponentsPerChannel>();
		//	brush.ComponentsPerChannel.Add( CreateInstance<APBrushComponentsPerChannel>());
		//	brush.ComponentsPerChannel[ 0 ].Color = Color.black;
		//	brush.Size = new BrushSize( 1f );
		//	brush.Strength = new BrushFlow( 1f );
		//	brush.Rotation = new BrushRotation( 0f );
		//	brush.Method = StrokeMethod.Space;
		//	brush.Spacing = new BrushSpacing( 0.2f );
		//	brush.Mask = APResources.GuassianTexture;
		//	brush.BackCulling = true;
		//	brush.Alignment = ProjectionAlignment.TangentWrap;
		//	m_availableBrushes.Add( brush );
		//	if( OnBrushAddedEvent != null )
		//		OnBrushAddedEvent( null );
		//	return brush;
		//}

		//public APBrush AddBrush( Color color, BrushSize size, BrushFlow strength, StrokeMethod method, BrushSpacing spacing, BrushRotation rotation, ProjectionAlignment alignment )
		//{
		//	APBrush brush = CreateInstance<APBrush>();
		//	brush.ComponentsPerChannel = new List<APBrushComponentsPerChannel>();
		//	brush.ComponentsPerChannel.Add( CreateInstance<APBrushComponentsPerChannel>());
		//	brush.ComponentsPerChannel[ 0 ].Color = color;
		//	brush.Size = size;
		//	brush.Strength = strength;
		//	brush.Method = method;
		//	brush.Spacing = spacing;
		//	brush.Rotation = rotation;
		//	brush.BackCulling = true;
		//	brush.Alignment = alignment;
		//	m_availableBrushes.Add( brush );
		//	if( OnBrushAddedEvent != null )
		//		OnBrushAddedEvent( null );
		//	return brush;
		//}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public void AddBrush( APBrush brush )
		{
			m_availableBrushes.Add( brush );
			if( OnBrushAddedEvent != null )
				OnBrushAddedEvent( null );
		}

		public bool CanPaintOnChannel( APChannel channel )
		{
			return m_availableBrushes[ m_currentBrush ].CanPaintOnChannel( channel.CurrentIdx );
		}


		// Projection Settings <cull, wrap, empty, empty>
		public Vector4 GetProjectionSettingsVector()
		{
			s_auxVec4.x = CurrentBrush.BackCulling ? 1 : 0;
			s_auxVec4.y = CurrentBrush.Alignment == ProjectionAlignment.TangentWrap ? 1 : 0;
			s_auxVec4.z = 0;
			s_auxVec4.w = 0;
			return s_auxVec4;
		}

		// Brush Settings <radius, opacity, angle, empty>
		public Vector4 GetBrushSettingsVector( bool shaderValues )
		{
			if( shaderValues )
			{
				s_auxVec4.x = CurrentBrush.Size.ShaderValue * 0.5f;
				s_auxVec4.y = CurrentBrush.Strength.ShaderValue;
				s_auxVec4.z = CurrentBrush.Rotation.ShaderValue * Mathf.Deg2Rad;
				s_auxVec4.w = 0;
			}
			else
			{
				s_auxVec4.x = CurrentBrush.Size.Value * 0.5f;
				s_auxVec4.y = CurrentBrush.Strength.Value;
				s_auxVec4.z = CurrentBrush.Rotation.Value * Mathf.Deg2Rad;
				s_auxVec4.w = 0;
			}
			return s_auxVec4;
		}

		// Texel Params (needed??)
		public Vector4 GetTexelParamsVector( int channelIdx )
		{
			s_auxVec4.x = 1f / CurrentBrush.GetTexture( channelIdx ).width;
			s_auxVec4.y = 1f / CurrentBrush.GetTexture( channelIdx ).height;
			s_auxVec4.z = CurrentBrush.GetTexture( channelIdx ).width;
			s_auxVec4.w = CurrentBrush.GetTexture( channelIdx ).height;
			return s_auxVec4;
		}

		public void Update()
		{
			// Update tablet data
			if( m_currentTablet != null )
				m_currentTablet.UpdateData();

			CurrentBrush.Size.UpdatePressure( m_currentTablet.Pressure );
			CurrentBrush.Strength.UpdatePressure( m_currentTablet.Pressure );
			CurrentBrush.Rotation.UpdatePressure( m_currentTablet.Pressure );
			CurrentBrush.Spacing.UpdatePressure( m_currentTablet.Pressure );
		}

		public void StartPressureInterpolation()
		{
			CurrentBrush.Size.InitialValue = CurrentBrush.Size.InterpValue;
			CurrentBrush.Strength.InitialValue = CurrentBrush.Strength.InterpValue;
			CurrentBrush.Rotation.InitialValue = CurrentBrush.Rotation.InterpValue;
			CurrentBrush.Spacing.InitialValue = CurrentBrush.Spacing.InterpValue;
		}

		public void CalculatePressure()
		{
			CurrentBrush.Size.InterpValue = CurrentBrush.Size.PressureValue;
			CurrentBrush.Strength.InterpValue = CurrentBrush.Strength.PressureValue;
			CurrentBrush.Rotation.InterpValue = CurrentBrush.Rotation.PressureValue;
			CurrentBrush.Spacing.InterpValue = CurrentBrush.Spacing.PressureValue;
		}

		public void CalculatePressure( float interpolation )
		{
			if( CurrentBrush.Spacing.PenActive )
				CurrentBrush.Spacing.InterpValue = Mathf.Lerp( CurrentBrush.Spacing.InitialValue, CurrentBrush.Spacing.PressureValue, interpolation );

			if( CurrentBrush.Size.PenActive )
				CurrentBrush.Size.InterpValue = Mathf.Lerp( CurrentBrush.Size.InitialValue, CurrentBrush.Size.PressureValue, interpolation );

			if( CurrentBrush.Strength.PenActive )
				CurrentBrush.Strength.InterpValue = Mathf.Lerp( CurrentBrush.Strength.InitialValue, CurrentBrush.Strength.PressureValue, interpolation );

			if( CurrentBrush.Rotation.PenActive )
				CurrentBrush.Rotation.InterpValue = Mathf.Lerp( CurrentBrush.Rotation.InitialValue, CurrentBrush.Rotation.PressureValue, interpolation );
		}

		public void EndPressureInterpolation()
		{
			CurrentBrush.Size.InitialValue = CurrentBrush.Size.InterpValue;
			CurrentBrush.Strength.InitialValue = CurrentBrush.Strength.InterpValue;
			CurrentBrush.Rotation.InitialValue = CurrentBrush.Rotation.InterpValue;
			CurrentBrush.Spacing.InitialValue = CurrentBrush.Spacing.InterpValue;
		}

		public float BrushDistance()
		{
			if( CurrentBrush.Size.PenActive )
				return CurrentBrush.Spacing.ShaderValue * CurrentBrush.Size.InterpValue;
			else
				return CurrentBrush.Spacing.ShaderValue * CurrentBrush.Size.Value;
		}

		public void AdjustSize( float delta )
		{
			CurrentBrush.Size.Value = Mathf.Max( 0, CurrentBrush.Size.Value + 0.1f / delta );
		}

		public void HookTablet()
		{
			if( m_currentTablet != null )
				m_currentTablet.Hook();
		}

		public void UnhookTablet()
		{
			if( m_currentTablet != null )
				m_currentTablet.Unhook();
		}

		public void SelectBrush( int brushIdx, bool generateNotification )
		{
			m_currentBrush = Mathf.Clamp( brushIdx, 0, m_availableBrushes.Count - 1 );
			if( generateNotification && OnBrushSelectedEvent != null )
				OnBrushSelectedEvent( m_currentBrush, m_availableBrushes[ m_currentBrush ] );
		}

		public void UpdateAllChannelsInfo( List<APChannel> channels, int brushIdx )
		{
			for( int i = 0; i < m_availableBrushes[ brushIdx ].ComponentsPerChannel.Count; i++ )
			{
				DestroyImmediate( m_availableBrushes[ brushIdx ].ComponentsPerChannel[ i ] );
			}
			m_availableBrushes[ brushIdx ].ComponentsPerChannel.Clear();

			int channelsCount = channels.Count;
			m_availableBrushes[ brushIdx ].ComponentsPerChannel = new List<APBrushComponentsPerChannel>();

			for( int i = 0; i < channelsCount; i++ )
			{
				m_availableBrushes[ brushIdx ].ComponentsPerChannel.Add( CreateInstance<APBrushComponentsPerChannel>());
				m_availableBrushes[ brushIdx ].ComponentsPerChannel[ i ].ChannelId = channels[ i ].Name;

				if( channels[ i ].Template != null )
				{
					m_availableBrushes[ brushIdx ].ComponentsPerChannel[ i ].Color = channels[ i ].Template.InitialColorValue;
				}
				else
				{
					bool isNormal = channels[ i ].Name.IndexOf( "Normal" ) >= 0;
					m_availableBrushes[ brushIdx ].ComponentsPerChannel[ i ].Color = isNormal ? new Color( 0.5f, 0.5f, 1.0f, 1.0f ) : Color.white;
				}
			}
			SelectBrush( brushIdx, true );
		}

		public void OnChannelsReset()
		{
			for( int i = 0; i < m_availableBrushes[ m_currentBrush ].ComponentsPerChannel.Count; i++ )
			{
				DestroyImmediate( m_availableBrushes[ m_currentBrush ].ComponentsPerChannel[ i ] );
			}
			m_availableBrushes[ m_currentBrush ].ComponentsPerChannel.Clear();

			if( OnChannelsResetEvent != null )
				OnChannelsResetEvent();
		}

		public void AddChannelToBrush( APChannel channel )
		{
			APBrushComponentsPerChannel info = CreateInstance<APBrushComponentsPerChannel>();
			info.ChannelId = channel.Name;
			if( channel.Template != null )
			{
				info.Color = channel.Template.InitialColorValue;
			}
			else
			{
				bool isNormal = channel.Name.IndexOf( "Normal" ) >= 0;
				info.Color = isNormal ? new Color( 0.5f, 0.5f, 1.0f, 1.0f ) : Color.white;
			}

			m_availableBrushes[ m_currentBrush ].ComponentsPerChannel.Insert( channel.CurrentIdx, info );
			if( OnChannelAddedEvent != null )
				OnChannelAddedEvent( channel.CurrentIdx, m_availableBrushes[ m_currentBrush ] );
		}

		public void RemoveChannelFromBrush( int channelIdx )
		{
			DestroyImmediate( m_availableBrushes[ m_currentBrush ].ComponentsPerChannel[ channelIdx ] );
			m_availableBrushes[ m_currentBrush ].ComponentsPerChannel.RemoveAt( channelIdx );
			if( OnChannelRemovedEvent != null )
				OnChannelRemovedEvent( channelIdx, m_availableBrushes[ m_currentBrush ] );
		}

		public void UpdateChannelInfoOnBrush( APChannel channel )
		{
			m_availableBrushes[ m_currentBrush ].ComponentsPerChannel[ channel.CurrentIdx ].ChannelId = channel.Name;
			if( OnChannelUpdatedEvent != null )
				OnChannelUpdatedEvent( channel.CurrentIdx, m_availableBrushes[ m_currentBrush ] );
		}

		public List<APBrush> AvailableBrushes { get { return m_availableBrushes; } }
		public APTabletData CurrentTablet { get { return m_currentTablet; } }
		public APBrush CurrentBrush { get { return AvailableBrushes[ m_currentBrush ]; } }
	}
}
