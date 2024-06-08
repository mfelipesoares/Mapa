using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Change Transform on Click" + USDZExporter.ComponentMenuTags)]
	public class ChangeTransformOnClick : MonoBehaviour
	{
		[Tooltip("The object being moved")]
		public Transform @object;
		[Tooltip("Where the object moves to. When relativeMotion is on, this specifies the offset in local space.")]
		public Transform target;

		public float duration = 1f;
		public bool relativeMotion = false;

		private void OnDrawGizmosSelected()
		{
			if (!@object || !target) return;
			SetActiveOnClick.DrawLineAndBounds(target, @object);
		}
	}
}