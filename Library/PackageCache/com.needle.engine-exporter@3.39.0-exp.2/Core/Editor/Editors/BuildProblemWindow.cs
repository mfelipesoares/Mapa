#nullable enable
using System;
using Needle.Engine.Core;
using Needle.Engine.Problems;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Needle.Engine.Editors
{
	public class BuildProblemWindow : EditorWindow
	{
		[MenuItem(Constants.MenuItemRoot + "/Internal/Build Information")]
		public static void Open()
		{
			var existing = GetWindow<BuildProblemWindow>();
			if (existing)
			{
				existing.keepOpen = true;
				existing.Show();
			}
			else
			{
				existing = CreateInstance<BuildProblemWindow>();
				existing.keepOpen = true;
				existing.Show();
			}
		}

		private void OnEnable()
		{
			minSize = new Vector2(515, 430);
			maxSize = new Vector2(700, 700);
			titleContent = new GUIContent("Needle Build Information");

			var root = new VisualElement();
			var header = new VisualElement();
			UIComponents.BuildHeader(header);
			header.style.paddingBottom = 20;
			root.Add(header);
			var mainContainer = new IMGUIContainer();
			mainContainer.onGUIHandler += OnDrawUI;
			root.Add(mainContainer);
			VisualElementRegistry.Register(header);
			rootVisualElement.Add(root);
			rootVisualElement.style.paddingLeft = 10;
			rootVisualElement.style.paddingRight = 10;
			rootVisualElement.style.paddingBottom = 10;
		}

		private GUIStyle? _wrappedLabelStyle;
		private Vector2 scroll = Vector2.zero;
		private bool keepOpen = false;
		
		private void OnDrawUI()
		{
			
			_wrappedLabelStyle ??= new GUIStyle(EditorStyles.wordWrappedLabel);
			_wrappedLabelStyle.richText = true;
			
			if (!BuildResultInformation.HasAnyProblems && !keepOpen)
			{
				try
				{
					Close();
				}
				catch (InvalidOperationException)
				{
					
				}
				return;
			}
			
			using var scrollScope = new EditorGUILayout.ScrollViewScope(this.scroll);
			this.scroll = scrollScope.scrollPosition;

			// using (new GUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Your current scene uses some features not included in <b>Needle Engine Basic</b>.\nUpgrade to Needle Engine <b>Indie</b> or <b>Pro</b> to unlock these features. You can also modify your scene to stay within the limits.", _wrappedLabelStyle);
			}
			GUILayout.Space(10);
			if (GUILayout.Button("Purchase a commercial license " + Constants.ExternalLinkChar, GUILayout.Height(40)))
			{
				Application.OpenURL(Constants.BuyLicenseUrl);
			}

			GUILayout.Space(20);
			EditorGUILayout.LabelField("Usage Summary", EditorStyles.largeLabel);
			GUILayout.Space(5);
			
			foreach (var kvp in BuildResultInformation.GroupedByDescriptions)
			{
				var description = kvp.Key;
				var entries = kvp.Value;
				var didDrawLicense = false;
				foreach (var info in entries)
				{
					if (info.Severity != ProblemSeverity.Error) continue;
					using (new GUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField("• " + info.Message, _wrappedLabelStyle);
						DrawPingable(info.Context);
						GUILayout.FlexibleSpace();
						if (didDrawLicense) continue;
						didDrawLicense = true;
						EditorGUILayout.LabelField("<b>Indie / PRO</b>", _wrappedLabelStyle);
					}
				}
				using (new GUILayout.HorizontalScope())
				{
					GUILayout.Space(15);
					EditorGUILayout.LabelField("→ " + description, _wrappedLabelStyle);
				}
			}
			
			GUILayout.Space(20);
			GUILayout.FlexibleSpace();
		}

		private void DrawPingable(Object obj)
		{
			if (!obj) return;
			var lastRect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
			if (Event.current.button == 0 && Event.current.type == EventType.MouseDown &&
			    lastRect.Contains(Event.current.mousePosition))
			{
				Selection.activeObject = obj;
				EditorGUIUtility.PingObject(obj);
			}
		}
	}
}