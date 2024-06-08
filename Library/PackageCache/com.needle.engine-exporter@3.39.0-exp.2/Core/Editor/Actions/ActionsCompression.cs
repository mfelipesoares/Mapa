using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace Needle.Engine
{
	public static class ActionsCompression
	{
		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Compress/Preview Build (Progressive + Compression)")]
		private static async void PreviewBuild()
		{
			var exportInfo = ExportInfo.Get();
			var directory = exportInfo ? Path.GetFullPath(exportInfo.GetProjectDirectory()) : "";
			using (new NeedleLock(directory))
			{
				var assetsDirectory = Path.GetFullPath(ProjectInfo.GetAssetsDirectory());
				if (Directory.Exists(assetsDirectory))
				{
					await Tools.Transform(assetsDirectory);
				}
				else Debug.LogError("Can not preview compression because assets directory does not exist at " + assetsDirectory);
			}
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Compress/Apply Compression")]
		private static async void CompressLocalFilesMenu() => await CompressLocalFiles();
		internal static Task<bool> CompressLocalFiles()
		{
			var directory = Path.GetFullPath(ProjectInfo.GetAssetsDirectory());
			return CompressFiles(directory);
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Compress/Make Progressive")]
		private static async void MakeProgressiveLocalFilesMenu() => await MakeProgressiveLocalFiles();
		private static Task MakeProgressiveLocalFiles()
		{
			var exportInfo = ExportInfo.Get();
			if (!exportInfo) return Task.CompletedTask;
			var assetsDirectory = Path.GetFullPath(ProjectInfo.GetAssetsDirectory());
			return MakeProgressive(assetsDirectory);
		}


		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Compress/Clear Caches", priority = 10_000)]
		private static async void ClearCaches()
		{
			if (EditorUtility.DisplayDialog("Clear Caches", "Are you sure you want to clear the gltf compression caches?", "Yes", "No, do not clear caches"))
			{
				await Tools.ClearCaches();
			}
		}

		internal static Task<bool> CompressFiles(string directoryOrFile)
		{
			return Tools.Transform_Compress(directoryOrFile);
		}

		internal static Task<bool> MakeProgressive(string directory)
		{
			return Tools.Transform_Progressive(directory);
		}

		// internal static Task<bool> MakeProgressiveSingle(string glbPath)
		// {
		// 	glbPath = Path.GetFullPath(glbPath).Replace("\\", "/");
		// 	var cmd = "npm run transform:make-progressive \"" + glbPath + "\"";
		// 	var workingDirectory = TransformPackagePath;
		// 	Debug.Log("<b>Begin transform progressive</b>: " + glbPath + "\ncwd: " + workingDirectory);
		// 	var task = ProcessHelper.RunCommand(cmd, workingDirectory);
		// 	task.ContinueWith(t => Debug.Log("<b>End transform progressive</b>: " + glbPath));
		// 	return task;
		// }

		// private static readonly Regex makeProgressiveNodeDirectory = new Regex("\"transform:make-progressive\": \"node (?<path>.+)\"?");
		//
		// /// <summary>
		// /// Get script location for progressive texture conversion from engine package json
		// /// e.g. "transform:make-progressive": "node C:/git/needle-gltf-extensions/package/make-progressive.mjs",
		// /// If the string after node is not found it will fallback to use the engine directory
		// /// </summary>
		// private static string GetProgressiveTextureScriptDirectory(string dir)
		// {
		// 	var packageJson = Path.GetFullPath(dir + "/package.json");
		// 	if (File.Exists(packageJson))
		// 	{
		// 		var text = File.ReadAllText(packageJson);
		// 		var match = makeProgressiveNodeDirectory.Match(text);
		// 		if (match.Success)
		// 		{
		// 			var path = match.Groups["path"].Value;
		// 			if (File.Exists(path))
		// 			{
		// 				return Path.GetDirectoryName(path);
		// 			}
		// 		}
		// 	}
		// 	// default is engine directory
		// 	return dir + "/node_modules/@needle-tools/engine";
		// }
	}
}