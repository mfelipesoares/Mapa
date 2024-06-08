using Needle.Engine.Server;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.EditorSync
{
	internal static class WebEditorConnection
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Connection.Instance.Message += OnMessage;
		}

		private static void OnMessage(RawMessage msg)
		{
			if (msg.type == "needle:editor:propertyChanged")
			{
				var data = msg.data;
				var editorId = data.Value<string>("id");
				Debug.Log(editorId + "\n" + data);
				if (!string.IsNullOrEmpty(editorId))
				{
					var obj = AssetDatabase.GUIDToAssetPath(editorId);
					if (!string.IsNullOrEmpty(obj))
					{
						var property = data.Value<string>("property");
						var value = data.Value<string>("value");
						var asset = AssetDatabase.LoadAssetAtPath<Object>(obj);
						if (asset && value != null)
						{
							Undo.RegisterCompleteObjectUndo(asset, "Edited in browser: " + property);
							Debug.Log(asset + ": " + property, asset);
							if (asset is Material mat)
							{
								var col = mat.color;
								if (property.EndsWith(".r"))
									col.r = float.Parse(value);
								else if (property.EndsWith(".g"))
									col.g = float.Parse(value);
								else if (property.EndsWith(".b"))
									col.b = float.Parse(value);
								else if (property.EndsWith(".a"))
									col.a = float.Parse(value);
								mat.color = col;
							}
						}
					}
				}
			}
		}

		// TODO: check if the currently running server is this project
		internal static bool CanSend => Connection.Instance.IsConnected;

		internal static void SendPropertyModifiedInEditor(string guid, string property, JToken value)
		{
			if (CanSend)
			{
				var res = new JObject();
				res["guid"] = guid;
				res["propertyName"] = property;
				res["value"] = value;
				// Debug.Log("Send " + res);
				Connection.Instance.Send("needle:editor:modified-property", res);
			}
		}
	}
}