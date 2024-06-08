using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public interface IGltfImporterBridge
	{
		Task<Material> GetMaterial(int index);
		Task<AnimationClip> GetAnimation(int index);
	}

	internal readonly struct UnityGltfImporterBridge : IGltfImporterBridge
	{
		private readonly GLTFSceneImporter importer;

		public UnityGltfImporterBridge(GLTFSceneImporter importer)
		{
			this.importer = importer;
		}

		public Task<Material> GetMaterial(int index)
		{
			var materialCache = importer.MaterialCache;
			if (materialCache != null && materialCache.Length > index)
			{
				var material = materialCache[index];
				if (material != null) return Task.FromResult(material.UnityMaterial);
			}
			// we can not load the material while the importer is running
			if (importer.IsRunning)
			{
				return Task.FromResult<Material>(null);
			}
			return importer.LoadMaterialAsync(index);
		}

		public Task<AnimationClip> GetAnimation(int index)
		{
			if (index < importer.AnimationCache.Length)
				return Task.FromResult(importer.AnimationCache[index].LoadedAnimationClip);
			return Task.FromResult(default(AnimationClip));
		}
	}
}