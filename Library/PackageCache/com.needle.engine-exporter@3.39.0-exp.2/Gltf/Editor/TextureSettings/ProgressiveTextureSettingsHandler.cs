using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Needle.Engine.Gltf.Experimental.progressive;
using Needle.Engine.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Gltf
{
	[UsedImplicitly]
	public class ProgressiveTextureSettingsHandler : GltfExtensionHandlerBase, ITextureExportHandler
	{
		private ProgressiveTexturesSettings progressiveTextureSettingsComponent;
		// private bool isUsingProgressiveTextures;
		
		public override void OnBeforeExport(GltfExportContext context)
		{
			base.OnBeforeExport(context);
			// isUsingProgressiveTextures = false;
			progressiveTextureSettingsComponent = Object.FindObjectsByType<ProgressiveTexturesSettings>(FindObjectsSortMode.None).FirstOrDefault(e => e.enabled);
			TextureExportHandlerRegistry.Register(this);
		}

		public bool OnTextureExport(GltfExportContext context, ref TextureExportSettings exportSettings, string textureSlot, List<object> extensions)
		{
			if (exportSettings.Texture is Texture2D tex2d)
			{
#if UNITY_EDITOR
				if (tex2d.IsHDR())
				{
					return false;
				}
				
				// First check if we have a use progressive textures component on the exported asset
				if (!context.Root.TryGetComponent(out ProgressiveTexturesSettings progressiveTexturesSettings))
				{
					// Otherwise fallback to another progressive texture component found in the scene
					progressiveTexturesSettings = progressiveTextureSettingsComponent;
				}

				// Check if progressive texture loading has been disabled by the settings component
				if (progressiveTexturesSettings && progressiveTexturesSettings.AllowProgressiveLoading == false)
				{
					// Workaround to disable progressive textures: setting an invalid max size to -1
					extensions.Add(new NEEDLE_progressive_texture_settings(tex2d.GetId(),-1, false));
					return true;
				}
				
				// Try get the progressive texture settings for the texture
				if (NeedleAssetSettingsProvider.TryGetTextureSettings(tex2d, out var textureSettings))
				{
					if (textureSettings.Override && textureSettings.UseProgressiveLoading)
					{
						extensions.Add(new NEEDLE_progressive_texture_settings(tex2d.GetId(), textureSettings.ProgressiveLoadingSize));
						// isUsingProgressiveTextures = true;
						return true;
					}
				}
				
				// check Asset labels if the texture should be skipped
				var tags = UnityEditor.AssetDatabase.GetLabels(exportSettings.Texture);
				foreach (var tag in tags)
				{
					if (tag.Equals("noprogressive", StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}
				
				// check if the asset is a sprite and thus likely to be used in a UI element
				// those don't support progressive loading so make them progressive
				// https://github.com/needle-tools/needle-tiny-playground/issues/539
				var path = UnityEditor.AssetDatabase.GetAssetPath(tex2d);
				if (!string.IsNullOrEmpty(path))
				{
					var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
					if (importer && importer.textureType == UnityEditor.TextureImporterType.Sprite)
					{
						return false;
					}
				}
				
				// dont make lightmaps progressive until we support that
				var lm = LightmapSettings.lightmaps;
				foreach (var lightmap in lm)
				{
					if (tex2d == lightmap.lightmapColor) return false;
				}
				
				// TODO: filter skybox textures
				
				// add the extension containing the settings for making this texture progressive
				// this is done as part of a gltf transform step
				if (progressiveTexturesSettings != null && progressiveTexturesSettings.UseMaxSize)
				{
					// We only need to load it progressively if the texture is larger than the max size
					if (progressiveTexturesSettings.MaxSize < Mathf.Max(tex2d.width, tex2d.height))
					{
						var ext = new NEEDLE_progressive_texture_settings(tex2d.GetId(), progressiveTexturesSettings.MaxSize);
						extensions.Add(ext);
						return true;
					}
					return false;
				}
				// If no explicit settings were found we just export and let the compression package resize as it sees fit (depending on the usecase or further arguments)
				return false;
#endif
			}
			return false;
		}
		
		// public override void OnExportFinished(GltfExportContext context)
		// {
		// 	// We now run the progressive command in either the whole assets directory or when building in the dist directory
		// 	if (isUsingProgressiveTextures)
		// 	{
		// 		// Only run progressive transformation for distribution builds
		// 		if (!context.TryGetBuildContext(out var ctx) || ctx.IsDistributionBuild)
		// 		{
		// 			var path = context.Path;
		// 			var dir = context.ProjectDirectory;
		// 			Debug.Log("<b>Begin transform progressive</b>: " + path + "\n" + dir);
		// 			var task = ActionsCompression.MakeProgressiveSingle(path);
		// 			// ensure that the progressive transformation is done before deployment
		// 			BuildTaskList.SchedulePostExport(task);
		// 		}
		// 	}
		// }
	}
}