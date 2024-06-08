using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using JetBrains.Annotations;
using Needle.Engine.Gltf.Experimental.progressive;
using Needle.Engine.Gltf.ImportSettings;
using Needle.Engine.Utils;
using UnityEngine;

namespace Needle.Engine.Gltf
{
	[UsedImplicitly] 
	public class ProgressiveMeshSettingsHandler: GltfExtensionHandlerBase
	{
		private ProgressiveTexturesSettings progressiveLoadingComponent;
		// private bool isUsingProgressiveTextures;
		
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			progressiveLoadingComponent = Object.FindObjectsByType<ProgressiveTexturesSettings>(FindObjectsSortMode.None).FirstOrDefault(e => e.enabled);
		}
		
		public override void OnAfterMeshExport(GltfExportContext context, Mesh mesh, GLTFMesh gltfmesh, int index, List<object> extensions)
		{
			base.OnAfterMeshExport(context, mesh, gltfmesh, index, extensions);
			
			// First check if we have a use progressive textures component on the exported asset
			if (!context.Root.TryGetComponent(out ProgressiveTexturesSettings progressiveTexturesSettings))
			{
				// Otherwise fallback to another progressive texture component found in the scene
				progressiveTexturesSettings = progressiveLoadingComponent;
			}
			
			var guid = mesh.GetId();

			// check if LOD generation is disabled
			if (progressiveTexturesSettings?.AllowProgressiveLoading == false || progressiveTexturesSettings?.GenerateLODs == false)
			{
				extensions.Add(new NEEDLE_progressive_mesh_settings(guid, false));
				return;
			}
			
			if (NeedleAssetSettings.TryGetSettings(mesh, out var settings))
			{
				if(settings is MeshSettings meshSettings)
				{
					if (meshSettings.useProgressiveMesh == false)
					{
						extensions.Add(new NEEDLE_progressive_mesh_settings(guid, false));
						return;
					}
				}
			}
			
			// we dont want to compress e.g. the default cube 
			if (mesh.vertexCount > 32)
			{
				extensions.Add(new NEEDLE_progressive_mesh_settings(guid, true));
			}
		}
	}
}