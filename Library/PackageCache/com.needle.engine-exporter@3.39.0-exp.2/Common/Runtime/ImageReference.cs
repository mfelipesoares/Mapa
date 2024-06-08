using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
	/// <summary>
	/// Add to FileReference field to specify the allowed type and extensions
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
	public class FileReferenceTypeAttribute : Attribute
	{
		public readonly Type AllowedType;
		public readonly string[] AllowedExtensions;

		public FileReferenceTypeAttribute()
		{
		}

		public FileReferenceTypeAttribute(Type type = null, params string[] allowedExtensions)
		{
			this.AllowedType = type;
			this.AllowedExtensions = allowedExtensions;
		}
	}

	/// <summary>
	/// Add the FileReferenceType attribute to specify the allowed type and extensions
	/// </summary>
	[Serializable]
	public class FileReference
	{
		[FormerlySerializedAs("Texture")] public Object File;
	}

	[Serializable, FileReferenceType(typeof(Texture2D))]
	public class ImageReference : FileReference
	{
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(FileReference), true)]
	[CustomPropertyDrawer(typeof(ImageReference), true)]
	public class FileReferenceDrawer : PropertyDrawer
	{
		private static readonly Dictionary<SerializedProperty, IList<object>> cachedAttributes =
			new Dictionary<SerializedProperty, IList<object>>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (cachedAttributes.TryGetValue(property, out var list))
			{
				if (TryValidateCustomAttributes(list, position, property, label, true))
					return;
			}
			else
			{
				if (TryValidateCustomAttributes(fieldInfo.GetCustomAttributes(typeof(FileReferenceTypeAttribute)),
					    position, property, label)) 
					return;
				if (TryValidateCustomAttributes(fieldInfo.FieldType.GetCustomAttributes(), position, property, label))
					return;
			}

			EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(FileReference.File)), label);
		}

		private static bool TryValidateCustomAttributes(
			IEnumerable<object> attributes,
			Rect position,
			SerializedProperty property,
			GUIContent label, bool isCached = false)
		{
			if (!cachedAttributes.TryGetValue(property, out var cache))
			{
				cache = new List<object>();
				cachedAttributes.Add(property, cache);
			}
			foreach (var attr in attributes)
			{
				if (attr is FileReferenceTypeAttribute fileAttr)
				{
					if(!isCached)
						cache.Add(attr);
					
					if (fileAttr.AllowedType != null)
					{
						Object OnValidate(Object[] references, Type type, SerializedProperty p)
						{
							if (references.Length == 0) return null;
							var r = references[0];
							if (r)
							{
								if (!fileAttr.AllowedType.IsInstanceOfType(r)) return null;
								if (fileAttr.AllowedExtensions.Length > 0)
								{
									var path = AssetDatabase.GetAssetPath(r);
									if (!string.IsNullOrEmpty(path))
									{
										var foundAllowed = false;
										for (var i = 0; i < fileAttr.AllowedExtensions.Length; i++)
										{
											if (foundAllowed) break;
											var ext = fileAttr.AllowedExtensions[i];
											if (path.EndsWith(ext)) foundAllowed = true;
										}
										if (!foundAllowed)
										{
											Debug.LogWarning("File Type " + Path.GetExtension(path) +
											                 " is not assignable to " + property.propertyPath);
											return null;
										}
									}
								}
							}
							return r;
						}

						// TODO: this can be cached
						PublicEditorGUI.ObjectField(position, property.FindPropertyRelative(nameof(FileReference.File)),
							fileAttr.AllowedType, label, OnValidate);
						return true;
					}
				}
			}
			return false;
		}
	}
#endif

// #if UNITY_EDITOR
// 	[CustomPropertyDrawer(typeof(ImageReference))]
// 	public class ImageReferenceDrawer : PropertyDrawer
// 	{
// 		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
// 		{
// 			EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(ImageReference.File)), label);
// 		}
// 	}
// #endif
}