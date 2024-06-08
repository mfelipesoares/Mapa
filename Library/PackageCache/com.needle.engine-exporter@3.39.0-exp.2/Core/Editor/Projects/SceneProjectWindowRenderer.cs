using System;
using System.Collections.Generic;
using System.Linq;
using Needle.Engine.Editors;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Projects
{
	class AssetDbModifications: UnityEditor.AssetModificationProcessor
	{
		private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
		{
			SceneProjectWindowRenderer.GuidToPath.Remove(sourcePath);
			SceneProjectWindowRenderer.GuidToPath.Remove(destinationPath);
			return AssetMoveResult.DidNotMove;
		}

		private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			SceneProjectWindowRenderer.GuidToPath.Remove(assetPath);
			return AssetDeleteResult.DidNotDelete;
		}
	}
	
	internal static class SceneProjectWindowRenderer
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItem;
		}

		private static GUIStyle style;

		internal static readonly Dictionary<string, string> GuidToPath = new Dictionary<string, string>();

		private static void OnProjectWindowItem(string guid, Rect rect)
		{
			if (style == null)
			{
				style = new GUIStyle(EditorStyles.label);
				style.alignment = TextAnchor.MiddleRight;
				style.fontStyle = FontStyle.Bold;
			}

			if (!GuidToPath.TryGetValue(guid, out var path))
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				GuidToPath[guid] = path;
			}
			
			if (path.EndsWith(".unity", StringComparison.Ordinal))
			{
				var selected = Selection.assetGUIDs.Contains(guid);
				if (ProjectsData.TryGetScene(path, out var data))
				{
					if (data.Projects.Count > 0)
					{
						if (Event.current.type == EventType.ContextClick)
						{
							if (rect.Contains(Event.current.mousePosition))
							{
								// Event.current.Use();
								// var menu = new GenericMenu();
								// menu.AddItem(new GUIContent("Show in Explorer"), false, () =>
								// {
								// 	EditorUtility.RevealInFinder(path);
								// });
								// menu.AddItem(new GUIContent("Open Scene: " + Path.GetFileNameWithoutExtension(path)), false, () =>
								// {
								// 	EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
								// });
								// foreach (var project in data.Projects)
								// {
								// 	var pp = project.ProjectPath.Split('/');
								// 	var folderName = pp.LastOrDefault();
								// 	var projectPath = pp.Length >= 2 ? pp[pp.Length - 2] : null;
								// 	var text =  "Web Project: " + projectPath + " | " + folderName;
								// 	menu.AddItem(new GUIContent(text + "/Open Directory"), false, () =>
								// 	{
								// 		EditorUtility.RevealInFinder(project.PackageJsonPath);	
								// 	});
								// 	menu.AddItem(new GUIContent(text + "/Open package.json"), false, () =>
								// 	{
								// 		EditorUtility.OpenWithDefaultApp(project.PackageJsonPath);	
								// 	});
								// 	var workspace = project.VSCodeWorkspace;
								// 	if (workspace != null && workspace.Exists)
								// 	{
								// 		menu.AddItem(new GUIContent(text + "/Open Workspace"), false, () =>
								// 		{
								// 			EditorUtility.OpenWithDefaultApp(workspace.FullName);	
								// 		});
								// 	}
								// }
								// menu.ShowAsContext();
							}
						}
						else
						{
							var isSingleLine = Mathf.Approximately(rect.height, 16);
							var maxSize = Mathf.Min(20, rect.height);
							rect.height = maxSize;
							
							rect.width -= 5;
							rect.height -= 4;
							rect.y += 2;

							if (!isSingleLine)
							{
								rect.x += 5;
								rect.y -= 2;
							}

							var unselectedColor = Color.white;
							if (isSingleLine) unselectedColor.a = .6f;
							using (new ColorScope(selected ? Color.white : unselectedColor))
							{
								GUI.Label(rect, new GUIContent(Assets.Logo, string.Join("\n", data.Projects.Select(p => p.ProjectPath))), style);
							}
						}
					}
				}
			}
		}
	}
}