using System.Collections.Generic;
using System.Runtime;
using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Scene Switcher" + Needle.Engine.Constants.NeedleComponentTags)]
	public class SceneSwitcher : MonoBehaviour
	{
		public bool autoLoadFirstScene = true;
		
		[Tooltip("Assign Prefabs or SceneAssets"), Info("Assign Prefabs or SceneAssets")]
		public List<AssetReference> scenes = new List<AssetReference>();
		
		[Tooltip("Optional: a scene to be displayed while loading other scenes")]
		public AssetReference loadingScene;

		[Tooltip("the url parameter that is set/used to store the currently loaded scene in, set to \"\" to disable")]
		public string queryParameterName = "scene";
		[Tooltip("When enabled the scene name will be stored in the Url to switch between scenes")]
		public bool useSceneName = true;
		[Tooltip("When enabled, the scene switcher will clamp the index to the range of available scenes, otherwise it will wrap around (e.g. -1 will be the last scene)")]
		public bool clamp = false;
		[Tooltip("when enabled the new scene is pushed to the browser navigation history, only works with a valid query parameter set")]
		public bool useHistory = true;
		[Tooltip("when enabled you can switch between scenes using keyboard left, right, A and D or number keys")]
		public bool useKeyboard = true;
		[Tooltip("when enabled you can switch between scenes using swipe (mobile only)")]
		public bool useSwipe = true;
		[Tooltip("when enabled will automatically apply the environment scene lights")]
		public bool useSceneLighting = true;
		
		[Header("Preload")]
		[Tooltip("How many scenes after the currently active scene should be preloaded")]
		public uint preloadNext = 1;
		[Tooltip("How many scenes before the currently active scene should be preloaded")]
		public uint preloadPrevious = 1;
		[Tooltip("How many scenes can be preloaded in parallel")]
		public uint preloadConcurrent = 2;

		public void selectNext() {}
		public void selectPrev() {}
		public void select(int index) {}
		public void select(Transform t) {}
		#if UNITY_EDITOR
		public void select(UnityEditor.SceneAsset t) {}
		#endif

		public void OnEnable()
		{
			
		}
		
		// #if UNITY_EDITOR
		// [CustomEditor(typeof(SceneSwitcher))]
		// private class SceneSwitcherEditor : Editor
		// {
		// 	private ReorderableList list;
		//
		// 	private void OnEnable()
		// 	{
		// 		list = new ReorderableList(serializedObject, serializedObject.FindProperty("scenes"), true, true, true, true);
		// 		list.onAddDropdownCallback += OnDrop;
		// 		list.onMouseDragCallback += OnDrag;
		// 		ReorderableList.
		// 	}
		//
		// 	private void OnDrag(ReorderableList reorderableList)
		// 	{
		// 		
		// 	}
		//
		// 	private void OnDrop(Rect buttonrect, ReorderableList reorderableList)
		// 	{
		// 		
		// 	}
		//
		// 	public override void OnInspectorGUI()
		// 	{
		// 		base.OnInspectorGUI();
		// 		
		// 		list.DoLayoutList();
		// 		
		// 	}
		// } 
		// #endif
	}
	
}