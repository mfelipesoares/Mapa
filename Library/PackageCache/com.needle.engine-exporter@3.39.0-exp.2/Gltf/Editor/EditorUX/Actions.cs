using System;
using System.IO;
using Needle.Engine.Core;
using Needle.Engine.Core.References.ReferenceResolvers;
using Needle.Engine.Gltf.UnityGltf;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_2020_3
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Needle.Engine.Gltf
{
	internal static class EditorActions
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Builder.DoExportCurrentScene = ctx =>
			{
				var scene = SceneManager.GetActiveScene();
				if (string.IsNullOrEmpty(scene.path))
				{
					throw new Exception("The current scene can not yet be exported because it has not been saved yet. Please save your scene before exporting.");
				}
				var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) as Object;
				GltfReferenceResolver.ClearCache();
				Export.AsGlb(asset, out var path, true);
				return path;
			};
		}
		
		internal static bool TryExportCurrentScene()
		{
			var scene = SceneManager.GetActiveScene();
			var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) as Object;

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage)
			{
				asset = prefabStage.prefabContentsRoot;
			}

			if (asset)
			{
				GltfReferenceResolver.ClearCache();
				UnityGltfExportHandler.ResetExported();
				return Export.AsGlb(asset, out _, true);;
			}
			return false;
		}
	}
}