using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Needle.Engine.Codegen;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Needle.Engine.ProjectBundle
{
	// TODO: add warning when vite.config is detected but resolve is missing dependencies required by this package

	[CustomEditor(typeof(BundleImporter))]
	public class BundleImporterEditor : Editor
	{
		private static readonly Dictionary<string, (Bundle bundle, bool dirty)> cache =
			new Dictionary<string, (Bundle bundle, bool dirty)>();

		private string filePath;
		private ExportInfo exportInfo;
		private ComponentGenerator componentGenerator;
		private Texture2D npmLogo;

		private void OnEnable()
		{
			exportInfo = ExportInfo.Get();
			componentGenerator = FindAnyObjectByType<ComponentGenerator>();
			Load();
			BundleRegistry.Instance.EnsureWatching();
			npmLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(
				AssetDatabase.GUIDToAssetPath("dace6e0299c6683409dd99e9ac45a74b"));
		}

		private void Load()
		{
			var importer = target as BundleImporter;
			var path = importer?.assetPath;
			this.filePath = path;
			if (path == null) return;
			if (cache.ContainsKey(path)) return;
			if (!File.Exists(path)) return;
			var json = File.ReadAllText(path);
			var b = JsonConvert.DeserializeObject<Bundle>(json);
			if (b == null) return;
			b.FilePath = path;
			cache.Add(path, (b, false));
			b.Validate();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			// when installed from immutable source we still want to be able to use the GUI and buttons (since they all operate on the hidden folder its ok)
			var enabled = GUI.enabled;
			try
			{
				GUI.enabled = true;
				InternalOnGUI();
			}
			finally
			{
				GUI.enabled = enabled;
			}
		}

		private void InternalOnGUI()
		{
			const int buttonWidth = 50;

			if (cache == null) return;
			if (cache.TryGetValue(filePath, out var kvp))
			{
				using var change = new EditorGUI.ChangeCheckScope();
				var bundle = kvp.bundle;
				var packagePath = bundle.PackageFilePath;
				var packageExists = File.Exists(packagePath);
				// var fileName = Path.GetFileName(filePath);
				var packageName = bundle.FindPackageName();

				// var bundleName = Path.GetFileNameWithoutExtension(filePath);
				// var expectedBundleName = Regex.Replace(packageName.TrimStart('@'), "[\\/]", ".");
				// if (!string.Equals(expectedBundleName, bundleName, StringComparison.OrdinalIgnoreCase))
				// {
				// 	EditorGUILayout.HelpBox("Package name does not match npmdef name. Consider renaming your npmdef to " + expectedBundleName,
				// 		MessageType.Warning);
				// }

				EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);

				var rendersError = false;
				if (!packageExists)
				{
					rendersError = true;
					EditorGUILayout.HelpBox(
						"NPM Package is missing at " + packagePath +
						"\nDid you copy the npmdef outside of Unity? Please make sure to copy the hidden package folder too.",
						MessageType.Error);
					if (GUILayout.Button("Relink"))
					{
						bundle.SelectLocalPath();
					}
				}
				else if (string.IsNullOrWhiteSpace(packageName))
				{
					rendersError = true;
					using (new GUILayout.HorizontalScope())
					{
						EditorGUILayout.HelpBox("Invalid package: Missing name", MessageType.Error);
						if (GUILayout.Button("Fix", GUILayout.Width(buttonWidth), GUILayout.Height(38)))
							EditorUtility.OpenWithDefaultApp(Path.GetFullPath(bundle.PackageFilePath));
					}
				}

				if (rendersError)
					GUILayout.Space(10);
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(new GUIContent("Name", packagePath), new GUIContent(packageName ?? "<Missing name>", packagePath));
					using (new EditorGUI.DisabledScope(!packageExists))
					{
						if (GUILayout.Button(
							    new GUIContent("Edit",
								    "Tip: You can also double click the .npmdef asset in Unity's project browser"),
							    GUILayout.Width(buttonWidth)))
						{
							EditorUtility.OpenWithDefaultApp(Path.GetFullPath(bundle.PackageFilePath));
						}
					}
				}
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("Version", bundle.FindPackageVersion());
					using (new EditorGUI.DisabledScope(!packageExists))
					{
						if (GUILayout.Button(
							    new GUIContent("Edit",
								    "Tip: You can also double click the .npmdef asset in Unity's project browser"),
							    GUILayout.Width(buttonWidth)))
						{
							EditorUtility.OpenWithDefaultApp(Path.GetFullPath(bundle.PackageFilePath));
						}
					}
				}
				
				using (new GUILayout.HorizontalScope())
				{
					var value = "Embedded";
					var tooltip = default(string);
					var defaultTooltip =
						"The npm package can either be \n— embedded (hidden but inside your Unity project, next to the npmdef)\n— local (via some local path on your disc) \n— or remote (being loaded from a package registry like npm, it is immutable in this case)";
					if (bundle.IsLocal)
					{
						value = "Local";
						tooltip = Path.GetFullPath(bundle.localPath);
					}
					EditorGUILayout.LabelField(new GUIContent("Location", defaultTooltip), new GUIContent(value, tooltip ?? defaultTooltip));
				}
				if (bundle.IsLocal)
				{
					EditorGUILayout.HelpBox(bundle.PackageDirectory, MessageType.None);
				}

				// GUILayout.Space(3);

				if (!packageExists)
				{
					// using (new GUILayout.HorizontalScope())
					// {
					// 	EditorGUILayout.HelpBox("Package is not valid: package.json does not exist at " + packagePath, MessageType.Warning);
					// 	// if (GUILayout.Button("Try find package", GUILayout.Height(38)))
					// 	// {
					// 	// 	var sourceDir = Path.GetDirectoryName(bundle.FilePath)!;
					// 	// 	foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*~"))
					// 	// 	{
					// 	// 		var jsonPath = dir + "/package.json";
					// 	// 		if (!File.Exists(jsonPath)) continue;
					// 	// 		var from = Path.GetFullPath(bundle.FilePath); 
					// 	// 		bundle.Path = new Uri(from).MakeRelativeUri(new Uri(Path.GetFullPath(jsonPath))).ToString();
					// 	// 		GUIUtility.keyboardControl = -1;
					// 	// 	}
					// 	// }
					// }
				}

				// GUILayout.Space(5);
				// if (change.changed)
				// {
				// 	kvp.dirty = true;
				// 	cache[filePath] = kvp;
				// }
				// using (new EditorGUI.DisabledScope(!kvp.dirty))
				// {
				// 	using (new EditorGUILayout.HorizontalScope())
				// 	{
				// 		GUILayout.FlexibleSpace();
				// 		if (GUILayout.Button("Revert", GUILayout.Width(buttonWidth)))
				// 		{
				// 			// remove focus
				// 			GUIUtility.keyboardControl = -1;
				// 			cache.Remove(filePath);
				// 			Load();
				// 		}
				// 		if (GUILayout.Button("Apply", GUILayout.Width(buttonWidth)))
				// 		{
				// 			GUIUtility.keyboardControl = -1;
				// 			kvp.dirty = false;
				// 			cache[filePath] = kvp;
				// 			bundle.Save(filePath);
				// 		}
				// 	}
				// }

				if (packageExists)
				{
					GUILayout.Space(10);
					var buttonOptions = new[] { GUILayout.Height(23) };
					EditorGUILayout.LabelField("Package", EditorStyles.boldLabel);

					var label = "Show in Explorer";
#if UNITY_EDITOR_OSX
					label = "Reveal in Finder";
#endif

					if (GUILayout.Button(label))
					{
						EditorUtility.RevealInFinder(packagePath);
					}

					if (GUILayout.Button(new GUIContent("Open Workspace",
						    "You can also double click the npmdef asset in Unity's project browser")))
					{
						Actions.OpenWorkspace(Path.GetFullPath(bundle.PackageDirectory));
					}

					// using (new EditorGUI.DisabledScope(!bundle.IsMutable()))
					// {
					if (GUILayout.Button(
						    new GUIContent("Install for Development", "Run npm install in " + bundle.PackageDirectory),
						    buttonOptions))
					{
						Debug.Log("Install " + packageName);
						Actions.InstallBundle(bundle);
					}
					// }

					if (exportInfo)
					{
						using (new EditorGUI.DisabledScope(!exportInfo.Exists()))
						{
							var projectName = exportInfo.DirectoryName;
							var installed = bundle.IsInstalled(exportInfo.PackageJsonPath);
							var buttonContent = installed
								? new GUIContent($"Remove from {projectName}",
									"Removes this package from the current project at " +
									exportInfo.GetProjectDirectory())
								: new GUIContent($"Install in Project \"{projectName}\"",
									"Adds this package to the current project at " + exportInfo.GetProjectDirectory());
							if (GUILayout.Button(buttonContent, buttonOptions))
							{
								if (installed) bundle.Uninstall(exportInfo);
								else bundle.Install(exportInfo);
							}
						}

						// 	if (GUILayout.Button(
						// 		    new GUIContent("Re-Generate Components",
						// 			    "Click to re-generate Unity components from typescript files found in this NPM Definition project"), buttonOptions))
						// 	{
						// 		if (!componentGenerator)
						// 		{
						// 			componentGenerator = Undo.AddComponent<ComponentGenerator>(exportInfo.gameObject);
						// 		}
						//
						// 		var imports = new List<ImportInfo>();
						// 		bundle.FindImports(imports, exportInfo.DirectoryName);
						// 		foreach (var script in imports)
						// 		{
						// 			componentGenerator.ScheduleGenerateComponent(script.FilePath);
						// 		}
						// 	}
					}

					// if (GUILayout.Button("Update SubAssets"))
					// {
					// 	BundleImporter.MarkDirty(bundle);
					// }

// #if UNITY_EDITOR_WIN
// 					if (GUILayout.Button("Open in Commandline"))
// 					{
// 						ProcessUtils.OpenCommandLine(bundle.PackageDirectory);
// 					}
// #endif

					if (GUILayout.Button(new GUIContent("Move NPM Package","Move the hidden NPM project to a different location")))
					{
						// if (EditorUtility.DisplayDialog("Move npm package",
						// 	    "Are you sure you want the package " + bundle.Name + " to a different location?",
						// 	    "Yes - Select a Folder", "No don't move"))
						{
							bundle.SelectLocalPath();
						}
						// else Debug.Log("Moving package " + bundle.Name + " cancelled");
					}
					if (bundle.IsLocal)
					{
						if (GUILayout.Button(new GUIContent("Relink NPM Package","Select a new npm package directory")))
						{
							var currentPath = bundle.localPath;
							bundle.localPath = "";
							if(!bundle.SelectLocalPath()) bundle.localPath = currentPath;
						}
					}

					GUILayout.Space(5);
					EditorGUILayout.LabelField("Publishing", EditorStyles.boldLabel);
					if (GUILayout.Button(new GUIContent("Publish to NPM")))
					{
						PackageUtils.Publish(bundle.PackageDirectory);
					}

					GUILayout.Space(5);
					EditorGUILayout.LabelField("Component Generator", EditorStyles.boldLabel);
					if (GUILayout.Button(new GUIContent("Regenerate Components",
						    "Will re-generate all Unity stub components from the Typescript/Javascript files found in this NpmDef")))
					{
						BundleRegistry.Instance.RunCodeGenForBundle(bundle);
					}

					if (GUILayout.Button(new GUIContent("Regenerate C# Types",
						    "Click to regenerate the C# type map which is used to automatically resolve C# types from Typescript types when generating Stub Unity Components. If you create a new C# type in Unity and expect it to be automatically resolved you might need to click this button")))
					{
						TypesGenerator.GenerateTypesAndShow();
					}
					var watcher = BundleRegistry.Instance.TryFindWatcher(bundle);
					if (watcher != null && watcher.GeneratorPath != null)
					{
						var path = Path.GetFullPath(Path.GetDirectoryName(watcher.GeneratorPath) +
						                            "../../package.json");
						if (PackageUtils.TryGetVersion(path, out var ver))
						{
							using (new ColorScope(new Color(.7f, .7f, .7f)))
								EditorGUILayout.LabelField("Component Generator Version: " + ver);
						}
					}
					EditorGUILayout.Space(-20);
				}
			}
		}
	}
}