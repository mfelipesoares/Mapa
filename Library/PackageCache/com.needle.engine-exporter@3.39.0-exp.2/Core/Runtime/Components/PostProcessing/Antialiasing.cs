using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{
	[AddComponentMenu("Needle Engine/Postprocessing/Antialiasing" + Needle.Engine.Constants.NeedleComponentTags)]
	public class Antialiasing : PostProcessingEffect
	{
		public enum Quality
		{
			Low = 0,
			Medium = 1,
			High = 2,
			Ultra = 3
		}
		
		public Quality Preset = Quality.Medium;
		
		// [Range(0.01f, .1f)]
		// public float edgeDetectionThreshold = .01f;
	}
}