#nullable enable
using System;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
	/// <summary>
	/// Can be put into a web project directory as needle.config.json to configure export targets
	/// </summary>
	[Serializable]
	public class NeedleProjectConfig
	{
		public const string NAME = "needle.config.json";

		/// <summary>
		/// If configured the loading path for glbs will be changed to this base path instead of using the assetsDirectory
		/// </summary>
		/// <remarks>
		/// the base url mainly exists for codegen for cases where the local assets directory is not directly mapped to a server url (e.g. in next.js we have a public folder and this folder is the root server directory, so a local path like "public/assets" will actually be "assets" on the server)</remarks>
		public string? baseUrl = null;
		
		/// <summary>
		/// This is the directory where the files will be exported to
		/// </summary>
		public string buildDirectory = "dist";
		public string assetsDirectory = "assets";
		public string scriptsDirectory = "src/scripts";
		public string codegenDirectory = "src/generated";
		
		public static bool TryLoad(string projectDirectory, out NeedleProjectConfig config)
		{
			var path = Path.Combine(projectDirectory, NAME);
			if (File.Exists(path))
			{
				var json = File.ReadAllText(path);
				if (!string.IsNullOrWhiteSpace(json))
				{
					config = JsonConvert.DeserializeObject<NeedleProjectConfig>(json)!;
					return config != null!;
				}
			}
			config = default!;
			return false;
		}

		public static bool TryCreate(IProjectInfo project, out NeedleProjectConfig config, out string? path)
		{
			var dir = project.ProjectDirectory;
			if (Directory.Exists(dir) && File.Exists(dir + "/package.json"))
			{
				config = new NeedleProjectConfig();

				if (!Directory.Exists(project.AssetsDirectory))
				{
					if (Directory.Exists(dir + "/public"))
						config.assetsDirectory = "public";
				}

				path = Path.Combine(dir, NAME);
				var content = JsonConvert.SerializeObject(config, Formatting.Indented);
				File.WriteAllText(path, content);
				return true;
			}

			path = null;
			config = null!;
			return false;
		}


		private static ExportInfo? exportInfo;
		private static NeedleProjectConfig? config;
		private static DateTime nextCheck;

		private static bool Init()
		{
			if (!exportInfo || config == null || DateTime.Now > nextCheck)
			{
				nextCheck = DateTime.Now.AddSeconds(10);
				exportInfo = ExportInfo.Get();
				if (!exportInfo) 
					return false;
				if (!TryLoad(exportInfo.GetProjectDirectory(), out config))
					return false;
			}
			return true;
		}

		public static bool TryGetBuildDirectory(out string dir)
		{
			dir = null!;
			if (!Init()) return false;
			dir = exportInfo!.GetProjectDirectory() + "/" + config!.buildDirectory;
			return Directory.Exists(dir);
		}

		public static bool TryGetCodegenDirectory(out string dir)
		{
			dir = null!;
			if (!Init()) return false;
			dir = exportInfo!.GetProjectDirectory() + "/" + config!.codegenDirectory;
			return true;
		}

		public static bool TryGetAssetsDirectory(out string dir)
		{
			dir = null!;
			if (!Init()) return false;
			dir = exportInfo!.GetProjectDirectory() + "/" + config!.assetsDirectory;
			return true;
		}

		public static bool TryGetBaseUrl(out string dir)
		{
			dir = null!;
			if (!Init()) return false;
			if (!string.IsNullOrEmpty(config!.baseUrl))
			{
				dir = exportInfo!.GetProjectDirectory() + "/" + config!.baseUrl;
				return true;
			}
			return false;
		}
	}
}