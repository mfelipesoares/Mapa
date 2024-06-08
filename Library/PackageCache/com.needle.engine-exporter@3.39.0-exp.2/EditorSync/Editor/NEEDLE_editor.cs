using System.Collections.Generic;
using Needle.Engine.Utils;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Needle.Engine.EditorSync
{
	public class NEEDLE_editor
	{
		public string id;

		public static bool TryGetId(object obj, out string id)
		{
			return obj.TryGetEditorId(out id);
		}
	}

	internal static class NEEDLE_editor_utils
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorSceneManager.activeSceneChangedInEditMode += (_, e) => ClearCaches();
		}

		[MenuItem("CONTEXT/Component/Needle Engine/Print Editor GUID")]
		private static void CalculateGuid(MenuCommand cmd)
		{
			if (!cmd.context) return;
			if(TryGetEditorId(cmd.context, out var id)) Debug.Log("Editor ID for " + cmd.context.name + " is " + id, cmd.context);
			else Debug.Log("Could not calculate Editor ID for " + cmd.context.name, cmd.context);
		}

		private static readonly Dictionary<string, GameObject> loadedAssetsCache = new Dictionary<string, GameObject>();
		private static readonly Dictionary<object, string> previouslyResolved = new Dictionary<object, string>();
		private static readonly ProfilerMarker calculateGuidMarker = new ProfilerMarker("NeedleEditor CalculateGuid");

		private static void ClearCaches()
		{
			loadedAssetsCache.Clear();
			previouslyResolved.Clear();
		}
		
		public static bool TryGetEditorId(this object obj, out string id)
		{
			if (obj == null)
			{
				id = null;
				return false;
			}
			
			if (previouslyResolved.TryGetValue(obj, out id))
			{
				NeedleDebug.Log(TracingScenario.EditorSync, "EditorSync*: " + obj + ", " + id);
				return true;
			}

			using (calculateGuidMarker.Auto())
			{
				if (obj is Object unityObject && unityObject)
				{
					if (unityObject.TryResolvePrefabGuid(out id))
					{
						// done :)  
					}
					else
					{
						// These are written into the extension on export, we can use the cheap id here
						// if(unityObject is Material || unityObject is Transform)
						// 	id = unityObject.GetInstanceID().ToString();
						// else 
						id = unityObject.GetId();
						NeedleDebug.Log(TracingScenario.EditorSync, "EditorSync: " + unityObject + ", " + id);
					}
				}
				previouslyResolved.Add(obj, id);
				return !string.IsNullOrEmpty(id);
			}
		}

		private static bool TryResolvePrefabGuid(this Object unityObject, out string id)
		{
			var transform = GetTransform(unityObject);
			var wasComponent = unityObject is Component && !(unityObject is Transform);
			var originalObject = unityObject;
			
			if (!transform)
			{
				id = null;
				return false;
			}
			
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null)
			{
				// When we are editing objects in a prefab stage
				// we need to make sure we get the guid from the original prefab asset
				// Otherwise the exported guid and the edited guid are not matching
				var root = stage.prefabContentsRoot.transform;
				var rootPath = stage.assetPath;
				if (!loadedAssetsCache.TryGetValue(rootPath, out var prefab))
				{
					prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rootPath);
					loadedAssetsCache.Add(rootPath, prefab);
				}
				var path = new List<int>();
				FindPath(transform, root, path);
				var res = ResolvePath(prefab.transform, path) as Component;
				// If the original object was a component make sure to
				if (originalObject && wasComponent)
				{
					res.TryGetComponent(originalObject.GetType(), out var comp);
					res = comp;
				}
				id = res.GetId();
				return true;
				
				// if (transform.parent)
				// {
				// 	var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.parent.gameObject);
				// 	if (nearestRoot && nearestRoot != root.gameObject)
				// 	{
				// 		// id = null;
				// 		// return false;
				// 		// root = nearestRoot.transform;
				// 		// var asset = PrefabUtility.GetCorrespondingObjectFromSource(root);
				// 		// rootPath = AssetDatabase.GetAssetPath(asset);
				// 	}
				// }
				// if (root)
				{
				}
			}
			
			// If we export a prefab we always want to get the original object
			// if (PrefabUtility.IsPartOfPrefabInstance(unityObject))
			// {
			// 	if (unityObject is Component comp) unityObject = comp.gameObject;
			//
			// 	if (PrefabUtility.IsOutermostPrefabInstanceRoot(unityObject as GameObject))
			// 	{
			// 	}
			// 		
			// 	// TODO: this doesnt work yet with a prefab that contains multiple instances of another prefab. All the exported objects will receive the same prefab guid then instead of the guid of their respective instance
			// 	// https://github.com/needle-mirror/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Editor/Mono/Inspector/PropertyEditor.cs#L749
			// 	// if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			// 	{
			// 		// 	if (unityObject is GameObject go) unityObject = go.transform;
			// 		// 	if (unityObject is Transform tr && tr.parent)
			// 		// 	{
			// 		// 		var root = PrefabUtility.GetNearestPrefabInstanceRoot(tr.parent);
			// 		// 		var c = PrefabUtility.GetCorrespondingObjectFromSource(unityObject);
			// 		// 	}
			// 		var cor = PrefabUtility.GetCorrespondingObjectFromSource(unityObject);
			// 		if (cor) unityObject = cor;
			// 		
			// 		// id = unityObject.GetId();
			// 		// return true;
			// 	}
			// }

			id = null;
			return false;
		}

		private static Transform GetTransform(Object obj)
		{
			if (obj is GameObject go) return go.transform;
			if (obj is Transform tr) return tr;
			if (obj is Component comp) return comp.transform;
			return null;
		}

		private static void FindPath(Transform obj, Transform root, List<int> path)
		{
			if (!obj) return;

			var index = obj.GetSiblingIndex();
			path.Add(index);
			
			if (obj.parent == root)
			{
				return;
			}

			FindPath(obj.parent, root, path);
		}

		private static Transform ResolvePath(Transform root, List<int> path)
		{
			var current = root;
			for (var i = path.Count - 1; i >= 0; i--)
			{
				var index = path[i];
				if (index >= current.childCount)
				{
					break;
				}
				current = current.GetChild(index);
			}
			return current;
		}
	}
}