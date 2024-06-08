#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;
using Task = System.Threading.Tasks.Task;

namespace Needle.Engine
{
	public interface IComponentHeaderDrawer
	{
		VisualElement CreateVisualElement(Editor editor);
	}

	public static class ComponentHeaderLinks
	{
		private static readonly IList<IComponentHeaderDrawer> _drawers = new List<IComponentHeaderDrawer>();

		public static void Register(IComponentHeaderDrawer drawer)
		{
			_drawers.Add(drawer);
		}

		private static readonly FieldInfo editorElementHeaderField =
			typeof(EditorElement).GetField("m_Header", BindingFlags.Instance | BindingFlags.NonPublic);

		[InitializeOnLoadMethod]
		private static void Init()
		{
			InspectorHook.Inject += OnInject;
			Editor.finishedDefaultHeaderGUI += OnDidDrawHeader;
		}

		private static void OnDidDrawHeader(Editor obj)
		{
			// EditorGUILayout.LabelField("TEST");
		}

		public static int IndexOf(Editor editor)
		{
			for (var i = 0; i < ActiveEditorTracker.sharedTracker.activeEditors.Length; i++)
			{
				if (ActiveEditorTracker.sharedTracker.activeEditors[i] == editor)
					return i;
			}
			return 0;
		}


		private static async void OnInject(Editor editor, VisualElement arg2)
		{
			if (editor is GameObjectInspector) return;
			if (editor is MaterialEditor) return;
			if ((editor.target is Component) == false) return;

			if (arg2 is EditorElement el)
			{
				var header = editorElementHeaderField.GetValue(el);
				if (header is IMGUIContainer container)
				{
					await Task.Delay(5);
					var width = container.layout.width;

					var customHeaders = new VisualElement();
					customHeaders.style.display = DisplayStyle.Flex;
					customHeaders.style.right = 0;
					customHeaders.style.alignItems = Align.FlexEnd;
					customHeaders.style.justifyContent = Justify.FlexEnd;
					customHeaders.style.flexDirection = FlexDirection.Row;
					customHeaders.style.alignContent = Align.FlexEnd;
					customHeaders.style.flexWrap = Wrap.NoWrap;
					// customHeaders.style.flexGrow = 1;
					customHeaders.style.flexShrink = 1;
					// customHeaders.style.backgroundColor = new Color(1, 0, 0, 0.2f);
					customHeaders.style.maxHeight = 20f;
					customHeaders.style.minHeight = 17f;
					customHeaders.style.paddingTop = 4.5f;
					customHeaders.style.paddingRight = 65;
					customHeaders.style.paddingBottom = 2.5f;
					customHeaders.pickingMode = PickingMode.Ignore;

					if (editor.target is Transform)
						customHeaders.style.marginTop = -7;


					foreach (var drawer in _drawers)
					{
						var elem = drawer.CreateVisualElement(editor);
						if (elem != null)
						{
							elem.style.marginLeft = 5;
							customHeaders.Add(elem);
						}
					}
					if (customHeaders.childCount > 0)
						container.Add(customHeaders);


					// var height = container.layout.height;
					// var top = container.layout.y;
					// if (editor.target is Transform)
					// 	top -= 7;
					// const int paddingRight = 60;
					// var rightEdge = width - paddingRight;
					// var rect = new Rect(rightEdge - 20, top + 2.4f, 16, 16);
					// var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
					// 	AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f"));
					//
					// var index = IndexOf(editor);
					// var wasExpanded = ActiveEditorTracker.sharedTracker.GetVisible(index);

					// var index = ComponentHeaderLinks.IndexOf(editor);
					// var wasExpanded = ActiveEditorTracker.sharedTracker.GetVisible(index);
					// container.onGUIHandler += () =>
					// {
					// 	GUI.Label(rect, logo);
					// 	if (Event.current.button == 0 && Event.current.type == EventType.Used)
					// 	{
					// 		if (rect.Contains(Event.current.mousePosition))
					// 		{
					// 			Application.OpenURL("https://docs.needle.tools");
					// 			var state = wasExpanded;
					// 			ActiveEditorTracker.sharedTracker.SetVisible(index, state);
					// 			InternalEditorUtility.SetIsInspectorExpanded(editor.target, state > 0);
					// 		}
					// 	}
					// 	wasExpanded = ActiveEditorTracker.sharedTracker.GetVisible(index);
					// };
				}
			}
		}
	}
}

#endif