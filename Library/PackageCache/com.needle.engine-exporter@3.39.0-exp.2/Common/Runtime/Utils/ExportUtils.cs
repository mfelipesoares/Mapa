using System;
using System.IO;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Object = UnityEngine.Object;

namespace Needle.Engine.Utils
{
	public static class ExportUtils
	{
		public static GLTFSceneExporter GetExporter(Transform transform, out ExportContext exportContext, GLTFSettings gltfExportSettings = null)
		{ 
			exportContext = new ExportContext(gltfExportSettings);
			exportContext.TexturePathRetriever = RetrieveTexturePath;

			var exporter = new GLTFSceneExporter(new[] { transform }, exportContext);
			return exporter;
		}

		private static ProfilerMarker exportWithUnityGltfMarker = new ProfilerMarker("ExportWithUnityGltf");

		public static void ExportWithUnityGltf(GLTFSceneExporter exporter, string path, bool exportBinary = true)
		{
			using (exportWithUnityGltfMarker.Auto())
			{
				var dir = Path.GetDirectoryName(path);
				if (dir == null) throw new Exception("Directory is null?");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				var fileName = Path.GetFileName(path);
				// if(exportAdditionalTextures != null)
				// 	exporter.ExportAdditionalTexture += exportAdditionalTextures;
				if (exportBinary)
					exporter.SaveGLB(dir, fileName);
				else
					exporter.SaveGLTFandBin(dir, fileName);
			}
		}

		private static string RetrieveTexturePath(Texture texture)
		{
#if UNITY_EDITOR
			return AssetDatabase.GetAssetPath(texture);
#else
			return null;
#endif
		}

	}
}