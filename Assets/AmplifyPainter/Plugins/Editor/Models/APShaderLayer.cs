// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyPainter
{
	[Serializable]
	public sealed class APShaderLayer : APSMORTLayer
	{
		public RenderTexture ShaderValue;
		public Material CurrentMaterial;

		override protected void OnEnable()
		{
			base.OnEnable();
			if( CurrentMaterial != null )
			{
				APResources.RegisterExternalResource( CurrentMaterial.shader, this );
			}
		}

		override protected void OnDestroy()
		{
			base.OnEnable();
			ShaderValue.Release();

			DestroyImmediate( ShaderValue );
			ShaderValue = null;

			APResources.ReleaseExternalResource( CurrentMaterial.shader, this );
			DestroyImmediate( CurrentMaterial );
			CurrentMaterial = null;
		}

		public void Init( Material material, RenderTextureFormat format, int width, int height )
		{
			BaseInit();
			SMORTInit( width, height, format );
			APResources.RegisterExternalResource( material.shader, this );
			IsEditable = false;
			CurrentMaterial = new Material( material )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			ShaderValue = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			ShaderValue.Create();
			int shaderDirtyCount = UnityEditor.EditorUtility.GetDirtyCount( CurrentMaterial.shader );
			RefreshValue();
		}

		public void Init( Shader shader, RenderTextureFormat format, int width, int height )
		{
			BaseInit();
			SMORTInit( width, height, format );
			APResources.RegisterExternalResource( shader, this );
			IsEditable = false;
			CurrentMaterial = new Material( shader )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			ShaderValue = new RenderTexture( width, height, 0, format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			ShaderValue.Create();
			int shaderDirtyCount = UnityEditor.EditorUtility.GetDirtyCount( CurrentMaterial.shader );
			RefreshValue();
		}

		public override APDropResult OnDroppedObjects( UnityEngine.Object[] objects )
		{
			Material newMaterial = null;
			bool foundMatch = false;

			Shader shader = objects[ 0 ] as Shader;
			if( shader != null )
			{
				foundMatch = true;
				newMaterial = new Material( shader )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}
			else
			{
				Material material = objects[ 0 ] as Material;
				if( material != null )
				{
					foundMatch = true;
					newMaterial = new Material( material )
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
			}

			if( foundMatch )
			{
				APResources.ReleaseExternalResource( CurrentMaterial.shader, this );
				APResources.RegisterExternalResource( newMaterial.shader, this );
				DestroyImmediate( CurrentMaterial );
				CurrentMaterial = newMaterial;
				int shaderDirtyCount = UnityEditor.EditorUtility.GetDirtyCount( CurrentMaterial.shader );
				RefreshValue();
				return APDropResult.Success;
			}

			return ( objects[ 0 ] is Texture2D ) ? APDropResult.SwitchToTexture : APDropResult.Fail;
		}

		public override void RefreshValue()
		{
			RenderTexture buffer = RenderTexture.active;
			RenderTexture.active = null;
			Graphics.Blit( Texture2D.whiteTexture, ShaderValue, CurrentMaterial );
			LayerOpsMat.SetVector( LayerOpParamsId, LayerOpsParams );
			Graphics.Blit( ShaderValue, FinalValue, LayerOpsMat );
			RenderTexture.active = buffer;
		}

		//public override Texture Value { get { return m_value; } }
		public override APLayerType LayerType { get { return APLayerType.Shader; } }
		public override void SetSize( int width, int height )
		{
			ShaderValue = ShaderValue.SetSize( width, height );
		}
	}
}
