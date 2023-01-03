// Amplify Painter - Painting Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using UnityEditor;
using UnityEngine;
namespace AmplifyPainter
{
	public class AngleAttribute : PropertyAttribute
	{
		public readonly float snap;
		public readonly float min;
		public readonly float max;

		public AngleAttribute()
		{
			snap = 1;
			min = -360;
			max = 360;
		}

		public AngleAttribute( float snap )
		{
			this.snap = snap;
			min = -360;
			max = 360;
		}

		public AngleAttribute( float snap, float min, float max )
		{
			this.snap = snap;
			this.min = min;
			this.max = max;
		}
	}

	[CustomPropertyDrawer( typeof( AngleAttribute ) )]
	public class AngleDrawer : PropertyDrawer
	{
		private static Vector2 mousePosition;

		public static float FloatAngle( Rect rect, float value )
		{
			return FloatAngle( rect, value, -1, -1, -1 );
		}

		public static float FloatAngle( Rect rect, float value, float snap )
		{
			return FloatAngle( rect, value, snap, -1, -1 );
		}

		public static float FloatAngle( Rect rect, float value, float snap, float min, float max )
		{
			return FloatAngle( rect, value, snap, min, max, Vector2.right );
		}

		public static float FloatAngle( Rect rect, float value, float snap, float min, float max, Vector2 zeroVector )
		{
			int id = GUIUtility.GetControlID( FocusType.Passive, rect );
			float originalValue = value;
			Rect knobRect = new Rect( rect.x, rect.y, rect.height, rect.height - 2 );

			float delta;
			if( min != max )
				delta = ( ( max - min ) / 360 );
			else
				delta = 1;

			if( Event.current != null )
			{
				if( Event.current.type == EventType.MouseDown && knobRect.Contains( Event.current.mousePosition ) )
				{
					GUIUtility.hotControl = id;

					value = CalculateMouseAngle( knobRect, originalValue, snap, delta, zeroVector );
				}
				else if( Event.current.type == EventType.MouseUp && GUIUtility.hotControl == id )
				{
					GUIUtility.hotControl = 0;
				}
				else if( Event.current.type == EventType.MouseDrag && GUIUtility.hotControl == id )
				{
					value = CalculateMouseAngle( knobRect, originalValue, snap, delta, zeroVector );
				}
			}

			float angleOffset = ( CalculateAngle( Vector2.up, zeroVector ) + 360f ) % 360f;

			GUI.DrawTexture( knobRect, APResources.KnobBackTexture );

			Rect knobdot = knobRect;
			knobdot.width = 16;
			knobdot.height = 16;
			Vector2 upvec = Vector2.down * 20;
			if( min != max )
				knobdot.position = upvec.Rotate( ( angleOffset - value ) * ( 360 / ( max - min ) ) );
			else
				knobdot.position = upvec.Rotate( ( angleOffset - value ) );

			knobdot.position += knobRect.center - Vector2.one * 8;

			Matrix4x4 matrix = GUI.matrix;

			if( min != max )
				GUIUtility.RotateAroundPivot( ( angleOffset - value ) * ( 360 / ( max - min ) ), knobRect.center );
			else
				GUIUtility.RotateAroundPivot( ( angleOffset - value ), knobRect.center );

			//Vector2.
			//knobRect.center.Rotate( )
			//Rect knobdot = knobRect;
			//knobdot.width = 8;
			//knobdot.height = 8;
			//knobdot.x += 32 - 4;
			//knobdot.y += 8;


			GUI.matrix = matrix;
			GUI.DrawTexture( knobdot, APResources.KnobTexture );

			Rect label = new Rect( rect.x + rect.height, rect.y + ( rect.height / 2 ) - 9, rect.height, 18 );
			Rect field = new Rect( rect );
			field.height = EditorGUIUtility.singleLineHeight;
			field.y = ( ( rect.height - field.height ) * 0.5f ) - 2;
			field.xMin += rect.height;

			field.y = rect.y + ( rect.height / 2 ) - 9;

			if( value < min )
				value += max - min;
			if( value > max )
				value -= max - min;
			value = EditorGUI.Slider( field, value, min, max );
			//if( min != max )
			//	value = Mathf.Clamp( value, min, max );

			return value;
		}
		//public static class Vector2Extension
		//{

		//	public static Vector2 Rotate( this Vector2 v, float degrees )
		//	{
		//		float sin = Mathf.Sin( degrees * Mathf.Deg2Rad );
		//		float cos = Mathf.Cos( degrees * Mathf.Deg2Rad );

		//		float tx = v.x;
		//		float ty = v.y;
		//		v.x = ( cos * tx ) - ( sin * ty );
		//		v.y = ( sin * tx ) + ( cos * ty );
		//		return v;
		//	}
		//}

		private static float CalculateMouseAngle( Rect knobRect, float originalValue, float snap, float delta, Vector2 zeroVector )
		{
			float angle;
			Vector2 mouseStartDirection = -( Event.current.mousePosition - knobRect.center ).normalized;
			float startAngle = CalculateAngle( zeroVector, mouseStartDirection );
			angle = startAngle;

			if( snap > 0 )
			{
				float mod = angle % snap;

				if( mod < ( delta * 3 ) || Mathf.Abs( mod - snap ) < ( delta * 3 ) )
					angle = Mathf.Round( angle / snap ) * snap;
			}

			if( angle != originalValue )
				GUI.changed = true;

			return angle;
		}

		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
			AngleAttribute attr = (AngleAttribute)attribute;
			SerializedProperty valueProperty = property.FindPropertyRelative( "Value" );

			if( valueProperty == null )
				valueProperty = property;

			bool enabledBuffer = GUI.enabled;
			float cache = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;
			EditorGUI.BeginProperty( position, label, valueProperty );
			GUI.enabled = true;
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Passive ), label );
			if( valueProperty.propertyType == SerializedPropertyType.Float )
			{
				valueProperty.floatValue = FloatAngle( position, valueProperty.floatValue, attr.snap, attr.min, attr.max, Vector2.up );
			}

			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
			EditorGUIUtility.labelWidth = cache;
			GUI.enabled = enabledBuffer;
		}

		public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
		{
			return 64;
			//return base.GetPropertyHeight( property, label ) + 2;
		}

		public static float CalculateAngle( Vector3 from, Vector3 to )
		{
			float angle = Vector3.SignedAngle( from, to, Vector3.back );
			return angle < 0 ? angle + 360 : angle;
		}
	}
}
