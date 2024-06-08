using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Needle.Engine.Core;
using Needle.Engine.Core.References;
using Needle.Engine.Core.References.ReferenceResolvers;
using Needle.Engine.Gltf.Spritesheets;
using Needle.Engine.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityGLTF.Extensions;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf
{
	[Serializable]
	public class SerializedDictionary
	{
		public List<object> Keys = new List<object>();
		public List<object> Values = new List<object>();
	}

	public class GltfValueResolver : IValueResolver
	{
		// Can be overridden by custom implementations
		public static IValueResolver Default = new GltfValueResolver();


		public bool TryGetValue(IExportContext ctx, object instance, MemberInfo member, ref object value)
		{
			// Debug.Log("RESOLVE: " + value);
			if (ctx is GltfExportContext context)
			{
				if (value == null || (value is Object o && !o))
				{
					value = null;
					return true;
				}

				if (value is LayerMask layerMask)
				{
					value = layerMask.value;
					return true;
				}

				context.Debug.WriteDebugReferenceInfo(instance, member.Name, value);

				if (value is IDictionary dict)
				{
					if (dict.Count <= 0) return true;
					var newDict = new SerializedDictionary();
					// var newDict = new Dictionary<object, object>();
					var keys = dict.Keys;
					foreach (var keyObj in keys)
					{
						var key = keyObj;
						var val = dict[key];
						TryGetValue(ctx, instance, member, ref key);
						TryGetValue(ctx, instance, member, ref val);
						newDict.Keys.Add(key);
						newDict.Values.Add(val);
					}
					value = newDict;
					return true;
				}

				if (value is IList list)
				{
					if (list.Count <= 0)
					{
						return true;
					}

					var recurse = list is Array;
					if (!recurse && value.GetType().IsConstructedGenericType)
					{
						var gen = value.GetType().GenericTypeArguments;
						foreach (var type in gen)
						{
							if (!type.IsPrimitive && typeof(string) != type)
							{
								recurse = true;
							}
						}
					}
					if (!recurse) return true;
					var newList = new object[list.Count];
					for (var i = 0; i < list.Count; i++)
					{
						var val = list[i];
						if (TryGetValue(ctx, instance, member, ref val))
						{
							newList[i] = val;
						}
					}
					value = newList;
					return true;
				}

				if (value is FileReference file)
				{
					if (file.File)  
					{
						var path = AssetDatabase.GetAssetPath(file.File);
						var fileName = Path.GetFileName(path);
						var target = Path.GetDirectoryName(context.Path) + "/" + fileName;
						File.Copy(path, target, true);
						value = fileName;
						return true;
					}
					return true;
				}
				
				// copy text assets to the same directory as the gltf file
				if (value is TextAsset textAsset)
				{
					var path = AssetDatabase.GetAssetPath(textAsset);
					var fileName = Path.GetFileName(path);
					var target = Path.GetDirectoryName(context.Path) + "/" + fileName;
					File.Copy(path, target, true);
					value = fileName;
					return true;
				}

				if (value is AssetReference reference)
				{
					// we can serialize the whole AssetReference with extra data (e.g. an id/name) at some point once the runtime can deserialize an AssetReference that is not just a string
					value = reference.asset;
				}

				if (value is Mesh mesh && mesh)
				{
					var id = context.Bridge.TryGetMeshId(mesh);
					if (id < 0)
					{
						context.Bridge.AddMesh(mesh);
						id = context.Bridge.TryGetMeshId(mesh);
					}
					value = id.AsMeshPointer(); // new JObject() { { "src", id.AsMeshPointer() } };
					return true;
				}

				if (value is Material mat && mat)
				{
					var id = context.Bridge.TryGetMaterialId(mat);
					if (id < 0)
					{
						context.Bridge.AddMaterial(mat);
						id = context.Bridge.TryGetMaterialId(mat);
					}
					value = id.AsMaterialPointer();
					return true;
				}
				
				if (value is Sprite sprite)
				{
					if (TryHandleExportSprite(instance, sprite, context, out var res))
					{
						value = res;
						return true;
					}
				}

				if (value is Texture tex && tex)
				{
					var id = context.Bridge.TryGetTextureId(tex);
					if (id < 0)
					{
						context.Bridge.AddTexture(tex);
						id = context.Bridge.TryGetTextureId(tex);
					}
					value = id.AsTexturePointer();
					return true;
				}

				if (value is Font font && font)
				{
					var style = FontStyle.Normal;
					if (instance is Text text) style = text.fontStyle;
					var outputPath = FontsHelper.TryGenerateRuntimeFont(font, style, context.AssetsDirectory, false,
						instance as Object);
					if (outputPath == null)
					{
						if (font)
							value = "font:" + font.name;
						else value = null;
						return true;
					}
					value = Path.GetFileName(outputPath);
					return true;
				}

				if (value is AnimationClip anim && anim)
				{
					var owner = default(Transform);
					
					if (instance is AnimatorOverrideController || instance is AnimatorController || instance is AnimatorState)
					{
						// TODO: we need a way to get the animator that was referencing the animator override controller
						return false;
					}
					
					if(instance is Component comp) owner = comp.transform;
					
					var id = context.Bridge.TryGetAnimationId(anim, owner);
					if (id < 0)
					{
						context.Bridge.AddAnimationClip(anim, owner?.transform, 1);
						id = context.Bridge.TryGetAnimationId(anim, owner);
						if(id < 0) {
                            Debug.LogError("Could not export animation: " + anim.name, owner as Object);
                            return false;
						}
					}
					value = id.AsAnimationPointer();
					return true;
				}

				if (value is AudioMixer mixer)
				{
					// TODO
					Debug.LogWarning("AudioMixer Export is not yet supported: " + instance, instance as Object);
					value = "mixer/" + mixer.GetId();
					return true;
				}

				if (value is UnityEngine.Shader shader)
				{
					return shader;
				}

				if (value is AudioClip clip)
				{
					var assetPath = AssetDatabase.GetAssetPath(clip);
					var filename = Path.GetFileName(assetPath);
					// Export the audio clip next to the scene file
					var targetFullPath = Path.GetDirectoryName(context.Path) + "/" + filename;
					if (!File.Exists(targetFullPath))
						File.Copy(assetPath, targetFullPath, false);
					value = filename.AsRelativeUri();
					return true;
				}

				if (value is VideoClip videoClip)
				{
					value = VideoResolver.ExportVideoClip(videoClip, context);
					return true;
				}

				if (value is UnityEventBase evt)
				{
					var hasCalls = evt.TryFindCalls(out var calls);
					var eventList = new JObject();
					eventList.Add("type", "EventList");
					var array = new JArray();
					eventList.Add("calls", array);
					if (hasCalls)
					{
						foreach (var callEntry in calls)
						{
							if (callEntry == null) continue;
							var e = callEntry as object;
							if (TryGetValue(context, instance, member, ref e))
							{
								array.Add(e);
							}
						}
					}
					value = eventList;
					return true;
				}

				if (value is CallInfo call)
				{
					var methodName = call.MethodName;
					// if no method is selected, Unity shows "No Function" and has an empty method name. We don't want to export that.
					if (methodName == "")
						return false;
					
					// TODO: create proper type that matches EventListCall at runtime + update SignalCall type (in Timeline Signal exporter)
					var node = new JObject();
					// TODO: get reference from store
					var targetId = call.Target.GetId();
					node.Add("target", new JValue(targetId));
					node.Add("method", new JValue(methodName));
					var enabled = call.State != UnityEventCallState.Off;
					node.Add("enabled", new JValue(enabled));
					if (call.Argument != null)
					{
						if (TryGetValue(context, instance, member, ref call.Argument))
						{
							if (call.Argument is JToken token)
								node.Add("argument", token);
							else // if(call.Argument is bool) 
								node.Add("argument", new JValue(call.Argument));
							// else
							// 	node.Add("argument", call.Argument?.ToString());
						}
					}
					value = node;
					return true;
				}

				if (value is Color col)
				{
					var needsConversionToLinear = true;

					if (instance is SpriteRenderer)
					{
						needsConversionToLinear = true;
					}

					var isHdrColor = false;
					var attrs = member.GetCustomAttribute<ColorUsageAttribute>(true);
					if (attrs != null)
					{
						isHdrColor = attrs.hdr;
					}
					
					// colors in color fields need to be converted
					if (instance is MonoBehaviour)
					{
						if (member is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(Color))
						{
							if (!isHdrColor)
								needsConversionToLinear = true;
						}
					}

					if (instance is Camera)
					{
						// backgroundColor
						needsConversionToLinear = true;
					}

					// Otherwise we want to work in linear color space.
					NeedleDebug.Log(TracingScenario.ColorSpaces, "Exporting " + member.DeclaringType + "." + member.Name + " (" + member.MemberType + ") " + (isHdrColor ? "[HDR]" : "") + " - needsConversionToLinear: " + needsConversionToLinear);
					
					if (needsConversionToLinear) 
						value = col.linear;
					
					return true;
				}

				// TODO: cleanup this logic mess. I think this is not in all cases correct and we should create tests for exporting persistent assets with various configurations. We have cases with prefabs and scenes where we need to correctly detect if a reference is actually an asset or just a reference within an asset
				if (value is Object obj)
				{
					// if (instance is Object ownerObject)
					// {
					// 	if (EditorUtils.IsCrossSceneReference(ownerObject, obj))
					// 	{
					// 		Debug.LogWarning("Found cross scene reference on " + ownerObject, ownerObject);
					// 		value = null;
					// 		return false;
					// 	}
					// }

					// persistent asset export:
					if (EditorUtility.IsPersistent(obj) && context.AssetExtension.CanAdd(instance, obj))
					{
						// if a component in a prefab root is referenced
						var exportAsset = false;

						// test against transform
						if (obj is GameObject gameObject) value = gameObject.transform;

						if ((value is Component comp && !(value is Transform) && !comp.transform.parent))
						{
							exportAsset = true;
							obj = comp.transform;

							// check if the component that references this component is in the same prefab
							if (instance is Component ownerComponent && ownerComponent.transform == comp.transform)
							{
								exportAsset = false;
							}
						}

						if (exportAsset || !(value is Component))
						{
							if (obj is SceneAsset scene)
							{
								if (EnsureReferenceIsNotInPackageCache(scene, out var newObj))
									obj = newObj;
							}

							var serializedOrPath = context.AssetExtension.GetPathOrAdd(obj, instance, member);
							value = serializedOrPath;
							return true;
						}
					}
				}

				foreach (var rec in context.ValueResolvers)
				{
					if (rec == this) continue;
					if (rec.TryGetValue(context, instance, member, ref value))
						return true;
				}

				// URP lights are handled in URPLightHandler
				if (instance is Light)
				{
					switch (member.Name)
					{
						case "shadowBias":
							if (value is float shadowBias)
							{
								// invert factor since the effect in unity seems to work that wy
								// also add a little bias by default to reduce artifacts when using default settings
								value = shadowBias * .0033f * -1 + 0.0000025f;
								return true;
							}
							break;
						case "shadowNormalBias":
							if (value is float normalBias)
							{
								value = normalBias * .075f;
								return true;
							}
							break;
					}
				}
				

				// handle reference to another node in the same glTF
				try
				{
					// We only want to reference the transform here if it's NOT a RectTransform
					// otherwise the code below would export the RectTransform reference
					// where we instead would expected a Object3D reference
					if (value is GameObject go && !(go.transform is RectTransform))
					{
						value = go.transform;
					}
				}
				catch (MissingReferenceException)
				{
					Debug.LogWarning("Missing reference detected in " + member.Name + " on " + instance, instance as Object);
					return false;
				}

				var isRectTransform = value is RectTransform;
				if (value is Transform t && !isRectTransform)
				{
					var id = context.Bridge.TryGetNodeId(t);
					if (id >= 0)
					{
						value = "/nodes/" + id;
						return true;
					}
				}

				if (value is Object anyObj)
				{
					// TBD: use extension component path at some point?!
					if (!(anyObj is Component))
						Debug.LogWarning($"Could not find node id for \"{member.Name}: {value.GetType().Name}\", " +
						                 $"this means this is probably an external reference. This will probably only load and work within the context of this scene.",
							instance as Object);
					var guid = anyObj.GetId();
					value = new JObject { { "guid", new JValue(guid) } };
					return true;
				}
			}
			return true;
		}

		private bool TryHandleExportSprite(object owner,
			Sprite sprite,
			GltfExportContext exportContext,
			out object value)
		{
			// TODO: check if we export sprite sheets for every type we do not break UI
			if (owner is SpriteRenderer)
			{
				if(SpriteSheet.TryCreate(owner, sprite, exportContext, out var spriteSheet))
				{
					value = spriteSheet;
					return true;
				}
			}
			value = null;
			return false;
		}
		
		/// <returns>true if the reference was rewritten</returns>
		internal static bool EnsureReferenceIsNotInPackageCache(Object obj, out Object newObject)
		{
			newObject = null;
			if (!obj) return false;
			var input = obj;
			var currentAssetPath = AssetDatabase.GetAssetPath(input);
			if (string.IsNullOrEmpty(currentAssetPath)) return false;
			var fullPathOnDisc = Path.GetFullPath(currentAssetPath);
			// we only run this code if we're in an immutable package
			if (fullPathOnDisc.Contains("PackageCache") == false) return false;
			Debug.LogWarning("Found reference to asset in PackageCache, copying to scene folder", obj);
			var currentActiveScene = SceneManager.GetActiveScene();
			if (string.IsNullOrEmpty(currentActiveScene.path)) return false;
			var currentSceneFullPath = Path.GetFullPath(currentActiveScene.path);
			// if the current scene path is also in the PackageCache we do nothing
			if(currentSceneFullPath.Contains("PackageCache")) return false;
			var newAssetPath = Path.GetDirectoryName(currentActiveScene.path) + "/" + Path.GetFileName(currentAssetPath);
			if(!File.Exists(newAssetPath))
				File.Copy(fullPathOnDisc, newAssetPath);
			AssetDatabase.Refresh();
			newObject = AssetDatabase.LoadAssetAtPath(newAssetPath, obj.GetType());
			var currentGuid = AssetDatabase.AssetPathToGUID(currentAssetPath);
			var newGuid = AssetDatabase.AssetPathToGUID(newAssetPath);
			// replace the guid in the scene file
			var currentSceneMeta = File.ReadAllText(currentActiveScene.path);
			var newSceneMeta = currentSceneMeta.Replace(currentGuid, newGuid, StringComparison.OrdinalIgnoreCase);
			File.WriteAllText(currentActiveScene.path, newSceneMeta);
			AssetDatabase.Refresh();
			EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<SceneAsset>(currentActiveScene.path));
			return true;
		}
	}
}