using UnityEngine;

namespace Needle.Engine.Components.Experimental
{
	[AddComponentMenu("Needle Engine/Character Controller Input" + Constants.NeedleComponentTags)]
	public class CharacterControllerInput : MonoBehaviour
	{
		[Info("This component is experimental and will likely change.\nDo not use in production yet!", InfoAttribute.InfoType.Warning)]
		public CharacterController controller;
		public Animator animator;
		[Tooltip("Meter per second")]
		public float movementSpeed = .3f;
		[Tooltip("Degrees per second")]
		public float rotationSpeed = 45;
		[Tooltip("Set to 0 to disable jump")]
		public float jumpForce = .5f;
		[Tooltip("Set to 0 to disable double jump")]
		public float doubleJumpForce = 1.0f;
		public bool lookForward = true;

		public void move(Vector2 move) { }
		public void look(Vector2 move) { }
		public void jump() { }

        private void OnValidate()
		{
			if (!controller)
				TryGetComponent(out controller);
		}
	}
}