using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Codegen;
using Needle.Engine.Core.References;
using Needle.Engine.Utils;
using UnityEditor;

namespace Needle.Engine.ProjectBundle
{
	internal static class NodeModulesComponentGeneration
	{
		private static readonly ComponentGeneratorRunner generator = new ComponentGeneratorRunner();

		/// <summary>
		/// Generate c# component stubs for node_module packages with needle engine typescript components
		/// </summary>
		/// <param name="packageDirectory">The directory to scan for components to generate</param>
		/// <param name="webProjectDirectory">The directory where the web project exists</param>
		internal static async void GenerateComponents(string packageDirectory, string webProjectDirectory)
		{
			if (!Directory.Exists(webProjectDirectory)) return;

			if (!CodeWatcher.TryFindCodeGeneratorPath(webProjectDirectory, out var generatorInstallPath))
			{
				return;
			}
			generatorInstallPath = Path.GetDirectoryName(generatorInstallPath);


			var packageJsonPath = packageDirectory + "/package.json";
			if (PackageUtils.TryGetVersion(packageJsonPath, out var version))
			{
				var name = PackageUtils.GetPackageName(packageJsonPath);
				var baseDirectory = "Assets/Needle/" + name.ToJsVariable() + ".codegen";

				using (new AssemblyReloadLockScope())
				{
					// Make sure to delete all previously generated versions
					// In the future we could ensure we have a asmdef for each version to keep other versions
					// But this would also be confusing when the same component exists 10 times in the project
					// So for now we just delete all other versions and make sure we only have one version
					var dirInfo = new DirectoryInfo(baseDirectory);
					if (dirInfo.Exists)
					{
						foreach (var dir in dirInfo.EnumerateDirectories())
						{
							if (dir.Name != version)
								await FileUtils.DeleteDirectoryRecursive(dir.FullName);
							var meta = dir.FullName + ".meta";
							if (File.Exists(meta)) File.Delete(meta);
						}
					}

					var outputDirectory = baseDirectory + "/" + version;
					// If we already generated types once we can skip this step
					if (Directory.Exists(outputDirectory))
					{
						return;
					}

					Directory.CreateDirectory(outputDirectory);

					// If the directory doesnt exist we want to scan for all types once more
					TypesGenerator.GenerateTypesIfNecessary(true);

					var types = new List<ImportInfo>();
					TypeScanner.FindTypesExcludingNodeModules(packageDirectory, types);
					foreach (var type in types)
					{
						var relativePath = type.FilePath.RelativeTo(packageDirectory);
						var targetDirectory = outputDirectory + "/" + Path.GetDirectoryName(relativePath);
						await generator.Run(generatorInstallPath, type.FilePath, targetDirectory);
					}

					if (types.Count <= 0)
					{
						File.WriteAllText(outputDirectory + "/empty", "");
					}
					else
					{
						// If no cs file was generated we want to put an empty marker in the directory 
						// Just so we know we dont have to re-generate and can check it into source control
						var anyFiles = Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories).Any();
						if (!anyFiles)
						{
							await FileUtils.DeleteDirectoryRecursive(outputDirectory);
						}
					}
				}


				AssetDatabase.Refresh();
			}
			;
		}
	}
}