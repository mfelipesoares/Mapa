using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Shaders
{
	internal static class CustomShaderInspectorDrawer
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			InspectorHook.Inject += OnInject;
		}

		private static void OnInject(Editor editor, VisualElement ui)
		{
			if(editor.targets.Length > 1) return;

			var target = editor.target;
			var targetMat = target as Material;
			if (targetMat) target = targetMat.shader;
			
			if (target is Shader shader)
			{
				if (shader.name.StartsWith("UnityGLTF/")) return;
				// We can not edit immutable shaders
				var shaderPath = AssetDatabase.GetAssetPath(shader);
				// if (shaderPath.Contains("PackageCache")) return;
				// Only shadergraph shaders are supported
				
				// TODO do we want to show the upgrade button for all Skybox/Cubemap?
				// Or only with ExportInfo? Or only when set as scene skybox?
				if (shader.name == "Skybox/Cubemap" && ExportInfo.Get())
				{
					var skyboxUI = new VisualElement();
					var hint = new HelpBox("This skybox can be upgraded to a Needle Skybox. It supports a nice background blur and more options.\n\n", HelpBoxMessageType.None);
					hint.style.marginTop = 5;
					skyboxUI.Add(hint);
					var group = new VisualElement() { style =
					{
						flexDirection = FlexDirection.Row,
						position = Position.Absolute,
						alignContent = Align.FlexEnd,
						right = 0,
						bottom = 2,
					} };
					var upgradeButton = new Button(() =>
					{
						var mat = RenderSettings.skybox;
						if (!mat) return;
						Undo.RegisterCompleteObjectUndo(mat, "Changed Skybox Shader to Better Cubemap (Needle)");
						if (editor is MaterialEditor materialEditor)
							// This will call AssignNewShaderToMaterial correctly in contrast to mat.shader = ...
							materialEditor.SetShader(Shader.Find("Skybox/Better Cubemap (Needle)"));
						EditorUtility.SetDirty(mat);
						skyboxUI.parent.Remove(skyboxUI);
					})
					{
						text = "Upgrade now",
					};
					group.Add(upgradeButton);
					hint.Add(group);
					skyboxUI.style.marginBottom = 10;
					if (ui.childCount > 1)
						ui.Insert(1, skyboxUI);
					else 
						ui.Add(skyboxUI);
					
					return;
				}
				if (!shaderPath.EndsWith(".shadergraph")) return;
				
				var isMarkedForExport = ShaderExporterRegistry.HasExportLabel(shader);
				var customShaderSettings = new VisualElement();
				var canBeEdited = !AssetDatabase.IsSubAsset(shader) && !shaderPath.Contains("PackageCache");
				customShaderSettings.SetEnabled(canBeEdited);
				if (!canBeEdited)
				{
					customShaderSettings.tooltip = "This shader can not be edited because it is a sub-asset or inside an immutable package. Please extract the shader/material first";
				}
				customShaderSettings.style.paddingLeft = 10;
				customShaderSettings.style.paddingRight = 10;
				customShaderSettings.style.marginTop = 10;
				customShaderSettings.style.marginBottom = 10;
				
				// insert the custom shader options at the top after the header
				if (ui.childCount > 1)
					ui.Insert(1, customShaderSettings);
				else 
					ui.Add(customShaderSettings);
				
				customShaderSettings.Add(new Label("Needle Engine — Custom Shader Settings")
				{
					style =
					{
						unityFontStyleAndWeight = FontStyle.Bold,
						marginBottom = 5
					}
				});
				
				var helpBox = new HelpBox();
				var link = new Label("Open Documentation " + Constants.ExternalLinkChar)
				{
					style = { color = new Color(.3f, .6f, 1f), marginTop = 3}
				};
				link.AddManipulator(new Clickable(() =>
				{
					Application.OpenURL(Constants.DocumentationUrlCustomShader);
				}));
				var toggle = new Toggle("Export as Custom Shader");
				toggle.value = isMarkedForExport;
				toggle.RegisterValueChangedCallback(evt =>
				{
					ShaderExporterRegistry.SetExportLabel(shader, evt.newValue);
					OnExportSettingHasChanged();
				});

				
				customShaderSettings.Add(toggle);
				customShaderSettings.Add(helpBox);
				
				if (!canBeEdited)
				{
					var helpbox2 = new HelpBox();
					helpbox2.text += "This shader can not be edited because it is a sub-asset or inside an immutable package. Please extract the shader/material first (click this message to ping the shader)";
					helpbox2.messageType = (HelpBoxMessageType) MessageType.Warning;
					helpbox2.AddManipulator(new Clickable(() =>
					{
						EditorGUIUtility.PingObject(shader);
					}));
					customShaderSettings.Add(helpbox2);
				}
				
				customShaderSettings.Add(link);
				
				void OnExportSettingHasChanged()
				{
					isMarkedForExport = ShaderExporterRegistry.HasExportLabel(shader);
					helpBox.text = isMarkedForExport 
						? "This shader will be export as a custom shader to Needle Engine (Note that we only support exporting custom Unlit shaders at the moment)" 
						: "This shader is not marked as a custom shader for export to Needle Engine. It will instead be exported as a standard glTF material.";
					helpBox.messageType = (HelpBoxMessageType)(isMarkedForExport ? MessageType.Info : MessageType.Warning);
					toggle.value = isMarkedForExport;
				}
				OnExportSettingHasChanged();
				

				// var imgui = new IMGUIContainer(() =>
				// {
				// 	GUILayout.Space(10);
				// 	GUILayout.Label("Needle Engine — Custom Shader Settings", EditorStyles.boldLabel);
				// 	GUILayout.Space(5);
				// 	if (isMarkedForExport)
				// 		EditorGUILayout.HelpBox("This shader is marked for export", MessageType.Info);
				// 	else
				// 		EditorGUILayout.HelpBox("This shader is not marked for export", MessageType.Warning);
				// 	
				// 	if (GUILayout.Button("Open Shader Editor"))
				// 	{
				// 		
				// 	}
				// });
				// ui.Add(imgui);
			}
		}
	}
}