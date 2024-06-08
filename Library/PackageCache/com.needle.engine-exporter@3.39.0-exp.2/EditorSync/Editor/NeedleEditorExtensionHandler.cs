using JetBrains.Annotations;
using Needle.Engine.Gltf;
using Unity.Profiling;
using UnityEngine;

namespace Needle.Engine.EditorSync
{
	[UsedImplicitly]
	public class NeedleEditorExtensionHandler : GltfExtensionHandlerBase
	{
		private static ProfilerMarker profileNodes = new ProfilerMarker("NeedleEditorExtensionHandler.OnAfterNodeExport");
		private static ProfilerMarker profileMaterials = new ProfilerMarker("NeedleEditorExtensionHandler.OnAfterNodeExport");
		
		private const string EXTENSION_NAME = nameof(NEEDLE_editor);

		public override void OnAfterNodeExport(GltfExportContext context, Transform transform, int nodeId)
		{
			base.OnAfterNodeExport(context, transform, nodeId);

			if (!SyncSettings.Enabled) return;
			// TODO: warn that users need to re-export if they change this setting at runtime
			if (SyncSettings.SyncComponents == false) return;

			using (profileNodes.Auto())
			{
				// We are not interested in NestedGltf objects because they are empty and just load another glb
				if (transform.TryGetComponent<NestedGltf>(out _)) return;
				
				if (NEEDLE_editor.TryGetId(transform, out var guid))
				{
					var ext = new NEEDLE_editor();
					ext.id = guid;
					context.Bridge.AddNodeExtension(nodeId, EXTENSION_NAME, ext);
				}
			}
		}

		public override void OnAfterMaterialExport(GltfExportContext context, Material material, int materialId)
		{
			base.OnAfterMaterialExport(context, material, materialId);

			if (!SyncSettings.Enabled) return;

			using (profileMaterials.Auto())
			{
				if (NEEDLE_editor.TryGetId(material, out var guid))
				{
					var ext = new NEEDLE_editor();
					ext.id = guid;
					context.Bridge.AddMaterialExtension(materialId, EXTENSION_NAME, ext);
				}
			}
		}
	}
}