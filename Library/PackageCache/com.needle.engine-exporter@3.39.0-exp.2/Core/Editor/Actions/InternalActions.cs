using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Needle.Engine
{
	internal static class InternalActions
	{
		private static string _localEngineLastDirectorySelected
		{
			get => EditorPrefs.GetString("needle-tools.local-engine-last-directory-selected", null);
			set => EditorPrefs.SetString("needle-tools.local-engine-last-directory-selected", value);
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Internal/Select Local Needle Engine", priority = 10_000)]
		private static void SwitchToLocalEngine()
		{
			var exp = ExportInfo.Get();
			if (!exp) return;
			var selectedPath = EditorUtility.OpenFolderPanel("Select Needle Engine Directory",
				_localEngineLastDirectorySelected, "");
			if (string.IsNullOrEmpty(selectedPath)) return;
			_localEngineLastDirectorySelected = selectedPath;
			if (PackageUtils.GetPackageName(Path.Combine(selectedPath, "package.json")) != "@needle-tools/engine")
			{
				Debug.LogError("The selected directory does not contain a valid @needle-tools/engine package");
				return;
			}
			var packageJsonPath = exp.GetProjectDirectory() + "/package.json";
			if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
			{
				if (deps.TryGetValue("@needle-tools/engine", out _))
				{
					var newValue = "file:" + selectedPath;
					deps["@needle-tools/engine"] = newValue;
					PackageUtils.TryWriteDependencies(packageJsonPath, deps);
					Debug.Log("Switched to @needle-tools/engine@" + newValue);
				}
				else Debug.LogWarning("The project does not contain a dependency to @needle-tools/engine");
			}
			else Debug.LogWarning("Could not read dependencies from project package.json");
		}

		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Internal/Select Registry Needle Engine", priority = 10_000)]
		private static void SwitchToRegistryEngine()
		{
			var exp = ExportInfo.Get();
			if (!exp) return;
			var packageJsonPath = exp.GetProjectDirectory() + "/package.json";
			if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
			{
				if (deps.TryGetValue("@needle-tools/engine", out _) &&
				    NpmUnityEditorVersions.TryGetRecommendedVersion("@needle-tools/engine", out var recommendedVersion))
				{
					deps["@needle-tools/engine"] = recommendedVersion;
					PackageUtils.TryWriteDependencies(packageJsonPath, deps);
					Debug.Log("Switched to @needle-tools/engine@" + recommendedVersion);
				}
				else Debug.LogWarning("The project does not contain a dependency to @needle-tools/engine");
			}
			else Debug.LogWarning("Could not read dependencies from project package.json");
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Profile Export", priority = 10_000)]
		private static async void ProfileBuild()
		{
			ProfilerAccess.SetProfilerEnabled(true);
			ProfilerDriver.profileEditor = !Application.isPlaying;
			await Task.Delay(300);
			var sw = Stopwatch.StartNew();
			var frame = ProfilerAccess.GetCurrentFrame();
			Actions.Play();
			sw.Stop();
			await Task.Delay(2000);
			ProfilerAccess.SetProfilerEnabled(false);
			ProfilerAccess.SetCurrentFrame(frame + 2);
		}


		[MenuItem("CONTEXT/ComponentGenerator/Internal/Open CommandTester Window")]
		private static void DebugComponentCompilerCodegen(MenuCommand command)
		{
			var window = CommandTesterWindow.Create();
			window.command = "node component-compiler.js \"" +
			                 Path.GetFullPath(Application.dataPath + "/Needle/Components.codegen") + "\"" +
			                 " \"path/to/script.ts\"";
			window.directory =
				"Library/Needle/Sample/node_modules/@needle-tools/engine/node_modules/@needle-tools/needle-component-compiler/src";
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Mark Types Dirty", priority = 7_000)]
		private static void MarkTypesDirty()
		{
			TypesUtils.MarkDirty();
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Compile Typescript", priority = 7_000)]
		private static async void CompileTypescript()
		{
			var exportInfo = ExportInfo.Get();
			if (exportInfo)
			{
				Debug.Log("Compile Typescript");
				var res = await ProcessHelper.RunCommand("tsc", Path.GetFullPath(exportInfo.GetProjectDirectory()));
				// Only run tsc in main project - otherwise we get errors because if peerDependencies
				// foreach (var dep in exportInfo.Dependencies)
				// {
				// 	if (dep.TryGetVersionOrPath(out var path))
				// 	{
				// 		Debug.Log("Compile dependency: " + dep.Name + " at " + path);
				// 		res &= await ProcessHelper.RunCommand("tsc", Path.GetFullPath(path));
				// 	}
				// }
				if (res) Debug.Log("Typescript compiled successfully");
				else Debug.LogError("Typescript compilation failed");
			}
		}

		internal static async Task<bool> HasSupportedNodeJsInstalled()
		{
			var version = await GetNodeJsVersion();
			return version.StartsWith("v18") || version.StartsWith("v20");
		}

		private static string _lastGetNodeJsVersionResult;
		private static Task<string> getNodeVersionTask;
		private static DateTime _lastGetNodeVersionTaskStartTime;

		internal static async Task<string> GetNodeJsVersion()
		{
			if (getNodeVersionTask == null || DateTime.Now - _lastGetNodeVersionTaskStartTime > TimeSpan.FromSeconds(5))
			{
				_lastGetNodeVersionTaskStartTime = DateTime.Now;
				getNodeVersionTask = InternalNodeVersionTask();
			}
			_lastGetNodeJsVersionResult = await getNodeVersionTask; 
			return _lastGetNodeJsVersionResult; 

			Task<string> InternalNodeVersionTask()
			{
				foreach (var log in ProcessHelper.RunCommandEnumerable("node --version")) 
				{
					if (log != null && log.StartsWith("v"))
					{
						return Task.FromResult(log);
					}
				}
				return Task.FromResult("");
			}
		}

		internal static Task<string> GetNpmVersion() 
		{
			foreach (var log in ProcessHelper.RunCommandEnumerable("npm --version")) 
			{
				return System.Threading.Tasks.Task.FromResult(log);
			}
			return System.Threading.Tasks.Task.FromResult("");
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Has npm installed", priority = 7_000)]
		private static async void HasNpmInstalled()
		{
			if (!await Actions.HasNpmInstalled(true))
			{
				Debug.LogError(
					$"→ <b>Nodejs is not installed or could not be found</b> — please {"install nodejs".AsLink("https://nodejs.org")}\nRead more about using nodejs in Needle Engine: {Constants.DocumentationUrlNodejs}\n{string.Join("\n", ExporterProjectSettings.instance.npmSearchPathDirectories)}");
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
				Debug.Log("Please run `which npm` from your terminal and add the path in the settings to the additional search paths list.");
				// foreach(var line in ProcessHelper.RunCommandEnumerable("`which npm`"))
				// 	Debug.Log(line);
#endif
			}
			else Debug.Log("<b>Npm is installed.</b>".AsSuccess());
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Has vscode installed", priority = 7_000)]
		private static void HasVSCodeInstalled()
		{
			if (!Actions.HasVsCodeInstalled())
				Debug.LogError("VSCode is not installed or could not be found. Please install VSCode.");
			else Debug.Log("<b>VSCode is installed.</b>".AsSuccess());
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Download VsCode", priority = 7_000)]
		private static async void DownloadVsCode()
		{
			await VsCodeHelper.DownloadAndInstallVSCode();
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Log npm path", priority = 7_000)]
		private static void LogNpmPath()
		{
			NpmUtils.LogPaths();
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Has Toktx installed", priority = 7_000)]
		private static async void HasToktxInstalled()
		{
			if (!await Actions.HasToktxInstalled())
			{
				Debug.LogError("Could not find toktx installation");
			}
			else
			{
				Debug.Log("<b>toktx is installed.</b>".AsSuccess());
				if (!Actions.HasMinimumToktxVersionInstalled(out var detectedVersion))
				{
					Debug.LogWarning(
						$"Your toktx version is out of date ({detectedVersion}). Please update to 4.3+ on " +
						"https://github.com/KhronosGroup/KTX-Software/releases".AsLink());
				}
			}
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/List node processes", priority = 7_000)]
		private static void ListNodeProcesses()
		{
			if (ProcessUtils.TryFindNodeProcesses(out var list))
			{
				Debug.Log("Found " + list.Count + " processes:");
				foreach (var proc in list)
				{
					Debug.Log(proc.Process.ProcessName + ", id=" + proc.Process.Id + ", command=\"" + proc.CommandLine +
					          "\"");
				}
			}
			else Debug.Log("Did not find any node processes");
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Can build for production", priority = 7_000)]
		private static async void CanBuildForProduction()
		{
			var hasError = !await Actions.HasToktxInstalled();
			if (!hasError)
			{
				if (!Actions.HasMinimumToktxVersionInstalled(out var detectedVersion))
				{
					Debug.LogError($"Your toktx version is out of date ({detectedVersion}). Please update to 4.1+ on " +
					               "https://github.com/KhronosGroup/KTX-Software/releases".AsLink());
				}
				else
				{
					LogHelpers.LogWithoutStacktrace(
						$"<b>{"Congrats".AsSuccess()}</b>, your machine ready to build for production and has all required dependencies installed!");
				}
			}
			else
				LogHelpers.LogWithoutStacktrace(
					"Your machine is not setup to build for production. Please see error in console!");
		}

		internal static void LogFeedbackFormUrl()
		{
			var isDarkSkin = EditorGUIUtility.isProSkin;
			var highlightColor = isDarkSkin ? "#a4d501" : "#6d0198";
			Debug.Log(
				$"<b><color={highlightColor}>Send us feedback</color> at {Constants.FeedbackFormUrl.AsLink()}</b> — Thank you!");
		}

		[MenuItem(Constants.MenuItemRoot + "/Internal/Delete vite caches", priority = 7_000)]
		internal static void DeleteViteCaches()
		{
			if (Actions.HasStartedLocalServer())
			{
				Debug.LogWarning(
					"Currently a local server is running - please stop the server before clearing vite caches");
				return;
			}
			var export = ExportInfo.Get(true);
			if (export)
			{
				ViteActions.DeleteCache(Path.GetFullPath(export.GetProjectDirectory()));
			}
			else Debug.LogWarning("No ExportInfo found");
		}

		internal static void DeletePackageJsonLock()
		{
			var export = ExportInfo.Get(true);
			if (export && export.Exists())
			{
				var lockPath = Path.Combine(export.GetProjectDirectory(), "package-lock.json");
				if (File.Exists(lockPath))
				{
					File.Delete(lockPath);
				}
			}
		}
	}
}