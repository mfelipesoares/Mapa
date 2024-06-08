using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{ 
	public abstract class PostProcessingEffect : MonoBehaviour
	{
		// public string type => "custom-effect";
		public bool active => enabled;
		
		private void OnEnable()
		{
			// for the UI
		}
		
#if UNITY_EDITOR
		[CustomEditor(typeof(PostProcessingEffect), true)]
		private class PPEditor : Editor
		{
			private static readonly List<Component> components = new List<Component>();
			
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var comp = target as PostProcessingEffect;
				if (comp == null) return;
				components.Clear();
				comp.GetComponents(components);
				if (!components.Any(c => c.GetType().Name == "Volume"))
				{
					EditorGUILayout.HelpBox("Missing Volume Component: Please add a Volume Component for the effect to work", MessageType.Warning);
				}
			}
		}
#endif
	}
	
}