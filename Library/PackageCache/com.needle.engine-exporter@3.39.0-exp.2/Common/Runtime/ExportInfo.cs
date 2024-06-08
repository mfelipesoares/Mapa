using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR_WIN
using Needle.Engine.Utils;
using UnityEditor;
#endif

namespace Needle.Engine
{
	[HelpURL(Constants.DocumentationUrl)]
	[AddComponentMenu("Needle Engine/Export Info (Web Project)" + Constants.NeedleComponentTags)]
	[NeedleEngineIgnore]
	public class ExportInfo : MonoBehaviour, IProjectInfo
	{
		[CanBeNull]
		public static ExportInfo Get(bool findInactive = false)
		{
// UNITY_2020_3_45_OR_NEWER || UNITY_2022_2_5_OR_NEWER || UNITY_2021_3_18_OR_NEWER
#if UNITY_2023_1_OR_NEWER
			return FindAnyObjectByType<ExportInfo>(findInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
			return FindObjectOfType<ExportInfo>(findInactive);
#endif
		}
		internal static event Action<IProjectInfo> RequestGeneratingTempProject;

		string IProjectInfo.ProjectDirectory => GetProjectDirectory();
		string IProjectInfo.AssetsDirectory => GetProjectDirectory() + "/assets";
		string IProjectInfo.BaseUrl => NeedleProjectConfig.TryGetBaseUrl(out var baseUrl) ? baseUrl : null;

		public static string GetFullPath(string directoryName) => Path.GetFullPath(Application.dataPath + "/../" + directoryName);

		[Tooltip("The relative path to your javascript project where the Unity scene will be exported to. To create a new project just enter a new path, select a template from and click \"Generate project\"")]
		public string DirectoryName = null;
		/// <summary>
		/// A web project can be cloned from a remote repository. If this is set and the local directory doesnt exist this will be used to clone the project
		/// </summary>
		public string RemoteUrl = null;

		[Tooltip("Exporting on save when enabled, otherwise only via menu items or build")]
		public bool AutoExport = true;

		[Tooltip("Apply Compression automatically on export when enabled")]
		public bool AutoCompress = true;

		[SerializeField] public List<Dependency> Dependencies = new List<Dependency>();

		public string BasePath => _basePath ??= Application.dataPath + "/../";
		public string GetProjectDirectory()
		{
			if (DirectoryName.StartsWith("Assets/")) return Path.GetFullPath(DirectoryName);
			if (DirectoryName.StartsWith("Packages/")) return Path.GetFullPath(DirectoryName);
			var subPath = DirectoryName;
			if (string.IsNullOrWhiteSpace(subPath)) subPath = "Library/Needle";
			return BasePath + subPath;
		}
		public bool Exists() => !string.IsNullOrWhiteSpace(DirectoryName) && File.Exists(PackageJsonPath);

		public string PackageJsonPath => DirectoryName + "/package.json";

		public bool IsInstalled()
		{
			if (!Exists()) return false;
			var dir = GetProjectDirectory() + "/node_modules/" + Constants.RuntimeNpmPackageName;
			return !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
		}

		/// <summary>
		/// a project is considered a temp project when it is in Library/
		/// </summary>
		public bool IsTempProject()
		{
			var dir = DirectoryName;
			if (dir == null) return false;
			if (dir.StartsWith("Library/")) return true;
			return false;
		}

		public bool IsValidDirectory() => IsValidDirectory(DirectoryName, out _);

		public static bool IsValidDirectory(string dir, out string reason)
		{
			reason = null;
			if (string.IsNullOrWhiteSpace(dir))
			{
				reason = "Path is empty";
				return false;
			}
			if (dir.StartsWith("Temp/"))
			{
				reason = "Path is in Temp folder";
				return false;
			}
			var isAssetsPath = dir.StartsWith("Assets/");
			var isPackagesPath = dir.StartsWith("Packages/");
			if(isAssetsPath || isPackagesPath)
			{
				// web projects may exist in the Assets or Packages folder if they're hidden (so Unity wont import them)
				// TODO: this doesnt work if a parent folder of the Unity project contains a ~
				var isHidden = dir.EndsWith("~") || dir.Contains("~/") || dir.Contains("~\\");
				if(!isHidden) reason = "Path is in Assets or Packages folder but is not hidden (e.g. a folder in the path must contain '~')\n" +
				                       "Valid examples:\n" +
				                       "• Assets/MyProject~ (inside a hidden folder in Assets)\n" +
				                       "• Needle/MyProject (next to the Unity project)\n" +
				                       "• Library/MyProject (a temporary project)";
				return isHidden;
			}
			try
			{
				if (Path.IsPathRooted(dir))
				{
					reason = "Path is absolute";
					return false;
				}
			}
			catch (ArgumentException)
			{
				return false;
			}

			reason = "Path must not end with a slash or backslash";
			return !(dir.EndsWith("\\") || dir.EndsWith("/"));
		}

		private string _packageJsonPath;
		private static string _basePath;

		private void OnValidate()
		{
			_packageJsonPath = DirectoryName + "/package.json";

			if (DirectoryName == null || DirectoryName.Length <= 0)
			{
				CreateName(SceneManager.GetActiveScene());

				if (transform.childCount <= 0 && GetComponents<Component>().Length <= 2)
				{
					SetNameAndTag();
				}

				async void SetNameAndTag()
				{
					await Task.Yield();
					gameObject.name = "Export Info";
					// ReSharper disable once Unity.InefficientPropertyAccess
					gameObject.tag = "EditorOnly";
				}
			}

			if (DirectoryName != null && (DirectoryName.Contains("github.com") || DirectoryName.Contains("gitlab.com") || DirectoryName.EndsWith(".git")))
			{
				RemoteUrl = DirectoryName;
			}

			// Do not implicitly remove dependencies here, we want to show these in the inspector
			// It may happen if for example an npmdef is copied outside of unity but the hidden package folder is still there
			// for (var index = Dependencies.Count - 1; index >= 0; index--)
			// {
			// 	var dep = Dependencies[index];
			// 	if (string.IsNullOrEmpty(dep.Name))
			// 	{
			// 		Dependencies.RemoveAt(index);
			// 	}
			// }
		}

		internal void RequestInstallationIfTempProjectAndNotExists()
		{
			var dir = DirectoryName;
			if (!string.IsNullOrEmpty(dir) && IsValidDirectory())
			{
				if (!IsInstalled() || !Directory.Exists(dir))
				{
					if (IsTempProject())
					{
						RequestGeneratingTempProject?.Invoke(this);
					}
				}
			}
		}

		internal void CreateName(Scene scene)
		{
			var projectName = scene.name;
			if (string.IsNullOrWhiteSpace(projectName)) projectName = "newProject";
			DirectoryName = "Needle/" + projectName;
		}

#if UNITY_EDITOR_WIN
		[ContextMenu("Open in commandline")]
		private void OpenCmdLine()
		{
			var dir = GetProjectDirectory();
			if (!Exists()) Debug.LogWarning("Project directory does not exist: " + dir);
			else
				ProcessUtils.OpenCommandLine(GetProjectDirectory());
		}

		[ContextMenu("Internal/Create Web Project Config")]
		private void CreateProjectConfig()
		{
			if (NeedleProjectConfig.TryCreate(this, out _, out var path))
			{
				EditorUtility.RevealInFinder(path);
			}
		}
#endif
	}
}