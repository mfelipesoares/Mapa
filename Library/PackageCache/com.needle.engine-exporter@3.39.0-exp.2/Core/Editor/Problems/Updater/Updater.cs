using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Projects;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Problems
{
	internal static class Updater
	{
		[MenuItem("CONTEXT/" + nameof(ExportInfo) + "/Update/Run Web Project Updater")]
		private static void RunUpdate_MenuItem() => RunUpdate(false);

		[InitializeOnLoadMethod]
		private static void Init()
		{
			Builder.BuildStarting += OnBuild;
			Builder.BuildEnded += OnBuildEnded;
			ProjectGenerator.BeforeInstall += (path, _) => RunForProject(path, false);
		}

		private static void OnBuild()
		{
			if (ExporterProjectSettings.instance.allowRunningProjectFixes)
				RunUpdate(true);
		}

		private static async void OnBuildEnded()
		{
			if (_assemblyLockScope == null) return;
			await Task.Delay(2000);
			var maxTime = DateTime.Now.AddSeconds(10);
			while (Actions.IsInstalling() || Actions.IsRunningBuildTask || Actions.IsStartingServerTask)
			{
				if (DateTime.Now > maxTime) break;
				await Task.Delay(1000);
			}
			_assemblyLockScope.Dispose();
		}

		private static Update[] updates;
		private static ProfilerMarker updaterMarker = new ProfilerMarker("Needle Engine Updater");
		private static bool _didRun = false;
		private static string _lastProjectPath = null;
		private static AssemblyReloadLockScope _assemblyLockScope;

		private static void RunUpdate(bool automaticTrigger)
		{
			using var profiling = updaterMarker.Auto();
			var export = ExportInfo.Get();
			if (export && export.IsValidDirectory())
			{
				var dir = export.GetProjectDirectory();

				if (automaticTrigger && dir == _lastProjectPath && _didRun) return;

				_lastProjectPath = dir;
				var fullPath = Path.GetFullPath(dir);

				if (automaticTrigger)
				{
					_assemblyLockScope?.Dispose();
					_assemblyLockScope = new AssemblyReloadLockScope();
				}
				else Debug.Log("Running updater for project: " + fullPath);

				var force = !automaticTrigger;
				RunForProject(fullPath, force);

				_didRun = true;
				
				if(!automaticTrigger) Debug.Log("Finished running updater for project: " + fullPath);
			}
		}

		public static void RunForProject(string projectDirectory, bool force)
		{
			projectDirectory = Path.GetFullPath(projectDirectory);
			
			if (!Directory.Exists(projectDirectory)) return;
			
			updates ??= InstanceCreatorUtil.CreateCollectionSortedByPriority<Update>()
				.OrderBy(u => u.UpgradeDate)
				.ToArray();
			
			var codeUpdater = new CodeUpdateHelper(projectDirectory);
			foreach (var update in updates)
			{
				if (!force)
				{
					if (update.DidRun && update.RunOnce) continue;
				}
				update.Apply(projectDirectory, codeUpdater);
			}
			codeUpdater.Apply();
		}
	}
}