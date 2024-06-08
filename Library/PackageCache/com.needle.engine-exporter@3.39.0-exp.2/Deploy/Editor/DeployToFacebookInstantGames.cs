using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using Unity.SharpZipLib.GZip;
using Unity.SharpZipLib.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	[UsedImplicitly]
	public class FacebookInstantGamesMeta : IBuildConfigProperty
	{
		public static bool Enabled = false;
		
		public string Key => "facebookInstantGames";

		public object GetValue(string projectDirectory)
		{
			if (!Enabled) return null;
			var jObj = new JObject();
			return jObj;
		}
	}

	public static class DeployToFacebookGamesRunner
	{
		public static async Task Build()
		{
			var exportInfo = ExportInfo.Get();
			if (!exportInfo)
			{
				Debug.LogError("Scene must contain Needle web project with a valid path but no " + nameof(ExportInfo) +
				               " component was found.");
				return;
			}
			var projectDir = exportInfo.GetProjectDirectory();
			var gzipWasEnabled = UseGizp.Enabled;
			try
			{
				UseGizp.Enabled = false;
				FacebookInstantGamesMeta.Enabled = true;
				var ctx = BuildContext.Production;
				ctx.AllowShowFolderAfterBuild = false;
				var build = Actions.ExportAndBuild(ctx);
				var res = await build;
				if (res)
				{
					var buildDirectory = ActionsBuild.GetBuildOutputDirectory(projectDir);
					var outputDir = Path.GetFullPath(Application.dataPath + "/../Temp/Needle/facebook-instant-games");
					Directory.CreateDirectory(outputDir);
					var outputPath = outputDir + "/" + new DirectoryInfo(projectDir).Name + ".zip";
					ZipUtility.CompressFolderToZip(outputPath, null, buildDirectory);
					EditorUtility.RevealInFinder(outputPath);
					Debug.Log(
						$"<b>{"Successfully".AsSuccess()} built zip for facebook instant games</b>. You can upload this zip to your app now: {outputPath.AsLink()}");
				}
			}
			finally
			{
				FacebookInstantGamesMeta.Enabled = false;
				UseGizp.Enabled = gzipWasEnabled;
			}
		}
	}

	[CustomEditor(typeof(DeployToFacebookInstantGames))]
	public class DeployToFacebookInstantGamesEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Build a Facebook Instant Games ready ZIP file that can be uploaded to your app. You can create a app on https://developers.facebook.com/apps", MessageType.None);
			using (new EditorGUI.DisabledScope(Actions.IsRunningBuildTask))
			{
				if (GUILayout.Button("Build for Instant Games", GUILayout.Height(32)))
				{
					Build();
					async void Build() => await DeployToFacebookGamesRunner.Build();
				}
			}
		}
	}
}