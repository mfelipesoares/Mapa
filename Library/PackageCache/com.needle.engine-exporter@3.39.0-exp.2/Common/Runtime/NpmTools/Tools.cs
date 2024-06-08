using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityGLTF;
#endif

namespace Needle.Engine
{
	internal static class Tools
	{
		public static bool IsUploadingToFTP => uploadingToFTPTask != null && !uploadingToFTPTask.IsCompleted;
		private static Task<bool> uploadingToFTPTask;
		public static Task<bool> UploadToFTP(string server,
			string username,
			string password,
			string localpath,
			string remotepath,
			bool sftp,
			bool delete,
			int port = -1,
			CancellationToken cancellationToken = default
			)
		{
			return uploadingToFTPTask = HiddenProject.Initialize().ContinueWith(res =>
			{
				var toolPath = HiddenProject.ToolsPath;
				if (Directory.Exists(toolPath))
				{
					var cmd = "npm run tool:upload-ftp --";
					cmd += " --server \"" + server + "\"";
					cmd += " --username \"" + username + "\"";
					cmd += " --password \"" + password + "\"";
					cmd += " --localpath \"" + localpath + "\"";
					cmd += " --remotepath \"" + remotepath + "\"";
					if (port >= 0)
					{
						cmd += " --port " + port;
					}
					if(sftp)
						cmd += " --sftp";
					if (delete)
						cmd += " --delete";
					return ProcessHelper.RunCommand(cmd, toolPath, null, true, true, -1, cancellationToken);
				}
				Debug.LogError("Tools directory does not exist at \"" + toolPath + "\"");
				return Task.FromResult(false);
			}, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}
		
		public static bool IsCloningRepository => CloneRepositoryTask != null && !CloneRepositoryTask.IsCompleted;

		private static Task<bool> CloneRepositoryTask;
		public static Task<bool> CloneRepository(string url, string targetDirectory)
		{
			return CloneRepositoryTask = HiddenProject.Initialize().ContinueWith(res =>
			{
				var toolPath = HiddenProject.ToolsPath;
				if (Directory.Exists(toolPath))
				{
					var cmd = "npm run tool:git-clone";
					cmd += " -- --url \"" + url + "\"";
					cmd += " --targetDir \"" + targetDirectory + "\"";
					return ProcessHelper.RunCommand(cmd, toolPath);
				}
				Debug.LogError("Tools directory does not exist at \"" + toolPath + "\"");
				return Task.FromResult(false);
			}, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}
		
		public static Task<bool> GenerateFonts(string fontPath, string targetDirectory, string charsetPath)
		{
			return HiddenProject.Initialize().ContinueWith(res =>
			{
				var toolPath = HiddenProject.ToolsPath;
				if (Directory.Exists(toolPath))
				{
					var cmd = "npm run tool:generate-font-atlas";
					cmd += $" -- --fontPath \"{fontPath}\"";
					cmd += $" --targetDirectory \"{targetDirectory}\"";
					cmd += $" --charset \"{charsetPath}\"";
					return ProcessHelper.RunCommand(cmd, toolPath);
				}
				Debug.LogError("Tools directory does not exist at \"" + toolPath + "\"");
				return Task.FromResult(false);
			}, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static async Task<bool> UploadBugReport(string pathToZipFile, string description)
		{
#if UNITY_EDITOR
			if (!await HiddenProject.Initialize()) return false;
			var packagePath = HiddenProject.ToolsPath;
			var userName = CloudProjectSettings.userName;
			if (!string.IsNullOrEmpty(CloudProjectSettings.organizationName))
				userName += "@" + CloudProjectSettings.organizationName;
			const int maxLength = 10000;
			if (description.Length > maxLength) description = description.Substring(0, maxLength);
			var encodedDescription = UnityWebRequest.EscapeURL(description); 
			var editorVersion = Application.unityVersion;
			var packageVersion = ProjectInfo.GetCurrentPackageVersion(Constants.UnityPackageName, out _);
			var cmd =
				$"npm run tool:upload-bugreport --" +
				$" --file \"{pathToZipFile}\"" +
				$" --source \"Unity {editorVersion}, {Constants.UnityPackageName}@{packageVersion}\"" +
				$" --user \"{userName}\"" +
				$" --description \"{encodedDescription}\"";
			var res = await ProcessHelper.RunCommand(cmd, packagePath);
			if (res)
			{
				return true;
			}
			return false;
#else
			await Task.Yield();
			return false;
#endif
		}

		public static async Task<bool> Transform(string fileOrDirectory)
		{
			var workingDirectory = BuildPipelinePath;
			if(workingDirectory == HiddenProject.BuildPipelinePath)
				await HiddenProject.Initialize();
			var cmd = "npm run transform \"" + fileOrDirectory + "\"";
			return await ProcessHelper.RunCommand(cmd, workingDirectory);
		}

		public static async Task<bool> Transform_Compress(string fileOrDirectory, string projectDirectory = null)
		{
			if (!Directory.Exists(fileOrDirectory) && !File.Exists(fileOrDirectory))
			{
				Debug.LogError(
					$"[{nameof(Tools)}.{nameof(Transform_Compress)}] Directory or file not found \"{fileOrDirectory}\", not compressing.");
				return false;
			}
			var workingDirectory = BuildPipelinePath;
			if(workingDirectory == HiddenProject.BuildPipelinePath)
				await HiddenProject.Initialize();
			var cmd = "npm run transform:pack-gltf \"" + fileOrDirectory + "\"";
			return await ProcessHelper.RunCommand(cmd, workingDirectory);
		}

		public static async Task<bool> Transform_Progressive(string file)
		{
			var workingDirectory = BuildPipelinePath;
			if(workingDirectory == HiddenProject.BuildPipelinePath)
				await HiddenProject.Initialize();
			var cmd = "npm run transform:make-progressive \"" + file + "\"";
			return await ProcessHelper.RunCommand(cmd, workingDirectory);
		}

		public static async Task ClearCaches()
		{
			var cmd = "npm run clear-caches";
			await ProcessHelper.RunCommand(cmd, BuildPipelinePath);
		}

		private static string BuildPipelinePath
		{
			get
			{
				var exportInfo = ExportInfo.Get();
				if (exportInfo)
				{
					var basePath = $"{Path.GetFullPath(exportInfo.GetProjectDirectory())}/node_modules/";
					var path = $"{basePath}/{Constants.GltfBuildPipelineNpmPackageName}";
					if (Directory.Exists(path))
					{
						NeedleDebug.Log(TracingScenario.Tools, "Found build pipeline at " + path);
						return path;
					}
					var pnpmDirectory = basePath + "/.pnpm";
					if(Directory.Exists(pnpmDirectory)) 
					{
						var pnpmVersions = Directory.GetDirectories(pnpmDirectory, Constants.GltfBuildPipelineNpmPackageName.Replace("/", "+") + "*", SearchOption.TopDirectoryOnly);
						foreach (var p in pnpmVersions)
						{
							var fullPath = p + "/node_modules/" + Constants.GltfBuildPipelineNpmPackageName;
							if (Directory.Exists(fullPath))
							{
								NeedleDebug.Log(TracingScenario.Tools, "Found build pipeline in pnpm directory at " + fullPath);
								return fullPath;
							}
						}
					}
				}
				NeedleDebug.Log(TracingScenario.Tools, "Use hidden tools build pipeline at" + HiddenProject.BuildPipelinePath);
				return HiddenProject.BuildPipelinePath;
			}
		}
	}
}