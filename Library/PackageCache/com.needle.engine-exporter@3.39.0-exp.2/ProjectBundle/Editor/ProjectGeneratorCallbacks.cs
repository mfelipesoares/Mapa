using Needle.Engine.Projects;
using Needle.Engine.Utils;
using UnityEditor;

namespace Needle.Engine.ProjectBundle
{
	internal static class ProjectGeneratorCallbacks
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			ProjectGenerator.BeforeInstall += OnBeforeInstall; 
		}

		private static void OnBeforeInstall(string projectDir, ExportInfo export)
		{
			// Because ExportInfo currently removes npmdef packages that are not explicitly added in Unity
			// we have to add it to ExportInfo on install
			var packageJsonPath = projectDir + "/package.json";
			if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
			{
				foreach (var dep in deps)
				{
					if (BundleRegistry.TryGetBundle(dep.Key, out var bundle))
					{
						var dependency = new Dependency()
						{
							Name = bundle.FindPackageName(),
							VersionOrPath = bundle.FindPackageVersion(),
						};
						if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(bundle.LoadAsset(), out var guid, out long _))
						   dependency.Guid = guid;
						export.Dependencies.Add(dependency);
						EditorUtility.SetDirty(export);
					}
				}
			}
		}
	}
}