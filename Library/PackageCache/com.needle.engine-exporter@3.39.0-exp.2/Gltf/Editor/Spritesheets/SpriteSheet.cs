using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Gltf.Spritesheets
{
	[Serializable]
	public class SpriteSheet
	{
		private static bool isSerializing = false;
		
		public static bool TryCreate(object owner, Sprite sprite, GltfExportContext context, out JToken res)
		{
			// Make sure we dont run into circular serialization
			if(isSerializing)
			{
				res = default;
				return false;
			}
			var assetPath = AssetDatabase.GetAssetPath(sprite);
			var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (importer && 
#if UNITY_2022_1_OR_NEWER
			    // These types are now only accessible via the Sprites package. Sprites are still in core.
			    // Which means people can still use spritesheets without having the package installed;
			    // Which means we need to detect this without the package being present.
 				// importer.GetType().GetInterfaces().Any(x => x.Name == "ISpriteEditorDataProvider")
			    AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).OfType<Sprite>().Any()
#else
			    importer.spritesheet != null
#endif
			)
			{
				try
				{
					isSerializing = true;
					context.RegisterValueResolver(new SpriteSheetResolver(importer));
					var pointer = (string) context.AssetExtension.GetPathOrAdd(importer, owner, null);
					var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).OfType<Sprite>().ToList();
					
					res = new JObject();
					res["spriteSheet"] = pointer;
					res["index"] = sprites.IndexOf(sprite);
					
					return true;
				}
				finally
				{
					isSerializing = false;
				}
			}
			res = null;
			return false;
		}

		public List<Sprite> sprites;
	}

	class SpriteSheetResolver : IValueResolver
	{
		private TextureImporter spriteSheetImporter;
		
		public SpriteSheetResolver(TextureImporter spriteSheetImporter)
		{
			this.spriteSheetImporter = spriteSheetImporter;
		}

		public bool TryGetValue(IExportContext ctx, object _, MemberInfo __, ref object value)
		{
			if (!(value is TextureImporter textureImporter) || textureImporter != spriteSheetImporter) return false;
			
			var sheet = new SpriteSheet();
			var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteSheetImporter.assetPath).OfType<Sprite>().ToList();
			sheet.sprites = sprites;

			value = sheet;
			return true;
		}
	}
}