using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Needle.Engine.AdditionalData;
using Needle.Engine.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Needle.Engine.Utils
{
	public static class SceneExportUtils
	{
		public static bool IsValidExportScene(out string directory, out ExportInfo info)
		{
			directory = null;
			info = ExportInfo.Get();
			if (!info) return false;
			if (!info.IsValidDirectory()) return false;
			directory = Path.GetFullPath(Builder.BasePath + "/" + info.DirectoryName);
			return Directory.Exists(directory);
		}

		public static bool TryGetGameObject(Object obj, out GameObject go)
		{
			go = obj as GameObject;
			if (go)
			{
				return true;
			}

			go = (obj as Component)?.gameObject;
			if (go)
			{
				return true;
			}
			
			return false;
		}

		public static bool TryGetInstanceForExport(Object obj, out GameObject root, out Action dispose)
		{
			root = null;
			dispose = null;

			if (TryCreateInstance(obj, out root, out dispose))
				return true;


			if (TryGetGameObject(obj, out root)) 
				return true;

			return false;
		}

		public static bool TryCreateInstance(Object obj, out GameObject root, out Action dispose)
		{
			if (obj is SceneAsset scene)
			{
				var scenePath = AssetDatabase.GetAssetPath(scene);
				var active = SceneManager.GetActiveScene();
				var exportingActiveScene = active.path == scenePath;
				
				// we can not export the root export scene as a scene asset reference at the moment
				if (scenePath == Builder.RootScene.path && !exportingActiveScene)
				{
					Debug.LogError("Exporting you main scene \"" + scenePath +
					               "\" as a scene reference in another sub-scene is not exported. Please remove the reference in \"" +
					               active.path + "\".");
					root = null;
					dispose = null;
					return false;
				}

				Scene sceneToExport;
				ParentScope parents = default;
				LightScope lights = default;
				GameObject tempObject = default;
				var openedScene = false;
				var createdSceneAssetPath = default(string);
				var sceneWasOpenBefore = false;
				var sceneWasLoadedBefore = false;
				// True if we opened a new scene and now need to check if this scene needs to be baked... 
				var mightNeedToBakeLightmaps = false;
				var sceneDirectory = "Assets/Needle/Scenes";

				if (exportingActiveScene) 
				{
					sceneToExport = active;
				}
				else
				{
					lights = new LightScope(Object.FindObjectsByType<Light>(FindObjectsSortMode.None));
					var path = AssetDatabase.GetAssetPath(scene);
					if (Path.GetFullPath(path).Contains("PackageCache"))
					{
						Directory.CreateDirectory(sceneDirectory);
						createdSceneAssetPath = sceneDirectory + "/" + scene.name + ".unity";
						File.Copy(path, createdSceneAssetPath, true);
						Debug.Log($"Copied scene to {createdSceneAssetPath} for export (temporarily) because it can not opened from an immutable package");
						AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
						path = createdSceneAssetPath;
					}
					var openScene = SceneManager.GetSceneByPath(path);
					sceneWasOpenBefore = openScene.IsValid();
					sceneWasLoadedBefore = openScene.isLoaded;
					if (sceneWasLoadedBefore)
					{
						EditorSceneManager.CloseScene(openScene, true);
					}
					sceneToExport = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
					SceneManager.SetActiveScene(sceneToExport);
					openedScene = true;
					mightNeedToBakeLightmaps = true;
				}

				var lightmaps = LightmapSettings.lightmaps;
				var roots = sceneToExport.GetRootGameObjects();
				parents = new ParentScope(roots);
				tempObject = new GameObject();
				tempObject.name = scene.name;
				root = tempObject;
				
				var didBakeLightmapsDuringExport = false;

				if (mightNeedToBakeLightmaps)
				{
					// check if the scene is using lightmaps and needs to be baked
					var hasAnyLightmaps = lightmaps.Any(l => l.lightmapColor);
					if (!hasAnyLightmaps)
					{
						
						var hasAnyLightsSetToBake = roots.Any(go =>
							go.GetComponentsInChildren<Light>(false)
								.Any(l => l.lightmapBakeType != LightmapBakeType.Realtime));
						if (hasAnyLightsSetToBake) 
						{
							var msg = "Bake Lightmaps for " + scene.name + "...";
							Debug.Log(msg + "\n" + sceneToExport.path);
							SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(msg));
							didBakeLightmapsDuringExport = true;
							var successfullyBaked = Lightmapping.Bake();
							if (successfullyBaked && sceneToExport.IsValid() && sceneToExport.isDirty)
							{
								Debug.Log($"Saving scene after baking lightmaps... ({scene.name})");
								AssetDatabase.Refresh();
								EditorSceneManager.SaveScene(sceneToExport);  
							}
						}
					}
				}

				if (openedScene)
				{
					FogSettings.Create(root);
				}

				foreach (var sceneObj in roots)
				{
					sceneObj.transform.SetParent(root.transform, true);
				}

				dispose = () =>
				{
					if (exportingActiveScene)
						parents.Dispose();

					lights.Dispose();
					tempObject.SafeDestroy();

					if (!exportingActiveScene && sceneToExport.IsValid())
					{
						if (active.IsValid()) SceneManager.SetActiveScene(active);
						EditorSceneManager.CloseScene(sceneToExport, sceneWasOpenBefore == false);
						if (sceneWasLoadedBefore)
						{
							EditorSceneManager.OpenScene(sceneToExport.path, OpenSceneMode.Additive);
						}
					}

					if (didBakeLightmapsDuringExport)
					{
						// set lightmaps back to original
						LightmapSettings.lightmaps = lightmaps;
						Debug.LogWarning("Lightmaps were baked during export:" + sceneToExport.name);
					}
				};
				return true;
			}

			if (PrefabUtility.IsPartOfPrefabAsset(obj))
			{
				// Only instantiate prefabs if the reference is a Transform or a GameObject. Otherwise it's a reference to a component on some asset or another custom asset / asset type which we also just want to export normally
				if (obj is GameObject || obj is Transform)
				{
					var asset = obj as GameObject;
					if(obj is Component c_) asset = c_.gameObject;
					var t = asset.transform;
					var isRoot = t.parent == null;
					var shouldInstantiate = false;
					if (isRoot)
					{
						foreach (Transform child in t)
						{
							if (shouldInstantiate) break;
							
							// create instance in scene if it contains a nested gltf
							var nestedExports = child.GetComponentInChildren<IExportableObject>(true);
							if (nestedExports != null)
							{
								shouldInstantiate = true;
							}
						}
					}

					// We dont want to instantiate prefabs every time (currently only if it contains a nested gltf)
					// because we need to modify the hierarchy. 
					if (shouldInstantiate)
					{
						// we do this so we can modify the hierarchy of the prefab at export time
						// currently we basically only need it for nested gltf's (a GltfObject in a prefab / exported glb)
						// because we create a new object and do reparenting which doesnt work when its a prefab asset
						var instance = PrefabUtility.InstantiatePrefab(obj);
						
						if (instance is Component c)
						{
							root = c.gameObject;
						}
						else
						{
							root = instance as GameObject;
						}
						
						
						// If a component in a prefab asset was referenced we get a prefab instance of the whole thing in our scene
						// so we want to destroy the whole instance again
						var objectToDestroy = PrefabUtility.GetOutermostPrefabInstanceRoot(instance);
						if (!objectToDestroy && instance is Component comp) objectToDestroy = comp.gameObject;
						objectToDestroy.hideFlags = HideFlags.HideAndDontSave;
						dispose = () => { Object.DestroyImmediate(objectToDestroy); };
						
						return true;
					}
					
				}
			}

			root = null;
			dispose = null;
			return false;
		}

		private readonly struct ParentScope : IDisposable
		{
			private readonly IList<GameObject> objects;
			private readonly IList<Transform> previousParents;

			public ParentScope(IList<GameObject> objects)
			{
				this.objects = objects;
				previousParents = this.objects.Select(o => o.transform.parent).ToArray();
			}

			public void Dispose()
			{
				if (objects == null) return;
				for (var i = 0; i < objects.Count; i++)
				{
					GameObject obj = objects[i];
					Transform prev = previousParents[i];
					if (!obj) continue;
					if (prev != null && !prev) continue; // if we had a previous parent but it doesnt exist anymore
					obj.transform.SetParent(prev, true);
				}
			}
		}

		private readonly struct LightScope : IDisposable
		{
			private readonly IList<Light> lightsInScene;
			private readonly IList<bool> prevEnabled;

			public LightScope(IList<Light> lights)
			{
				this.lightsInScene = lights;
				this.prevEnabled = lightsInScene.Select(l => l.enabled).ToArray();
				foreach (var light in lights) light.enabled = false;
			}

			public void Dispose()
			{
				if (lightsInScene == null) return;
				for (var i = 0; i < lightsInScene.Count; i++)
				{
					var light = lightsInScene[i];
					if (!light) continue;
					lightsInScene[i].enabled = prevEnabled[i];
				}
			}
		}
	}
}