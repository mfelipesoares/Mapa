using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Gltf;
using Needle.Engine.Server;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Needle.Engine
{
	internal static class DropAssetIntoBrowser
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Reset();
			Connection.Instance.Message += OnMessage;
		}

		private static void OnMessage(RawMessage msg)
		{
			if (msg.type == "needle-editor:drop-file")
			{
				var exportInfo = ExportInfo.Get();
				if (!exportInfo)
				{
					return;
				}
				
				var data = msg.data;
				var name = data.Value<string>("name");
				if (name != null)
				{
					var size = data.Value<long>("size");
					var lastModified = data.Value<long>("lastModified");
					var assets = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(name));
					foreach (var guid in assets)
					{
						var path = AssetDatabase.GUIDToAssetPath(guid);
						var info = new FileInfo(path);
						if (info.Exists && info.Length == size && info.Name == name)
						{
							var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
							var projectPath = Path.GetFullPath(exportInfo.GetProjectDirectory());
							ExportFile(obj, projectPath, guid, data);
							return;
						}
					}
				}
			}
		}

		private static Object obj;
		private static string projectPath;
		private static string guid;
		private static JToken data;

		private static void Reset()
		{
			obj = null;
		}

		private static void ExportFile(Object _obj, string _projectPath, string _guid, JToken _data)
		{
			obj = _obj;
			projectPath = _projectPath;
			guid = _guid;
			data = _data;
			ExportFile();
		}

		private static void ExportFile()
		{
			EditorGUIUtility.PingObject(obj);		
			var targetPath = projectPath + "/node_modules/Needle~/" + guid + "/" + obj.name + ".glb";
			var objectExport = new ObjectExportContext(BuildContext.LocalDevelopment, obj, projectPath, targetPath);
			if (Export.AsGlb(objectExport, obj, out var relPath, null, true))
			{
				var outputFileInfo = new FileInfo(targetPath);
				if (outputFileInfo.Exists)
				{
					var response = new JObject();
					response["path"] = relPath;
					response["location"] = data["location"];
					response["size"] = outputFileInfo.Length;
					Connection.Instance.Send("needle-editor:exported-file", response);
				}
			}
		}


		// ReSharper disable once UnusedType.Local
		private class Processor : AssetsModifiedProcessor
		{
			protected override async void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets)
			{
				if (changedAssets.Length > 0 && obj)
				{
					await Task.Delay(100);
					var dep = AssetDependency.Get(AssetDatabase.GetAssetPath(obj), ProjectInfoExtensions.GetCacheDirectory());
					if (dep.HasChanged)
					{
						ExportFile();
					}
				}
			}
		}
	}
}