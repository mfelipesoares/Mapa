using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal static class GitActions
	{
		public static bool IsCloneable(string url)
		{
			if (url == null) return false;
			return url.StartsWith("http") && (url.Contains("github") || url.Contains("gitlab") ||
			                                  url.EndsWith(".git"));
		}

		public static string GetRepositoryName(string url)
		{
			return Path.GetFileNameWithoutExtension(url);
		}
		
		private static string LastSelectedPath
		{
			get => SessionState.GetString("NeedleEngineCloneRepository",
					Path.GetFullPath(Application.dataPath + "/../"));
			set => SessionState.SetString("NeedleEngineCloneRepository", value);
		}

		public static Task<(bool success, string localPath)> CloneProject(string url, string suggestedTargetDirectory)
		{
			(bool success, string localPath) res = (false, null);
			if (IsCloneable(url) == false)
			{
				return Task.FromResult(res);
			}
			string targetDirectory = default;
			if (!string.IsNullOrWhiteSpace(suggestedTargetDirectory))
			{
				targetDirectory = Path.GetFullPath(suggestedTargetDirectory);
			}
			if(targetDirectory == null)
			{
				targetDirectory = EditorUtility.OpenFolderPanel("Select Project Directory", LastSelectedPath, "");
			}

			if (targetDirectory != null)
			{
				LastSelectedPath = targetDirectory;
				SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Cloning remote project..."));
				var expectedDirectory = targetDirectory + "/" + GetRepositoryName(url);
				return Tools.CloneRepository(url, targetDirectory).ContinueWith(cloneSucceeded =>
				{
					if(Directory.Exists(expectedDirectory)) targetDirectory = expectedDirectory;
					if (TryFindNeedleProjectDirectory(targetDirectory, out var needleDir))
					{
						targetDirectory = needleDir;
					}
					return (cloneSucceeded.Result, targetDirectory);
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
			
			return Task.FromResult(res);
		}


		public static Task<bool> CloneProject(ExportInfo exportInfo)
		{
			var url = exportInfo.RemoteUrl;
			if (!IsCloneable(url)) return Task.FromResult(false);
			string targetDirectory = default;
			if (EditorUtils.AreTestsRunning())
			{
				targetDirectory = Path.GetFullPath("Temp/Needle.Tests");
				Debug.Log("Tests are running: cloning project into TEMP directory at " + Path.GetFullPath(targetDirectory).AsLink());
				if (Directory.Exists(targetDirectory)) FileUtils.DeleteDirectoryRecursive(targetDirectory);
				Directory.CreateDirectory(targetDirectory);
			}
			else if (!string.IsNullOrWhiteSpace(exportInfo.DirectoryName) && !IsCloneable(exportInfo.DirectoryName))
			{
				var path = exportInfo.GetProjectDirectory();
				var isEmpty = Directory.Exists(path)== false || Directory.GetFiles(path).Length == 0;
				if (isEmpty) targetDirectory = path;
			}
			else
			{
				targetDirectory = EditorUtility.OpenFolderPanel("Select Project Directory", LastSelectedPath, "");
			}
			if (!string.IsNullOrEmpty(targetDirectory))
			{
				LastSelectedPath = targetDirectory;
				var expectedDirectory = targetDirectory + "/" + GetRepositoryName(url);
				Debug.Log("Setup project at " + expectedDirectory);
				if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Cloning remote project..."));
				return Tools.CloneRepository(url, targetDirectory).ContinueWith(res =>
				{
					if (res.Result)
					{
						if(Directory.Exists(expectedDirectory)) targetDirectory = expectedDirectory;
						if (TryFindNeedleProjectDirectory(targetDirectory, out var needleDir))
						{
							targetDirectory = needleDir;
						}
						if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Successfully cloned project..."));
						Debug.Log($"{"<b>Successfully</b>".AsSuccess()} pulled {url.AsLink()} into {targetDirectory.AsLink()}", exportInfo);
						// ExportInfo might be "missing" when running tests
						if(exportInfo) Undo.RecordObject(exportInfo, "Change git url to local path");
						exportInfo.DirectoryName = PathUtils.MakeProjectRelative(targetDirectory);
						return true;
					}
					return false;
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
			
			return Task.FromResult(false);
		}

		/// <summary>
		/// Searches for a needle.config.json in the provided directory
		/// </summary>
		private static bool TryFindNeedleProjectDirectory(string dir, out string needleProjectDir)
		{
			var dirs = Directory.GetFiles(dir, "needle.config.json", SearchOption.AllDirectories);
			needleProjectDir = dirs.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(needleProjectDir))
			{
				needleProjectDir = Path.GetDirectoryName(needleProjectDir);
				return true;
			}
			return false;
		}
	}
}