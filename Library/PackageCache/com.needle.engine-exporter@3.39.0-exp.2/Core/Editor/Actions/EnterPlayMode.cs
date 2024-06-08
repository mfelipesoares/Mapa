using System;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Editors;
using Needle.Engine.Projects;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Needle.Engine
{
	internal static class EnterPlayMode
	{
		internal static event Func<bool> OverridePlayModeNotInExportScene;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			isWaitingToRun = false;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		private static bool isWaitingToRun = false;

		private static void OnPlayModeChanged(PlayModeStateChange obj)
		{
			if (ExporterProjectSettings.instance.overrideEnterPlaymode)
			{
				if (obj == PlayModeStateChange.ExitingEditMode)
				{
					if (ExporterProjectSettings.instance.overrideEnterPlaymode)
					{
						Play();
					}
				}
			}
		}

		internal static async void Play()
		{
			var info = ExportInfo.Get();
			var exitPlayMode = ExporterProjectSettings.instance.overrideEnterPlaymode;
			var hasNodejsInstalled = await InternalActions.HasSupportedNodeJsInstalled();

			if (exitPlayMode && hasNodejsInstalled && ExporterUserSettings.FirstTimeRunningNeedleEngine)
			{
				var currentScene = SceneManager.GetActiveScene();
				if (info == null && currentScene.IsValid() && !string.IsNullOrEmpty(currentScene.path))
				{
					if (EditorUtility.DisplayDialog("Export with 🌵 Needle Engine", "Hello, thank you for having installed 🌵 Needle Engine.\n\n" +
						    "Since this is your first time using it and your scene does not have an ExportInfo component we'd like to ask if you want to use the Needle Engine web export now?", "Yes, export my scene to the web", "No, start the normal Unity Play Mode"))
					{
						Actions.SetupSceneForNeedleEngineExport();
						info = ExportInfo.Get();
					}
				}
			}

			if (info != null)
			{
				if (exitPlayMode)
					EditorApplication.ExitPlaymode();
				if (!hasNodejsInstalled)
				{
					Debug.LogWarning("Nodejs is not installed: opening Needle Engine Validation window");
					await Task.Delay(500);
					ProjectValidationWindow.Open();
					return;
				}
				Play(info);
			}
			else
			{
				if (OverridePlayModeNotInExportScene?.Invoke() ?? false)
				{
					if (exitPlayMode)
						EditorApplication.ExitPlaymode();
				}
			}
		}

		private static async void Play(IProjectInfo info)
		{
			if (info != null)
			{
				if (EulaWindow.RequiresEulaAcceptance)
				{
					Selection.activeObject = ExportInfo.Get();
					Debug.Log("You need to accept the Needle Engine EULA first before you can use Needle Engine.", Selection.activeObject);
					await Task.Delay(300);
					EulaWindow.Open(() => Play(info));
					return;
				}
				
				var wasInAnimationPreviewMode = AnimationWindowUtil.IsPreviewing();
				if (wasInAnimationPreviewMode) AnimationWindowUtil.StopPreview();

				if (isWaitingToRun)
				{
					if (Actions.IsInstalling())
						Debug.LogWarning(
							"Project is installing... please wait a moment, it will run automatically once installed");
					else
						Debug.LogWarning("Project is building...");
					return;
				}
				isWaitingToRun = true;
				try
				{
					if (info.Exists() && !info.IsInstalled())
					{
						await Actions.RunProjectSetup(true);
					}

					if (!info.Exists())
					{
						var projectDir = info.ProjectDirectory;
						var didCloneProject = false;
						if (info is ExportInfo exp && GitActions.IsCloneable(exp.RemoteUrl))
						{
							if (!EditorUtils.AreTestsRunning() && !EditorUtility.DisplayDialog("Needle Engine Project Setup",
								    $"The project will be downloaded from {exp.DirectoryName}\nPlease select a target directory to download into.", "Select Directory",
								    "Cancel Download"))
							{
								Debug.Log("Canceled cloning project");
								return;
							}
							var cloneRes = await GitActions.CloneProject(exp);
							projectDir = info.ProjectDirectory;
							didCloneProject = true;
							if (!cloneRes)
							{
								Debug.LogError("Failed to clone project... "  + exp.DirectoryName);
								return;
							}
						}

						// Check if the project should be created from a template
						if (!didCloneProject)
						{
							var template =
								ProjectGenerator.Templates.FirstOrDefault(v =>
									v.name.ToLowerInvariant().Contains("vite"));
							
							// if a user has selected a template in the editor
							var selectedTemplateIndex = ExportInfoEditor.selectedTemplateIndex;
							if(selectedTemplateIndex >= 0 && selectedTemplateIndex < ProjectGenerator.Templates.Count)
								template = ProjectGenerator.Templates[selectedTemplateIndex];
							
							// var isTemporaryProject = (info as ExportInfo)?.IsTempProject() ?? false;
							// (isTemporaryProject ||
							//  EditorUtility.DisplayDialog("Create a project",
							// 	 $"A web project does not yet exist, do you want to create a project using the \"{template.name}\" template now at {Path.GetFullPath(projectDir)}?",
							// 	 $"Yes, create a new web project for me"))
							if (!string.IsNullOrWhiteSpace(projectDir) && template != null)
							{
								Debug.Log("Creating web project at " + projectDir);
								await ProjectGenerator.CreateFromTemplate(projectDir, template);
							}
							else
							{
								var exportComp = ExportInfo.Get();
								const string msg0 =
									"<b>Can't run: missing project</b>, please generate a new project or select an existing project.";
								const string msg1 = "Select the " +
								                    nameof(ExportInfo) +
								                    " component, choose a template and click <i>Generate Project</i>";
								var warning = msg0;
								if (exportComp) warning = $"{msg0} {msg1}";
								Debug.LogWarning(warning, exportComp);
								if (exportComp)
									EditorGUIUtility.PingObject(exportComp);
								return;
							}
						}
					}

					var res = await Builder.Build(false, BuildContext.LocalDevelopment);
					if (res)
					{
						await Task.Delay(1000);
						MenuItems.StartDevelopmentServer(info, true);
					}
				}
				finally
				{
					isWaitingToRun = false;
					if (wasInAnimationPreviewMode) AnimationWindowUtil.StartPreview();
				}
			}
		}
	}
}