using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Needle.Engine.Projects
{
	internal static class TemporaryProjectHandler
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorSceneManager.sceneClosing += OnClosingScene;
		}

		private static void OnClosingScene(Scene scene, bool removeScene)
		{
			// CleanTemporaryProject();
		}

		/*
		 * This has a few problems:
		 * - if the server is still running we dont want to delete it
		 * - if we delete the node_modules folder the ProjectGenerator will re-create the project from the template files and override the package.json which we installed e.g. the EditorSync package to (because it's an optional dependency)
		 * 
		 */
		// private static async void CleanTemporaryProject()
		// {
		// 	var exportInfo = ExportInfo.Get();
		// 	if (exportInfo && exportInfo.IsValidDirectory() && exportInfo.IsTempProject())
		// 	{
		// 		var dir = Path.GetFullPath(exportInfo.DirectoryName) + "/node_modules";
		// 		if (Directory.Exists(dir))
		// 			await FileUtils.DeleteDirectoryRecursive(dir);
		// 	}
		// }
	}
}