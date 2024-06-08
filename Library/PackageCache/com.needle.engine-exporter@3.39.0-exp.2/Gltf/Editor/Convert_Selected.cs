using System;
using System.IO;
using System.Linq;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

namespace Needle.Engine.Gltf
{
	internal static class Convert_Selected
	{
		[MenuItem("Assets/Needle Engine 🌵/Convert GLB to glTF", true)]
		internal static bool ConvertSelectionToGlTF_Validate()
		{
			return Selection.gameObjects.Any(o => AssetDatabase.GetAssetPath(o).EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase));
		}

		[MenuItem("Assets/Needle Engine 🌵/Convert GLB to glTF", false)]
		internal static async void ConvertSelectionToGlTF()
		{
			foreach (var obj in Selection.gameObjects)
			{
				// input paths
				var originalPath = AssetDatabase.GetAssetPath(obj);
				if (!originalPath.EndsWith(".glb"))
				{
					Debug.LogWarning("Unknown file type: " + originalPath);
					continue;
				}
				var metaPath = originalPath + ".meta"; 
				var fullPath = Path.GetFullPath(originalPath);
				var name = Path.GetFileName(originalPath);
				var dir = Path.GetDirectoryName(fullPath)!;
				var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
				
				// output paths
				var newName = nameWithoutExtension + ".gltf";
				var newDirName = nameWithoutExtension;
				var newDir = Path.Combine(dir, newDirName);
				Directory.CreateDirectory(newDir);
				
				/*
				// Conversion with UnityGLTF – we parse the GLB, and write it out as glTF again
				var stream = File.OpenRead(fullPath);
				var glbObject = GLBBuilder.ConstructFromStream(stream, null, null, 0L, false);
				// Write the root back out as glTF, and write all buffer views as separate files.
				// TODO how we can we do that independently of what's actually in the file?
				// We'd need to know which things need to go to disk, even for buffers we don't understand.
				GLTFSceneExporter.SaveGltfAndBin(newDir, newName, glbObject);
				
				return;
				*/
				
				
				// run conversion
				Debug.Log("Converting " + name + " to " + newName);
				var cmd = $"npx gltf-pipeline -i \"{name}\" -o \"{newDirName}/{newName}\" --separate";
				await ProcessHelper.RunCommand(cmd, dir);
				
				var hasWorked = File.Exists(Path.Combine(newDir, newName));
				
				if (hasWorked) {
					// update meta and delete old files
					var newPath = Path.Combine(newDir, newName);
					File.Copy(metaPath, newPath + ".meta", true);
					File.Delete(metaPath);
					File.Delete(originalPath);
					
					// we now set UnityGLTF and call the fix import settings
					AssetDatabase.Refresh();
					var relDirectory = originalPath.Substring(0, originalPath.LastIndexOf("/", StringComparison.Ordinal));
					var newRelPath = relDirectory + "/" + newDirName + "/" + newName;	
					AssetDatabase.SetImporterOverride<GLTFImporter>(newRelPath);
					var importer = AssetImporter.GetAtPath(newRelPath) as GLTFImporter;
					GLTFImporterHelper.FixTextureImportSettings(importer);
					// theoretically we could revert the change here... but we want to use UnityGLTF in the Needle Engine context
				}
				else
				{
					Debug.LogError("Conversion failed. Please see console logs for more info.");
				}
			}
			AssetDatabase.Refresh();
		}
	}
}