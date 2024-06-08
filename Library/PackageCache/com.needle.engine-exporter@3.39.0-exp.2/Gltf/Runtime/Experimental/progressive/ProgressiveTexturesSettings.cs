using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf.Experimental.progressive
{
	// [EditorBrowsable(EditorBrowsableState.Never)]
	// [Obsolete("ProgressiveTexturesSettings has been deprecated. Use ProgressiveLoadingSettings instead. (UnityUpgradable) -> ProgressiveLoadingSettings")]
	[ExecuteAlways]
	[HelpURL(Constants.DocumentationUrl)]
	[AddComponentMenu("Needle Engine/Optimization/Progressive Loading Settings (Needle Engine)" + Constants.NeedleComponentTags + " progressiveloading progressivemeshes generate meshes textures optimization build loading deferred lods autolod")]
	public class ProgressiveTexturesSettings : MonoBehaviour
	{
		[Tooltip("When disabled no progressive loading is used (even if it's enabled in the Mesh- or TextureImporter settings)")]
		public bool AllowProgressiveLoading = true;
		
		// Textures
		public bool UseMaxSize = true;  
		[Tooltip("This is the max resolution for textures in the glb that is loaded at start - high-res textures with the original resolution will be loaded on demand.")]
		public int MaxSize = 128;
		
		// Meshes
		[Tooltip("When enabled LODs will be generated for the meshes. When disabled no LODs will be generated. You can also disable this option per mesh in the Mesh importer")]
		public bool GenerateLODs = true;

		// ReSharper disable once Unity.RedundantEventFunction
		private void OnEnable()
		{
			
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ProgressiveTexturesSettings))]
	public class UseProgressiveTexturesEditor : Editor
	{
		private SerializedProperty allowProgressiveLoading, useMaxSize, maxSize, generateLODs;
		
		private void OnEnable()
		{
			allowProgressiveLoading = serializedObject.FindProperty(nameof(ProgressiveTexturesSettings.AllowProgressiveLoading));
			useMaxSize = serializedObject.FindProperty(nameof(ProgressiveTexturesSettings.UseMaxSize));
			maxSize = serializedObject.FindProperty(nameof(ProgressiveTexturesSettings.MaxSize));
			generateLODs = serializedObject.FindProperty(nameof(ProgressiveTexturesSettings.GenerateLODs));
		}

		public override void OnInspectorGUI()
		{
			// ReSharper disable once LocalVariableHidesMember
			var target = (ProgressiveTexturesSettings) this.target;
			var change = new EditorGUI.ChangeCheckScope();
			EditorGUILayout.PropertyField(allowProgressiveLoading, new GUIContent("Allow Progressive Loading"));
			
			var messageType = allowProgressiveLoading.boolValue ? MessageType.None : MessageType.Warning;
			EditorGUILayout.HelpBox("Progressive Loading is enabled by default (for production builds). This component can be used to disable LOD generation for textures or meshes project wide. Use with care - we generally recommend having progressive loading enabled since it drastically reduces loading speed and performance!", messageType);
			
			using (new EditorGUI.DisabledScope(target.AllowProgressiveLoading == false))
			{
				GUILayout.Space(5);
				EditorGUILayout.LabelField(new GUIContent("Textures"), EditorStyles.boldLabel);
				
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.PropertyField(useMaxSize,
						new GUIContent("Max Size", "When disabled the default max size will be used (usually 256px)"));
					using (new EditorGUI.DisabledScope(target.UseMaxSize == false))
					{
						EditorGUILayout.PropertyField(maxSize, new GUIContent());
					}
				}
				
				GUILayout.Space(5);
				EditorGUILayout.LabelField(new GUIContent("Meshes"), EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(generateLODs, new GUIContent("Generate LODs"));
				
				
				GUILayout.Space(5);
				EditorGUILayout.HelpBox("Tip: You can append '?debugprogressive' to add a random delay to the progressive loading. Textures/Meshes can also be toggled between highres and lowres using P in that mode.", MessageType.None);
			}
			if(change.changed) serializedObject.ApplyModifiedProperties();
			
		}
	}
#endif
}
