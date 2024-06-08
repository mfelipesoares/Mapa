using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace Needle.Engine.UniversalRenderPipeline
{
	public static class VolumeEditorSync
	{
		 [InitializeOnLoadMethod]
		private static void Init()
		{
			EditorModificationListener.CreateCustomHook += OnCreateHook;
			EditorApplication.update += Update;
		}

		private static VolumeChangeTracker volumeTracker;
		private static int frame = 0;

		private static void Update()
		{
			if (frame++ % 10 != 0) return;
			volumeTracker?.Update();
		}

		private static void OnCreateHook(EditorModificationHookArguments obj)
		{
			if (obj.Editor is VolumeEditor vol)
			{
				obj.Used = true;
				var volume = vol.target as Volume;
				volumeTracker = new VolumeChangeTracker(volume, obj.PropertyChangedEvent);
			}
			
			volumeTracker?.Validate();
		}
	}
}