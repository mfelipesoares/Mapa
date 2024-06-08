using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Editors;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;

namespace Needle.Engine
{
	internal static class Analytics
	{
#if NEEDLE_ENGINE_DEV
		[UnityEditor.MenuItem(Constants.MenuItemRoot + "/Internal/Analytics/Register Installation")]
		private static void InternalRegisterInstallation() => RegisterInstallation();

		[UnityEditor.MenuItem(Constants.MenuItemRoot + "/Internal/Analytics/Register New Project")]
		private static void InternalRegisterNewProject() =>
			RegisterNewProject("Analytics Debug Project", "Analytics Project Template");

		[UnityEditor.MenuItem(Constants.MenuItemRoot + "/Internal/Analytics/Register New Deployment")]
		private static void InternalRegisterNewDeployment() =>
			AnalyticsHelper.SendDeploy("https://test.com", true);
#endif

		public static void RegisterInstallation()
		{
			var endpoint = "/api/v2/new/installation";
			var model = new NewInstallationModel();
			Send(model, endpoint);
		}

		public static void RegisterNewProject(string projectName, string templateName)
		{
			var endpoint = "/api/v2/new/project";
			projectName = UserCreatedProjectFromTemplateModel.AnonymizeProjectName(projectName);
			var model = new UserCreatedProjectFromTemplateModel(projectName, templateName);
			Send(model, endpoint);
		}

		private static async void Send(object model, string url)
		{
			await AnalyticsHelper.Api.SendPost(model, url);
		}


		[InitializeOnLoadMethod]
		private static async void EditorStart()
		{
			await Task.Delay(5000);
#if !NEEDLE_ENGINE_DEV
			if (SessionState.GetBool("NeedleEngineEditorStarted", false)) return;
#endif
			while (EditorApplication.isUpdating || EditorApplication.isCompiling) await Task.Delay(1000);
			while (EulaWindow.RequiresEulaAcceptance) await Task.Delay(1000);
			
			// give it a bit of time to get the user name
			for (var i = 0; i < 10; i++)
			{
				var name = CloudProjectSettings.userName;
				if(name == "anonymous" || string.IsNullOrEmpty(name)) await Task.Delay(1000);
				else break;
			}
			
			SessionState.SetBool("NeedleEngineEditorStarted", true);
			var model = new NewInstallationModel();
			Send(model, "/api/v2/new/editor_start"); 
		}

		private struct AssetFileInfo
		{
			public double size;
			public int count;
		}

		[InitializeOnLoadMethod]
		private static void RegisterBuild()
		{
			try
			{
				var startTime = System.DateTime.Now;
				Builder.BuildStarting += () => { startTime = System.DateTime.Now; };

				Builder.BuildEnded += () =>
				{
					try
					{
						if (NeedleProjectConfig.TryGetAssetsDirectory(out var dir) && Directory.Exists(dir))
						{
							var duration = System.DateTime.Now - startTime;

							var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
							var details = new Dictionary<string, object>();
							var totalSize = 0L;
							foreach (var file in files)
							{
								var ext = Path.GetExtension(file);
								var size = new FileInfo(file).Length;
								totalSize += size;
								if (details.ContainsKey(ext))
								{
									var entry = (AssetFileInfo)details[ext];
									entry.size += size;
									entry.count++;
									details[ext] = entry;
								}
								else
									details.Add(ext, new AssetFileInfo { size = size, count = 1 });
							}

							var model = new NewExportModel();
							var projectDir = TryFindProjectDirectory(new DirectoryInfo(dir));
							if (projectDir != null)
							{
								model.projectPath = projectDir.FullName;
								model.projectName = projectDir.Name;
							}
							model.buildDuration = duration.TotalSeconds;
							model.totalFilesCount = files.Length;
							model.totalFilesSize = totalSize / 1024f / 1024f;
							model.details = JsonConvert.SerializeObject(details);
							Send(model, "/api/v2/new/export");
						}
					}
					catch (Exception)
					{
						// TODO: send exceptions to backend
					}
				};

				DirectoryInfo TryFindProjectDirectory(DirectoryInfo dir)
				{
					if (dir == null) return null;
					if (File.Exists(dir.FullName + "/package.json"))
					{
						return dir;
					}
					return TryFindProjectDirectory(dir.Parent);
				}
			}
			catch
			{
				// ignore
			}
		}
	}
}