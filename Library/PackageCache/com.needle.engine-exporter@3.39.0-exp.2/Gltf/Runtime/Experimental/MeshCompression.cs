using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf.Experimental
{
	public enum MeshCompressionType
	{
		None = 0,
		Draco = 1,
		Meshopt = 2,
	}
	
	[AddComponentMenu("Needle Engine/Optimization/" + nameof(MeshCompression) + Constants.NeedleComponentTags + " optimization build")]
	[HelpURL(Constants.DocumentationUrlCompression)]
	public class MeshCompression : MonoBehaviour
	{
		public MeshCompressionType Compression = MeshCompressionType.Draco;
	}
	
	#if UNITY_EDITOR
	[CustomEditor(typeof(MeshCompression))]
	internal class MeshCompressionEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Add to your root scene, a GameObject with a GltfObject component, a referenced prefab or inside a referenced scene to control the mesh compression used for this glb.", MessageType.None);
			if (GUILayout.Button("Open Documentation"))
			{
				Application.OpenURL(Constants.DocumentationUrlCompression);
			}
		}
	}
	#endif
}