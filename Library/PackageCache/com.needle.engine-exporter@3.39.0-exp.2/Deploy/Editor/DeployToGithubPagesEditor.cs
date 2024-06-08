using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	[CustomEditor(typeof(DeployToGithubPages))]
	public class DeployToGithubPagesEditor : Editor
	{
		private SerializedProperty repositoryUrl;
		private Task<bool> deploymentTask;

		private void OnEnable()
		{
			repositoryUrl = serializedObject.FindProperty(nameof(DeployToGithubPages.repositoryUrl));
		}

		public override void OnInspectorGUI()
		{
			var t = (DeployToGithubPages)target;
			const int buttonHeight = 32;

			using (var change = new EditorGUI.ChangeCheckScope())
			{
				EditorGUILayout.PropertyField(repositoryUrl);
				if (change.changed)
				{
					var url = repositoryUrl.stringValue;
					var paramsIndex = url.IndexOf('?');
					if (paramsIndex > 0)
					{
						repositoryUrl.stringValue = url.Substring(0, paramsIndex);
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
			
			
			GUILayout.Space(2);

			var hasValidUrl = true;
			var isCurrentlyDeploying = deploymentTask != null && !deploymentTask.IsCompleted;

			if (string.IsNullOrEmpty(t.repositoryUrl))
			{
				hasValidUrl = false;
				EditorGUILayout.HelpBox(
					"No repository URL assigned. Please assign a repository URL on github.com to deploy to.",
					MessageType.Warning);
				// if(GUILayout.Button("Review github pages terms",GUILayout.Height(buttonHeight)))
				// 	Application.OpenURL("https://docs.github.com/en/pages/getting-started-with-github-pages/about-github-pages#limits-on-use-of-github-pages");
			}
			else if (!t.repositoryUrl.Contains("github.com") && !t.repositoryUrl.Contains("github.io"))
			{
				hasValidUrl = false;
				EditorGUILayout.HelpBox(
					"The url does not seem to be a valid Github Pages or repository url. Please make sure the provided is either a url to a repository on github.com or a github pages url (containing github.io)",
					MessageType.Warning);
			}
			else
			{
				using(new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox(GetGithubPagesURL(t.repositoryUrl), MessageType.None);
					if(GUILayout.Button("Copy", GUILayout.Width(50)))
						EditorGUIUtility.systemCopyBuffer = GetGithubPagesURL(t.repositoryUrl); 
				};
			}

			GUILayout.Space(5);
			using (new EditorGUI.DisabledScope(!hasValidUrl || isCurrentlyDeploying))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					var hasOutputDirectory = NeedleProjectConfig.TryGetBuildDirectory(out var dir);
					var devBuild = NeedleEngineBuildOptions.DevelopmentBuild;
					if (Event.current.modifiers == EventModifiers.Alt) devBuild = !devBuild;
					var str = "Build & Deploy";
					str += devBuild ? ": Dev" : ": Prod";
					if (GUILayout.Button(new GUIContent(str, Assets.GithubPageDeployLogo, "Hold ALT to toggle between a production and a development build"), GUILayout.Height(buttonHeight)))
					{
						deploymentTask = Deploy(dir, t.repositoryUrl, true, !devBuild);
					}

					using (new EditorGUI.DisabledScope(!hasOutputDirectory))
					{
						if (GUILayout.Button(new GUIContent("Deploy Only", Assets.GithubPageDeployLogo), GUILayout.Height(buttonHeight)))
						{
							deploymentTask = Deploy(dir, t.repositoryUrl, false, false);
						}
					}
				}
			}

			using (new EditorGUI.DisabledScope(!hasValidUrl))
			{
				if (GUILayout.Button("Open in Browser", GUILayout.Height(buttonHeight)))
				{
					var url = GetGithubPagesURL(t.repositoryUrl);
					if (url != null) Application.OpenURL(url);
				}	
			}
		}

		private static async Task<bool> Deploy(string directory, string repository, bool build, bool productionBuild)
		{
			var liveUrl = GetGithubPagesURL(repository);
			if (build)
			{
				var gzipSetting = UseGizp.Enabled;
				try
				{
					UseGizp.Enabled = false;
					var ctx = BuildContext.Distribution(productionBuild);
					ctx.LiveUrl = liveUrl;
					var buildRes = await Actions.ExportAndBuild(ctx);
					if (!buildRes)
						return false;
				}
				finally
				{
					UseGizp.Enabled = gzipSetting;
				}
			}
			
			Debug.Log("<b>Begin deployment</b> to github pages " + repository);
			if (!await HiddenProject.Initialize())
			{
				Debug.LogError("Failed initializing npm tools");
				return false;
			}

			var cmd =
				$"npm run tool:publish-to-github-pages -- --directory \"{directory}\" --repository \"{repository}\"";
			var workingDirectory = HiddenProject.ToolsPath;
			var res = await ProcessHelper.RunCommand(cmd, workingDirectory);
			if (res)
			{
				Debug.Log($"{"<b>Successfully</b>".AsSuccess()} deployed {liveUrl.AsLink()} (it might take a few minutes for github to update)");
			}
			else
			{
				Debug.Log($"{"<b>Failed</b>".AsError()} to deploy to github pages, see errors above\n{repository}");
			}

			return res;
		}

		private static string GetGithubPagesURL(string url)
		{
			if (url.Contains("github.io")) return url;
			if (url.Contains("github.com"))
			{
				var regex = new Regex("github\\.com[:/](?<user>[^/]+)/(?<repo>[^/]+)");
				var match = regex.Match(url);
				if (match.Success)
				{
					var user = match.Groups["user"].Value;
					var repo = match.Groups["repo"].Value;
					if(repo.EndsWith(".git")) repo = repo.Substring(0, repo.Length - 4);
					return $"https://{user}.github.io/{repo}";
				}
			}
			return null;
		}
	}
}