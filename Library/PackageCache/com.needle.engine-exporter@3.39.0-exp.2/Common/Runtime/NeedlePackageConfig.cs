using System.IO;
using Newtonsoft.Json;

namespace Needle.Engine
{
	public class NeedlePackageConfig
	{
		public const string NAME = "package.needle.json";

		public static bool Exists(string dir)
		{
			if (dir.EndsWith(NAME)) return File.Exists(dir);
			return File.Exists(dir + "/" + NAME);
		}

		public static void Create(string dir)
		{
			var path = dir + "/" + NAME;
			var content = JsonConvert.SerializeObject(new NeedlePackageConfig(), Formatting.Indented);
			File.WriteAllText(path, content);
		}
	}
}