// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APTexturePropertyController : ScriptableObject
	{
		[SerializeField]
		private Material m_currentMaterial;

		[SerializeField]
		private List<APTextureProperty> m_availableProperties;

		public void Init()
		{
			m_availableProperties = new List<APTextureProperty>();
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public void ResetTexturesOnMaterial()
		{
			if( m_currentMaterial != null )
			{
				for( int i = 0; i < m_availableProperties.Count; i++ )
				{
					m_currentMaterial.SetTexture( m_availableProperties[ i ].Name, m_availableProperties[ i ].Value ); ;
				}
			}
		}

		public APTextureProperty FindProperty( ShaderPropertyType property )
		{
			string propertyName = property.ToString();
			return m_availableProperties.Find(( x ) => x.Name.Equals( propertyName ));
		}

		public void SetProperties( Material material )
		{
			ResetTexturesOnMaterial();
			for( int i = 0; i < m_availableProperties.Count; i++ )
			{
				DestroyImmediate( m_availableProperties[ i ] );
			}
			m_availableProperties.Clear();

			m_currentMaterial = material;

			int propertyCount = ShaderUtil.GetPropertyCount( material.shader );
			int currId = 0;
			for( int i = 0; i < propertyCount; i++ )
			{
				if( ShaderUtil.GetPropertyType( material.shader, i ) == ShaderUtil.ShaderPropertyType.TexEnv &&
					!ShaderUtil.IsShaderPropertyHidden( material.shader, i ) )
				{
					string name = ShaderUtil.GetPropertyName( material.shader, i );
					string description = ShaderUtil.GetPropertyDescription( material.shader, i );
					APTextureProperty property = CreateInstance<APTextureProperty>();
					property.Init( currId++, name, description, material, material.GetTexture( name ) );
					m_availableProperties.Add( property );
				}
			}
		}

		public void ResetContainer()
		{
			ResetTexturesOnMaterial();
			for( int i = 0; i < m_availableProperties.Count; i++ )
			{
				DestroyImmediate( m_availableProperties[ i ] );
			}
			m_availableProperties.Clear();

			m_currentMaterial = null;
		}

		public void OnDestroy()
		{
			ResetTexturesOnMaterial();

			m_currentMaterial = null;

			for( int i = 0; i < m_availableProperties.Count; i++ )
			{
				DestroyImmediate( m_availableProperties[ i ] );
			}

			m_availableProperties.Clear();
			m_availableProperties = null;
		}

		public void Dump()
		{
			for( int i = 0; i < m_availableProperties.Count; i++ )
			{
				Debug.LogFormat( "{0}: {1}", m_availableProperties[ i ].Description, m_availableProperties[ i ].IsChannel );
			}
		}

		public List<APTextureProperty> AvailableProperties { get { return m_availableProperties; } }

		public Material CurrentMaterial { get { return m_currentMaterial; } }
	}
}
