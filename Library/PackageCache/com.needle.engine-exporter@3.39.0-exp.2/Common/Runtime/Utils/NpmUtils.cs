// #define UNITY_EDITOR_OSX

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using System.Linq;
#endif

namespace Needle.Engine.Utils
{
	public static class NpmUtils
	{
		public static Task<bool> UpdatePackages(IList<string> packages, string workingDirectory)
		{
			var cmd = "npm set registry https://registry.npmjs.org";
			cmd += " && npm update " + string.Join(" ", packages);
			Debug.Log($"Running npm update for {packages.Count} packages\n{string.Join(", ", packages)}");
			return ProcessHelper.RunCommand(cmd, workingDirectory, null, true, false);
		}
		
		public static string GetPackageUrl(string packageName)
		{
			return $"https://www.npmjs.com/package/{packageName}";
		}
		
		public static string GetCodeUrl(string packageName, string version)
		{
			return $"https://www.npmjs.com/package/{packageName}/v/{version.AsSpecificSemVer()}?activeTab=code";
		}
		
		public const string NpmRegistryUrl = "https://registry.npmjs.org/";
		public const string NpmInstallFlags = "--install-links=false";
		public const string NpmNoProgressAuditFundArgs = "--progress=false --no-audit --no-fund";

		private static readonly char[] _rangeChars = {' ', '^', '~', 'v'};
		private static readonly Regex _getSemverRegex = new Regex(@"(?<semver>\d{1,}\.\d{1,}\.\d{1,}(\-\w+)?(\.\d+)?)");
		
		public static string AsSpecificSemVer(this string version)
		{
			if (version.StartsWith("npm:"))
			{
				// e.g. npm:@needle-tools/three@^1.2.3
				version = version.Substring(4);
				var idx = version.LastIndexOf('@');
				// split out version (e.g. ^1.2.3)
				var actualVersion = idx >= 0 ? version.Substring(idx + 1) : version;
				actualVersion = actualVersion.AsSpecificSemVer();
				// concat again with prefix
				version = version.Substring(0, idx + 1) + actualVersion;
				return version;
			}

			// we use this to get the first semver in a range (e.g. <= 3.0.0 > 5.0.0-alpha would then result in 3.0.0)
			// https://regex101.com/r/yHZ0Jg/1
			var match = _getSemverRegex.Match(version);
			if (match.Success)
			{
				var semver = match.Groups["semver"];
				if (semver.Success)
				{
					version = semver.Value;
				}
			}

			return version.TrimStart(_rangeChars);
		}

		public static string GetNpmRegistryEndpointUrlForPackage(string packageName)
		{
			return NpmRegistryUrl + "/" + packageName;
		}

		public static async Task<JObject> TryGetCompletePackageInfo(string packageName)
		{
			try
			{
				using var client = new WebClient();
				var json = await client.DownloadStringTaskAsync(GetNpmRegistryEndpointUrlForPackage(packageName));
				return JObject.Parse(json);
			}
			catch
			{
				return null;
			}
		}

		public static async Task<JObject> TryGetPackageVersionInfo(string packageName, string packageVersion)
		{
			try
			{
				using var client = new WebClient();
				var json = await client.DownloadStringTaskAsync(GetNpmRegistryEndpointUrlForPackage(packageName) + "/" + packageVersion.AsSpecificSemVer());
				return JObject.Parse(json);
			}
			catch
			{
				return null;
			}
		}

		public static async Task<bool> PackageExists(string packageName, string packageVersion)
		{
			try
			{
				if (packageVersion.StartsWith("^") || packageVersion.StartsWith(">") || packageVersion.StartsWith("<"))
				{
					var packageUrl = GetNpmRegistryEndpointUrlForPackage(packageName) + "/" +
					                 packageVersion.AsSpecificSemVer();
					Debug.Log($"Checking if \"{packageName}@{packageVersion}\" exists (can not check version range yet)".LowContrast());
					var res = await WebHelper.MakeHeaderOnlyRequest(packageUrl);
					
					return res.result == UnityWebRequest.Result.Success;
					// var packageInfo = await TryGetCompletePackageInfo(packageName);
					// var versions = packageInfo?["versions"] as JObject;
					// if (versions == null) return false;
					// foreach (var ver in versions)
					// {
					// 	
					// }
				}
				if (packageVersion.StartsWith("npm:"))
				{
					// In this case the package name is a scoped package name
					// maybe we just need to check for @ instead to cover all cases?
					var scopedVersionString = packageVersion.AsSpecificSemVer();
					var separatorIndex = scopedVersionString.LastIndexOf('@');
					packageName = scopedVersionString.Substring(0, separatorIndex);
					packageVersion = scopedVersionString.Substring(separatorIndex + 1);
					var url = GetNpmRegistryEndpointUrlForPackage(packageName) + "/" + packageVersion;
					var res = await WebHelper.MakeHeaderOnlyRequest(url);
					return res.result == UnityWebRequest.Result.Success;
				}
				else
				{
					var url = GetNpmRegistryEndpointUrlForPackage(packageName) + "/" + packageVersion.AsSpecificSemVer();
					var res = await WebHelper.MakeHeaderOnlyRequest(url);
					return res.result == UnityWebRequest.Result.Success;
				}
			}
			catch
			{
				return false;
			}
		}

		public static string GetStartCommand(string projectDirectory)
		{
			if (PackageUtils.TryGetScripts(projectDirectory + "/package.json", out var scripts))
			{
				foreach (var kvp in scripts)
				{
					if (kvp.Value.Contains("next dev")) return "npm run " + kvp.Key;
				}
			}
			return "npm start";
		}

		public static string GetInstallCommand(string projectDirectory)
		{
			if (PackageUtils.TryGetScripts(projectDirectory + "/package.json", out var scripts))
			{
				if (scripts.TryGetValue("install", out var cmd))
					return cmd;
			}
			if (File.Exists(projectDirectory + "/yarn.lock"))
				return $"yarn install {NpmInstallFlags} {NpmNoProgressAuditFundArgs}";
			return $"npm install {NpmInstallFlags} {NpmNoProgressAuditFundArgs} --include=optional sharp";
		}

		public static bool IsInstallationCommand(string cmd)
		{
			if (cmd.Contains("npm install")) return true;
			if (cmd.Contains("npm i")) return true;
			if (cmd.Contains("yarn install")) return true;
			if (cmd.Contains("yarn add")) return true;
			return false;
		}

		internal static string TryFindNvmInstallDirectory()
		{
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
			var path = default(string);
			var userDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
			var npmDirectory = System.IO.Path.Combine(userDirectory, ".nvm/versions/node");
			if (Directory.Exists(npmDirectory))
			{
				var versions = System.IO.Directory.GetDirectories(npmDirectory);
				if (versions.Length > 0)
				{
					var latestVersion = versions.Last();
					path = System.IO.Path.Combine(latestVersion, "bin");
					if (!Directory.Exists(path)) path = null;

				}
			}
			return path;
#else
			return null;
#endif
		}

		public static void LogPaths()
		{
#if UNITY_EDITOR_WIN
			var npmPaths = new List<string>();
			foreach (var line in ProcessHelper.RunCommandEnumerable("echo %PATH%"))
			{
				var parts = line.Split(';');
				foreach (var part in parts)
				{
					if (part.Contains("npm") || part.Contains("nodejs"))
						npmPaths.Add(part);
				}
			}
			if (npmPaths.Count > 0)
				Debug.Log(string.Join("\n", npmPaths));
			else
				Debug.Log("No npm paths found in PATH environment variable");
#endif
		}
	}
}