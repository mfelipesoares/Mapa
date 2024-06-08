#if URP_INSTALLED

using Needle.Engine.Gltf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Needle.Engine.UniversalRenderPipeline
{
	internal static class URPGltfImportHandler
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			GltfImporter.AfterImported += OnAfterImport;
		}

		private static void OnAfterImport(GameObject obj)
		{
			if (!obj) return;
			var cameras = obj.GetComponentsInChildren<Camera>();
			foreach (var camera in cameras)
			{
				// Add a UniversalAdditionalCameraData component to the camera if it doesn't have one already.
				// Otherwise the camera inspector is broken in URP (2020.3.38) URP v10.10.0
				if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out _))
				{
					camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
				}
			}
		}
	}
}

#endif