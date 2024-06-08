using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Utils;
using UnityEngine;

namespace Needle.Engine.Codegen
{
	public class ProjectScriptDirectoryProvider : ITypesProvider
	{
		private readonly ProjectInfo projectInfo = new ProjectInfo("");

		public string GetScriptsDirectory(ComponentGenerator generator)
		{
			if (generator && generator.gameObject.TryGetComponent(out ExportInfo exp))
			{
				projectInfo.UpdateFrom(exp.DirectoryName);
				var path = Path.GetFullPath(projectInfo.ScriptsDirectory);
				if (!Directory.Exists(path)) return null;
				return path;
			}
			return null;
		}

		public void AddImports(List<ImportInfo> imports, IProjectInfo projectInfo)
		{
			this.projectInfo.UpdateFrom(projectInfo.ProjectDirectory);
			
			TypeScanner.FindTypes(this.projectInfo.ScriptsDirectory, imports);
			TypeScanner.FindTypes(this.projectInfo.EngineComponentsDirectory, imports);
			TypeScanner.FindTypes(this.projectInfo.ExperimentalEngineComponentsDirectory, imports); 
		}
	}
}