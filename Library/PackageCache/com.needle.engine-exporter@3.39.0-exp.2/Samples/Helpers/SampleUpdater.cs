using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Needle.Engine.Deployment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Needle.Engine.Samples.Helpers
{
    public static class SampleUpdater
    {
        internal static void PatchActiveScene()
		{
			try
			{
				RemoveKnownMissingComponents();
				UpgradeLightValues();
				CleanDeployToComponents();
			}
			catch (Exception e)
			{
				Debug.LogError($"Error when patching scene \"{SceneManager.GetActiveScene().name}\": {e}");
			}
		}

		private static Dictionary<string, string> knownComponentGuids = new Dictionary<string, string>()
		{
			{ "474bcb49853aa07438625e644c072ee6", "UniversalAdditionalLightData" },
			{ "a79441f348de89743a2939f4d699eac1", "UniversalAdditionalCameraData" },
			{ "dab5c7d4c32e743048dfca98e2d5914f", "SplineContainer" }
		};

		private static MethodInfo _CreateMissingReferenceObject;
		private static FieldInfo m_InstanceId;
		private static Object CreateMissingReferenceObject(int instanceId)
		{
#if UNITY_2022_1_OR_NEWER
			if (_CreateMissingReferenceObject == null)
				_CreateMissingReferenceObject = typeof(Object).GetMethod("CreateMissingReferenceObject", (BindingFlags)(-1));

			return _CreateMissingReferenceObject.Invoke(null, new object[] { instanceId }) as Object;
#else
			if (m_InstanceId == null) m_InstanceId = typeof(Object).GetField("m_InstanceID", (BindingFlags)(-1));
			var obj = new Object();
			if (m_InstanceId != null) m_InstanceId.SetValue(obj, instanceId);
			return obj;
#endif
		}
		
		private static void RemoveKnownMissingComponents()
		{
			// traverse the scene and remove missing components on Cameras and Lights, under the assumption that it's
			// URPAdditionalLightData or URPAdditionalCameraData
			var objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			var missingComponents = new List<(GameObject obj, Object missingReferenceObject, long localID, string guid)>();
			
			foreach (var obj in objects)
			{
				if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) <= 0) continue;
				
				var so = new SerializedObject(obj);
				var components = so.FindProperty("m_Component");
					
				var arrayLength = components.arraySize;
				for (var i = arrayLength - 1; i >= 0; i--)
				{
					var component = components.GetArrayElementAtIndex(i).FindPropertyRelative("component");
					if (component.objectReferenceValue) continue;
					
					var instanceId = component.objectReferenceInstanceIDValue;
					var missingReferenceObject = CreateMissingReferenceObject(instanceId);
					var localID = GetLocalID(missingReferenceObject);

					missingComponents.Add((obj, missingReferenceObject, localID, null));
				}
			}
			
			// We need to manually parse the scene file to find the GUID of the missing scripts... no API for that
			var sceneFile = SceneManager.GetActiveScene().path;

			
			var componentLocalIdToGuid = new Dictionary<long, string>();
			
			void CollectGuidsFromFile(string assetDatabasePath)
			{			
				// File types we need to check as they contain scene data and components:
				// .unity - scene file
				// .prefab - prefab file
				// .asset - scriptable object file
				var type = Path.GetExtension(assetDatabasePath);
				if (type != ".unity" && type != ".prefab" && type != ".asset") return;

                var allText = File.ReadAllText(assetDatabasePath);
				allText = allText.Replace("\r\n", "\n");

                var regexForAllComponents = new Regex(@"--- !u!114 &([0-9]+)\nMonoBehaviour:\n[\s\S]*?  m_GameObject: {fileID: ([0-9]+)}\n[\s\S]*?  m_Script: {fileID: 11500000, guid: ([a-f0-9]+), type: \d}", RegexOptions.Multiline);
				var allMatches = regexForAllComponents.Matches(allText);

                for (var i = 0; i < allMatches.Count; i++)
				{
                    var match = allMatches[i];
					var localID = long.Parse(match.Groups[1].Value);
					var guid = match.Groups[3].Value;
					// Debug.Log("Found component with localID " + localID + " and gameObjectID " + gameObjectID + " and GUID " + guid);
					componentLocalIdToGuid[localID] = guid;
                }
			}

			// as references can also be inside Prefabs, we need to check all dependencies
			var dependencies = AssetDatabase.GetDependencies(sceneFile, true);
			foreach (var dependency in dependencies)
				CollectGuidsFromFile(dependency);

			// At this point, we have
			// - a list of all missing localIDs, which gameObject localID they belong to, and what their script GUID is
			// - a list of MissingComponentObjects
			for (var i = 0; i < missingComponents.Count; i++)
			{
				var missing = missingComponents[i];
				var componentLocalID = GetLocalID(missing.missingReferenceObject);
				if (componentLocalIdToGuid.TryGetValue(componentLocalID, out var guid)) {
					// Debug.Log("Component " + componentLocalID + " is missing in " + missing.obj + ", has guid: " + guid);

					// We know the GUID now, so we can assign it here
					missing.guid = guid;
					missingComponents[i] = missing;
					
					if (knownComponentGuids.ContainsKey(guid))
					{
						var componentString = knownComponentGuids.TryGetValue(guid, out var name) ? name : "GUID: " + guid;
						NeedleDebug.Log(TracingScenario.Samples, $"Removing missing component {componentString} from {missing.obj}", missing.obj);
						
						// We can destroy the wrapped missing reference object, and it will delete the underlying missing component
						Object.DestroyImmediate(missing.missingReferenceObject);
						if (missing.missingReferenceObject)
						{
							Debug.LogError("Couldn't remove missing component from " + missing.obj + ". Please report a bug to Needle.", missing.obj);
						}
					}
					else
					{
						NeedleDebug.LogWarning(TracingScenario.Samples, $"Found unknown missing component with GUID {guid} on {missing.obj}", missing.obj);
					}
				}
				else
				{
					NeedleDebug.LogWarning(TracingScenario.Samples, $"Found missing component with unknown GUID on {missing.obj}; LocalID: {missing.localID}", missing.obj);
				}
			}
		}

		/// <summary>
		/// All Sample scenes are authored with these modern settings:
		/// <code>
		/// QualitySettings.activeColorSpace == ColorSpace.Linear
		/// GraphicsSettings.lightsUseLinearIntensity == true
		/// GraphicsSettings.lightsUseColorTemperature == true
		/// </code>
		/// This method allows us to patch the freshly copied scene to match the current scene settings.
		/// </summary>
		private static void UpgradeLightValues()
		{
			var needsLightValueUpgrade = GraphicsSettings.lightsUseLinearIntensity == false;
			if (!needsLightValueUpgrade) return;
			var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			// TODO implement linear <> non-linear conversion and URP <> BiRP light intensity conversion
		}
		
		private static void CleanDeployToComponents()
		{
			var deployToFtpComponents = Object.FindObjectsByType<DeployToFTP>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var component in deployToFtpComponents)
			{
				if (!component) continue;
				// This reference lives in Needle test projects, so won't be missing during tests, but
				// will be missing when installing the actual samples.
				component.FTPServer = null;
			}
		}
		
		private static long GetLocalID(Object obj)
		{
			// This gives us a format like this: GlobalObjectId_V1-2-385fb38372a85417096e20d059988fc5-212764579-0
			// which contains the GUID of the scene file and the LocalID of the missing component.
			// Turns out that this data is different to AssetDatabase.TryGetGUIDAndLocalFileIdentifier!
			var globalIdentifier = GlobalObjectId.GetGlobalObjectIdSlow(obj);
			return (long) globalIdentifier.targetObjectId;
		}
    }
}