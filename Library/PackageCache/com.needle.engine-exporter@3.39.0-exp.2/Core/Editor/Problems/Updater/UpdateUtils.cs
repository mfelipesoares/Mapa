using System.Collections.Generic;

namespace Needle.Engine.Problems
{
	internal static class UpdateUtils
	{
		internal static IEnumerable<string> ForeachFileWithoutNodeModules(string directory, IList<string> extensions)
		{
			var files = System.IO.Directory.GetFiles(directory, "*.*", System.IO.SearchOption.TopDirectoryOnly);
			foreach (var file in files)
			{
				foreach (var ext in extensions)
				{
					if (file.EndsWith(ext))
					{
						yield return file;
						break;
					}
				}
			}
			var dirs = System.IO.Directory.GetDirectories(directory);
			foreach (var dir in dirs)
			{
				if (dir.EndsWith("node_modules")) continue;
				foreach (var file in ForeachFileWithoutNodeModules(dir, extensions))
					yield return file;
			}
		}
	}
}