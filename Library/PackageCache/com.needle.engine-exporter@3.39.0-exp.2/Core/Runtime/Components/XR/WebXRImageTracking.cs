using System;
using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Needle.Engine.Components
{
	[Serializable]
	public class WebXRTrackedImage
	{
		[Tooltip("Tracked image marker. Make sure the image has good contrast and unique features to improve the tracking quality.")]
		public ImageReference Image;
		[Tooltip("Make sure this matches your physical marker size! Otherwise the tracked object will \"swim\" above or below the marker.")]
		public float WidthInMeters;
		[Tooltip("The object moved around by the image. Make sure the size matches WidthInMeters.")]
		public AssetReference @Object = null;
		[Tooltip("If true, a new instance of the referenced object will be created for each tracked image. Enable this if you're re-using objects for multiple markers.")]
		public bool CreateObjectInstance = false;
		[Tooltip("Use this for static images (e.g. markers on the floor). Only the first few frames of new poses will be applied to the model. This will result in more stable tracking.")]
		public bool ImageDoesNotMove = false;
		
		[Tooltip("Enable to hide the tracked object when the image is not tracked anymore. When disabled the tracked object will stay at the position it was last tracked at.")]
		public bool HideWhenTrackingIsLost = true;

		public WebXRTrackedImage(ImageReference image)
		{
			this.Image = image;
		}
	}
	
	[AddComponentMenu("Needle Engine/XR/Image Tracking" + Needle.Engine.Constants.NeedleComponentTags + " WebXR")]
	public class WebXRImageTracking : MonoBehaviour
	{
		public List<WebXRTrackedImage> TrackedImages = new List<WebXRTrackedImage>();
		
		private static Mesh _quadMesh;
		private static Material _quadMaterial;
		
		#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var anyIsSelected = false;
			var specificSelected = default(GameObject);
			foreach (var s in Selection.gameObjects)
			{
				if (!s) continue;
				if (s == gameObject)
				{
					anyIsSelected = true;
					break;
				}
				
				foreach (var trackedImage in TrackedImages)
				{
					if (s == trackedImage.Object.asset)
					{
						anyIsSelected = true;
						specificSelected = s;
						break;
					}
				}
			}
			
			if (!anyIsSelected) return;
			
			if (!_quadMesh)
			{
				var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
				primitive.hideFlags = HideFlags.HideAndDontSave;
				_quadMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
				if (Application.isPlaying) Destroy(primitive);
				else DestroyImmediate(primitive);
			}
			
			if (!_quadMaterial)
			{
				var shader = Shader.Find("UnityGLTF/UnlitGraph");
				if (!shader) return;
				_quadMaterial = new Material(shader);
				_quadMaterial.hideFlags = HideFlags.HideAndDontSave;
				var map = new UnlitGraphMap(_quadMaterial);
				map.AlphaMode = AlphaMode.BLEND;
				map.BaseColorFactor = new Color(1, 1, 1, 0.5f);
				_quadMaterial.SetFloat("_BUILTIN_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
				_quadMaterial.SetFloat("_BUILTIN_ZWrite", 0);
				_quadMaterial.SetFloat("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
				_quadMaterial.SetFloat("_ZWrite", 0);
			}
			
			if (!_quadMaterial || !_quadMesh) return;
			
			foreach (var trackedImage in TrackedImages)
			{
				if (!trackedImage.Image?.File || !trackedImage.Object?.asset) continue;
				var go = trackedImage.Object.asset as GameObject;
				if (specificSelected && specificSelected != go) continue;
				var path = AssetDatabase.GetAssetPath(trackedImage.Image.File);
				var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (!texture || !importer || !go) continue;
				
				importer.GetSourceTextureWidthAndHeight(out var width, out var height);
				_quadMaterial.SetTexture("baseColorTexture", texture);
				_quadMaterial.SetPass(0);
				var scale = new Vector3(trackedImage.WidthInMeters, trackedImage.WidthInMeters * height / width, 1);
				var tr = go.transform;
				scale = Vector3.Scale(scale, tr.lossyScale);
				Graphics.DrawMeshNow(_quadMesh, Matrix4x4.TRS(tr.position, tr.rotation * Quaternion.Euler(90, 180, 0), scale));
			}
		}
		#endif
	}
	
#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(WebXRTrackedImage))]
	internal class WebXRTrackedImageDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			const int spaceBetweenElements = 5;
			const int properties = 6;
			var defaultHeight = EditorGUIUtility.singleLineHeight * properties + spaceBetweenElements;
			// var hasObject = property.FindPropertyRelative(nameof(WebXRTrackedImage.@Object));
			// if (!hasObject.FindPropertyRelative(nameof(AssetReference.asset)).objectReferenceValue)
			// 	defaultHeight -= 2 * EditorGUIUtility.singleLineHeight;
			return defaultHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var r = position;
			r.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField(r, property.FindPropertyRelative(nameof(WebXRTrackedImage.Image)));
			r.y += EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField(r, property.FindPropertyRelative(nameof(WebXRTrackedImage.WidthInMeters)));
			r.y += EditorGUIUtility.singleLineHeight;
			var objectProp = property.FindPropertyRelative(nameof(WebXRTrackedImage.@Object));
			EditorGUI.PropertyField(r, objectProp);
			// if (objectProp.FindPropertyRelative(nameof(AssetReference.asset)).objectReferenceValue)
			{
				r.y += EditorGUIUtility.singleLineHeight;
				// EditorGUI.indentLevel++;
				EditorGUI.PropertyField(r, property.FindPropertyRelative(nameof(WebXRTrackedImage.CreateObjectInstance)));
				r.y += EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(r, property.FindPropertyRelative(nameof(WebXRTrackedImage.ImageDoesNotMove)));
				r.y += EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(r, property.FindPropertyRelative(nameof(WebXRTrackedImage.HideWhenTrackingIsLost)));
				// EditorGUI.indentLevel--;
			}
		}
	}
	
#endif
}