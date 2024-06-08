using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{
	[AddComponentMenu("Needle Engine/Postprocessing/Tiltshift Effect" + Needle.Engine.Constants.NeedleComponentTags)]
	public class TiltShiftEffect : PostProcessingEffect
	{
		[Range(-1, 1)]
		public float offset = 0;
		[Range(0f, 3.141f)]
		public float rotation = 0;
		[Range(0.01f, 1)]
		public float focusArea = .4f;
		[Range(0.01f, 1)]
		public float feather = .3f;
		public KernelSize kernelSize = KernelSize.LARGE;
		[Range(0.01f, 1)]
		public float resolutionScale = 1f;

		public enum KernelSize
		{
			VERY_SMALL,
			SMALL,
			MEDIUM,
			LARGE,
			VERY_LARGE,
			HUGE,
		}

	}
}