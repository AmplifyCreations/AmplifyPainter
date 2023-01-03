// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyPainter
{
	public enum ShaderPropertyType
	{
		Custom,
		_MainTex,
		_MetallicGlossMap,
		_BumpMap,
		_ParallaxMap,
		_OcclusionMap,
		_EmissionMap,
		_DetailMask,
		_DetailAlbedoMap,
		_DetailNormalMap,
		_SpecGlossMap
	};

	public enum ShaderType
	{
		Custom,
		StandardMetallic,
		StandardSpecular
	}

	//public enum TemplateChannelType
	//{
	//	Albedo,
	//	Transparency,
	//	Metallic,
	//	Specular,
	//	Smoothness,
	//	NormalMap,
	//	Heightmap,
	//	Occlusion,
	//	DetailMask,
	//	DetailAlbedo,
	//	DetailNormalMap
	//}

	[Serializable]
	public class APChannelTemplateItem
	{
		//public TemplateChannelType Type;
		public string Name;
		public string PropertyName;
		public ShaderPropertyType Property;
		public Vector4 ColorMask;
		public ChannelType Type;
		public bool Enabled;
		public Color InitialColorValue;
	}

	[Serializable]
	public class APAvailableTemplate
	{
		public ShaderType Type;
		public List<APChannelTemplateItem> Channels;
		private Dictionary<ShaderPropertyType, List<APChannelTemplateItem>> m_dict;

		public Dictionary<ShaderPropertyType, List<APChannelTemplateItem>> Dict
		{
			get
			{
				if( m_dict == null )
				{
					m_dict = new Dictionary<ShaderPropertyType, List<APChannelTemplateItem>>();
					for( int i = 0; i < Channels.Count; i++ )
					{
						if( !m_dict.ContainsKey( Channels[ i ].Property ) )
						{
							m_dict.Add( Channels[ i ].Property, new List<APChannelTemplateItem>() );
						}
						m_dict[ Channels[ i ].Property ].Add( Channels[ i ] );
					}
				}
				return m_dict;
			}
		}
	}

	public static class APAvailableTemplatesContainer
	{
		public static string[] MaskNames = {"_AMask",
											"_BMask",
											"_CMask",
											"_DMask"};

		public static string[] TexNames = { "_ATex",
											"_BTex",
											"_CTex",
											"_DTex"};

		public static Dictionary<ShaderType, string> ShaderTypeToKey = new Dictionary<ShaderType, string>
		{

			{ShaderType.Custom,string.Empty },
			{ShaderType.StandardMetallic,"Standard"} ,
			{ShaderType.StandardSpecular,"Standard (Specular setup)"}
		};

		public static Dictionary<string, APAvailableTemplate> AvailableTemplates = new Dictionary<string, APAvailableTemplate>
		{
			{
				ShaderTypeToKey[ShaderType.StandardMetallic],new APAvailableTemplate()
				{
					Type = ShaderType.StandardMetallic,
					Channels = new List<APChannelTemplateItem>()
					{
						new APChannelTemplateItem(){ Name = "Albedo",           PropertyName="_MainTex",        Type = ChannelType.Color, ColorMask = new Vector4(1,1,1,0),InitialColorValue = Color.white, Property = ShaderPropertyType._MainTex , Enabled = true},
						new APChannelTemplateItem(){ Name = "Alpha",            PropertyName="_AlphaMap",       Type = ChannelType.Color, ColorMask = new Vector4(0,0,0,1),InitialColorValue = Color.white, Property = ShaderPropertyType._MainTex , Enabled = true},
						new APChannelTemplateItem(){ Name = "Metallic",         PropertyName="_MetallicMap",    Type = ChannelType.Color, ColorMask = new Vector4(1,0,0,0),InitialColorValue = Color.black, Property = ShaderPropertyType._MetallicGlossMap, Enabled = true},
						new APChannelTemplateItem(){ Name = "Smooth",           PropertyName="_SmoothnessMap",  Type = ChannelType.Color, ColorMask = new Vector4(0,0,0,1),InitialColorValue = Color.gray,	Property = ShaderPropertyType._MetallicGlossMap, Enabled = true},
						new APChannelTemplateItem(){ Name = "Normal",           PropertyName="_BumpMap",        Type = ChannelType.Normal,ColorMask = new Vector4(1,1,1,0),InitialColorValue = new Color( 0.5f, 0.5f, 1.0f, 1.0f ), Property = ShaderPropertyType._BumpMap, Enabled = true},
						//new APChannelTemplateItem(){ Name = "Heightmap",        PropertyName="_Heightmap",      Type = ChannelType.Color, ColorMask = new Vector4(0,1,0,0), Property = ShaderPropertyType._ParallaxMap },
						new APChannelTemplateItem(){ Name = "Occlusion",        PropertyName="_OcclusionMap",   Type = ChannelType.Color, ColorMask = new Vector4(0,1,0,0), InitialColorValue = Color.white,Property = ShaderPropertyType._OcclusionMap, Enabled = true },
						new APChannelTemplateItem(){ Name = "Emission",         PropertyName="_EmissionMap",    Type = ChannelType.Color, ColorMask = new Vector4(1,1,1,0), InitialColorValue = Color.black,Property = ShaderPropertyType._EmissionMap, Enabled = true },
						//new APChannelTemplateItem(){ Name = "Detail Mask",      PropertyName="_DetailMask",     Type = ChannelType.Color, ColorMask = new Vector4(0,0,0,1), Property = ShaderPropertyType._DetailMask },
						//new APChannelTemplateItem(){ Name = "Detail Albedo",    PropertyName="_DetailAlbedo",   Type = ChannelType.Color, ColorMask = new Vector4(1,1,1,0), Property = ShaderPropertyType._DetailAlbedoMap },
						//new APChannelTemplateItem(){ Name = "Detail Normal Map",PropertyName="_DetailNormalMap",Type = ChannelType.Normal,ColorMask = new Vector4(1,1,1,0), Property = ShaderPropertyType._DetailNormalMap },
					}
				}
			},
			{
				ShaderTypeToKey[ShaderType.StandardSpecular], new APAvailableTemplate()
				{
					Type = ShaderType.StandardSpecular,
					Channels = new List<APChannelTemplateItem>()
					{
						new APChannelTemplateItem(){ Name = "Albedo",           PropertyName = "_MainTex",         Type = ChannelType.Color,    ColorMask = new Vector4(1,1,1,0),InitialColorValue = Color.white,   Property = ShaderPropertyType._MainTex, Enabled = true },
						new APChannelTemplateItem(){ Name = "Alpha",            PropertyName = "_AlphaMap",        Type = ChannelType.Color,    ColorMask = new Vector4(0,0,0,1),InitialColorValue = Color.white,   Property = ShaderPropertyType._MainTex, Enabled = true },
						new APChannelTemplateItem(){ Name = "Specular",         PropertyName = "_SpecGlossMap",    Type = ChannelType.Color,    ColorMask = new Vector4(1,1,1,0),InitialColorValue = new Color(0.2f,0.2f,0.2f,1.0f), Property = ShaderPropertyType._SpecGlossMap, Enabled = true },
						new APChannelTemplateItem(){ Name = "Smooth",           PropertyName = "_SmoothnessMap",   Type = ChannelType.Color,    ColorMask = new Vector4(0,0,0,1),InitialColorValue = Color.gray, Property = ShaderPropertyType._SpecGlossMap, Enabled = true },
						new APChannelTemplateItem(){ Name = "Normal",           PropertyName = "_BumpMap",         Type = ChannelType.Normal,   ColorMask = new Vector4(1,1,1,0),InitialColorValue = new Color( 0.5f, 0.5f, 1.0f, 1.0f ),   Property = ShaderPropertyType._BumpMap, Enabled = true },
						//new APChannelTemplateItem(){ Name = "Heightmap",        PropertyName = "_Heightmap",       Type = ChannelType.Color,    ColorMask = new Vector4(0,1,0,0),   Property = ShaderPropertyType._ParallaxMap },
						new APChannelTemplateItem(){ Name = "Occlusion",        PropertyName = "_OcclusionMap",    Type = ChannelType.Color,    ColorMask = new Vector4(0,1,0,0), InitialColorValue = Color.white,   Property = ShaderPropertyType._OcclusionMap, Enabled = true },
						new APChannelTemplateItem(){ Name = "Emission",         PropertyName = "_EmissionMap",     Type = ChannelType.Color,    ColorMask = new Vector4(1,1,1,0), InitialColorValue = Color.black,  Property = ShaderPropertyType._EmissionMap, Enabled = true },
						//new APChannelTemplateItem(){ Name = "Detail Mask",      PropertyName = "_DetailMask",      Type = ChannelType.Color,    ColorMask = new Vector4(0,0,0,1),   Property = ShaderPropertyType._DetailMask },
						//new APChannelTemplateItem(){ Name = "Detail Albedo",    PropertyName = "_DetailAlbedo",    Type = ChannelType.Color,    ColorMask = new Vector4(1,1,1,0),   Property = ShaderPropertyType._DetailAlbedoMap },
						//new APChannelTemplateItem(){ Name = "Detail Normal Map",PropertyName = "_DetailNormal Map",Type = ChannelType.Normal,   ColorMask = new Vector4(1,1,1,0),   Property = ShaderPropertyType._DetailNormalMap },
					}
				}
			}
		};
	}
}
