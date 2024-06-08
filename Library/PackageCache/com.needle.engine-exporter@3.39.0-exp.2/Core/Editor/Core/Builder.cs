#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Needle.Engine.Core.Emitter;
using Needle.Engine.Core.References;
using Needle.Engine.Editors;
using Needle.Engine.Interfaces;
using Needle.Engine.Problems;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using Needle.Engine.Writer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Transform = UnityEngine.Transform;

namespace Needle.Engine.Core
{
	internal class InvalidBuildException : Exception
	{
		public InvalidBuildException(string message = "") : base(message)
		{
		}
	}

	public struct BuildInfo
	{
		public readonly string ProjectDirectory;
		public readonly string PackageJsonPath;

		public bool IsValid() => !string.IsNullOrEmpty(ProjectDirectory) && File.Exists(PackageJsonPath);

		public BuildInfo(ExportInfo exp)
		{
			this.ProjectDirectory = exp.DirectoryName;
			this.PackageJsonPath = exp.PackageJsonPath;
		}

		public static BuildInfo FromExportInfo(ExportInfo? i)
		{
			if (i == null) return new BuildInfo();
			return new BuildInfo(i);
		}
	}

	public static class Builder
	{
		public static bool IsBuilding { get; private set; }
		public static IExportContext? CurrentContext => currentContext;
		/// <summary>
		/// The scene that started the export
		/// </summary>
		public static Scene RootScene { get; private set; }

		public static readonly string BasePath = Application.dataPath + "/../";
		private static readonly ImportsGenerator importsGenerator = new ImportsGenerator();
		private static readonly Stopwatch watch = new Stopwatch();
		private static IEmitter[]? emitters;
		private static IBuildStageCallbacks[]? buildProcessors;

		private static readonly List<IBuildCallbackComponent> buildCallbackComponents =
			new List<IBuildCallbackComponent>();

		// private static int currentBuildProgressId = -1;
		private static bool isCurrentBuildProgressCancelled = false;
		private static ExportContext? currentContext = null;
		private static Task<bool>? currentBuildProcess = default;
		private static bool isWaitingForInstallationToFinish = false;

		public static event Action? BuildStarting, BuildEnded, BuildFailed;

		/// <summary>
		/// Called when the build is done but it's not decided if it was successful or not
		/// </summary>
		internal static event Action? BuildEnding;

		internal static Func<ExportContext, string>? DoExportCurrentScene;

		// https://threejs.org/docs/#manual/en/introduction/Creating-a-scene
		// https://threejs.org/editor/

		internal static async Task<bool> Build(bool isImplicitExport,
			BuildContext buildContext,
			int parentTaskId = -1,
			BuildInfo? info = null)
		{
			if (isImplicitExport && BuildPipeline.isBuildingPlayer)
			{
				Debug.LogWarning("Editor is building – abort export");
				return false;
			}

			if (IsBuilding)
			{
				Debug.LogWarning("Build is already in process");
				if (currentBuildProcess != null)
					return await currentBuildProcess;
				return false;
			}

			if (Actions.IsInstalling())
			{
				Debug.LogWarning(
					"Project is currently installing → waiting for it to finish until trying to export the project");
				SceneView.lastActiveSceneView?.ShowNotification(
					new GUIContent("Needle Engine: Waiting for installation to finish..."));
				if (isWaitingForInstallationToFinish) return false;
				isWaitingForInstallationToFinish = true;
				var didInstall = await Actions.WaitForInstallationToFinish();
				if (!didInstall) return false;
				Debug.Log("Installation finished.");
				if (currentBuildProcess != null)
					return await currentBuildProcess;
			}
			isWaitingForInstallationToFinish = false;


			ProjectInfo? paths = default;
			if (info != null)
			{
				paths = new ProjectInfo(Path.GetFullPath(info.Value.ProjectDirectory));
			}
			else if (TryGetProjectInfo(isImplicitExport, out paths, out var expInfo))
			{
				info = BuildInfo.FromExportInfo(expInfo);
			}
			else
				return false;


			// var trialLicenseMessage = default(string);
			// var trialLicenseMessageType = LogType.Warning;
			// if (!await LicenseCheck.HasValidLicense())
			// {
			// 	if (NeedleEngineAuthorization.TrialEnded)
			// 	{
			// 		trialLicenseMessage =
			// 			"<b>Needle Engine Pro Trial has ended</b>: Purchase a license to continue using all features of Needle Engine. " +
			// 			Constants.BuyLicenseUrl.AsLink();
			// 	}
			// 	else
			// 	{
			// 		var daysUntilEndOfTrial = NeedleEngineAuthorization.DaysUntilTrialEnds;
			// 		if (daysUntilEndOfTrial > 0)
			// 		{
			// 			trialLicenseMessage = "<b>Needle Engine Pro Trial period ends in " + daysUntilEndOfTrial +
			// 			                      " days</b>. Purchase a commercial license: " +
			// 			                      Constants.BuyLicenseUrl.AsLink();
			// 		}
			// 	}
			// }

			BuildTaskList.ResetAllAndCancelRunning();

			if (EulaWindow.RequiresEulaAcceptance)
			{
				Debug.LogWarning("You need to accept our Terms of Use before using Needle Engine.", ExportInfo.Get());
				EulaWindow.Open();
				SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Needle Engine: EULA not accepted"));
				return false;
			}
			
			// if a user's trial has ended and they don't have a license they need to allow us to contact them
			if (LicenseCheck.HasLicense == false && !await EulaWindow.HasAllowedContact())
			{
				Debug.LogWarning("<b>You need to allow Needle to contact you regarding news and your use of Needle Engine. Please enable the checkbox in the Needle EULA window</b>. This option can be disabled when you have a Indie or Pro license activated.", ExportInfo.Get()); 
				EulaWindow.Open();
				EulaWindow.DidOpenDuringExport = true;
				SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Needle Engine: Enable Allow Contact in EULA window"));
				return false;
			}

			Debug.Log($"<b>Begin building</b> web scene");
			var didSucceed = false;
			watch.Restart();
			using var @lock = new NeedleLock(paths.ProjectDirectory);
			try
			{
				SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Needle Engine: Exporting Scene"));
				currentContext = null;
				isCurrentBuildProgressCancelled = false;
				IsBuilding = true;

				var authorized = await NeedleEngineAuthorization.IsAuthorized(LicenseCheck.LicenseEmail);
				if (!authorized.success)
				{
					if (LicenseCheck.LastLicenseResult != true)
					{
						var msg = authorized.message ??
						          $"Needle Engine is not authorized to build the project: Purchase a license at {Constants.BuyLicenseUrl.AsLink()}. If you believe this is a mistake, please contact hi@needle.tools.";
						Debug.LogError(msg);
						return false;
					}
				}

				BuildStarting?.Invoke();
				using (new CultureScope())
				{
					currentBuildProcess = InternalOnBuild(buildContext, paths, info.Value);
					didSucceed = await currentBuildProcess;
				}
				BuildEnding?.Invoke();
			}
			catch (InvalidBuildException invalid)
			{
				if (!string.IsNullOrEmpty(invalid.Message))
					Debug.LogError(invalid.Message);
				didSucceed = false;
				// trialLicenseMessageType = LogType.Error;
				if (NeedleProjectConfig.TryGetAssetsDirectory(out var assetsDirectory))
				{
					await FileUtils.DeleteDirectoryRecursive(assetsDirectory);
				}
				if (currentContext != null)
				{
					currentContext.Writer.Write("import { showBalloonWarning } from \"@needle-tools/engine\"");
					var msg =
						"\"Needle Engine Pro Trial has ended. Please see the Unity Editor for more information.\"";
					currentContext.Writer.Write("console.warn(" + msg + ");");
					currentContext.Writer.Write("showBalloonWarning(" + msg + ");");
					currentContext.Writer.Flush();
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);

				// invoke failed callbacks
				if (buildProcessors != null)
				{
					foreach (var proc in buildProcessors)
					{
						try
						{
							await proc.OnBuild(BuildStage.BuildFailed, currentContext);
						}
						catch (Exception processorException)
						{
							Debug.LogException(processorException);
						}
					}
				}
			}
			finally
			{
				currentBuildProcess = null;
				IsBuilding = false;
				BuildEnded?.Invoke();
			}

			var elapsed = watch.Elapsed.TotalMilliseconds;
			watch.Stop();
			var dir = paths.ProjectDirectory;
			Debug.Log($"<b>Finished building</b> in {elapsed:0} ms to <a href=\"{dir}\">{dir}</a>");
			if (!didSucceed)
			{
				Debug.LogWarning("<b>Build failed</b> - see logs for reason");
				BuildFailed?.Invoke();
			}

			if (elapsed > 2000 && ExporterProjectSettings.instance.smartExport == false)
			{
				Debug.LogWarning(
					"Consider enabling Smart Export in ProjectSettings/Needle Engine/Settings to speed up build times");
			}

			if (didSucceed)
			{
				SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Needle Engine: Export finished"), 5);
			}
			else
			{
				SceneView.lastActiveSceneView?.ShowNotification(
					new GUIContent("Needle Engine: Scene export failed (see console)"), 6);
			}

#if !UNITY_2021_3_OR_NEWER
			Debug.LogWarning("Unity " + Application.unityVersion + " is <b>not supported by Needle Engine</b>. Please upgrade to a Unity LTS version (2021.3, 2022.3, ...)");
#endif

			if (Random.value > .7f)
				InternalActions.LogFeedbackFormUrl();

			try
			{
				currentContext?.Dispose();
			}
			finally
			{
				currentContext = null;
			}

			// if (!string.IsNullOrWhiteSpace(trialLicenseMessage))
			// 	Debug.LogFormat(trialLicenseMessageType, LogOption.NoStacktrace, default(Object), trialLicenseMessage);

			return didSucceed;
		}

		private static bool TryGetProjectInfo(bool isAutoExport, out ProjectInfo projectPaths, out ExportInfo info)
		{
			projectPaths = null!;
			info = null!;

			var infos = Object.FindObjectsByType<ExportInfo>(FindObjectsSortMode.None);
			if (infos.Length <= 0)
			{
				if (ExporterProjectSettings.instance.debugMode)
					Debug.LogWarning($"Can't auto-build project, no {nameof(ExportInfo)} found in scene.");
				return false;
			}
			if (infos.Length > 1)
			{
				var foundExportInfoInActiveScene = false;
				var activeScenePath = SceneManager.GetActiveScene().path;
				foreach (var found in infos)
				{
					var scene = found.gameObject.scene;
					if (activeScenePath == scene.path)
					{
						Debug.LogWarning("Found multiple ExportInfo components: Will use the first one found in the active scene", found);
						foundExportInfoInActiveScene = true;
						infos[0] = found;
						break;
					}
				}
				if(!foundExportInfoInActiveScene)
					throw new Exception("Found multiple ExportInfo components. Only one is allowed: " +
			                     string.Join(", ", infos.Select(i => i.name)));
				
			}

			info = infos[0];

			if (!info.gameObject.activeInHierarchy)
			{
				Debug.LogError(
					$"Your ExportInfo GameObject \"{info.gameObject.name}\" is not enabled. Please make sure the GameObject is active in the hierarchy, otherwise the exported project may not work properly",
					info);
			}

			if (isAutoExport && !info.AutoExport)
				return false;

			if (string.IsNullOrEmpty(info.DirectoryName))
			{
				if (!isAutoExport)
					Debug.LogError("Empty project directory", info);
				return false;
			}


			var projectDir = Path.GetFullPath(info.DirectoryName);
			if (!Directory.Exists(projectDir))
			{
				var msg = "Project directory does not yet exist, select the " + nameof(ExportInfo) +
				          " component and generate a project.\n" + projectDir;
				if (isAutoExport) Debug.LogWarning(msg, info);
				else Debug.LogError(msg);
				info.RequestInstallationIfTempProjectAndNotExists();
				return false;
			}

			projectPaths = new ProjectInfo(projectDir);
			return true;
		}

		private static async Task<bool> InternalOnBuild(BuildContext buildContext,
			ProjectInfo projectPaths,
			BuildInfo info)
		{
			if (ProjectValidator.FindProblems(info.PackageJsonPath, out var problems))
			{
				if (!await ProblemSolver.TryFixProblems(projectPaths.ProjectDirectory, problems))
				{
					Debug.LogError(
						"Can not build because package.json has problems. Please fix errors listed below first:",
						ExportInfo.Get());
					foreach (var p in problems) Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "{0}", p);
					return false;
				}
			}

			var dir = Path.GetFullPath(projectPaths.ProjectDirectory);
			if (!DriveHelper.HasSymlinkSupport(dir))
			{
				Debug.LogWarning(
					"Your project drive does not have symlink support which might cause errors with symlinks created by npm. Consider moving your project to another drive.\n" +
					dir);
			}

			buildCallbackComponents.Clear();
			if (!Directory.Exists(projectPaths.AssetsDirectory))
				Directory.CreateDirectory(projectPaths.AssetsDirectory);
			if (!Directory.Exists(projectPaths.ScriptsDirectory))
				Directory.CreateDirectory(projectPaths.ScriptsDirectory);
			if (!Directory.Exists(projectPaths.GeneratedDirectory))
				Directory.CreateDirectory(projectPaths.GeneratedDirectory);
			if (!Directory.Exists(projectPaths.EngineComponentsDirectory))
			{
				Debug.LogWarning("Needle Engine directory not found → <b>please run Install</b>\n" +
				                 projectPaths.EngineComponentsDirectory, ExportInfo.Get());

				if (Actions.IsInstalling() == false && !await Actions.InstallCurrentProject(false))
				{
					return false;
				}
			}

			if (PlayerSettings.colorSpace != ColorSpace.Linear)
			{
				Debug.LogError("<b>Wrong colorspace</b> \"" + PlayerSettings.colorSpace +
				               "\" → please set it to Linear, otherwise your exported project will look incorrect. Change under \"Edit/Project Settings/Player/Other Settings\"");
			}
			
			RootScene = SceneManager.GetActiveScene();

			// var typesList = new List<ImportInfo>();
			importsGenerator.BeginWrite();

			// TODO: module type exports should go into the module and just be imported here (but need to figure out how to then parse existing scripts from imported modules then)

			// find scripts
			// var scriptPath = projectPaths.GeneratedDirectory + "/scripts.js";
			// TypesUtils.MarkDirty();
			var types = TypesUtils.GetTypes(projectPaths);
			// importsGenerator.WriteTypes(types, scriptPath, new DirectoryInfo(projectPaths.ProjectDirectory).Name);
			// importsGenerator.EndWrite(types, scriptPath);

			var generatePath = projectPaths.GeneratedDirectory + "/gen.js";
			// using var writer = new StringWriter();//fullPath, false, Encoding.UTF8);

			var hash = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()
				.ToString(); //.GetHashCode().ToString();
			var references = new TypeRegistry(types);
			var writer = new CodeWriter(generatePath);
			currentContext = new ExportContext(projectPaths.ProjectDirectory, hash, buildContext, projectPaths, writer,
				references, null);

			emitters ??= InstanceCreatorUtil.CreateCollectionSortedByPriority<IEmitter>().ToArray();
			buildProcessors ??= InstanceCreatorUtil.CreateCollectionSortedByPriority<IBuildStageCallbacks>().ToArray();

			foreach (var bb in buildProcessors)
			{
				var res = await bb.OnBuild(BuildStage.Setup, currentContext);
				if (!res)
				{
					Debug.LogError("Setup failed: " + bb.GetType().Name);
					return false;
				}
			}

			var runExport = buildContext.Command != BuildCommand.PrepareDeploy;

			if (ExporterProjectSettings.instance.smartExport)
			{
				// Always export the scene for distribution builds
				// This is necessary as long as we fully clear the assets directory
				// We could introduce something like a build distribution cache when smart export is enabled
				if (buildContext.IsDistributionBuild == false)
				{
					var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneManager.GetActiveScene().path);
					if (currentContext.TryGetAssetDependencyInfo(scene, out var sceneAssetInfo))
					{
						if (sceneAssetInfo.HasChanged == false && OutputDirectoryContainsGlbAssets(projectPaths))
						{
							Debug.Log(
								"~ Scene has not changed, skipping build. (Tip: You can force a full export by holding ALT and clicking the 'Play' button on the ExportInfo component)"
									.LowContrast());
							runExport = false;
							var lastSceneView = SceneView.lastActiveSceneView;
							if (lastSceneView)
							{
								lastSceneView.ShowNotification(
									new GUIContent("Scene has not changed, skipping export"));
							}
						}
					}
				}
			}

			if (runExport)
			{
				var exportInfo = ExportInfo.Get();
				
				// TODO: we might want to just delete GLB and glTF + bin files because this will also delete font assets etc OR the font asset generator should be have an cache somewhere else
				if(exportInfo.AutoCompress)
					await FileUtils.DeleteDirectoryRecursive(currentContext.AssetsDirectory);
				
				// Register dependencies promise. see https://linear.app/needle/issue/NE-4445
				writer.Write($"globalThis[\"needle:dependencies:ready\"] = import(\"./register_types.ts\")");

				// invoke pre build interface implementations
				currentContext.Reset();
				foreach (var bb in buildProcessors)
				{
					var res = await bb.OnBuild(BuildStage.PreBuildScene, currentContext);
					if (!res)
					{
						Debug.LogError("PreBuild callback failed: " + bb);
						return false;
					}
				}


				writer.Write("");
				writer.Write("export const needle_exported_files = new Array();");
				writer.Write("globalThis[\"needle:codegen_files\"] = needle_exported_files;");

				currentContext.Reset();
				foreach (var bb in buildProcessors)
				{
					await bb.OnBuild(BuildStage.BeginSceneLoadFunction, currentContext);
				}
				var didExport = await TraverseGameObjectsInScene(references, writer, currentContext, emitters);
				if (!didExport)
				{
					var scene = SceneManager.GetActiveScene();
					if (string.IsNullOrEmpty(scene.path))
					{
						if(EditorUtility.DisplayDialog("Save Scene", "Please save your scene first before exporting.", "Save Scene"))
						{
							EditorSceneManager.SaveScene(scene);
							AssetDatabase.Refresh();
						}
					}
					var path = DoExportCurrentScene?.Invoke(currentContext);
					if (!File.Exists(path)) path = currentContext.AssetsDirectory + "/" + path;
					if (File.Exists(path))
						GltfEmitter.WriteExportedFilePath(writer, currentContext, path);
				}
				currentContext.Reset();
				foreach (var bb in buildProcessors)
				{
					// this callback is mainly used by the resources gltf
					// so any member or field has time to add scene resources
					await bb.OnBuild(BuildStage.EndSceneLoadFunction, currentContext);
				}

				writer.Write(@"document.addEventListener(""DOMContentLoaded"", () =>");
				writer.BeginBlock();
				writer.Write("const needleEngine = document.querySelector(\"needle-engine\");");
				writer.Write("if(needleEngine && needleEngine.getAttribute(\"src\") === null)");
				writer.BeginBlock();
				writer.Write($"needleEngine.setAttribute(\"hash\", \"{currentContext.Hash}\");");
				writer.Write($"needleEngine.setAttribute(\"src\", JSON.stringify(needle_exported_files));");
				writer.EndBlock();
				writer.EndBlock(");");
				// writer.Write($"engine.build_scene_functions[\"{fnName}\"] = {fnName};");

				// invoke build interface implementations
				currentContext.Reset();
				foreach (var bb in buildProcessors)
				{
					await bb.OnBuild(BuildStage.PostBuildScene, currentContext);
				}

				if (LicenseCheck.LasLicenseResultIsProLicense == false)
				{
					var msg = "Made with ♥ by 🌵 Needle - https://needle.tools";
					var version = ProjectInfo.GetCurrentNeedleExporterPackageVersion(out _);
					if (version != null) msg += " — Version " + version;
					writer.Write($"\nconsole.log(\"{Regex.Escape(msg)}\");");
				}

				if (isCurrentBuildProgressCancelled)
					writer.Write(
						"console.warn(\"WARNING: The build process of the scene was cancelled - it therefor may be incomplete and throw errors\");");
				writer.Flush();

				foreach (var comp in buildCallbackComponents)
				{
					comp.OnBuildCompleted();
				}

				// wait for all tasks that must be finished before we can continue
				await BuildTaskList.WaitForPostExportTasksToComplete();

				if (exportInfo && exportInfo.AutoCompress && currentContext.BuildContext.Command == BuildCommand.BuildLocalDev)
				{
					try
					{
						Debug.Log("Run AutoCompression...\n" + currentContext.AssetsDirectory, exportInfo);
						await ActionsCompression.MakeProgressive(currentContext.AssetsDirectory);
						await ActionsCompression.CompressFiles(currentContext.AssetsDirectory);
					}
					catch (Exception e)
					{
						Debug.LogError("Failed to compress files: " + e);
					}
				}
			}


			return true;
		}

		private static bool OutputDirectoryContainsGlbAssets(IProjectInfo proj)
		{
			var dir = proj.AssetsDirectory;
			foreach (var file in Directory.EnumerateFiles(dir, "*.*"))
			{
				if (file.EndsWith(".glb") || file.EndsWith(".gltf"))
				{
					return true;
				}
			}
			return false;
		}

		private static async Task<bool> TraverseGameObjectsInScene(TypeRegistry references,
			ICodeWriter writer,
			ExportContext context,
			IEmitter[] em)
		{
			var gos = SceneManager.GetActiveScene().GetRootGameObjects();
			var didExportAny = false;
			for (var index = 0; index < gos.Length; index++)
			{
				if (isCurrentBuildProgressCancelled) break;
				var go = gos[index];
				if (!go) continue;
				context.Reset();
				// if we wait every object export becomes shorter. Reporting with sync flag doesnt update the UI immediately
				if (index <= 0) await Task.Delay(10);
				else if (index % 10 == 0) await Task.Delay(10);
				didExportAny |= await Traverse(go, context, em);
			}
			return didExportAny;
		}

		private static readonly List<Component> _componentsBuffer = new List<Component>();

		private static async Task<bool> Traverse(GameObject go, ExportContext context, IEmitter[] emitter)
		{
			if (isCurrentBuildProgressCancelled) return false;
			if (!go) return false;
			if (!go.CompareTag("EditorOnly"))
			{
				_componentsBuffer.Clear();
				go.GetComponents(_componentsBuffer);
				foreach (var comp in _componentsBuffer)
				{
					if (comp is IBuildCallbackComponent bc)
						buildCallbackComponents.Add(bc);
				}

				var wasInGltf = context.IsInGltf;
				var didExport = false;
				foreach (var e in emitter)
				{
					if (isCurrentBuildProgressCancelled) return false;
					foreach (var comp in _componentsBuffer)
					{
						if (isCurrentBuildProgressCancelled) return false;
						didExport |= ExportComponent(go, comp, context, e);
					}
				}
				var t = go.transform;
				var id = t.GetId();
				foreach (Transform child in go.transform)
				{
					if (isCurrentBuildProgressCancelled) return false;
					var name = $"{t.name}_{id}";
					ReferenceExtensions.ToJsVariable(ref name);
					context.ParentName = name;
					didExport |= await Traverse(child.gameObject, context, emitter);
				}
				context.IsInGltf = wasInGltf;
				return didExport;
			}

			return false;
		}

		private static bool ExportComponent(GameObject go, Component comp, ExportContext context, IEmitter emitter)
		{
			if (isCurrentBuildProgressCancelled) return false;
			if (!comp)
			{
				Debug.LogWarning("Missing script on " + go, go);
				return false;
			}
			var type = comp.GetType();
			if (type.GetCustomAttribute<NeedleEngineIgnore>() != null) return false;
			context.Root = go.transform;
			context.GameObject = go;
			context.Component = comp;
			context.VariableName = $"{go.name}_{comp.GetId()}".ToJsVariable();
			var res = emitter.Run(comp, context);
			if (res.Success)
			{
				context.IsExported = true;
				context.ObjectCreated = res.HierarchyExported;
				context.Writer.Write("");
				return true;
			}
			return false;
		}
	}
}