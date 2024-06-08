using System;
using System.Collections.Generic;
using System.IO;
using Needle.Engine.Projects;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.ProjectBundle
{
	internal static class BundleProjectGen
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			ProjectGenerator.UpdateVersionsInPackageJson += OnUpdateVersions;
		}

		private static void OnUpdateVersions(string obj)
		{
			EnsureValidNpmDefPathsInPackageJson(obj);
		}

		private static void EnsureValidNpmDefPathsInPackageJson(string packageJsonPath)
		{
			if (packageJsonPath.EndsWith("package.json"))
			{
				if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
				{
					var dir = Path.GetDirectoryName(packageJsonPath);
					var requireSave = false;
					var dependenciesToChange = new List<(string key, Bundle bundle)>();
					
					foreach (var dep in deps)
					{
						foreach (var bundle in BundleRegistry.Instance.Bundles)
						{
							if (bundle.FindPackageName() == dep.Key)
							{
								var path = Path.GetFullPath(bundle.PackageDirectory);
								var expectedPath = PackageUtils.GetFilePath(dir, path);
								if (dep.Value != expectedPath)
								{
									dependenciesToChange.Add((dep.Key, bundle));
								}
							}
						}
					}

					foreach (var dep in dependenciesToChange)
					{
						var path = Path.GetFullPath(dep.bundle.PackageDirectory);
						var expectedPath = PackageUtils.GetFilePath(dir, path);
						requireSave = true;
						Debug.Log("Update npmdef path: " + deps[dep.key] + " → " + expectedPath);
						deps[dep.key] = expectedPath;
					}

					if (requireSave)
					{
						PackageUtils.TryWriteDependencies(packageJsonPath, deps);
					}
				}
			}
		}
	}
}