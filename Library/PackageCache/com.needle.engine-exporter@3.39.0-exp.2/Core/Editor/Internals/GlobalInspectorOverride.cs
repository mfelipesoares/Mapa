#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace Needle.Engine
{
	public static class InspectorHook
	{
		public static event Action<Editor, VisualElement> Inject;

		public static void Rebuild() => UpdateInspector();
		
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			// EditorApplication.delayCall += UpdateInspector;
			EditorApplication.hierarchyChanged += UpdateInspector;
			ActiveEditorTracker.editorTrackerRebuilt += OnEditorTrackerRebuilt;
			/*
			var lastShader = default(Shader);
			Editor.finishedDefaultHeaderGUI += (e) =>
			{
				var m = e.target as Material;
				if (!m) return;
				var s = m.shader;
				if (s != lastShader)
				{
					lastShader = s;
					EditorApplication.delayCall += () =>
					{
						ActiveEditorTracker.sharedTracker?.ForceRebuild();
					};
				}
			};
			*/

			// var active = Selection.objects.ToArray();
			// Selection.objects = Array.Empty<Object>();
			// Selection.objects = active;
			
			await Task.Yield();
			ActiveEditorTracker.sharedTracker?.ForceRebuild();
		}

		private static void OnEditorTrackerRebuilt()
		{
			UpdateInspector(); 
		}

		private const string InjectionClassName = "__needle_threejs";
		
		private static void UpdateInspector()
		{
			if (Inject == null) return;
			var openEditors = ActiveEditorTracker.sharedTracker.activeEditors;
			foreach (var ed in openEditors) 
			{
				var inspectors = InspectorWindow.GetInspectors(); 
				foreach (var ins in inspectors)
				{
					// var editorElements = ins.rootVisualElement.Query<EditorElement>().ToList();
					ins.rootVisualElement.Query<EditorElement>().ForEach(editorElement =>
					{
						if (editorElement.editor != ed) return;
						if (editorElement.ClassListContains(InjectionClassName)) return;
						editorElement.AddToClassList(InjectionClassName);
						try
						{
							Inject?.Invoke(ed, editorElement);
							ed.Repaint();
						}
						catch (Exception e)
						{
							Debug.LogException(e);
						}
					});
				}
			}
		}
	}
}
#endif