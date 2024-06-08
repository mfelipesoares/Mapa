using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Needle.Engine.Samples.Helpers;
using Needle.Engine.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Needle.Engine.Samples
{
	public class SamplesWindow : EditorWindow, IHasCustomMenu
	{
		internal const string SamplesUrl = "https://engine.needle.tools/samples";
		// private const string Unity2020Requirements = "Unity 2020.3.33+ and URP";
		private const string Unity2021Requirements = "Unity 2021.3.9+";
		private const string Unity2022Requirements = "Unity 2022.3+";
		private const string NonLtsRequirements = "Newer non-LTS versions (use at your own risk)";
		
		// [MenuItem("Window/Needle/Needle Engine Samples", priority = -1000)]
		[MenuItem("Needle Engine/Explore Samples 👀", priority = Engine.Constants.MenuItemOrder - 998)]
		public static void Open()
		{
			var existing = Resources.FindObjectsOfTypeAll<SamplesWindow>().FirstOrDefault();
			if (existing)
			{
				existing.Show(true);
				existing.Focus();
			}
			else
			{
				CreateWindow<SamplesWindow>().Show();
			}
		}

		private static bool DidOpen
		{
			get => SessionState.GetBool("OpenedNeedleSamplesWindow", false);
			set => SessionState.SetBool("OpenedNeedleSamplesWindow", value);
		}
		
		/// <summary>
		/// Enable to view samples "as remote"
		/// </summary>
		private static bool ForceRemoteNeedleSamples
		{
			get => SessionState.GetBool(nameof(ForceRemoteNeedleSamples), false);
			set => SessionState.SetBool(nameof(ForceRemoteNeedleSamples), value);
		}

		[InitializeOnLoadMethod]
		private static async void Init()
		{
			if (DidOpen) return;
			DidOpen = true;
			await Task.Yield();
			// Open samples window automatically on start only when in samples project 
			if(Application.dataPath.Contains("Needle Engine Samples") && Application.isBatchMode == false)
				Open();
		}

		
		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Refresh"), false, Refresh);
			menu.AddItem(new GUIContent("Reopen Window"), false, () =>
			{
				Close();
				Open();
			});
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Force Remote Samples"), ForceRemoteNeedleSamples, () =>
			{
				ForceRemoteNeedleSamples = !ForceRemoteNeedleSamples;
				HaveFetchedNeedleSamples = false;
				Close();
				Open();
			});
			if (HaveSamplesPackage)
			{
				menu.AddItem(new GUIContent("Remove Sample Package"), false, () =>
				{
					Client.Remove(Constants.SamplesPackageName);
				});
			}
			
			var isDevMode = PackageUtils.IsMutable(Engine.Constants.TestPackagePath) ||
			                SessionState.GetBool("Needle_SamplesWindow_DevMode", false);
			if (isDevMode)
			{
				menu.AddSeparator("");
				menu.AddItem(
					new GUIContent(
						"Update Samples Artifacts", 
						"Creates samples.json and Samples.md in the repo root with the current sample data.\nAlso bumps the Needle Exporter dependency in the samples package to the current."), 
					false, () => {
						SampleCollectionEditor.ProduceSampleArtifacts();
					});
				menu.AddItem(
					new GUIContent(
						"Export Local Package .tgz", 
						"Outputs the Samples package as immutable needle-engine-samples.tgz.\nThis is referenced by Tests projects to get the same experience as installing the package from a registry."),
					false, () =>
					{
						SampleCollectionEditor.ExportLocalPackage();
					});
				menu.AddItem(
					new GUIContent("Copy Samples Checklist to Clipboard"), false, () =>
					{
						var samples = GetLocalSampleInfos();
						
						var joined = string.Join("\n", samples.OrderBy(x => x.Name).Select(x => x.Name));
						EditorGUIUtility.systemCopyBuffer = joined;
						
						ShowNotification(new GUIContent("Copied sample list to clipboard"));
					});
			}
		}

		private static bool HaveSamplesPackage =>
#if HAS_NEEDLE_SAMPLES
			!ForceRemoteNeedleSamples;
#else
			false;
#endif

		private void Refresh()
		{
			if (!this) return;
			rootVisualElement.Clear();
			RefreshAndCreateSampleView(rootVisualElement, this);
		}

		private static bool CanInstallSamples
		{
			#if (HAS_URP && HAS_2020_LTS) || HAS_2021_LTS || HAS_2022_LTS || HAS_2023_LTS || HAS_2024_LTS
			get => true;
			#else
			get => false;
			#endif
		}
		
		private static bool HasUnityLTSVersion
		{
			#if HAS_UNITY_NEWER_THAN_LTS
			get => false;
			#elif HAS_2021_LTS || HAS_2022_LTS || HAS_2023_LTS || HAS_2024_LTS
			get => true;
			#else
			get => false;
			#endif
		}

		private const string LTSWarning = "⚠️ Warning\nYou don't seem to be on a supported Unity LTS version.\nWe recommend using the latest LTS versions of Unity.";

		internal static int CountPathIndents(string path) => path.Count(y => y == '\\' || y == '/');

        internal static List<SampleInfo> GetLocalSampleInfos(bool includeNonSamples = false)
		{
			var sampleInfos = AssetDatabase.FindAssets("t:" + nameof(SampleInfo))
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<SampleInfo>)
				.ToList();

			AssetDatabase.Refresh();

			if (includeNonSamples)
			{
				var scenes = AssetDatabase.FindAssets("t:SceneAsset", new[] { Constants.SamplesDirectory });

				// Filtering out scenes that are not in the root folders of individual samples
				// + 2 since:
				// root has 2 idents (Packages/samples/Runtime)
				// scene path has 4 idents (Packages/samples/Runtime/Sample/Scene.unity
				int sampleSceneIndentLimit = CountPathIndents(Constants.SamplesDirectory) + 2;

				var filteredScenes = scenes
					.Select(AssetDatabase.GUIDToAssetPath)
					.Where(x => CountPathIndents(x) <= sampleSceneIndentLimit)
					.Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
					.Where(x => x) // seems after package updates we can get missing/null entries from the AssetDB...
					.OrderBy(x => x.name);

				foreach (var sceneAsset in filteredScenes)
				{
					if (sampleInfos.Any(s => s.Scene == sceneAsset)) continue;

					var info = CreateInstance<SampleInfo>();
					info.Scene = sceneAsset;
					info.name = sceneAsset.name;
					if (TryGetScreenshot(sceneAsset.name, out var screenshotPath))
					{
						info.Thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(screenshotPath);
					}
					sampleInfos.Add(info);
				}
			}

			sampleInfos = sampleInfos
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.DisplayNameOrName)
				.ThenBy(x => !(bool) x.Thumbnail)
                .ToList();
			
			return sampleInfos;
		}
		
		internal static async void RefreshAndCreateSampleView(VisualElement parent, object context)
		{
			var loadingLabel = new Label("Loading...")
			{
				name = "LoadingLabel",
				style = {
					fontSize = 24,
					unityTextAlign = TextAnchor.MiddleCenter,
					height = new StyleLength(new Length(90, LengthUnit.Percent)),
					opacity = 0.5f,
				}
			};
			parent.Add(loadingLabel);
			await Task.Delay(10); // give the UI a chance to update

			List<SampleInfo> sampleInfos = default;
			
			// sampleInfos can either come from the project we're in, or come from a JSON file.
			// when the samples package is present, we use that, otherwise we fetch the JSON from elsewhere
			if (HaveSamplesPackage)
			{
				sampleInfos = GetLocalSampleInfos();
			}
			else
			{
				var serializerSettings = SerializerSettings.Get();
				serializerSettings.Context = new StreamingContext(StreamingContextStates.Persistence, context);

				var cachePath = default(string);
				
#if false // for local testing
				var rootPath = "../../";
				var jsonPath = rootPath + "samples.json";
				var json = File.ReadAllText(jsonPath);
				await Task.CompletedTask;
#else
				var jsonPath = Constants.RemoteSampleJsonPath;
				cachePath = Constants.CacheRoot + SanitizePath(Path.GetFileName(jsonPath));	
				if (!cachePath.EndsWith(".json")) cachePath += ".json";
				if (HaveFetchedNeedleSamples && File.Exists(cachePath))
					jsonPath = "file://" + Path.GetFullPath(cachePath);

				var request = new UnityWebRequest(jsonPath);
				request.downloadHandler = new DownloadHandlerBuffer();
				var op = request.SendWebRequest();
				while (!op.isDone) await Task.Yield();
				if (request.result != UnityWebRequest.Result.Success)
				{
					var errorMessage = "Error: " + request.result + ", " + request.error;
					parent.Q<Label>("LoadingLabel").text = "Failed to download samples.json.\n" + errorMessage;
					Debug.LogError(errorMessage + ", File: " + jsonPath);
					HaveFetchedNeedleSamples = false;
					return;
				}
				var json = request.downloadHandler.text;
#endif
				
				var collection = JsonConvert.DeserializeObject<SampleCollection>(json, serializerSettings);
				if (collection != null && collection.samples.Any() && !HaveFetchedNeedleSamples && cachePath != null)
				{
					try
					{
						Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
						File.WriteAllText(cachePath, json);
						HaveFetchedNeedleSamples = true;
					}
					catch (Exception e)
					{
						Debug.LogError("Exception: " + e + " on path " + cachePath);
					}
				}

				sampleInfos = collection ? collection.samples : null;
			}

			parent.Clear();
			parent.Add(CreateSampleView(sampleInfos));
		}

		
		internal static bool HaveFetchedNeedleSamples
		{
			get => SessionState.GetBool(nameof(HaveFetchedNeedleSamples), false);
			set => SessionState.SetBool(nameof(HaveFetchedNeedleSamples), value);
		}

		private static bool TryGetScreenshot(string sceneName, out string path)
		{
			path = Constants.ScreenshotsDirectory + "/" + sceneName + ".png";
			if (File.Exists(path)) return true;
			path = Constants.ScreenshotsDirectory + "/" + sceneName + ".jpg";
			return File.Exists(path);
		}

		internal static string SanitizePath(string path)
		{
			return path.Replace("?", "_")
				.Replace(":", "")
				.Replace("/", "")
				.Replace("#", "_");
		}

		private async void OnEnable()
		{
			if (!this) 
				return;
			
			titleContent = new GUIContent("Needle Engine Samples", AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f")));
			// EditorSceneManager.activeSceneChangedInEditMode += (s, o) => Refresh();
			maxSize = new Vector2(10000, 5000);
			minSize = new Vector2(360, 420);
			
			// TODO not sure how to only do this if this window hasn't been manually resized by the user
			try {
				var p = position;
				p.width = 1080;
				if (this) position = p;
			}
			catch {
				// ignore
			}

			await Task.Delay(1);
			Refresh();
		}

		private Vector2 scroll;
		private double lastClickTime;

		internal static IEnumerable<StyleSheet> StyleSheet
		{
			get
			{
				yield return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("1d7049f4814274e4b9f6f99f2bc36c90"));
				#if UNITY_2021_3_OR_NEWER
				yield return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("34d4f048a70ad6e4d940ef9c8f74c2da"));
				#endif
			}
		}
		
		private void CreateGUI()
		{

		}

		internal static VisualElement CreateSampleView(List<SampleInfo> sampleInfos)
		{
			if (sampleInfos == null) return null;
			
			var root = new VisualElement();
			var scrollView = new ScrollView();

			string viewInBrowserText = "View in a Browser " + Needle.Engine.Constants.ExternalLinkChar;
			string viewInBrowserTooltip = "View and run all samples live in your browser.";
			
			// toolbar
			var tb = new Toolbar();
			tb.Add(new ToolbarButton(() => Application.OpenURL(SamplesUrl)) { text = viewInBrowserText, tooltip = viewInBrowserTooltip });
			tb.Add(new ToolbarButton(() => Application.OpenURL("https://engine.needle.tools/docs")) { text = "Documentation " + Needle.Engine.Constants.ExternalLinkChar});
			tb.Add(new ToolbarSpacer());
			var search = new SamplesSearchField(scrollView);
			tb.Add(search);
			root.Add(tb);
			
			var header = new VisualElement();
			header.AddToClassList("header");
			header.Add(new Label("Explore Needle Engine Samples"));
			var buttonContainer = new VisualElement();
			buttonContainer.AddToClassList("buttons");
			
			var samplesFolder = "Packages/" + Constants.SamplesPackageName + "/Runtime";
			var reallyHaveSamples = HaveSamplesPackage && Directory.Exists(samplesFolder);
			
			if (reallyHaveSamples)
			{
				
			}
			else if (!HaveSamplesPackage && CanInstallSamples)
			{
				var showWarning = !HasUnityLTSVersion;
				var text = "Install Samples Package";
				var tooltip = "Adds \"com.needle.engine-samples\" to your project.";
				if (showWarning)
				{
					text += " (LTS recommended)";
					tooltip = LTSWarning + "\n\n" + tooltip;
				}
				var installButton = CreateInstallSamplesButton(showWarning ? "" : text, tooltip);
				if (showWarning)
				{
					installButton.style.flexDirection = FlexDirection.Row;
					// get a texture for a warning symbol from the editor
					var warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D;
					installButton.Add(new Image() { image = warningIcon });
					installButton.Add(new Label(text) { style = { marginBottom = 0 }});
				}
				buttonContainer.Add(installButton);
			}
			
			if (reallyHaveSamples)
			{
				var version = ProjectInfo.GetCurrentNeedleEngineSamplesVersion();
				var v = new Label("Samples Installed " + version);
				var i = new Image() { image = EditorGUIUtility.IconContent("icons/packagemanager/dark/installed@2x.png").image };
				i.AddToClassList("icon");
				var v0 = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
				v0.Add(i);
				v0.Add(v);
				buttonContainer.Add(v0);


				// if (NpmUnityEditorVersions.TryGetRecommendedVersion(Engine.Constants.SamplesPackageName, out var recommendedSamplesVersion))
				// {
				// 	CheckForSampleUpdates(recommendedSamplesVersion);
				// }
				// async void CheckForSampleUpdates(string recommendedVersionString)
				// {
				// 	// query available versions for the samples package from PackMan
				// 	var request = Client.Search(Constants.SamplesPackageName, false);
				// 	while (!request.IsCompleted)
				// 		await Task.Yield(); 
				// 	Debug.Log("Results: " + string.Join("\n", request.Result.Select(x => x.name + "@" + x.version)));
				// 	var latestPackage = request.Result.OrderByDescending(x => x.version).FirstOrDefault();
				// }
			}
			
			if (!HaveSamplesPackage && !CanInstallSamples)
			{
				var tooltip = $"The samples package requires URP or BiRP and either\n" +
				              $" · {Unity2021Requirements}\n" +
				              $" · {Unity2022Requirements}\n" +
				              $" · {NonLtsRequirements}\n" +
				              $"It's recommended to use the latest LTS version of them.";
				var container = new VisualElement();
				container.Add(new HelpBox(tooltip, HelpBoxMessageType.Warning));
				container.Add(new Button(() =>
				{
					Application.OpenURL("https://engine.needle.tools/docs/getting-started");
				}) { text = "Learn more about installing the samples " + Needle.Engine.Constants.ExternalLinkChar, 
					tooltip = tooltip,
				});
				buttonContainer.Add(container);
			}
			header.Add(buttonContainer);
			scrollView.Add(header);

			Dictionary<Sample, SampleInfo> sampleInfoByUI = new Dictionary<Sample, SampleInfo>();

			// samples with thumbnails
			var itemContainer = new VisualElement();
			itemContainer.AddToClassList("items");
			var tags = new VisualElement();
			tags.AddToClassList("tags");
			
			Dictionary<Tag, TagButton> tagToButton = new Dictionary<Tag, TagButton>();
			
			foreach (var sample in sampleInfos.Where(x => x.Thumbnail))
			{
				var ui = new Sample(sample, (tag) => ApplyTagFiltering(sampleInfoByUI, tag));
                itemContainer.Add(ui);
                sampleInfoByUI.Add(ui, sample);
                
                // add to tag list at the top of the page
                foreach (var tag in sample.Tags)
                {
	                if (tag == null) continue;
	                if (!tagToButton.TryGetValue(tag, out var btn))
	                {
		                btn = new TagButton(tag, (t) => ApplyTagFiltering(sampleInfoByUI, t), 0);
		                btn.name = tag.name;
		                tags.Add(btn);
		                tagToButton.Add(tag, btn);
	                }
	                btn.Count++;
                }
            }

			tags.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
			scrollView.Add(tags);
			scrollView.Add(itemContainer);
			
			// samples without thumbnails
			var itemContainerNoThumbnail = new VisualElement();
			itemContainerNoThumbnail.AddToClassList("items");
			foreach (var sample in sampleInfos.Where(x => !x.Thumbnail))
			{
                var ui = new Sample(sample, (tag) => ApplyTagFiltering(sampleInfoByUI, tag));
                itemContainerNoThumbnail.Add(ui);
                sampleInfoByUI.Add(ui, sample);
            }
			scrollView.Add(itemContainerNoThumbnail);
			
			root.Add(scrollView);
			foreach (var style in StyleSheet)
				if (style)
					root.styleSheets.Add(style);
			if (!EditorGUIUtility.isProSkin) root.AddToClassList("__light");

			// responsive layout - basically a media query for screen width
			const int columnWidth = 360;
			const int maxCols = 10;
			root.RegisterCallback<GeometryChangedEvent>(evt =>
			{
				for (int i = 1; i < 20; i++)
					scrollView.RemoveFromClassList("__columns_" + i);
				var cols = Mathf.FloorToInt(evt.newRect.width / columnWidth);
				cols = Mathf.Min(cols, maxCols);
				cols = Mathf.Max(cols, 1);
				scrollView.AddToClassList("__columns_" + cols);
			});

			if (SessionState.GetBool(NeedsRefreshKey, false))
			{
				SessionState.EraseBool(NeedsRefreshKey);
				async void RefreshAfterDelay(int delay)
				{
					await Task.Delay(delay);
					GetWindow<SamplesWindow>().Refresh();
				}
				RefreshAfterDelay(1000);
			}
			
			search.Apply();
			var previouslySelectedTag = TagButton.RestoreSelectedState(tagToButton.Keys.ToArray());
			if(previouslySelectedTag) ApplyTagFiltering(sampleInfoByUI,previouslySelectedTag); 
			
			return root;
		}

		private static Button CreateInstallSamplesButton(string text, string tooltip)
		{
			Button installButton = default;
			installButton = new Button(InstallSamples)
			{
				text = text, 
				tooltip = tooltip
			};
			installButton.AddToClassList("install-samples-button");
			return installButton;
		}
		
		private static void OnInstallSamplesStarted()
		{
			var window = GetWindow<SamplesWindow>();
			if (window.rootVisualElement != null)
			{
				var buttons = window.rootVisualElement.Query<Button>(null, "install-samples-button").ToList();
				foreach (var button in buttons)
				{
					WaitForInstallationToFinish(button);
				}
			}

			async void WaitForInstallationToFinish(Button button)
			{
				var originalText = button.text;
				var originalWidth = button.layout.width;
				// Make sure the warnings etc that are inside the button are removed
				button.Clear();
				// we don't want to click the button while it's installing
				button.SetEnabled(false);
				var baseText = "Installing";
				button!.text = baseText + "..";
				var buttonWidth = button.layout.width;
				button.style.minWidth = buttonWidth;
				await Task.Delay(100);
				var i = 0;
				while (currentInstallationRequest != null && !currentInstallationRequest.IsCompleted)
				{
					await Task.Delay(500);
					button.text = baseText;
					for(var k = 0; k < i; k++) button.text += ".";
					i += 1;
					if (i > 3) i = 0;
				}
				button.style.minWidth = originalWidth; 
				// "Reset" the button state
				button.SetEnabled(true);
				button.text = originalText;	
			}
		}
		
		private static void ApplyTagFiltering(Dictionary<Sample, SampleInfo> samples, Tag newTag)
		{
			if (TagButton.IsSelected(newTag))
				TagButton.Deselect(newTag);
			else
				TagButton.Select(newTag);

			foreach(var x in samples)
			{
				var instance = x.Key;
				var data = x.Value;

				var shouldBeActive = !TagButton.HasSelection || (data && data.Tags != null && data.Tags.Any(TagButton.IsSelected));
				// instance.style.opacity = shouldBeActive ? 1 : 0.3f;
				
				if (!shouldBeActive)
					instance.AddToClassList("hidden");
				else
					instance.RemoveFromClassList("hidden");
			}
		}

		private class SamplesSearchField : ToolbarSearchField
		{
			const string cacheKey = "Needle Engine Samples Search";
			
			private readonly VisualElement sampleRoot;
			
			public SamplesSearchField(VisualElement sampleRoot)
			{
				this.sampleRoot = sampleRoot;
				this.RegisterValueChangedCallback(e =>
				{
					UpdateFilter(e.newValue);
				});
				// this.AddToClassList(ToolbarSearchField.popupVariantUssClassName);
				// this.searchButton.clickable.clicked += () =>
				// {
				// };
			}

			internal void Apply()
			{
				var text = SessionState.GetString(cacheKey, "");
				value = text; 
				if(!string.IsNullOrWhiteSpace(value)) UpdateFilter(text);
			}

			private void UpdateFilter(string filter)
			{
				var text = filter.ToLower();
				SessionState.SetString(cacheKey, text);
				// query samples in this container
				foreach (var sample in sampleRoot.Query<Sample>().ToList())
				{
					var shouldBeVisible =
						string.IsNullOrEmpty(text) ||
						(sample.Info.DisplayNameOrName != null && sample.Info.DisplayNameOrName.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1) || 
						(sample.Info.Description != null && sample.Info.Description.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1);
					if(!shouldBeVisible) sample.AddToClassList("hidden");
					else sample.RemoveFromClassList("hidden");
				}
			}
		}
		
        private static AddRequest currentInstallationRequest;
        private const string NeedsRefreshKey = "FreshInstallation_SamplesWindowNeedsDelayedRefresh";
        
		private static async void InstallSamples()
		{
			if(currentInstallationRequest != null && !currentInstallationRequest.IsCompleted)
				return;
			
			// show Editor dialogue asking for confirmation
			var result = EditorUtility.DisplayDialog(
				"Install Samples Package", 
				"This will add the Samples package to your project.\n\n" +
				(!HasUnityLTSVersion ? LTSWarning : "") +
				"Do you want to continue?", 
				"Yes", "No");
			if (!result)
			{
				Debug.Log("Installation cancelled.");
				return;
			}
			try
			{
				OnInstallSamplesStarted();
				EditorApplication.LockReloadAssemblies();
				Log("Installing Needle Engine Samples... please wait.");
				var progressId = Progress.Start("Installing Needle Engine Samples",
					"The samples package is being added using Unity's package manager, please stand by!",
					Progress.Options.Managed | Progress.Options.Indefinite);
				currentInstallationRequest = Client.Add(Constants.SamplesPackageName);
				SessionState.SetBool(NeedsRefreshKey, true);
				while (!currentInstallationRequest.IsCompleted)
					await Task.Delay(500);
				switch (currentInstallationRequest.Status)
				{
					case StatusCode.Success:
						Progress.Finish(progressId);
						Log($"<b>{"Successfully".AsSuccess()}</b> installed Needle Engine Samples package.");
						SessionState.SetBool(NeedsRefreshKey, true);
						break;
					case StatusCode.Failure:
						Progress.Finish(progressId, Progress.Status.Failed);
						Log($"<b>{"Failed".AsError()}</b> installing Needle Engine Samples package: {currentInstallationRequest.Error.message}");
						break;
					default:
						Progress.Finish(progressId);
						Log("Unexpected installation result: " + currentInstallationRequest.Status + ", " + currentInstallationRequest.Error.message);
						break;
				}
			}
			finally
			{
				currentInstallationRequest = null;
				await Task.Delay(200); // just to see the log
				EditorApplication.UnlockReloadAssemblies();
			}
		}

		private static void Log(object msg)
		{
			Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", msg);
		}

		internal class Sample : VisualElement
		{
			public SampleInfo Info => sample;
			private readonly SampleInfo sample;

			public Sample(SampleInfo sample, Action<Tag> onTagSelected = null)
			{
                this.sample = sample;

				if (!sample.Thumbnail)
				{
					AddToClassList("no-preview");
				}
				else
				{
					var preview = new Image() { image = sample.Thumbnail, scaleMode = ScaleMode.ScaleAndCrop};
					var v = new VisualElement();
					v.AddToClassList("image-container");
					v.Add(preview);
					Add(v);
				}

				var click = new Clickable(DoubleClick);
				click.activators.Clear();
				click.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, clickCount = 2} );
				this.AddManipulator(click);
				this.AddManipulator(new Clickable(Click));

				var content = new VisualElement() { name = "Content" };
				var overlay = new VisualElement();
				overlay.AddToClassList("overlay");
				overlay.Add(new Label() { name = "Title", text = sample.DisplayNameOrName } );
				overlay.Add(new Label() { text = sample.Description } );
				content.Add(overlay);
				
				var options = new VisualElement();
				options.AddToClassList("options");
				if (!string.IsNullOrEmpty(sample.LiveUrl))
				{
					var btn = new Button(_Live) { text = "Live ↗", tooltip = "Open " + sample.LiveUrl };
					btn.clickable.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt } );
					options.Add(btn);
				}
				if (sample.Scene)
					options.Add(new Button(_OpenScene) { text = "Open Scene" });
				else if (!HaveSamplesPackage && CanInstallSamples)
				{
					var installSamplesButton = CreateInstallSamplesButton("Install Samples",
						"Click to install the Needle Engine Samples package. This might take a moment.");
					options.Add(installSamplesButton);
				}
				content.Add(options);
				Add(content);
				if (sample.Tags != null)
				{
					var tags = new VisualElement();
					tags.AddToClassList("tags");
					foreach (var tag in sample.Tags)
					{
						if (tag == null) continue;
						tags.Add(new TagButton(tag, onTagSelected));
					}
					Add(tags);
				}
			}

			private void DoubleClick(EventBase evt) => _OpenScene();
			private void Click(EventBase evt) => EditorGUIUtility.PingObject(sample.Scene);

			private void _OpenScene()
			{
				if (sample.Scene)
				{
					OpenScene(sample.Scene);
					GUIUtility.ExitGUI();
				}
				else _Live();
			}

			private string NameToAnchor(string Name)
			{
				return Name.ToLowerInvariant()
					.Replace(" ", "-")
					.Replace("(", "-")
					.Replace(")", "-")
					.Replace("--","-")
					.Replace("--","-")
					.Trim('-');
			}
			
			private void _Live()
			{
				if (string.IsNullOrEmpty(sample.LiveUrl)) return;
				
				// check if ALT key is pressed
				if (Event.current.alt)
				{
					Application.OpenURL(sample.LiveUrl);
					return;
				}
				
				var url = SamplesUrl + "/?from-editor#" + NameToAnchor(sample.Name);
				Application.OpenURL(url);
			} 
		}

		internal static void OpenScene(SceneAsset asset)
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
			var scenePath = AssetDatabase.GetAssetPath(asset);
			if (PackageUtils.IsMutable(scenePath))
			{
				EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			}
			else
			{
				// make a copy of the scene file in Assets/Samples/Needle Engine/scene.unity
				var samplesFolder = Path.Combine("Assets", "Samples", "Needle Engine");
				if (!Directory.Exists(samplesFolder))
					Directory.CreateDirectory(samplesFolder);
				var targetPath = Path.Combine(samplesFolder, Path.GetFileName(scenePath));
				
				if (File.Exists(targetPath))
				{
					// show dialogue or open user scene again? --- user can manually delete to reopen the original sample scene
					// if (EditorUtility.DisplayDialog("Scene already exists", "The scene file already exists in the Samples folder.\nDo you want to overwrite it?", "Overwrite and open", "Just open"))
					// 	File.Delete(targetPath);
					// else
					EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
				}
				else
				{
					File.Copy(scenePath, targetPath, false);
					AssetDatabase.ImportAsset(targetPath);
					EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
					SampleUpdater.PatchActiveScene();
				}
				EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(targetPath));
			}
		}

		public static void MarkImagesDirty(VisualElement root)
		{
			root.Query<Image>().ForEach(x =>
			{
				var s = x.image;
				x.image = null;
				x.image = s;
			});
			root.MarkDirtyRepaint();
		}
	}

	internal sealed class TagButton : Button
	{
		private static readonly List<TagButton> TagButtons = new List<TagButton>();
		private static readonly List<Tag> SelectedTags = new List<Tag>();

		public static bool HasSelection => SelectedTags.Count > 0;
		public static bool IsSelected(Tag tag) => SelectedTags.Contains(tag);
		
		public static void Select(Tag tag, bool additive = false)
		{
			if (!additive) SelectedTags.Clear();
			SelectedTags.Add(tag);
			SessionState.SetString("NeedleEngine Samples SelectedTags", tag.name);
			foreach (var tagButton in TagButtons)
				tagButton.Selected = SelectedTags.Contains(tagButton.tag);
		}

		public static void Deselect(Tag tag)
		{
			if (!SelectedTags.Contains(tag)) return;
			SelectedTags.Remove(tag); 
			SessionState.SetString("NeedleEngine Samples SelectedTags", "");
			foreach (var tagButton in TagButtons)
				tagButton.Selected = SelectedTags.Contains(tagButton.tag);
		}

		public static Tag RestoreSelectedState(Tag[] tags)
		{
			var selected = SessionState.GetString("NeedleEngine Samples SelectedTags", "");
			if (!string.IsNullOrEmpty(selected))
			{
				foreach (var tag in tags)
				{
					if (tag.name == selected)
					{
						return tag;
					}
				}
			}
			return null;
		}

		public bool Selected
		{
			set
			{
				if (value) AddToClassList("selected");
				else RemoveFromClassList("selected");
			}
		}

		private readonly Tag tag;
		private readonly string baseText;
		private int count;

		public int Count
		{
			get => count;
			set
			{
				count = value;
				text = baseText + (value > 0 ? " · " + value : "");
			}
		}
		
        public TagButton(Tag tag, Action<Tag> onTagSelected = null, int count = 0)
		{
            this.count = count;
            this.tag = tag;
            baseText = tag.name;
			clicked += () => onTagSelected?.Invoke(tag);
			Count = count;
			
			TagButtons.Add(this);
			
			RegisterCallback<AttachToPanelEvent>(evt =>
			{
				TagButtons.Add(this);
				Selected = SelectedTags.Contains(tag);
			});
			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				TagButtons.Remove(this);
			});
			
			// TODO additive filtering with clicking not properly implemented yet
			/*
			var shiftClickable = new Clickable(() =>
			{
				if (IsSelected(tag)) Deselect(tag);
				else Select(tag, true);
			});
			shiftClickable.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
			this.AddManipulator(shiftClickable);
			*/
		}
	}
}