using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Needle.Engine.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Needle.Engine.Codegen
{
	internal struct ComponentGenerationInfo
	{
		public string SourceDirectory;
		public string TargetDirectory;
		public bool IncludeSubDirectories;
	}

	internal struct FileChangedInfo
	{
		public string Path;
		public WatcherChangeTypes ChangeType;
	}
	
	[RequireComponent(typeof(ExportInfo))]
	[ExecuteAlways]
	[HelpURL(Constants.DocumentationComponentGenerator)]
	public class ComponentGenerator : MonoBehaviour
	{
		[HideInInspector] public string compilerDirectory = "";

		public bool Debug;

#if UNITY_EDITOR
		private const string compilerFileName = "component-compiler.js";
		public string FullCompilerPath => compilerDirectory + "/" + compilerFileName;

		public bool FileWatcherIsActive => watcher?.HasWatchers() == true;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			var comp = Object.FindAnyObjectByType<ComponentGenerator>();
			if (comp)
			{
				comp.UpdateWatcher();
				CheckThatWatcherIsRunning();
				async void CheckThatWatcherIsRunning()
				{
					var lastUpdate = DateTime.Now;
					var lastProjectDirectory = "";
					var editorFocus = false;
					while (true)
					{
						if (!comp) return;
						if (comp.watcher == null || comp.watcher.HasWatchers() == false) 
							comp.UpdateWatcher();
						else if ((DateTime.Now - lastUpdate).TotalSeconds > 20)
						{
							// Make sure we update the watcher when the project directory changes
							var exportInfo = ExportInfo.Get();
							if (exportInfo && exportInfo.DirectoryName != lastProjectDirectory)
							{
								lastUpdate = DateTime.Now;
								comp.UpdateWatcher();
							}
						}
						#if UNITY_EDITOR
						else if (InternalEditorUtility.isApplicationActive != editorFocus) 
							comp.UpdateWatcher();
						editorFocus = InternalEditorUtility.isApplicationActive;
						#endif
						await Task.Delay(2000);
					}
				}
			}
		}

		// ReSharper disable once Unity.RedundantEventFunction
		private void OnEnable()
		{
			// to be able to disable this component
			OnValidate();
		}

		private void OnDisable()
		{
			StopWatching();
		}

		private void OnValidate()
		{
			if (!this) return;
			watcher?.Dispose();
			watcher = null;
			this.UpdateWatcher();
		}

		private readonly ProjectScriptDirectoryProvider scriptsPathProvider = new ProjectScriptDirectoryProvider();
		// internal string[] WatchedDirectories { get; } = Array.Empty<string>();
		private CodeWatcher watcher;


		private void StopWatching()
		{
			watcher?.StopWatch();
		}
		
		internal void UpdateWatcher()
		{
			if(Debug) UnityEngine.Debug.Log("UPDATE SRC/SCRIPTS WATCHER");
			watcher?.StopWatch();
			var dir = scriptsPathProvider.GetScriptsDirectory(this);
			if (dir != null)
			{
				watcher ??= new CodeWatcher("Assets/Needle/Components.codegen");
				watcher.UpdateGeneratorPath();
				watcher.BeginWatch(dir, true);
			}
			if(watcher != null)
				watcher.DebugCompiler = Debug;
			// TODO: currently components generator does not delete old generated components
		}

		internal bool TryFindCompilerDirectory(Dictionary<string, string> deps)
		{
			if (Directory.Exists(compilerDirectory))
			{
				return true;
			}
			if (deps != null)
			{
				var exp = ExportInfo.Get(true);
				if (!exp) return false;
				
				if (deps.TryGetValue("@needle-tools/needle-component-compiler", out var val))
				{
					if (PackageUtils.TryGetPath( exp.GetProjectDirectory(), val, out var fullPath))
					{
						compilerDirectory = fullPath + "/src";
						return true;
					}
				}
				
				// fallback to needle engine compiler installation
				compilerDirectory = $"{exp.GetProjectDirectory()}/node_modules/@needle-tools/needle-component-compiler/src";
				return true;
			}
			return false;
		}

#endif
	}
}