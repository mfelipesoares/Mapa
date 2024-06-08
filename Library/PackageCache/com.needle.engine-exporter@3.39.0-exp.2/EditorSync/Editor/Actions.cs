using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Editors;
using Needle.Engine.Server;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.EditorSync
{
	public static class EditorSyncActions
	{
		internal const string packageJsonKey = "devDependencies";
		private static bool isInstallingEditor;

		public static bool IsInstallingEditor => isInstallingEditor;
		internal static int SecondsSinceInstallationStarted => (int)(DateTime.Now - installationStarted).TotalSeconds;
		private static DateTime installationStarted;

		private static bool _lastInstalledResult;
		private static DateTime _nextInstalledCheck;
		public static bool CheckIsInstalled(bool force = false)
		{
			if (!force && DateTime.Now < _nextInstalledCheck)
				return _lastInstalledResult;
			_nextInstalledCheck = DateTime.Now + TimeSpan.FromSeconds(3);
			var exportInfo = ExportInfo.Get();
			if (!exportInfo) return false;
			return _lastInstalledResult = File.Exists(exportInfo.GetProjectDirectory() + "/node_modules/" + Constants.PackageName + "/package.json");
		}

		public static async void InstallEditor()
		{
			if (isInstallingEditor) return;
			var exportInfo = ExportInfo.Get();
			try
			{
				isInstallingEditor = true;
				installationStarted = DateTime.Now;
				if (TryGetPackageJson(exportInfo, out var path))
				{
					if (PackageUtils.TryReadDependencies(path, out var deps, "devDependencies"))
					{
						if (!deps.ContainsKey(Constants.PackageName))
						{
							Debug.Log("Add " + Constants.PackageName + " to devDependencies");
							deps.Add(Constants.PackageName, "^1.0.0-pre");
							PackageUtils.TryWriteDependencies(path, deps, "devDependencies");
							await Task.Delay(1000);
						}
						var directory = Path.GetDirectoryName(path);
						Debug.Log("Update " + Constants.PackageName);
						await ProcessHelper.RunCommand("npm update " + Constants.PackageName, directory);
						var packageLock = directory + "/package-lock.json";
						if (File.Exists(packageLock)) File.Delete(packageLock);
						await Actions.InstallCurrentProject(false);
						await Task.Delay(500);
						Debug.Log("Installation finished: Restarting local server");
						RequestSoftServerRestart();
					}
				}
			}
			finally
			{
				isInstallingEditor = false;
			}
		}

		public static async void UninstallEditor()
		{
			var exportInfo = ExportInfo.Get();
			if (!exportInfo) return;
			var packageJsonPath = exportInfo.GetProjectDirectory() + "/package.json";
			if (PackageUtils.TryReadDependencies(packageJsonPath, out var dependencies, packageJsonKey))
			{
				var isLocalInstallation = IsLocalInstallation(dependencies);
				dependencies.Remove(Constants.PackageName);
				PackageUtils.TryWriteDependencies(packageJsonPath, dependencies, packageJsonKey);
				RequestServerRestart();
				
				var installationPath = Path.GetFullPath(exportInfo.GetProjectDirectory()) +
				                   "/node_modules/" + Constants.PackageName;
				if (Directory.Exists(installationPath) && !isLocalInstallation)
				{
					await FileUtils.DeleteDirectoryRecursive(installationPath);
				}
			}
		}

		[MenuItem("CONTEXT/" + nameof(NeedleEditorSync) + "/Restart Server")]
		public static void RequestServerRestart()
		{
			if (Connection.Instance.IsConnected)
			{
				Connection.Instance.SendRaw("needle:editor:stop");
				Actions.StartLocalServer();
			}
		}

		[MenuItem("CONTEXT/" + nameof(NeedleEditorSync) + "/Restart Server (Soft)")]
		public static void RequestSoftServerRestart()
		{
			Server.Actions.RequestSoftServerRestart();
		}

		private static bool TryGetPackageJson(ExportInfo exportInfo, out string packageJsonPath)
		{
			packageJsonPath = exportInfo.GetProjectDirectory() + "/package.json";
			if (!File.Exists(packageJsonPath))
			{
				Debug.LogError("No package.json found at path " + packageJsonPath +
				               ", please make sure your project is setup correctly");
				return false;
			}
			return true;
		}

		internal static bool IsLocalInstallation(Dictionary<string, string> dict)
		{
			if (dict == null) return false;
			if (dict.TryGetValue(Constants.PackageName, out var value))
			{
				if (PackageUtils.IsLocalVersion(value)) return true;
			}
			return false;
		}

		internal static void SendEditorSyncEnabledStatusUpdate(bool enabled)
		{
			if (Connection.Instance.IsConnected)
			{
				var msg = enabled
					? "needle:editor:editor-sync-enabled"
					: "needle:editor:editor-sync-disabled";
				Connection.Instance.SendRaw(msg);
			}
		}
	}
	
}