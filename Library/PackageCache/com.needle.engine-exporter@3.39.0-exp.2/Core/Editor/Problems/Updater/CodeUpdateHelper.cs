using System.Collections.Generic;
using System.IO;
using Needle.Engine.Utils;
using UnityEngine;

namespace Needle.Engine.Problems
{
	public class CodeUpdateHelper
	{
		public delegate bool UpdateDelegate(string filePath, string[] lines);
		
		private readonly List<string> extensions = new List<string> { ".ts", ".js", ".vue", ".jsx" };

		private readonly string directory;
		private string[] filePaths;
		private string[][] fileContents;
		private bool[] changed;

		public CodeUpdateHelper(string directory)
		{
			this.directory = directory;
		}
		
		private readonly List<UpdateDelegate> updates = new List<UpdateDelegate>();

		public void Register(UpdateDelegate update)
		{
			this.updates.Add(update);
		}

		internal void Apply()
		{
			if (updates.Count <= 0) return;
			UpdateScriptImportsInDirectory();
			updates.Clear();
			WriteChanges();
		}

		private void UpdateScriptImportsInDirectory()
		{
			if (!Directory.Exists(directory)) return;
			// Dont update files in immutable package
			if (directory.Contains("PackageCache"))
			{
				// but its ok if the directory is hidden
				var isHiddenDirectory = directory.EndsWith("~") || directory.Contains("~/") || directory.Contains("~\\");
				if (!isHiddenDirectory)
				{
					return;
				}
			}

			if (filePaths == null)
			{
				var paths = new List<string>();
				// read files in directory
				foreach (var path in UpdateUtils.ForeachFileWithoutNodeModules(directory, extensions))
					paths.Add(path);
				// read dependencies
				var packageJsonPath = Path.Combine(directory, "package.json");
				if (PackageUtils.TryReadDependencies(packageJsonPath, out var deps))
				{
					foreach (var dep in deps)
					{
						if (dep.Key.StartsWith("@needle-tools/engine")) continue;
						if (dep.Key == "three") continue;

						if (PackageUtils.TryGetPath(directory, dep.Value, out var dir) && Directory.Exists(dir))
						{
							foreach (var path in UpdateUtils.ForeachFileWithoutNodeModules(dir, extensions))
								paths.Add(path);
						}
					}
				}
				
				filePaths = paths.ToArray();
			}
			fileContents ??= new string[filePaths.Length][];
			changed ??= new bool[filePaths.Length];

			for (var index = 0; index < filePaths.Length; index++)
			{
				Update(index);
			}
		}

		private void Update(int index)
		{
			var filePath = filePaths[index];
			if (filePath.EndsWith(".d.ts")) return;
			if (filePath.EndsWith("register_types.js")) return;
			if (filePath.EndsWith("register_types.ts")) return;
			if (filePath.EndsWith("generated\\scripts.js")) return;

			var content = File.ReadAllLines(filePath);
			fileContents[index] = content;
			
			var didUpdate = false;
			foreach (var update in updates)
			{
				didUpdate |= update(filePath, content);
			}
			// for (var lineIndex = 0; lineIndex < content.Length; lineIndex++)
			// {
			// 	var line = content[lineIndex];
			// 	if (line.TrimStart().StartsWith("import"))
			// 	{
			// 		if (line.Contains("@needle-tools/engine/engine"))
			// 		{
			// 			content[lineIndex] = line.Replace("@needle-tools/engine/", "@needle-tools/engine/src/");
			// 			changed = true;
			// 		}
			// 	}
			// }
			
			this.changed[index] = didUpdate;
		}

		private void WriteChanges()
		{
			if (fileContents == null) return;
			for (var i = 0; i < filePaths.Length; i++)
			{
				var fileChanged = this.changed[i];
				if (!fileChanged) continue;
				var path = filePaths[i];
				var content = fileContents[i];
				if (content != null && content.Length > 0 && File.Exists(path))
				{
					filePaths[i] = null;
					fileContents[i] = null;
					File.WriteAllLines(path, content);
					Debug.Log("File updated at " + path);
				}
			}
		}
	}
}