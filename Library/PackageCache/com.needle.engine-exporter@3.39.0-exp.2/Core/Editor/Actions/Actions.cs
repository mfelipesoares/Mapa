using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Problems;
using Needle.Engine.Projects;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if !UNITY_EDITOR_OSX
using Semver;
#endif

namespace Needle.Engine
{
	public static class Actions
	{
		internal static void SetupSceneForNeedleEngineExport()
		{
			EditorApplication.ExecuteMenuItem(Constants.SetupSceneMenuItem);
		}
		
		public static bool IsRunningAnyAction =>
			Actions.IsInstalling() || Actions.IsRunningBuildTask || Actions.IsStartingServerTask;

		internal static Task<bool> buildTask;
		public static bool IsRunningBuildTask => buildTask != null && !buildTask.IsCompleted;
		
		public static bool IsStartingServerTask => MenuItems._startingServerTask != null && !MenuItems._startingServerTask.IsCompleted;
		
		public static event Action<string> InstallationStarting;
		/// <summary>
		/// Raised when installation finishes in directory (bool is if it was successful)
		/// </summary>
		public static event Action<string, bool> InstallationFinished;

		public static bool OpenChangelog(bool local = true)
		{
			if (!local)
			{
				Application.OpenURL("https://github.com/needle-tools/needle-engine-support/releases");
				return true;
			}
			var path = Path.GetFullPath(Constants.ExporterPackagePath) + "/CHANGELOG.md";
			if (File.Exists(path))
			{
				EditorUtility.OpenWithDefaultApp(path);
				return true;
			}
			return false;
		}

		public static void OpenReleaseOnGithub(string version)
		{
			Application.OpenURL($"https://github.com/needle-tools/needle-engine-support/releases/tag/release%2F{version}");
		}

		public static void Play()
		{
			EnterPlayMode.Play();
		}

		public static void OpenNeedleExporterProjectSettings()
		{
			SettingsService.OpenProjectSettings("Project/Needle");
		}
        
		/// <summary>
		/// For full re-export, making sure all types are collected again, smart export asset hashes are cleared and the cache directory is deleted
		/// </summary>
		public static void ClearCaches(ExportInfo exp = null)
		{
			TypesUtils.MarkDirty();
			AssetDependency.ClearCaches(); 
			
			if (!exp)
			{
				exp = ExportInfo.Get();
				if (!exp) return;
			}

			var fullPath = Path.GetFullPath(exp.GetProjectDirectory());
			ViteActions.DeleteCache(fullPath);
			NextActions.DeleteCache(fullPath);
			
			var cacheDir = ProjectInfoExtensions.GetCacheDirectory();
			if (Directory.Exists(cacheDir))
			{
				Debug.Log("Delete caches: " + cacheDir);
				Directory.Delete(cacheDir, true);
			}
			
			HiddenProject.Delete();
		}

		public static Task<bool> ExportAndBuild(bool dev)
		{
			return MenuItems.BuildForDistAsync(dev ? BuildContext.Development : BuildContext.Production );
		}

		public static Task<bool> ExportAndBuild(BuildContext context)
		{
			return MenuItems.BuildForDistAsync(context);
		}

		public static Task<bool> ExportAndBuildDevelopment()
		{
			return MenuItems.BuildForDistAsync(BuildContext.Development);
		}

		public static Task<bool> ExportAndBuildProduction()
		{
			return MenuItems.BuildForDistAsync(BuildContext.Production);
		}

		public static Task<bool> BuildDist(BuildContext context)
		{
			return ActionsBuild.InternalBuildDistTask(context);
		}

		public static Task<bool> BuildDevelopmentDist()
		{
			return ActionsBuild.InternalBuildDistTask(BuildContext.Development);
		}

		public static Task<bool> BuildProductionDist()
		{
			return ActionsBuild.InternalBuildDistTask(BuildContext.Production);
		}

		public static void StartLocalServer() => MenuItems.StartDevelopmentServer();

		public static bool HasStartedLocalServer() => ProgressHelper.GetStartedAndRunningProcesses(p => p.IsThisProject()).Any();

		public static void StopLocalServer(bool force = false)
		{
			if (force)
			{
				var currentProject = ExportInfo.Get();
				if (currentProject && currentProject.IsValidDirectory())
				{
					var fullPath = Path.GetFullPath(currentProject.GetProjectDirectory());
					ProcessUtils.KillNodeProcesses(cmd =>
					{
						if (cmd.Contains(fullPath))
							return true;
						return false;
					});
					return;
				}
			}

			var killedAny = false;
			foreach (var proc in ProgressHelper.GetStartedAndRunningProcesses(p => p.IsThisProject()))
			{
				killedAny = true;
				proc.Kill();
			}
			if (killedAny) ProgressHelper.UpdateStartedProcessesList();
		}

		public static async void TestValidInstallation(bool logToConsole = false)
		{
			// if the server is not running it might be because npm is not installed
			// run npm just to print out possibly errors
			if (!await ProcessHelper.RunCommand("npm --version", null, null, true, logToConsole))
			{
				Debug.LogError(
					$"→ <b>Nodejs is not installed or could not be found</b> — please {"install nodejs".AsLink("https://nodejs.org")}\nRead more about using nodejs in Needle Engine: {Constants.DocumentationUrlNodejs}\n{string.Join("\n", ExporterProjectSettings.instance.npmSearchPathDirectories)}");
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
				Debug.Log("Please run `which npm` from your terminal and add the path in the settings to the additional search paths list.");
#endif
			}
		}
		
		public static async Task<bool> HasToktxInstalled(bool printError = true)
		{
#if UNITY_EDITOR_WIN
			// the gltf build pipeline checks this path and adds it to the paths
			if (File.Exists(ToolsHelper.GetToktxDefaultInstallationLocation() + "/toktx.exe"))
				return true;
#endif
			var cmd = "toktx --version";
			ToolsHelper.SetToktxCommandPathVariable(ref cmd);
			var cmdResult = ProcessHelper.RunCommandEnumerable(cmd);
			var isError = cmdResult.Any(l => l.Contains("is not recognized", StringComparison.InvariantCultureIgnoreCase) || l.Contains("command not found", StringComparison.InvariantCultureIgnoreCase));
			if (isError)
			{
				if (printError)
				{
					var msg =
						$"→ <b>toktx is not installed</b> — but it is required for production builds. Visit {"https://github.com/KhronosGroup/KTX-Software/releases".AsLink()} and download and install the latest version.";
#if UNITY_EDITOR_WIN
					msg += "\nMake sure to enable add to PATH in the installer!";
#endif
					msg += "\n" + cmd;
					Debug.LogError(msg);
				}
				return false;
			}

			await Task.CompletedTask;
			return true;
		}

		public static bool HasMinimumToktxVersionInstalled(out string version)
		{
			var cmd = "toktx --version";
			ToolsHelper.SetToktxCommandPathVariable(ref cmd);
			// try to run the command in the toktx directory if it exists
			var dir = ToolsHelper.GetToktxDefaultInstallationLocation();
			if (dir != null && !Directory.Exists(dir)) dir = null;
			foreach (var line in ProcessHelper.RunCommandEnumerable(cmd, dir))
			{
				if (line == null) continue;
				if (line.StartsWith("toktx v4.0") || line.StartsWith("toktx v4.1") || line.StartsWith("toktx v4.2"))
				{
					version = line;
					return false;
				}
				if (line.StartsWith("toktx v"))
				{
					version = line;
					return true;
				}
			}

			version = null;
			return false;
		}

		private static Task<bool> npmInstallCheckTask;
		public static async Task<bool> HasNpmInstalled(bool logToConsole = false)
		{
			// Make sure we dont spawn many npm check tasks at once
			if (npmInstallCheckTask is { IsCompleted: false })
			{
				return await npmInstallCheckTask;
			}
			var success = false;
			npmInstallCheckTask = ProcessHelper.RunCommand("npm --version", null, null, true, logToConsole, -1, default, 
				(_, str) =>
				{
					if (string.IsNullOrEmpty(str)) return;
					var firstCharIsNumber = char.IsNumber(str[0]);
				if (firstCharIsNumber) success = true;
			});
			await npmInstallCheckTask.ContinueWith(t => success, TaskScheduler.FromCurrentSynchronizationContext());
			return success;
		}

		public static bool HasVsCodeInstalled()
		{
			#if UNITY_EDITOR_OSX
			return File.Exists("/Applications/Visual Studio Code.app/Contents/MacOS/Electron");
			#else
			foreach (var line in ProcessHelper.RunCommandEnumerable("code --version"))
			{
				if (SemVersion.TryParse(line, SemVersionStyles.Any, out _)) return true;
			}
			return false;
			#endif
		}

		/// <summary>
		/// Requires global typescript installation
		/// </summary>
		internal static async Task<bool> TryCompileTypescript(string projectDirectory)
		{
			var tempPath = Application.dataPath + "/../Temp/tsc.log";
			var cmd = "tsc --noEmit --skipLibCheck";
			// see https://github.com/needle-tools/needle-engine-support/issues/119
			// some errors seem to be only be only throws in strict false unfortunately
			if (await ProcessHelper.RunCommand(cmd + " --strict true", projectDirectory, tempPath))
			{
				if (await ProcessHelper.RunCommand(cmd + " --strict false", projectDirectory, tempPath))
				{
					return true;
				}
			}

			// Note: this requires global typescript installation
			// return ProcessHelper.RunCommand("tsc -noEmit", projectDirectory);
			return false;
		}

		public static bool IsInExportScene() => ExportInfo.Get();

		/// <summary>
		/// First Argument is the package json path
		/// </summary>
		public static event Action<string> BeforeInstallCurrentProject;

		public static async Task<bool> InstallCurrentProject(bool showWindow = false, bool userRequestedInstallation = false)
		{
			if (SceneExportUtils.IsValidExportScene(out var path, out _))
			{
				return await InstallProject(path, showWindow, userRequestedInstallation);
			}
			
			// don't log if we don't know for what
			if (!string.IsNullOrEmpty(path))
				Debug.LogWarning("Can not install - no valid project path found. Does the project exist? " + path.AsLink());
		
			return false;
		}

		internal static async Task<bool> InstallProject(string projectPath, bool showWindow = false, bool userRequestedInstallation = false)
		{
			EnsureDependenciesAreAddedToPackageJson();
			var packageJsonPath = projectPath + "/package.json";
			BeforeInstallCurrentProject?.Invoke(packageJsonPath);
			
			if (ProjectValidator.FindProblems(packageJsonPath, out var problems))
			{
				if (!await ProblemSolver.TryFixProblems(projectPath, problems))
				{
					Debug.LogError("Can not build because package.json has problems. Please fix errors listed below first:",
						ExportInfo.Get());
					foreach (var p in problems)
					{
						Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "{0}: {1}", p.Id, p.Message);
					}
					return false;
				}
			}
			ViteActions.DeleteCache(projectPath);
			
			if (File.Exists(packageJsonPath)) await ProjectGenerator.UpdateVersions(packageJsonPath);
			
			if (userRequestedInstallation)
			{
				if(NpmUnityEditorVersions.TryGetVersions(out var obj, NpmUnityEditorVersions.Registry.Npm))
				{
					var keys = new List<string>();
					foreach (var key in obj)
					{
						keys.Add(key.Key);
					}
					await NpmUtils.UpdatePackages(keys, projectPath);
				}
			}
			
			await ActionsHelperPackage.UpdateThreeTypes(projectPath);

			// Make sure all locally referenced packages are installed first
			// e.g. packages that are referenced via file:../my-package
			// this CAN be totally unknown to Unity so we iterate the actual paths here
			// See: https://discord.com/channels/717429793926283276/1091740937492910090/1091776181239554208
			var successfullyInstalledDependencies = await InstallDependenciesRecursive(userRequestedInstallation, projectPath);
			
			if (!successfullyInstalledDependencies)
			{
				Debug.LogError("Some dependencies could not be installed. Please check the console for more information.");
			}

			var res = await RunNpmInstallAtPath(projectPath, showWindow);
			if (res) TypesUtils.MarkDirty();
			return res;
		}

		/// <param name="userRequestedInstallation">true if this was triggered by a click on e.g. a Install button</param>
		private static async Task<bool> InstallDependenciesRecursive(bool userRequestedInstallation, string directory, List<string> alreadyDiscoveredPaths = null, bool allowVersionsUpdate = true)
		{				
			var failedInstalling = false;
			var packageJsonPath = directory + "/package.json";
			if (PackageUtils.TryReadDependencies(packageJsonPath, out var dependencies))
			{
				foreach (var dep in dependencies)
				{
					if (PackageUtils.TryGetPath(directory, dep.Value, out var fullPath))
					{
						// Make sure we only install each package once
						var normalizedPath = fullPath.Replace("\\", "/");
						if (alreadyDiscoveredPaths == null) alreadyDiscoveredPaths = new List<string>();
						if (!alreadyDiscoveredPaths.Contains(normalizedPath))
						{
							alreadyDiscoveredPaths.Add(normalizedPath);
							if (!Directory.Exists(fullPath))
							{
								Debug.LogWarning("Found missing local path: " + dep.Key + " at " + fullPath + " in your package at " + directory);
								continue;
							}
							

							var npmDefPackageJsonPath = normalizedPath + "/package.json";
							var versionHasChanged = false;
							if (File.Exists(npmDefPackageJsonPath))
							{
								if (dep.Key == Constants.RuntimeNpmPackageName)
								{
									allowVersionsUpdate = false;
									// we don't want to automatically update local needle engine package dependencies.
									Debug.Log("Not updating dependencies in local needle engine package".LowContrast());
								}
								
								if(allowVersionsUpdate)
								{
									versionHasChanged = await ProjectGenerator.UpdateVersions(npmDefPackageJsonPath);
								}
							}

							// Only run install again in the local package if it appears to be not installed 
							var needsInstalling = Directory.Exists(fullPath + "/node_modules") == false;
							if (userRequestedInstallation || needsInstalling || versionHasChanged)
							{
								var didInstall = await RunNpmInstallAtPath(fullPath, false);
								if (!didInstall)
								{
									failedInstalling = true;
									Debug.LogError("Failed installing dependency: " + dep.Key + " at " + fullPath +
									               " in your package at " + directory);
									continue;
								}
							}
							
							// check dependencies of locally referenced package too
							await InstallDependenciesRecursive(userRequestedInstallation, fullPath, alreadyDiscoveredPaths, allowVersionsUpdate);
						}
					}
					else
					{
						var expectedDirectory = directory + "/node_modules/" + dep.Key;
						if (Directory.Exists(expectedDirectory))
						{
							// If a package directory exists but does not contain a package it's likely broken
							// e.g. when switching between local versions and npm versions this may happen
							// In which case we both want to delete the directory as well as the package-lock.json
							// To enforce a clean re-install
							var packageJson = expectedDirectory + "/package.json";
							if (!File.Exists(packageJson))
							{
								await FileUtils.DeleteDirectoryRecursive(expectedDirectory);
								// Make sure to delete the lock file of the directory that we currently are in
								var packageJsonLock = directory + "/package-lock.json";
								if (File.Exists(packageJsonLock)) File.Delete(packageJsonLock);
							}
						}
					}
				}
			}
			return !failedInstalling;
		}

		public static void EnsureDependenciesAreAddedToPackageJson(ExportInfo exportInfo = null)
		{
			if (!exportInfo) exportInfo = ExportInfo.Get();
			if (!exportInfo)
			{
				Debug.LogWarning("Can't install dependencies, no ExportInfo found.");
				return;
			}
			
			var packageJsonPath = Path.GetFullPath(exportInfo.PackageJsonPath);
			foreach (var dep in exportInfo.Dependencies)
			{
				dep.Install(packageJsonPath);
			}
		}

		public static async Task<bool> InstallPackage(bool clean, bool showWindow = false, bool silent = false, bool userRequestedInstall = false)
		{
			try
			{
				if (clean)
				{
					if (!silent && !EditorUtility.DisplayDialog("Clean installation",
						    "You are about to run clean install - this will delete node_module folders and package-lock files as well as shut down running node processes. Do you want to continue?",
						    "Yes, perform clean install", "No cancel"))
					{
						Debug.LogWarning("Clean install cancelled");
						return false;
					}

					var running = ProgressHelper.GetStartedAndRunningProcesses().ToArray();
					foreach (var proc in running) proc.Kill();
					ProcessUtils.KillNodeProcesses(cmd =>
					{
						if(cmd.Contains("vite") || cmd.EndsWith("start") || cmd.EndsWith("serve") || cmd.EndsWith("dev"))
							return true;
						return false;
					});

					// delete of current project
					var exp = ExportInfo.Get();
					var dir = exp.GetProjectDirectory();
					await RecursiveCleanInstall(dir, new List<string>());
					
					ClearCaches(exp);

					Debug.Log("Run npm update - this might take a moment...");
					var preInstallUpdate = ProcessHelper.RunCommand("npm set registry https://registry.npmjs.org && npm update " + NpmUtils.NpmNoProgressAuditFundArgs, dir);
					var key = "npm-update:" + dir;
					installationTasks.Add(key, (preInstallUpdate, DateTime.Now));
					await preInstallUpdate;
					installationTasks.Remove(key);
				}

				var success = await InstallCurrentProject(showWindow, userRequestedInstall);

				// this is necessary to reload types after modules installation
				TypesUtils.MarkDirty();

				if (success)
					Debug.Log("<b>Install finished</b>");
				else
				{
					Debug.LogWarning("<b>Installation did not succeed</b> - please see logs for errors or problems");
				}
				return success;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			return false;
		}

		private static async Task<bool> RecursiveCleanInstall(string dir, ICollection<string> alreadyDiscoveredPaths)
		{
			if (!Directory.Exists(dir)) return false;
			if (alreadyDiscoveredPaths.Contains(dir)) return false;
			alreadyDiscoveredPaths.Add(dir);
			var packagePath = dir + "/package.json";
			if (PackageUtils.TryReadDependencies(packagePath, out var deps))
			{
				foreach (var dep in deps.Values)
				{
					if (PackageUtils.TryGetPath(dir, dep, out var fp))
					{
						await RecursiveCleanInstall(fp, alreadyDiscoveredPaths);
					}
				}
			}
			await DeleteDirectory(dir, "node_modules");
			var lockFile = dir + "/package-lock.json";
			if (File.Exists(lockFile)) File.Delete(lockFile);
			return true;
		}

		private static Task<bool> DeleteDirectory(string dir, string name)
		{
#if UNITY_EDITOR_WIN
			// /Q is quiet mode, /s is subdirectories/files
			return ProcessHelper.RunCommand("rmdir /s /Q " + name, dir);
#else
			return ProcessHelper.RunCommand("rm -rf " + name, dir);
#endif
		}

		public static bool IsInstalling()
		{
			return installationTasks.Count > 0 && installationTasks.Values.Any(t => !t.task.IsCompleted);
		}

		public static async Task<bool> WaitForInstallationToFinish()
		{
			var t = installationTasks.Values.FirstOrDefault(t => !t.task.IsCompleted);
			if (t.task != null)
			{
				var res = await t.task;
				return res;
			}
			return true;
		}

		private static readonly Dictionary<string, (Task<bool> task, DateTime startTime)> installationTasks =
			new Dictionary<string, (Task<bool> task, DateTime startTime)>();

		internal static Task<bool> RunNpmInstallAtPath(string path, bool showWindow)
		{
			var installCommand = NpmUtils.GetInstallCommand(path);
			if (installCommand.Contains("npm"))
			{
				HasNpmInstalled(false).ContinueWith(res =>
				{
					if(res.Result) return BeginInstall(installCommand, path, showWindow);
					Debug.LogError($"npm is not installed - please install Node LTS first. Visit {"https://nodejs.org".AsLink()} for more information. If you did install Nodejs and this message still appears please restart your computer.");
					return res;
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
			return BeginInstall(installCommand, path, showWindow);
		}

		private static Task<bool> BeginInstall(string installCommand, string path, bool showWindow)
		{
			path = Path.GetFullPath(path);
			if (installationTasks.TryGetValue(path, out var t))
			{
				if (t.task.IsCompleted) installationTasks.Remove(path);
				else return t.task;
			}
			
			var logPrefix = "Installing";
			var pathDisplayName = PathUtils.GetShortDisplayPath(path);
			SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Installing web project " + pathDisplayName), 5);
			Debug.Log($"<b>{logPrefix}</b> <a href=\"{path}\">{pathDisplayName}</a>\nCMD=\"{installCommand}\"\nat: " + path);
			if (!DriveHelper.HasEnoughAvailableDiscSpace(path, 200))
			{
				Debug.LogWarning(
					"<b>It looks like you dont have enough disc space available!</b> Make sure you have at least 200 mb or more, otherwise installation might not be able to finish!");
				return Task.FromResult(false);
			}
			
			var packageLock = path + "/package-lock.json";
			if(File.Exists(packageLock)) File.Delete(packageLock);
			
			var noWindow = !showWindow;
			var cmd = NpmCommands.SetDefaultNpmRegistry;
			cmd += " && " + installCommand + " --force";
			if (showWindow) cmd += " && timeout 5";
			// for some reason std output does sometimes only output stuff when the process has already ended
			var logFilePath = Path.GetFullPath(Application.dataPath + "/../Temp/needle_npm_install.log");
			var info = new TaskProcessInfo(path, cmd);
			InstallationStarting?.Invoke(path);
			var task = ProcessHelper.RunCommand(cmd, path, logFilePath, noWindow, true);
			task.ContinueWith(inst => InstallationFinished?.Invoke(path, inst.Result), TaskScheduler.FromCurrentSynchronizationContext());
			installationTasks.Add(path, (task, DateTime.Now));
			WatchInstallationTask(path, task, info);
			return task;
		}

		private static async void WatchInstallationTask(string directory, IAsyncResult task, TaskProcessInfo info)
		{
			var startTime = DateTime.Now;
			var unexpectedLongTime = TimeSpan.FromMinutes(5);
			var packageJsonLockPath = directory + "/package-lock.json";
			while (task != null && !task.IsCompleted)
			{
				if (DateTime.Now - startTime > unexpectedLongTime)
				{
					if (File.Exists(packageJsonLockPath))
					{
						if (EditorUtility.DisplayDialog("Installation is taking longer than expected",
							    $"Installation in {directory} takes longer than expected. You might need to delete the package-lock.json and retry.",
							    "Delete package-lock.json now", "Continue waiting"))
						{
							File.Delete(packageJsonLockPath);
							ProcessHelper.CancelTask(info);
							Debug.Log("Deleted package-lock.json. Please try restarting installation.", ExportInfo.Get());
						}
						break;
					}
				}
				await Task.Delay(20_000);
			}
		}


		private static Task<bool> _projectSetupTask;
		internal static bool ProjectSetupIsRunning => _projectSetupTask != null && !_projectSetupTask.IsCompleted;

		internal static void RunProjectSetupIfNecessary()
		{
			RunProjectSetup(false);
		}

		internal static Task<bool> RunProjectSetup(bool runInstall = false)
		{
			if (ProjectSetupIsRunning) return _projectSetupTask;
			_projectSetupTask = OnRunProjectSetup(runInstall);
			return _projectSetupTask;
		}

		private static async Task<bool> OnRunProjectSetup(bool runInstall)
		{
			if (!runInstall) return true;
			
			if (await InstallCurrentProject(false))
			{
				return true;
			}

			var exportInfo = ExportInfo.Get();
			if (exportInfo)
			{
				if (exportInfo.Exists())
					Debug.LogWarning("Failed to install current project, please see the console for errors", exportInfo);
				else 
					Debug.Log("<b>You are ready to create a web project</b>. Select the " + nameof(ExportInfo) + " component to get started!", exportInfo);
			}
			return false;
		}

	}
}