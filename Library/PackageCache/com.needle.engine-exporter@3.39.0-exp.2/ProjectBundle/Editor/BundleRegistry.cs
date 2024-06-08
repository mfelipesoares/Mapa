using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Needle.Engine.Codegen;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Needle.Engine.ProjectBundle
{
	[Serializable]
	public class BundleRegistry
	{
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			var wasActive = true;
			EditorApplication.update += () =>
			{
				if (wasActive == InternalEditorUtility.isApplicationActive) return;
				wasActive = InternalEditorUtility.isApplicationActive;
				if (wasActive) Instance.EnsureWatching(true);
			};
			
			// When the editor is starting up for the first time we need to wait for it to be finished initializing
			// Otherwise AssetDB won't find out npmdef objects
			do
			{
				await Task.Delay(10);
			} while (EditorApplication.isUpdating || EditorApplication.isCompiling);
			// Ready to search for all npmdefs in the project
			TryFindNpmdefFiles();
		}

		internal static void TryFindNpmdefFiles()
		{
			var objs = AssetDatabase.FindAssets("t:" + nameof(NpmDefObject));
			foreach (var guid in objs)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				Register(path);
			}
		}
		
		public IReadOnlyList<string> FilePaths => _bundleFilePaths;
		
		private List<string> _bundleFilePaths = new List<string>();
		private List<Bundle> _bundles = null;
		[ItemCanBeNull] private List<CodeWatcher> _watchers = new List<CodeWatcher>();

		public IReadOnlyList<Bundle> Bundles
		{
			get
			{
				if (_bundles == null || isDirty) LoadBundles();
				return _bundles;
			}
		}

		public void MarkDirty() => isDirty = true;

		private bool isDirty = false;

		private void LoadBundles()
		{
			_bundles ??= new List<Bundle>();
			_bundles.Clear();
			isDirty = false;
			// Just a another check to make sure we have the npmdef files when we need them
			if (FilePaths.Count <= 0) TryFindNpmdefFiles();
			foreach (var e in FilePaths)
			{
				if (File.Exists(e))
				{
					var json = File.ReadAllText(e);
					var bundle = JsonConvert.DeserializeObject<Bundle>(json);
					if (bundle == null) continue;
					bundle.FilePath = e;
					_bundles.Add(bundle);
				} 
			}
			
			EnsureWatching(true);
		}

		private int _ensureWatcherId = 0;

		internal async void EnsureWatching(bool recreate = false)
		{
			var id = ++_ensureWatcherId;
			do
			{
				await Task.Delay(300);
			} while (EditorApplication.isCompiling || EditorApplication.isUpdating);
		
			if (id != _ensureWatcherId) return;
			
			if (_watchers.Count != FilePaths.Count || recreate)
			{
				if (_bundles == null)
				{
					LoadBundles();
					if (_bundles == null) return;
				}
				foreach(var watcher in _watchers) watcher?.Dispose();
				_watchers.Clear();
				GC.Collect();
				var watchersToStart = new List<(Bundle bundle, CodeWatcher watcher)>();
				for (var index = 0; index < FilePaths.Count; index++)
				{
					if (index >= _bundles.Count) break;
					var e = FilePaths[index];
					var bundle = _bundles[index];
					var guid = AssetDatabase.GUIDFromAssetPath(e).ToString();
					if (bundle.IsMutable())
					{
						var watcher = new CodeWatcher(bundle.FindScriptGenDirectory(), guid, bundle.PackageDirectory.Length);
						_watchers.Add(watcher);
						watchersToStart.Add((bundle, watcher));
						watcher.Changed += file =>
						{
							BundleImporter.MarkDirty(bundle);
							AssetDatabase.Refresh();
						};
					}
					else
					{
						_watchers.Add(null);
					}
				}
				await Task.Run(() =>
				{
					foreach (var kvp in watchersToStart)
					{
						if (_ensureWatcherId != id) break;
						// On windows we can watch all directories with individual watchers. On mac we can only watch the root directory
						// if we watch the root for each on windows Unity freezes.
						// We might want to refactor this and only start watching if the npmdef was selected at least once in the session
#if UNITY_EDITOR_WIN
						foreach (var dir in kvp.bundle.EnumerateDirectories())
						{
							kvp.watcher.BeginWatch(dir, false);
						}
#else
						kvp.watcher.BeginWatch(kvp.bundle.PackageDirectory, true);
#endif
					}
				});
			}
		}
		
		internal CodeWatcher TryFindWatcher(Bundle bundle)
		{
			for (var i = 0; i < Bundles.Count; i++)
			{
				if (i >= _watchers.Count) break;
				if(Bundles[i]?.FilePath == bundle.FilePath)
				{
					return _watchers[i];
				}
			}
			return null;
		}

		internal void RunCodeGenForBundle(Bundle bundle)
		{
			for (var i = 0; i < Bundles.Count; i++)
			{
				if (i >= _watchers.Count) break;
				if(Bundles[i]?.FilePath == bundle.FilePath)
				{
					var watcher = _watchers[i];
					if (watcher == null) continue;
					var list = new List<ImportInfo>();
					bundle.FindImports(list, null);
					foreach (var type in list)
					{
						watcher.RunFor(type.FilePath);
					}
					break;
				}
			}
		}

		internal bool RunCodeGen(string filePath)
		{
			for (var index = 0; index < Bundles.Count; index++)
			{
				var bundle = Bundles[index];
				if (index >= _watchers.Count) break;
				if (bundle.PackageDirectory.StartsWith(filePath))
				{
					var watcher = _watchers[index];
					watcher?.RunFor(filePath);
					return true;
				}
			}
			return false;
		}

		public static bool HasNpmDef(string packageName)
		{
			foreach (var bundle in Instance.Bundles)
			{
				var name = bundle.FindPackageName();
				if (name == packageName) return true;
			}
			return false;
		}

		public static bool TryGetBundle(string packageName, out Bundle npmdef)
		{
			foreach (var bundle in Instance.Bundles)
			{
				if (bundle == null) continue;
				var name = bundle.FindPackageName();
				if (name == packageName)
				{
					npmdef = bundle;
					return true;
				}
			}
			npmdef = null;
			return false;
		}

		public static bool IsRegistered(string path)
		{
			return Instance._bundleFilePaths.Contains(path);
		}

		public static bool Register(string path)
		{
			var fi = new FileInfo(path);
			if (!fi.Exists) return false;
			if (Instance._bundleFilePaths == null) Instance._bundleFilePaths = new List<string>();
			if (!Instance._bundleFilePaths.Contains(path))
			{
				Instance.isDirty = true;
				Instance._bundleFilePaths.Add(path);
			}
			return true;
		}

		private static BundleRegistry _instance;
		public static BundleRegistry Instance
		{
			get
			{
				if (_instance != null)
					return _instance;
				_instance = new BundleRegistry();
				return _instance;
			}
		}
	}
}