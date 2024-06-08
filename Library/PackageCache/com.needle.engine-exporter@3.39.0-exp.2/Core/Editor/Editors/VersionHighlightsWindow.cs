using System.Collections.Generic;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors
{
	public class VersionHighlightsWindow : EditorWindow, ICanRebuild
	{
		[MenuItem(Constants.MenuItemRoot + "/Version Highlights ✨", false, Constants.MenuItemOrder - 994)]
		[MenuItem("Help/Needle Engine/Version Highlights", false)]
		private static void OpenMenuItem() => Open(true, true);
		
		[MenuItem("Help/Needle Engine/Internal/Highlights in newer releases", false)]
		private static void OpenMenuItem_Future() => Open(true, true);
		
		
		
		[InitializeOnLoadMethod]
		private static void Init()
		{
			CheckIfVersionHighlightWindowShouldOpen();
			EditorSceneManager.sceneOpened += (a, b) => CheckIfVersionHighlightWindowShouldOpen();
		}

		private static async void CheckIfVersionHighlightWindowShouldOpen()
		{
			while (EditorApplication.isUpdating || EditorApplication.isCompiling) await Task.Delay(500);
			if (!ExportInfo.Get()) return;
			if (UpdateVersions())
			{
				await Task.Delay(1000);
				VersionsUtil.WriteVersionInstalled();
				Open(false);
			}
		}
		
		

		private static string current;
		private static string previous;
		/// <summary>
		/// Update static versions and returns true if the version has changed
		/// </summary>
		private static bool UpdateVersions()
		{
			return VersionsUtil.VersionChanged(out current, out previous);
		}


		private bool openedByUserAction = false;
		
		public static void Open(bool openedActively, bool showFutureHighlights = false)
		{
			var window = GetWindow<VersionHighlightsWindow>();
			if (window)
			{
				window.showFutureHighlights = showFutureHighlights;
				window.openedByUserAction = openedActively;
				window.Show();
				window.OnEnable();
			}
			else
			{
				window = CreateInstance<VersionHighlightsWindow>();
				window.showFutureHighlights = showFutureHighlights;
				window.openedByUserAction = openedActively;
				window.Show();
			}
		}

		private async void OnEnable()
		{
			UpdateVersions();

			// Logo
			var logo = AssetDatabase.LoadAssetAtPath<Texture>(
				AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f"));
			titleContent = new GUIContent("Version Highlights", logo);
			minSize = new Vector2(400, 400);
			maxSize = new Vector2(1200, 1000); 
			VisualElementRegistry.Register(rootVisualElement);
			// Window
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("16d541f707f349c4ad4eeb1d6d7ad1c4"), this);
			// Highlight uxml
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("f673e8102c4148a98f359b8ab7d88e42"), this);
			// Highlight style uxml
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("7948367020f7491b8d33a1e405895eb4"), this);
			// header separator
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("a7e86041c1304515bdb27eac6e02567f"), this);

			await Task.Delay(1);
			Rebuild();
		}

		private List<VersionHighlight> highlights = null;
		private bool showFutureHighlights = false;

		private void OnFocus()
		{
			UpdateVersions();
			highlights?.Clear();
			if(openedByUserAction) Rebuild();
		}

		public void Rebuild()
		{
			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					AssetDatabase.GUIDToAssetPath("16d541f707f349c4ad4eeb1d6d7ad1c4"));
			var ui = uiAsset.CloneTree();
			if (ui != null)
			{
				ui.AddToClassList("main");
				if (!EditorGUIUtility.isProSkin) ui.AddToClassList("__light");
				rootVisualElement.Clear();
				rootVisualElement.Add(ui);
				VisualElementRegistry.HookEvents(ui);

				var licenseVersionLabel = ui.Q<Label>(null, "version");
				if (licenseVersionLabel != null)
				{
					licenseVersionLabel.text = current;
				}

				var content = ui.Q(null, "content");

				if (content != null && GetHighlights(out highlights) && highlights.Count > 0)
				{
					content.Clear();
					var highlightElement =
						AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
							AssetDatabase.GUIDToAssetPath("f673e8102c4148a98f359b8ab7d88e42"));
					var versionSeparator =
						AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
							AssetDatabase.GUIDToAssetPath("a7e86041c1304515bdb27eac6e02567f"));
					var lastVersion = default(string);
					foreach (var highlight in highlights)
					{
						if (lastVersion != highlight.Version)
						{
							var separator = versionSeparator.CloneTree();
							separator.Q<TextElement>(null, "version").text = highlight.Version;
							content.Add(separator);
						}
						lastVersion = highlight.Version;

						var highlightUI = highlightElement.CloneTree();
						highlightUI.Q<TextElement>(null, "title").text = highlight.Title;
						var body = highlightUI.Q<VisualElement>(null, "body");
						body.Clear();
						// TODO: convert markdown links to rich-text links
						var text = new TextElement()
						{
							text = highlight.Text
						};
						text.AddToClassList("text");
						body.Add(text);
						content.Add(highlightUI);
					}
				}
				else if (!openedByUserAction)
				{
					Task.Delay(1).ContinueWith(t =>
					{
						if (this) Close();
						
					}, TaskScheduler.FromCurrentSynchronizationContext());
				}
			}
		}

		private bool GetHighlights(out List<VersionHighlight> list)
		{
			if (highlights != null && highlights.Count > 0)
			{
				list = highlights;
				return true;
			}

			// If a user opens the window we want to show future and previous highlights
			// We yet need a visual distinction between the two tho and maybe a way to paginate at some point
			if (openedByUserAction)
			{
				var previousVersion = previous;
				if (current == previousVersion && openedByUserAction) previousVersion = "0.0.0";
				var all = new List<VersionHighlight>();
				if (VersionsUtil.TryGetFutureVersionHighlights(out var future))
					all.AddRange(future);
				if(VersionsUtil.TryGetPreviousVersionHighlights(current, previousVersion, out var prev))
					all.AddRange(prev);
				list = all;
				return list.Count > 0;
			}
			
			if (showFutureHighlights)
			{
				return VersionsUtil.TryGetFutureVersionHighlights(out list);
			}
			return VersionsUtil.TryGetPreviousVersionHighlights(current, previous, out list);
		}
	}
}