using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu(USDZExporter.ComponentMenuPrefix + "Change Material on Click" + USDZExporter.ComponentMenuTags)]
	public class ChangeMaterialOnClick : MonoBehaviour
	{
		public Material materialToSwitch;
		public Material variantMaterial;
		
		[Header("USDZ only")]
		public float fadeDuration = 0;
	}
}