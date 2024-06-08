using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf.UnityGltf.Import
{
	public class NEEDLE_components_Importer
	{
		private readonly Dictionary<GameObject, JArray> componentData = new Dictionary<GameObject, JArray>();
		// private readonly List<ICommand> postImportCommands = new List<ICommand>();

		internal void OnBeforeImport(IImportContext context)
		{
			componentData.Clear();
			// postImportCommands.Clear();
		}

		internal void OnImport(IImportContext context, GameObject obj, int index, JObject ext)
		{
			var components = ext.Value<JArray>("builtin_components");
			if (components == null) return;
			if (components.Count > 0) componentData.Add(obj, components);
		}

		internal void OnAfterImport(IImportContext context)
		{
			foreach (var data in componentData)
			{
				var obj = data.Key;
				var components = data.Value;
				foreach(var entry in components)
				{
					var componentInfo = (JObject)entry;
					var componentType = componentInfo["name"]!.ToString();

					var type = TryFindType(componentType);
					if (type == null)
					{
						Debug.LogWarning("Failed to find type for component " + componentType + " on " + obj.name, obj);
						continue;
					}

					if (!obj.TryGetComponent(type, out var component))
						component = obj.AddComponent(type);

					var guid = componentInfo["guid"]?.ToString();
					if (string.IsNullOrEmpty(guid))
					{
						Debug.LogWarning("Missing guid for component " + componentType + " on " + obj.name, obj);
					}
					else
					{
						context.Register(guid, component);
					}

					// TODO: if we use URP and the component is a camera we need to add the additional camera data component. Otherwise the Unity asset importer throws exceptions. Probably the same with lights?

					foreach (var kvp in componentInfo)
					{
						if (kvp.Key == "name") continue;
						var memberName = kvp.Key;
						var token = kvp.Value;
						if (token == null) continue;
						if(ShouldIgnore(component, memberName)) 
							continue;
						try
						{
							if (TryResolve(context, component, memberName, token, -1, out var value))
							{
								ReflectionUtils.TrySet(component, memberName, value);
							}
						}
						catch (Exception e)
						{
							Debug.LogError("Failed setting " + memberName + " on " + component.GetType().Name, component);
							Debug.LogException(e);
						}
					}

					// Flip camera forward
					if (component is Camera cam)
					{
						var transform = cam.transform;
						transform.forward = -transform.forward;
					}
				}
				
			}
		}

		private bool TryResolve(IImportContext context, object component, string memberName, JToken token, int index, out object value)
		{
			value = token;
			if (TryResolveJsonPointer(context, ref value))
			{
				return true;
			}
			if (!TryResolveValue(context, component, memberName, token, index, out value))
			{
				return false;
			}
			return true;
		}

		private bool TryResolveJsonPointer(IImportContext context, ref object value)
		{
			var val = value;
			if (value is JToken tok && tok.Type == JTokenType.String)
			{
				val = tok.Value<string>();
			}

			if (val is string str && str.StartsWith("/"))
			{
				if (str.StartsWith("/nodes/"))
				{
					if (context.TryResolve(str, out var obj))
					{
						value = obj;
						return true;
					}
				}
				else if (str.StartsWith("/materials/"))
				{
					var index = int.Parse(str.Substring("/materials/".Length));
					// TODO: running sync here is not good. We should probably use a task queue.
					var material = AsyncHelper.RunSync(() => context.Bridge.GetMaterial(index));
					value = material;
					return true;
				}
				// e.g. persistent assets or other json pointers we dont know about
				else if (context.TryResolve(str, out var asset))
				{
					value = asset;
					return true;
				}
			}
			return false;
		}

		private bool TryResolveValue(IImportContext context, object instance, string memberName, JToken token, int index, out object value)
		{
			value = null;
			
			if (token is JValue val)
			{
				value = val.Value;
				return true;
			}
			
			if (token is JArray array)
			{
				var type = ReflectionUtils.TryGetType(instance, memberName);
				var elementType = type?.GetElementType();
				if (elementType != null)
				{
					var isArray = typeof(Array).IsAssignableFrom(type);
					var list = isArray 
						? Array.CreateInstance(elementType, array.Count) 
						: Activator.CreateInstance(type) as IList;
					
					if (list != null)
					{
						for (var k = 0; k < array.Count; k++)
						{
							var entry = array[k];
							var res = TryResolve(context, list, memberName, entry, k, out var entryValue);
							if (res)
							{
								if (entryValue != null)
								{
									if (entryValue is GameObject go && elementType == typeof(Transform))
									{
										entryValue = go.transform;
									}
									else if (elementType.IsInstanceOfType(entryValue) == false && !ReflectionUtils.TryGetMatchingType(ref entryValue, elementType))
									{
										continue;
									}
								}
								if (list is Array) list[k] = entryValue;
								else if (list.Count > k) list[k] = entryValue;
								else list.Add(entryValue);
							}
						}
						value = list;
						return true;
					}
				}
			}
			else if (token is JObject obj)
			{
				if (obj.TryGetValue("guid", out var guid))
				{
					var cmd = new ResolveReference(context, instance, memberName, guid.ToString(), index);
					context.AddCommand(ImportEvent.AfterImport, cmd);
					return false;
				}
				if (obj.TryGetValue("node", out var nodeIndex))
				{
					var cmd = new ResolveReference(context, instance, memberName, nodeIndex.ToString(), index);
					context.AddCommand(ImportEvent.AfterImport, cmd);
					return false;
				}


				// TODO: could probably be done via newtonsoft converters too.
				// TODO: objects with xyz etc could also be float3 when using mathematics?
				if (
					obj.TryGetValue("r", out var value1) &&
					obj.TryGetValue("g", out var value2) &&
					obj.TryGetValue("b", out var value3))
				{
					var color = new Color();
					color.r = value1!.Value<float>();
					color.g = value2!.Value<float>();
					color.b = value3!.Value<float>();
					if (obj.TryGetValue("a", out var value4))
						color.a = value4!.Value<float>();
					else color.a = 1;
					value = color;
					return true;
				}

				if (obj.TryGetValue("w", out var w))
				{
					// We need to figure out if this ia a quaternion or a vector4
					var type = ReflectionUtils.TryGetType(instance, memberName);

					if (type == typeof(Quaternion))
					{
						var vec = new Quaternion();
						vec.w = w!.Value<float>();
						vec.x = obj.Value<float>("x");
						vec.y = obj.Value<float>("y");
						vec.z = obj.Value<float>("z");
						value = vec;
						return true;
					}

					if (type == typeof(Vector4))
					{
						var vec = new Vector4();
						vec.w = w!.Value<float>();
						vec.x = obj.Value<float>("x");
						vec.y = obj.Value<float>("y");
						vec.z = obj.Value<float>("z");
						value = vec;
						return true;
					}
				}
				else if (obj.TryGetValue("z", out var z))
				{
					var vec = new Vector3();
					vec.x = obj.Value<float>("x");
					vec.y = obj.Value<float>("y");
					vec.z = z.Value<float>();
					value = vec;
					return true;
				}
				else if (obj.TryGetValue("y", out var y))
				{
					var vec = new Vector2();
					vec.x = obj.Value<float>("x");
					vec.y = y.Value<float>();
					value = vec;
					return true;
				}
			}

			return false;
		}

		private static bool ShouldIgnore(object instance, string memberName)
		{
			if (instance is Renderer)
			{
				switch (memberName)
				{
					case "sharedMaterials": 
						return true;
				}
			}
			return false;
		}

		private static Type TryFindType(string name)
		{
			var components = TypeCache.GetTypesDerivedFrom<Component>();
			foreach (var type in components)
			{
				if (type.Name == name)
				{
					return type;
				}
			}
			return null;
		}
	}
}