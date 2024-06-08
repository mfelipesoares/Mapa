using System;
using UnityEngine;

namespace Needle.Engine.Components.XR
{
	[AddComponentMenu("Needle Engine/XR/XR Controller Model (Rendering)" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
	public class XRControllerModel : MonoBehaviour
	{
		[Info("Add this component on the same GameObject as the WebXR component", InfoAttribute.InfoType.None, new []{ typeof(WebXR) })]
		[Tooltip("Enable to create controller models when using controllers in WebXR")]
		public bool CreateControllerModel = true;
		[Tooltip("Enable to create hand models when using hands in WebXR")]
		public bool CreateHandModel = true;

		[Header("Custom Hand Models")]
		public Transform customLeftHand;
		public Transform customRightHand;
	}
}