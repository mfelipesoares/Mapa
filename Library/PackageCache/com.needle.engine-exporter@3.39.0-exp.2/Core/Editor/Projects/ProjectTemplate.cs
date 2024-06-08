using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Projects
{
	[CreateAssetMenu(menuName = Constants.MenuItemRoot + "/Project Template", order = Constants.MenuItemOrder)]
	public class ProjectTemplate : ScriptableObject
	{
		public string RemoteUrl;

		public int Priority = 0;

		public string Title = "";
		[Multiline(5)] public string Description;

		public string DisplayName => string.IsNullOrWhiteSpace(Title) ? name : Title;
		public string[] Links;
		[Tooltip("Npmdef dependencies")] public Object[] Dependencies;

		public string GetPath()
		{
			return Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
		}

		public string GetFullPath()
		{
			return Path.GetFullPath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this))!);
		}

		public bool HasPackageJson()
		{
			var path = GetPath();
			if (!File.Exists(path + "/package.json")) return false;
			return true;
		}

		public bool IsRemoteTemplate()
		{
			return !string.IsNullOrWhiteSpace(this.RemoteUrl) && GitActions.IsCloneable(this.RemoteUrl);
		}
	}

	[CustomEditor(typeof(ProjectTemplate))]
	internal class ProjectTemplateEditor : Editor
	{
		private string directory;

		private void OnEnable()
		{
			var path = AssetDatabase.GetAssetPath(target);
			if (string.IsNullOrWhiteSpace(path)) return;
			directory = Path.GetDirectoryName(path);
		}

		public override void OnInspectorGUI()
		{
			using var scope = new EditorGUI.ChangeCheckScope();
			// ComponentEditorUtils.DrawDefaultInspectorWithoutScriptField(this.serializedObject);
			base.OnInspectorGUI();
			
			GUILayout.Space(25);

			var template = (ProjectTemplate)target;
			if (template.IsRemoteTemplate())
			{
				EditorGUILayout.LabelField("Remote Template", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox(
					"This is a remote template. The content will be pulled from the repository when creating a new project.",
					MessageType.None);
			}
			else
			{
				if (directory != null)
				{
					EditorGUILayout.LabelField("Local Template", EditorStyles.boldLabel);
					EditorGUILayout.HelpBox("Copied Directory: " + directory, MessageType.None);

					GUILayout.Space(10);
					if (!File.Exists(directory + "/package.json"))
					{
						EditorGUILayout.HelpBox("Missing package.json", MessageType.Warning);
					}
					if (!File.Exists(directory + "/tsconfig.json"))
					{
						EditorGUILayout.HelpBox("Missing tsconfig.json", MessageType.Warning);
					}
				}
			}

			if (scope.changed)
			{
				ProjectGenerator.MarkTemplatesDirty();
			}
		}
	}
}