using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Core.References.ReferenceResolvers;
using Needle.Engine.Editors;
using Needle.Engine.Gltf.UnityGltf;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf
{
	internal static class Export_Selected
	{
		private const string ExportGlbRoot = Constants.MenuItemRoot + "/Export Selection/";
		private const int ExportsGlbOrder = MenuItems.MenuItemOrderExport;

		private const string ExportGlb = "Export to GLB";
		private const string ExportGlbCompressed = "Export to compressed GLB";
		private const string ExportGlbCompressedNoProgressive = "Export to compressed GLB (no progressive)";
		private const string ExportGltf = "Export to glTF";
		private const string ExportGltfCompressed = "Export to compressed glTF";
		
		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlb, true)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlb, true)]
		[MenuItem(ExportGlbRoot + ExportGlb, true)]
		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlbCompressed, true)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlbCompressed, true)]
		[MenuItem(ExportGlbRoot + ExportGlbCompressed, true)]
		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlbCompressedNoProgressive, true)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlbCompressedNoProgressive, true)]
		[MenuItem(ExportGlbRoot + ExportGlbCompressedNoProgressive, true)]
		[MenuItem(Constants.AssetsMenuItemRoot + ExportGltf, true)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGltf, true)]
		[MenuItem(ExportGlbRoot + ExportGltf, true)]
		[MenuItem(Constants.AssetsMenuItemRoot + ExportGltfCompressed, true)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGltfCompressed, true)]
		[MenuItem(ExportGlbRoot + ExportGltfCompressed, true)]
		private static bool HasObjectSelectedValidateFunction()
		{
			return Selection.activeObject || Selection.objects.Length > 0;
		}

		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlb, false, 31)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlb, false, 31)]
		[MenuItem(ExportGlbRoot + ExportGlb, priority = ExportsGlbOrder)]
		private static void ExportSelectedObject() => ExportSelectedObject(true, false);

		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlbCompressed, false, 31)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlbCompressed, false, 31)]
		[MenuItem(ExportGlbRoot + ExportGlbCompressed, priority = ExportsGlbOrder)]
		private static void ExportSelectedObjectCompressed() => ExportSelectedObject(true, true);

		[MenuItem(Constants.AssetsMenuItemRoot + ExportGlbCompressedNoProgressive, false, 31)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGlbCompressedNoProgressive, false, 31)]
		[MenuItem(ExportGlbRoot + ExportGlbCompressedNoProgressive, priority = ExportsGlbOrder)]
		private static void ExportSelectedObjectCompressed_NoProgressive() => ExportSelectedObject(true, true, false);

		[MenuItem(Constants.AssetsMenuItemRoot + ExportGltf, false, 31)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGltf, false, 31)]
		[MenuItem(ExportGlbRoot + ExportGltf, priority = ExportsGlbOrder)]
		private static void ExportSelectedObjectGltf() => ExportSelectedObject(false, false);

		[MenuItem(Constants.AssetsMenuItemRoot + ExportGltfCompressed, false, 31)]
		[MenuItem(Constants.GameObjectMenuItemRoot + ExportGltfCompressed, false, 31)]
		[MenuItem(ExportGlbRoot + ExportGltfCompressed, priority = ExportsGlbOrder)]
		private static void ExportSelectedObjectGltfCompressed() => ExportSelectedObject(false, true);

		private static readonly Dictionary<string, Task> _exportTasks = new Dictionary<string, Task>();

		private static void ExportSelectedObject(bool binary, bool compress, bool progressive = true)
		{
			if (Selection.activeObject && Selection.objects.Length <= 0)
			{
				Debug.LogWarning("Can not run export because you don't have a object in the hierarchy selected");
				return;
			}
			if (EulaWindow.RequiresEulaAcceptance)
			{
				Debug.LogWarning("In order to start exporting Needle Engine assets, you need to accept the EULA first.", ExportInfo.Get());
				EulaWindow.Open();
				return;
			}
			
			UnityGltfExportHandler.ResetExported();
			
			foreach (var obj in Selection.objects)
			{
				var outputDirectory = Path.GetFullPath(Application.dataPath + "/../Library/Needle/Export/" + obj.name);
				if (Directory.Exists(outputDirectory))
				{
					try
					{
						Directory.Delete(outputDirectory, true);
					}
					catch (Exception)
					{
						// If deletion doesnt remove all files we throw here
						// the root directory might be locked if we've currently opened the directory with a CLI tool which is fine
						if (Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories).Any())
						{
							throw;
						}
					}
				}
				
				var targetPath = outputDirectory + "/" + obj.name.ToFileName() + (binary ? ".glb" : ".gltf");

				if (_exportTasks.TryGetValue(targetPath, out var t))
				{
					if (t is { IsCompleted: false })
					{
						Debug.LogWarning("Task to export " + obj.name + " already running");
						continue;
					}
				}

				try
				{
					var task = Run();
					_exportTasks.Add(targetPath, task);
				}
				finally
				{
					if (_exportTasks.ContainsKey(targetPath))
						_exportTasks.Remove(targetPath);
				}

				async Task Run()
				{
					Directory.CreateDirectory(outputDirectory);
					var buildContext = BuildContext.Distribution(compress);
					buildContext.ViaContextMenu = true;
					var ctx = new ObjectExportContext(buildContext, obj, outputDirectory, targetPath);
					GltfReferenceResolver.ClearCache();
					var exportInfo = ExportInfo.Get();
					if (exportInfo)
					{
						// TypesUtils.MarkDirty();
						TypesUtils.GetTypes(exportInfo);
					}
					else
					{
						Debug.LogWarning("No " + nameof(ExportInfo) +
						                 " in scene found. Some components might not be exported if they are not part of the core needle engine package. If you want to export objects containing custom components in npm Definition files you need to add a " +
						                 nameof(exportInfo) + " component to your scene and reference those packages in there.");
					}
					if (Export.AsGlb(ctx, obj, out _))
					{
						Debug.Log("Successfully exported to " + targetPath);
						if (compress)
						{
							Debug.Log("Start compressing " + targetPath);
							var res = true;
							if(progressive)
								res &= await ActionsCompression.MakeProgressive(outputDirectory);
							res &= await ActionsCompression.CompressFiles(outputDirectory);

							// var res = await ActionsGltf.Compress(targetPath);
							//
							// var visited = new HashSet<object>();
							// visited.Add(targetPath);
							// var dependencyInfo = new DependencyExportInfo();
							// res &= await CompressDependencies(ctx.DependencyRegistry, visited, dependencyInfo);
							//
							if (!res) Debug.LogError("Failed compressing files in " + outputDirectory);
							else
							{
								Debug.Log($"{"<b>Successfully</b>".AsSuccess()} compressed files in " + outputDirectory);
							}
						}
						EditorUtility.RevealInFinder(targetPath);
					}
					else Debug.LogError("Export failed");
				}
			}
		}

		// public class DependencyExportInfo
		// {
		// 	public readonly List<string> Paths = new List<string>();
		// }
		//
		// private static async Task<bool> CompressDependencies(IDependencyRegistry reg, HashSet<object> visited, DependencyExportInfo info)
		// {
		// 	if (visited.Contains(reg)) return true;
		// 	visited.Add(reg);
		// 	var success = true;
		// 	foreach (var dep in reg.Dependencies)
		// 	{
		// 		success &= await ExportFile(dep.uri);
		// 	}
		// 	foreach (var ctx in reg.Contexts)
		// 	{
		// 		success &= await ExportFile(ctx.Path);
		// 		success &= await CompressDependencies(ctx.DependencyRegistry, visited, info);
		// 	}
		//
		// 	async Task<bool> ExportFile(string uri)
		// 	{
		// 		if (visited.Contains(uri)) return true;
		// 		visited.Add(uri);
		// 		if (File.Exists(uri))
		// 		{
		// 			Debug.Log("<b>Compress</b> " + uri);
		// 			info.Paths.Add(uri);
		// 			if (!await ActionsGltf.Compress(uri))
		// 			{
		// 				Debug.LogWarning("Compressing failed: " + uri);
		// 				return false;
		// 			}
		// 		}
		// 		else Debug.LogWarning("Dependency not found: " + uri);
		// 		return true;
		// 	}
		//
		// 	return success;
		// }
	}
}