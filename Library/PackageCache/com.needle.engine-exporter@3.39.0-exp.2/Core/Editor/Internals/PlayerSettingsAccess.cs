#if UNITY_EDITOR
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Needle.Engine
{
	public static class PlayerSettingsAccess
	{
		private static LightmapEncodingQuality LightmapQuality
		{
			get
			{
#if UNITY_2023_3_OR_NEWER
				return PlayerSettings.GetLightmapEncodingQualityForPlatform(EditorUserBuildSettings.activeBuildTarget);
#else
				return PlayerSettings.GetLightmapEncodingQualityForPlatformGroup(EditorUserBuildSettings.activeBuildTargetGroup);
#endif
			}
			set
			{
#if UNITY_2023_3_OR_NEWER
				PlayerSettings.SetLightmapEncodingQualityForPlatform(EditorUserBuildSettings.activeBuildTarget, value);
#else
				PlayerSettings.SetLightmapEncodingQualityForPlatformGroup(EditorUserBuildSettings.activeBuildTargetGroup, value);
#endif
			}
		}

		public static bool IsLightmapEncodingSetToNormalQuality()
		{
			return LightmapQuality == LightmapEncodingQuality.Normal;
		}

		public static string GetLightmapEncodingSettingName()
		{
			return LightmapQuality.ToString();
		}

		public static int GetLightmapEncodingSetting()
		{
			return (int) LightmapQuality;
		}

		public static void SetLightmapEncodingToNormalQuality()
		{
			LightmapQuality = LightmapEncodingQuality.Normal;
		}

		public static void SetLightmapEncodingQuality(int encoding)
		{
			LightmapQuality = (LightmapEncodingQuality) encoding;
		}
	}
}
#endif