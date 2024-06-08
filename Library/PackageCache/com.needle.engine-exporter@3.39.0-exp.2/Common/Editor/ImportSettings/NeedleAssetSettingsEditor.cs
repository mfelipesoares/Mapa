using System;
using Needle.Engine.Gltf.ImportSettings;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.ImportSettings
{
	[CustomEditor(typeof(NeedleAssetSettings))]
	public class NeedleAssetSettingsEditor : Editor
	{
		private string assetPath;
		private SerializedProperty asset;

		private void OnEnable()
		{
			assetPath = AssetDatabase.GetAssetPath((target as NeedleAssetSettings)!.asset);
			asset = serializedObject.FindProperty("asset");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Asset Import Settings for " + assetPath, MessageType.None);
			GUILayout.Space(5);

			if (asset != null)
			{
				using (new EditorGUI.DisabledScope(true))
				{
					EditorGUILayout.PropertyField(asset);
				}
			}
		}
	}
}