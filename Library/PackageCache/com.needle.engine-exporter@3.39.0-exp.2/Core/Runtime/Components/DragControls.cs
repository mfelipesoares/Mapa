using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Drag Controls" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class DragControls : MonoBehaviour
	{
		[Tooltip("How and where the object is dragged along.")]
		public DragMode dragMode = DragMode.DynamicViewAngle;

		[Tooltip("Snap dragged objects to a XYZ grid – 0 means: no snapping.")]
		public float snapGridResolution = 0f;
    
		[Tooltip("Keep the original rotation of the dragged object.")]
		public bool keepRotation = true;
    
		[Header("XR")]
		
		[Tooltip("How and where the object is dragged along while dragging in XR.")]
		public DragMode xrDragMode = DragMode.Attached;
		
		[Tooltip("Keep the original rotation of the dragged object while dragging in XR.")]
		public bool xrKeepRotation = false;
		
		[Tooltip("Accelerate dragging objects closer / further away when in XR")]
		public float xrDistanceDragFactor = 1;
		
		[Header("Visualization")]
		[Tooltip("When enabled, draws a line from the dragged object downwards to the next raycast hit.")]
		public bool showGizmo = false;
	}

	public enum DragMode
	{
		/** Object stays at the same horizontal plane as it started. Commonly used for objects on the floor */
		XZPlane = 0,
		/** Object is dragged as if it was attached to the pointer. In 2D, that means it's dragged along the camera screen plane. In XR, it's dragged by the controller/hand. */
		Attached = 1,
		/** Object is dragged along the initial raycast hit normal. */
		HitNormal = 2,
		/** Combination of XZ and Screen based on the viewing angle. Low angles result in Screen dragging and higher angles in XZ dragging. */
		DynamicViewAngle = 3,
		/** The drag plane is adjusted dynamically while dragging. */
		SnapToSurfaces = 4,
		/** Don't allow dragging the object */
		None = 5,
	} 
}