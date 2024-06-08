using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal static class MaterialChangeListener
	{
		private static readonly List<(string key, MaterialPropertyHandler handler)> addedHandlers = new List<(string, MaterialPropertyHandler)>();

		public static void Create(MaterialEditor editor, PropertyChangedEvent evt)
		{
			// var material = editor.target as Material;
			// if (material)
			// {
			// 	MaterialProperty[] materialProperties = MaterialEditor.GetMaterialProperties(editor.targets);
			// 	for (int index = 0; index < materialProperties.Length; ++index)
			// 	{
			// 		var materialProperty = materialProperties[index];
			// 		var handler = MaterialPropertyHandler.GetHandler(material.shader, materialProperties[index].name);
			// 	}
			// }
			//

			
			const BindingFlags propertyHandlerFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

			if (typeof(MaterialPropertyHandler).GetField(("s_PropertyHandlers"), propertyHandlerFlags)?.GetValue(null) is
			    Dictionary<string, MaterialPropertyHandler> materialPropertyHandlers)
			{
				addedHandlers.Clear();
				foreach (var kvp in materialPropertyHandlers)
				{
					var handler = kvp.Value;
					if (handler == null)
					{
						handler = new MaterialPropertyHandler();
						addedHandlers.Add((kvp.Key, handler));
						// continue;
					}
					var field = typeof(MaterialPropertyHandler).GetField("m_PropertyDrawer", EditorModificationListener.PropertyDrawerFlags);
					var propertyDrawer = field?.GetValue(handler) as MaterialPropertyDrawer;
					if (propertyDrawer is MaterialPropertyDrawerWrapper) continue;
					var wrapper = new MaterialPropertyDrawerWrapper(propertyDrawer, evt);
					field?.SetValue(handler, wrapper);
				}
				foreach (var added in addedHandlers) materialPropertyHandlers[added.key] = added.handler;
			}

			if (editor.customShaderGUI != null)
			{
				var customShaderGUIField = typeof(MaterialEditor).GetField("m_CustomShaderGUI", BindingFlags.Instance | BindingFlags.NonPublic);
				var customShaderGUI = customShaderGUIField?.GetValue(editor) as ShaderGUI;
				if (!(customShaderGUI is ShaderGUIWrapper))
				{
					var wrapper = new ShaderGUIWrapper(customShaderGUI, evt);
					customShaderGUIField?.SetValue(editor, wrapper);
				}
			}
		}

		private class MaterialPropertyDrawerWrapper : MaterialPropertyDrawer
		{
			private readonly MaterialPropertyDrawer original;
			private readonly PropertyChangedEvent propertyChangedEvent;

			public MaterialPropertyDrawerWrapper(MaterialPropertyDrawer original, PropertyChangedEvent propertyChangedEvent)
			{
				this.original = original;
				this.propertyChangedEvent = propertyChangedEvent;
			}

			public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
			{
				if (original != null) return original.GetPropertyHeight(prop, label, editor);
				return MaterialEditor.GetDefaultPropertyHeight(prop);
				// return base.GetPropertyHeight(prop, label, editor);
			}

			public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
			{
				using var change = new EditorGUI.ChangeCheckScope();
				if (this.original != null)
				{
					this.original.OnGUI(position, prop, label, editor);
				}
				else
				{
					editor.DefaultShaderProperty(position, prop, label.text);
				}
				
				if (change.changed)
				{
					propertyChangedEvent?.Invoke(editor.targets, prop.name, GetValue(prop));
				}
			}
		}

		private class ShaderGUIWrapper : ShaderGUI
		{
			private readonly ShaderGUI shaderGUI;
			private readonly PropertyChangedEvent propertyChangedEvent;
			private Object[] currentTargets;

			public ShaderGUIWrapper(ShaderGUI shaderGUI, PropertyChangedEvent propertyChangedEvent)
			{
				this.shaderGUI = shaderGUI;
				this.propertyChangedEvent = propertyChangedEvent;
			}

			public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
			{
				this.currentTargets = materialEditor.targets;
				foreach (var prop in properties)
				{
					prop.applyPropertyCallback = this.OnPropertyChanged;
				}
				this.shaderGUI.OnGUI(materialEditor, properties);
			}

			private bool OnPropertyChanged(MaterialProperty prop, int changemask, object previousvalue)
			{
				propertyChangedEvent?.Invoke(currentTargets, prop.name, GetValue(prop));
				return false;
			}
		}

		private static object GetValue(MaterialProperty property)
		{
			switch (property.type)
			{
				case MaterialProperty.PropType.Color:
					return property.colorValue;
				case MaterialProperty.PropType.Vector:
					return property.vectorValue;
				case MaterialProperty.PropType.Float:
				case MaterialProperty.PropType.Range:
					return property.floatValue;
				case MaterialProperty.PropType.Texture:
					return property.textureValue;
			}
			return null;
		}
	}
}