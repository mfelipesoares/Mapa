using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal static class ActionsHelperPackage
	{
		[MenuItem(Constants.MenuItemRoot + "/Internal/Update @types-three")]
		private static async void UpdateThreeTypesMenuItem()
		{
			var project = ExportInfo.Get();
			if (!project || !project.Exists()) return;
			await UpdateThreeTypes(project.GetProjectDirectory());
		}
		
		internal static Task UpdateThreeTypes(string directory)
		{
			var packagePath = Path.GetFullPath(directory) + "/node_modules/@needle-tools/engine/node_modules/@needle-tools/helper";
			if (!Directory.Exists(packagePath))
				return Task.CompletedTask;
			var cmd = "npm run tool:update-types-three \"" + directory + "\"";
			return ProcessHelper.RunCommand(cmd, packagePath);
		}

		internal static bool NeedsWebProjectForBugReport() => false;

		internal static async Task<bool> UploadBugReport(string zipPath, string description)
		{
			while (true)
			{
				Debug.Log("<b>BugReport</b>: Begin uploading...");
				var res = await Tools.UploadBugReport(zipPath, description);
				if (res)
				{
					Debug.Log("<b>BugReport</b>: Upload finished...");
					return true;
				}
				Debug.LogError("<b>BugReport</b>: Upload failed. Please see logs above for more information.");
				await Task.Delay(1000);
				if (EditorUtility.DisplayDialog("Bug Reporter", "Uploading the BugReport failed. Do you want to retry?", "Yes, try upload again", "No, I will send the files manually"))
				{
					continue;
				}
				Debug.Log("Please upload the zip at " + zipPath.AsLink() + " and send a link to Needle");
				return false;
			}
		}
	}
}