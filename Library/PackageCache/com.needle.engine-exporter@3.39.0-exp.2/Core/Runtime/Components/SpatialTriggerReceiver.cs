using UnityEngine;
using UnityEngine.Events;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Spatial Trigger Receiver" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class SpatialTriggerReceiver : MonoBehaviour
	{
		public LayerMask TriggerMask;
		public UnityEvent OnEnter, OnStay, OnExit;
	}
}