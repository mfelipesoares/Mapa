using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Samples
{
	[CreateAssetMenu(menuName = "Needle Engine/Samples/Sample Info")]
	internal class SampleInfo : ScriptableObject
	{
		[UsedImplicitly]
		public string Name
		{
			get => DisplayNameOrName;
			set => DisplayName = value;
		}
		
		[JsonIgnore]
		public string DisplayName;
		public string Description;
		public Texture2D Thumbnail;
		[JsonIgnore]
		public SceneAsset Scene;
		public string LiveUrl;
		public int Priority;
        [JsonIgnore]
		public string DisplayNameOrName => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : ObjectNames.NicifyVariableName(name);
		public Tag[] Tags;
		
		[JsonIgnore][HideInInspector]
		public SampleInfo reference;

		/// <summary>
		/// Git directory relative path to the scene
		/// </summary>
		[JsonProperty("ReadmeUrl")]
		private string ReadmeUrl
		{
			get
			{
				if (!Scene) return "";
				var sceneAssetPath = AssetDatabase.GetAssetPath(Scene);
				if (sceneAssetPath == null)
				{
					Debug.LogError("Missing scene asset path...", this);
					return "";
				}
				return Constants.RepoRoot + MakeGitDirectoryRelative(sceneAssetPath) + "/README.md";
			}
		}

		private string MakeGitDirectoryRelative(string str)
		{
			var abs = Path.GetDirectoryName(Path.GetFullPath(str))?.Replace("\\", "/");
			var dirInfo = new FileInfo(str).Directory;
			var gitDirectory = dirInfo;
			while (gitDirectory != null && gitDirectory.Exists)
			{
				var fileOrDir = gitDirectory.GetFileSystemInfos(".git");
				if (fileOrDir.Length > 0)
				{
					break;
				}
				gitDirectory = gitDirectory.Parent;
			}
			var gitPath = gitDirectory?.FullName.Replace("\\", "/");
			var gitRelative = abs.Substring(gitPath!.Length);
			while (gitRelative.StartsWith("/"))
				gitRelative = gitRelative.Substring(1);
			return gitRelative;
		}

		private void OnValidate()
		{
			if (!Scene)
			{
				var path = AssetDatabase.GetAssetPath(this);
				if (string.IsNullOrWhiteSpace(path)) return;
				var scenes = AssetDatabase.FindAssets("t:SceneAsset", new[] { Path.GetDirectoryName(path) });
				foreach (var guid in scenes)
				{
					var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
					Scene = scene;
					if (scene)
						break;
				}
			}
		}

#if UNITY_EDITOR
		[OnOpenAsset(100)]
		private static bool OpenAsset(int instanceID, int line)
		{ 
			// Handle SampleInfo assets
			if (EditorUtility.InstanceIDToObject(instanceID) is SampleInfo sampleInfo)
			{
				SamplesWindow.OpenScene(sampleInfo.Scene);
				return true;
			}

			// Handle scenes that are part of a sample / immutable
			if (EditorUtility.InstanceIDToObject(instanceID) is SceneAsset sceneAsset)
			{
				var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
				if (PackageUtils.IsMutable(scenePath))
					return false;
				SamplesWindow.OpenScene(sceneAsset);
				return true;
			}
        
			return false;
		}
#endif

		public override string ToString()
		{
			return DisplayNameOrName + " – " + name;
		}
	}
	    
#if UNITY_EDITOR
	[CustomEditor(typeof(SampleInfo), true)]
	[CanEditMultipleObjects]
	class SampleInfoEditor : Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			// if we have multiple sample assets selected just use the default inspector (for editing multiple tags at once)
			if (targets.Length > 1) return null;
			var t = target as SampleInfo;
			if (!t) return new Label("<null>");

			var isSubAsset = AssetDatabase.IsSubAsset(t);
			var v = new VisualElement();
			foreach (var style in SamplesWindow.StyleSheet)
				v.styleSheets.Add(style);

			if (!EditorGUIUtility.isProSkin) v.AddToClassList("__light");
			v.Add(new SamplesWindow.Sample(t));

			if (!isSubAsset)
			{
				var container = new IMGUIContainer(() => DrawDefaultInspector());
				container.style.marginTop = 20;
				v.Add(container);
			}
			else 
				v.style.maxHeight = 500;
			
			return v;
		}
	}
#endif
}