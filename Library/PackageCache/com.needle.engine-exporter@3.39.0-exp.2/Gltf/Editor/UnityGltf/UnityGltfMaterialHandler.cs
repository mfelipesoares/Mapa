// using UnityEditor;
// using UnityEngine;
// namespace Needle.Engine.Gltf.UnityGltf
// {
//  DISABLED because causing issues, see https://discord.com/channels/717429793926283276/1098239981304168510
// 	public class UnityGltfMaterialHandler : UnityEditor.AssetModificationProcessor
// 	{
// 		private static Shader pbrGraph, pbrUnlitGraph;
// 		private static ExportInfo _exportInfo;
//
// 		[InitializeOnLoadMethod]
// 		private static void Init()
// 		{
// 			pbrGraph = Shader.Find("UnityGLTF/PBRGraph");
// 			pbrUnlitGraph = Shader.Find("UnityGLTF/UnlitGraph");
// 		}
// 		
// 		private static void OnWillCreateAsset(string assetName)
// 		{
// 			if (!pbrGraph || !pbrUnlitGraph) return;
// 			
// 			if (!_exportInfo) _exportInfo = ExportInfo.Get();
// 			
// 			if (assetName.EndsWith(".mat") && _exportInfo)
// 			{
// 				EditorApplication.delayCall += () =>
// 				{
// 					var mat = AssetDatabase.LoadAssetAtPath<Material>(assetName);
// 					if (mat)
// 					{
// 						var originalShader = mat.shader;
// 						if(mat.shader.name == "Standard")
// 							mat.shader = pbrGraph;
// 						else if(mat.shader.name == "Universal Render Pipeline/Lit")
// 							mat.shader = pbrGraph;
// 						else if(mat.shader.name == "Universal Render Pipeline/Unlit")
// 							mat.shader = pbrUnlitGraph;
// 						
// 						if (originalShader != mat.shader)
// 						{
// 							Debug.Log(
// 								$"Replaced shader \"{originalShader.name}\" with \"{mat.shader.name}\" for {assetName}");
// 							AssetDatabase.Refresh();
// 						}
// 					}
// 				};
// 			}
// 		}
// 	}
// }