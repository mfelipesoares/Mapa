using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

#if UNITY_EDITOR
using Unity.CodeEditor;
using UnityEditor;
#endif

namespace Needle.Engine.Utils
{
	public static class WorkspaceUtils
	{
		public static async void OpenWorkspace(string directory, bool allowChangingTitle = true, bool useDefaultEditor = false, string filename = null)
		{
			directory = Path.GetFullPath(directory);
			directory = PathUtils.ResolveSymlink(directory);
			
				
			// first check if the npmdef is a dependency of the current web project
			// if yes we try to open the workspace of the web project instead
			var exp = ExportInfo.Get();
			if (exp && exp.Exists())
			{
				var packageName = PackageUtils.GetPackageName(directory + "/package.json");
				if (PackageUtils.IsDependency(exp.PackageJsonPath, packageName))
				{
					var newDirectory = exp.GetProjectDirectory();
					if (filename != null)
					{
						if (filename.StartsWith("Packages/") || filename.StartsWith("Assets/"))
							filename = Path.GetFullPath(filename);
						// try to make the filename path a "node_modules path"
						if (Path.IsPathRooted(filename) && filename.StartsWith(directory))
						{
							filename = filename?.Substring(directory.Length + 1);
						}
						var newFileName = newDirectory + "/node_modules/" + packageName + "/" + filename;
						if (File.Exists(newFileName)) filename = newFileName;
					}
					// the file is a dependency of the current web project
					// make sure the workspace local folders are up to date / synced with the package.json local dependencies
					if(TryGetWorkspace(newDirectory, out var workspacePath))
						AddLocalPackages(exp.PackageJsonPath, workspacePath);
					
					OpenWorkspace(newDirectory, allowChangingTitle, useDefaultEditor, filename);
					return;
				}
			}

			if (useDefaultEditor && await OpenWithDefaultEditor(directory, filename))
			{
			}
			else if (TryGetWorkspace(directory, out var workspacePath))
			{
				var packageJson = directory + "/package.json";
				if (File.Exists(directory))
					AddLocalPackages(packageJson, workspacePath);
#if UNITY_EDITOR
				EditorUtility.OpenWithDefaultApp(workspacePath);
				if (!string.IsNullOrEmpty(filename))
				{
					// sometimes this is a virtual path (so we cannot use "IsRooted"), sometimes it may be absolute already... sometimes it's a relative path to the directory
					if(!File.Exists(filename))
						filename = Path.Combine(directory, filename);
					filename = Path.GetFullPath(filename);
					if (File.Exists(filename)) 
						EditorUtility.OpenWithDefaultApp(filename);
				}
#endif

				if (allowChangingTitle)
				{
					TryWriteWorkspaceTitle(workspacePath);
				}
			}
			else if(await OpenWithCode(directory))
			{
				// Ignore
			}
			else
			{
				await OpenWithDefaultEditor(directory, filename);
			}
		}

		private static async Task<bool> OpenWithCode(string directory, string file = null)
		{
			var cmd = "code \"" + directory + "\"";
			if(!string.IsNullOrEmpty(file)) cmd += " --file \"" + file + "\"";
			return await ProcessHelper.RunCommand(cmd, null);
		}

		public static async Task<bool> OpenWithDefaultEditor(string directory, string withfile = null)
		{
#if UNITY_EDITOR
			var inst = CodeEditor.CurrentEditorInstallation;
			if (inst.IndexOf("rider", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				// TODO: open withfile if it exists
				await ProcessHelper.RunCommand($"\"{inst}\" \"{directory}\"", null);
				return true;
			}
			if (inst.IndexOf("visual studio", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				// TODO: open withfile if it exists
				await ProcessHelper.RunCommand($"\"{inst}\" /Edit \"{directory}\"", null);
				return true;
			}
			if (inst.IndexOf("code", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return await OpenWithCode(directory, withfile);
			}
#endif
			return await Task.FromResult(false);
		}
 
		public static bool TryWriteWorkspaceTitle(string workspacePath, string title = null, bool allowOverride = true)
		{
			if (File.Exists(workspacePath))
			{
				try
				{
					var workspaceFileInfo = new FileInfo(workspacePath);
					// Default title name is parent/directory name
					if (title == null) title = workspaceFileInfo.Directory?.Name;
					var workspace = File.ReadAllText(workspacePath);
					var obj = JsonConvert.DeserializeObject<JObject>(workspace);
					if (obj["settings"] == null) obj["settings"] = new JObject();
					var settings = (JObject)obj["settings"];
					var titleObj = settings["window.title"] as JValue;
					if (titleObj == null || titleObj.Value<string>() != title)
					{
						if (allowOverride == false && titleObj != null)
						{
							return false;
						}
						settings["window.title"] = title;
						File.WriteAllText(workspacePath, obj.ToString());
					}
					return true;
				}
				catch
				{
					// Ignore
				}
			}
			return false;
		}
		
		public static bool TryGetWorkspace(string directory, out string workspacePath)
		{
			if (string.IsNullOrWhiteSpace(directory))
			{
				workspacePath = null;
				return false;
			}
			directory = directory.TrimEnd('\\').TrimEnd('/');
			directory = Path.GetFullPath(directory);
			workspacePath = Directory.GetFiles(directory, "*.code-workspace", SearchOption.TopDirectoryOnly).FirstOrDefault();
			var success = !string.IsNullOrEmpty(workspacePath);
			return success;
		}

		public static void AddLocalPackages(string packageJson, string workspacePath)
		{
			if (!PackageUtils.TryReadDependencies(packageJson, out var deps)) return;

			if (TryReadWorkspace(workspacePath, out var workspaceContent))
			{
				var changed = false;
				foreach (var dep in deps)
				{
					if (dep.Value.EndsWith("~") || dep.Value.StartsWith("file:"))
					{
						if (AddToFolders(workspaceContent, dep.Key))
							changed = true;
					}
				}
				changed |= PrettifyWorkspace(workspaceContent);
				if (changed)
				{
					WriteWorkspace(workspaceContent, workspacePath);
				}
			}
		}

		private static bool TryEnsureWorkspace(ref string workspacePathOrDirectory)
		{
			if (!workspacePathOrDirectory.EndsWith(".code-workspace") && (File.Exists(workspacePathOrDirectory) || Directory.Exists(workspacePathOrDirectory)))
			{
				var attr = File.GetAttributes(workspacePathOrDirectory);
				var dir = workspacePathOrDirectory;
				if (attr.HasFlag(FileAttributes.Directory) == false)
				{
					dir = Path.GetDirectoryName(workspacePathOrDirectory);
				}
				if (dir != null)
				{
					workspacePathOrDirectory = Directory.GetFiles(dir, "*.code-workspace", SearchOption.TopDirectoryOnly).FirstOrDefault();
				}
			}
			return workspacePathOrDirectory != null && File.Exists(workspacePathOrDirectory);
		}

		public static bool TryReadWorkspace(string workspacePath, out JObject obj)
		{
			try
			{
				obj = null;
				if (!TryEnsureWorkspace(ref workspacePath)) return false;
				var text = File.ReadAllText(workspacePath);
				obj = JsonConvert.DeserializeObject<JObject>(text);
				return obj != null;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				obj = null;
				return false;
			}
		}

		public static void WriteWorkspace(JObject workspace, string workspacePath)
		{
			if (!TryEnsureWorkspace(ref workspacePath)) return;
			var result = JsonConvert.SerializeObject(workspace, Formatting.Indented);
			File.WriteAllText(workspacePath, result);
		}

		/// <returns>True if was added, false if already in array</returns>
		public static bool AddToFolders(JObject workspace, string name, string path = null)
		{
			var foldersArray = (JArray)workspace["folders"] ?? new JArray();
			for (var index = foldersArray.Count - 1; index >= 0; index--)
			{
				var jToken = foldersArray[index];
				var entry = (JObject)jToken;
				var currentName = entry["name"]?.Value<string>();

				// get the display name map
				foreach (var kvp in displayNameMap)
				{
					if(kvp.Value == currentName)
					{
						currentName = kvp.Key;
						break;
					}
				}
				
				if (currentName == name)
				{
					return false;
				}
			}
			var dependency = new JObject();
			var newPath = path ?? "node_modules/" + name;
			dependency["name"] = name;
			dependency["path"] = newPath;
			dependency["@needle:managed"] = true;
			foldersArray.Add(dependency);
			workspace["folders"] = foldersArray;
			return true;
		}

		public static bool RemoveFromFolders(JObject workspace, string name)
		{
			var folders = (JArray)workspace["folders"];
			if (folders == null) return false;
			var changed = false;
			for (var index = folders.Count - 1; index >= 0; index--)
			{
				var jToken = folders[index];
				var entry = (JObject)jToken;
				var currentName = entry["name"]?.Value<string>();
				
				// get the display name map
				foreach (var kvp in displayNameMap)
				{
					if(kvp.Value == currentName)
					{
						currentName = kvp.Key;
						break;
					}
				}
				if (currentName == name)
				{
					folders.RemoveAt(index);
					changed = true;
				}
			}
			return changed;
		}
		
		private static readonly Dictionary<string, string> displayNameMap = new Dictionary<string, string>()
		{
			{"@needle-tools/engine", "🌵 Needle Engine"},
		};
		
		public static bool PrettifyWorkspace(JObject workspace)
		{
			var folders = (JArray)workspace["folders"];
			var changed = false;
			if (folders != null)
			{
				for (var index = 0; index < folders.Count; index++)
				{
					var jToken = folders[index];
					var entry = (JObject)jToken;
					var path = entry["path"]?.Value<string>();
					
					// ensure that the engine is always displayed as "🌵 Needle Engine"
					if (path != null && path.Contains("node_modules/@needle-tools/engine"))
					{
						if (entry["name"] == null)
						{
							entry["name"] = "🌵 Needle Engine";
							changed = true;
						}
					}
					
					var name = entry["name"]?.Value<string>();
					if(name != null && displayNameMap.TryGetValue(name, out var displayName))
					{
						entry["name"] = displayName;
						changed = true;
					}
				}
			}
			return changed;
		}
	}
}