using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	public static class Assets
	{
		private static Texture2D glitchRemixIcon = null;

		internal static Texture2D GlitchRemixIcon
		{
			get
			{
				if (!glitchRemixIcon)
					glitchRemixIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(@"e42e7092484fddc4f97ff5b33ba54620"));
				return glitchRemixIcon;
			}
		}
		
		private static Texture2D githubLogoIcon = null;
		internal static Texture2D GithubPageDeployLogo
		{
			get
			{
				if (!githubLogoIcon)
					githubLogoIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(@"5c178313feda6b546b07f5713eb9d8a6"));
				return githubLogoIcon;
			}
		}
	}
}