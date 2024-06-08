using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Interfaces;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Core.References.ReferenceResolvers
{
	[UsedImplicitly]
	public static class GltfReferenceResolver
	{
		private static readonly List<Object> exported = new List<Object>();

		[InitializeOnLoadMethod]
		private static void Init()
		{
			Builder.BuildStarting += ClearCache;
			Builder.BuildEnded += ClearCache;
		}

		internal static void ClearCache()
		{
			exported.Clear();
		}

		private static readonly List<IBeforeExportGltf> BeforeExportGltfCallbackReceivers = new List<IBeforeExportGltf>();
		
		public static void Register(IBeforeExportGltf callbackReceiver)
		{
			BeforeExportGltfCallbackReceivers.Add(callbackReceiver);
		}

		public static bool ExportReferencedObject(object owner,
			Object source,
			GameObject instance,
			IExportableObject exp,
			IExportContext context,
			ref string path,
			bool force = false)
		{
			var resultPath = path;
			var fileName = exp.name + context.GetExtension(instance);
			if (string.IsNullOrEmpty(resultPath))
				resultPath = context.AssetsDirectory + "/" + fileName;

			// ensure that when exporting we store the gameObject
			// and then nested / via Addressable try to export the transform of the same object
			// causing IOExceptions and export to break when trying to write to the same file twice
			var gameObject = source;
			if (SceneExportUtils.TryGetGameObject(gameObject, out var go))
				gameObject = go;

			if (exported.Contains(gameObject))
			{
				if (!force)
				{
					path = GetSerializedPath(context, fileName);
					return true;
				}
			}
			else
			{
				exported.Add(gameObject);
			}

			foreach (var cb in BeforeExportGltfCallbackReceivers)
			{
				if (cb.OnBeforeExportGltf(resultPath, instance, context) == false)
				{
					return false;
				}
			}

			context.DependencyRegistry?.RegisterDependency(resultPath, context.Path, context);

			if (!DetectIfAssetHasChangedSinceLastExport(resultPath, source, context))
			{
				path = GetSerializedPath(context, fileName);
				return true;
			}

			var obj = owner as Object;
			using (new Timer("<b>Exports:</b> <i>" + instance.name + ".glb</i>, referenced by " + (obj ? obj.name : owner), obj))
			{
				if (exp.Export(resultPath, false, context) || File.Exists(resultPath))
				{
					path = GetSerializedPath(context, fileName);
					return true;
				}
			}
			path = null;
			return false;
		}

		private static string GetSerializedPath(IExportContext context, string filename)
		{
			return filename.AsRelativeUri();
		}

		private static bool DetectIfAssetHasChangedSinceLastExport(string outputPath, Object sourceAsset, IExportContext context)
		{
			// If this feature is disabled we always want to export
			if (ExporterProjectSettings.instance.smartExport == false)
			{
				return true;
			}

			// TODO: I think we dont need this anymore since we now pass in the source asset
			var assetPath = AssetDatabase.GetAssetPath(sourceAsset);
			if (!EditorUtility.IsPersistent(sourceAsset))
			{
				// if the object is set to hide and dont save it's a prefab temporary instantiated for export
				// TODO: this must be removed and we should merge this with Export.cs
				if (sourceAsset.hideFlags == HideFlags.HideAndDontSave)
				{
					assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceAsset);
					sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				}
			}

			if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return true;

			var fileInfo = new FileInfo(outputPath);
			// If the file is less than 1KB, it's probably not a valid glTF file
			if (fileInfo.Exists && fileInfo.Length <= 1024) return true;
			
			var checkIfAssetHasChanged = fileInfo.Exists;
			if (checkIfAssetHasChanged && context is IHasBuildContext hasBuildContext)
			{
				if (hasBuildContext.BuildContext != null)
				{
					// never check if the asset has changed if we are exporting via context menu
					if (hasBuildContext.BuildContext.ViaContextMenu == false) 
						// also never check for dist builds
						if (hasBuildContext.BuildContext.IsDistributionBuild && !AssetDependencyCache.IsSupported)
							checkIfAssetHasChanged = false;
				}
			}

			if (checkIfAssetHasChanged)
			{
				if (context.TryGetAssetDependencyInfo(sourceAsset, out var info))
				{
					if (!info.HasChanged)
					{
						if (File.Exists(outputPath))
						{
							var msg = "~ Skip exporting " + Path.GetFileName(outputPath) + " → it has not changed\nYou may disable " +
							          nameof(ExporterProjectSettings.instance.smartExport) +
							          " in <b>ProjectSettings/Needle</b> if you think this is not working correctly.";
							Debug.Log(msg.LowContrast());
							return false;
						}

						if (AssetDependencyCache.TryRestoreFromCache(outputPath))
						{
							var msg = "~ Restored from export cache " + Path.GetFileName(outputPath) + "\nYou may disable " +
							          nameof(ExporterProjectSettings.instance.smartExport) +
							          " in <b>ProjectSettings/Needle</b> if you think this is not working correctly.";
							Debug.Log(msg.LowContrast());
							return false;
						}
					}
					else
					{
						info.WriteToCache();
					}
				}
			}

			return true;
		}
	}
}