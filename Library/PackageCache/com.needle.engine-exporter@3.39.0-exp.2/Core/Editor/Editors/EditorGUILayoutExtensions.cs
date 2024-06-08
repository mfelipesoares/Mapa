using Needle.Engine.Editors;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal static class EditorGUILayoutExtensions
	{
		public static void HelpBoxChecked(string message)
		{
			EditorGUILayout.LabelField(
				new GUIContent(message, Assets.Checkmark),
				EditorStyles.helpBox);
		}
	}
}