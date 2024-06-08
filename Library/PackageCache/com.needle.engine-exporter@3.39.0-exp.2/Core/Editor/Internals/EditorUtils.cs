using UnityEditor;

namespace Needle.Engine
{
	public static class EditorAccess
	{
		public static float contextWidth
		{
			get
			{
				// IN-70302: EditorGUIUtility.currentViewWidth throws an exception on
				// Unity 2022.3+ and 2023.3+ inside the GetHeight() method of DecoratorDrawers
                // We could potentially also use Screen.width / EditorGUIUtility.pixelsPerPoint,
                // but it's not clear when that would be correct / when not.
				return (bool) (UnityEngine.Object) GUIView.current ? GUIView.current.position.width : 100.0f;
			}
		}
	}
}