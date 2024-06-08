using UnityEngine;
using UnityEngine.Animations;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Camera/Orbit Controls" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class OrbitControls : MonoBehaviour
	{
		[Tooltip("When enabled OrbitControls will automatically raycast find a look at target in start")]
		public bool autoTarget = true;
		[Tooltip("When enabled the scene content will be automatically fit into the view")]
		public bool autoFit = false;
		
		[Header("Rotation")]
		public bool enableRotate = true;
        public bool autoRotate = false;
		public float autoRotateSpeed = .2f;
		[Range(0, Mathf.PI)]
		public float minPolarAngle = 0;
		[Range(0, Mathf.PI)]
		public float maxPolarAngle = Mathf.PI;
		[Tooltip("How far you can orbit horizontally, lower limit. If set, the interval [ min, max ] must be a sub-interval of [ - 2 PI, 2 PI ] with ( max - min < 2 PI ). Default: Infinity")]
		public float minAzimuthAngle = Mathf.Infinity;
		[Tooltip("How far you can orbit horizontally, upper limit. If set, the interval [ min, max ] must be a sub-interval of [ - 2 PI, 2 PI ], with ( max - min < 2 PI ). Default: Infinity")]
		public float maxAzimuthAngle = Mathf.Infinity;
		[Header("Zoom")]
		public bool enableZoom = true;
		public float minZoom = .1f;
		public float maxZoom = 500;
		public float zoomSpeed = 1;
		public bool zoomToCursor = false;
		
		[Header("Pan")]
		public bool enablePan = true;
		
		[Header("Smoothing")]
		public bool enableDamping = true;
		[Range(0.001f, 1), Tooltip("Low values translate to more damping")]
		public float dampingFactor = .1f;
		[Tooltip("Duration in seconds, used when lerping to a target position"), Range(0.01f, 10)]
		public float targetLerpDuration = 1;
		
		[Header("Input")]
		public bool enableKeys = false;
		public bool middleClickToFocus = true;
		public bool doubleClickToFocus = true;
		[Range(-1, 3), Tooltip("Click the background nth times to fit the scene. E.g. if set to 2, the scene will be fit on the second click in a short amount of time. Set to -1 to disable")]
		public int clickBackgroundToFitScene = 2;
		[Tooltip("When enabled user input will interrupt camera auto rotation and any target animation (e.g. if camera is set to animate to a specific point)")]
		public bool allowInterrupt = true;

		[Header("Constrain Look At")]
		public LookAtConstraint lookAtConstraint;


		private void OnEnable()
		{
			
		}

		public void SetCameraAndLookTarget(Transform obj) {}
		public void SetCameraTargetPosition(Transform obj) {}
		public void SetLookTargetPosition(Transform obj) {}
		public void FitCamera() {}
	}
}