using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine
{
	internal class PropertyDrawerWrapper : PropertyDrawer
	{
		private readonly PropertyDrawer drawer;
		private readonly PropertyChangedEvent evt;

		public PropertyDrawerWrapper([CanBeNull] PropertyDrawer drawer, PropertyChangedEvent evt)
		{
			this.drawer = drawer;
			this.evt = evt;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (drawer == null) return EditorGUI.GetPropertyHeight(property, label);
			return drawer.GetPropertyHeight(property, label);
		}

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (drawer != null) return drawer.CreatePropertyGUI(property);
			return base.CreatePropertyGUI(property);
		}

		private static readonly Color activeColor = new Color(.3f, 1f, .56f, 0.5f);
		private static readonly Color inactiveColor = new Color(.5f, .5f, .5f, .5f);

		// private Dictionary<SerializedProperty, object> valuesCache = new Dictionary<SerializedProperty, object>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var active = EditorModificationListener.Enabled;
			active &= EditorModificationListener.AllowComponentModifications;
			// Transforms are handled via TransformChangeListeners
			active &= property.serializedObject?.targetObject is Transform == false;
			
			// if (Event.current.modifiers == EventModifiers.Alt)
			{
				var rect = new Rect(position);
				rect.width = 2;
				rect.x -= rect.width + 4;
				var color = active ? activeColor : inactiveColor;
				EditorGUI.DrawRect(rect, color);
			}

			using var scope = new EditorGUI.ChangeCheckScope();
			if (this.drawer != null)
				this.drawer.OnGUI(position, property, label);
			else
			{
				EditorGUI.PropertyField(position, property, label, true);
			}

			var changed = scope.changed;
			// var v = GetValue(property, true);
			// if(!valuesCache.TryGetValue(property, out var oldV) || (v != null && v.GetType().IsPrimitive && !Equals(oldV, v)))
			// {
			// 	valuesCache[property] = v;
			// 	changed = true;
			// }

			if (changed && active)
			{
				var value = GetValue(property);
				this.evt?.Invoke(property.serializedObject.targetObjects, property.propertyPath, value);
			}
		}

		private static object GetValue(SerializedProperty property, bool shallow = false)
		{
			if (property.isArray)
			{
				if (property.arrayElementType != "char" && !shallow)
				{
					var array = new object[property.arraySize];
					for (var i = 0; i < property.arraySize; i++)
					{
						var prop = property.GetArrayElementAtIndex(i);
						var val = GetValue(prop);
						array[i] = val;
					}
					return array;
				}
			}

			switch (property.propertyType)
			{
				case SerializedPropertyType.Color:
					return property.colorValue;
				case SerializedPropertyType.Vector2:
					return property.vector2Value;
				case SerializedPropertyType.Vector2Int:
					return property.vector2IntValue;
				case SerializedPropertyType.Vector3:
					return property.vector3Value;
				case SerializedPropertyType.Vector3Int:
					return property.vector3IntValue;
				case SerializedPropertyType.Vector4:
					return property.vector4Value;
				case SerializedPropertyType.Float:
					return property.floatValue;
				case SerializedPropertyType.Integer:
					return property.intValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue;
				case SerializedPropertyType.Generic:
					break;
				case SerializedPropertyType.Boolean:
					return property.boolValue;
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.LayerMask:
					return property.layerMaskBits;
				case SerializedPropertyType.Enum:
					// the index is not necessarily the value we need (e.g. camera clear flags)
					// so we need to get the enum type and get the value from the index
					if (TryGetEnumValue(property, out var value)) return value;
					var index = property.intValue;
					return index;
				case SerializedPropertyType.Rect:
					return property.rectValue;
				case SerializedPropertyType.ArraySize:
					return property.arraySize;
				case SerializedPropertyType.Character:
					break;
				case SerializedPropertyType.AnimationCurve:
					return property.animationCurveValue;
				case SerializedPropertyType.Bounds:
					return property.boundsValue;
				case SerializedPropertyType.Gradient:
					return property.gradientValue;
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue;
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue;
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize;
				case SerializedPropertyType.RectInt:
					return property.rectValue;
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue;
				case SerializedPropertyType.ManagedReference:
					return property.managedReferenceFullTypename;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return null;
		}

		private static bool TryGetEnumValue(SerializedProperty property, out int value)
		{
			var index = property.enumValueIndex;

			switch (property.name)
			{
				case "m_ClearFlags":
					var enumValue = Enum.GetValues(typeof(CameraClearFlags));
					value = (int)enumValue.GetValue(index);
					return true;
			}
			// var members = property.serializedObject.targetObject.GetType().GetMember(property.name,
			// 	BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			// foreach (var member in members)
			// {
			// 	if (member is FieldInfo field)
			// 	{
			// 		
			// 	}
			// 	else if (member is PropertyInfo prop)
			// 	{
			// 		
			// 	}
			// }

			value = -1;
			return false;
		}
	}
}