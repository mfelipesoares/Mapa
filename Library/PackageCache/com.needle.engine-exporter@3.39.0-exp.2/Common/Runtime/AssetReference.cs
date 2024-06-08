using System;
using UnityEngine;
#if UNITY_2021_3_OR_NEWER
using UnityEngine.Search;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;


namespace Needle.Engine
{
	[Serializable]
	public class AssetReference
	{
#if UNITY_EDITOR
#if UNITY_2021_3_OR_NEWER
		[SearchContext("t:SceneAsset or t:GameObject", SearchViewFlags.CompactView)]
#endif
		public Object asset;
#else
		public object asset;
#endif

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(AssetReference))]
		public class AssetReferenceDrawer : PropertyDrawer
		{
			// private static GUIContent defaultLabel;

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				var prop = property.FindPropertyRelative("asset");
				// // if(defaultLabel == null) defaultLabel = new GUIContent("", "You can assign both Prefabs and Scenes");
				var isPartOfArray = property.propertyPath.EndsWith("]", StringComparison.Ordinal);
#if UNITY_2021_3_OR_NEWER
				EditorGUI.PropertyField(position, prop, isPartOfArray ? GUIContent.none : label);
#else
                PublicEditorGUI.ObjectField(position, prop, typeof(GameObject), isPartOfArray ? GUIContent.none : label, CustomValidate);
#endif
			}

#if !UNITY_2021_3_OR_NEWER
			private static Object CustomValidate(Object[] references, Type type, SerializedProperty property)
			{
				for (var index = 0; index < references.Length; index++)
				{
					var obj = references[index];
					if (obj is SceneAsset) return obj;

					if (obj is GameObject || obj is Transform)
					{
						return obj;
					}
				}
				return null;
			}
#endif
		}
#endif
	}
}