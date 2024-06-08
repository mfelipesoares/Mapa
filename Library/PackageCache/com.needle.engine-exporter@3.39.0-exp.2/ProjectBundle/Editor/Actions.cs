using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.ProjectBundle
{
	internal static class Actions
	{
		/// <summary>
		/// This command ensures that versions match between the web project's package.json and
		/// the dependency package's package.json. Local references to the engine and three.js are
		/// kept in sync this way.
		/// </summary>
		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Update/Ensure versions match between web project and dependencies")]
		private static void EnsurePackageVersionsMatch(MenuCommand cmd)
		{
			var exportInfo = cmd.context as ExportInfo;
			if (!exportInfo) return;
			var packageJsonPath = exportInfo.PackageJsonPath;
			if (!PackageUtils.TryReadDependencies(packageJsonPath, out var dependencies)) return;
			
			foreach (var dep in dependencies)
			{
				if (PackageUtils.TryGetPath(exportInfo.GetProjectDirectory(), dep.Value, out var fullPath))
					SetDevDependenciesToRegistry(fullPath);
			}
		}
		
		internal static void SetDevDependenciesToRegistry(string directory)
		{
			Dictionary<string, string> currentProjectDependencies = default;
			var currentProject = ExportInfo.Get();
			if (currentProject)
			{
				PackageUtils.TryReadDependencies(currentProject.PackageJsonPath, out currentProjectDependencies);
			}

			var path = directory + "/package.json";
			var changed = false;
			UpdateDependencies("devDependencies");
			UpdateDependencies("peerDependencies");

			void UpdateDependencies(string key)
			{
				if (PackageUtils.TryReadDependencies(path, out var deps, key) &&
				    NpmUnityEditorVersions.TryGetVersions(out var recommendedVersions, NpmUnityEditorVersions.Registry.Npm))
				{
					const string engine = "@needle-tools/engine";
					if (deps.TryGetValue(engine, out var currentEngineVersion) &&
					    recommendedVersions.TryGetValue(engine, out var recommended))
					{
						var recommendedVersion = recommended.ToString();
						if (currentEngineVersion != recommendedVersion &&
						    !PackageUtils.IsAliasVersion(currentEngineVersion))
						{
							changed = true;
							deps["@needle-tools/engine"] = recommendedVersion;
						}

						// Test if the current version has a different local path (for dev) installed)
						if (currentProjectDependencies != null &&
						    currentProjectDependencies.TryGetValue(engine, out var currentProjectVersion))
						{
							if (PackageUtils.TryGetPath(currentProject?.DirectoryName, currentProjectVersion,
								    out var currentLocalEngineInstalledInProject))
							{
								currentLocalEngineInstalledInProject =
									Path.GetFullPath(currentLocalEngineInstalledInProject);
								if (Directory.Exists(currentLocalEngineInstalledInProject))
								{
									changed = true;
									deps[engine] = "file:" + currentLocalEngineInstalledInProject;
								}
							}
						}
					}
					if (deps.TryGetValue("three", out var currentThreeVersion) &&
					    recommendedVersions.TryGetValue("three", out recommended))
					{
						var threeVersion = recommended.ToString();
						if (currentThreeVersion != threeVersion)
						{
							var isNeedleThreeVersion = currentThreeVersion.StartsWith("npm:@needle-tools/three@");
							var isPath = PackageUtils.IsPath(currentEngineVersion);
							if (isPath || isNeedleThreeVersion)
							{
								changed = true;
								deps["three"] = threeVersion;
							}
						}
					}

					if (changed)
					{
						PackageUtils.TryWriteDependencies(path, deps, key);

						// delete package.lock
						var lockPath = directory + "/package-lock.json";
						if (File.Exists(lockPath))
							File.Delete(lockPath);
					}
				}
			}
		}

		internal static void AddToWorkspace(string projectDirectory, string packageName)
		{
			if (string.IsNullOrWhiteSpace(projectDirectory)) return;
			projectDirectory = Path.GetFullPath(projectDirectory);

			if (WorkspaceUtils.TryReadWorkspace(projectDirectory, out var workspace))
			{
				if (WorkspaceUtils.AddToFolders(workspace, packageName))
				{
					WorkspaceUtils.WriteWorkspace(workspace, projectDirectory);
				}
			}
		}

		internal static void RemoveFromWorkspace(string projectDirectory, string packageName)
		{
			if (string.IsNullOrWhiteSpace(projectDirectory)) return;
			projectDirectory = Path.GetFullPath(projectDirectory);

			if (WorkspaceUtils.TryReadWorkspace(projectDirectory, out var workspace))
			{
				if (WorkspaceUtils.RemoveFromFolders(workspace, packageName))
					WorkspaceUtils.WriteWorkspace(workspace, projectDirectory);
			}
		}

		internal static async void InstallBundle(Bundle bundle)
		{
			await InstallBundleTask(bundle);
		}

		internal static Task<bool> InstallBundleTask(Bundle bundle)
		{
			var projectDir = Path.GetFullPath(bundle.PackageDirectory);
			return InstallBundleTask(projectDir);
		}

		internal static Task<bool> InstallBundleTask(string dir)
		{
			var dirInfo = new DirectoryInfo(dir);
			if (!dirInfo.Exists)
				return Task.FromResult(false);
			var packageJson = dir + "/package.json";
			if (!File.Exists(packageJson))
			{
				Debug.LogWarning("<b>Install NpmDef</b> - no package.json found in " + dir);
				return Task.FromResult(false);
			}
			Debug.Log($"<b>Install NpmDef {dirInfo.Name}</b>");
			SetDevDependenciesToRegistry(dir);
			return ProcessHelper.RunCommand($"{NpmCommands.SetDefaultNpmRegistry} && {NpmUtils.GetInstallCommand(dir)}", dir);
		}

		internal static bool OpenWorkspace(string path, string fileToOpen = null)
		{
			string dir = path;
			if (dir.EndsWith(".npmdef"))
			{
				dir = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "~";

				// try to find the registered bundle for it (which is external) and matches the path
				var npmdefPath = new FileInfo(path);
				foreach (var bundle in BundleRegistry.Instance.Bundles)
				{
					if (bundle.IsLocal)
					{
						var otherNpmdefPath = new FileInfo(bundle.FilePath);
						if (otherNpmdefPath.Exists && otherNpmdefPath.FullName == npmdefPath.FullName)
						{
							dir = bundle.PackageDirectory;
							break;
						}
					}
				}
			}

			if (File.Exists(dir)) dir = Path.GetDirectoryName(dir);

			if (string.IsNullOrEmpty(dir)) return false;

			dir = Path.GetFullPath(dir);

			if (Directory.Exists(dir))
			{
				InstallBundleTask(dir);

				if (!ExporterUserSettings.instance.UseVSCode)
				{
					WorkspaceUtils.OpenWorkspace(dir, true, true, fileToOpen);
					return true;
				}

				WorkspaceUtils.OpenWorkspace(dir, true, false, fileToOpen);
				return true;
			}
			return false;
		}

		private static Task<bool> DeleteDirectory(string dir, string name)
		{
			if (!Directory.Exists(dir))
			{
				Debug.LogError("Directory does not exist: " + dir);
				return Task.FromResult(false);
			}
#if UNITY_EDITOR_WIN
			// /Q is quiet mode, /s is subdirectories/files
			return ProcessHelper.RunCommand("rmdir /s /Q \"" + name + "\"", Path.GetFullPath(dir));
#else
			return ProcessHelper.RunCommand("rm -rf " + name, dir);
#endif
		}

		internal static async void DeleteRecursive(string targetDir)
		{
			var dirInfo = new DirectoryInfo(targetDir);
			await DeleteDirectory(dirInfo.Parent?.FullName, dirInfo.Name);
		}
	}
}