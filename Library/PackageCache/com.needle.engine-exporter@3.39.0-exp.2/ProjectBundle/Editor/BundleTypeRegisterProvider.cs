using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Needle.Engine.Interfaces;
using Needle.Engine.Utils;

namespace Needle.Engine.ProjectBundle
{
	[UsedImplicitly]
	public class BundleTypeRegisterProvider : ITypeRegisterProvider
	{
		private static string GetCodeGenPath(Bundle bundle)
		{
			var codegenDir = bundle.PackageDirectory + "/codegen";
			Directory.CreateDirectory(codegenDir);
			var codegenPath = codegenDir + "/register_types.ts";
			return codegenPath;
		}
		
		public void RegisterTypes(List<TypeRegisterInfo> infos, IProjectInfo pi)
		{
			var list = new List<ImportInfo>();
			foreach (var bundle in BundleRegistry.Instance.Bundles)
			{
				if (!bundle.IsInstalled(pi.PackageJsonPath)) continue;
				list.Clear();
				bundle.FindImports(list, pi.ProjectDirectory);
				var info = new TypeRegisterInfo(GetCodeGenPath(bundle), list.ToList());
				infos.Add(info);
				
			} 
		}

		public void GetTypeRegisterPaths(List<TypeRegisterFileInfo> paths, IProjectInfo pi)
		{
			foreach (var bundle in BundleRegistry.Instance.Bundles)
			{
				if (!bundle.IsInstalled(pi.PackageJsonPath)) continue;
				
				// if we have a main file we want to import that
				if(PackageUtils.TryGetMainFile(bundle.PackageFilePath, out var mainFile))
				{
					var mainFilePath = bundle.PackageDirectory + "/" + mainFile;
					if (File.Exists(mainFilePath))
					{
						var rel = bundle.FindPackageName();
						var info = new TypeRegisterFileInfo()
						{
							RelativePath = rel,
							AbsolutePath = mainFilePath
						};
						paths.Add(info);
					}
				}
				
				var codegenPath = GetCodeGenPath(bundle);
				// codegen path
				// the file might not exist yet when installed for the first time
				// the codegen part that writes the file paths into the register_types.ts file does again check if it exists 
				// so we don't have to check here
				// if (File.Exists(codegenPath))
				{
					var packageDirectory = bundle.PackageDirectory + "/";
					var rel = bundle.FindPackageName() + "/" +  new Uri(packageDirectory).MakeRelativeUri(new Uri(codegenPath)).ToString();
					var info = new TypeRegisterFileInfo()
					{
						RelativePath = rel,
						AbsolutePath = codegenPath 
					};
					paths.Add(info);
				}
				
				// We want to delete the old register_types.js file if it exists
				// the change of .js to .ts was made in 322b3637be4479e1c08d78a0a4109418dac14b49
				var jsExt = ".js";
				if (!codegenPath.EndsWith(".js"))
				{
					var pathWithJsExt = Path.ChangeExtension(codegenPath, jsExt);
					if (File.Exists(pathWithJsExt))
					{
						File.Delete(pathWithJsExt);
					}
				}
			}
		}
	}	
}