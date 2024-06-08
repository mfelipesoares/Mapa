using System;
using System.IO;
using Needle.Engine.Utils;

namespace Needle.Engine
{
	public class ImportInfo : IImportedTypeInfo
	{
		public string TypeName { get; }
		public string FilePath { get; }
		public bool IsInstalled { get; set; } = true;
		public FileInfo PackageJson { get; }
		public bool IsAbstract = false;

		private readonly string fileContent;

		public ImportInfo(string typename, string filepath, string content, FileInfo packageJson)
		{
			this.TypeName = typename;
			this.FilePath = Path.GetFullPath(filepath);
			this.fileContent = content;
			this.PackageJson = packageJson;
		}

		public string RelativeTo(string directory)
		{
			try
			{
				return new Uri(directory).MakeRelativeUri(new Uri(FilePath)).ToString().Replace("%20", " ");
			}
			catch (UriFormatException)
			{
				return FilePath;
			}
		}

		public bool ShouldIgnore => FilePath.StartsWith("__");

		public string PackageName
		{
			get
			{
				if (_packageName == null)
				{
					_packageName = "";
					var nodeModulesIndex = FilePath.IndexOf("node_modules", StringComparison.Ordinal);
					if (nodeModulesIndex >= 0)
					{
						nodeModulesIndex += "node_modules".Length;
						var end = FilePath.IndexOfAny(pathSeparators, nodeModulesIndex + 1);
						var name = end >= 0 
							? FilePath.Substring(nodeModulesIndex + 1, end - nodeModulesIndex - 1) 
							: FilePath.Substring(nodeModulesIndex + 1);
						if (name.StartsWith("@"))
						{
							var end2 = FilePath.IndexOfAny(pathSeparators, end + 1);
							if (end2 >= 0)
								name = FilePath.Substring(nodeModulesIndex + 1, end2 - nodeModulesIndex - 1);
						}
						_packageName = name;
						return _packageName;
					}

					var fileInfo = new FileInfo(FilePath);
					var dir = fileInfo.Directory;
					var level = 0;
					while (dir != null)
					{
						if (level++ > 4) break;
						if (_packageName != null && _packageName.Length > 0) break; 
						var packageJsonPath = Path.Combine(dir.FullName, "package.json");
						var name = PackageUtils.GetPackageName(packageJsonPath);
						if(!string.IsNullOrWhiteSpace(name)) _packageName = name;
						dir = dir.Parent;
					}
				}
				return _packageName;
			}
		}

		private string _packageName;
		
		private static readonly char[] pathSeparators = {'/', '\\'};
	}
}