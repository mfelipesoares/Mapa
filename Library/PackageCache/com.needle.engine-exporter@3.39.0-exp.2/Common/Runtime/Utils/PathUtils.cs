using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Utils
{
	public static class PathUtils
	{
		// https://stackoverflow.com/a/26473940
		public static bool IsSymlinked(string path)
		{
			var pathInfo = new FileInfo(path);
			if (!pathInfo.Exists) return false;
			return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
		}

		public static string ResolveSymlink(string potentialSymlink)
		{
			var pathInfo = new FileInfo(potentialSymlink);
			if (!pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
				return pathInfo.FullName;
#if UNITY_EDITOR_WIN
			var realPath = PathUtilsWindows.GetFinalPathName(potentialSymlink);
			return realPath;
#else
			return pathInfo.FullName;
#endif
		}

		public static string GetShortDisplayPath(this string path)
		{
			var dir = new DirectoryInfo(path);
			if (dir.Exists) return dir.Parent?.Name + "/" + dir.Name;
			var file = new FileInfo(path);
			if (file.Exists) return file.Directory?.Name + "/" + file.Name;
			return path;
		}

		// https://stackoverflow.com/a/32428566
		public static string GetHomePath()
		{
			// Not in .NET 2.0
			// System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				return Environment.GetEnvironmentVariable("HOME");

			return Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
		}

		private static readonly char[] DirectorySeparators = new[] { '/', '\\' };

		public static string GetDirectoryNameSafe(string path)
		{
			while (path.EndsWith("\\") || path.EndsWith("/"))
				path = path.Substring(0, path.Length - 1);
			var slash = path.LastIndexOfAny(DirectorySeparators);
			if (slash < 0) return path;
			return path.Substring(slash + 1);
		}

		private static readonly Regex invalidFileNameChars = new Regex(@"[<>:""/\\|?*\x00-\x1F]");

		public static string ToFileName(this string path)
		{
			return invalidFileNameChars.Replace(path, " ");
		}

		public static bool IsFilePath(string path)
		{
			var isFilePath = false;
			try
			{
				var attr = File.GetAttributes(path);
				isFilePath = (attr & FileAttributes.Directory) != FileAttributes.Directory;
			}
			catch (FileNotFoundException)
			{
				// -> we cannot check if the file exists tho because it might not exist yet
				// A directory path may contain a DOT in which case the following extension check fails
				// e.g. my/path/com.my.package~ will return .package
				var isHiddenDirectory = path.EndsWith("~");
				isFilePath = !isHiddenDirectory && !string.IsNullOrEmpty(Path.GetExtension(path));
			}
			return isFilePath;
		}

		/// <summary>
		/// Relative to the Unity Project
		/// </summary>
		public static string MakeProjectRelative(string path, bool allowVirtualPaths = true)
		{
			return MakeProjectRelative(path, false, allowVirtualPaths);
		}

		/// <summary>
		/// Relative to the Unity Project
		/// </summary>
		public static string MakeProjectRelative(string path, bool encoded, bool allowVirtualPaths)
		{
			if (allowVirtualPaths && TryMakeToVirtualPath(path, out var virtualPath)) return virtualPath;
			var assetsPath = Application.dataPath;
			return MakeRelative(assetsPath, path, encoded);
		}

		private static bool TryMakeToVirtualPath(string path, out string virtualPath)
		{
			virtualPath = null;

			if (path.Contains("Assets"))
			{
				var assetsPath = Application.dataPath;
				var projectPath = Path.GetFullPath(assetsPath + "../").Replace("\\", "/");
				path = path.Replace("\\", "/");
				if (path.StartsWith(projectPath))
				{
					virtualPath = "Assets" + path.Substring(projectPath.Length);
					return true;
				}
			}
			else
			{
				// Traverse all parents trying to find a unity package
				var targetDirectory = new DirectoryInfo(path);
				var currentDirectory = targetDirectory;
				while (currentDirectory != null)
				{
					var packagePath = currentDirectory.FullName + "/package.json";
					if (File.Exists(packagePath))
					{
						var packageName = PackageUtils.GetPackageName(packagePath);
						if (!string.IsNullOrWhiteSpace(packageName))
						{
							var unityPackagePath = "Packages/" + packageName;
							if (Directory.Exists(unityPackagePath))
							{
								var packageDirectoryRelative = MakeRelative(currentDirectory.FullName + "/",
									targetDirectory.FullName, false);
								var finalPath = unityPackagePath + "/" + packageDirectoryRelative;
								virtualPath = finalPath;
								return true;
							}
						}
					}
					currentDirectory = currentDirectory.Parent;
				}
			}

			return false;
		}

		public static string MakeRelative(string relativeFrom, string path, bool encoded = false)
		{
			if (string.IsNullOrEmpty(path)) return path;

			var isFilePath = IsFilePath(path);

			// if we make a file relative we need to append a "/" to the root directory
			if (isFilePath)
			{
				if (!relativeFrom.EndsWith("/") && !relativeFrom.EndsWith("\\"))
					relativeFrom += "/";
			}
			var isHiddenDirectory = path.EndsWith("~");
			if (isHiddenDirectory) path = path.Substring(0, path.Length - 1);
			relativeFrom = Path.GetFullPath(relativeFrom);
			path = Path.GetFullPath(path);
			var res = new Uri(relativeFrom, UriKind.Absolute).MakeRelativeUri(new Uri(path)).ToString();
			if (isHiddenDirectory) res += "~";
			if (!encoded) res = res.Replace("%20", " ");
			return res;
		}

		public static string RelativeTo(this string path, string basePath, bool encoded = false)
		{
			return MakeRelative(basePath, path, encoded);
		}

#if UNITY_EDITOR
		public static string SelectPath(string title, string currentPath = null)
		{
			const string prevPathKey = "Needle_Three_PreviouslySelectedFolder";
			string folder = default;
			if (currentPath != null && Directory.Exists(currentPath)) folder = Path.GetFullPath(currentPath);
			else
			{
				folder = EditorPrefs.GetString(prevPathKey);
				if (folder == null || !Directory.Exists(folder)) folder = Application.dataPath;
			}
			var sel = EditorUtility.OpenFolderPanel(title, folder, "");
			EditorPrefs.SetString(prevPathKey, sel);
			return sel;
		}


		public static void AddContextMenu(Action<GenericMenu> onOpen)
		{
			if (Event.current.type == EventType.ContextClick)
			{
				var last = GUILayoutUtility.GetLastRect();
				if (last.Contains(Event.current.mousePosition))
				{
					var menu = new GenericMenu();
					onOpen(menu);
					menu.ShowAsContext();
				}
			}
		}
#endif
	}
}