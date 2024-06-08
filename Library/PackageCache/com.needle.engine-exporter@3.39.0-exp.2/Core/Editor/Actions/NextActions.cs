using System.IO;

namespace Needle.Engine
{
	public class NextActions
	{
		public static bool DeleteCache(string projectDirectory = null)
		{
			var cachePath = projectDirectory + "/.next/cache";
			if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true);
			return true;
		}
	}
}