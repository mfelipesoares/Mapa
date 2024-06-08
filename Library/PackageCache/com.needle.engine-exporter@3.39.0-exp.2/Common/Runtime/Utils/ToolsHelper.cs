using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Needle.Engine
{
	internal static class ToolsHelper
	{
		public const string NodejsWinUrl = "https://nodejs.org/dist/v20.9.0/node-v20.9.0-x64.msi";
		public const string NodejsOsxUrl = "https://nodejs.org/dist/v20.9.0/node-v20.9.0.pkg";

		public static bool IsDownloadingNodejs { get; private set; }

		public static bool IsNodejsInstalledOnDisc()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return File.Exists(Path.Combine(programFilesPath, "nodejs", "node.exe"));
			}
			return false;
		}

		public static string NodejsDownloadLocation
		{
#if UNITY_EDITOR
			get => EditorPrefs.GetString("NEEDLE_NodeDownloadLocation", "");
			private set => EditorPrefs.SetString("NEEDLE_NodeDownloadLocation", value);
#else
			get => "";
			set => _ = value;
#endif
		}

		internal static async void DownloadAndRunNodejs()
		{
			IsDownloadingNodejs = true;
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var url = isWindows ? NodejsWinUrl : NodejsOsxUrl;
			var path = await DownloadHelper.Download(url, "nodejs");
			IsDownloadingNodejs = false;
			NodejsDownloadLocation = path;
			RunInstaller(path);
		}


		public const string TokTxWinUrl = "https://fwd.needle.tools/needle-engine/toktx/win";
		public const string TokTxOsxUrl = "https://fwd.needle.tools/needle-engine/toktx/osx";
		public const string TokTxOsxSiliconUrl = "https://fwd.needle.tools/needle-engine/toktx/osx-silicon";


		public static bool IsDownloadingToktx { get; private set; }

		public static string ToktxDownloadLocation
		{
#if UNITY_EDITOR
			get => EditorPrefs.GetString("NEEDLE_ToktxDownloadLocation", "");
			private set => EditorPrefs.SetString("NEEDLE_ToktxDownloadLocation", value);
#else
			get => "";
			set => _ = value;
#endif
		}

		private static Task<string> DownloadToktx()
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var isArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
			var url = isWindows ? TokTxWinUrl : isArm ? TokTxOsxSiliconUrl : TokTxOsxUrl;
			IsDownloadingToktx = true;
			Debug.Log("Downloading toktx...");
			return DownloadHelper.Download(url, "toktx").ContinueWith(res =>
			{
				IsDownloadingToktx = false;
				ToktxDownloadLocation = res.Result;
				return res.Result;
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		internal static async void DownloadAndRunToktxInstaller()
		{
			var path = await DownloadToktx();
			RunInstaller(path);
		}

		private static bool RunInstaller(string path)
		{
			if (File.Exists(path))
			{
				try
				{
					Debug.Log("Run Installer: " + path.AsLink());
					Process.Start(Path.GetFullPath(path));
					return true;
				}
				catch (Win32Exception ex)
				{
					if (ex.Message.Contains("canceled")) Debug.Log("Installer process cancelled " + path.AsLink());
					else Debug.LogException(ex);
#if UNITY_EDITOR
					EditorUtility.RevealInFinder(path);
#endif
				}
			}
			else Debug.LogWarning("Installer not found at: " + path.AsLink());
			return false;
		}

		internal static void SetToktxCommandPathVariable(ref string cmd)
		{
#if UNITY_EDITOR_WIN
			var toktxPath = GetToktxDefaultInstallationLocation();
			cmd = $"set PATH=%PATH%;{toktxPath} && {cmd}";
#elif UNITY_EDITOR_OSX
			var toktxPath = GetToktxDefaultInstallationLocation();
			cmd = $"export PATH=$PATH:{toktxPath} && {cmd}";
#else
#endif
		}

		internal static string GetToktxDefaultInstallationLocation()
		{
#if UNITY_EDITOR_WIN
			var defaultInstallationLocation = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			return defaultInstallationLocation + "\\KTX-Software\\bin";
#elif UNITY_EDITOR_OSX
			var defaultInstallationLocation = "/usr/local/bin";
			return defaultInstallationLocation;
#else
			return null;
#endif
		}

		// https://stackoverflow.com/questions/19705401/how-to-set-system-environment-variable-in-c/19705691#19705691
// 		[MenuItem("Test/TryAddToktx path")]
// 		public static void TryAddToktxPath()
// 		{
// #if UNITY_EDITOR_WIN
// 			var PATH = "PATH";
// 			var scope = EnvironmentVariableTarget.Machine;
// 			var val = Environment.GetEnvironmentVariable(PATH, scope);
//
// 			// ProcessHelper.RunCommand(@"set PATH=%PATH%;C:\your\path\here\")
//
// 			// Environment.SetEnvironmentVariable(PATH, val, scope);
// #endif
// 		}
	}
}