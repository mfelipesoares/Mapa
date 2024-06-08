using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Needle.Engine.Gltf.Experimental;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf
{
	[UsedImplicitly]
	public class MeshCompressionHandler : GltfExtensionHandlerBase
	{
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			// Try find mesh compression component first in children of the current root
			// then search on the ExportInfo component
			// and fallback anywhere in the current scene (perhaps the root scene)
			var meshCompression = context.Root?.GetComponentInChildren<MeshCompression>();
			if (!meshCompression)
			{
				var exportInfo = ExportInfo.Get();
				exportInfo?.TryGetComponent(out meshCompression);
			}
			if (!meshCompression) meshCompression = Object.FindAnyObjectByType<MeshCompression>();
			if (meshCompression)
			{
				var ext = new NEEDLE_mesh_compression_root();
				switch (meshCompression.Compression)
				{
					case MeshCompressionType.None:
						ext.compression = "none";
						break;
					case MeshCompressionType.Draco:
						ext.compression = "draco";
						break;
					case MeshCompressionType.Meshopt:
						ext.compression = "meshopt";
						break;
				}
				context.Bridge.AddExtension("NEEDLE_mesh_compression", ext);
			}
		}

		public override void OnAfterPrimitiveExport(GltfExportContext context, Mesh mesh, List<object> extensions)
		{
			base.OnAfterPrimitiveExport(context, mesh, extensions);

			if (NeedleAssetSettingsProvider.TryGetMeshSettings(mesh, out var settings))
			{
				if (settings.@override)
				{
					var ext = new NEEDLE_mesh_compression();
					ext.useSimplifier = settings.useSimplifier;
					ext.error = settings.error;
					ext.ratio = settings.ratio;
					ext.lockBorder = settings.lockBorder;
					extensions.Add(ext);
					return;
				}
			}
			
			
			var labels = AssetDatabase.GetLabels(mesh);
			foreach (var label in labels)
			{
				if (label.Equals("simplify", StringComparison.OrdinalIgnoreCase))
				{
					var ext = new NEEDLE_mesh_compression();
					ext.useSimplifier = true;
					extensions.Add(ext);
				}
			}
		}
	}
}