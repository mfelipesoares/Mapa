using System;
using System.Diagnostics;
using System.IO;
using Needle.Engine.Editors;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using Semver;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Needle.Engine.Problems
{
	internal class ProjectValidation
	{
		internal static readonly ProjectValidation Instance = new ProjectValidation();

		[InitializeOnLoadMethod]
		private static void Init()
		{
			Instance.DetectIfSamplesPackageIsValid();
			AssetDatabase.importPackageCompleted += i =>
			{
				if(i == Constants.SamplesPackageName)
					Instance.DetectIfSamplesPackageIsValid();
			};
			CompilationPipeline.compilationFinished += i =>
			{
				Instance.DetectIfSamplesPackageIsValid();
			};
		}
		
		private ProjectValidation()
		{
			hasCorrectToktxVersionInstalled = Actions.HasMinimumToktxVersionInstalled(out _);
		}

		private void DetectIfSamplesPackageIsValid()
		{
			hasValidSamplesPackage = true;
			currentSamplesPackageVersion = null;
			
			// have we even samples installed?
			var current = ProjectInfo.GetCurrentNeedleEngineSamplesVersion();
			if (string.IsNullOrEmpty(current)) 
				return; // no samples package installed... that's ok of course

			currentSamplesPackageVersion = current;
			
			// now lets see if we have a recommended version
			if (NpmUnityEditorVersions.TryGetRecommendedVersion(Constants.SamplesPackageName, out var recommended))
			{
				var recommendedFullString = recommended;
				var allowMinorUpdate = recommended.StartsWith("^");
				// for the semver check we have to remove the ^ from the version
				if(allowMinorUpdate) recommended = recommended.Substring(1);
				if (SemVersion.TryParse(recommended, SemVersionStyles.Any, out var recommendedSemVer))
				{
					recommendedSamplesPackageVersion = recommended;
					var currentSemVer = SemVersion.Parse(current, SemVersionStyles.Any);
					if (recommendedSemVer > currentSemVer)
					{
						var updateMsg = "Needle Engine Samples package is outdated. Please update to version " +
						                recommended;
						if (allowMinorUpdate) updateMsg += " or newer";
						Debug.LogWarning(updateMsg);
						hasValidSamplesPackage = false;
					}
					else if (currentSemVer > recommendedSemVer)
					{
						if (!allowMinorUpdate && (currentSemVer.Minor > recommendedSemVer.Minor || currentSemVer.Major > recommendedSemVer.Major))
						{
							Debug.LogWarning($"Needle Engine Samples package {currentSemVer} is newer than recommended version {recommendedSemVer}. This is not supported!");
							hasValidSamplesPackage = false; 
						}
					}
				}
			}
		}

		internal void Refresh()
		{
			DetectIfSamplesPackageIsValid();
		}

		private static bool hasCorrectToktxVersionInstalled = true;
		private static DateTime lastToktxCheckTime;

		private static bool hasCorrectNodejsVersionInstalled = true;
		private static DateTime lastNodejsCheckTime;
		private static string nodejsVersionInstalled, npmVersionInstalled;
		private static DateTime updateNpmTime;
		
		private static bool hasValidSamplesPackage;
		private static string recommendedSamplesPackageVersion, currentSamplesPackageVersion;

		private static GUIStyle _bigButtonStyle, _rightLabelStyle;
		private static GUILayoutOption _rightButtonWidth;

		internal static void DrawMinimalInfoIfAnythingNeedsInstallation(bool collapseMessages = false)
		{
			var hasNodeJsInstalled = CheckIfSupportedNodejsInstalled();
			var hasToktxInstalled = CheckIfToktxVersionIsInstalled();
			var hasWrongColorspaceSettings = PlayerSettings.colorSpace != ColorSpace.Linear;
			var wrongLightmapEncoding = PlayerSettingsAccess.IsLightmapEncodingSetToNormalQuality() == false;
			var isUsingLightmaps = LightmapSettings.lightmaps.Length > 0;
			var hasAnyProblem = !hasNodeJsInstalled || !hasToktxInstalled || hasWrongColorspaceSettings ||
			                    EulaWindow.RequiresEulaAcceptance || !hasValidSamplesPackage;

			if(isUsingLightmaps)
				hasAnyProblem |= wrongLightmapEncoding;

			if (npmVersionInstalled != null &&
			    SemVersion.TryParse(npmVersionInstalled, SemVersionStyles.Any, out var npmversion))
			{
				if (npmversion.Major < 10) 
					hasAnyProblem = true;
			}
			
			if (hasAnyProblem)
			{
				GUILayout.Space(12);
				EditorGUILayout.LabelField("Project Validation", EditorStyles.boldLabel);
				if (!collapseMessages)
				{
					if (EulaWindow.RequiresEulaAcceptance)
					{
						EditorGUILayout.HelpBox(
							"Needle Engine EULA is not accepted",
							MessageType.Warning);
					}
					if (!hasNodeJsInstalled)
					{
						EditorGUILayout.HelpBox(
							("Nodejs is not installed. Please install Nodejs via to start using Needle Engine."),
							MessageType.Warning);
					}
					if (!hasToktxInstalled)
					{
						EditorGUILayout.HelpBox(
							("Toktx is not installed. Install Toktx for fast and optimized production builds."),
							MessageType.Warning);
					}
					if (hasWrongColorspaceSettings)
					{
						EditorGUILayout.HelpBox(("Please set your project Colorspace to Linear"), MessageType.Warning);
					}
					if (wrongLightmapEncoding)
					{
						EditorGUILayout.HelpBox(
							("Lightmap Encoding is not set to Normal. Please set it to Normal for best results."),
							MessageType.Warning);
					}
					if (hasValidSamplesPackage == false)
					{
						EditorGUILayout.HelpBox(
							$"Your Needle Engine Samples package is outdated. Please update to version {recommendedSamplesPackageVersion}",
							MessageType.Warning);
					}
				}
				else
				{
					EditorGUILayout.HelpBox(
						"We detected some issues in your project or machine setup. Please open the Validation Window for details",
						MessageType.Warning);
				}
				if (GUILayout.Button("Open Validation Window"))
				{
					ProjectValidationWindow.Open();
				}
				GUILayout.Space(5);
			}
			else 
			{
				if (!collapseMessages)
				{
					GUILayout.Space(8);
					EditorGUILayout.LabelField("Project Validation", EditorStyles.boldLabel);
					EditorGUILayout.HelpBox("Your project and machine is setup correctly to use Needle Engine. Everything looks good", MessageType.Info);
					if (GUILayout.Button("Open Validation Window"))
					{
						ProjectValidationWindow.Open();
					}
					GUILayout.Space(5);
				}
			}
		}

		private const int rightWidth = 150;
		private static Vector2 scroll;

		internal static void DrawProjectValidationUI()
		{
			using var scroll = new EditorGUILayout.ScrollViewScope(ProjectValidation.scroll);
			ProjectValidation.scroll.y = scroll.scrollPosition.y;

			GUILayout.Space(10);
			// EditorGUILayout.LabelField("Project Validation", EditorStyles.boldLabel,
			// 	GUILayout.Width(EditorGUIUtility.labelWidth));

			if (_rightButtonWidth == null)
			{
				_rightButtonWidth = GUILayout.Width(150);
			}
			if (_bigButtonStyle == null)
			{
				_bigButtonStyle = new GUIStyle("button");
				_bigButtonStyle.wordWrap = true;
				_bigButtonStyle.fixedWidth = rightWidth;
				_bigButtonStyle.fixedHeight = 38;
			}
			if (_rightLabelStyle == null)
			{
				_rightLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
				_rightLabelStyle.fixedWidth = rightWidth;
			}


			var hasNodeJsInstalled = CheckIfSupportedNodejsInstalled();
			var hasToktxInstalled = CheckIfToktxVersionIsInstalled();
			var hasWrongProjectSettings = PlayerSettings.colorSpace != ColorSpace.Linear ||
			                              !PlayerSettingsAccess.IsLightmapEncodingSetToNormalQuality();

			var hasAnyProblem = !hasNodeJsInstalled || !hasToktxInstalled || hasWrongProjectSettings;

			if (hasAnyProblem)
			{
				using (ColorScope.LowContrast())
				{
					var msg =
						"The Needle Engine project validation window helps you to correctly setup your Unity project and machine. Use this window to check if everything is in order or if there are optimizations available or tools missing that we recommend you to use";
					EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedLabel);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("Your machine and project is setup and ready to run Needle Engine. Everything looks good!",
					MessageType.None);
			}

			GUI.enabled = true;
			DrawBasicSetup();
			DrawUnityProjectSettings();
			DrawSceneInfo();
			DrawOptimizationGUI();
		}

		private static void DrawSceneInfo()
		{
			GUILayout.Space(5);
			GUILayout.Label("Scene Settings", EditorStyles.largeLabel);

			var scene = SceneManager.GetActiveScene();
			if (scene.IsValid() && scene.path != null)
			{
				EditorGUILayoutExtensions.HelpBoxChecked("Scene is saved");
			}
			else
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox("Scene is unsaved: Please save your Unity scene first", MessageType.Warning);
				}
			}

			var exportInfo = ExportInfo.Get();
			if (!exportInfo)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox("Scene contains no Needle Engine ExportInfo. This is necessary to export your project to the web", MessageType.Warning);
					if (GUILayout.Button("Create Export Component", _bigButtonStyle))
					{
						Actions.SetupSceneForNeedleEngineExport();
					}
				}
			}
			else
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayoutExtensions.HelpBoxChecked("Scene contains Needle Engine ExportInfo");
				}
			}
		}

		private static void DrawUnityProjectSettings()
		{
			var hasWrongColorspace = PlayerSettings.colorSpace != ColorSpace.Linear;
			var hasWrongLightmapSettings = PlayerSettingsAccess.IsLightmapEncodingSetToNormalQuality() == false;
			var hasAllowedInsecureHttp = WebHelper.HttpConnectionsAllowed == false;
			
			GUILayout.Space(5);
			GUILayout.Label("Unity Project", EditorStyles.largeLabel);

			if (hasWrongColorspace)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox(
						"Please set your project colorspace to \"Linear Colorspace\" in Player Settings",
						MessageType.Warning);
					if (GUILayout.Button("Fix Colorspace", _bigButtonStyle))
					{
						Debug.Log("Setting colorspace to linear");
						PlayerSettings.colorSpace = ColorSpace.Linear;
					}
				}
			}
			else
			{
				EditorGUILayoutExtensions.HelpBoxChecked("Project colorspace is set to linear");
			}

			if (hasWrongLightmapSettings)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox("Please set lightmap encoding to \"Normal Quality\" in Player Settings",
						MessageType.Warning);
					if (GUILayout.Button("Fix Lightmap Encoding", _bigButtonStyle))
					{
						Debug.Log("Setting lightmap encoding to normal quality");
						PlayerSettingsAccess.SetLightmapEncodingToNormalQuality();
					}
				}
			}
			else
			{
				EditorGUILayoutExtensions.HelpBoxChecked("Lightmap encoding is set to normal quality");
			}

			if (!hasValidSamplesPackage)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox(
						$"Please update your Needle Engine Samples package to {recommendedSamplesPackageVersion} (current: {currentSamplesPackageVersion})",
						MessageType.Warning);
					if (GUILayout.Button("Open Package Manager", _bigButtonStyle))
					{
						UnityEditor.PackageManager.UI.Window.Open(Constants.SamplesPackageName);
					}
				}
			}
			else if(!string.IsNullOrEmpty(recommendedSamplesPackageVersion))
			{
				EditorGUILayoutExtensions.HelpBoxChecked($"Needle Engine Samples package version {currentSamplesPackageVersion} is supported");
			}
			
// #if UNITY_2022_3_OR_NEWER
// 			if (!hasAllowedInsecureHttp)
// 			{
// 				using (new GUILayout.HorizontalScope())
// 				{
// 					EditorGUILayout.HelpBox("Please change allowed HTTP connections to \"Development\" or \"Always\" in Player Settings",
// 						MessageType.Warning);
// 					if (GUILayout.Button("Allow HTTP Connections", _bigButtonStyle))
// 					{
// 						Debug.Log("Setting http connections to development");
// 						PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
// 					}
// 				}
// 			}
// 			else
// 			{
// 				EditorGUILayoutExtensions.HelpBoxChecked("HTTP connections are allowed");
// 			}
// #endif
		}

		private static void DrawBasicSetup()
		{
			GUILayout.Space(5);
			GUILayout.Label("Setup", EditorStyles.largeLabel);
            
			if (EulaWindow.RequiresEulaAcceptance == false)
			{				
				EditorGUILayoutExtensions.HelpBoxChecked($"Needle Engine EULA is accepted");
			}
			else
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox("Needle Engine EULA needs to be accepted before you can start using Needle Engine.",
						MessageType.Warning);
					if (GUILayout.Button("Open EULA Window", _bigButtonStyle))
					{
						EulaWindow.Open();
					}
				}
			}

			if (!CheckIfSupportedNodejsInstalled())
			{
				using (new GUILayout.HorizontalScope())
				{
					var msg =
						"Nodejs could not be found on your machine. Please install Nodejs 18 or 20 LTS. You may need to restart your machine after installation.";
#if !UNITY_EDITOR_WIN
					msg +=
						"\nIf you have installed nodejs but it is not found (e.g. when using nvm) you can enter additional npm search paths with the path to your nodejs/bin directory. See list below.";
#endif
					if (!string.IsNullOrWhiteSpace(nodejsVersionInstalled))
						msg += $" You have currently Node {nodejsVersionInstalled} and NPM {npmVersionInstalled} installed.";
					EditorGUILayout.HelpBox(msg, MessageType.Warning);
					if (!File.Exists(ToolsHelper.NodejsDownloadLocation))
					{
						using (new EditorGUI.DisabledScope(ToolsHelper.IsDownloadingNodejs))
						{
							var text = ToolsHelper.IsDownloadingNodejs
								? "Downloading..."
								: "Download & Run\nNodejs Installer";
							if (GUILayout.Button(text, _bigButtonStyle))
							{
								ToolsHelper.DownloadAndRunNodejs();
							}
						}
					}
					else
					{
						using (new GUILayout.VerticalScope())
						{
							if (ToolsHelper.IsNodejsInstalledOnDisc())
							{
								using(ColorScope.LowContrast())
									GUILayout.Label("Please restart your machine to finished the installation!", _rightLabelStyle);
							}
							else
							{
								if (GUILayout.Button("Show Installer", _rightButtonWidth))
								{
									EditorUtility.RevealInFinder(ToolsHelper.NodejsDownloadLocation);
								}
								if (GUILayout.Button("Run Installer", _rightButtonWidth))
								{
									Debug.Log("Running nodejs installer at " + ToolsHelper.NodejsDownloadLocation);
									Process.Start(ToolsHelper.NodejsDownloadLocation);
								}
							}
						}
					}
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(nodejsVersionInstalled))
				{
					EditorGUILayoutExtensions.HelpBoxChecked("Found Nodejs");
				}
				else if (npmVersionInstalled != null &&
				         SemVersion.TryParse(npmVersionInstalled, SemVersionStyles.Any, out var semver))
				{
					if (semver.Major < 10)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							EditorGUILayout.HelpBox($"Your current NPM version {npmVersionInstalled} is outdated - please update NPM to version 10", MessageType.Warning);
							var disabled = DateTime.Now - updateNpmTime < TimeSpan.FromSeconds(10);
							using var _ = new EditorGUI.DisabledScope(disabled);
							if(GUILayout.Button(disabled ? "Updating NPM... " : "Update NPM", _bigButtonStyle))
							{
								updateNpmTime = DateTime.Now;
								Debug.Log("Updating NPM - please wait...");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
								ProcessHelper.RunCommand("npm update -g npm", Application.dataPath);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
							}
						}
					}
					else EditorGUILayoutExtensions.HelpBoxChecked($"Found Nodejs {nodejsVersionInstalled} and NPM {npmVersionInstalled}");
				}
				else EditorGUILayoutExtensions.HelpBoxChecked($"Found Nodejs {nodejsVersionInstalled} and NPM {npmVersionInstalled}");
			}

#if UNITY_EDITOR_WIN
			const bool isWindows = true;
#else
            const bool isWindows = false;
#endif
#pragma warning disable CS0162 // Unreachable code detected
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (!isWindows)
			{
				GUILayout.Space(5);
				var prop = ExporterProjectSettings.NpmSearchPathDirectoriesProperty;
				if (prop != null)
				{
					EditorGUILayout.PropertyField(prop,
						new GUIContent("Additional npm search paths",
							"These are only used on OSX and Linux and should be used to declare where your npm installation is or should be searched. For example in \"/usr/local/bin/\""));
					GUI.enabled = true;
					foreach (var path in ExporterProjectSettings.instance.npmSearchPathDirectories)
					{
						if (string.IsNullOrWhiteSpace(path)) continue;
						if (Directory.Exists(path)) continue;
						if (File.Exists(path))
						{
							EditorGUILayout.HelpBox(
								"Found invalid npm search path: must be a directory path (not a file path)",
								MessageType.Error);
						}
					}
				}
				GUILayout.Space(5);
			}
#pragma warning restore CS0162 // Unreachable code detected
			
			

			if (LicenseCheck.LicenseCheckInProcess)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.HelpBox("Needle Engine License check in progress...", MessageType.Info);
					if (GUILayout.Button("Get a License" + Constants.ExternalLinkChar, _bigButtonStyle))
					{
						Application.OpenURL(Constants.BuyLicenseUrl);
					}
				}
			}
			else if (LicenseCheck.HasLicense)
			{
				EditorGUILayoutExtensions.HelpBoxChecked($"Found valid Needle Engine {LicenseCheck.LastLicenseTypeResult.ToUpper()} license. Thank you for your support!");
			}
			else
			{
				using (new GUILayout.HorizontalScope())
				{
					if (NeedleEngineAuthorization.IsInTrialPeriod)
					{
						EditorGUILayoutExtensions.HelpBoxChecked($"Needle Engine Trial is active for " + NeedleEngineAuthorization.DaysUntilTrialEnds + " more days");
					}
					else
					{
						EditorGUILayout.HelpBox("Needle Engine Basic license found. Get a Needle Engine INDIE or PRO License for commercial use and unlimited access to all features",
							MessageType.Warning);
					}
					if (GUILayout.Button("Get a License" + Constants.ExternalLinkChar, _bigButtonStyle))
					{
						Application.OpenURL(Constants.BuyLicenseUrl);
					}
				}
			}
			
			
		}

		private static void DrawOptimizationGUI()
		{
			GUILayout.Space(5);
			GUILayout.Label("Build Optimization", EditorStyles.largeLabel);

			if (!CheckIfToktxVersionIsInstalled())
			{
				using (new GUILayout.HorizontalScope())
				{
					var msg =
						"Please install the recommended toktx version to support optimized production builds. Toktx is used to compress your glTF and GLB files for faster loading and less memory usage.";
					if (ToolsHelper.IsDownloadingToktx)
					{
						msg += " → Downloading toktx...";
					}
					EditorGUILayout.HelpBox(msg, MessageType.Warning, true);
					GUILayout.FlexibleSpace();

					if (!File.Exists(ToolsHelper.ToktxDownloadLocation))
					{
						using (new EditorGUI.DisabledScope(ToolsHelper.IsDownloadingToktx))
						{
							var text = ToolsHelper.IsDownloadingToktx
								? "Downloading..."
								: "Download & Run\nToktx Installer";
							var label = new GUIContent(text,
								"Toktx is used for compressing your glb output files when making production builds. Click this button to download the recommended toktx version and start the installer!");
							if (GUILayout.Button(label, _bigButtonStyle))
							{
								ToolsHelper.DownloadAndRunToktxInstaller();
							}
						}
					}
					else
					{
						DrawInstallerButtons(ToolsHelper.ToktxDownloadLocation);
					}
				}
			}
			else
			{
				EditorGUILayoutExtensions.HelpBoxChecked("KTX2 Texture compression is available");
			}

			EditorGUILayoutExtensions.HelpBoxChecked("WebP Texture compression is available");
			EditorGUILayoutExtensions.HelpBoxChecked("Draco Mesh compression is available");
			EditorGUILayoutExtensions.HelpBoxChecked("Meshopt Mesh compression is available");
		}

		private static void DrawInstallerButtons(string installerLocation)
		{
			using (new EditorGUILayout.VerticalScope())
			{
				if (GUILayout.Button("Show Installer", _rightButtonWidth))
				{
					EditorUtility.RevealInFinder(installerLocation);
				}
				if (GUILayout.Button(new GUIContent("Restart Unity"), _rightButtonWidth))
				{
					EditorApplication.OpenProject(Directory.GetCurrentDirectory());
				}
				GUILayout.Space(5);
			}
		}


		private static bool CheckIfToktxVersionIsInstalled()
		{
			if (DateTime.Now - lastToktxCheckTime < TimeSpan.FromSeconds(5)) return hasCorrectToktxVersionInstalled;
			lastToktxCheckTime = DateTime.Now;
			return hasCorrectToktxVersionInstalled = Actions.HasMinimumToktxVersionInstalled(out _);
		}

		private static bool CheckIfSupportedNodejsInstalled()
		{
			if (DateTime.Now - lastNodejsCheckTime < TimeSpan.FromSeconds(5)) return hasCorrectNodejsVersionInstalled;
			lastNodejsCheckTime = DateTime.Now;

			async void _run()
			{
				hasCorrectNodejsVersionInstalled = await InternalActions.HasSupportedNodeJsInstalled();
				nodejsVersionInstalled = await InternalActions.GetNodeJsVersion();
				npmVersionInstalled = await InternalActions.GetNpmVersion();
				// if (!string.IsNullOrWhiteSpace(npmVersionInstalled))
				// {
				// 	hasCorrectNpmVersionInstalled = false;
				// }
				// else if (SemVersion.TryParse(npmVersionInstalled, SemVersionStyles.Any, out var npmVersion))
				// {
				// 	hasCorrectNpmVersionInstalled = npmVersion >= new SemVersion(10, 0, 0);
				// }
			}

			_run();
			return hasCorrectNodejsVersionInstalled;
		}
	}
}