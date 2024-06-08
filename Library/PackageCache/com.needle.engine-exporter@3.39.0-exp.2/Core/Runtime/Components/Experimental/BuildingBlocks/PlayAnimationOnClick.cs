using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Needle.Engine.Components
{
	[AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Play Animation on Click" + USDZExporter.ComponentMenuTags)]
	public class PlayAnimationOnClick : MonoBehaviour
	{
		public Animator animator;
		[AnimatorStateName(nameof(animator))]
		public string stateName = "";

		private void OnDrawGizmosSelected()
		{
			if(animator)
				SetActiveOnClick.DrawLineAndBounds(transform, animator.transform);
		}
	}
	
	class AnimatorStateNameAttribute: PropertyAttribute
	{
		public string animatorName;
		public AnimatorStateNameAttribute(string animatorName)
		{
			this.animatorName = animatorName;
		}
	}
	
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(AnimatorStateNameAttribute))]
	public class AnimatorStateNameAttributeDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// First get the attribute since it contains the range for the slider
			var attrib = attribute as AnimatorStateNameAttribute;
			var animatorPropertyField = property.serializedObject.FindProperty(attrib.animatorName);
			var animator = animatorPropertyField.objectReferenceValue as Animator;
			if (!animator)
			{
				EditorGUI.PropertyField(position, property, new GUIContent(label.text + " (no Animator)"));
				return;
			}

			var controller = animator.runtimeAnimatorController as AnimatorController;
			if (!controller)
			{
				EditorGUI.PropertyField(position, property, new GUIContent(label.text + " (no AnimatorController)"));
				return;
			}

			var states = controller.layers[0].stateMachine.states;
			
			// show dropdown with these state names
			var names = new GUIContent[states.Length + 1];
			var currentIndex = names.Length - 1; // "" → "None" by default
			
			for (var i = 0; i < states.Length; i++)
			{
				names[i] = new GUIContent(states[i].state.name);
				if (names[i].text == property.stringValue)
					currentIndex = i;
			}
			names[names.Length - 1] = new GUIContent("None");

			var newValue = EditorGUI.Popup(position, label, currentIndex, names);
			if (newValue != currentIndex)
			{
				if (newValue == names.Length - 1)
					property.stringValue = "";
				else if (newValue >= 0 && newValue < states.Length)
					property.stringValue = names[newValue].text;
			}
		}
	}
#endif
}