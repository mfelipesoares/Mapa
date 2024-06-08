using System;
using Newtonsoft.Json;
using UnityEditor;

namespace Needle.Engine
{
	internal static class SerializedPropertyUtil
	{
		public static void WriteSerializedProperty(this JsonTextWriter writer, SerializedProperty property)
		{
			writer.WritePropertyName(property.name);
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					writer.WriteValue(property.intValue);
					break;
				case SerializedPropertyType.Boolean:
					writer.WriteValue(property.boolValue);
					break;
				case SerializedPropertyType.Float:
					writer.WriteValue(property.floatValue);
					break;
				case SerializedPropertyType.String:
					writer.WriteValue(property.stringValue);
					break;
			}
		}
	}
}