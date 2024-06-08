using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Server;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.EditorSync
{
	[CustomEditor(typeof(NeedleEditorSync))]
	internal class NeedleEditorSyncEditor : UnityEditor.Editor
	{
		private static bool pluginIsInstalled;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			Connection.Instance.Message += OnMessage;
		}

		private static void OnMessage(RawMessage obj)
		{
			if (obj.type == "needle:editor-sync:installation-status" && obj.data != null)
			{
				pluginIsInstalled = (bool)obj.data;
			}
		}


		private ExportInfo exportInfo;
		private Dictionary<string, string> dependencies;
		private string packageJsonPath, installationPath;
		private DateTime lastUpdateTime;


		private async void OnEnable()
		{
			UpdateState();
			var comp = target as NeedleEditorSync;
			if (comp && comp.enabled && !pluginIsInstalled)
			{
				if (EditorSyncActions.CheckIsInstalled())
				{
					EditorSyncActions.RequestSoftServerRestart();
					await Task.Delay(100);
					EditorSyncActions.SendEditorSyncEnabledStatusUpdate(comp.enabled);
				}
			}
		}

		private void UpdateState()
		{
			lastUpdateTime = DateTime.Now;
			exportInfo = ExportInfo.Get();
			if (exportInfo)
			{
				packageJsonPath = exportInfo.GetProjectDirectory() + "/package.json";
				PackageUtils.TryReadDependencies(packageJsonPath, out dependencies, EditorSyncActions.packageJsonKey);
				installationPath = Path.GetFullPath(exportInfo.GetProjectDirectory()) +
				                   "/node_modules/" + Constants.PackageName;
			}
		}

		private static readonly GUILayoutOption[] bigButton = new[] { GUILayout.Height(32) };
		private static DateTime _lastPingTime = DateTime.Now;

		public override void OnInspectorGUI()
		{
			if (Connection.Instance.IsConnected)
			{
				if (DateTime.Now - _lastPingTime > TimeSpan.FromSeconds(3))
				{
					_lastPingTime = DateTime.Now;
					Task.Run(() => Connection.Instance.SendRaw("ping"));
				}
			}

			if (DateTime.Now - lastUpdateTime > TimeSpan.FromSeconds(5))
			{
				UpdateState();
			}

			var installed = Directory.Exists(installationPath);
			var isInDependencies = dependencies != null && dependencies.TryGetValue(Constants.PackageName, out _);
			var comp = target as NeedleEditorSync;
			var enabled = comp?.enabled ?? false;
			try
			{

				base.OnInspectorGUI();

				if (comp && enabled != comp.enabled)
				{
					EditorSyncActions.SendEditorSyncEnabledStatusUpdate(comp.enabled);
				}

				if (exportInfo)
				{
					if (isInDependencies)
					{
						if (!installed || EditorSyncActions.IsInstallingEditor)
						{
							DrawInstallInfo();
						}
						else
						{
							GUILayout.Space(5);
							if (!Actions.IsInstalling())
							{
								if (GUILayout.Button(
									    new GUIContent("Uninstall Editor Sync Package",
										    "This will remove the package as a devDependency from your web project"),
									    bigButton))
								{
									EditorSyncActions.UninstallEditor();
								}
							}
						}
					}
					else
					{
						if (File.Exists(packageJsonPath))
						{
							DrawInstallInfo();
						}
					}
				}
			}
			finally
			{
				DrawFooter(installed && isInDependencies, comp && comp.enabled);
			}
		}

		private static void DrawInstallInfo()
		{
			var isInstallingEditor = EditorSyncActions.IsInstallingEditor;
			using (new EditorGUI.DisabledScope(isInstallingEditor))
			{
				if (isInstallingEditor)
				{
					var timePassed = EditorSyncActions.SecondsSinceInstallationStarted;
					if (timePassed > 120)
					{
						GUI.enabled = true;
						EditorGUILayout.HelpBox(
							"Installing package takes longer than expected. Please check your internet connection and make sure you have node installed.",
							MessageType.Warning);
						if (GUILayout.Button("Open NPM Log"))
						{
							Debug.Log("NPM Logs at " + NpmLogCapture.LogsDirectory);
							if (NpmLogCapture.GetLastLogFileCreated(out var log))
							{
								EditorUtility.OpenWithDefaultApp(log);
							}
						}
						GUI.enabled = false;
					}
					EditorGUILayout.HelpBox(
						$"Installing Needle Editor Sync package... please wait", MessageType.Info);
				}
				else
				{
					EditorGUILayout.HelpBox(
						$"{Constants.PackageName} package is not installed - click the button below to add it to your web project",
						MessageType.Warning);
				}
				var msg = isInstallingEditor
					? new GUIContent("Installing...", "Please wait")
					: new GUIContent("Install Editor Sync Package",
						"Clicking this button will add the " + Constants.PackageName +
						" package to your project (devDependency). It can be used for changes to edit your threejs scene from the Unity Editor");
				if (GUILayout.Button(msg, bigButton))
				{
					EditorSyncActions.InstallEditor();
				}
			}
		}
		
		private void DrawFooter(bool installed, bool enabled)
		{
			if (installed)
			{					
				EditorGUILayout.HelpBox("EditorSync is experimental: Please use Chrome for it to work!", MessageType.Warning);
			}
			
			var editorPath = installationPath + "/package.json";
			if (!Actions.IsInstalling() && PackageUtils.TryGetVersion(editorPath, out var version))
			{
				using (ColorScope.LowContrast())
				{
					var versionString = "Version " + version;
					if (EditorSyncActions.IsLocalInstallation(dependencies)) versionString += " (local)";
					EditorGUILayout.LabelField(versionString);
				}
			}

			if (Connection.Instance.IsConnected)
			{
				GUILayout.Space(5);
				var msg = "Connected to local server ✓";
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox(msg, MessageType.None, true);
					GUILayout.FlexibleSpace();
					// disable all buttons here during installation
					using var _ = new EditorGUI.DisabledScope(EditorSyncActions.IsInstallingEditor);
					if (GUILayout.Button(new GUIContent("Soft Restart", "Performs a soft local server restart"),
						    GUILayout.Height(17), GUILayout.Width(75)))
					{
						EditorSyncActions.RequestSoftServerRestart();
					}
				}
			}
		}
	}
}