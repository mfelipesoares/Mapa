using UnityEditor;

namespace Needle.Engine
{
	public static class BuildWindowAccess
	{
		public static void ShowBuildWindowWithNeedleEngineSelected() 
		{
			EditorUserBuildSettings.selectedBuildTargetGroup = BuildPlatformConstants.BuildTargetGroup;
			EditorWindow.GetWindow<BuildPlayerWindow>(false, "Build Settings");
		}
	}
}