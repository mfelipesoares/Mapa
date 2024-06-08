using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	public static class ProfilerAccess
	{
		public static void SetProfilerEnabled(bool enabled)
		{
#if UNITY_EDITOR
			var window = EditorWindow.GetWindow<ProfilerWindow>();
			if (window == null)
			{
				if (enabled) window = ScriptableObject.CreateInstance<ProfilerWindow>();
				else return;
			}

			window.SetRecordingEnabled(enabled);
#endif
		}

		public static long GetCurrentFrame()
		{
#if UNITY_EDITOR
			var window = EditorWindow.GetWindow<ProfilerWindow>();
			if (window) return window.lastAvailableFrameIndex;
#endif
			return -1;
		}

		public static void SetCurrentFrame(long frame)
		{
#if UNITY_EDITOR
			var window = EditorWindow.GetWindow<ProfilerWindow>();
			if (window) window.SetActiveVisibleFrameIndex((int)frame);
#endif
		}
	}
}