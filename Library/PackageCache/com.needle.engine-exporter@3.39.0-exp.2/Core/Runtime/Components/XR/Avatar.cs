using UnityEngine;

namespace Needle.Engine.Components.XR
{
	[AddComponentMenu("Needle Engine/XR/Avatar" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
	public class Avatar : MonoBehaviour
	{
		public GameObject Head;
		public GameObject LeftHand;
		public GameObject RightHand;
	}
}