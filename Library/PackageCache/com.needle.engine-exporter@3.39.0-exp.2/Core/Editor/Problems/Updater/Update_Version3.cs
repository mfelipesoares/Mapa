using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Needle.Engine.Utils;
using UnityEngine;

namespace Needle.Engine.Problems
{
	[UsedImplicitly]
	internal class Update_Version3 : Update
	{
		public override DateTime UpgradeDate { get; } = new DateTime(2023, 04, 06);

		public override bool Apply(string fullProjectPath, CodeUpdateHelper codeUpdateHelper)
		{
			var packageJsonPath = fullProjectPath + "/package.json";
			codeUpdateHelper.Register(UpdateSrcImportsToPackageName);

			// Update scripts, replacing the pack-gltf commands
			if (PackageUtils.TryGetScripts(packageJsonPath, out var scripts))
			{
				var changedScripts = new List<(string key, string value)>();
				foreach (var kvp in scripts)
				{
					var script = kvp.Value;
					if (script.Contains("run pack-gltf"))
					{
						var changed = script.Replace("run pack-gltf", "run gltf:transform");
						changedScripts.Add((kvp.Key, changed));
						Debug.Log("Update: change script calling pack-gltf to gltf:transform");
					}

					if (kvp.Key == "pack-gltf")
					{
						changedScripts.Add((kvp.Key, null));
						changedScripts.Add(("gltf:transform",
							"npm run transform --prefix node_modules/@needle-tools/gltf-build-pipeline"));
					}
				}

				if (changedScripts.Count > 0)
				{
					foreach (var (key, script) in changedScripts)
					{
						if (script != null)
							scripts[key] = script;
						else
							scripts.Remove(key);
					}

					PackageUtils.TryWriteBlock(packageJsonPath, "scripts", scripts);
				}
			}

			// Add the tools package if it doesnt exist
			if (NpmUnityEditorVersions.TryGetRecommendedVersion(Constants.ToolsNpmPackageName, out var version)
			    && PackageUtils.TryReadDependencies(packageJsonPath, out var devDeps, "devDependencies"))
			{
				if (!devDeps.TryGetValue(Constants.ToolsNpmPackageName, out _))
				{
					devDeps.Add(Constants.ToolsNpmPackageName, version);
					Debug.Log($"Update: add {Constants.ToolsNpmPackageName} to devDependencies");
					PackageUtils.TryWriteDependencies(packageJsonPath, devDeps, "devDependencies");
				}
			}

			if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
			{
				var changed = false;
				if (deps.TryGetValue("three", out var threeValue))
				{
					if (threeValue.StartsWith("npm:@needle-tools/three@"))
					{
						// this is fine 🔥 (seriously)
					}
					else if (string.IsNullOrEmpty(threeValue) || threeValue.StartsWith("<"))
					{
						changed = true;
						deps.Remove("three");
						Debug.Log(
							"Update: remove three from dependencies. It is now automatically pulled in by @needle-tools/engine");
					}
					// We want to keep the three dependency if it's explicitly declared
					else if (PackageUtils.TryGetPath(fullProjectPath, threeValue, out var path))
					{
						if (!Directory.Exists(path))
						{
							changed = true;
							deps.Remove("three");
							Debug.Log(
								"Update: remove three from dependencies. It is now automatically pulled in by @needle-tools/engine");
						}
					}
				}

				if (deps.TryGetValue("@needle-tools/engine", out var engineValue))
				{
					if (PackageUtils.IsPath(engineValue) && engineValue.Contains("PackageCache"))
					{
						if (NpmUnityEditorVersions.TryGetRecommendedVersion(Constants.RuntimeNpmPackageName,
							    out var recommendedEngineVersion))
						{
							changed = true;
							deps["@needle-tools/engine"] = recommendedEngineVersion;
							Debug.Log(
								$"Update: change @needle-tools/engine from \"{engineValue}\" to recommended version ({recommendedEngineVersion})");
						}
					}
				}

				if (changed)
				{
					PackageUtils.TryWriteDependencies(packageJsonPath, deps);
				}
			}

			return true;
		}

		private readonly string[] ignoredFiles =
		{
			"engine_three_utils"
		};

		private bool UpdateSrcImportsToPackageName(string filepath, string[] lines)
		{
			var changed = false;
			const string newImport = "@needle-tools/engine";
			for (var index = 0; index < lines.Length; index++)
			{
				var line = lines[index];
				if (line.TrimStart().StartsWith("import"))
				{
					if (line.Contains("@noupdate")) break;
					if (ignoredFiles.Any(f => line.Contains(f))) continue;
					var startIndex = line.IndexOf("@needle-tools/engine/src", StringComparison.Ordinal);
					if (startIndex > 0)
					{
						var endIndex = line.IndexOf("\"", startIndex, StringComparison.Ordinal);
						if (endIndex > 0)
						{
							var newLine = line.Substring(0, startIndex) + newImport + line.Substring(endIndex);
							lines[index] = newLine;
							changed = true;
						}
					}
				}
			}
			if (changed)
				Debug.Log($"Update imports to use @needle-tools/engine in {filepath}".LowContrast());
			return changed;
		}
	}
}