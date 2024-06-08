using JetBrains.Annotations;
using Needle.Engine.Settings;

namespace Needle.Engine.Core
{
	[UsedImplicitly]
	internal class UseGizp : IBuildConfigProperty
	{
		public static bool Enabled
		{
			get => NeedleEngineBuildOptions.UseGzipCompression;
			set => NeedleEngineBuildOptions.UseGzipCompression = value;
		}
		
		public string Key => "gzip";
		public object GetValue(string projectDirectory)
		{
			return NeedleEngineBuildOptions.UseGzipCompression;
		}
	}


	[UsedImplicitly]
	internal class UseHotReload : IBuildConfigProperty
	{
		public string Key => "allowHotReload";
		public object GetValue(string projectDirectory)
		{
			return ExporterProjectSettings.instance.useHotReload;
		}
	}
	
	
	[UsedImplicitly]
	internal class BuildType : IBuildConfigProperty
	{
		public string Key => "developmentBuild";
		public object GetValue(string projectDirectory)
		{
			// This should be the default. E.g. if a user wants to export from a menu item and kicks OFF a production build
			// we don't really care about the build window setting. The context should hold the right information
			if (BuildContext.Current!= null)
			{
				return BuildContext.Current.Command != BuildCommand.BuildProduction;
			} 
			return NeedleEngineBuildOptions.DevelopmentBuild;
		}
	}
}