using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using Unity.SharpZipLib.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Needle.Engine.Problems
{
	internal static class ProjectInfoReporter
	{
		private class CollectionSettings
		{
			public bool IncludeNodeModulesFolderVersions = false;
			public bool IncludeUnityProjectAssets = false;
			public bool IncludeWebProjectAssets = false;
			public bool Upload = false;
		}

		private const string CreateBugReportMenuItem = "Create Bug Report (from current scene)";
		private const string CollectLogsMenuItem = "Collect Logs Only";
		private const string CopyProjectMenuItem = "Copy Project Info to Clipboard";
		internal const int HelpItemPriority = Constants.MenuItemOrder + 1000;

		[MenuItem(Constants.MenuItemRoot + "/Report a Bug/Show Previous Reports", priority = Constants.MenuItemOrder, validate = true)]
		private static bool OpenBugReportDirectory_Validate() => Directory.Exists(GetBugReportDirectory());

		[MenuItem(Constants.MenuItemRoot + "/Report a Bug/Show Previous Reports", priority = Constants.MenuItemOrder + 100)]
		private static void OpenBugReportDirectory()
		{
			var dir = GetBugReportDirectory();
			if (Directory.Exists(dir))
				EditorUtility.RevealInFinder(dir);
			else Debug.Log("Bug Report Directory does not exist: " + dir);
		}


		// [MenuItem("CONTEXT/" + nameof(ExportInfo) + "/" + ProjectBundleEntry)]
		[MenuItem(Constants.MenuItemRoot + "/Report a Bug/" + CreateBugReportMenuItem, priority = Constants.MenuItemOrder - 100)]
		[MenuItem("Help/Needle Engine/" + CreateBugReportMenuItem, priority = HelpItemPriority)]
		internal static void ReportABugFromCurrentScene()
		{
			InternalCollectDebugFiles("Needle Bug Report", new CollectionSettings()
			{
				IncludeNodeModulesFolderVersions = true,
				IncludeUnityProjectAssets = true,
				IncludeWebProjectAssets = true,
				Upload = true
			});
		}

		// [MenuItem("CONTEXT/" + nameof(ExportInfo) + "/" + LogCollectionEntry)]
		[MenuItem(Constants.MenuItemRoot + "/Report a Bug/" + CollectLogsMenuItem, priority = Constants.MenuItemOrder)]
		[MenuItem("Help/Needle Engine/" + CollectLogsMenuItem, priority = HelpItemPriority)]
		private static void CollectLogs()
		{
			InternalCollectDebugFiles("Needle Bug Report", new CollectionSettings()
			{
				IncludeNodeModulesFolderVersions = true,
				IncludeUnityProjectAssets = false,
				IncludeWebProjectAssets = false,
				Upload = false
			});
		}


		// [MenuItem("CONTEXT/" + nameof(ExportInfo) + "/" + CopyProjectInfoEntry)]
		[MenuItem(Constants.MenuItemRoot + "/Report a Bug/" + CopyProjectMenuItem, priority = Constants.MenuItemOrder)]
		[MenuItem("Help/Needle Engine/" + CopyProjectMenuItem, priority = HelpItemPriority)]
		private static async void CopyProjectInfo()
		{
			var exportInfo = ExportInfo.Get(true);
			if (!exportInfo)
			{
				EditorGUIUtility.systemCopyBuffer = "No ExportInfo found in the scene";
				Debug.LogError("No ExportInfo component in scene");
				return;
			}
			Debug.Log("Copying project info - please wait a second");
			var info = await ProjectInfoModel.Create(exportInfo);
			info.TypeScriptTypes.Clear();
			var json = info.ToString();
			EditorGUIUtility.systemCopyBuffer = json;
			Debug.Log("<b>Copied</b> project info to clipboard");
		}

		private static string GetBugReportDirectory()
		{
#if UNITY_EDITOR_WIN
			var baseDir = Path.Combine(Path.GetTempPath(), "Needle/BugReports");
#else
			var baseDir = Application.dataPath + "/../Temp/Needle/Reports";
#endif
			return baseDir;
		}

		private static async void InternalCollectDebugFiles(string header, CollectionSettings settings)
		{
			var reportDirectory = GetBugReportDirectory() + "/" + DateTime.Now.ToString("yyMMdd-hhmmss") + "/Report";
			try
			{
				Debug.Log("<b>Begin creating bug report</b>...");
				await Task.Delay(100);
				
				var exportInfo = ExportInfo.Get(true);
				if (!exportInfo)
				{
					// Debug.LogError(
					// 	$"Your current scene is not setup for exporting with Needle Engine! Please see {Constants.DocumentationUrl.AsLink()} for more information. Abort collecting logs.");
					// return;
				}

				if (EditorUtility.DisplayCancelableProgressBar(header, "Collecting project information...", 0.1f))
					return;

				Directory.CreateDirectory(reportDirectory);
				DirectoryInfo projectDir = default;
				ProjectInfoModel projectInfo = default;
				if (exportInfo)
				{
					Debug.Log("Collect project information...");
					var projectPath = exportInfo.GetProjectDirectory();
					projectDir = new DirectoryInfo(projectPath);
					var infoPath = reportDirectory + "/project-info.json";
					projectInfo = await ProjectInfoModel.Create(exportInfo);
					projectInfo.SaveTo(infoPath);
				}

				if (EditorUtility.DisplayCancelableProgressBar(header, "Collect known typescript types...", .2f))
					return;

				DirectoryInfo webDirectory = default;
				if (projectDir?.Exists == true)
				{
					var projectInfoDirectoryPath = reportDirectory + "/project/" + projectDir.Name;
					Directory.CreateDirectory(projectInfoDirectoryPath);
					webDirectory = new DirectoryInfo(projectInfoDirectoryPath);
					if (EditorUtility.DisplayCancelableProgressBar(header, "Collecting web project: " + webDirectory.Name, 0.4f))
						return;
					Debug.Log("Copy web project files...");
					CopyWebProjectFiles(projectDir, webDirectory, settings);
				}

				var unityProjectName = new DirectoryInfo(Application.dataPath + "/..").Name;
				var projectName = settings.IncludeUnityProjectAssets ? ("BugReport Unity " + unityProjectName) : "unity";
				if (EditorUtility.DisplayCancelableProgressBar(header, "Collecting unity project: " + unityProjectName, 0.8f))
					return;
				Debug.Log("Copy relevant unity project files...");
				CopyUnityProjectFiles(new DirectoryInfo(reportDirectory + "/" + projectName), settings, exportInfo?.DirectoryName, webDirectory);
                
				// We could start collecting the description earlier but then the dialog should only open when the description is not yet available
				// for now just ask for the description before zipping everything (otherwise it's not included!)
				var description = await CreateDescription(reportDirectory);
				// insert additional project info into description:
				var humanProjectInfo = CollectHumanReadableProjectInformation(projectInfo);
				if (!string.IsNullOrWhiteSpace(humanProjectInfo)) description = description + "\n\n" + humanProjectInfo;
				
				if (EditorUtility.DisplayCancelableProgressBar(header, "Zip files", 1f))
					return;
				Debug.Log("Start zipping files...");
				var outputName = "Bugreport-" + SceneManager.GetActiveScene().name;
				outputName += "-" + DateTime.Now.ToString("yyMMdd-hhmmss");
				var additionalFlags = "";
				if (settings.IncludeUnityProjectAssets) additionalFlags += "u";
				if (settings.IncludeWebProjectAssets) additionalFlags += "w";
				if (!string.IsNullOrEmpty(additionalFlags)) outputName += "_" + additionalFlags;

				var zipPath = Path.GetFullPath(reportDirectory + "/../" + outputName + ".zip");
				ZipUtility.CompressFolderToZip(zipPath, null, reportDirectory);
				Debug.Log($"<b>Created bug report</b> at " + zipPath.AsLink() +
				          ", please send to the Needle team for debugging purposes (this file may contain sensitive information, so please only send to the development team directly and don't upload it to a public place).");
				EditorUtility.DisplayCancelableProgressBar(header, "Rename output directory", 1f);
				if (settings.Upload)
				{
					if (ActionsHelperPackage.NeedsWebProjectForBugReport() && exportInfo && exportInfo.Exists() == false)
					{
						EditorUtility.DisplayDialog("Bug Report", "Can not automatically upload Bug Report because your web project is not installed: please send the files manually", "Ok, I will send the files manually");
					}
					else
					{
						var size = new FileInfo(zipPath).Length;
						var sizeInMb = (size / 1024f / 1024f).ToString("0.0");
						var msg =
							"Do you want to send this Bug Report to 🌵 Needle? \nSize: " + sizeInMb + " MB\n\nFiles you submit will be end-to-end encrypted and only used for debugging purposes";
						if (EditorUtility.DisplayDialog("Upload Bug Report", msg, "Yes, upload report (recommended)", "No, I will send the files manually"))
						{
							var uploadTask = ActionsHelperPackage.UploadBugReport(zipPath, description);
							var progress = 0f;
							var progressMessage = $"Uploading BugReport ({sizeInMb} MB)";
							while (!uploadTask.IsCompleted)
							{
								EditorUtility.DisplayProgressBar(header, progressMessage, progress);
								await Task.Delay(500);
								progress += (1 - progress) * .01f;
							}
							EditorUtility.DisplayCancelableProgressBar(header,
								$"Finished Uploading BugReport ({sizeInMb} MB)", 1);
							await Task.Delay(1000);
						}
					}
				}
				EditorUtility.RevealInFinder(zipPath);
				FileUtil.DeleteFileOrDirectory(reportDirectory);
			}
			catch (Exception ex)
			{
				var error = ex.ToString();
				var path = reportDirectory + "/exception_on_collecting_error_logs.oops";
				File.WriteAllText(path, error);
				Debug.LogError($"Creating bug report failed, see {path} \n{error}");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static async Task<string> CreateDescription(string reportDirectory)
		{
			var descriptionPath = reportDirectory + "/description.md";
			var description = "";
			var defaultDescription = $@"# <TITLE>
## Describe the bug
e.g. I did xyz and then abc happened

## Steps to reproduce
1) ...
2) ...
3) ...

## How can we contact you if we have further questions?
- email
- discord username
- link to discord post or github issue
";
			var reasonForDescriptionToBeRejected = default(string);
			const int k_maxIterations = 10;
			for(var i = 0; i < k_maxIterations; i++)
			{
				if (!File.Exists(descriptionPath))
				{
					var file = File.CreateText(descriptionPath);
					await file.WriteLineAsync(defaultDescription);
					file.Dispose();
				}
				Debug.Log("Opening " + descriptionPath);
				EditorUtility.OpenWithDefaultApp(descriptionPath);
				await Task.Delay(1000);
				// after a few repeats give the user more time to read the log
				if (i > 4) await Task.Delay(3000);
				bool res = true;
				var message = reasonForDescriptionToBeRejected ??
				              "Please enter a description for the bug.\nReplace <TITLE> with a meaningful title for the bug.";
				if (i < 2)
				{
					res = EditorUtility.DisplayDialog("Bug Report Description", message,"Continue...", "Open Description File");
				}
				else
				{
					var option = EditorUtility.DisplayDialogComplex("Bug Report Description", message, "Continue...", "Open Description File", "Cancel");
					// if user selected ALT (Cancel) or pressed ESC
					if (option == 2) return File.ReadAllText(descriptionPath);
					
				}
				
				if (!res)
				{
					continue;
				}
				description = File.ReadAllText(descriptionPath);
				if (description.Trim().StartsWith(defaultDescription.Trim()))
				{
					reasonForDescriptionToBeRejected =
						"Please describe the bug that you are observing to help us fix the issue... Make sure to replace the <TITLE> with a meaningful title for the bug.";
					Debug.Log(reasonForDescriptionToBeRejected);
					continue;
				}
				// if (description.Trim().StartsWith("## <TITLE>"))
				// {
				// 	reasonForDescriptionToBeRejected = "Please enter a bug title - replace <TITLE> with a short title for the bug";
				// 	Debug.Log(reasonForDescriptionToBeRejected);
				// 	continue;
				// }
				if (description.Trim().Length > 100)
				{
					break;
				}
				reasonForDescriptionToBeRejected = "Please enter a bug description. Your description is too short. It's only " +
				                                   description.Length + " characters long - but must be at least 100 characters";
				Debug.Log(reasonForDescriptionToBeRejected);
			}
			
			return description;
		}

		private static string CollectHumanReadableProjectInformation(ProjectInfoModel projectInfoModel)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("## Project Information");
				sb.AppendLine("Username: " + Environment.UserName);
				sb.AppendLine("License: " + LicenseCheck.LastLicenseTypeResult);
				sb.AppendLine("Unity Version: " + Application.unityVersion);
				sb.AppendLine("Operating System: " + SystemInfo.operatingSystem);
				sb.AppendLine("Graphics Device: " + SystemInfo.graphicsDeviceName);
				if (projectInfoModel != null)
				{
					sb.AppendLine("Node Version: " + projectInfoModel.NodeVersion);
					sb.AppendLine("NPM Version: " + projectInfoModel.NpmVersion);
					sb.AppendLine("RenderPipeline: " + projectInfoModel.RenderPipeline);
					sb.AppendLine("Needle Engine Unity Package: " + projectInfoModel.NeedleEngineExporterVersion);
					sb.AppendLine("Needle Engine Samples: " + projectInfoModel.NeedleEngineSamplesVersion ??
					              "Not installed?");
					sb.AppendLine("Needle Engine: " + projectInfoModel.NeedleEngineVersion ?? "Not installed?");
					sb.AppendLine("Typescript Version: " + projectInfoModel.TypescriptVersion);
				}
				return sb.ToString();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			return "";
		}

		private static void CopyUnityProjectFiles(DirectoryInfo to,
			CollectionSettings settings,
			[CanBeNull] string exportInfoProjectPath,
			[CanBeNull] DirectoryInfo reportedWebDirectory)
		{
			try
			{
				bool TraverseDirectory(DirectoryInfo d) => d.Name != "node_modules" && d.Name != ".git";

				Directory.CreateDirectory(to.FullName);
				var projectDir = new DirectoryInfo(Application.dataPath + "/..");

				var componentGenLog = projectDir + "/Temp/component-compiler.log";
				if (File.Exists(componentGenLog))
				{
					File.Copy(componentGenLog, to.FullName + "/component-compiler.log");
				}

				// Copy toplevel directory files (max 500kb)
				FileUtils.CopyRecursively(projectDir, to, f =>
				{
					switch (f.Extension)
					{
						case ".csproj":
						case ".sln":
						case ".DotSettings":
						case ".user":
						case ".exe":
							return false;
					}
					return f.Length < 1024 * 512;
				}, d => false);

				var sourceScenePath = SceneManager.GetActiveScene().path;
				var dependencyPaths = AssetDatabase.GetDependencies(sourceScenePath, true).ToList();
				var dependencyPathsJson = JsonConvert.SerializeObject(dependencyPaths, Formatting.Indented);
				File.WriteAllText(to.FullName + "/scene_dependencies.json", dependencyPathsJson);
				// ensure we grab all meta files as well
				for (var index = dependencyPaths.Count - 1; index >= 0; index--)
				{
					var dep = dependencyPaths[index];
					dependencyPaths.Add(dep + ".meta");
					
					// Workaround until gltf importer properly registers .bin dependencies
					// assume a .bin file with the same name as the .gltf file next to it is a dependency
					if (dep.EndsWith(".gltf"))
					{
						var binPath = dep.Substring(0, dep.Length - 5) + ".bin";
						if (File.Exists(binPath))
						{
							dependencyPaths.Add(binPath);
							dependencyPaths.Add(binPath + ".meta");
						}
					}
				}
				var sceneDependencies = dependencyPaths.Select(p => new FileInfo(p)).ToArray();

				foreach (var dir in projectDir.EnumerateDirectories())
				{
					switch (dir.Name)
					{
						case "Assets":
							if (settings.IncludeUnityProjectAssets)
							{
								FileUtils.CopyRecursively(dir, new DirectoryInfo(to + "/" + dir.Name),
									f => sceneDependencies.Any(d => d.FullName == f.FullName),
									TraverseDirectory);
							}
							break;
						case "Library":
							if (settings.IncludeUnityProjectAssets)
							{
								var filePath = dir.FullName + "/LastSceneManagerSetup.txt";
								if (File.Exists(filePath))
								{
									var targetLibrary = to.FullName + "/Library";
									Directory.CreateDirectory(targetLibrary);
									File.Copy(filePath, targetLibrary + "/LastSceneManagerSetup.txt");
								}
							}
							break;
						case "Logs":
						case "Packages":
						case "ProjectSettings":
							FileUtils.CopyRecursively(dir, new DirectoryInfo(to + "/" + dir.Name),
								f => f.Length < 1024 * 1024,
								d => false);
							break;
						case "UserSettings":
							FileUtils.CopyRecursively(dir, new DirectoryInfo(to + "/" + dir.Name),
								f => f.Length < 1024 * 1024 && f.Name.Contains("Needle"),
								d => false);
							break;
					}
				}

				if (settings.IncludeUnityProjectAssets)
				{
					var rp = GraphicsSettings.currentRenderPipeline;
					if (rp)
					{
						CopyAsset(rp);

						if (rp.GetType()
							    .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic)
							    ?.GetValue(rp) is Array array)
						{
							foreach (var entry in array)
							{
								if (entry is Object obj)
									CopyAsset(obj);
							}
						}

						void CopyAsset(Object asset)
						{
							var assetPath = AssetDatabase.GetAssetPath(asset);
							var sourcePath = Path.GetFullPath(assetPath);
							var targetPath = to.FullName + "/" + assetPath;
							var dir = Path.GetDirectoryName(targetPath);
							if (dir != null)
							{
								Directory.CreateDirectory(dir);
								File.Copy(sourcePath, targetPath, true);
								var sourceMeta = sourcePath + ".meta";
								var targetMeta = targetPath + ".meta";
								if (File.Exists(sourceMeta))
									File.Copy(sourceMeta, targetMeta, true);
							}
						}
					}
				}

				// if both the unity project and the web project are included 
				// attempt to re-write the project directory in the reported scene
				// so it will already point to the correct report project
				if (settings.IncludeUnityProjectAssets && settings.IncludeWebProjectAssets && reportedWebDirectory?.Exists == true)
				{
					if (exportInfoProjectPath != null)
					{
						var resultScenePath = to.FullName + "/" + sourceScenePath;
						if (File.Exists(resultScenePath))
						{
							var content = File.ReadAllLines(resultScenePath);
							var lineToReplace = "  DirectoryName: " + exportInfoProjectPath;
							var newPath = "  DirectoryName: " + reportedWebDirectory.FullName.RelativeTo(to.FullName + "/");
							var found = false;
							for (var index = 0; index < content.Length; index++)
							{
								var line = content[index];
								if (line == lineToReplace)
								{
									found = true;
									content[index] = newPath;
									break;
								}
							}
							if (found) File.WriteAllLines(resultScenePath, content);
						}
					}
				}

				if (settings.IncludeUnityProjectAssets)
				{
					var sourcePackagesDir = projectDir.FullName + "/Packages";
					var targetPackagesDir = to.FullName + "/Packages";
					var manifestPath = to.FullName + "/Packages/manifest.json";
					CollectLocalPackages(manifestPath);

					void CollectLocalPackages(string packageManifestPath)
					{
						if (File.Exists(packageManifestPath) && PackageUtils.TryReadDependencies(packageManifestPath, out var deps, "dependencies"))
						{
							var newDependencies = new Dictionary<string, string>();
							foreach (var dep in deps)
							{
								var value = dep.Value;
								var isEmbedded = false;
								if (PackageUtils.TryGetPath(sourcePackagesDir, dep.Value, out var path))
								{
									if (Directory.Exists(path))
									{
										// ignore packages that are not used by any scene dependency
										var packageDir = new DirectoryInfo(path);
										// we can not filter packages currently because we would have to check dependencies between packages then too (e.g. needle engine requires unity gltf etc)
										// var packagePath = packageDir.FullName;
										// if (!sceneDependencies.Any(d => d.FullName.StartsWith(packagePath)))
										// 	continue;

										var embeddedPath = targetPackagesDir + "/" + dep.Key;
										isEmbedded = true;
										Directory.CreateDirectory(embeddedPath);
										FileUtils.CopyRecursively(
											packageDir,
											new DirectoryInfo(embeddedPath), f => f.Length < 1024 * 1024 * 4, TraverseDirectory);
									}
								}
								if (!isEmbedded)
									newDependencies.Add(dep.Key, value);
							}
							File.Copy(packageManifestPath, packageManifestPath + ".original.json", true);
							PackageUtils.TryWriteDependencies(packageManifestPath, newDependencies, "dependencies");
						}
					}
				}

				var needleLibrary = new DirectoryInfo(projectDir + "/Library/Needle");
				if (needleLibrary.Exists)
				{
					FileUtils.CopyRecursively(needleLibrary, new DirectoryInfo(to + "/Library/Needle"),
						f => f.Length < 1024 * 512,
						d => false);
				}

				string log1 = default, log2 = default;
#if UNITY_EDITOR_WIN
				var localDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Unity/Editor";
				log1 = localDir + "/Editor.log";
				log2 = localDir + "/Editor-prev.log";
#elif UNITY_EDITOR_OSX
				log1 = "~/Library/Logs/Unity/Editor.log";
				log2 = "~/Library/Logs/Unity/Editor-prev.log";
#else
				log1 = "~/.config/unity3d/Editor.log";
				log2 = "~/.config/unity3d/Editor-prev.log";
#endif

				var log1File = new FileInfo(log1);
				if (log1File.Exists && log1File.Length < 1024 * 1024 * 10)
				{
					var target = to.FullName + "/Editor.log";
					File.Copy(log1File.FullName, target);
					// AnonymizeSerialInEditorLog(target);
				}
				var log2File = new FileInfo(log2);
				if (log2File.Exists && log2File.Length < 1024 * 1024 * 10)
				{
					var target = to.FullName + "/Editor-prev.log";
					File.Copy(log2File.FullName, target, true);
					// AnonymizeSerialInEditorLog(target);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		// private static void AnonymizeSerialInEditorLog(string editorLogPath)
		// {
		// 	if (!File.Exists(editorLogPath)) return;
		// 	var content = File.ReadAllText(editorLogPath);
		//	// serial regex: \"[\d\w]{2}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\"
		// }

		private static void CopyWebProjectFiles(DirectoryInfo webProjectDir, DirectoryInfo to, CollectionSettings settings)
		{
			Directory.CreateDirectory(to.FullName);
			FileUtils.CopyRecursively(webProjectDir, to, f => f.Length < 1024 * 1024 * 2,
				d => settings.IncludeWebProjectAssets && d.Name != "node_modules");

			var info = new ProjectInfo(webProjectDir.FullName);

			var logsList = new List<string>();
			const float logsMaxAgeInSeconds = 24 * 60 * 60; // get npm logs of last 24 hours
			var npmLogsDir = to.FullName + "/npm_logs";
			Directory.CreateDirectory(npmLogsDir);
			if (NpmLogCapture.GetLastLogFileCreated(out var newest, logsMaxAgeInSeconds, logsList))
			{
				foreach (var log in logsList)
				{
					try
					{
						File.Copy(log, npmLogsDir + "/" + Path.GetFileName(log), true);
					}
					catch (UnauthorizedAccessException)
					{
						Debug.LogWarning("Access to " + log + " is not allowed");
					}
				}
			}

			if (settings.IncludeNodeModulesFolderVersions)
			{
				var nodeModules = new DirectoryInfo(webProjectDir.FullName + "/node_modules");
				var targetDir = to.FullName + "/node_modules_versions";
				Directory.CreateDirectory(targetDir);
				CopyDirectoriesAsFiles(nodeModules);

				void CopyDirectoriesAsFiles(DirectoryInfo directory, string prefix = null)
				{
					if (!directory.Exists) return;
					foreach (var dir in directory.EnumerateDirectories())
					{
						if (dir.Name.StartsWith("@"))
						{
							CopyDirectoriesAsFiles(dir, dir.Name + "-");
							continue;
						}
						var path = targetDir + "/" + prefix + dir.Name;
						var finalPath = path;
						if (PackageUtils.TryGetVersion(dir.FullName + "/package.json", out var ver))
							finalPath += "@" + ver;
						try
						{
							File.WriteAllText(finalPath, ver);
						}
						catch (Exception)
						{
							try
							{
								File.WriteAllText(path + "@missing-version", ver);
							}
							catch (Exception e)
							{
								Debug.LogException(e);
							}
						}
					}
				}
			}


			if (!settings.IncludeWebProjectAssets)
			{
				var scriptsPath = new DirectoryInfo(info.ScriptsDirectory);
				if (scriptsPath.Exists)
				{
					// TODO: find relative path to projectFolder
					var targetPath = new DirectoryInfo(to.FullName + "/src/scripts");
					FileUtils.CopyRecursively(scriptsPath, targetPath, f => f.Length < 1024 * 128, d => true);
				}

				var genPath = new DirectoryInfo(info.GeneratedDirectory);
				if (genPath.Exists)
				{
					// TODO: find relative path to projectFolder
					var targetPath = new DirectoryInfo(to.FullName + "/src/generated");
					FileUtils.CopyRecursively(genPath, targetPath, f => f.Length < 1024 * 128, d => true);
				}
			}
			else
			{
				var packageJson = info.PackageJsonPath;
				CopyLocalPackagesAndRewriteDeps(
					new FileInfo(packageJson),
					new FileInfo(to.FullName + "/package.json")
				);

				void CopyLocalPackagesAndRewriteDeps(FileInfo packageJsonPath, FileInfo copiedPackageJsonPath)
				{
					if (packageJsonPath.Exists && PackageUtils.TryReadDependencies(packageJsonPath.FullName, out var deps))
					{
						var targetDirectory = copiedPackageJsonPath.Directory!.FullName;
						foreach (var dep in deps)
						{
							if (dep.Key == "three") continue;
							if (dep.Key == Constants.RuntimeNpmPackageName) continue;
							if (PackageUtils.TryGetPath(packageJsonPath.Directory!.FullName, dep.Value, out var path))
							{
								var relativeDirectory = "./local_dependencies/" + dep.Key;
								var copyTo = new DirectoryInfo(targetDirectory + "/" + relativeDirectory);
								copyTo.Create();
								FileUtils.CopyRecursively(new DirectoryInfo(path), copyTo, f => f.Length < 1024 * 1024 * 2, d => d.Name != "node_modules");
								PackageUtils.ReplaceDependency(copiedPackageJsonPath.FullName, dep.Key, "file:" + relativeDirectory);
							}
						}
					}
				}
			}
		}

		private class ProjectInfoModel
		{
			public string ExportInfoGameObjectName;
			public bool ExportInfoGameObjectIsEnabled;
			public string UnityProjectPath;
			public string UnityVersion;
			public string SceneName;
			public string ProjectPath;
			public bool ProjectDirectoryExists;
			public bool ProjectIsInstalled;
			public bool NeedleEngineInstalled;
			public bool HasNodeInstalled;
			public string NodeVersion;
			public string NpmVersion;
			public string TypescriptVersion;
			public bool HasTokTxInstalled;
			public bool HasMinimumToktxVersionInstalled;
			public string RenderPipeline;
			public bool GzipEnabled;
			public string NeedleEngineExporterVersion;
			public string NeedleEngineVersion;
			[CanBeNull] public string NeedleEngineSamplesVersion;
			public string NeedleEngineExporterPath;
			public string NeedleEnginePath;
			public string FileStats;
			public List<string> NeedleComponentsInScene = new List<string>();
			public bool TypeCacheIsDirty;
			public List<ImportInfo> TypeScriptTypes;

			public static async Task<ProjectInfoModel> Create(ExportInfo info)
			{
				var exporterVersion = ProjectInfo.GetCurrentNeedleExporterPackageVersion(out var exporterPackageJsonPath);
				var runtimeVersion = ProjectInfo.GetCurrentNeedleEngineVersion(info.GetProjectDirectory(), out var runtimePackageJsonPath);
				var samplesVersion = ProjectInfo.GetCurrentNeedleEngineSamplesVersion();
				var nodejsInstalled = await Actions.HasNpmInstalled();
				var hasTokTxInstalled = await Actions.HasToktxInstalled(false);
				var model = new ProjectInfoModel()
				{
					ExportInfoGameObjectName = info.name,
					ExportInfoGameObjectIsEnabled = info.gameObject.activeInHierarchy,
					UnityProjectPath = Application.dataPath,
					UnityVersion = Application.unityVersion,
					ProjectPath = info.DirectoryName,
					SceneName = SceneManager.GetActiveScene().name,
					ProjectDirectoryExists = info.Exists(),
					ProjectIsInstalled = info.IsInstalled(),
					NeedleEngineInstalled = Directory.Exists(Path.GetFullPath(info.GetProjectDirectory()) + "/node_modules/" + Constants.RuntimeNpmPackageName),
					HasNodeInstalled = nodejsInstalled,
					HasTokTxInstalled = hasTokTxInstalled,
					HasMinimumToktxVersionInstalled = Actions.HasMinimumToktxVersionInstalled(out _),
					RenderPipeline = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.ToString() : "Built-in",
					GzipEnabled = NeedleEngineBuildOptions.UseGzipCompression,
					NeedleEngineVersion = runtimeVersion,
					NeedleEngineExporterVersion = exporterVersion,
					NeedleEnginePath = runtimePackageJsonPath,
					NeedleEngineExporterPath = exporterPackageJsonPath,
					NeedleEngineSamplesVersion = samplesVersion,
					FileStats = FileUtils.CalculateFileStats(new DirectoryInfo(info.GetProjectDirectory() + "/assets")),
					TypeCacheIsDirty = TypesUtils.IsDirty,
				};
				if (nodejsInstalled)
				{
					try
					{
						var nodeLogs = string.Join("; ", ProcessHelper.RunCommandEnumerable("node --version").ToArray());
						model.NodeVersion = nodeLogs;
						var npmLogs = string.Join("; ", ProcessHelper.RunCommandEnumerable("npm --version").ToArray());
						model.NpmVersion = npmLogs;
						var typescriptVersion = string.Join("; ", ProcessHelper.RunCommandEnumerable("tsc --version").ToArray());
						model.TypescriptVersion = typescriptVersion.Replace("\u0000", "");
					}
					catch (Exception)
					{
						// ignored
					}
				}
				model.CollectNeedleComponentsInProject();
				model.TypeScriptTypes = TypesUtils.CurrentTypes.ToList();
				return model;
			}

			private void CollectNeedleComponentsInProject()
			{
				var list = new List<Component>();
				foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
				{
					root.GetComponentsInChildren(list);
					foreach (var comp in list)
					{
						try
						{
							if (!comp) continue;
							var type = comp.GetType();
							if (type.Namespace?.Contains("Needle") ?? false)
							{
								if (!NeedleComponentsInScene.Contains(type.FullName))
									NeedleComponentsInScene.Add(type.FullName);
								// NeedleComponents.Add(EditorJsonUtility.ToJson(comp));
							}
						}
						catch (Exception ex)
						{
							Debug.LogException(ex);
						}
					}
				}
			}

			public void SaveTo(string path)
			{
				if (File.Exists(path)) File.Delete(path);
				File.WriteAllText(path, ToString());
			}

			public override string ToString()
			{
				var json = JsonConvert.SerializeObject(this, Formatting.Indented);
				return json;
			}
		}
	}
}