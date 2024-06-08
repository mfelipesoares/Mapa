using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{
	[AddComponentMenu("Needle Engine/Postprocessing/Screenspace Ambient Occlusion" + Needle.Engine.Constants.NeedleComponentTags + " AO")]
	public class ScreenSpaceAmbientOcclusion : PostProcessingEffect
	{
		public float intensity = 2f;
		public float falloff = 1f;
		[Range(1, 20)]
		public int samples = 9;
		public Color color = Color.black;
		public float luminanceInfluence = 0.7f;
	}
}