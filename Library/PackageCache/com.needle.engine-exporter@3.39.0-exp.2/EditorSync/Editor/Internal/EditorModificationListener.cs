using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine
{
	public delegate void PropertyChangedEvent(object owner, string propertyName, object newValue);

	public class EditorModificationHookArguments
	{
		public readonly Editor Editor;
		public readonly VisualElement VisualElement;
		/// <summary>
		/// Set to true to prevent the default hook from being created
		/// </summary>
		public bool Used;

		public readonly PropertyChangedEvent PropertyChangedEvent;

		public EditorModificationHookArguments(Editor editor, VisualElement visualElement, PropertyChangedEvent evt)
		{
			Editor = editor;
			VisualElement = visualElement;
			PropertyChangedEvent = evt;
		}
	}

	public static class EditorModificationListener
	{
		public static bool Enabled = true;
		internal static bool Materials = true;
		internal static bool Components = true;
		
		public static event PropertyChangedEvent PropertyChanged;
		public static event Action<EditorModificationHookArguments> CreateCustomHook;

		internal static bool AllowComponentModifications = true;

		private static TransformChangeListener _transformChangeListener;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			InspectorHook.Inject += OnInject;
			ObjectChangeEvents.changesPublished += OnChanged;
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			_transformChangeListener?.Update();
		}

		internal const BindingFlags PropertyDrawerFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default;

		private static async void OnInject(Editor editor, VisualElement element)
		{
			if (!Enabled) return;

			var args = new EditorModificationHookArguments(editor, element, OnPropertyChanged);
			CreateCustomHook?.Invoke(args);
			if (args.Used) return;
			
			if (Materials && editor is MaterialEditor materialEditor)
			{
				await Task.Delay(5);
				MaterialChangeListener.Create(materialEditor, OnPropertyChanged);
			}

			if (Components)
			{
				if (editor is GameObjectInspector goInspector)
				{
					element.Add(new AddRemoveComponentListener(goInspector, OnPropertyChanged));
				}
				else if (editor is TransformInspector)
				{
					_transformChangeListener = new TransformChangeListener(editor, OnPropertyChanged);
				}
				else if (editor is RectTransformEditor)
				{
					_transformChangeListener = new TransformChangeListener(editor, OnPropertyChanged);
				}
				else if (editor is ParticleSystemInspector particleSystemInspector)
				{
				}
				else if(editor != null)
				{
					await Task.Delay(5);
					TryWrapPropertyFields(editor);
				}
			
				// additional
				if (editor is CameraEditor)
				{
					element.Add(new GenericWatcher(editor, OnPropertyChanged, "fov",obj =>
					{
						if (obj is Camera cam) return cam.fieldOfView;
						return null;
					}));
				
					element.Add(new GenericWatcher(editor, OnPropertyChanged, "clearFlags",obj =>
					{
						if (obj is Camera cam) return cam.clearFlags;
						return null;
					}));
				}
				else if (editor is ReflectionProbeEditor)
				{
					element.Add(new GenericWatcher(editor, OnPropertyChanged, "texture",obj =>
					{
						if (obj is ReflectionProbe r) return r.customBakedTexture;
						return null;
					}));
				}
				else if (editor is LightEditor)
				{
					element.Add(new GenericWatcher(editor, OnPropertyChanged, "shadowBias",obj =>
					{
						if (obj is Light r) return r.shadowBias * .00001f * -1 + 0.000025f;
						return null;
					}));
					element.Add(new GenericWatcher(editor, OnPropertyChanged, "shadowNormalBias",obj =>
					{
						if (obj is Light r) return r.shadowNormalBias * .01f;
						return null;
					}));
				}
			}
		}

		private static void TryWrapPropertyFields(Editor editor)
		{
#if UNITY_2021_3_OR_NEWER
			var field = typeof(PropertyHandler).GetField("m_PropertyDrawers", PropertyDrawerFlags);
#else
				var field = typeof(PropertyHandler).GetField("m_PropertyDrawer", PropertyDrawerFlags);
#endif
			if (field == null)
			{
				return;
			}
			try
			{
				if (!editor.target) return;
				if (editor.serializedObject.isValid == false) return;
				var iterator = editor.serializedObject.GetIterator();
				for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
				{
					var prop = editor.serializedObject.FindProperty(iterator.propertyPath);
					if (!prop.isValid) continue;
					var handler = editor.propertyHandlerCache.GetHandler(prop);
					if (handler == null) handler = new PropertyHandler();
#if UNITY_2021_3_OR_NEWER
					var drawer = field?.GetValue(handler) as List<PropertyDrawer>;
					if (drawer == null) drawer = new List<PropertyDrawer>();
					if (drawer.Count <= 0)
					{
						drawer.Add(new PropertyDrawerWrapper(null, OnPropertyChanged));
					}
					for (var i = 0; i < drawer.Count; i++)
					{
						drawer[i] = HandleWrapping(drawer[i]);
					}
					field.SetValue(handler, drawer);
#else
						var drawer = field?.GetValue(handler) as PropertyDrawer;
						field.SetValue(handler, HandleWrapping(drawer));
#endif
				}
			}
			catch (Editor.SerializedObjectNotCreatableException notCreatableException)
			{
				Debug.LogError("Failed to create serialized object: " + editor + " - " + editor.target, editor.target);
				Debug.LogException(notCreatableException);
			}
			catch(Exception e)
			{
				Debug.LogException(e);
			}
		}

		private static PropertyDrawer HandleWrapping(PropertyDrawer drawer)
		{
			if (drawer is PropertyDrawerWrapper)
			{
				// dont wrap properties multiple times	
			}
			else
			{
				var wrapper = new PropertyDrawerWrapper(drawer, OnPropertyChanged);
				return wrapper;
			}
			
			return drawer;
		}

		private static void OnPropertyChanged(object owner, string propertyName, object value)
		{
			if (!Enabled) return;
			// Debug.Log($"Property {propertyName} changed to {value}", owner as Object);
			PropertyChanged?.Invoke(owner, propertyName, value);
		}

		private static void OnChanged(ref ObjectChangeEventStream stream)
		{
			// for (var i = 0; i < stream.length; i++)
			// {
			// 	var type = stream.GetEventType(i);
			// 	switch (type)
			// 	{
			// 		case ObjectChangeKind.None:
			// 			break;
			// 		case ObjectChangeKind.ChangeScene:
			// 			break;
			// 		case ObjectChangeKind.CreateGameObjectHierarchy:
			// 			stream.GetCreateGameObjectHierarchyEvent(i, out var createdGameObject);
			// 			var objFromInstanceId = EditorUtility.InstanceIDToObject(createdGameObject.instanceId);
			// 			break;
			// 		case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
			// 			stream.GetChangeGameObjectStructureHierarchyEvent(i, out var changedHierarchy);
			// 			break;
			// 		case ObjectChangeKind.ChangeGameObjectStructure:
			// 			stream.GetChangeGameObjectStructureEvent(i, out var changedGameObject);
			// 			break;
			// 		case ObjectChangeKind.ChangeGameObjectParent:
			// 			stream.GetChangeGameObjectParentEvent(i, out var changedGameObjectParent);
			// 			break;
			// 		case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
			// 			stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeGameObjectOrComponent);
			// 			break;
			// 		case ObjectChangeKind.DestroyGameObjectHierarchy:
			// 			stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyGameObject);
			// 			break;
			// 		case ObjectChangeKind.CreateAssetObject:
			// 			break;
			// 		case ObjectChangeKind.DestroyAssetObject:
			// 			break;
			// 		case ObjectChangeKind.ChangeAssetObjectProperties:
			// 			// stream.GetChangeAssetObjectPropertiesEvent(i, out var assetChanges);
			// 			break;
			// 		case ObjectChangeKind.UpdatePrefabInstances:
			// 			break;
			// 	}
			// }
		}
	}
}