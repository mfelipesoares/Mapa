using UnityEngine;

namespace Needle.Engine.Core
{
	public class GeneratorMeta : IBuildConfigProperty
	{
		public string Key => "generator";

		public object GetValue(string projectDirectory)
		{
			var unityVersion = Application.unityVersion;
			return "Unity " + unityVersion + ", Needle Engine Integration @" + ProjectInfo.GetCurrentNeedleExporterPackageVersion(out _);
		}
	}
}