using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using Unity.SharpZipLib.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using StreamContent = System.Net.Http.StreamContent;

namespace Needle.Engine.Deployment
{
	[CustomEditor(typeof(DeployToNetlify))]
	public class DeployToNetlifyEditor : Editor
	{
		private ExportInfo exportInfo;
		private HttpClient client;
		private SerializedProperty siteNameField;
		private Task deploymentTask;

		private void OnEnable()
		{
			exportInfo = ExportInfo.Get();
			client = new HttpClient();
			siteNameField = serializedObject.FindProperty(nameof(Deployment.DeployToNetlify.siteName));
		}

		private void OnDisable()
		{
			client.Dispose();
		}

		public override void OnInspectorGUI()
		{
			var netlify = (DeployToNetlify)target;


			using (new EditorGUILayout.HorizontalScope())
			{
				if(Event.current.modifiers == EventModifiers.Alt)
					NetlifyAccessKey = EditorGUILayout.TextField("Access Key", NetlifyAccessKey);
				else
					NetlifyAccessKey = EditorGUILayout.PasswordField("Access Key", NetlifyAccessKey);
				if (GUILayout.Button(
					    new GUIContent("Create",
						    "Click this button to open Netlify's user settings where you can create a new personal access token.\nAfter creating the token copy and paste it into the Access Key field and you're ready to go!"),
					    GUILayout.Width(52)))
				{
					OpenNewPersonalAccessTokenSite();
				}
			}
			var hasAccessToken = !string.IsNullOrWhiteSpace(NetlifyAccessKey);

			using (new EditorGUI.DisabledScope(!hasAccessToken))
			{
				EditorGUILayout.PropertyField(siteNameField);
			}

			if (serializedObject.ApplyModifiedProperties())
			{
				if(TryParseSiteName(netlify.siteName, out var siteId))
					netlify.siteName = siteId;
			}

			var hasSite = !string.IsNullOrWhiteSpace(netlify.siteName);

			GUILayout.Space(5);

			var instructions = "Instructions:" +
			                   "\n1) Paste your Netlify personal access token into the AccessKey field" +
			                   "\n2) Click \"Create Site and Deploy\" to create a new Netlify site " +
			                   "\n - or enter an existing Netlify site id";

			if (!hasAccessToken)
			{
				EditorGUILayout.HelpBox(instructions, MessageType.None);
			}
			else
			{
				if (!hasSite)
					EditorGUILayout.HelpBox(instructions, MessageType.None);

				using (new EditorGUI.DisabledScope(!exportInfo))
				{
					var isDeploying = deploymentTask != null && !deploymentTask.IsCompleted;
					var isProduction = !NeedleEngineBuildOptions.DevelopmentBuild;
					if(Event.current.modifiers == EventModifiers.Alt) isProduction = !isProduction;
					
					using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(NetlifyAccessKey) || isDeploying))
					{
						if (!hasSite)
						{
							// If a user doesnt have a site yet we create it with a custom name and then deploy to it
							if (GUILayout.Button(new GUIContent("Create a New Website", "Clicking this button will create a new site on Netlify and deploy your current project to it!"), GUILayout.Height(32)))
							{
								deploymentTask = CreateANewSite(netlify).ContinueWith(res =>
								{
									if (res.Result)
									{
										// if the build directory already exists we just want to deploy the current state
										var deployOnly = NeedleProjectConfig.TryGetBuildDirectory(out _);
										return DeployToNetlify(netlify, netlify.siteName, deployOnly, isProduction);
									}
									return Task.CompletedTask;
								}, TaskScheduler.FromCurrentSynchronizationContext());
							}
						}
						else
						{
							using (new EditorGUILayout.HorizontalScope())
							{
								var label = "Build & Deploy";
								if(isProduction) label += " (Production)";
								else label += " (Development)";
								if (GUILayout.Button(new GUIContent(label, "Click this button to build and deploy a new version of your website to Netlify.\nHold ALT to toggle between making a Development (unoptimized) and a Production (compressed & optimized) build"), GUILayout.Height(32)))
								{
									deploymentTask = DeployToNetlify(netlify, netlify.siteName, false, isProduction);
								}
								// Deploy only is disabled if the dist directory does not exist
								var canDeployOnly = NeedleProjectConfig.TryGetBuildDirectory(out var dir);
								var isGzipped = false;
								if (canDeployOnly)
								{
									// if the file is gzipped we need to build again without compression
									// because netlify does handle that (and doesnt like it when we upload it compressed)
									if (File.Exists(dir + "/index.html.gz"))
									{
										canDeployOnly = false;
										isGzipped = true;
									}
								}
								using (new EditorGUI.DisabledScope(!canDeployOnly))
								{
									var tooltip = "";
									if(isGzipped) tooltip = "The last build was made with gzip enabled: Please click Build & Deploy to build again without gzip compression.";
									else tooltip = "Deploy the current build to Netlify";
									if (GUILayout.Button(new GUIContent("Deploy Only", tooltip), GUILayout.Height(32)))
									{
										deploymentTask = DeployToNetlify(netlify, netlify.siteName, true, isProduction);
									}
								}
							}
						}
					}

					using (new EditorGUI.DisabledScope(!hasSite))
					{
						var tooltip = "";
						if (hasSite) tooltip = "Open " + netlify.siteName + ".netlify.app in your browser";
						else tooltip = "Create a site first ↑";
						if (GUILayout.Button(new GUIContent("Open in Browser", tooltip), GUILayout.Height(32)))
						{
							var projectName = netlify.siteName;
							Application.OpenURL("https://" + projectName + ".netlify.app");
						}
					}
				}

				if (deploymentTask != null && deploymentTask?.IsCompleted == false)
				{
					EditorGUILayout.HelpBox("Deployment in progress... please wait", MessageType.Warning);
				}

				PollState();
				if (!string.IsNullOrEmpty(_lastDeployStateResponse))
				{
					GUILayout.Space(5);
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.HelpBox("Deploy State: " + _lastDeployStateResponse, MessageType.None);
						if (GUILayout.Button("View", GUILayout.Width(52)))
						{
							var url = $"https://app.netlify.com/sites/{netlify.siteName}/deploys/{LastDeployId}";
							Application.OpenURL(url);
						}
					}
				}
			}
		}

		private static string NetlifyAccessKey
		{
			get => EditorPrefs.GetString("DeployToNetlify.AccessKey", "");
			set => EditorPrefs.SetString("DeployToNetlify.AccessKey", value);
		}

		private static string LastDeployId
		{
			get => SessionState.GetString("DeployToNetlify.LastDeployId", "");
			set => SessionState.SetString("DeployToNetlify.LastDeployId", value);
		}

		private static void OpenNewPersonalAccessTokenSite()
		{
			Application.OpenURL(PersonalAccessTokenUrl);
		}

		private static bool TryParseSiteName(string input, out string name)
		{
			var regex = new Regex(@"https?\:\/{1,}(?<name>.+)\.netlify.app");
			var match = regex.Match(input);
			if (match.Success)
			{
				name = match.Groups["name"].Value;
				if (!string.IsNullOrEmpty(name))
				{
					return true;
				}
			}
			else
			{
				var regex2 = new Regex(@"app.netlify.com\/sites\/(?<name>.+?)\/");
				var match2 = regex2.Match(input);
				if (match2.Success)
				{
					name = match2.Groups["name"].Value;
					if (!string.IsNullOrEmpty(name))
					{
						return true;
					}
				}
			}
			name = null;
			return false;
		}
		
		private const string PersonalAccessTokenUrl = "https://app.netlify.com/user/applications/personal";

		private async Task<bool> CreateANewSite(DeployToNetlify component)
		{
			var sceneName = SceneManager.GetActiveScene().name;
			var siteId = "needle-engine-" + sceneName;
			siteId += "-" + DateTime.Now.Ticks;
			siteId = siteId.ToLower();
			var json = new JObject
			{
				{ "name", siteId },
				// { "password", true },
				{ "force_ssl", true },
			};
			var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
			// https://docs.netlify.com/api/get-started/#create-site
			var request = new HttpRequestMessage(HttpMethod.Post, "https://api.netlify.com/api/v1/sites")
			{
				Headers = { { "Authorization", "Bearer " + NetlifyAccessKey }, },
				Content = content,
			};
			var response = await client.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var responseString = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					var site = JObject.Parse(responseString);
					if (site.TryGetValue("name", out var id))
					{
						siteId = id.ToString();
					}
					component.siteName = siteId;
					return true;
				}
			}

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Debug.LogError(
					$"Your access token seems to be invalid: please enter a valid access key in the DeployToNetlify component. Go to {PersonalAccessTokenUrl.AsLink()} to create a new one.");
				return false;
			}

			Debug.LogError("Failed to create a new site: " + response.ReasonPhrase + "\n" + response.StatusCode);
			return false;
		}

		private async Task DeployToNetlify(DeployToNetlify component,
			string projectName,
			bool deployOnly,
			bool production,
			CancellationToken token = default)
		{
			if (!exportInfo) return;

			if (!deployOnly)
			{
				var context = BuildContext.Distribution(production);
				context.LiveUrl = "https://" + projectName + ".netlify.app";
				var previousGzipOption = UseGizp.Enabled;
				// netlify does handle that
				UseGizp.Enabled = false;
				try
				{
					if (!await Actions.ExportAndBuild(context))
					{
						Debug.LogError("Deployment failed because the build failed.");
						return;
					}
				}
				finally
				{
					UseGizp.Enabled = previousGzipOption;
				}
			}

			if (NeedleProjectConfig.TryGetBuildDirectory(out var dir))
			{
				if (!Directory.Exists(dir))
				{
					Debug.LogError("Build directory does not exist: " + dir);
					return;
				}

				var siteId = projectName;
				var isCreatingANewSite = string.IsNullOrWhiteSpace(siteId);

				string url;
				if (string.IsNullOrWhiteSpace(siteId))
				{
					url = "https://api.netlify.com/api/v1/sites";
					var sceneName = SceneManager.GetActiveScene().name;
					siteId = "needle-engine-" + sceneName + "-" + DateTime.Now.Ticks;
				}
				else
				{
					if (siteId.EndsWith("netlify.app"))
						url = $"https://api.netlify.com/api/v1/sites/{siteId}/deploys";
					else
						url = $"https://api.netlify.com/api/v1/sites/{siteId}.netlify.app/deploys";
				}

				var outputPath = Application.dataPath + "/../Temp/@website.zip";
				if (File.Exists(outputPath)) File.Delete(outputPath);
				ZipUtility.CompressFolderToZip(outputPath, null, dir);
				if (token.IsCancellationRequested) return;
				if (File.Exists(outputPath))
				{
					LastDeployId = "";
					// Docs: https://docs.netlify.com/api/get-started/#zip-file-method
					Debug.Log("Deploying to Netlify: " + siteId + ". Please wait...");
					using var deployClient = new HttpClient();
					// client.DefaultRequestHeaders.Add("Content-Type", "application/zip");
					deployClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + NetlifyAccessKey);
					// post with zip in body
					using var stream = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
					using var content = new StreamContent(stream);
					content.Headers.Add("Content-Type", "application/zip");
					var res = await deployClient.PostAsync(url, content, token);

					stream.Dispose();
					if (res.IsSuccessStatusCode)
					{
						var siteUrl = $"https://{siteId}.netlify.app";
						// if (File.Exists(outputPath)) File.Delete(outputPath);
						var response = await res.Content.ReadAsStringAsync();
						if (!string.IsNullOrEmpty(response))
						{
							var jObj = JObject.Parse(response);
							if (jObj.TryGetValue("deploy_id", out var deployId))
							{
								LastDeployId = deployId.ToString();
							}
							else if (jObj.TryGetValue("id", out var id))
							{
								LastDeployId = id.ToString();
							}
							if (jObj.TryGetValue("subdomain", out var subdomain))
							{
								component.siteName = subdomain.ToString();
								siteUrl = $"https://{subdomain}.netlify.app";
							}
						}
						if (isCreatingANewSite)
						{
							Debug.Log(
								$"<b>Successfully created new site on Netlify!</b> {siteUrl.AsLink()}\n{res.StatusCode}");
						}
						else
							Debug.Log($"<b>Successfully deployed to Netlify!</b> {siteUrl.AsLink()}\n{res.StatusCode}");

						Debug.Log("Waiting for site to be ready...");
						_isWaitingForSiteToBeCreated = true;
						SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Successfully deployed to Netlify! Waiting for site to be ready..."));
					}
					else
					{
						if (res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.NotFound)
						{
							SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Failed to deploy to Netlify! See console for details."));
							Debug.LogError(
								$"<b>Failed to deploy to Netlify!</b>: Please make sure you entered a valid Access Key and you own {$"https://{siteId}.netlify.app".AsLink()}.\nYou can create a new access token at {PersonalAccessTokenUrl.AsLink()}");
						}
						else
						{
							Debug.LogError(
								$"<b>Failed to deploy to Netlify!</b>: {res.ReasonPhrase}\n{res.RequestMessage}");
						}
					}
				}
			}
			else
			{
				Debug.LogError("Failed to find build directory: " + dir);
			}
		}

		private static DateTime _lastPollTime;
		private static string _lastDeployStateResponse;

		private static bool _isWaitingForSiteToBeCreated
		{
			get => SessionState.GetBool("DeployToNetlify.IsWaitingForSiteToBeCreated", false);
			set => SessionState.SetBool("DeployToNetlify.IsWaitingForSiteToBeCreated", value);
		}

		private static async void PollState()
		{
			if (string.IsNullOrEmpty(LastDeployId)) return;
			if (DateTime.Now - _lastPollTime < TimeSpan.FromSeconds(5)) return;
			_lastPollTime = DateTime.Now;
			using var pollClient = new HttpClient();
			pollClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + NetlifyAccessKey);
			var res = await pollClient.GetAsync($"https://api.netlify.com/api/v1/deploys/{LastDeployId}");
			if (res.IsSuccessStatusCode)
			{
				var response = await res.Content.ReadAsStringAsync();
				if (!string.IsNullOrEmpty(response))
				{
					var jObj = JObject.Parse(response);
					if (jObj.TryGetValue("state", out var state))
					{
						_lastDeployStateResponse = state.ToString();
						if (_isWaitingForSiteToBeCreated && state.ToString() == "ready")
						{
							_isWaitingForSiteToBeCreated = false;
							if (jObj.TryGetValue("ssl_url", out var url))
							{
								Debug.Log("Site is ready: " + url);
								await Task.Delay(1000);
								Application.OpenURL(url.ToString());
							}
						}
					}
					if (jObj.TryGetValue("updated_at", out var time))
					{
						_lastDeployStateResponse += " " + time;
					}
				}
			}
			else
			{
				_lastDeployStateResponse = null;
			}
		}
	}
}