using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Needle.Engine.Deployment
{
	[CustomEditor(typeof(DeployToFTP))]
	public class DeployToFTPEditor : Editor
	{
		private IProjectInfo project;
		private SerializedProperty settings, path, useGzipCompression, overrideGzipCompression;

		private void OnEnable()
		{
			project = ObjectUtils.FindObjectOfType<IProjectInfo>();
			path = serializedObject.FindProperty(nameof(DeployToFTP.Path));
			settings = serializedObject.FindProperty(nameof(DeployToFTP.FTPServer));
			useGzipCompression = serializedObject.FindProperty(nameof(DeployToFTP.UseGzipCompression));
			overrideGzipCompression = serializedObject.FindProperty(nameof(DeployToFTP.OverrideGzipCompression));
		}

		public override void OnInspectorGUI()
		{
			if (project == null)
			{
				EditorGUILayout.HelpBox("No project found - please add a " + nameof(ExportInfo) + " component to you scene", MessageType.Warning);
				return;
			}

			var ftp = target as DeployToFTP;
			if (!ftp)
			{
				base.OnInspectorGUI();
				return;
			}


			ftp.Path = ftp.Path?.TrimStart('.');
			if (string.IsNullOrWhiteSpace(ftp.Path)) ftp.Path = "/";

			var change = new EditorGUI.ChangeCheckScope();

			EditorGUILayout.LabelField("FTP", EditorStyles.boldLabel);

			using (new GUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(settings, new GUIContent("Server"));
				if (!settings.objectReferenceValue)
				{
					if (GUILayout.Button("Create", GUILayout.Width(50)))
					{
						var instance = CreateInstance<FTPServer>();
						AssetDatabase.CreateAsset(instance, "Assets/FTPServer.asset");
						settings.objectReferenceValue = instance;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}

			var server = ftp.FTPServer;
			string key = default;
			if (server)
				server.TryGetKey(out key);
			var password = SecretsHelper.GetSecret(key);

			var hasInvalidDeploymentPath = (server && !server.AllowTopLevelDeployment) &&
			                             (string.IsNullOrWhiteSpace(path.stringValue) ||
			                              path.stringValue.Trim() == "/");

			if (server)
			{
				EditorGUILayout.PropertyField(path, new GUIContent("Path", "The path on the ftp server where you want to deploy your website to."));

				if (hasInvalidDeploymentPath)
				{
					EditorGUILayout.HelpBox("Deployment to the top level directory is not allowed. Please specify a subfolder path.", MessageType.Warning);
				}
				
				using (new EditorGUILayout.HorizontalScope())
				{
					UseGizp.Enabled = EditorGUILayout.Toggle(new GUIContent("Use Gzip", "Enable gzip compression for the files. Make sure your server supports gzip compression."), UseGizp.Enabled);
				}
			}

			if (change.changed)
			{
				serializedObject.ApplyModifiedProperties();
			}

			var hasPassword = !string.IsNullOrWhiteSpace(password);
			NeedleProjectConfig.TryGetBuildDirectory(out var directory);
			var canDeploy = server && hasPassword;


			if (server)
			{
				if (string.IsNullOrWhiteSpace(server.Servername) || string.IsNullOrWhiteSpace(server.Username))
				{
					EditorGUILayout.Space(2);
					EditorGUILayout.HelpBox(
						"Please enter your FTP server, username and password to the FTP server settings. You can get this information from your web provider. Don't worry: your password is not saved with the project and will not be shared.",
						MessageType.Warning);
				}
			}
			else
			{
				EditorGUILayout.Space(2);
				EditorGUILayout.HelpBox("Assign or create a FTP server settings object", MessageType.Info);
			}

			EditorGUILayout.Space(5);

			if (!canDeploy)
			{
				if (!hasPassword && server)
					EditorGUILayout.HelpBox("Server configuration is missing a password", MessageType.None);
			}

			var isDeploying = currentTask != null && !currentTask.IsCompleted;
			using (new EditorGUI.DisabledScope(!canDeploy || hasInvalidDeploymentPath))
			{
				using(new EditorGUI.DisabledScope(isDeploying))
				using (new GUILayout.HorizontalScope())
				{
					var devBuild = NeedleEngineBuildOptions.DevelopmentBuild;
					if ((Event.current.modifiers & EventModifiers.Alt) != 0) devBuild = !devBuild;

					if (GUILayout.Button("Build & Deploy: " + (devBuild ? "Dev" : "Prod"), GUILayout.Height(30)))
					{
						RunDeployment(target as DeployToFTP, true, devBuild);
					}

					using (new EditorGUI.DisabledScope(!Directory.Exists(directory)))
					{
						if (GUILayout.Button("Deploy Only", GUILayout.Height(30)))
						{
							RunDeployment(target as DeployToFTP,false, devBuild);
						}
					}
				}
			}

			var hasRemoteUrl = server && server.RemoteUrlIsValid;
			if (hasRemoteUrl)
			{
				var fullUrl = server.GetUrl(ftp.Path);
				// using (new EditorGUI.DisabledScope(!hasRemoteUrl))
				if (GUILayout.Button(new GUIContent("Open in Browser " + Constants.ExternalLinkChar, fullUrl), GUILayout.Height(30)))
				{
					Application.OpenURL(fullUrl);
				}
			}
			
			if (isDeploying)
			{
				EditorGUILayout.HelpBox("Deployment to FTP is in progress...", MessageType.None);	
			}
		}

		private static Task<bool> currentTask;
		private static CancellationTokenSource cancel;

		public static async void RunDeployment(DeployToFTP comp, bool exportScene, bool devBuild)
		{
			var projectInfo = ObjectUtils.FindObjectOfType<IProjectInfo>();
			var server = comp.FTPServer;
			var remotePath = comp.Path;
			var username = server.Username;
			server.TryGetKey(out var key);
			var password = SecretsHelper.GetSecret(key);
			var sftp = server.SFTP;
			var port = server.Port;

			Debug.Log("Begin uploading...");

			cancel?.Cancel();
			if (currentTask != null && currentTask.IsCompleted == false)
			{
				await currentTask;
				return;
			}
			const int maxUploadDurationInMilliseconds = 10 * 60 * 1000;
			cancel = new CancellationTokenSource(maxUploadDurationInMilliseconds);

			var progId = Progress.Start("FTP Upload", "", Progress.Options.Managed);
			Progress.RegisterCancelCallback(progId, () =>
			{
				if (!cancel.IsCancellationRequested)
				{
					Debug.Log("Cancelling FTP upload...");
					cancel.Cancel();
				}
				return true;
			});

			BuildContext buildContext;
			if (exportScene) buildContext = BuildContext.Distribution(!devBuild);
			else buildContext = BuildContext.PrepareDeploy;

			if (server.RemoteUrlIsValid)
			{
				var baseUrl = server.RemoteUrl;
				// ensure we don't have double slashes (while keeping https:// intact)
				while(baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
				while (remotePath.StartsWith("/")) remotePath = remotePath.Substring(1);
				while(remotePath.Contains("//")) remotePath = remotePath.Replace("//", "/");
				buildContext.LiveUrl = baseUrl + "/" + remotePath;
			}

			var distDirectory = projectInfo.ProjectDirectory + "/dist";
			if (NeedleProjectConfig.TryGetBuildDirectory(out var dir))
			{
				distDirectory = dir;
			}
			var buildResult = false;
			var postBuildMessage = default(string);
			if (exportScene)
			{
				Progress.SetDescription(progId, "Export and Build");
				var dev = NeedleEngineBuildOptions.DevelopmentBuild;
				Debug.Log("<b>Begin building distribution</b>");
				currentTask = Actions.ExportAndBuild(buildContext);
				buildResult = await currentTask;
				postBuildMessage = "<b>Successfully built distribution</b>";
			}
			else
			{
				currentTask = Actions.ExportAndBuild(buildContext);
				buildResult = await currentTask;
			}

			if (cancel.IsCancellationRequested)
			{
				Debug.LogWarning("Upload cancelled");
				return;
			}
			if (!buildResult)
			{
				Debug.LogError("Build failed, aborting FTP upload - see console for errors");
				return;
			}
			if (postBuildMessage != null) Debug.Log(postBuildMessage);

			Debug.Log("<b>Begin uploading</b> " + distDirectory);
			Progress.SetDescription(progId, "Upload " + Path.GetDirectoryName(projectInfo.ProjectDirectory) + " to FTP");
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Begin Upload..."), 5);
			
			currentTask = Tools.UploadToFTP(server.Servername, username, password, distDirectory, remotePath, sftp, false, port, cancel.Token);
				
			// currentTask = UploadDirectory(distDirectory, opts);
			var uploadResult = await currentTask;
			if (cancel.Token.IsCancellationRequested)
				Debug.LogWarning("<b>FTP upload was cancelled</b>");
			else if (uploadResult)
			{
				if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Upload succeeded"), 5);
				Debug.Log($"<b>FTP upload {"succeeded".AsSuccess()}</b> " + distDirectory);
				if (!string.IsNullOrWhiteSpace(buildContext.LiveUrl))
				{
					Application.OpenURL(buildContext.LiveUrl);
				}
			}
			else Debug.LogError("Uploading failed. Please see console for errors.\n" + distDirectory);
			if (Progress.Exists(progId))
				Progress.Finish(progId);
		}
	}
}