using UnityEngine;

namespace Needle.Engine.Components.XR
{
	[AddComponentMenu("Needle Engine/XR/XR Controller Movement (Input)" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
	public class XRControllerMovement : MonoBehaviour
	{
		[Tooltip("The speed at which the user will move when using the thumbstick")]
		public float MovementSpeed = 1;
		
		[Tooltip("The speed at which the user will rotate when using the thumbstick")]
		[Range(0, 180)] public float RotationStep = 60;

		[Tooltip("When enabled using the right controller's thumbstick y axis will be used to teleport\nWhen disabled teleportation will be allowed on any object the controller is pointing to")]
		public bool UseTeleport = true;
		
		[Tooltip("When enabled teleportation will only work on objects that have a TeleportTarget component on themselves or in the parent hierarchy")]
		public bool UseTeleportTarget = false;
		
		[Tooltip("When enabled the scene will fade to black when teleporting")]
		public bool UseTeleportFade = false;

		[Header("Visualization")]
		[Tooltip("When enabled rays will be drawn to visualize the controller direction")]
		public bool ShowRays = true;
		[Tooltip("When enabled a hit disc will be rendered at the object where the controller is pointing at")]
		public bool ShowHits = true;
	}
}