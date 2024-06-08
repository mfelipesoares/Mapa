using System.Collections.Generic;
using System.IO;
using Needle.Engine.Utils;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

namespace Needle.Engine.ProjectBundle
{
	internal class NodeModuleTypesImporter : ITypesProvider, ITypeRegisterProvider
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Engine.Actions.InstallationFinished += OnInstalled;
		}

		private static ExportInfo _exportInfo;

		private static void OnInstalled(string installationPath, bool success)
		{
			// We only want to generate types for the main project, not for any dependency
			if (success)
			{
				if (!_exportInfo)
				{
					_exportInfo = ExportInfo.Get();
					if (!_exportInfo) return;
				}
				if (!_exportInfo.Exists()) return;
				var projectPath = _exportInfo.GetProjectDirectory();
				// Search in the project for types to generate
				SearchNodeModulesFolder(installationPath, directory =>
				{
					// Found a needle package -> check if it's a npmdef in this project
					if (IsNpmDefPackage(directory.FullName)) return false;
					
					// If it is solely from npm installed we want to generate the matching typescript components
					NodeModulesComponentGeneration.GenerateComponents(directory.FullName, projectPath);
					return false;
				});
			}
		}
		
		public void AddImports(List<ImportInfo> imports, IProjectInfo projectInfo)
		{
			SearchNodeModulesFolder(projectInfo.ProjectDirectory, directory =>
			{
				if (IsNpmDefPackage(directory.FullName)) return false;
				TypeScanner.FindTypes(directory.FullName, imports, SearchOption.TopDirectoryOnly);
				return true;
			});
		}

		public void RegisterTypes(List<TypeRegisterInfo> infos, IProjectInfo projectInfo)
		{
			// Nothing to do here for node_modules packages (they should already contain a import)
		}

		public void GetTypeRegisterPaths(List<TypeRegisterFileInfo> paths, IProjectInfo projectInfo)
		{
			SearchNodeModulesFolder(projectInfo.ProjectDirectory, directory =>
			{
				// Test if this directory is a package that is already part of Unity
				var packageJsonPath = directory.FullName + "/package.json";
				var packageName = PackageUtils.GetPackageName(packageJsonPath);
				if(BundleRegistry.TryGetBundle(packageName, out _))
				{
					// The package is a npmdef in this project, dont generate extra types for it
					return false;
				}
				
				if (!File.Exists(packageJsonPath)) return false;
				var name = PackageUtils.GetPackageName(packageJsonPath);
				var fullPath = directory.FullName + "/codegen/register_types.ts";
				paths.Add(new TypeRegisterFileInfo()
				{
					RelativePath = name + "/codegen/register_types.ts",
					AbsolutePath = fullPath
				});
				// NodeModulesComponentGeneration.GenerateComponents(directory.FullName, projectInfo.ProjectDirectory);
				return false;
			});
		}

		private static bool IsNpmDefPackage(string directory)
		{
			var packageJsonPath = directory + "/package.json";
			var packageName = PackageUtils.GetPackageName(packageJsonPath);
			if(BundleRegistry.TryGetBundle(packageName, out _))
			{
				// The package is a npmdef in this project, dont generate extra types for it
				return true;
			}
			return false;
		}

		private static void SearchNodeModulesFolder(string projectDirectory, Predicate<DirectoryInfo> onFound)
		{
			if (projectDirectory.StartsWith("http")) return;
			var nodeModulesPath = projectDirectory + "/node_modules";
			var nodeModules = new DirectoryInfo(nodeModulesPath);
			if (!nodeModules.Exists) return;
			foreach (var dir in nodeModules.EnumerateDirectories())
			{
				FindTypesInDirectory(dir, onFound);
			}
		}

		private static void FindTypesInDirectory(DirectoryInfo directory, Predicate<DirectoryInfo> onFound)
		{
			// Dont traverse into node_modules folder
			if (directory.Name == "node_modules")
			{
				return;
			}
			// if it's an org package we need to traverse into it
			if (directory.Name.StartsWith("@"))
			{
				foreach (var dir in directory.EnumerateDirectories())
				{
					FindTypesInDirectory(dir, onFound);
				}
			}
			else
			{
				// If the package directory contains a needle package file
				if (NeedlePackageConfig.Exists(directory.FullName))
				{
					var traverseChildren = onFound.Invoke(directory);
					if (!traverseChildren) return;
					foreach (var dir in directory.EnumerateDirectories())
					{
						FindTypesInDirectory(dir, onFound);
					}
				}
			}
		}
	}
}