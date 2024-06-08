using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Needle.Engine.Components
{
	[AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Set Active on Click" + USDZExporter.ComponentMenuTags)]
	public class SetActiveOnClick : MonoBehaviour
	{
		[Tooltip("The target object that will be toggled on/off when clicking this object.")]
		public Transform target;
		[Tooltip("If true, the target object will be toggled on/off when clicking this object. Both targetState and hideSelf will be ignored.")]
		public bool toggleOnClick = false;
		[Tooltip("The target object will have this state after clicking this object.")]
		public bool targetState = true;
		[Tooltip("If true, this object will be hidden after clicking it.")]
		public bool hideSelf = true;

		private void OnDrawGizmosSelected()
		{
			if (!target) return;
			DrawLineAndBounds(transform, target);
		}

		internal static void DrawLineAndBounds(Transform from, Transform target)
		{
			if (!from || !target) return;
			
			var c = Gizmos.color;
			Gizmos.color = new Color(1,1,1,0.3f);
			DrawBounds(target, out var bounds);
			Gizmos.DrawLine(from.position, bounds.center);
			Gizmos.color = c;
		}
		
		internal static void DrawBounds(Transform target, out Bounds bounds)
		{
			if (!target) {
				bounds = new Bounds();
				return;
			}
			
			bounds = new Bounds(target.position, Vector3.zero);

			// get bounds of target
			var renderers = target.GetComponentsInChildren<Renderer>();
			if (!renderers.Any()) return;
			
			bounds = renderers[0].bounds;
			for (var i = 1; i < renderers.Length; i++)
				bounds.Encapsulate(renderers[i].bounds);
			
			// draw bounds
			Gizmos.DrawWireCube(bounds.center, bounds.size * 1.15f);
		}
	}
	
#if UNITY_EDITOR
	[CustomEditor(typeof(SetActiveOnClick))]
	internal class SetActiveOnClickEditor : Editor
	{
		private SerializedProperty targetObj;
		private SerializedProperty toggleOnClick;
		private SerializedProperty hideSelf;
		private SerializedProperty targetState;
		
		private void OnEnable()
		{
			targetObj = serializedObject.FindProperty(nameof(SetActiveOnClick.target));
			toggleOnClick = serializedObject.FindProperty(nameof(SetActiveOnClick.toggleOnClick));
			hideSelf = serializedObject.FindProperty(nameof(SetActiveOnClick.hideSelf));
			targetState = serializedObject.FindProperty(nameof(SetActiveOnClick.targetState));
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(targetObj);
			EditorGUILayout.PropertyField(toggleOnClick);
			
			// targetState and hideSelf don't have any effect when toggleOnClick is on, so disable them
			EditorGUI.BeginDisabledGroup(toggleOnClick.boolValue);
			EditorGUILayout.PropertyField(targetState);
			EditorGUILayout.PropertyField(hideSelf);
			EditorGUI.EndDisabledGroup();      
			
			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}