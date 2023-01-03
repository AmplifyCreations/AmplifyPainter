// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Text;

using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor.EditorTools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AmplifyPainter
{
	public static class EditorToolsContextEx
	{
		private static Type m_type = null;
		public static EditorTool GetCurrentTool()
		{
			return (EditorTool)Type.InvokeMember( "GetActiveTool", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[] { } );
		}

		public static APEditorTool GetSingleton()
		{
			Type type = typeof( APEditorTool );
			return (APEditorTool)Type.InvokeMember( "GetSingleton", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { type } );
		}

		public static Type Type { get { return ( m_type == null ) ? m_type = Type.GetType( "UnityEditor.EditorTools.EditorToolContext, UnityEditor" ) : m_type; } }
	}

	public static class Vector2Extension
	{
		public static Vector2 Rotate( this Vector2 v, float degrees )
		{
			float sin = Mathf.Sin( degrees * Mathf.Deg2Rad );
			float cos = Mathf.Cos( degrees * Mathf.Deg2Rad );

			float tx = v.x;
			float ty = v.y;
			v.x = ( cos * tx ) - ( sin * ty );
			v.y = ( sin * tx ) + ( cos * ty );
			return v;
		}
	}

	public static class RenderTextureEx
	{
		private static Material s_dilateMat;

		public static RenderTexture SetSize( this RenderTexture obj, int width, int height )
		{
			RenderTexture newRT = new RenderTexture( width, height, 0, obj.format )
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			newRT.Create();

			Graphics.Blit( obj, newRT );
			obj.Release();
			ScriptableObject.DestroyImmediate( obj );
			return newRT;
		}

		public static void FastDilate( this RenderTexture output, int padding, RenderTexture mask = null )
		{
			//double time = Time.realtimeSinceStartup;

			// TODO: remove direct GUID reference
			if( s_dilateMat == null )
			{
				s_dilateMat = new Material( APResources.DilateShader )
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			int width = output.width;
			int height = output.height;
			int depth = output.depth;
			RenderTextureFormat format = output.format;

			RenderTexture cache = RenderTexture.active;
			RenderTexture.active = null;

			RenderTexture tempTex = RenderTexture.GetTemporary( width, height, depth, format );
			RenderTexture maskTex = RenderTexture.GetTemporary( width, height, depth, format );

			Graphics.Blit( mask, maskTex, s_dilateMat, 5 );

			for( int i = 0; i < padding; i++ )
			{
				if( i >= 16 )
					Shader.SetGlobalFloat( "_DilateDistance", i - 14 );
				else
					Shader.SetGlobalFloat( "_DilateDistance", 1 );

				s_dilateMat.SetTexture( "_MaskTex", maskTex );
				Graphics.Blit( output, tempTex, s_dilateMat, 3 );
				Graphics.Blit( tempTex, output );

				Graphics.Blit( maskTex, tempTex, s_dilateMat, 1 );
				Graphics.Blit( tempTex, maskTex );
			}

			RenderTexture.ReleaseTemporary( maskTex );
			RenderTexture.ReleaseTemporary( tempTex );
			RenderTexture.active = cache;

			UnityEngine.Object.DestroyImmediate( s_dilateMat );
			//Debug.Log( ((Time.realtimeSinceStartup - time) * 1000f)+"ms to dilate" );
		}
	}

	public static class RaycastHitEx
	{
		public static Vector3 BariNormal( this RaycastHit hit )
		{
			MeshCollider mc = hit.collider as MeshCollider;
			if( mc == null )
				return hit.normal;
			Mesh m = mc.sharedMesh;
			Vector3[] normals = m.normals;
			int[] indices = m.triangles;
			Vector3 n0 = normals[ indices[ hit.triangleIndex * 3 + 0 ] ];
			Vector3 n1 = normals[ indices[ hit.triangleIndex * 3 + 1 ] ];
			Vector3 n2 = normals[ indices[ hit.triangleIndex * 3 + 2 ] ];
			Vector3 b = hit.barycentricCoordinate;
			Vector3 localNormal = ( b[ 0 ] * n0 + b[ 1 ] * n1 + b[ 2 ] * n2 ).normalized;
			return mc.transform.TransformDirection( localNormal );
		}

		public static Vector3 BariTangent( this RaycastHit hit )
		{
			MeshCollider mc = hit.collider as MeshCollider;
			if( mc == null )
				return hit.normal;
			Mesh m = mc.sharedMesh;
			Vector4[] tangents = m.tangents;
			int[] indices = m.triangles;
			Vector3 n0 = tangents[ indices[ hit.triangleIndex * 3 + 0 ] ];
			Vector3 n1 = tangents[ indices[ hit.triangleIndex * 3 + 1 ] ];
			Vector3 n2 = tangents[ indices[ hit.triangleIndex * 3 + 2 ] ];
			Vector3 b = hit.barycentricCoordinate;
			Vector3 localTangent = ( b[ 0 ] * n0 + b[ 1 ] * n1 + b[ 2 ] * n2 ).normalized;
			return mc.transform.TransformDirection( localTangent );
		}

		public static Vector3 BariTextCoord( this RaycastHit hit, int channel )
		{
			MeshCollider mc = hit.collider as MeshCollider;
			if( mc == null )
				return Vector3.zero;
			Mesh m = mc.sharedMesh;
			List<Vector3> texCoord = new List<Vector3>( m.vertices.Length );
			m.GetUVs( channel, texCoord );
			int[] indices = m.triangles;
			Vector3 N0 = texCoord[ indices[ hit.triangleIndex * 3 + 0 ] ];
			Vector3 N1 = texCoord[ indices[ hit.triangleIndex * 3 + 1 ] ];
			Vector3 N2 = texCoord[ indices[ hit.triangleIndex * 3 + 2 ] ];
			Vector3 B = hit.barycentricCoordinate;
			return ( B[ 0 ] * N0 + B[ 1 ] * N1 + B[ 2 ] * N2 );
		}
	}

#if UNITY_EDITOR
	public static class MaterialEx
	{
		public static void CopyPropertiesFrom( this Material to, Material from )
		{
			int count = ShaderUtil.GetPropertyCount( from.shader );
			for( int i = 0; i < count; i++ )
			{
				var ty = ShaderUtil.GetPropertyType( from.shader, i );
				var name = ShaderUtil.GetPropertyName( from.shader, i );
				switch( ty )
				{
					case ShaderUtil.ShaderPropertyType.Color:
					to.SetColor( name, from.GetColor( name ) );
					break;
					case ShaderUtil.ShaderPropertyType.Vector:
					to.SetVector( name, from.GetVector( name ) );
					break;
					case ShaderUtil.ShaderPropertyType.Float:
					to.SetFloat( name, from.GetFloat( name ) );
					break;
					case ShaderUtil.ShaderPropertyType.Range:
					to.SetFloat( name, from.GetFloat( name ) );
					break;
					case ShaderUtil.ShaderPropertyType.TexEnv:
					to.SetTexture( name, from.GetTexture( name ) );
					to.SetTextureOffset( name, from.GetTextureOffset( name ) );
					to.SetTextureScale( name, from.GetTextureScale( name ) );
					break;
					default:
					break;
				}
			}
			to.renderQueue = from.renderQueue;
			to.globalIlluminationFlags = from.globalIlluminationFlags;
			to.shaderKeywords = from.shaderKeywords;
			foreach( var keyword in to.shaderKeywords )
			{
				to.EnableKeyword( keyword );
			}
			to.enableInstancing = from.enableInstancing;
			EditorUtility.SetDirty( to );
		}
	}
#endif

	public class HandlesEx
	{
		private static Type m_type = null;
		public static void ClearHandles()
		{
			Type.InvokeMember( "ClearHandles", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, null );
		}

		public static Type Type { get { return ( m_type == null ) ? m_type = Type.GetType( "UnityEditor.Handles, UnityEditor" ) : m_type; } }
	}

	public class EditorGuiEx
	{
		private static System.Type m_type = null;
		public static System.Type Type { get { return ( m_type == null ) ? m_type = typeof( EditorGUI ) : m_type; } }

		public static int ColorPickerID
		{
			get
			{
				var field = Type.GetField( "s_ColorPickID", BindingFlags.NonPublic | BindingFlags.Static );
				return (int)field.GetValue( null );
			}
		}
	}

	public class ColorPickerEx
	{
		private static System.Type m_type = null;
		public static void Show( Action<Color> colorChangedCallback, Color col, bool showAlpha = true, bool hdr = false )
		{
			object[] param = { colorChangedCallback, col, showAlpha, hdr };
			Type.InvokeMember( "Show", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, param );
		}

		public static bool IsVisible
		{
			get
			{
				var field = Type.GetField( "s_Instance", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
				var instance = field.GetValue( null );

				if( instance == null )
					return false;
				else
				{
					var member = Type.GetField( "m_ColorBoxMode", BindingFlags.Instance | BindingFlags.NonPublic );
					return (int)member.GetValue( instance ) == 1 ? true : false;
				}
			}
		}

		public static System.Type Type { get { return ( m_type == null ) ? m_type = System.Type.GetType( "UnityEditor.ColorPicker, UnityEditor" ) : m_type; } }
	}

	public static class APHelpers
	{
		private static readonly Mesh s_quadMesh;
		private static Material s_quadMat;
		private static Vector3[] s_dottedBrush;

		[DllImport( "user32.dll" )]
		public static extern bool SetCursorPos( int X, int Y );

		public static void DrawWire( Vector3 center, Vector3 normal, Camera camera/*, Rect rect*/ )
		{
			if( s_quadMat == null )
			{
				s_quadMat = new Material( Shader.Find( "Hidden/AmplifyPainter/BasicBrush" ) )
				{
					enableInstancing = true,
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			if( s_dottedBrush == null )
			{
				s_dottedBrush = new Vector3[ 128 ];
				for( int i = 0; i < s_dottedBrush.Length; i++ )
				{
					float angle = ( (float)i / 128.0f ) * Mathf.PI * 2;
					//angle
					s_dottedBrush[ i ].x = Mathf.Cos( angle );
					s_dottedBrush[ i ].y = Mathf.Sin( angle );
					s_dottedBrush[ i ].z = angle;
				}
			}

			Quaternion rot = Quaternion.LookRotation( -normal, camera.transform.up );
			Matrix4x4 m = Matrix4x4.TRS( center, rot, Vector3.one );

			Shader.SetGlobalTexture( "_DotTexture", APResources.DotTexture );
			GL.PushMatrix();
			//Debug.Log( Camera.current );
			//if( Camera.current == null )
			//	GL.Viewport( new Rect( 0, 0, rect.width, rect.height - 17 ) );
			s_quadMat.SetPass( 0 );
			GL.LoadProjectionMatrix( camera.projectionMatrix );
			//GL.modelview = GL.modelview * m;

			// TODO: this needs a differnt check
			//if( Camera.current == null )
			//	GL.MultMatrix( camera.worldToCameraMatrix * m );
			//else
			GL.MultMatrix( m );

			GL.Begin( GL.LINE_STRIP );
			for( int i = 0; i < s_dottedBrush.Length; i++ )
			{
				GL.Vertex3( s_dottedBrush[ i ].x, s_dottedBrush[ i ].y, s_dottedBrush[ i ].z );
			}
			GL.Vertex3( s_dottedBrush[ 0 ].x, s_dottedBrush[ 0 ].y, s_dottedBrush[ 127 ].z + 0.01f );
			GL.End();
			GL.PopMatrix();
		}
	}
}
