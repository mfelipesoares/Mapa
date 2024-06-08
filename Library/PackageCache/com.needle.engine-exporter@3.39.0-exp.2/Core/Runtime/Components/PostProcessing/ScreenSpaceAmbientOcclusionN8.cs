using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{
	[AddComponentMenu("Needle Engine/Postprocessing/Screenspace Ambient Occlusion N8" + Needle.Engine.Constants.NeedleComponentTags + " AO")]
	public class ScreenSpaceAmbientOcclusionN8 : PostProcessingEffect
	{
		public bool gammaCorrection = true;
		public float intensity = 1f;
		public float falloff = 1f;
		public float aoRadius = 5f;
		public bool screenspaceRadius = false;
		public Color color = Color.black;
		
		public QualityMode quality = QualityMode.Medium;

		public enum QualityMode
		{
			Performance = 0,
			Low = 1,
			Medium = 2,
			High = 3,
			Ultra = 4,
		}
	}
}