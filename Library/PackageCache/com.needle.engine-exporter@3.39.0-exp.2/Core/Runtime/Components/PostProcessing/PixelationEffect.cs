using UnityEngine;

namespace Needle.Engine.Components.PostProcessing
{
	[AddComponentMenu("Needle Engine/Postprocessing/Pixelation Effect" + Needle.Engine.Constants.NeedleComponentTags)]
	public class PixelationEffect : PostProcessingEffect
	{
		public uint granularity = 30;
	}
}