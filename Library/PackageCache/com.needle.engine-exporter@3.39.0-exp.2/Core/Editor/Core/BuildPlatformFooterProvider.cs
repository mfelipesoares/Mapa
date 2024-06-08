using System;
using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Core
{
	internal class BuildPlatformToktx : INeedleBuildPlatformGUIProvider
	{
		private static DateTime lastTimeToktxVersionChecked;
#pragma warning disable 414
		private static bool toktxValid = true;
#pragma warning restore
		
		public void OnGUI(NeedleEngineBuildOptions buildOptions)
		{
			// if (NeedleEngineBuildOptions.DevelopmentBuild == false)
			// {
			// 	var now = DateTime.Now;
			// 	if (now - lastTimeToktxVersionChecked > TimeSpan.FromSeconds(10))
			// 	{
			// 		lastTimeToktxVersionChecked = now;
			// 		toktxValid = Actions.HasMinimumToktxVersionInstalled();
			// 	}
			// 	if (!toktxValid)
			// 	{
			// 		EditorGUILayout.HelpBox("Minimum recommended toktx version is not installed. Please download and install the recommend version for making production builds (or tick the Development Build checkbox above)", MessageType.Warning);
			// 		if (GUILayout.Button("Download toktx installer", GUILayout.Height(38)))
			// 		{
			// 			InternalActions.DownloadAndInstallToktx();
			// 		}
			// 		GUILayout.Space(16);
			// 	}
			// }
		}

	}
	
	[UsedImplicitly]
	internal class BuildPlatformFooterProvider : INeedleBuildPlatformFooterGUIProvider
	{
		
		public void OnGUI(NeedleEngineBuildOptions buildOptions)
		{
			using (new EditorGUI.DisabledScope(Actions.IsRunningBuildTask))
			{
				var exp = ExportInfo.Get();
				if (!exp)
				{
					if (GUILayout.Button("Add Needle Engine Integration to current Scene"))
					{
						Actions.SetupSceneForNeedleEngineExport();
					}
				}
				else
				{
					const float width = BuildPlatformConstants.LeftColumnWidth;
					
					if (GUILayout.Button(new GUIContent("Build", "Perform a build of your web app for deployment. The resulting files can then be uploaded to your webserver. You can also use any of our Deployment components listed above to directly build and deploy your scene to the web without any manual steps."), GUILayout.Width(width)))
					{
						MenuItems.BuildForDist(NeedleEngineBuildOptions.DevelopmentBuild ? BuildContext.Development : BuildContext.Production);
						GUIUtility.ExitGUI();
					}
					if (GUILayout.Button(new GUIContent("Preview Build", "Build & Start preview server. This does disable gzip compression to work with vite preview and can be used to preview a build on your local machine.\n\nRun \"Build\" to make a final build for deployment."), GUILayout.Width(width)))
					{
						PreviewBuild();
						GUIUtility.ExitGUI();
					}
				}
			}
		}

		private static async void PreviewBuild()
		{
			var gzip = UseGizp.Enabled;
			UseGizp.Enabled = false;
			var ctx = NeedleEngineBuildOptions.DevelopmentBuild
				? BuildContext.Development
				: BuildContext.Production;
			var res = await Actions.ExportAndBuild(ctx);
			UseGizp.Enabled = gzip;
			if (!res)
			{
				Debug.Log("Build failed, can not start preview server...");
				return;
			}
			var exp = ExportInfo.Get();
			if (!exp || !exp.Exists()) return;
			await WebProjectUtils.StartPreviewServer(Path.GetFullPath(exp.GetProjectDirectory()));
		}

		private static async void RunInBrowser()
		{
			await Builder.Build(false, BuildContext.LocalDevelopment);
			MenuItems.StartDevelopmentServer();
		}
	}
}