namespace Needle.Engine
{
	public static class Constants
	{
		// Keep in sync with BuildTargetConstants
#if UNITY_2021_1_OR_NEWER
		public const string PlatformName = "EmbeddedLinux";
#else
		public const string PlatformName = "Lumin";
#endif
		
		public const string MenuItemRoot = "Needle Engine";
		public const string AssetsMenuItemRoot = "Assets/Needle Engine 🌵/";
		public const string GameObjectMenuItemRoot = "GameObject/Needle Engine 🌵/";
		
		public const int MenuItemOrder = 150;
		
		/// <summary>
		/// Needle Engine Unity Integration
		/// </summary>
		public const string UnityPackageName = "com.needle.engine-exporter";
		
		/// <summary>
		/// Needle Engine NPM Package name
		/// </summary>
		public const string RuntimeNpmPackageName = "@needle-tools/engine";

		public const string GltfBuildPipelineNpmPackageName = "@needle-tools/gltf-build-pipeline";
		public const string ComponentCompilerNpmPackageName = "@needle-tools/needle-component-compiler";
		public const string ToolsNpmPackageName = "@needle-tools/helper";

		public const string ExporterPackagePath = "Packages/" + UnityPackageName;
		public const string SamplesPackageName = "com.needle.engine-samples";
		public const string SamplesPackagePath = "Packages/" + SamplesPackageName;
		public const string TestPackagePath = "Packages/com.needle.engine-tests";
		
		
		public const string DocumentationUrl = "https://fwd.needle.tools/needle-engine/help";
		public const string DocumentationUrlCompression = "https://fwd.needle.tools/needle-engine/docs/compression";
		public const string DocumentationUrlDeployment = "https://fwd.needle.tools/needle-engine/docs/deployment";
		public const string DocumentationUrlScripting = "https://fwd.needle.tools/needle-engine/docs/scripting";
		public const string DocumentationUrlNodejs = "https://docs.needle.tools/nodejs";
		public const string DocumentationComponentGenerator = "https://fwd.needle.tools/needle-engine/docs/scripting#automatically-generating-unity-components-from-typescript-files";
		public const string DocumentationUrlNetworking = "https://fwd.needle.tools/needle-engine/docs/networking";
		public const string DocumentationUrlCustomShader = "https://fwd.needle.tools/needle-engine/docs/customshader";
		
		public const string EulaUrl = "https://needle.tools/eula";
		public const string FeedbackFormUrl = "https://engine.needle.tools/feedback";
		public const string IssuesUrl = "https://fwd.needle.tools/needle-engine/issues";
		public const string SamplesUrl = "https://engine.needle.tools/samples";
		public const string BuyLicenseUrl = "https://buy.needle.tools/needle-engine";
		public const string ManageLicenseUrl = "https://buy.needle.tools/needle-engine/manage";

		public const char ExternalLinkChar = '↗';
		
		public const string SearchTagDelimiter = "\0\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
		public const string NeedleComponentTags = SearchTagDelimiter + "Needle Engine";

		internal const string SetupSceneMenuItem = Constants.MenuItemRoot + "/Add Needle Engine Exporter to this Scene";
	}
}