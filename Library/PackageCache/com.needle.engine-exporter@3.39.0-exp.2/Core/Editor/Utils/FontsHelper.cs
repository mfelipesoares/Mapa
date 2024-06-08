using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Utils
{
	public static class FontsHelper
	{
		public static void ClearCache()
		{
			exportedFontPaths.Clear();
		}

		private static string[] osFontPaths = null;

		private static readonly Dictionary<(Font, string, string), string> exportedFontPaths =
			new Dictionary<(Font, string, string), string>();

		private static readonly Dictionary<Font, Task> generateTasks = new Dictionary<Font, Task>();

		public static string TryGenerateRuntimeFont(Font font,
			FontStyle style,
			string targetDirectory,
			bool force = false,
			object owner = null)
		{
			SelectStyleFromFontName(font, ref style);
			var res = InternalGenerateRuntimeFont(font, style, targetDirectory, out var generatedFont, force, owner);
			// If the font atlas was NOT generated we assume it ran already before
			// we can then exit early and skip handling other styles
			if (!generatedFont) return res;

			switch (style)
			{
				case FontStyle.Normal:
					InternalGenerateRuntimeFont(font, FontStyle.Bold, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.Italic, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.BoldAndItalic, targetDirectory, out _, force, owner);
					break;
				case FontStyle.Italic:
					InternalGenerateRuntimeFont(font, FontStyle.Bold, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.Normal, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.BoldAndItalic, targetDirectory, out _, force, owner);
					break;
				case FontStyle.Bold:
					InternalGenerateRuntimeFont(font, FontStyle.Normal, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.Italic, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.BoldAndItalic, targetDirectory, out _, force, owner);
					break;
				case FontStyle.BoldAndItalic:
					InternalGenerateRuntimeFont(font, FontStyle.Normal, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.Bold, targetDirectory, out _, force, owner);
					InternalGenerateRuntimeFont(font, FontStyle.Italic, targetDirectory, out _, force, owner);
					break;
			}

			return res;
		}

		private static void SelectStyleFromFontName(Font font, ref FontStyle style)
		{
			if (font.name.EndsWith("-regular", StringComparison.OrdinalIgnoreCase))
				style = FontStyle.Normal;
			if (font.name.EndsWith("-italic", StringComparison.OrdinalIgnoreCase))
				style = FontStyle.Italic;
			else if (font.name.EndsWith("-bold", StringComparison.OrdinalIgnoreCase))
				style = FontStyle.Bold;
			else if (font.name.EndsWith("-bolditalic", StringComparison.OrdinalIgnoreCase))
				style = FontStyle.BoldAndItalic;
		}

		private static string InternalGenerateRuntimeFont(Font font,
			FontStyle style,
			string targetDirectory,
			out bool generatedFont,
			bool force = false,
			object owner = null
		)
		{
			generatedFont = false;
			font = TryFindFontAssetForStyle(font, style, out var fontPath, out var isBuiltinFont, owner);

			var chars = GetCharset(font, isBuiltinFont);
			var charsetDir = Application.dataPath + "/../Library/Needle/FontCharsets~";
			var charsHash = !string.IsNullOrEmpty(chars) ? GuidGenerator.GetGuid(chars) : "0";
			var charsetPath = $"{charsetDir}/{font.name}-{charsHash}-charset.txt";

			const string extension = "-msdf.json";

			var resultPath = targetDirectory + "/" + font.name;
			var fullResultPath = resultPath + extension;
			// Another check if this font file already exists
			if (!force && File.Exists(fullResultPath) && File.Exists(charsetPath))
			{
				// get real name on disc with the correct casing
				var files = Directory.GetFiles(targetDirectory, font.name + "*.png");
				if (files.Length > 0)
				{
					var fileInfo = new FileInfo(files[0]);
					return $"{fileInfo.Directory!.FullName}/{Path.GetFileNameWithoutExtension(files[0])}";
				}
				return resultPath;
			}

			if (isBuiltinFont)
			{
				osFontPaths ??= Font.GetPathsToOSFonts();
				if (osFontPaths != null)
				{
					var expectedName = font.name + ".";
					fontPath = osFontPaths.FirstOrDefault(p =>
						p.IndexOf(expectedName, StringComparison.InvariantCultureIgnoreCase) > 0);
					// e.g. arial on windows path is "Fonts/arial.ttf" but the font name is "Arial"
					// we have to make sure the casing of the font name matches the file name
					var actualName = Path.GetFileNameWithoutExtension(fontPath);
					if (actualName != font.name)
					{
						resultPath = targetDirectory + "/" + actualName;
					}
				}
			}

			// Check if the font needs to be re-exported
			var key = (font, targetDirectory, charsHash);
			if (exportedFontPaths.TryGetValue(key, out var path))
			{
				// Check if a task is already running for this font
				if (generateTasks.TryGetValue(font, out var task))
				{
					if (!task.IsCompleted) return path;
					generateTasks.Remove(font);
				}

				// Check if the font exists (or when re-export is enforced remove the path from the cache)
				var filePath = path + extension;
				if (force || !File.Exists(filePath))
				{
					exportedFontPaths.Remove(key);
				}
				else // the font was already exported and still exists
					return path;
			}

			if (fontPath == null || !File.Exists(fontPath))
			{
				Debug.LogWarning("Could not find path to font: " + font.name, font);
				return null;
			}

			// var generateCharset = true;
			if (!string.IsNullOrEmpty(charsetDir) && !string.IsNullOrEmpty(charsetPath))
			{
				Directory.CreateDirectory(charsetDir);
				File.WriteAllText(charsetPath, chars);
			}

			if (font.dynamic && !isBuiltinFont)
			{
				Debug.LogWarning(
					$"Font texture for {font.name} is generated with dynamic set - this may lead to fonts having only the characters currently used in your project. If you want to provide dynamic text support you should change this setting in your font asset",
					font);
			}

			RunCommand(font, Path.GetFullPath(fontPath), targetDirectory, charsetPath);
			exportedFontPaths.Add(key, resultPath);
			generatedFont = true;

			return resultPath;
		}

		// Keep in sync with list in core engine Text.ts
		private static readonly string[] unsupportedStyleNames = new[] 
			{ "medium", "mediumitalic", "black", "blackitalic", "thin", "thinitalic", "extrabold", "light", "lightitalic", "semibold" };

		/// <summary>
		/// Resolve actual font asset and paths, given a font and a style
		/// Returned font can differ from the given font
		/// </summary>
		private static Font TryFindFontAssetForStyle(Font font,
			FontStyle style,
			out string fontPath,
			out bool isBuiltinFont,
			object context = null)
		{
			fontPath = AssetDatabase.GetAssetPath(font);
			isBuiltinFont = fontPath.EndsWith("unity default resources");
			if (isBuiltinFont)
			{
				if (font.name == "LegacyRuntime") font.name = "Arial";
			}

			// handle selected font style
			// we need to find the correct font asset for a style
			// e.g. when a Regular font is assigned but as style Bold is selected we want to find the path to the bold font asset
			switch (style)
			{
				default:
					if (!isBuiltinFont)
					{
						var styleName = style.ToString();
						
						if (style == FontStyle.BoldAndItalic) styleName = "BoldItalic";
						else if (style == FontStyle.Normal) styleName = "Regular";
			
						// check if the font is something like -Medium or -Thin or -Black
						// in this cases there's no equivalent FontStyle for Unity and we can not just use the FontStyle enum
						// instead we want to search for the font asset with the correct name
						var styleSeparator = font.name.LastIndexOf("-", StringComparison.Ordinal);
						if (styleSeparator > 0)
						{
							var styleNameInString = font.name.Substring(styleSeparator + 1);
							if (unsupportedStyleNames.Contains(styleNameInString.ToLowerInvariant()))
							{
								styleName = styleNameInString;
							}
						}

						var ext = Path.GetExtension(fontPath);
						var newPath = fontPath;
						var expectedPath = "-" + styleName + ext;
						if (!newPath.EndsWith(expectedPath))
						{
							var dashIndex = fontPath.LastIndexOf("-", StringComparison.Ordinal);
							if (dashIndex > 0)
							{
								newPath = fontPath.Substring(0, dashIndex) + "-" + styleName + ext;
							}
							else newPath = newPath.Replace(ext, expectedPath);
							if (File.Exists(newPath))
							{
								font = AssetDatabase.LoadAssetAtPath<Font>(newPath);
								fontPath = newPath;
							}
							else
							{
								if (!couldNotFindFontPaths.Contains(newPath))
								{
									couldNotFindFontPaths.Add(newPath);
									Debug.LogWarning($"Could not find font asset for style {styleName} at {newPath}",
										context as Object);
								}
							}
						}
					}
					break;
			}

			return font;
		}
		
		private static readonly HashSet<string> couldNotFindFontPaths = new HashSet<string>();

		// private static readonly Dictionary<Font, string> charset = new Dictionary<Font, string>();
		private static string GetCharset(Font font, bool isBuiltinFont)
		{
			// if (font.name == "Arial" || isBuiltinFont)
			// {
			// 	return null;
			// }
			var set = "";
			var chars = font.characterInfo;
			foreach (var info in chars)
			{
				set += (char)info.index;
				if (set.Length > 5000)
				{
					Debug.LogWarning(
						$"Font {font.name} has more than 5000 characters. This may lead to performance issues. Consider using a smaller font or a font atlas with a custom set of characters. You can configure this in the font asset in Unity.",
						font);
					break;
				}
			}

			// ensure we have the default ascii characters included if e.g. a font asset is used without any chars
			// https://www.w3schools.com/charsets/ref_html_ascii.asp
			if (chars.Length <= 0 || font.dynamic)
			{
				for (var i = 32; i < 127; i++)
				{
					set += (char)i;
				}
			}
			// if encoding fails we get a lot of ? at runtime so make sure the char is in the font texture
			set += "?";

			var additionalCharacters = Object
				.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
				.Where(x => x && x is IAdditionalFontCharacters)
				.Cast<IAdditionalFontCharacters>();
			
			foreach (var additional in additionalCharacters)
				set += additional.GetAdditionalCharacters();
			
			// ensure unique characters
			set = new string(set.Distinct().ToArray());

			return set;
		}

		private static async void RunCommand(Font font, string fontPath, string targetDirectory, string charsetPath)
		{
			if (!generateTasks.TryGetValue(font, out var task))
			{
				Debug.Log(
					$"<b>Generate font files</b> for \"{font.name}\" from {fontPath} at {targetDirectory} using chars at {charsetPath}");
				var fontTask = Tools.GenerateFonts(fontPath, targetDirectory, charsetPath);
				task = fontTask;
				BuildTaskList.SchedulePostExport(fontTask);
				// task = ProcessHelper.RunCommand(cmd, dir);
				generateTasks.Add(font, task);
				var res = await fontTask;
				if(!res) Debug.LogError($"Failed to generate font files for {font.name}");
			}
			else await task;
			if (generateTasks.ContainsKey(font))
				generateTasks.Remove(font);
		}
	}
}