using System;
using System.IO;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Editors
{
	internal static class ExportInfoContextMenu
	{
		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Internal/Move Project")]
		private static void MoveProject(MenuCommand cmd)
		{
			var context = cmd.context as ExportInfo;
			if (!context) return;
			if (context.Exists() == false)
			{
				Debug.LogError("Can not move project that does not exist: " + context.DirectoryName, context);
				return;
			}
			var source = Path.GetFullPath(context.GetProjectDirectory());
			var projectName = PathUtils.GetDirectoryNameSafe(source);
			var selectedDirectory = EditorUtility.OpenFolderPanel("Move Project: " + projectName, source, "");
			if (!string.IsNullOrWhiteSpace(selectedDirectory))
			{
				var newDirectory = (selectedDirectory + "/" + projectName).Replace("\\", "/");
				source = source.Replace("\\", "/");
				if (newDirectory == source)
				{
					Debug.Log("Selected same directory - your project is already at " + newDirectory);
					return;
				}
				if (newDirectory.StartsWith(source))
				{
					Debug.LogError("You can not move a project into itself. Please select a different directory.", context);
					return;
				}
				
				if (EditorUtility.DisplayDialog("Move Project",
					    "Do you want to move the project from " + source + " to " + newDirectory + "?", "Yes move",
					    "No cancel") == false)
				{
					Debug.Log("Moving project cancelled");
					return;
				}
				
				var relativeDirectory = PathUtils.MakeProjectRelative(newDirectory);
				if (!ExportInfo.IsValidDirectory(relativeDirectory, out var reason))
				{
					Debug.LogError("Selected invalid directory: " + reason + "\n" + newDirectory);
					return;
				}

				Debug.Log("Will move project from " + source + " to " + newDirectory);
				if (FileUtils.MoveFiles(source, newDirectory))
				{
					context.DirectoryName = PathUtils.MakeProjectRelative(newDirectory);
					Debug.Log("Moved project to " + newDirectory.AsLink());
				}
			}
			else Debug.Log("Moving project cancelled");
		}
		
		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Update/Update Vite Config From Template")]
		private static void UpdateViteConfig(MenuCommand cmd)
		{
			var context = cmd.context as ExportInfo;
			if (context)
			{
				var viteConfigPath = new FileInfo(context.GetProjectDirectory() + "/" + ViteUtils.ViteConfigName);
				if (viteConfigPath.Exists)
				{
					var userWantsToUpdate = EditorUtility.DisplayDialog("Update vite.config",
						"Do you want to update the vite.config.js file in " + context.GetProjectDirectory() +
						" from the vite project template? A backup of your original file will be created.", "Yes update", "No do nothing");
					if (!userWantsToUpdate)
					{
						Debug.Log("Update cancelled");
						return;
					}
					var templateVite = AssetDatabase.GUIDToAssetPath("b883d0e9d63cadb45ab8599cb98b7501");
					if (!File.Exists(templateVite))
					{
						Debug.LogWarning("Could not find vite.config in template files. This is a bug - please report. Thank you!");
						return;
					}

					// create a backup copy
					var backupDir = $"{Application.dataPath}/../Library/Needle/Backups";
					var backupPath = $"{backupDir}/{viteConfigPath.Name}-{GetTimestamp()}.js";
					Directory.CreateDirectory(backupDir);
					File.Copy(viteConfigPath.FullName, backupPath, true);
					Debug.Log("Created backup at " + backupPath.AsLink());
					File.Copy(templateVite, viteConfigPath.FullName, true);
					Debug.Log("Updated vite.config at " + viteConfigPath.FullName.AsLink() + " from " + templateVite);
				}
				else
				{
					Debug.LogWarning("No vite.config file found at " + viteConfigPath);
				}
			}
		}

		private static string GetTimestamp()
		{
			return DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Build Distribution/Open Build Window")]
		private static void OpenBuildWindow(MenuCommand cmd)
		{
			EditorApplication.ExecuteMenuItem("File/Build Settings...");
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Build Distribution/Production")]
		private static void RunProductionBuild(MenuCommand cmd)
		{
			Actions.ExportAndBuildProduction();
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Build Distribution/Development")]
		private static void RunDevelopmentBuild(MenuCommand cmd)
		{
			Actions.ExportAndBuildDevelopment();
		}
	}
}