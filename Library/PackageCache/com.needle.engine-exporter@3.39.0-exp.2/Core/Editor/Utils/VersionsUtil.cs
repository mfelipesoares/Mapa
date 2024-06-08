using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Semver;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Needle.Engine.Utils
{
	internal struct VersionHighlight
	{
		public string Version;
		public string Title;
		public string Text;
	}

	internal static class VersionsUtil
	{
		/// <summary>
		/// Get highlights in changelog for versions that the user has currently not installed
		/// </summary>
		internal static bool TryGetFutureVersionHighlights(out List<VersionHighlight> highlights)
		{
			highlights = null;
			
			GithubReleaseHelper.TryGetLatestReleases();
			var changelogPath = GithubReleaseHelper.changelogPath;
			if (!File.Exists(changelogPath)) return false;
			var currentVersionPath = Path.GetFullPath(Constants.ExporterPackagePath) + "/package.json";
			if (!File.Exists(currentVersionPath)) return false;
			if (!PackageUtils.TryGetVersion(currentVersionPath, out var currentVersion)) return false;
			var json = File.ReadAllText(changelogPath);
			if(string.IsNullOrEmpty(json)) return false;
			var jsonArray = JArray.Parse(json);
			var allVersions = new StringBuilder(5000);
			foreach (var token in jsonArray)
			{
				var release = token as JObject;
				if (release == null) continue;
				var version = release["name"]?.Value<string>(); 
				if (string.IsNullOrEmpty(version)) continue;
				if (version == currentVersion) break;
				allVersions.AppendLine("## [" + version + "]()");
				var body = release["body"]?.Value<string>();
				if (string.IsNullOrEmpty(body)) continue;
				allVersions.AppendLine(body);
			}
			return ParseHighlights(allVersions.ToString(), "9999.0.0", currentVersion, out highlights);
		}
		
		/// <summary>
		/// Get highlights in previous versions (between two version numbers)
		/// </summary>
		internal static bool TryGetPreviousVersionHighlights(string currentVersionStr,
			string previousVersionStr,
			out List<VersionHighlight> highlights,
			int limitMaxVersionsWithHighlights = 3)
		{
			var path = Path.GetFullPath(Constants.ExporterPackagePath) + "/Changelog.md";
			if (File.Exists(path))
			{
				var text = File.ReadAllText(path);
				return ParseHighlights(text, currentVersionStr, previousVersionStr, out highlights,
					limitMaxVersionsWithHighlights);
			}

			highlights = null;
			return false;
		}

		private static readonly string[] highlightPointSeparator = { "#### **" };

		internal static bool ParseHighlights(string text,
			string currentVersionStr,
			string previousVersionStr,
			out List<VersionHighlight> highlights,
			int limitMaxVersionsWithHighlights = 3
		)
		{
			highlights = null;

			try
			{
				if (string.IsNullOrEmpty(currentVersionStr)) return false;
				if (string.IsNullOrEmpty(previousVersionStr)) return false;
				if (!SemVersion.TryParse(currentVersionStr, SemVersionStyles.Any, out var currentVersion)) 
					return false;
				if (!SemVersion.TryParse(previousVersionStr, SemVersionStyles.Any, out var previousVersion))
					return false;

				// if someone does downgrade
				if (previousVersion > currentVersion)
				{
					// swap prev with current (prev > current)
					(previousVersion, currentVersion) = (currentVersion, previousVersion);
				}

				var prevIsCurrent = previousVersion == currentVersion;

				var versionsWithHighlightsCollected = 0;
				highlights = new List<VersionHighlight>();
				var regex = new Regex("##\\s+?\\[(?<version>.+)\\]", RegexOptions.Multiline);
				var matches = regex.Matches(text);
				for (var i = 0; i < matches.Count - 1; i++)
				{
					if (limitMaxVersionsWithHighlights > 0 &&
					    versionsWithHighlightsCollected >= limitMaxVersionsWithHighlights)
					{
						break;
					}
					var match = matches[i];
					var versionStr = match.Groups["version"].Value;
					var nextMatch = matches[i + 1];
					if (SemVersion.TryParse(versionStr, SemVersionStyles.Any, out var version))
					{
						var collectHighlight = version <= currentVersion && version > previousVersion;
						if (prevIsCurrent) collectHighlight = version == currentVersion;
						if (collectHighlight)
						{
							var chunk = text.Substring(match.Index, nextMatch.Index - match.Index);
							var sections = chunk.Split(highlightPointSeparator,
								StringSplitOptions.RemoveEmptyEntries);
							var hasHighlights = sections.Length > 1;
							if (hasHighlights)
								versionsWithHighlightsCollected += 1;

							for (var k = 1; k < sections.Length; k++)
							{
								var sectionText = sections[k];
								var titleEndIndex = sectionText.IndexOf("**", StringComparison.Ordinal);
								var title = sectionText.Substring(0, titleEndIndex).Trim();
								var textStart = titleEndIndex + 2;
								var sectionBody = sectionText.Substring(textStart);
								var nextBlockIndex = sectionBody.IndexOf("### ", StringComparison.Ordinal);
								if (nextBlockIndex > 0) sectionBody = sectionBody.Substring(0, nextBlockIndex);
								var highlight = new VersionHighlight()
								{
									Version = versionStr,
									Title = title,
									Text = sectionBody.Trim()
								};
								highlights.Add(highlight);
							}
						}
						else
						{
							break;
						}
					}
				}


				return highlights != null && highlights.Count > 0;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		private static string VersionTextFilePath => Application.dataPath + "/../Library/Needle/Version.txt";

		public static string PreviousVersionInstalled()
		{
			var path = VersionTextFilePath;
			if (!File.Exists(path)) return "0.0.0";
			var version = File.ReadAllText(path);
			return version;
		}

		public static bool VersionChanged(out string currentVersion, out string previousVersion)
		{
			currentVersion = null;
			previousVersion = null;
			var packageJson = Constants.ExporterPackagePath + "/package.json";
			if (!File.Exists(packageJson)) return false;
			if (PackageUtils.TryGetVersion(packageJson, out currentVersion))
			{
				previousVersion = PreviousVersionInstalled();
				return currentVersion != previousVersion;
			}
			return false;
		}

		public static bool WriteVersionInstalled()
		{
			var packageJson = Constants.ExporterPackagePath + "/package.json";
			if (!File.Exists(packageJson)) return false;
			if (PackageUtils.TryGetVersion(packageJson, out var version))
			{
				var path = VersionTextFilePath;
				var dir = Path.GetDirectoryName(path);
				Directory.CreateDirectory(dir!);
				File.WriteAllText(path, version);
				return true;
			}
			return false;
		}

		public static bool HasExporterPackageUpdate(out string latest, out string latestMinor)
		{
			return HasUpdate(Constants.UnityPackageName, out latest, out latestMinor);
		}
		
		public static bool HasSamplesPackageUpdate(out string latest, out string latestMinor)
		{
			return HasUpdate(Constants.SamplesPackageName, out latest, out latestMinor);
		}

		public static void ClearCache()
		{
			availableUpdates.Clear();
		}

		
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			while (true)
			{
				await Task.Delay(1000 * 120);
				ClearCache();
			}
		}

		private static bool HasUpdate(string packageName, out string latest, out string latestMinor)
		{
			if (availableUpdates.TryGetValue(packageName, out var update))
			{
				latest = update.latest;
				latestMinor = update.latestMinor;
				return latest != null;
			}
			DetectUpdateAvailable(packageName);
			latest = default;
			latestMinor = default;
			return false;
		}

		private static readonly List<string> searching = new List<string>();

		private static readonly Dictionary<string, (string latest, string latestMinor)>
			availableUpdates = new Dictionary<string, (string latest, string latestMinor)>();

		private static async void DetectUpdateAvailable(string packageName)
		{
			if (searching.Contains(packageName)) return;
			searching.Add(packageName);
			var request = Client.Search(packageName, false);
			while (request.IsCompleted == false) await Task.Yield();
			searching.Remove(packageName);
			availableUpdates.TryAdd(packageName, default);

			if (SemVersion.TryParse(ProjectInfo.GetCurrentPackageVersion(packageName, out _), SemVersionStyles.Any,
				    out var installed))
			{
				if (request.Result == null) return;
				foreach (var packageInfo in request.Result)
				{
					foreach (var version in packageInfo.versions.all)
					{
						HandleAvailableVersion(version);
					}
				}
				
				// HandleAvailableVersion("3.31.0");

				void HandleAvailableVersion(string version)
				{
					if (!SemVersion.TryParse(version, SemVersionStyles.Any, out var availableVersion)) return;
					if (installed < availableVersion)
					{
						var currentOptions = availableUpdates[packageName];
							
						// save the latest version available
						if(currentOptions.latest == null || !SemVersion.TryParse(currentOptions.latest, SemVersionStyles.Any, out var currentLatest) || availableVersion > currentLatest)
							currentOptions.latest = version;
							
						// also save the latest version of the same major version
						if (availableVersion.Major == installed.Major && availableVersion.Minor == installed.Minor)
						{
							if (currentOptions.latestMinor == null)
							{
								currentOptions.latestMinor = version;
							}
							else if (!SemVersion.TryParse(currentOptions.latestMinor, SemVersionStyles.Any,
								         out var currentMinor) || availableVersion > currentMinor)
							{
								currentOptions.latestMinor = version;
							}
						}
						availableUpdates[packageName] = currentOptions;
					}
				}
			}
		}
	}
}