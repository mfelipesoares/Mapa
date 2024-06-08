using UnityEditor;
using UnityEditor.Modules;
using UnityEngine;

namespace Needle.Engine
{
	internal class NeedleEngineTextureImportSettings : DefaultTextureImportSettingsExtension
	{
		private readonly INeedleTextureSettingsGUIProvider[] guiProvider;

		internal NeedleEngineTextureImportSettings()
		{
			guiProvider = InstanceCreatorUtil.CreateCollectionSortedByPriority<INeedleTextureSettingsGUIProvider>().ToArray();
		}

		private bool wasOverriden = false;

		public override void ShowImportSettings(BaseTextureImportPlatformSettings settings)
		{
			DrawMaxSize(settings);
			var enabled = GUI.enabled;
			
			var platformSettings = settings.model.platformTextureSettings;

			if (platformSettings.overridden && !wasOverriden)
			{
				if(platformSettings.compressionQuality == 50) platformSettings.compressionQuality = 90;

			}
			wasOverriden = platformSettings.overridden;
				
			// enable progressive loading when override is disabled
			// because we want this to be enabled in general (and it's enabled by default)
			// without this change Progressive loading is currently disabled when a user overrides importer settings which is not what we want
			if (!platformSettings.overridden)
			{
				var cur = platformSettings.androidETC2FallbackOverride;
				platformSettings.androidETC2FallbackOverride = (AndroidETC2FallbackOverride)((int)platformSettings.androidETC2FallbackOverride).SetBit(0, true);
				// apply if it changed
				if(platformSettings.androidETC2FallbackOverride != cur)
					settings.Apply();
			}
			
			foreach (var prov in this.guiProvider)
			{
				prov.OnGUI(platformSettings);
			}
			GUI.enabled = enabled;
		}



		// private 

		#region Max Size
		private void DrawMaxSize(BaseTextureImportPlatformSettings settings)
		{
			var needleEngineMaxSize = settings.model.platformTextureSettings.maxTextureSize;
			var importer = settings.GetDefaultImportSettings();
			var currentTextureMaxSize = int.MaxValue;
			if (importer != null && importer.model.maxTextureSizeProperty != null)
			{
				currentTextureMaxSize = importer.model.maxTextureSizeProperty.intValue;
				if (currentTextureMaxSize < needleEngineMaxSize)
				{
					EditorGUILayout.HelpBox($"The max size is currently limited by your default importer settings set to {currentTextureMaxSize}. This limits Needle Engine's max texture size because Unity already reduced the texture size. Please increase the Max Size in the default importer settings if you want to export larger textures for Needle Engine", MessageType.Warning);
				}
			}
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = settings.model.maxTextureSizeIsDifferent;
			int maxTextureSize = EditorGUILayout.IntPopup(maxSize.text, needleEngineMaxSize, kMaxTextureSizeStrings,
				kMaxTextureSizeValues);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				settings.model.SetMaxTextureSizeForAll(maxTextureSize);
				if (maxTextureSize > currentTextureMaxSize)
				{
					Debug.LogWarning($"The max texture size is currently limited by your default importer settings to {currentTextureMaxSize} so setting the Needle Engine Max Size to ${maxTextureSize} will have no effect. To fix this please increase the Max Size in the default importer settings if you want to export larger textures for Needle Engine.");
				}
			}
		}

		private static readonly string[] kMaxTextureSizeStrings = new string[10]
		{
			"32",
			"64",
			"128",
			"256",
			"512",
			"1024",
			"2048",
			"4096",
			"8192",
			"16384"
		};

		private static readonly int[] kMaxTextureSizeValues = new int[10]
		{
			32,
			64,
			128,
			256,
			512,
			1024,
			2048,
			4096,
			8192,
			16384
		};

		private static readonly GUIContent maxSize = EditorGUIUtility.TrTextContent("Max Size", "Textures larger than this will be scaled down.");
		#endregion
	}
}