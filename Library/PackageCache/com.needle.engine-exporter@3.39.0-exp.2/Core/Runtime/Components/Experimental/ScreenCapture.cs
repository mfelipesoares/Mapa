using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public enum ScreenCaptureDevice {
	Screen = 0,
	Camera = 1,
	WebglCanvas = 2,
	Microphone = 3,
}

[AddComponentMenu("Needle Engine/Screencapture" + Needle.Engine.Constants.NeedleComponentTags)]
public class ScreenCapture : MonoBehaviour
{
	[Tooltip("When enabled the component will start sending a stream when it becomes active (enabled in the scene)")]
	public bool autoConnect = false;
	
	[Tooltip("When assigned the video stream will be displayed by this player")]
	public VideoPlayer videoPlayer;
	[Tooltip("The device to capture from e.g. the screen, a camera or a microphone")]
	public ScreenCaptureDevice device = ScreenCaptureDevice.Screen;
	[Tooltip("This can be the deviceId or device label of the camera to be used. Make sure to select `device.Camera`")]
	public string deviceName;
	
	[Header("Experimental")]
	[Tooltip("If true the object this component is attached to can be clicked to start or end the stream")]
	public bool allowStartOnClick = true;
    
	public void Share() {}
	public void Close() {}

	#if UNITY_EDITOR
	[CustomEditor(typeof(ScreenCapture))]
	internal class ScreenCaptureEditor : Editor
	{
		private string[] devices; 
		
		const string anyLabel = "Any";
			
		private void OnEnable()
		{
			var webcamsAndCameras = WebCamTexture.devices;
			devices = new string[webcamsAndCameras.Length+1];
			devices[0] = anyLabel;
			for (var i = 0; i < webcamsAndCameras.Length; i++)
			{
				devices[i+1] = webcamsAndCameras[i].name;
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (devices.Length > 1)
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField("Available devices:", EditorStyles.boldLabel);
				var screenCapture = (ScreenCapture) target;
				if (screenCapture.device == ScreenCaptureDevice.Camera)
				{
					var currentIndex = Array.IndexOf(devices, screenCapture.deviceName);
					if (currentIndex < 0) currentIndex = 0;
					using(var change = new EditorGUI.ChangeCheckScope())
					{
						currentIndex = EditorGUILayout.Popup("Camera", currentIndex, devices);
						if (change.changed)
						{
							screenCapture.deviceName = devices[currentIndex];
							if(screenCapture.deviceName == anyLabel) screenCapture.deviceName = null;
						}
					}
				}
			}
		}
	}
	#endif
}

