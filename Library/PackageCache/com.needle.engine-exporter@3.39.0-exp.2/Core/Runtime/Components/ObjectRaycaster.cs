using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Object Raycaster" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class ObjectRaycaster : MonoBehaviour
	{
		[Tooltip("When enabled raycasting will ignore SkinnedMeshes")]
		public bool IgnoreSkinnedMeshes = false;
	}
}