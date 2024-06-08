using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Editors
{
	public class CameraInspectorExtension : ComponentEditorExtension
	{
		public override bool ShouldExtend(Object target)
		{
			return target is Camera;
		}

		public override void OnInspectorGUI(Object target)
		{
			if (target is Camera cam)
			{
				if (cam.backgroundColor.a == 0 && cam.clearFlags == CameraClearFlags.SolidColor)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(5);
						EditorGUILayout.HelpBox("Needle Engine Warning:\nCamera Background is set to solid color but color alpha is zero! The color will not be visible in the WebGl scene and instead the background color of your website will be used.\nTo use the background color defined in Unity you need to set the alpha to 1 (values lower than one will be mixed with the website background color)", MessageType.Warning);
						GUILayout.Space(5);
					}
					GUILayout.Space(12);
				}
			}
		}
	}
}