#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	public static class AnimationWindowUtil
	{
		public static bool IsPreviewing()
		{
			var animationWindow = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault(w => w);
			if (animationWindow && animationWindow.state != null)
			{
				return animationWindow.state.previewing;
			}
			return false;
		}
		
		public static bool StartPreview()
		{
			var animationWindow = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault(w => w);
			if (animationWindow && animationWindow.state?.canPreview == true && animationWindow.state.activeAnimationClip)
			{
#if UNITY_2023_1_OR_NEWER
				animationWindow.state.previewing = true;
#else
				animationWindow.state.StartPreview();
#endif
				return true;
			}
			return false;
		}

		public static void StopPreview()
		{
			var animationWindow = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault(w => w);
			if (animationWindow)
			{
#if UNITY_2023_1_OR_NEWER
				if (animationWindow.state != null) animationWindow.state.previewing = false;
#else
				animationWindow.state?.StopPreview();
#endif
			}
		}
	}
}

#endif