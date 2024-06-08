using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Needle.Engine.Codegen;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.ProjectBundle
{
	// can not be a scriptable object because this should not require a dependency to or core package
	public class Bundle
	{
		internal static bool TryGetFromPath(string dirOrNpmdefPath, out Bundle res)
		{
			if (File.Exists(dirOrNpmdefPath))
			{
				// try to find the registered bundle for it (which is external) and matches the path
				var npmdefPath = new FileInfo(dirOrNpmdefPath);
				foreach (var bundle in BundleRegistry.Instance.Bundles)
				{
					var otherNpmdefPath = new FileInfo(bundle.FilePath);
					if (otherNpmdefPath.Exists && otherNpmdefPath.FullName == npmdefPath.FullName)
					{
						res = bundle;
						return true;
					}
				}
			}
			res = null;
			return false;
		}
		
		[JsonIgnore] public string Name => System.IO.Path.GetFileNameWithoutExtension(FilePath) + "~";

		/// <summary>
		/// If an npmdef does not exist next to this Asset this is the path to the package (project relative if possible)
		/// </summary>
		[JsonProperty] internal string localPath;

		private static string _lastSelectedPath
		{
			get => SessionState.GetString("needle.move.npmdef", "");
			set => SessionState.SetString("needle.move.npmdef", value);
		}

		internal void SetLocalPath(string path)
		{
			localPath = PathUtils.MakeProjectRelative(path);
		}
		
		internal bool SelectLocalPath()
		{
			var dialoguePath = _lastSelectedPath;
			if (this.IsLocal || string.IsNullOrWhiteSpace(_lastSelectedPath))
			{
				if(Directory.Exists(PackageDirectory))
					dialoguePath = PackageDirectory;
			}
			var selectedPath = EditorUtility.OpenFolderPanel("Select npm package", dialoguePath, "");
			if (string.IsNullOrEmpty(selectedPath))
			{
				Debug.Log("Selecting external path cancelled.");
				return false;
			}
			_lastSelectedPath = selectedPath;
			if (File.Exists(selectedPath + "/package.json"))
			{
				Debug.Log("Selected directory: " + selectedPath);
				localPath = PathUtils.MakeProjectRelative(selectedPath);
				Save(FilePath);
				BundleRegistry.Instance.RunCodeGenForBundle(this);
				return true;
			}
			var dirInfo = new DirectoryInfo(selectedPath);
			var oldPath = PackageDirectory != null ? new DirectoryInfo(PackageDirectory) : null;
			if(dirInfo.Exists && oldPath != null)
			{
				if (oldPath.FullName == dirInfo.FullName)
				{
					Debug.Log("Selected directory is the same as the current one - nothing to do here: " + dirInfo.FullName);
					return true;
				}
				var unityProjectDir = new DirectoryInfo(Application.dataPath);
				var isInUnityProject = dirInfo.FullName.StartsWith(unityProjectDir!.FullName);
				var needsInstall = true;
				
				Engine.Actions.StopLocalServer();
				if (oldPath.Exists)
				{
					var targetPath = selectedPath + "/" + oldPath.Name;
					// Make sure if moving the package into the unity directory that it ends with a ~
					if (isInUnityProject && !targetPath.EndsWith("~"))
					{
						Debug.LogWarning("The selected directory is inside the Unity project → we will move the package to a hidden folder (ending with ~) so that Unity will not import all the files in node_modules");
						targetPath += "~";
					}
					if (Directory.Exists(targetPath))
					{
						// If the target path already exists and is NOT empty we can not move there
						if (new DirectoryInfo(targetPath).EnumerateFileSystemInfos().Any())
						{
							Debug.LogError("Selected directory already contains a folder named " + oldPath.Name +
							               ". Please select a different location or remove the folder at " +
							               targetPath);
							return false;
						}
						// otherwise we have to delete the empty directory to be able to move the package to it
						Directory.Delete(targetPath);
					}
					selectedPath = targetPath;
					if (!FileUtils.MoveFiles(oldPath.FullName, targetPath))
					{
						Debug.LogError("Moving package from \"" + oldPath + "\" to \"" + selectedPath + "\" failed. You have to move it manually.");
						EditorUtility.RevealInFinder(oldPath.FullName);
					}
					else Debug.Log("Moved package to " + selectedPath);
				}
				else
				{
					if (ProjectWindowActions.DoesUserWantToCreateANewNpmPackageAtSelectedPath(selectedPath))
					{
						needsInstall = false;
						ProjectWindowActions.CreateNewNpmPackageForLinkedBundle(this, selectedPath);
					}
					else
					{
						Debug.Log("Selected directory is not a npm package: it does not contain a package.json\n" + selectedPath);
						return false;
					}
				}
				localPath = PathUtils.MakeProjectRelative(selectedPath);
				Save(FilePath);
				BundleRegistry.Instance.RunCodeGenForBundle(this);
				if(needsInstall)
					Install();
				return true;
			}
			return false;
		}

		internal void RemoveExternalPath()
		{
			localPath = null;
		}
		
		/// <summary>
		/// Local means that the package exists on local disc but not inside the Unity project next to the npmdef
		/// </summary>
		public bool IsLocal => localPath != null && Directory.Exists(localPath);
		/// <summary>
		/// Embedded means that the package exists next to the npmdef in the Unity project. It is hidden with a ~
		/// </summary>
		public bool IsEmbedded => Directory.Exists(GetEmbeddedPath());

		public bool IsMutable()
		{
			var fp = System.IO.Path.GetFullPath(FilePath);
			return !fp.Contains("Library\\PackageCache");
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(Name) && File.Exists(PackageFilePath);
		}

		public bool Validate()
		{
			var dir = PackageDirectory;
			if (Directory.Exists(dir))
			{
				if (!NeedlePackageConfig.Exists(dir)) 
					NeedlePackageConfig.Create(dir);
				return true;
			}
			return false;
		}
		
		// TODO: cache this to avoid file reads
		[JsonIgnore]
		public string PackageDirectory
		{
			get
			{
				if (localPath != null && Directory.Exists(localPath))
					return Path.GetFullPath(localPath);
				return GetEmbeddedPath();
			}
		}

		private string GetEmbeddedPath()
		{
			var path = Name;
			if (path.EndsWith("package.json"))
				path = Path.GetDirectoryName(path);
			var dir = Path.GetDirectoryName(FilePath);
			var fullDir = Path.GetFullPath(dir + "/" + path);
			return fullDir;
		}

		/// <summary>
		/// Path to package json
		/// </summary>
		[JsonIgnore]
		public string PackageFilePath => PackageDirectory + "/package.json";

#if UNITY_EDITOR
		internal NpmDefObject LoadAsset()
		{
			return AssetDatabase.LoadAssetAtPath<NpmDefObject>(FilePath);
		}
#endif

		[JsonIgnore]
		internal string FilePath
		{
			get => _filePath;
			set
			{
				if (string.Equals(value, this._filePath, StringComparison.Ordinal)) return;
				this._filePath = value;
				codeGenDirectory = null;
			}
		}

		[JsonIgnore, NonSerialized] private string _filePath;

		public string FindPackageName()
		{
			var path = PackageDirectory + "/package.json";
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
			var name = PackageUtils.GetPackageName(path);
			return name;
		}

		public string FindPackageVersion()
		{
			var path = PackageDirectory + "/package.json";
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
			if (PackageUtils.TryGetVersion(path, out var version))
				return version;
			return null;
		}

		private DirectoryInfo codeGenDirectory = null;

		public string FindScriptGenDirectory()
		{
			if (codeGenDirectory == null)
			{
				var dir = new FileInfo(FilePath);
				codeGenDirectory = new DirectoryInfo(dir.DirectoryName + "/" + System.IO.Path.GetFileNameWithoutExtension(FilePath) + ".codegen");
			}
			return codeGenDirectory.FullName;
		}

		public bool IsInstalled(string packageJsonPath)
		{
			if (packageJsonPath != null && File.Exists(packageJsonPath))
				return PackageUtils.IsDependency(packageJsonPath, FindPackageName());
			return false;
		}

		public void Install(ExportInfo exportInfo = null)
		{
			var exp = exportInfo ? exportInfo : ExportInfo.Get();
			if (!exp) return;
			var path = PackageDirectory;
			var projectDirectory = exp.GetProjectDirectory();
			if (PackageUtils.AddPackage(projectDirectory, path))
			{
				Debug.Log("<b>Added package</b> " + FilePath + " to " + exp.PackageJsonPath.AsLink());
				TypesUtils.MarkDirty();
				Actions.AddToWorkspace(projectDirectory, FindPackageName());
			}
			else
				Debug.LogWarning("Installation failed: " + path);
		}

		public void Uninstall(ExportInfo exp = null)
		{
			exp = exp ? exp : ExportInfo.Get();
			if (!exp) return;
			var name = FindPackageName();
			if (PackageUtils.TryReadDependencies(exp.PackageJsonPath, out var deps))
			{
				if (deps.ContainsKey(name))
				{
					deps.Remove(name);
					if (PackageUtils.TryWriteDependencies(exp.PackageJsonPath, deps))
					{
						Actions.RemoveFromWorkspace(exp.GetProjectDirectory(), FindPackageName());
						Debug.Log("<b>Removed package</b> " + name + " from " + exp.PackageJsonPath.AsLink());
					}
				}
			}
		}

		public Task<bool> RunInstall()
		{
			return Actions.InstallBundleTask(this);
		}

		internal void Save(string path)
		{
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
			BundleRegistry.Instance.MarkDirty();
			AssetDatabase.Refresh();
			// AssetDatabase.ImportAsset(FilePath, ImportAssetOptions.ForceSynchronousImport);
		}

		internal void FindImports(List<ImportInfo> list, [CanBeNull] string projectDirectory)
		{
			var packageDir = PackageDirectory;
			if (!Directory.Exists(packageDir)) return;
			var installed = projectDirectory == null || IsInstalled(projectDirectory + "/package.json");
			var startCount = list.Count;
			TypeScanner.FindTypes(packageDir, list, SearchOption.TopDirectoryOnly, true);
			RecursiveFindTypesIgnoringNodeModules(list, packageDir);
			for (var i = startCount; i < list.Count; i++)
				list[i].IsInstalled = installed;
		}

		internal IEnumerable<string> EnumerateDirectories(bool skipNodeModule = true, int maxLevel = 2)
		{
			IEnumerable<string> EnumerateDir(DirectoryInfo currentDirectory, int currentLevel)
			{
				if (!currentDirectory.Exists) yield break;
				if (skipNodeModule && currentDirectory.Name == "node_modules") yield break;
				if(currentDirectory.Name.EndsWith("codegen")) yield break;
				yield return currentDirectory.FullName;
				if (currentLevel >= maxLevel) yield break;
				var dirs = currentDirectory.GetDirectories();
				foreach (var d in dirs)
				{
					foreach (var sub in EnumerateDir(d, currentLevel + 1))
						yield return sub; 
				}
			}

			var dir = new DirectoryInfo(PackageDirectory);
			return EnumerateDir(dir, 0);
		}

		private static void RecursiveFindTypesIgnoringNodeModules(List<ImportInfo> list, string currentDir)
		{
			if (!Directory.Exists(currentDir)) return;
			foreach (var dir in Directory.EnumerateDirectories(currentDir))
			{
				if (dir.EndsWith("node_modules")) continue;
				TypeScanner.FindTypes(dir, list, SearchOption.TopDirectoryOnly);
				RecursiveFindTypesIgnoringNodeModules(list, dir);
			}
		}

		// private void FindCodeGenDirectory(ref DirectoryInfo dir)
		// {
		// 	if (dir?.Exists ?? false) return;
		// 	var currentDirectory = System.IO.Path.GetDirectoryName(FilePath);
		// 	Debug.Log(currentDirectory);
		// 	var folders = new string[] { currentDirectory };
		// 	var guids = AssetDatabase.FindAssets("t:" + nameof(AssemblyDefinitionAsset), folders);
		// 	foreach (var guid in guids)
		// 	{
		// 		var path = AssetDatabase.GUIDToAssetPath(guid);
		// 		var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
		// 		
		// 	} 
		// 	// while (currentDirectory != null)
		// 	// {
		// 	// 	foreach (var asmdefPath in Directory.EnumerateFiles(currentDirectory, "*.asmdef", SearchOption.TopDirectoryOnly))
		// 	// 	{
		// 	// 		// Compiler
		// 	// 	}
		// 	// }
		// }
	}
}