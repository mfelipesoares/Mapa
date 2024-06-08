using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Needle.Engine.Problems
{
	[UsedImplicitly]
	internal class Update_MovedEngineIntoSrc : Update
	{
		public override DateTime UpgradeDate { get; } = new DateTime(2023, 03, 08);

		public override bool Apply(string fullProjectPath, CodeUpdateHelper codeUpdateHelper)
		{
			UpdatePackageJson(fullProjectPath);
			codeUpdateHelper.Register(UpdateToSrc);
			return true;
		}

		private static void UpdatePackageJson(string projectPath)
		{
			var packageJsonPath = Path.Combine(projectPath, "package.json");
			var packageJson = File.ReadAllText(packageJsonPath);
			if (packageJson.Contains("\"from\": \"node_modules/@needle-tools/engine/include/**/*.*\","))
			{
				Debug.Log("Update copy include file paths");
				var replace = packageJson.Replace(
					"node_modules/@needle-tools/engine/include/**/*.*",
					"node_modules/@needle-tools/engine/src/include/**/*.*"
				);
				File.WriteAllText(packageJsonPath, replace);
			}
		}
		
		private static bool UpdateToSrc(string filePath, string[] content)
		{
			var changed = false;
			for (var index = 0; index < content.Length; index++)
			{
				var line = content[index];
				if (line.TrimStart().StartsWith("import"))
				{
					if (line.Contains("@needle-tools/engine/engine"))
					{
						content[index] = line.Replace("@needle-tools/engine/", "@needle-tools/engine/src/");
						changed = true;
					}
				}
			}
			return changed;
		}
	}
}