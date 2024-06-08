using UnityEngine;

namespace Needle.Engine.AdditionalData
{
	public class SpriteRendererData : AdditionalComponentData<SpriteRenderer>
	{
		public bool transparent = true;
		[Range(0,1), Tooltip("Default is 0, alpha values lower than this value will be cut away. Will also affect the shape of shadows if enabled")]
		public float cutoutThreshold = 0f;
		public bool castShadows = false;
	}
}