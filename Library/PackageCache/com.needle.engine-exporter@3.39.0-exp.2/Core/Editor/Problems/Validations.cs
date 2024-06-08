using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Problems
{
	public class Validations
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			var obj = ExportInfo.Get();
			if (obj && obj.Exists())
				RunValidation(obj.GetProjectDirectory() + "/package.json", false);
		}
		
		[MenuItem(Constants.MenuItemRoot + "/Internal/Try Validate Project")]
		private static void RunValidation()
		{
			var obj = ExportInfo.Get();
			if (!obj)
			{
				Debug.LogWarning("No " + nameof(ExportInfo) + " component found");
				return;
			}
			if (obj.Exists())
			{
				Debug.Log("Run Validation");
				if (!RunValidation(obj.GetProjectDirectory() + "/package.json", true, false))
					Debug.LogError("Validation failed");
				else Debug.Log("Validation finished");
			}
			else Debug.LogWarning("Project directory doest not exit: " + obj.GetProjectDirectory(), obj);
		}

		private static string lastTestDirectory = null;

		public static bool RunValidation(string packageJsonPath, bool force = false, bool silent = true)
		{
			if (!File.Exists(packageJsonPath)) return false;
			if (!force && lastTestDirectory == packageJsonPath) return true;
			if (Actions.IsInstalling())
			{
				if(!silent) Debug.LogWarning("Project is currently installing. Please wait for installation to finish before running validation.");
				return true;
			}
			lastTestDirectory = packageJsonPath;
			ValidateMainTsImport(packageJsonPath);
			return true;
		}

		private static void ValidateMainTsImport(string packageJsonPath)
		{
			var mainTsPath = Path.GetDirectoryName(packageJsonPath) + "/src/main.ts";
			if (File.Exists(mainTsPath))
			{
				var content = File.ReadAllText(mainTsPath);
				if (content.IndexOf("@needle-tools/engine/index", StringComparison.Ordinal) >= 0)
				{
					Debug.Log("Update main.ts @needle-tools/engine import");
					content = content.Replace("@needle-tools/engine/index", "@needle-tools/engine");
					File.WriteAllText(mainTsPath, content);
				}
			}
		}
	}
}