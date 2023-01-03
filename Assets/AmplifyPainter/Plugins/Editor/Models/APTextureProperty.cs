// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEngine;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APTextureProperty : ScriptableObject
	{
		public string Name;
		public string Description;
		public bool IsChannel;
		public Texture Value;
		public Material Material;
		public int Id;

		[SerializeField]
		public string FileName;

		public void Init( int id, string name, string description, Material material, Texture value )
		{
			Id = id;
			Name = name;
			Description = description;
			Value = value;
			IsChannel = false;
			FileName = "$property";
			Material = material;
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public void Reset()
		{
			IsChannel = false;
			if( Material != null && Material.HasProperty( Name ) )
			{
				Material.SetTexture( Name, Value );
			}
		}
	}
}
