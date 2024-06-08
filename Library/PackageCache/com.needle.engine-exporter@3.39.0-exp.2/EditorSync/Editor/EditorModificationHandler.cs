using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Needle.Engine.Serialization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.EditorSync
{
	public class EditorModificationEvent
	{
		public object Object;
		public string PropertyName;
		public object Value;
		/// <summary>
		/// Set prevent default to true to ignore this change. It will not be sent to clients
		/// </summary>
		public bool PreventDefault;

		public EditorModificationEvent(object o, string propertyName, object value)
		{
			Object = o;
			PropertyName = propertyName;
			Value = value;
		}
	}
	
	public static class EditorModificationHandler
	{
		/// <summary>
		/// Called before the editor change is parsed and sent to the clients. Can be used to prevent the change from being sent (or to modify it)
		/// </summary>
		public static event Action<EditorModificationEvent> HandleChange;
		
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorModificationListener.PropertyChanged += OnPropertyChanged;
			EditorApplication.update += OnUpdate;
			serializer = new NewtonsoftSerializer();
		}

		private static ISerializer serializer;

		private static readonly Dictionary<(object owner, string propertyName), object> bufferedChanges =
			new Dictionary<(object owner, string propertyName), object>();

		private static void OnPropertyChanged(object owner, string propertyName, object newValue)
		{
			// Ignore UnityEvent changes
			if (propertyName.Contains("m_PersistentCalls.m_Calls.Array.data")) return;
			var key = (owner, propertyName);
			bufferedChanges[key] = newValue;
		}

		private const int interval = 2;
		private static int count = 0;
		private static DateTime lastActiveTime;

		private static void OnUpdate()
		{
			if(UnityEditorInternal.InternalEditorUtility.isApplicationActive)	
				lastActiveTime = DateTime.Now;

			if (bufferedChanges.Count == 0) return;
			EditorModificationListener.AllowComponentModifications = SyncSettings.SyncComponents;
			
			if (count < interval)
			{
				count++;
				return;
			}
			count = 0;

			if (DateTime.Now - lastActiveTime > TimeSpan.FromSeconds(1)) 
				return;
			
			try
			{
				//Debug.Log("Handle " + bufferedChanges.Count + " changes");
				foreach (var kvp in bufferedChanges.ToArray())
				{
					OnHandleChange(kvp.Key.owner, kvp.Key.propertyName, kvp.Value);
				}
			}
			finally
			{
				bufferedChanges.Clear();
			}
		}

		private static readonly EditorModificationEvent _editorModification = new EditorModificationEvent(null, null, null);
		
		private static void OnHandleChange(object owner, string propertyName, object newValue)
		{
			if (WebEditorConnection.CanSend == false)
				return;
			if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs == false) 
				return;

			if (HandleChange != null)
			{
				_editorModification.Object = owner;
				_editorModification.PropertyName = propertyName;
				_editorModification.Value = newValue;
				_editorModification.PreventDefault = false;
				HandleChange.Invoke(_editorModification);
				if (_editorModification.PreventDefault) return;
				owner = _editorModification.Object;
				propertyName = _editorModification.PropertyName;
				newValue = _editorModification.Value;
			}

			switch (owner)
			{
				case Material _:
					if (SyncSettings.SyncMaterials == false) return;
					break;
				case Component _:
					if (SyncSettings.SyncComponents == false)
					{
						if(owner is Transform) {}
						else return;
					}
					break;
			}
			
			
			// When editing a volume component this is false so we can not check here
			// Instead we check in the Update loop the last time the editor was active
			// if (UnityEditorInternal.InternalEditorUtility.isApplicationActive == false) 
			// 	return;

			// this might happen if we have multi object editing
			// then the owner is an Object[] array
			if (owner is Object[] arr)
			{
				foreach (var entry in arr)
					OnHandleChange(entry, propertyName, newValue);
				return;
			}

			propertyName = TryResolvePropertyName(owner, propertyName);

			if (!NEEDLE_editor.TryGetId(owner, out var id))
			{
				Debug.LogWarning("Could not find id for " + owner, owner as Object);
				return;
			}

			if (owner is Material || owner is Component)
			{
				var name = propertyName;
				MaterialPropertyNameConverter.ToGltfName(ref name);
				TryResolvePropertyValue(owner, propertyName, name, ref newValue);

				JToken val = default;
				if (newValue is JToken tok)
				{
					val = tok;
				}
				// if is primitive
				else if (newValue is string || newValue is int || newValue is float || newValue is bool)
					val = JToken.FromObject(newValue);
				else if (newValue == null)
					val = null;
				else
				{
					if (newValue is Object)
					{
						// Can not serialize Object
						return;
					}

					// TODO: handle transform or gameObject arrays / values
					var json = serializer.Serialize(newValue);
					val = new JRaw(json);
				}

				//Debug.Log($"Property {propertyName} changed to {newValue} in {id}", owner as Object);

				WebEditorConnection.SendPropertyModifiedInEditor(id, name, val);
			}
			else if (owner is GameObject)
			{
				if (newValue is bool)
				{
					switch (propertyName)
					{
						case "isActive":
							WebEditorConnection.SendPropertyModifiedInEditor(id, "visible", JToken.FromObject(newValue));
							break;
					}
				}
				else if (newValue is Component comp)
				{
					switch (propertyName)
					{
						case "component:added":
							var json = EditorJsonUtility.ToJson(comp);
							WebEditorConnection.SendPropertyModifiedInEditor(id, propertyName, new JRaw(json));
							break;
						case "component:removed":
							// TODO: this doesnt work because we can not get the id for the already removed component
							if (NEEDLE_editor.TryGetId(newValue, out var componentId))
							{
								WebEditorConnection.SendPropertyModifiedInEditor(id, propertyName, componentId);
							}
							break;
					}
				}
			}
			else
			{
				
			}
		}

		private static void TryResolvePropertyValue(object owner, string propertyName, string gltfName, ref object value)
		{
			// Handle glossiness to roughness conversion
			if (propertyName.IndexOf("roughness", StringComparison.OrdinalIgnoreCase) == -1 && gltfName == "roughnessFactor" && propertyName != "_Smoothness")
			{
				value = 1f - (float)value;
			}
			else if (owner is Light)
			{
				// Modify light intensity changes
				if (gltfName == "intensity" && value is float f)
				{
					value = f * Mathf.PI;
				}
			}
			else if (owner is Transform tr)
			{
				// Rotation changes on camera and light need to be inverted
				if (tr.TryGetComponent<Camera>(out _) || tr.TryGetComponent<Light>(out _))
				{
					if (value is Quaternion q)
					{
						value = q * Quaternion.Euler(0, 180, 0);
					}
				}
			}
			else if (owner is Material)
			{
				// Color changes on materials should be linear
				if (value is Color col)
				{
					value = col.linear;
				}
			}
			
			// if (value is Sprite sprite)
			// {
			// 	value = sprite.texture;
			// }

			// Handle material assignments
			if (value is Material)
			{
				WarnAboutUnsupportedChanges(value);
				value = null;
			}
			else if (value is Component)
			{
				WarnAboutUnsupportedChanges(value);
				value = null;
			}
			else if (value is Sprite)
			{
				WarnAboutUnsupportedChanges(value);
				value = null;
			}
			else if (value is Texture tex)
			{
				if (value is Texture2D || value is Cubemap)
				{
					var path = AssetDatabase.GetAssetPath((Object)value);
					var bytes = File.ReadAllBytes(path);
					var ext = Path.GetExtension(path).Substring(1).ToLower();
					if (ext == "asset")
					{
						if (value is Texture2D t2d)
						{
							bytes = t2d.EncodeToPNG();
							ext = "png";
						}
					}
					var obj = new JObject();
					obj["type"] = "texture";
					obj["data"] = $"data:image/{ext};base64,{Convert.ToBase64String(bytes)}";
					obj["filter"] = new JValue((int)tex.filterMode);
					obj["anisotropy"] = new JValue(tex.anisoLevel);
					obj["wrap"] = new JValue((int)tex.wrapMode);
					obj["name"] = new JValue(tex.name);
					value = obj;
				}
			}
			
			// Handle lists changing
			if (value is IList arr)
			{
				for (var i = 0; i < arr.Count; i++)
				{
					var entry = arr[i];
					TryResolvePropertyValue(owner, propertyName, gltfName, ref entry);
					arr[i] = entry;
				}
			}
		}

		private static DateTime _lastWarningTime = DateTime.Now;
		
		private static void WarnAboutUnsupportedChanges(object obj)
		{
			if (obj == null) return;
			if (DateTime.Now - _lastWarningTime < TimeSpan.FromMilliseconds(100)) return;
			_lastWarningTime = DateTime.Now;
			var name = obj.GetType().Name;
			var msg = $"Can not sync changes for {name}";
			Debug.LogWarning(msg, obj as Object);
			var view = SceneView.lastActiveSceneView;
			if(view) view.ShowNotification(new GUIContent(msg));
		}

		private static string TryResolvePropertyName(object owner, string propertyName)
		{
			// TODO: refactor this to be extensible / implementations by interfaces

			// send properties like m_BackgroundColor as backgroundColor
			if (owner is Camera)
			{
				switch (propertyName)
				{
					case "m_BackGroundColor":
						return "backgroundColor";
					case "near clip plane":
						return "nearClipPlane";
					case "far clip plane":
						return "farClipPlane";
				}
			}

			if (owner is Light)
			{
				switch (propertyName)
				{
					case "m_Shadows.m_Type":
						return "shadows";
				}
			}

			if (owner is Renderer)
			{
				if (propertyName == "m_Materials")
					return "sharedMaterials";
			}

			if (propertyName.StartsWith("m_"))
			{
				var first = propertyName[2];
				propertyName = propertyName.Substring(3);
				propertyName = char.ToLower(first) + propertyName;
			}

			if (owner is Transform)
			{
				if (propertyName == "localScale") return "scale";
				if (propertyName == "localRotation") return "rotation";
				if (propertyName == "localPosition") return "position";
			}
			
			if(owner is Component && char.IsUpper(propertyName[0]))
			{
				propertyName = char.ToLower(propertyName[0]) + propertyName.Substring(1);
			}

			return propertyName;
		}
	}
}