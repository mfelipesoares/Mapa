#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Projects
{
	internal static class ProjectPaths
	{
		public static string UnityProjectDirectory
		{
			get
			{
				projectDirectory ??= Path.GetFullPath(Application.dataPath + "/../");
				return projectDirectory;
			}
		}

		public static string? PackageDirectory
		{
			get
			{
				if (packagePath == null)
				{
					var package = AssetDatabase.GUIDToAssetPath("041e32dc0df5f4641b30907afb5926e6");
					if(!string.IsNullOrEmpty(package))
					{
						packagePath = Path.GetFullPath(package);
						if(!string.IsNullOrEmpty(packagePath))
							packageDirectory = Path.GetDirectoryName(packagePath);
					}
				}
				return packageDirectory;
			}
		}

		private static string? projectDirectory = null;
		private static string? packagePath = null;
		private static string? packageDirectory = null;
	}
}