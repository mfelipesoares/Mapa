using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;

// ReSharper disable once CheckNamespace
namespace Needle.Engine
{
	public static class BuildTargetManager
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			var bi = BuildPlatforms.instance;
			var platforms = bi.buildPlatforms.ToList();

			var platformToShim = platforms.Find(x => x.targetGroup == BuildPlatformConstants.BuildTargetGroup);
			if (platformToShim != null)
				platforms.Remove(platformToShim);

			var platform = BuildPlatformConstants.Platform;
			
			// insert above webgl
			var inserted = false;
			for (var i = 0; i < platforms.Count; i++)
			{
				var other = platforms[i];
				if (other.name == "WebGL")
				{
					inserted = true;
					platforms.Insert(i, platform);
					break;
				}
			}
			if (!inserted) platforms.Insert(3, platform);
			
			bi.buildPlatforms = platforms.ToArray();
		}
	}
}