using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors
{
	internal class LicenseWindow : EditorWindow, ICanRebuild
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Builder.BuildStarting += OnBuild;
		}

		private static async void OnBuild()
		{
			if (Application.isBatchMode) return;

			var hasLicense = await LicenseCheck.HasValidLicense();
			if (hasLicense) return;

			// dont show every export
			var exportCount = SessionState.GetInt("NeedleEngine-ExportCount", 1);
			SessionState.SetInt("NeedleEngine-ExportCount", exportCount+1);
			var modulo = exportCount < 100 ? 10 : 5;
			if (exportCount % modulo != 0)
			{
				return;
			}
			ShowLicenseWindowAfterDelay();
		}

		private static async void ShowLicenseWindowAfterDelay()
		{
			await Task.Delay(1000);
			// close previous window to make sure it's not docked
			var window = GetWindow<LicenseWindow>();
			if(window) window.Close();
			ShowLicenseWindow();
		}
		[MenuItem("Needle Engine/Get a License", false,  Constants.MenuItemOrder - 993)]
		internal static void ShowLicenseWindow()
		{
			if (HasOpenInstances<LicenseWindow>())
			{
				var window = GetWindow<LicenseWindow>();
				window.Show();
			}
			else
			{
				var window = CreateInstance<LicenseWindow>();
				window.Show();
			}
		}

		private void Awake()
		{
		}

		private void OnEnable()
		{
			// var texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f"));
			titleContent = new GUIContent("Needle Engine License");
			minSize = new Vector2(480, 350);
			maxSize = new Vector2(480, 350);
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("386edbb0582e46b681bc6d04586c1fdc"), this);
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("3a2a854189a948f9b4646e41524e47ae"), this);
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("e66b654596c34b8aa460cd295a9df397"), this);
			UxmlWatcher.Register(AssetDatabase.GUIDToAssetPath("f85c6c94ddbd41b693a42604925dab44"), this);
			VisualElementRegistry.Register(rootVisualElement);
			BuildWindow();
		}

		private void OnGUI()
		{
			if (docked)
			{
				// undock
				position = position;
			}
		}

		private VisualElement ui;

		public void Rebuild()
		{
			BuildWindow();
		}

		private void BuildWindow()
		{
			var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("386edbb0582e46b681bc6d04586c1fdc"));
			ui = uiAsset.CloneTree();
			if (ui != null)
			{
				ui.AddToClassList("main");
				if (!EditorGUIUtility.isProSkin) ui.AddToClassList("__light");
				rootVisualElement.Clear();
				rootVisualElement.Add(ui);
				VisualElementRegistry.HookEvents(ui);

				// var password = ui.Q<TextField>();
				// if (password != null)
				// {
				// 	// TODO
				// }
			}
		}

	}
}