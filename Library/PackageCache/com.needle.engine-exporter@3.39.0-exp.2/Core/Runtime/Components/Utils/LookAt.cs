// experimental - seems like there are some issues when exporting this as a RectTransform, so we don't do it right now
// #define USE_DRIVEN_PROPERTIES

using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;

namespace Needle.Engine.Components
{
	[AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Look At" + USDZExporter.ComponentMenuTags)]
	[ExecuteAlways]
	public class LookAt : MonoBehaviour
	{
		[Tooltip("When null, will look at the main camera")]
		public Transform target;
		public bool invertForward = false;
		[Tooltip("Will keep the up vector at (0,1,0) when set to true")]
		public bool keepUpDirection = true;
		[Tooltip("When true, will face the target's normal plane (e.g. camera plane) instead of directly looking at it.")]
		public bool copyTargetRotation = false;

		[Header("Editor Settings")] 
		[JsonIgnore] 
		public bool updateInEditor = false;
		
		private bool isDriven = false;
#if USE_DRIVEN_PROPERTIES
		private DrivenRectTransformTracker tracker;
#endif

		private void OnEnable()
		{
			if (updateInEditor)
				SetUpDrivenProperties();
		}

		private void OnValidate()
		{
			if (updateInEditor && enabled)
				SetUpDrivenProperties();
			else
				StopDrivenProperties();
		}

		private void SetUpDrivenProperties()
		{
			if (isDriven) return;
			isDriven = true;
			
#if USE_DRIVEN_PROPERTIES
			tracker = new DrivenRectTransformTracker();
			if (!TryGetComponent<RectTransform>(out var rectTransform)) {
				rectTransform = gameObject.AddComponent<RectTransform>();
			}
			tracker.Add(this, rectTransform, DrivenTransformProperties.Rotation);
#endif

			Camera.onPreRender += OnBeforeRender;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private void StopDrivenProperties()
		{
			Camera.onPreRender -= OnBeforeRender;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			
#if USE_DRIVEN_PROPERTIES
			tracker.Clear();
#endif
			isDriven = false;
		}

		private void OnDisable()
		{
			StopDrivenProperties();
		}

		private void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
		{
			OnBeforeRender(arg2);
		}

		private void OnBeforeRender(Camera cam)
		{
			if (!isDriven) return;
			
			var lookTarget = target ? target : cam.transform;
			
			try
			{
				// it may be destroyed
				if (!lookTarget) return;
				
				if (!transform)
				{
					StopDrivenProperties();
					return;
				}
			}
			catch (MissingReferenceException)
			{
				StopDrivenProperties();
				return;
			}
			
			var pos = lookTarget.position;
			if (copyTargetRotation)
			{
				transform.rotation = lookTarget.rotation * Quaternion.Euler(0, 180, 0);
			}
			else
			{
				if (keepUpDirection)
					pos.y = transform.position.y;
				
				transform.LookAt(pos);
			}
			
			if (invertForward)
				transform.rotation *= Quaternion.Euler(0, 180, 0);
		}
	}
}