#if UNITY_EDITOR
using System.IO;
using Needle.Engine.Codegen;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightType = UnityEngine.LightType;

namespace Needle.Engine.Components
{
	public static class MenuItems
	{
		[MenuItem(Constants.SetupSceneMenuItem, validate = true)]
		internal static bool CanSetupScene()
		{
			return !ExportInfo.Get();
		}

		[MenuItem(Constants.SetupSceneMenuItem, priority = Constants.MenuItemOrder + 4988)]
		[MenuItem("GameObject/Needle Engine 🌵/Add ExportInfo for web export", false, 41)]
		internal static void SetupScene()
		{
			var export = ExportInfo.Get();
			
			var sceneName = SceneManager.GetActiveScene().name;
			if (string.IsNullOrEmpty(sceneName)) sceneName = new DirectoryInfo(Application.dataPath + "/../").Name;
			
			if (!export)
			{
				var go = new GameObject("Export");
				go.transform.SetSiblingIndex(0);
				Undo.RegisterCreatedObjectUndo(go, "Create export GameObject");
				go.tag = "EditorOnly";
				export = Undo.AddComponent<ExportInfo>(go);
				export.DirectoryName = "Needle/" + sceneName;
				Debug.Log("Created " + nameof(ExportInfo) + " component and set web project path", export);
			}
			Selection.activeObject = export.gameObject;

			var componentGen = Object.FindAnyObjectByType<ComponentGenerator>();
			if (!componentGen) Undo.AddComponent<ComponentGenerator>(export.gameObject);
            
			var cam = Object.FindAnyObjectByType<Camera>();
			if (!cam)
			{
				var camGo = new GameObject("Camera");
				camGo.tag = "MainCamera";
				camGo.transform.position = new Vector3(0, 0, -3);
				Undo.RegisterCreatedObjectUndo(camGo, "Create camera GameObject");
				cam = Undo.AddComponent<Camera>(camGo);
				cam.nearClipPlane = 0.01f;
				cam.farClipPlane = 100;
				Undo.AddComponent<OrbitControls>(camGo);
				Debug.Log("Added Camera and OrbitControls", camGo);
			}
			else if (!cam.gameObject.TryGetComponent<OrbitControls>(out _))
			{
				Undo.AddComponent<OrbitControls>(cam.gameObject);
				Debug.Log("Added OrbitControls to camera", cam);
			}

			var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
				
			if (lights.Length <= 0)
			{
				var lightGo = new GameObject("Directional Light");
				Undo.RegisterCreatedObjectUndo(lightGo, "Create light GameObject");
				var light = Undo.AddComponent<Light>(lightGo);
				light.type = LightType.Directional;
				light.shadows = LightShadows.Hard;
				var lt = light.transform;
				var pos = lt.localPosition;
				pos.y = 1;
				lt.localPosition = pos;
				var euler = lt.eulerAngles;
				euler.x = 45;
				euler.y = 45;
				lt.eulerAngles = euler;
				Debug.Log("Created Directional Light", light);
			}

			Debug.Log("Setup scene complete 🌵", export);
			if (SceneView.lastActiveSceneView) 
				SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Scene setup for Needle Engine Web Export"), 5);
		}

		[MenuItem("GameObject/Needle Engine 🌵/Create GltfObject", false, 42)]
		internal static void CreateGltfObject()
		{
			var go = new GameObject();
			Undo.RegisterCreatedObjectUndo(go, "Created GltfObject GameObject");
			Undo.AddComponent<GltfObject>(go);
			Selection.activeObject = go;
		}
	}
}
#endif