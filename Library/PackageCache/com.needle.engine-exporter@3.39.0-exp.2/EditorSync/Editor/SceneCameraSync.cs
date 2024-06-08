using System;
using System.Collections.Generic;
using Needle.Engine.EditorSync.Utils;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Object = UnityEngine.Object;

#if UNITY_2020_3
using UnityEditor.SceneManagement;
#else
using UnityEditor.SceneManagement;
#endif

namespace Needle.Engine.EditorSync
{
	internal static class SceneCameraSync
	{
		private static Texture2D _editorSyncCircle;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorApplication.update += OnUpdate;
			SceneView.duringSceneGui += OnSceneGUI;

			_editorSyncCircle =
				AssetDatabase.LoadAssetAtPath<Texture2D>(
					AssetDatabase.GUIDToAssetPath("2e4455f843379fe4ba7d942d735773a2"));
			EditorSceneManager.sceneClosing += OnSceneClosing;
		}

		private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool _)
		{
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "enabled", false);
		}

		private static NeedleEditorSync comp => SyncSettings.NeedleEditorSync;

		private static int frame = 0;
		private static List<PropertyWatcher> watchers = new List<PropertyWatcher>();

		private static void OnUpdate()
		{
			frame += 1;

			// Only send updates when editor is focused
			if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) return;
			if (!SyncSettings.Enabled) return;


			if (frame % 10 == 0)
			{
				SceneView.RepaintAll();
			}

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				if (prefabStage.mode == PrefabStage.Mode.InIsolation)
					return;
			}

			var sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null) return;
			if (sceneView.hasFocus == false && frame % 300 != 0) return;
			if (WebEditorConnection.CanSend == false) return;


			// TODO: optimize to only send updates on changes.
			if (frame % 10 != 0) return;

			if (!comp) return;

			// if (PropertyWatcher.Update(watchers, 0, comp.enabled))
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "enabled", comp.sceneCamera);

			var cam = sceneView.camera;
			// if (PropertyWatcher.Update(watchers, 1, cam.nearClipPlane))
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "near", cam.nearClipPlane);
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "far", cam.farClipPlane);

			var t = cam.transform;
			var pos = t.localPosition;
			TransformUtils.ToThreePosition(ref pos);
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "position",
				new JRaw(JsonUtility.ToJson(pos)));
			var rot = t.localRotation;
			TransformUtils.ToThreeQuaternion(ref rot);
			rot *= Quaternion.Euler(0, 180, 0);
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "rotation",
				new JRaw(JsonUtility.ToJson(rot)));
			WebEditorConnection.SendPropertyModifiedInEditor("scene-camera", "fov", new JRaw(cam.fieldOfView));
		}


		private static GUIContent _cameraSyncGuiContent;
		private static GUIStyle _cameraSyncGuiStyle;
		private static DateTime _nextChangeTime;
		private static bool _lastBlinkValue;

		private static void OnSceneGUI(SceneView obj)
		{
			if (!SyncSettings.Installed) return;

			// TODO: could be a floating panel in 2022 or so
			if (!comp) return;
			if (!comp.enabled) return;


			if (_cameraSyncGuiContent == null)
			{
				_cameraSyncGuiContent =
					new GUIContent("Camera Sync", "Click to toggle Needle Engine scene camera sync");
			}

			if (_editorSyncCircle)
				// if (Event.current.type == EventType.Repaint)
			{
				Handles.BeginGUI();
				const float size = 10;
				var circleRect = new Rect(10, 10, size, size);
				var circleActive = true;
				var syncIsEnabled = comp.sceneCamera;

				if (syncIsEnabled)
				{
					if (DateTime.Now > _nextChangeTime)
					{
						_lastBlinkValue = !_lastBlinkValue;
						_nextChangeTime = DateTime.Now.AddSeconds(.8f);
					}
					circleActive = _lastBlinkValue;
				}

				if (circleActive)
				{
					var color = syncIsEnabled ? Color.red : new Color(.55f, .55f, .55f);
					GUI.DrawTexture(circleRect, _editorSyncCircle, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
				}
				comp.sceneCamera = EditorGUI.Toggle(circleRect, syncIsEnabled, GUIStyle.none);
				EditorGUIUtility.AddCursorRect(circleRect, MouseCursor.Link);

				var textRect = new Rect(circleRect);

				// label
				textRect.x += 15;
				textRect.y -= 5.6f;
				textRect.width = 200;
				textRect.height = 20;

				// background
				// var bgRect = rect;
				// bgRect.x -= 2;
				// bgRect.y -= 2;
				// bgRect.width += 4;
				// bgRect.height += 4;
				// GUI.color = new Color(.2f, .2f, .2f, 1);
				// GUI.Box(bgRect, GUIContent.none, EditorStyles.helpBox);
				GUI.color = new Color(.3f, .3f, .3f);
				if (syncIsEnabled) _cameraSyncGuiContent.text = "Camera Sync: ON";
				else _cameraSyncGuiContent.text = "Camera Sync: OFF";
				GUI.Label(textRect, _cameraSyncGuiContent);
				comp.sceneCamera = EditorGUI.Toggle(textRect, comp.sceneCamera, GUIStyle.none);
				EditorGUIUtility.AddCursorRect(textRect, MouseCursor.Link);


				Handles.EndGUI();
			}
		}
	}
}