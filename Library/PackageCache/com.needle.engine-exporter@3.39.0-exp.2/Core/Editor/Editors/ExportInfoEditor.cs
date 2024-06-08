using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Needle.Engine.Problems;
using Needle.Engine.Projects;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors
{
	[CustomEditor(typeof(ExportInfo))]
	public class ExportInfoEditor : Editor
	{
		private static int HasNpmInstalled
		{
			get => SessionState.GetInt("NEEDLE_NPM_INSTALLED", -1);
			set => SessionState.SetInt("NEEDLE_NPM_INSTALLED", value);
		}
		private static bool hasNpmInstalled => HasNpmInstalled != 0;
		
		private static int HasNodeInstalled
		{
			get => SessionState.GetInt("NEEDLE_NODE_INSTALLED", -1);
			set => SessionState.SetInt("NEEDLE_NODE_INSTALLED", value);
		}
		private static bool hasNodeInstalled => HasNodeInstalled != 0;
		private static string nodejsVersionInstalled;

		private static GUIStyle _multiLineLabel;

		private static GUIStyle multilineLabel
		{
			get
			{
				_multiLineLabel ??= new GUIStyle(EditorStyles.label);
				_multiLineLabel.wordWrap = true;
				return _multiLineLabel;
			}
		}

		private readonly List<IExportableObject> exportableObjectsInScene = new List<IExportableObject>();
		private readonly IDictionary<string, IList<IProblem>> problems = new Dictionary<string, IList<IProblem>>();

		public static event Action<ExportInfo> Enabled, LateInspectorGUI;
		
		private SerializedProperty directoryNameProperty, remoteUrlProperty, autoExportProperty, autoCompressProperty;
		private const int PAD_RIGHT = 0;
		private const string customTemplateName = "Custom Template";
		private static int customTemplateIndex = -1;
			
		private async void OnEnable()
		{
			var templates = ProjectGenerator.Templates.Where(t => t).Select(t => t.DisplayName).ToList();
			templates.Add(customTemplateName);
			customTemplateIndex = templates.Count - 1;
			templateOptions = templates.ToArray();

			var projectOptionsList = new List<string>();
			var projectOptionsDisplayList = new List<string>();
			foreach (var proj in ProjectsData.EnumerateProjects())
			{
				if (!proj.Exists) continue;
				projectOptionsList.Add(proj.ProjectPath);
				projectOptionsDisplayList.Add(Regex.Replace(proj.ProjectPath, "[\\/\\.]", " ").Trim());
			}
			projectOptions = projectOptionsList.ToArray();
			projectDisplayOptions = projectOptionsDisplayList.ToArray();
			directoryNameProperty = serializedObject.FindProperty(nameof(ExportInfo.DirectoryName));
			remoteUrlProperty = serializedObject.FindProperty(nameof(ExportInfo.RemoteUrl));
			autoExportProperty = serializedObject.FindProperty(nameof(ExportInfo.AutoExport));
			autoCompressProperty = serializedObject.FindProperty(nameof(ExportInfo.AutoCompress));
			
			Enabled?.Invoke(target as ExportInfo);

			exportableObjectsInScene.Clear();
			ObjectUtils.FindObjectsOfType(exportableObjectsInScene);
			
			ValidateProject();
			_GetNodejsVersion();
			_HasToktxInstalled();

			if (HasNpmInstalled == -1)
			{
				var res = await Actions.HasNpmInstalled();
				HasNpmInstalled = res ? 1 : 0;
			}
			if (HasNpmInstalled == 1)
			{
				TestServerIsRunning();
				Actions.RunProjectSetupIfNecessary();
			}
			
		}

		private bool _hasToktxInstalled = true;
		private void _HasToktxInstalled()
		{
			_hasToktxInstalled = Actions.HasMinimumToktxVersionInstalled(out _);
		}

		private async void _GetNodejsVersion()
		{
			var node = await InternalActions.HasSupportedNodeJsInstalled();
			HasNodeInstalled = node ? 1 : 0;
			nodejsVersionInstalled = await InternalActions.GetNodeJsVersion();
			if (!node)
			{
				await Task.Delay(5000);
				_GetNodejsVersion();
			}
		}

		private void OnDisable()
		{
			serverIsRunningRoutine = false;
		}

		private bool serverIsRunning = true;
		private bool serverIsRunningRoutine = false;
		private static string localServerUrl = null;

		private async void TestServerIsRunning()
		{
			if (serverIsRunningRoutine) return;
			serverIsRunningRoutine = true;
			localServerUrl = await WebHelper.GetLocalServerUrl();
			var failedPings = 0;
			while (serverIsRunningRoutine)
			{
				var res = await WebHelper.IsResponding(localServerUrl);
				if (!res)
				{
					// test both http and https
					var testOtherOption = true;
					if (localServerUrl.StartsWith("https:"))
					{
#if UNITY_2022_1_OR_NEWER
						// can't test further if insecure http is not allowed on 2022.x+
						if (PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed)
						{
							testOtherOption = false;
						}
#endif
						localServerUrl = localServerUrl.Replace("https:", "http:");
					}
					else if (localServerUrl.StartsWith("http:")) 
						localServerUrl = localServerUrl.Replace("http:", "https:");
					
					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if(testOtherOption)
						res = await WebHelper.IsResponding(localServerUrl);
				}
				if (!res) failedPings += 1;
				else failedPings = 0;
				serverIsRunning = res;
				await Task.Delay(2000 + (failedPings * 1000));
			}
		}

		internal static int selectedTemplateIndex
		{
			get => SessionState.GetInt("NeedleThreeProjectTemplateIndex", 0);
			set => SessionState.SetInt("NeedleThreeProjectTemplateIndex", value);
		}

		public override VisualElement CreateInspectorGUI()
		{
			var t = target as ExportInfo;
			if (!t) return null;
			var root = new VisualElement();
			var header = new VisualElement();
			UIComponents.BuildHeader(header);
			header.style.paddingBottom = 12;
			header.style.paddingLeft = 4;
			root.Add(header);
			var mainContainer = new IMGUIContainer();
			mainContainer.onGUIHandler += OnInspectorGUI;
			root.Add(mainContainer);
			VisualElementRegistry.Register(header);
			return root;
		}
		
		private static string[] templateOptions;
		private static string[] projectOptions, projectDisplayOptions;

		private static readonly string[] invalidDirectories =
		{
			"Assets", "Library", "Temp", "Packages", "Logs", ".idea", "obj", "ProjectSettings", "UserSettings"
		};

		public override void OnInspectorGUI()
		{
			var t = target as ExportInfo;
			if (!t) return;
			GUI.color = Color.white;
			if (directoryNameProperty == null) return;

			// if (EulaWindow.RequiresEulaAcceptance)
			// {
			// 	GUILayout.Space(10);
			// 	EditorGUILayout.HelpBox("In order to use Needle Engine you must read and accept the Needle Engine EULA", MessageType.Warning);
			// 	if(GUILayout.Button("Open EULA Window", GUILayout.Height(32)))
			// 		EulaWindow.Open();
			// 	GUILayout.Space(10);
			// 	return;
			// }

			if (hasNodeInstalled == false)
			{
				ProjectValidation.DrawMinimalInfoIfAnythingNeedsInstallation(true);
				GUILayout.Space(10);
				ShowVersionInfo(t.GetProjectDirectory());
				return;
// 				var showWarningOnly = false;
// 				var msg = "No Nodejs found on this machine:\nPlease install Nodejs 16 or 18";
// 				if (nodejsVersionInstalled != null)
// 				{
// 					var version = nodejsVersionInstalled;
// 					if (string.IsNullOrWhiteSpace(version)) version = "Nodejs not installed or found.";
// 					else version = "Unsupported Nodejs version installed: " + version;
// 					msg = $"{version}\nPlease install Node 18 or Node 20";
// 				}
// 				EditorGUILayout.HelpBox(msg, MessageType.Warning);
// 				if (showWarningOnly == false)
// 				{
// 					if(GUILayout.Button("Open Nodejs Download Page " + Constants.ExternalLinkChar, GUILayout.Height(32)))
// 						Application.OpenURL("https://nodejs.org/en/download/");
// #if !UNITY_EDITOR_WIN
// 				EditorGUILayout.HelpBox($"You may need to add an additional search path to the settings if you are using nvm for example.", MessageType.None);
// #endif
// 					if(GUILayout.Button("Open Settings"))
// 						Actions.OpenNeedleExporterProjectSettings();
// 					GUILayout.Space(10);
// 					return;
// 				}
			}

			var projectDirectory = t.DirectoryName;
			var invalidCharacterIndex = projectDirectory.IndexOfAny(Path.GetInvalidPathChars());
			var hasInvalidCharacters = invalidCharacterIndex != -1;
			var hasPath = !string.IsNullOrWhiteSpace(t.DirectoryName);
			var projectExists = hasPath
			                    && Directory.Exists(t.DirectoryName)
			                    && File.Exists(t.DirectoryName + "/package.json");
			var fullDirectoryPath = t.GetProjectDirectory();
			var isInstalled = Directory.Exists(t.DirectoryName + "/node_modules");
			var isRepository = GitActions.IsCloneable(remoteUrlProperty.stringValue);

			using (var change = new EditorGUI.ChangeCheckScope())
			{
				var needExit = false;
				const int buttonRightWidth = 35;
				
				using (new EditorGUILayout.HorizontalScope())
				{
					var label = "Project Folder";
					var tooltip =
						"The web development folder. This is where your actual web project is located (and will be generated if it doesnt exist yet). The path is relative to your Unity project folder.";

					EditorGUILayout.PropertyField(directoryNameProperty, new GUIContent(label, tooltip));
					if (GUILayout.Button(("Pick"), GUILayout.Width(buttonRightWidth)))
					{
						needExit = true;
						var folder = Path.GetFullPath(t.GetProjectDirectory());
						if (!Directory.Exists(folder))
						{
							folder = Path.GetFullPath(Application.dataPath + "/../");
							var previouslySelectedPath = EditorPrefs.GetString("Needle_PreviouslySelectedProjectDirectory");
							if (!string.IsNullOrEmpty(previouslySelectedPath) && Directory.Exists(previouslySelectedPath) &&
							    previouslySelectedPath.StartsWith(folder))
								folder = previouslySelectedPath;
						}
						var selectedPath = EditorUtility.OpenFolderPanel("Select Needle Project folder", folder, string.Empty);
						if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
						{
							EditorPrefs.SetString("Needle_PreviouslySelectedProjectDirectory", Path.GetFullPath(selectedPath));
							if (!File.Exists(selectedPath + "/package.json"))
								selectedPath += "/" + SceneManager.GetActiveScene().name;
							Debug.Log("Selected path: " + selectedPath);
							while(selectedPath.EndsWith("/")) selectedPath = selectedPath.Substring(0, selectedPath.Length - 1);
							directoryNameProperty.stringValue = PathUtils.MakeProjectRelative(selectedPath);
						}
					}
				}

				var showRemoteProperty = isRepository && !projectExists &&
				                         remoteUrlProperty.stringValue != directoryNameProperty.stringValue;
				if (showRemoteProperty)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField("Repository", remoteUrlProperty.stringValue, EditorStyles.linkLabel);
						var lastRect = GUILayoutUtility.GetLastRect();
						EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
						if(Event.current.button == 0 && Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
							Application.OpenURL(remoteUrlProperty.stringValue);
						if (GUILayout.Button(("X"), GUILayout.Width(buttonRightWidth)))
						{
							if (EditorUtility.DisplayDialog("Remove repository",
								    "Are you sure you want to remove the repository reference?\nIf you remove the link to \"" + remoteUrlProperty.stringValue + "\" you won't be able to clone the remote project. You can undo this action.", "Yes, remove repository link", "No, don't remove the repository link"))
							{
								remoteUrlProperty.stringValue = "";
							}
						}
					}
				}
				else
				{
					EditorGUILayout.PropertyField(this.autoExportProperty);
					EditorGUILayout.PropertyField(this.autoCompressProperty);
				}
				
				if (change.changed)
				{
					this.serializedObject.ApplyModifiedProperties();
					TypesUtils.MarkDirty();
					if(needExit)
						GUIUtility.ExitGUI();
				}

				if (hasInvalidCharacters)
				{
					EditorGUILayout.HelpBox(
						$"Your project directory path contains invalid characters at index {invalidCharacterIndex} (\"{projectDirectory[invalidCharacterIndex]}\") and will not work. Please make sure you enter a valid path.", MessageType.Error);
					return;
				}
				
				GUILayout.Space(5);
			}
			// ComponentEditorUtils.DrawDefaultInspectorWithoutScriptField(this.serializedObject);

			
			if (!hasPath && isInstalled)
			{
				EditorGUILayout.HelpBox("Enter a directory name/path where the web project should be generated.", MessageType.Warning);
				GUILayout.Space(5);
			}
			// else
			// {
			// var path = string.IsNullOrWhiteSpace(t.DirectoryName) ? t.DirectoryName : Path.GetFullPath(t.DirectoryName);
			// if (projectExists)
			// 	EditorGUILayout.HelpBox("Project Directory:\n" + path, MessageType.None);
			// }


			var isValidDirectory = ExportInfo.IsValidDirectory(t.DirectoryName, out var invalidReason);
			if (hasPath && !isValidDirectory && !isRepository)
			{
				if (Path.IsPathRooted(t.DirectoryName))
				{
					using(new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.HelpBox("Absolute paths are not allowed!", MessageType.Error);
						if (GUILayout.Button("Fix", GUILayout.Width(50), GUILayout.Height(38)))
						{
							directoryNameProperty.stringValue = PathUtils.MakeProjectRelative(t.DirectoryName);
							this.serializedObject.ApplyModifiedProperties();
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox("The Project folder is not valid: " + invalidReason, MessageType.Error);
				}
			}


			if (hasPath && !Directory.Exists(t.DirectoryName))
			{
				if (t.IsTempProject() && !isRepository)
				{
					EditorGUILayout.HelpBox(
						"This is a temporary project path because it is in a directory that is generally excluded from VC. It will be generated from the default template.",
						MessageType.None);
				}
			}
			
#if UNITY_EDITOR_WIN
			// TODO: check if any dependency in project is a local path (only then symlinks are a problem)
			if (!DriveHelper.HasSymlinkSupport(fullDirectoryPath))
			{
				var hasFilePathDependency = false;
				if (PackageUtils.TryReadDependencies(fullDirectoryPath + "/package.json", out var deps))
				{
					foreach (var dep in deps)
					{
						if (hasFilePathDependency) break;
						if(PackageUtils.IsPath(dep.Value)) hasFilePathDependency = true;
					}
				}
				if (hasFilePathDependency)
				{
					EditorGUILayout.HelpBox("Your project is on a drive that does not support symlinks: Please choose a different drive for your project", MessageType.Error);
					return;
				}
				
				EditorGUILayout.HelpBox("Your project is on a drive that does not support symlinks: Please choose a different drive for your project", MessageType.Warning);
			}
#endif
			
			// EditorGUILayout.Space();
			
			if (hasPath && Directory.Exists(t.DirectoryName))
			{
				GUILayout.Space(5);

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Directory " + Constants.ExternalLinkChar))
					{
						var packagePath = t.DirectoryName + "/package.json";
						if (File.Exists(packagePath)) EditorUtility.RevealInFinder(Path.GetFullPath(packagePath));
						else EditorUtility.RevealInFinder(Path.GetFullPath(t.DirectoryName));
					}

					if (GUILayout.Button("Workspace " + Constants.ExternalLinkChar))
					{
						var useDefaultEditor = ExporterUserSettings.instance.UseVSCode == false;
						if (Event.current.modifiers == EventModifiers.Alt) useDefaultEditor = !useDefaultEditor;
						// We want the workspace to contain dependencies upon opening it via ExportInfo
						if (WorkspaceUtils.TryGetWorkspace(t.GetProjectDirectory(), out var workspacePath))
							WorkspaceUtils.AddLocalPackages(t.PackageJsonPath, workspacePath);
						WorkspaceUtils.OpenWorkspace(t.GetProjectDirectory(), true, useDefaultEditor, "Readme.md");
					}
					
					var width = Screen.width / EditorGUIUtility.pixelsPerPoint;
					if (width > 340 && GUILayout.Button(new GUIContent("Build Window", "Click to open the Unity Build Window with the Needle Engine build target selected")))
					{
						BuildWindowAccess.ShowBuildWindowWithNeedleEngineSelected();
					}
					if (GUILayout.Button("Settings"))
						Actions.OpenNeedleExporterProjectSettings();
				}
				EditorGUILayout.Space();
			}
			
			if (isValidDirectory && Actions.IsInstalling())
			{
				EditorGUILayout.HelpBox(
					"Installation in progress. Please wait for it to finish. See progress indicator in bottom right corner in Unity.",
					MessageType.Info, true);
				ShowVersionInfo(projectDirectory);
				return;
			}
			
			// if (isValidDirectory && !isInstalled && !Actions.ProjectSetupIsRunning)
			// {
			// 	EditorGUILayout.HelpBox(
			// 		"Project requires installation. Please click the button below to install the needle runtime npm package.",
			// 		MessageType.Warning, true);
			// 	ExporterProjectSettingsProvider.DrawFixSettingsPathsGUI();
			// 	ShowVersionInfo(projectDirectory);
			// 	return;
			// }

			var moduleDirectory = t.GetProjectDirectory() + "/node_modules/@needle-tools/engine";
			isInstalled = Directory.Exists(moduleDirectory);
			// if (isValidDirectory && !isInstalled && projectExists && hasNpmInstalled)
			// {
				// var logMessage = "Project requires installation. Please run install (button below).";
				// EditorGUILayout.HelpBox(logMessage, MessageType.Warning, true);
				// // Perform clean install when alt is NOT pressed (by default) in cases where installation failed half way through by npm and node_modules is incomplete
				// var cleanInstall = Event.current.modifiers != EventModifiers.Alt;
				// if (GUILayout.Button(new GUIContent("Install Project", "Hold ALT to toggle clean install" + $"\n• Needle Engine Exists?: {Directory.Exists(moduleDirectory)} at {moduleDirectory}"), GUILayout.Height(38)))
				// {
				// 	RunInstall(cleanInstall, true);
				// 	GUIUtility.ExitGUI();
				// }
				// ShowVersionInfo(projectDirectory);
			// }
			
			if (!string.IsNullOrWhiteSpace(t.DirectoryName))
			{
				if (!projectExists)
				{
					if (isRepository)
					{
						DrawGitCloneGUI(t);
					}
					else
					{
						DrawProjectTemplateGUI(t);
					}
				}
				else if(Directory.Exists(projectDirectory))
				{
					EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
					using (new GUILayout.HorizontalScope())
					{
						using (new EditorGUI.DisabledScope(hasNpmInstalled == false || Actions.IsInstalling()))
						{
							var alt = Event.current.modifiers == EventModifiers.Alt;
							if (alt)
							{
								if (GUILayout.Button(new GUIContent("Clean Install",
									    "Removes the node_modules folder and then re-installs all npm packages in your project directory.")))
								{
									RunInstall(true);
									GUIUtility.ExitGUI();
								}
							}
							else
							{
								if (GUILayout.Button(new GUIContent("Install",
									    "Installs npm packages in your project directory (Hold ALT to perform a clean installation)")))
								{
									RunInstall(false);
									GUIUtility.ExitGUI();
								}
							}
						}

						DrawStartServerButtons(fullDirectoryPath, serverIsRunning);
					}
					using (new GUILayout.HorizontalScope())
					{
						using (new EditorGUI.DisabledScope(hasNpmInstalled == false))
						{
							var fullExport = Event.current.modifiers == EventModifiers.Alt;
							var text = !serverIsRunning ? "Play ▶" : "Play ↺";
							if (fullExport) text = "Full Export & Play ▶";
						
							if (GUILayout.Button(
								    new GUIContent(text,
									    "Build for local development. Requires local server to run.\n\nTip: When Override Playmode is enabled in settings you can also just click the Play button to run your project.\n\nHold ALT to perform a full export (it will clean caches before running)"),
								    GUILayout.Height(30)))
							{
								if (fullExport)
								{
									Actions.ClearCaches(t);
								}
								Actions.Play();
								GUIUtility.ExitGUI();
								return;
							}
						}
					}
				}

				LateInspectorGUI?.Invoke(t);

				ProjectValidation.DrawMinimalInfoIfAnythingNeedsInstallation(true);

				ShowVersionInfo(projectDirectory);
				ShowUpdateInfo();
				
				ValidateProject();
				if (problems.Count > 0)
				{
					var hasFixesForProblems = false;
					foreach (var kvp in problems)
					{
						if (kvp.Value?.Count > 0)
						{
							var text = kvp.Value.Print();
							EditorGUILayout.HelpBox(kvp.Key + ": " + text, MessageType.Warning);
							if (!hasFixesForProblems)
								hasFixesForProblems = kvp.Value.Any(p => p.Fix != null);
						}
					}

					if (hasFixesForProblems && GUILayout.Button("Fix problems"))
					{
						ProblemSolver.TryFixProblemsButIDontCareIfItWorks(t.GetProjectDirectory(), problems.Values.SelectMany(x => x).ToList());
						lastProblemSearchTime = DateTime.MinValue;
					}
				}
			}
		}

		private static DateTime _clickedDownloadToktxButton;

		internal static void DrawStartServerButtons(string fullDirectoryPath, bool serverIsRunning = false)
		{
			var assetsDirectory = fullDirectoryPath + "/assets";
			if(NeedleProjectConfig.TryGetAssetsDirectory(out var dir))
				assetsDirectory = dir;
			var canStartServer = Directory.Exists(assetsDirectory) && Directory.Exists(fullDirectoryPath + "/node_modules");
			using (new EditorGUI.DisabledScope(!canStartServer))
			{
				var startUsingIp = Event.current.modifiers != EventModifiers.Alt;
				if (serverIsRunning)
				{
					var label = startUsingIp ? "Open Server ↗" : "Restart Server ↩";
					if (GUILayout.Button(new GUIContent(label, "Open server in a browser.\nHold ALT to restart")))
					{
						if (startUsingIp)
						{
							if(!string.IsNullOrEmpty(localServerUrl))
								ActionsBrowser.OpenBrowser(localServerUrl);
						}
						else
						{
							Actions.StopLocalServer(true);
							Actions.StartLocalServer();
						}
					}
				}
				else
				{
					var tooltip = canStartServer ? "Starts the local development server. This is done automatically when you click Play.\nHold ALT to open using localhost instead of IP address"
						: "Cannot start server - you need to run Install first or click Play";
					var label = startUsingIp ? "Start Server ↗" : "Start Server (localhost) ↗";
					if (GUILayout.Button(new GUIContent(label, tooltip)))
					{
						MenuItems.StartDevelopmentServer(new DefaultProjectInfo(fullDirectoryPath), startUsingIp);
					}
				}
			}
		}

		private DateTime lastProblemSearchTime = DateTime.MinValue;

		private void ValidateProject()
		{
			var now = DateTime.Now;
			if ((now - lastProblemSearchTime).TotalSeconds < 5f) return;
			lastProblemSearchTime = now;
			problems.Clear();
			var exp = target as ExportInfo;
			if (!exp || !exp.Exists()) return;
			var packageJson = exp.PackageJsonPath;
			Task.Run(() =>
			{
				if (ProjectValidator.FindProblems(packageJson, out var res))
				{
					foreach (var prob in res)
					{
						if (!problems.ContainsKey(prob.Id)) problems.Add(prob.Id, new List<IProblem>());
						problems[prob.Id].Add(prob);
					}
				}
			});
		}

		private async void RunInstall(bool clean, bool silent = false)
		{
			await Actions.InstallPackage(clean, false, silent, true);
		}

		private static async void GenerateProject(string path)
		{
			if (selectedTemplateIndex < ProjectGenerator.Templates.Count)
			{
				var template = ProjectGenerator.Templates[selectedTemplateIndex];
				await ProjectGenerator.CreateFromTemplate(path, template);
			}
			else Debug.LogWarning("Please select a project template from the dropdown");
		}

		public static bool TryGetVsCodeWorkspacePath(string directory, out string path)
		{
			return WorkspaceUtils.TryGetWorkspace(directory, out path);
		}

		private static void DrawSelectExistingProjectGUI(ExportInfo t, SerializedObject obj)
		{
			if (projectOptions.Length <= 0) return;
			EditorGUILayout.LabelField("Select an existing project", EditorStyles.boldLabel);
			using (var scope = new EditorGUI.ChangeCheckScope())
			{
				var selection = EditorGUILayout.Popup("Projects", 0, projectDisplayOptions);
				if (scope.changed && selection >= 0 && selection <= projectOptions.Length)
				{
					var selected = projectOptions[selection];
					var prop = obj.FindProperty(nameof(t.DirectoryName));
					if (prop == null)
					{
						t.DirectoryName = selected;
					}
					else
					{
						prop.stringValue = selected;
						obj.ApplyModifiedProperties();
					}
				}
			}
			GUILayout.Space(10);
		}

		private static GUIStyle _wrappedLinkLabel;

		private static string _customTemplateUrl
		{
			get => SessionState.GetString("needle.customTemplateUrl", "");
			set => SessionState.SetString("needle.customTemplateUrl", value);
		}
		
		private static void DrawProjectTemplateGUI(ExportInfo t)
		{
			_wrappedLinkLabel ??= new GUIStyle(EditorStyles.linkLabel);
			_wrappedLinkLabel.wordWrap = true;
			
			using (new EditorGUI.DisabledScope(!t.IsValidDirectory() || Tools.IsCloningRepository))
			{
				GUILayout.Space(6);
				var hasPath = !string.IsNullOrWhiteSpace(t.DirectoryName);
				EditorGUILayout.LabelField("Generate a new web project", EditorStyles.boldLabel);
				var dir = hasPath ? Path.GetFullPath(t.DirectoryName) : "";
				if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length > 0)
				{
					EditorGUILayout.HelpBox(
						"Directory is not empty but also does not contain a package.json! Please select a new or empty directory path to generate a new project or a directory containing a package.json.",
						MessageType.Error);
				}
				else if (Directory.Exists(dir) && invalidDirectories.Contains(new DirectoryInfo(dir).Name))
				{
					EditorGUILayout.HelpBox("Directory is not allowed: " + new DirectoryInfo(dir).Name, MessageType.Error);
				}
				else
				{
					selectedTemplateIndex = EditorGUILayout.Popup("Template", selectedTemplateIndex, templateOptions);
					var fullDirectoryPath = t.GetProjectDirectory();
					var path = Path.GetFullPath(fullDirectoryPath);
					if (selectedTemplateIndex == customTemplateIndex)
					{
						_customTemplateUrl = EditorGUILayout.TextField(new GUIContent("Repository URL", "Enter a github.com repository url to clone your web project"), _customTemplateUrl);
						GUILayout.Space(6);
						var isValidUrl = _customTemplateUrl != null && (_customTemplateUrl.Contains("github") ||
						                                                _customTemplateUrl.Contains("gitlab") ||
						                                                _customTemplateUrl.EndsWith(".git"));
						
						EditorGUILayout.HelpBox("Please enter a git repository URL for cloning the project", MessageType.None);
						using(new EditorGUI.DisabledScope(!isValidUrl && !Tools.IsCloningRepository))
						{
							if (GUILayout.Button(
								    new GUIContent("Generate Project",
									    "Clicking this button will generate a new project in " + path),
								    GUILayout.Height(30)))
							{
								ProjectGenerator.CreateFromRemoteUrl(t.GetProjectDirectory(), _customTemplateUrl, new ProjectGenerationOptions()
								{
									StartAfterGeneration = true
								});
							}
						}
					}
					else
					{
						var validSelection = selectedTemplateIndex >= 0 && selectedTemplateIndex < ProjectGenerator.Templates.Count;
						if (!validSelection) EditorGUILayout.HelpBox("Selected template does not exist", MessageType.Warning);
						using (new EditorGUI.DisabledScope(!validSelection))
						{
							if (validSelection && ProjectGenerator.Templates.Count > selectedTemplateIndex)
							{
								var template = ProjectGenerator.Templates[selectedTemplateIndex];
								if (template != null)
								{
									using (ColorScope.LowContrast())
									{
										if (!string.IsNullOrWhiteSpace(template.Description))
										{
											EditorGUILayout.LabelField(template.Description, multilineLabel);
										}
										if (template.IsRemoteTemplate())
										{
											using (new GUILayout.HorizontalScope())
											{
												EditorGUILayout.PrefixLabel("Source URL");
												if (GUILayout.Button(template.RemoteUrl + " ↗", _wrappedLinkLabel))
													Application.OpenURL(template.RemoteUrl);
											}
										}
										if (template.Links.Any())
										{
											EditorGUILayout.BeginHorizontal();
											EditorGUILayout.PrefixLabel("Learn More");
											EditorGUILayout.BeginVertical();
											foreach (var link in template.Links)
											{
												if (string.IsNullOrWhiteSpace(link)) continue;
												var l = link;
												if (!l.StartsWith("http")) l = "https://" + l;
												if (GUILayout.Button(link + " ↗", EditorStyles.linkLabel))
													Application.OpenURL(l);
											}
											EditorGUILayout.EndVertical();
											EditorGUILayout.EndHorizontal();
										}
									}
								}
							}
							GUILayout.Space(6);
							using (new GUILayout.HorizontalScope())
							{
								if (GUILayout.Button(
									    new GUIContent("Generate Project",
										    "Clicking this button will generate a new project in " + path),
									    GUILayout.Height(30)))
								{
									var template = ProjectGenerator.Templates[selectedTemplateIndex];
									if (template.IsRemoteTemplate() == false && !template.HasPackageJson())
									{
										if (!EditorUtility.DisplayDialog("Template check",
											    $"Template is missing package.json - are you sure you want to use {Path.GetFullPath(template.GetPath())} as a web template?",
											    "Yes copy content", "Cancel"))
										{
											Debug.Log("Cancelled generating project from " + template.GetFullPath());
											return;
										}
									}
									GenerateProject(t.DirectoryName);
									EditorGUILayout.Space(5);
								}
								GUILayout.Space(PAD_RIGHT);
							}
							// using (new GUILayout.HorizontalScope())
							// {
							// 	EditorGUILayout.HelpBox("Target Full Path: " + Path.GetFullPath(fullDirectoryPath), MessageType.None);
							// 	GUILayout.Space(PAD_RIGHT);
							// }
						}
					}
					
					GUILayout.Space(10);
				}
			}
		}

		private static void DrawGitCloneGUI(ExportInfo t)
		{
			if (GitActions.IsCloneable(t.RemoteUrl))
			{
				using (new EditorGUI.DisabledScope(Tools.IsCloningRepository))
				{
					if (GUILayout.Button("Create Project from Repository", GUILayout.Height(32)))
					{
						GitActions.CloneProject(t).ContinueWith(r =>
						{
							if(r.Result) Actions.Play();
						}, TaskScheduler.FromCurrentSynchronizationContext());
					}
					if (Tools.IsCloningRepository)
					{
						EditorGUILayout.HelpBox("Cloning project... please wait a moment", MessageType.None);
					}
				}
			}
			else
			{
				EditorGUILayout.HelpBox("Unknown URL", MessageType.Error);
			}
		}

		private static void ShowVersionInfo(string projectDirectory)
		{
			using (ColorScope.LowContrast())
			{
				GUILayout.Space(3);
				var exporterVersion = ProjectInfo.GetCurrentNeedleExporterPackageVersion(out var exporterPackageJsonPath);
				if (exporterVersion != null)
				{
					var runtimePackageJsonPath = "";
					string runtimeVersion = null;
					var isLocalRuntime = false;
					if (PackageUtils.TryReadDependencies(projectDirectory + "/package.json", out var deps))
					{
						if (deps.TryGetValue(Constants.RuntimeNpmPackageName, out var version))
						{
							runtimeVersion = version;
							if (PackageUtils.TryGetPath(projectDirectory, version, out var path))
							{
								isLocalRuntime = true;
								runtimePackageJsonPath = Path.GetFullPath(path + "/package.json");
								PackageUtils.TryGetVersion(runtimePackageJsonPath, out runtimeVersion);
							}
							else
							{
								var @index = runtimeVersion.LastIndexOf("@", StringComparison.Ordinal);
								if (@index >= 0) runtimeVersion = runtimeVersion.Substring(@index + 1);
							}
							
							// Check the actually installed version:
							var installedEnginePath = projectDirectory + "/node_modules/" +
							                          Constants.RuntimeNpmPackageName + "/package.json";
							if (PackageUtils.TryGetVersion(installedEnginePath, out var installedVersion))
							{
								runtimeVersion = installedVersion;
								runtimePackageJsonPath = Path.GetFullPath(installedEnginePath);
							}
						}
					}
					
					var runtimeLocalPostfix = isLocalRuntime ? " (local)" : "";
					
					var exporterFullPath = Path.GetFullPath(exporterPackageJsonPath);
					var exporterLocalPostfix = exporterFullPath.Contains("PackageCache") ? "" : " (local)";
					const string tooltipExporter = "Click to open changelog on github or context click for more options";
					var str = $"Version {exporterVersion}{exporterLocalPostfix}";
					if(runtimeVersion != null) str += $" • NE {runtimeVersion}{runtimeLocalPostfix}";
					if(!string.IsNullOrEmpty(nodejsVersionInstalled)) str += " • Node " + nodejsVersionInstalled;
					if (LicenseCheck.HasLicense) str += " • Active License";
					EditorGUILayout.LabelField(new GUIContent(str, tooltipExporter));
					var rect = GUILayoutUtility.GetLastRect();
					EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
					if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
						Actions.OpenChangelog(false);
					PathUtils.AddContextMenu(m =>
					{
						var hasExporter = File.Exists(exporterPackageJsonPath);
						var hasRuntime = File.Exists(runtimePackageJsonPath);
						if (hasExporter)
						{
							m.AddItem(new GUIContent("Exporter/Open directory"), false, () => EditorUtility.RevealInFinder(exporterFullPath));
							m.AddItem(new GUIContent("Exporter/Open package.json"), false, () => EditorUtility.OpenWithDefaultApp(exporterFullPath));
						}
						if (hasRuntime)
						{
							m.AddItem(new GUIContent("Runtime/Open directory"), false, () => EditorUtility.RevealInFinder(runtimePackageJsonPath));
							m.AddItem(new GUIContent("Runtime/Open package.json"), false, () => EditorUtility.OpenWithDefaultApp(runtimePackageJsonPath));						
							m.AddItem(new GUIContent("Runtime/Show code on npm"), false, () => Application.OpenURL(NpmUtils.GetCodeUrl(Constants.RuntimeNpmPackageName, runtimeVersion)));		
							m.AddItem(new GUIContent("View on NPM"), false, () => Application.OpenURL(NpmUtils.GetPackageUrl(Constants.RuntimeNpmPackageName)));
						}
						if (hasExporter && hasRuntime)
						{
							m.AddItem(new GUIContent("Open package.json and changelogs"), false, () =>
							{
								var fp1 = Path.GetFullPath(runtimePackageJsonPath);
								var fp2 = Path.GetFullPath(exporterPackageJsonPath);
								var fp3 = Path.GetFullPath(Path.GetDirectoryName(runtimePackageJsonPath) + "/Changelog.md");
								var fp4 = Path.GetFullPath(Path.GetDirectoryName(exporterPackageJsonPath) + "/Changelog.md");
								EditorUtility.OpenWithDefaultApp(fp1);
								EditorUtility.OpenWithDefaultApp(fp2);
								EditorUtility.OpenWithDefaultApp(fp3);
								EditorUtility.OpenWithDefaultApp(fp4);
							});
							m.AddItem(new GUIContent("Open directories"), false, () =>
							{
								var fp1 = Path.GetFullPath(runtimePackageJsonPath);
								var fp2 = Path.GetFullPath(exporterPackageJsonPath);
								EditorUtility.RevealInFinder(fp1);
								EditorUtility.RevealInFinder(fp2);
							});
						}
					});
				}
			}
		}

		private static AddRequest _addRequest;

		private static void ShowUpdateInfo()
		{
			VersionsUtil.HasExporterPackageUpdate(out var latest, out var latestMinor);
			var version = latest;
			
			// Updating to latest minor release should be the priority
			if (latestMinor != null)
			{
				version = latestMinor;
			}
				
			if (version != null)
			{
				var name = Constants.UnityPackageName;
				var displayName = "Needle Engine";
				var updateVersion = version;
				var message = "Update available for package " + displayName + " to version " + updateVersion;
				GUILayout.Space(5);
				using (new EditorGUILayout.HorizontalScope())
				{
					var installUpdateImmediately = Event.current.modifiers == EventModifiers.Alt;
					EditorGUILayout.HelpBox(new GUIContent(message, updateVersion), true);
					if (GUILayout.Button(new GUIContent(installUpdateImmediately ? "Install" : "Open",
						    "Clicking this button will open the PackageManager. Hold ALT while clicking to install without opening Package Manager.")))
					{
						if (_addRequest != null && !_addRequest.IsCompleted)
						{
							Debug.LogWarning("Add request is still running: " + _addRequest.Status);
							return;
						}

						VersionsUtil.ClearCache();

						if (!installUpdateImmediately)
						{
							Debug.Log("Open " + name + " in PackageManager");
							// packagemanager also selects the package manifest which we dont want here
							var currentSelection = Selection.activeObject;
							UnityEditor.PackageManager.UI.Window.Open(name);
							Selection.activeObject = currentSelection;
						}
						else
						{
							var text = "Do you want to update " + name + " to version " + updateVersion + "?";
							var shouldOpenChangelog = false;
							var shouldOpenGithubRelease = false;
							
							if (PackageUtils.TryGetVersion(Constants.ExporterPackagePath + "/package.json",
								    out var currentVersion))
							{
								if (PackageUtils.IsMajorVersionChange(currentVersion, updateVersion))
								{
									shouldOpenChangelog = true;
									text +=
										"\n\nThis is a major version change. Please check the changelog for breaking changes before updating!";
								}
								else if (PackageUtils.IsChangeToPreReleaseVersion(currentVersion, updateVersion))
								{
									shouldOpenGithubRelease = true;
									text +=
										"\n\nYou are updating to a pre-release version - please check the changelog before updating!";
								}
							}

							if (EditorUtility.DisplayDialog("Update " + displayName, text,
								    "Yes, update package",
								    "No, do not update"))
							{
								HandleOpeningOfChangelogOrVersion();
								Debug.Log("Will update package " + name + " to version " + updateVersion);
								_addRequest = Client.Add(name + "@" + updateVersion);
								WaitForRequestCompleted();

								async void WaitForRequestCompleted()
								{
									while (!_addRequest.IsCompleted) await Task.Delay(100);
									if (_addRequest.Status == StatusCode.Failure) Debug.LogError(_addRequest.Error?.message);
									else Debug.Log("Updating " + name + " to " + updateVersion + " completed");
									VersionsUtil.ClearCache();
								}

								GUIUtility.ExitGUI();
							}
							else 
								HandleOpeningOfChangelogOrVersion();

							void HandleOpeningOfChangelogOrVersion()
							{
								EditorApplication.delayCall += OnDelayCall;
								async void OnDelayCall()
								{
									await Task.Delay(1000);
									if(shouldOpenChangelog)
										Actions.OpenChangelog(false);;
									if(shouldOpenGithubRelease)
										Actions.OpenReleaseOnGithub(updateVersion);
								}
							}
						}
					}
				}
			}
		}
	}
}