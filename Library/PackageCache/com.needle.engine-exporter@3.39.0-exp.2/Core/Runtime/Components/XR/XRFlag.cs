using System;
using UnityEngine;

namespace Needle.Engine.Components
{
	[Flags]
	public enum XRStateFlag
	{
		Never = 0,
		Browser = 1 << 0,
		AR = 1 << 1,
		VR = 1 << 2,
		FirstPerson = 1 << 3,
		ThirdPerson = 1 << 4,
	}

	[AddComponentMenu("Needle Engine/XR/XR Flags" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
	[HelpURL(Constants.DocumentationUrl)]
	public class XRFlag : MonoBehaviour
	{
		public XRStateFlag VisibleIn = (XRStateFlag)(~0);

		[ContextMenu(nameof(VisibleIn))]
		private void Print()
		{
			Debug.Log((VisibleIn & (XRStateFlag.VR | XRStateFlag.FirstPerson)).ToString());
		}
	}
}