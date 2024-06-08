using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Needle.Engine.Utils;
using Object = UnityEngine.Object;

namespace Needle.Engine.Codegen
{
	public static class ComponentGeneratorUtil
	{
		public static string GetGuid(string str)
		{
			return GuidGenerator.GetGuid(str);
		}

		/// <summary>
		/// Generates a stable GUID for the given file path and seed.
		/// </summary>
		/// <param name="filePath">Folder and file extension are ignored.</param>
		/// <param name="seed"></param>
		/// <returns></returns>
		public static string GetGuid(string filePath, string seed)
		{			
			// we need to do this since GenerateAndSetStableGuid was already doing it...
			// otherwise we end up with different GUIDs for the same filename
			if (!filePath.EndsWith(".meta"))
				filePath += ".meta";
			return GetGuid(Path.GetFileNameWithoutExtension(filePath) + seed);
		}

		public static string GenerateAndSetStableGuid(string filePath, string seed)
		{
			NeedleDebug.Log(TracingScenario.ComponentGeneration, "Generating GUID and .meta file for " + filePath + " with seed " + (seed ?? "<null>"));
			
			if (!filePath.EndsWith(".meta"))
			{
				// if the script was just created and no guid exists yet
				filePath += ".meta";
			}
			
			if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".meta")) return null;
			
			if (!File.Exists(filePath))
			{
				const string guidTemplate = @"fileFormatVersion: 2
guid: <guid>";
				var newGuid = GetGuid(filePath, seed);
				File.WriteAllText(filePath, guidTemplate.Replace("<guid>", newGuid));
				return newGuid;
			}
			
			var content = File.ReadAllText(filePath);
			var guid = GetGuid(filePath, seed);
			content = Regex.Replace(content, "(^guid:\\s?)([a-zA-Z0-9]{32})", $"$1{guid}", RegexOptions.Multiline | RegexOptions.ECMAScript);
			File.WriteAllText(filePath, content);
			return guid;
		}

		internal static void EnsureAssemblyDefinition(string filePath)
		{
			// TODO: we should generate a asmdef here in this directory if none exists yet
		}
		
		internal static void FilterDirectoriesToWatch(List<ComponentGenerationInfo> directories)
		{
			var proj = ExportInfo.Get();
			if (proj && proj.Exists())
			{
				var projectDir = new DirectoryInfo(proj.GetProjectDirectory());
				var packageJson = projectDir.FullName + "/package.json";
				if (PackageUtils.TryReadDependencies(packageJson, out var dict))
				{
					for (var index = directories.Count - 1; index >= 0; index--)
					{
						var path = directories[index];
						var dir = new DirectoryInfo(path.SourceDirectory);

						if (dir.FullName.StartsWith(projectDir.FullName)) continue;
						var found = false;
						foreach (var dep in dict)
						{
							if (!dep.Value.StartsWith("file:")) continue;
							if (!Directory.Exists(dep.Value)) continue;
							var depDir = new DirectoryInfo(Path.GetFullPath(dep.Value));
							if (!dir.FullName.StartsWith(depDir.FullName)) continue;
							found = true;
							break;
						}
						if (!found)
						{
							directories.RemoveAt(index);
						}
					}
				}
			}
		}
	}
}