using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Needle.Engine.Utils
{
	public static class PackageUtils
	{
		public static async Task<bool> NeedleEngineUnityVersionExists(string version)
		{
			var url = $"https://packages.needle.tools/com.needle.engine-exporter/-/com.needle.engine-exporter-{version}.tgz";
			var res = await WebHelper.MakeHeaderOnlyRequest(url);
			return res != null && res.result == UnityWebRequest.Result.Success; 
		}
		
		public static bool IsMajorVersionChange(string version1, string version2)
		{
			var majorIndex1 = version1.IndexOf('.');
			var majorIndex2 = version2.IndexOf('.');
			if(majorIndex1 < 0 || majorIndex2 < 0) return false;
			var major1 = version1.Substring(0, majorIndex1);
			var major2 = version2.Substring(0, majorIndex2);
			return major1 != major2;
		}

		public static bool IsChangeToPreReleaseVersion(string current, string version2)
		{
			if (current.Contains("pre") || current.Contains("exp"))
			{
				return false;
			}
			if (version2.Contains("pre") || version2.Contains("exp"))
			{
				return true;
			}
			return false;
		}
		
		public static async void Publish(string directory)
		{
			if (!File.Exists(directory + "/package.json"))
			{
				Debug.LogError("No package.json found in " + directory);
				return;
			}
			await ProcessHelper.RunCommand("npm publish --access public & pause",directory, null, false);
		}
		
		public static bool IsMutable(string path)
		{
			if (!Directory.Exists(path) && !File.Exists(path)) return false;
			path = Path.GetFullPath(path);
			var mutable = path.Contains("Library/PackageCache") == false && path.Contains("Library\\PackageCache") == false;
			return mutable;
		}
		
		private static string GetBlockPattern(string key) => "(\"" + key + "\" ?\\: ?)(?<dependencies>\\{.*?\\})";
		private static readonly Dictionary<string, Regex> _blockRegexCache = new Dictionary<string, Regex>();

		private static Regex GetBlockRegex(string key)
		{
			if (_blockRegexCache.ContainsKey(key)) return _blockRegexCache[key];
			var regex = new Regex(GetBlockPattern(key), RegexOptions.Singleline | RegexOptions.Compiled);
			_blockRegexCache.Add(key, regex);
			return regex;
		}

		private static readonly Regex versionRegex = new Regex("(\"version\" ?: ?)(?<version>\".+\")", RegexOptions.Multiline | RegexOptions.Compiled);

		public static bool SetVersion(string packageJsonPath, string version)
		{
			if (!File.Exists(packageJsonPath)) return false;
			var res = versionRegex.Replace(File.ReadAllText(packageJsonPath), "$1" + "\"" + version + "\"");
			File.WriteAllText(packageJsonPath, res);
			return true;
		}

		public static bool IsLocalVersion(string versionOrPath)
		{
			return versionOrPath != null && versionOrPath.StartsWith("file");
		}

		public static bool TryGetVersion(string packageJsonPath, out string version)
		{
			version = null;
			if (!File.Exists(packageJsonPath)) return false;
			var content = File.ReadAllText(packageJsonPath);
			var match = versionRegex.Match(content);
			if (match.Success)
			{
				version = match.Groups["version"].Value?.Trim('"');
				return !string.IsNullOrWhiteSpace(version);
			}
			return false;
		}

		public static string GetPackageName(string packageJsonPath)
		{
			if (!File.Exists(packageJsonPath)) return null;
			var content = File.ReadAllText(packageJsonPath);
			var name = Regex.Match(content, "\"name\" ?: ?\"(?<name>.+)\"");
			if (name.Success) return name.Groups["name"].Value;
			return null;
		}

		public static bool TryGetMainFile(string packageJsonPath, out string mainFile)
		{
			var content = File.ReadAllText(packageJsonPath);
			var main = Regex.Match(content, "\"main\"\\: ?\"(?<main>.+)\"");
			if (main.Success)
			{
				mainFile = main.Groups["main"].Value;
				return true;
			}
			mainFile = null;
			return false;
		}

		private class PackageJsonScripts
		{
			public Dictionary<string, string> scripts;
		}
		public static bool TryGetScripts(string packageJsonPath, out Dictionary<string, string> scripts)
		{
			if (File.Exists(packageJsonPath))
			{
				var content = File.ReadAllText(packageJsonPath);
				var obj = JsonConvert.DeserializeObject<PackageJsonScripts>(content);
				scripts = obj?.scripts;
				return scripts != null;
			}
			scripts = null;
			return false;
		}

		public static bool TryWriteScripts(string packageJsonPath, Dictionary<string, string> scripts)
		{
			return TryWriteBlock(packageJsonPath, "scripts", scripts);
		}

		public static bool IsDependency(string packageJsonPath, string packageName)
		{
			if (!string.IsNullOrEmpty(packageName) && TryReadDependencies(packageJsonPath, out var deps))
				return deps.ContainsKey(packageName);
			return false;
		}

		public static bool TryReadBlock(string packageJsonPath, string field, out Dictionary<string, string> dict)
		{
			dict = null;
			if (!File.Exists(packageJsonPath)) return false;
			var targetJson = File.ReadAllText(packageJsonPath);
			var dependenciesMatch = GetBlockRegex(field).Match(targetJson);

			if (dependenciesMatch.Success)
			{
				var match = dependenciesMatch.Groups["dependencies"];
				if (match != null && match.Success)
				{
					dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(match.Value);
					return dict != null;
				}
			}
			return false;
		}

		public static bool TryWriteBlock(string packageJsonPath, string field, Dictionary<string, string> dict)
		{
			var content = File.ReadAllText(packageJsonPath);
			var res = JsonConvert.SerializeObject(dict, Formatting.Indented);
			const char indent = '\t';
			res = res.Replace("\n", "\n" + indent);

			// $1 is referring to the "dependencies" capture (first group, 0 is ALL)
			var final = Regex.Replace(content, GetBlockPattern(field), $"$1{res}", RegexOptions.Singleline);
			File.WriteAllText(packageJsonPath, final);
			return true;
		}
		
		/// <summary>
		/// Reads the dependencies from a package.json file.
		/// </summary>
		/// <param name="packageJsonPath">Absolute path to package.json</param>
		/// <param name="dependencies">Package-to-version</param>
		/// <param name="key">The package.json entry to read, e.g. "dependencies" or "peerDependencies".</param>
		/// <returns></returns>
		public static bool TryReadDependencies(string packageJsonPath, out Dictionary<string, string> dependencies, string key = "dependencies")
		{
			return TryReadBlock(packageJsonPath, key, out dependencies);
		}

		public static bool TryWriteDependencies(string packageJsonPath, Dictionary<string, string> dependencies, string key = "dependencies")
		{
			return TryWriteBlock(packageJsonPath, key, dependencies);
		}

		public static bool AddPackage(string targetDir, string targetPackageDir)
		{
			var targetPath = targetDir + "/package.json";
			if (!File.Exists(targetPath)) return false;
			var packagePath = targetPackageDir + "/package.json";
			if (!File.Exists(packagePath)) return false;
			if (TryReadDependencies(targetPath, out var dependencies))
			{
				var name = GetPackageName(packagePath);
				var relPath = GetFilePath(targetDir, targetPackageDir);
				if (!dependencies.ContainsKey(name))
					dependencies.Add(name, relPath);
				else dependencies[name] = relPath;

				return TryWriteDependencies(targetPath, dependencies);
			}

			return false;
		}

		public static bool Remove(string packageJsonPathOrDirectory, string name, string key = "dependencies")
		{
			if(!packageJsonPathOrDirectory.EndsWith("package.json"))
				packageJsonPathOrDirectory += "/package.json";
			if (!File.Exists(packageJsonPathOrDirectory)) return false;
			if (TryReadDependencies(packageJsonPathOrDirectory, out var dict, key))
			{
				dict.Remove(name);
				return TryWriteDependencies(packageJsonPathOrDirectory, dict, key);
			}
			return false;
		}

		/// <summary>
		/// Get a project relative file path for package.json
		/// </summary>
		public static string GetFilePath(string targetDir, string sourceDir)
		{
			var packageJsonUri = new Uri(targetDir + "/");
			if (!Path.IsPathRooted(sourceDir)) sourceDir = Path.GetFullPath(sourceDir);
			var sourceUri = new Uri(sourceDir);
			return "file:./" + packageJsonUri.MakeRelativeUri(sourceUri).ToString().Replace("%20", " ");
		}

		public static bool IsAliasVersion(string val)
		{
			// e.g. npm:@needle-tools/engine@^0.125.0
			// https://docs.npmjs.com/cli/v9/using-npm/package-spec#aliases
			return val.StartsWith("npm:") && val.Contains("@");
		}

		public static bool IsPath(string val)
		{
			if (val.StartsWith("file:")) return true;
			if (val == "latest") return false;
			if (val.StartsWith("git:")) return false;
			// not allowed path character on windows
			// and it's used for explicit versions like "npm:@needle-tools/engine@^2.67.9-pre"
			// and possibly other registries?
			if(val.Contains(":")) return false;
			// Not entirely sure anymore why we check for the starting @
			return !val.StartsWith("@") && val.Contains("/");
		}

		public static bool TryGetPath(string dir, string val, out string fullPath)
		{
			if (IsPath(val))
			{
				if (val.StartsWith("file:")) val = val.Substring("file:".Length);
				// if its already a full path
				if (Path.IsPathRooted(val))
				{
					fullPath = val;
					return true;
				}
				// otherwise we need to reconstruct the full path relative to the directory
				fullPath = Path.GetFullPath(dir + "/" + val);
				return true;
			}
			fullPath = null;
			return false;
		}

		private static readonly string valueRegexPattern = "\"<packageName>\" ?\\: ?\"(?<value>.*)\"";
		public static bool GetDependencyValue(string packageJsonPathOrContent, string packageName, out string value)
		{
			if (string.IsNullOrWhiteSpace(packageJsonPathOrContent))
			{
				value = null;
				return false;
			}
			
			if (File.Exists(packageJsonPathOrContent)) packageJsonPathOrContent = File.ReadAllText(packageJsonPathOrContent);

			var pattern = valueRegexPattern.Replace("<packageName>", Regex.Escape(packageName));
			var match = Regex.Match(packageJsonPathOrContent, pattern);
			if (match.Success)
			{
				value = match.Groups["value"].Value;
				return true;
			}
			
			value = null;
			return false;
		}

		public struct Options
		{
			public bool MakePathRelativeToPackageJson;
		}

		public static bool ReplacePeerDependency(string packageJsonPath, string name, string versionOrPath, Options options = default)
		{
			return Replace("peerDependencies", packageJsonPath, name, versionOrPath, options);
		}

		public static bool ReplaceDevDependency(string packageJsonPath, string name, string versionOrPath, Options options = default)
		{
			return Replace("devDependencies", packageJsonPath, name, versionOrPath, options);
		}

		public static bool ReplaceDependency(string packageJsonPath, string name, string versionOrPath, Options options = default)
		{
			return Replace("dependencies", packageJsonPath, name, versionOrPath, options);
		}

		private static bool Replace(string blockName, string packageJsonPath, string name, string versionOrPath, Options options = default)
		{
			if (options.MakePathRelativeToPackageJson)
			{
				if (Directory.Exists(versionOrPath))
				{
					versionOrPath = new Uri(packageJsonPath).MakeRelativeUri(new Uri(Path.GetFullPath(versionOrPath))).ToString();
				}
			}

			var pattern = "(" + blockName + ".+?\"" + Regex.Escape(name) + "\" ?: ?\")(?<version>.+?)(\")";
			var content = File.ReadAllText(packageJsonPath);
			versionOrPath = versionOrPath.Replace("\\", "/");
			content = Regex.Replace(content, pattern, "$1" + versionOrPath + "$2", RegexOptions.Singleline);
			File.WriteAllText(packageJsonPath, content);
			return true;
		}
	}
}