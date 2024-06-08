using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Needle.Engine.EditorSync
{
	[HelpURL(Constants.DocumentationUrl)]
	public class NeedleEditorSync : MonoBehaviour
	{
		internal static event Action Validate;
		
		[Tooltip("Turn off to disable the editor sync")]
		public new bool enabled = true;

		[Header("Synchronize")] 
		[Tooltip("Enable to synchronize component modifications. Might be slow in large scenes. Adding or removing components is currently not supported")]
		public bool components = true;
		[Tooltip("Enable to synchronize material modifications")]
		public bool materials = true;
		[Tooltip("Enable to synchronize the editor scene camera view with the runtime camera (disable to return to the default scene camera)")]
		public bool sceneCamera;

		private void OnValidate()
		{
			Validate?.Invoke();
		}
	}

	public class NeedleEditorConfig : IBuildConfigProperty
	{
		public string Key => "needleEditor";
		
		public object GetValue(string projectDirectory)
		{
			return Object.FindObjectsByType<NeedleEditorSync>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				.FirstOrDefault();
		}
	}
}
