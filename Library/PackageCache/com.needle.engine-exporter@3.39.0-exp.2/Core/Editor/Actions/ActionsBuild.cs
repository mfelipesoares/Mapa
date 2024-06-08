using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal static class ActionsBuild
	{
		// TODO: move all the deployment options to this class
		// refactor to take cancellation token and pass through process id for root cancellation

		internal static string GetBuildOutputDirectory(string projectDirectory)
		{
			if (NeedleProjectConfig.TryLoad(projectDirectory, out var config))
			{
				if (!string.IsNullOrWhiteSpace(config.buildDirectory))
					return projectDirectory + "/" + config.buildDirectory;
			}
			return projectDirectory + "/dist";
		}

		private static IBuildDistCallbackReceiver[] beforeBuildDistReceivers;

		internal static Task<bool> BeforeBuild(string projectDir)
		{
			if(PackageUtils.TryGetScripts(projectDir + "/package.json", out var scripts))
			{
				if(scripts.ContainsKey("pre-build"))
				{
					Debug.Log("Run pre-build script");
					return ProcessHelper.RunCommand("npm run pre-build", projectDir);
				}
				Debug.Log("Info: Add 'pre-build' script to your package.json to run it before building (For example to run 'tsc' to make sure your code compiles)".LowContrast());
			}
			return Task.FromResult(true);
		}

		internal static async Task<bool> InternalBuildDistTask(BuildContext buildContext, string projectDirectory = null, int buildProgressId = -1)
		{
			if (string.IsNullOrEmpty(projectDirectory))
				SceneExportUtils.IsValidExportScene(out projectDirectory, out _);

			if (string.IsNullOrEmpty(projectDirectory))
			{
				Debug.LogError("No project directory found");
				return false;
			}

			// Set build context
			BuildContext.Current = buildContext;
			
			var dir = GetBuildOutputDirectory(projectDirectory);
			beforeBuildDistReceivers ??= InstanceCreatorUtil.CreateCollectionSortedByPriority<IBuildDistCallbackReceiver>().ToArray();
			foreach (var b in beforeBuildDistReceivers)
			{
				await b.BeforeBuildDist(projectDirectory, dir);
			}
				
			var buildFileName = "build.log";
			var buildFilePath = projectDirectory + "/" + buildFileName;
			if (File.Exists(buildFileName)) File.Delete(buildFilePath);

			// this is for support of legacy vite template projects
			// newer projects will use a vite plugin to enable/disable gzip compression
			var useGzip = NeedleEngineBuildOptions.UseGzipCompression;
			ViteUtils.ChangeGzipCompression(projectDirectory, useGzip, out _);

			var scripts = GetScripts(projectDirectory);

			var cmd = GetBuildCommand(scripts, buildContext);

			var buildTypeDisplayString = buildContext.Command == BuildCommand.BuildProduction ? "Production" : "Development";
			
			if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Begin {buildTypeDisplayString} Build"), 5);

			if (buildProgressId >= 0) Progress.Report(buildProgressId, .5f, "Run build command: " + cmd);
			var success = await ProcessHelper.RunCommand(cmd, projectDirectory, buildFilePath, true, true, buildProgressId);

			if (buildProgressId >= 0 && Progress.Exists(buildProgressId))
				Progress.Finish(buildProgressId);

			if (success)
			{
				if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"{buildTypeDisplayString} Build finished successfully"), 5);
				
				if (NeedleEngineBuildOptions.UseGzipCompression == false)
				{
					Debug.LogWarning(
						"<b>Gzip compression is disabled</b>: consider enabling gzip compression if your webserver supports it to reduce download size and improve performance. You can enable it in the Needle Engine Build settings.");
				}
				
				if (Application.isBatchMode == false && buildContext.LiveUrl == null && buildContext.AllowShowFolderAfterBuild)
					Application.OpenURL(GetBuildOutputDirectory(projectDirectory));
			}
			else
			{
				SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"{buildTypeDisplayString} Build Failed"), 5);
			}
			return success;
		}

		public static string GetBuildCommand(Dictionary<string, string> scripts, BuildContext context)
		{
			var cmd = InternalGetBuildCommand(scripts, context, out var commandValue);
			if (cmd != null)
				cmd += GetBuildCommandArgs(context);

			cmd = $"npm run {cmd}";
			return cmd;
		}

		private static Dictionary<string, string> GetScripts(string projectDirectory)
		{
			if (PackageUtils.TryGetScripts(projectDirectory + "/package.json", out var scripts))
				return scripts;
			Debug.LogWarning("Failed reading scripts from package.json at " + projectDirectory);
			return null;
		}
		
		private static string InternalGetBuildCommand(Dictionary<string, string> scripts, BuildContext context, out string scriptCommand)
		{
			var buildCommand = context.Command;

			// if we are in a vite project we will perform a development build
			// because we handle compression (via build:production) via the vite plugin
			if (buildCommand == BuildCommand.BuildProduction)
			{
				// if any of the scripts contains vite and build, we assume it's a vite project
				var isViteProject = scripts.Any((kvp) =>
				{
					if (kvp.Value.Contains("vite ") && kvp.Value.Contains(" build")) return true;
					return false;
				});
				// alternatively we could also modify the package.json to only contain a script for buidling and remove the build:dev/build:production and gltf:transform scripts
				if (isViteProject)
				{
					// https://linear.app/needle/issue/NE-3798
					Debug.Log("Vite project detected - will run the production build using the build:dev command because compression will be handled by the needle-build-pipeline vite plugin");
					buildCommand = BuildCommand.BuildDev;
				}
			}
			
			var scriptName = buildCommand switch
			{
				BuildCommand.BuildProduction => "build:production",
				BuildCommand.BuildDev => "build:dev",
				BuildCommand.PrepareDeploy => "build:dev",
				_ => "build"
			};

			if (scripts != null)
			{
				// check if the package json contains one of our known script names
				if (scripts.TryGetValue(scriptName, out scriptCommand))
				{
					return scriptName;
				}

				// the package json has no known build command
				foreach (var kvp in scripts)
				{
					if (kvp.Key.IndexOf("build", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						scriptCommand = kvp.Value;
						return kvp.Key;
					}
				}
			}

			scriptCommand = null;
			return scriptName;
		}

		private static string GetBuildCommandArgs(BuildContext context)
		{
			return "";
		}

		private static async Task<bool> HandleCompressionIfNecessary(string projectDirectory, BuildContext context, Dictionary<string, string> scripts)
		{
			if (context.Command == BuildCommand.BuildProduction)
			{
				if (NeedleProjectConfig.TryLoad(projectDirectory, out var config))
				{
					if (!string.IsNullOrWhiteSpace(config.buildDirectory))
					{
						var dir = projectDirectory + "/" + config.buildDirectory;
						if (Directory.Exists(dir))
						{
							Debug.Log("<b>Begin progressive</b> in " + dir);
							await ActionsCompression.MakeProgressive(dir);
							Debug.Log("<b>Begin compression</b> in " + dir);
							return await ActionsCompression.CompressFiles(dir);
						}

						Debug.LogError("Configured build directory does not exist. Please check the config file in your web project: \"" +
						               config.buildDirectory + "\"");
						return false;
					}
				}
			}

			return true;
		}
	}
}