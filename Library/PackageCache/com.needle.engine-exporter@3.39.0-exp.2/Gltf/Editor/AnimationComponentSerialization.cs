using System.Collections.Generic;
using Needle.Engine.AdditionalData;
using UnityEngine;

namespace Needle.Engine.Gltf
{
	public class AnimationComponentSerialization : BaseAdditionalData
	{
		public override void GetAdditionalData(IExportContext context, object instance, List<(object key, object value)> additionalData)
		{
			if (instance is Animation anim && context is GltfExportContext gltfExportContext)
			{
				var list = new string[anim.GetClipCount()];
				var clips = UnityEditor.AnimationUtility.GetAnimationClips(anim.gameObject);
				var i = 0;
				foreach (AnimationClip clip in clips)
				{
					// TODO: calling AddAnimationClip here results in the clip being exported multiple times
					var index = gltfExportContext.Bridge.TryGetAnimationId(clip, anim.transform);
					if (index >= 0)
					{
						list[i] = index.AsAnimationPointer();
					}
					++i;
				}
				additionalData.Add(("clips", list));
			}
		}
	}
}