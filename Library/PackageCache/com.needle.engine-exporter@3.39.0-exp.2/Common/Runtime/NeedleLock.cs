using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;

namespace Needle.Engine
{
	public class NeedleLock : IDisposable
	{
		private static int global;
		private readonly string path;
		
		public NeedleLock(string projectDirectory)
		{
			if (projectDirectory == null)
				return;
			projectDirectory = Path.GetFullPath(projectDirectory);
			if (!Directory.Exists(projectDirectory)) return;
			path = Path.Combine(projectDirectory, "needle.lock");
			global += 1;
			File.WriteAllText(path, global.ToString());
		}
		
		public void Dispose()
		{
			global -= 1;
			if (global > 0) return;
			if (!File.Exists(path)) return;
			File.Delete(path);
		}
	}
}