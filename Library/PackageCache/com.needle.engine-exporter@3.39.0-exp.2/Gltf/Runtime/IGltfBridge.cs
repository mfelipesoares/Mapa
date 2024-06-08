using UnityEngine;

namespace Needle.Engine.Gltf
{
	/// <summary>
	/// Methods to interop with exporters for UnityGLTF and potentially GLTFast once we add support for that
	/// </summary>
	public interface IGltfBridge
	{
		int TryGetNodeId(Transform t);
		int TryGetMaterialId(Material mat);
		int TryGetMeshId(Mesh m);
		int TryGetTextureId(Texture tex);
		int TryGetAnimationId(AnimationClip clip, Transform transform);
		bool AddTextureExtension<T>(int textureId, string name, T extension);
		bool AddNodeExtension(int nodeId, string name, object extension);
		bool AddMaterialExtension(int materialId, string name, object extension);
		void AddExtension(string name, object extension);
		void AddMaterial(Material material);
		int AddMesh(Mesh mesh);
		int AddTexture(Texture texture);
		int AddAnimationClip(AnimationClip clip, Transform transform, float speed);
	}
}