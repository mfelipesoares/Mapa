using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Needle.Engine.Editors
{
	public static class InspectorComponentLink
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			exportInfo = ExportInfo.Get();
			InspectorHook.Inject += OnInject;
		}

		private static readonly Type[] ignore =
		{
			typeof(GameObject)
		};

		private static void OnInject(Editor editor, VisualElement visualElement)
		{
			var type = editor.target?.GetType();
			if (ignore.Contains(type)) return;
			var children = visualElement.Children();
			var header = children.FirstOrDefault();
			var drawer = new TypescriptHookDrawer(header, editor.target, type);
			visualElement.Insert(1, drawer);
		}

		private static ExportInfo exportInfo = null;
		private static readonly ProjectInfo projectInfo = new ProjectInfo(null);
		private static DateTime lastClick = DateTime.MinValue;

		private class TypescriptHookDrawer : VisualElement
		{
			public TypescriptHookDrawer(VisualElement header, Object target, Type type)
			{
				this.type = type;
				this.target = target;
				this.typeName = type.Name;

				var group = new VisualElement();
				this.label = new Label();
				group.Add(this.label);
				
				this.postfix = new Label();
				group.Add(this.postfix);
				this.Add(group);
				
				this.experimentalWarning = new HelpBox("This component is experimental and may change in future updates", HelpBoxMessageType.Warning);
				this.experimentalWarning.style.display = DisplayStyle.None;
				this.Add(this.experimentalWarning);
				
				this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
				header.RegisterCallback<ClickEvent>(OnClickedHeader);
				// this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

				this.style.overflow = Overflow.Hidden;
				this.style.minHeight = 12;
				this.style.paddingTop = 4;
				this.style.paddingBottom = 2;
				this.style.paddingLeft = 18;
				this.style.display = DisplayStyle.Flex;
				group.style.flexDirection = FlexDirection.Row;

				var clickable = new Clickable(OnClick);
				this.label.AddManipulator(clickable);
				this.UpdateVisibility();
			}

			private async void OnClick()
			{
				if (this.matchingTypescript != null)
				{
					
					// try open workspace in current package json directory
					// this should open the npmDef workspace when the file is part of an npmDef
					var packageJsonDirectory = this.matchingTypescript.PackageJson?.DirectoryName;

					
					// Clicking an uninstalled package should ping the npmdef is it exists
					if (this.matchingTypescript.IsInstalled == false)
					{					
						var now = DateTime.Now;
						var timeSinceLastClick = now - lastClick;
						lastClick = now;
						
						// When double clicking still open the default file
						if (timeSinceLastClick.TotalSeconds > .5f && packageJsonDirectory != null && packageJsonDirectory.EndsWith("~"))
						{
							var npmdefPath = packageJsonDirectory.TrimEnd('~') + ".npmdef";
							if (File.Exists(npmdefPath))
							{
								var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
									PathUtils.MakeProjectRelative(npmdefPath));
								if (asset)
								{
									SceneView.lastActiveSceneView?.ShowNotification(new GUIContent("Script is not installed. See console for details"));
									Debug.Log(
										$"The NpmDef package at \"{npmdefPath}\" containing this script is not installed in your current web project.\n<b>To install it</b> drag it into the Dependencies list of the ExportInfo component (or click the \"Install in Project\" button in the Npmdef asset): {asset}", asset);
									EditorGUIUtility.PingObject(asset);
									return;
								}
							}
						}
					}
					
					var defaultEditor = ExporterUserSettings.instance.UseVSCode == false;
					var filePath = this.matchingTypescript.FilePath;
					if (!string.IsNullOrEmpty(packageJsonDirectory))
					{
						WorkspaceUtils.OpenWorkspace(packageJsonDirectory, false, defaultEditor, filePath);
						return;
					}
					if (exportInfo != null)
					{
						WorkspaceUtils.OpenWorkspace(exportInfo.DirectoryName, false, defaultEditor, filePath);
						return;
					}
					// fallback to just open the file if we dont have a workspace etc
					if (defaultEditor)
					{
						await WorkspaceUtils.OpenWithDefaultEditor(filePath);
					}
					else EditorUtility.OpenWithDefaultApp(filePath);
				}
			}

			private readonly string typeName;
			private readonly Object target;
			private readonly Type type = null;
			private readonly Label label = null;
			private readonly Label postfix = null;
			private ImportInfo matchingTypescript = null;
			private bool showLabel = false;
			private VisualElement possibleWrongComponentInfo = null;
			private HelpBox experimentalWarning;

			// To be able to switch to other scenes and still get the type info and links from the last opened project
			private static string _lastProjectDirectory
			{
				get => SessionState.GetString("Needle-InspectorLink-LastProjectDir", "");
				set => SessionState.SetString("Needle-InspectorLink-LastProjectDir", value);
			}
            
			private void OnAttachToPanel(AttachToPanelEvent evt)
			{
				if (!exportInfo)
					exportInfo = ExportInfo.Get();
				if (exportInfo) _lastProjectDirectory = exportInfo.DirectoryName;
			
				IReadOnlyList<ImportInfo> types = default;
				if (_lastProjectDirectory != null)
				{
					projectInfo.UpdateFrom(_lastProjectDirectory);
					types = TypesUtils.GetTypes(projectInfo);
				}
				
				this.showLabel = false;
				matchingTypescript = null;
				this.label.text = "";
				this.label.tooltip = "";
				
				if (types != null)
				{
					var match = types.FirstOrDefault(
						k => string.Equals(k.TypeName, this.typeName, StringComparison.InvariantCultureIgnoreCase)
					);
					this.matchingTypescript = match;
					if (match != null)
					{
						var ext = Path.GetExtension(match.FilePath);
						this.showLabel = true;
						var col = match.IsInstalled ? new Color(.7f, .8f, 1f) : new Color(.8f, .5f, .7f);
						this.label.style.color = new StyleColor(col);
						this.label.text = $"Open {match.TypeName}{ext} {Constants.ExternalLinkChar}";
						this.label.tooltip = "Click to open the TypeScript component\n\n " +
						                     Path.GetFullPath(match.FilePath!);
						if (!match.IsInstalled) this.label.tooltip += "\n\n(not installed in current project)";

						this.postfix.style.color = new StyleColor(new Color(.7f, .7f, .7f));
						this.postfix.style.paddingLeft = 5;
						var packageName = match.PackageName;
						if (!string.IsNullOrWhiteSpace(packageName))
						{
							this.postfix.text = "in " + packageName;
						}
						else if (exportInfo.Exists())
						{
							var webProjectPath = Path.GetFullPath(exportInfo.GetProjectDirectory());
							if (match.FilePath.StartsWith(webProjectPath))
							{
								this.postfix.text = "in " + new FileInfo(match.FilePath).Directory?.Name + " (current web project)";
							}
						}
						this.postfix.tooltip = match.FilePath;

						if (match.FilePath.Contains("experimental", StringComparison.OrdinalIgnoreCase))
						{
							this.experimentalWarning.style.display = DisplayStyle.Flex;
						}

						if (HasCodegenFileButIsNotUsingCodeGenFile(match, out _))
						{
							if (possibleWrongComponentInfo == null)
							{
								var warningLabel =
									new Label(
										text:
										"A generated component exists for this type but you are not using it here. Changes in codegen will not be reflected. Check for duplicate script names.");
								possibleWrongComponentInfo = warningLabel;
								possibleWrongComponentInfo.style.fontSize = 10;
								possibleWrongComponentInfo.style.whiteSpace = WhiteSpace.Normal;
								possibleWrongComponentInfo.style.color = new StyleColor(new Color(.7f, .7f, .7f));
								this.Add(possibleWrongComponentInfo);
							}
						}
						else if (this.possibleWrongComponentInfo?.parent != null)
						{
							this.possibleWrongComponentInfo.RemoveFromHierarchy();
						}
					}
					else
					{
						this.RemoveFromHierarchy();
					}
				}
				this.UpdateVisibility();
			}

			private void OnClickedHeader(ClickEvent evt)
			{
				UpdateVisibility();
			}

#if UNITY_2022_1_OR_NEWER
			protected override void ExecuteDefaultAction(EventBase evt)
			{
				base.ExecuteDefaultAction(evt);
				this.UpdateVisibility();
			}
#else
			public override void HandleEvent(EventBase evt)
			{
				base.HandleEvent(evt);
				this.UpdateVisibility();
			}
#endif

			private void UpdateVisibility()
			{
				var exp = showLabel && InternalEditorUtility.GetIsInspectorExpanded(this.target);
				this.visible = exp;
				this.style.display = (exp ? DisplayStyle.Flex : DisplayStyle.None);
			}

			private bool HasCodegenFileButIsNotUsingCodeGenFile(ImportInfo typeInfo, out string codeGenFilePath)
			{
				codeGenFilePath = null;
				if (!(target is MonoBehaviour mb)) return false;
				var script = MonoScript.FromMonoBehaviour(mb);
				var targetPath = AssetDatabase.GetAssetPath(script);
				if (targetPath.Contains(".codegen/")) return false;
				var typePathEnd = "/" + typeInfo.TypeName + ".cs";
				var paths = AssetDatabase.FindAssets(typeInfo.TypeName).Select(AssetDatabase.GUIDToAssetPath)
					.Where(e => e.EndsWith(typePathEnd));
				foreach (var p in paths)
				{
					if (p.Contains(".codegen/"))
					{
						codeGenFilePath = p;
						return true;
					}
				}
				return false;
			}
		}
	}
}