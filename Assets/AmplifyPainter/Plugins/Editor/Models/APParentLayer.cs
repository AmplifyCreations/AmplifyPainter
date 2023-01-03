// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyPainter
{
	public enum APLayerBlendMode
	{
		Normal = 0,
		Multiply,
		ColorBurn,
		ColorDodge,
		Darken,
		Divide,
		Difference,
		Exclusion,
		SoftLight,
		HardLight,
		HardMix,
		Lighten,
		LinearBurn,
		LinearDodge,
		LinearLight,
		Overlay,
		PinLight,
		Subtract,
		Screen,
		VividLight
	}

	public enum APLayerType
	{
		Default = 0,
		//SMORT
		Texture2D,
		Shader
	}

	public enum APDropResult
	{
		Success,
		Fail,
		SwitchToTexture,
		SwitchToShader,
		SwitchToMaterial
	}

	[Serializable]
	public class APParentLayer : ScriptableObject
	{
		[SerializeField]
		public float Opacity;

		[SerializeField]
		public APLayerBlendMode BlendMode;

		[SerializeField]
		public string Name;

		public bool IsEditable;

		public void BaseInit()
		{
			Opacity = 100;
			BlendMode = APLayerBlendMode.Normal;
			Name = "New Layer ";
		}

		private void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;
		}

		public virtual void RefreshValue() { }
		public virtual APDropResult OnDroppedObjects( UnityEngine.Object[] objects ) { return APDropResult.Fail; }
		public virtual Texture Value { get { return null; } }
		public virtual APLayerType LayerType { get { return APLayerType.Default; } }
		public virtual void SetSize( int witdh, int Size ) { }
	}
}
