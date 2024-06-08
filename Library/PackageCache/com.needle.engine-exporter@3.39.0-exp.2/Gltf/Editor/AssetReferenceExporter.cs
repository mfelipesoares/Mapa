using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf
{
	[UsedImplicitly]
	public class AssetReferenceExporter : GltfExtensionHandlerBase
	{
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			context.RegisterValueResolver(new AssetReferenceResolver());
		}

		private class AssetReferenceResolver : IValueResolver
		{
			public bool TryGetValue(IExportContext ctx, object instance, MemberInfo member, ref object value)
			{
				// Only export referenced objects as a separate glb if they point to Transforms or GameObjects
				// For example if a component inside an asset (Cube.prefab/MyComponent) points to another component on that asset (Cube.prefab/MyOtherComponent)
				// We dont want to export it as a separate glb
				// Similarly if it points to a transform inside the same asset we dont want to export it as a separate glb either
				if (value is Transform || value is GameObject || value is SceneAsset)
				{
					if (value is Object obj && EditorUtility.IsPersistent(obj))
					{
						// if a component inside a prefab references the prefab root then we dont want to resolve it/export it as an asset path
						// instead we just want to export the node id / guid
						// see NE-2814
						if (ctx.Root == value as Transform) return false;

						if (Export.AsGlb(ctx, obj, out var path, instance))
						{
							value = path;
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}