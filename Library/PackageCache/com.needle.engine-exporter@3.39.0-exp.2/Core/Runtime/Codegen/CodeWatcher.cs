using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Codegen
{
	public class CodeWatcher : IDisposable
	{
		public static bool TryFindCodeGeneratorPath(string projectDirectory, out string compilerPath)
		{
			compilerPath = null;
			
			if (projectDirectory != null)
			{
				// First check the project node_modules directly
				compilerPath = projectDirectory + "/node_modules/" + Constants.ComponentCompilerNpmPackageName + "/src/component-compiler.js";
				if (File.Exists(compilerPath))
				{
					compilerPath = Path.GetFullPath(compilerPath);
					return true;
				}
				
				// This code path only runs when an ExportInfo exists
				// We don't care if the projectDirectory exists here.
				// If someone has a Needle Engine ExportInfo in their scene then we assume that they also want to compile ts components
				var hiddenCompilerPath = HiddenProject.ComponentCompilerPath + "/src/component-compiler.js";
				if (File.Exists(hiddenCompilerPath))
				{
					compilerPath = hiddenCompilerPath;
					return true;
				}
				HiddenProject.Initialize();
			}
            

			return false;
		}
		
		
		public string GeneratorPath => _generatorPath;
		public string CodeGenDirectory => _codeGenDirectory;

		public bool DebugCompiler;

		private static readonly string[] extensionsToWatch = { ".ts", ".js", ".tsx" };

		private static ComponentGeneratorRunner _runner;
		private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
		private readonly string _codeGenDirectory;
		private string _generatorPath;
		private string _generatorDir;
		private readonly object _changedFilesLock = new object();
		private readonly List<string> _changedFiles = new List<string>();
		private readonly string seed;
		private readonly int _baseDirLength;

#if UNITY_EDITOR
		public event Action<string, bool> AfterRun;
		public event Action<string> Changed;
#endif

		public CodeWatcher(string outputDir, string seed = null, int baseDirLength = 0)
		{
#if UNITY_EDITOR
			this._codeGenDirectory = outputDir;
			this._baseDirLength = baseDirLength;
			_runner = new ComponentGeneratorRunner();
			this.seed = seed;
			UpdateGeneratorPath();
#endif
		}

		private ExportInfo exportInfo;

		internal void UpdateGeneratorPath()
		{
			if (!exportInfo) exportInfo = ExportInfo.Get();
			if (exportInfo && TryFindCodeGeneratorPath(exportInfo.GetProjectDirectory(), out var generatorPath))
			{
				this._generatorPath = Path.GetFullPath(generatorPath);
				this._generatorDir = Path.GetDirectoryName(generatorPath);
			}
		}

		public bool IsWatching(string dir)
		{
			if(_watchers.Count == 0) return false;
			foreach (var w in _watchers)
			{
				if (w.IncludeSubdirectories && dir.StartsWith(w.Path)) return true;
				if (w.Path == dir) return true;
			}
			return false;
		}

		public bool HasWatchers() => _watchers.Count > 0;

		public void BeginWatch(string path, bool includeSubdirectories = false)
		{
#if UNITY_EDITOR
			if (!Directory.Exists(path)) return;
			if (!File.Exists(_generatorPath))
			{
				// the directory might not exist when the project is not (yet) installed
				if (exportInfo) UpdateGeneratorPath();
				return;
			}
			var watcher = new FileSystemWatcher();
			_watchers.Add(watcher);
			watcher.Path = path;
			watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
			watcher.Deleted -= OnChanged;
			watcher.Created -= OnChanged;
			watcher.Changed -= OnChanged;

			watcher.Deleted += OnChanged;
			watcher.Changed += OnChanged; 
			watcher.Created += OnChanged;
			watcher.EnableRaisingEvents = true;
			// because we dont want to watch e.g. node_modules
			watcher.IncludeSubdirectories = includeSubdirectories;
			NeedleDebug.Log(TracingScenario.ComponentGeneration, $"Watching {path} for changes, includeSubdirectories: {includeSubdirectories}");
#endif
		}

		public void StopWatch()
		{
			this.Dispose();
		}

		public void RunFor(string filePath)
		{
#if UNITY_EDITOR
			InternalAdd(filePath);
#endif
		}

#if UNITY_EDITOR
		private int _changeEventId = 0;

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			// make sure the file is a ts or js file
			if (!extensionsToWatch.Any(ext => e.FullPath.EndsWith(ext)))
				return;
			
			// ignore changes in node module folder after basedir (so the base dir is allowed to be in a node_modules folder but we dont want to detect changes in node_modules)
			if (_baseDirLength > 0 && e.FullPath.Length > _baseDirLength && e.FullPath.Substring(_baseDirLength).Contains("node_modules"))
				return;
			
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Deleted:
					var fp = e.FullPath;
					

					void MainEventChanged()
					{
						Debug.Log("File deleted: " + fp);
						EditorApplication.update -= MainEventChanged;
						Changed?.Invoke(fp);
					}

					EditorApplication.update += MainEventChanged;
					// InternalEditorUtility.RepaintAllViews();
					break;

				case WatcherChangeTypes.Changed:
				case WatcherChangeTypes.Created:
					InternalAdd(e.FullPath);
					break;
			}
		}

		private async void InternalAdd(string filePath)
		{
			lock (_changedFilesLock)
			{
				if (_changedFiles.Contains(filePath)) return;
				_changedFiles.Add(filePath);
			}

			var evt = ++_changeEventId;
			await Task.Delay(300);
			if (evt != this._changeEventId) return;

			// schedule on main thread and force update
			EditorApplication.update -= ProcessChangedFiles;
			EditorApplication.update += ProcessChangedFiles;
			// InternalEditorUtility.RepaintAllViews();
		}

		private async void ProcessChangedFiles()
		{
			try
			{
				string[] files;
				lock (_changedFilesLock)
				{
					files = this._changedFiles.Distinct().ToArray();
					_changedFiles.Clear();
				}

				using (new AssemblyReloadLockScope())
				{
					foreach (var file in files)
					{
						_runner.debug = DebugCompiler;
						var res = await _runner.Run(this._generatorDir, file, this._codeGenDirectory, this.seed);
						AfterRun?.Invoke(file, res);
						if (res) Changed?.Invoke(file);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				lock (_changedFilesLock)
				{
					_changedFiles.Clear();
				}
			}
		}
#endif

		public void Dispose()
		{
			for (var index = _watchers.Count - 1; index >= 0; index--)
			{
				var watcher = _watchers[index];
				watcher.Dispose();
			}
			_watchers.Clear();
		}
	}
}