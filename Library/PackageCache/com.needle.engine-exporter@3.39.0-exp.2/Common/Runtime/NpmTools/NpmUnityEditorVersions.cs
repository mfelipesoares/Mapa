using System;
using System.IO;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Needle.Engine
{
	public static class NpmUnityEditorVersions
	{
		public enum Registry { Npm, Needle }
		
		private static JObject _packageJson;
		private static DateTime _fileModifiedTime;
		private const string packageJsonPath = Constants.ExporterPackagePath + "/package.json";
		
		public static bool TryGetVersions(out JObject versions, Registry registry)
		{
			var fi = new FileInfo(packageJsonPath);
			if (fi.Exists && fi.LastWriteTimeUtc != _fileModifiedTime)
			{
				_fileModifiedTime = fi.LastWriteTimeUtc;
				_packageJson = null;
			}
			
			if (_packageJson == null)
			{
				var json = File.ReadAllText(packageJsonPath);
				_packageJson = JObject.Parse(json);
			}

			if (_packageJson != null)
			{
				var key = registry == Registry.Npm ? "npm" : "needle";
				if (_packageJson.TryGetValue(key, out var ver))
				{
					versions = ver as JObject;
					return true;
				}
				versions = null;
				return false;
			}
			
			versions = null;
			return false;
		}
		
		/// <summary>
		/// Return the version that this version of the Unity integration recommends for a given package, if any
		/// </summary>
		public static bool TryGetRecommendedVersion(string packageName, out string version)
		{
			if (TryGetVersions(out var npm, Registry.Npm))
			{
				var recommendedVersion = npm[packageName];
				if (recommendedVersion != null)
				{
					version = recommendedVersion.ToString();
					return true;
				}
			}
			if(TryGetVersions(out var needle, Registry.Needle))
			{
				var recommendedVersion = needle[packageName];
				if (recommendedVersion != null)
				{
					version = recommendedVersion.ToString();
					return true;
				}
			}
			version = null;
			return false;
		}

		public static string TryGetRecommendedVersion(string packageName, string fallbackVersion)
		{
			if (TryGetRecommendedVersion(packageName, out var version)) return version;
			return fallbackVersion;
		}
		
	}
}