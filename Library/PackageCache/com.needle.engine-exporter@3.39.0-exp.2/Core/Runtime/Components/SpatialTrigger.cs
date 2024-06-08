using System;
using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Spatial Trigger" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class SpatialTrigger : MonoBehaviour
	{
		public LayerMask TriggerMask;

		private void OnDrawGizmos()
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
		}
	}
}