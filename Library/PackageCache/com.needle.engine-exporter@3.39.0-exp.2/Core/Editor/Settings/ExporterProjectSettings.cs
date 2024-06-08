using System;
using System.Linq;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Settings
{
	[FilePath("ProjectSettings/NeedleExporterSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class ExporterProjectSettings : ScriptableSingleton<ExporterProjectSettings>
	{
		public bool overrideEnterPlaymode = true;
		// public bool overrideBuildSettings = true;
		public bool smartExport = false;
		public bool debugMode = false;
		public bool generateReport = false;
		public bool allowRunningProjectFixes = true;
		public string[] npmSearchPathDirectories = Array.Empty<string>();
		public bool useHotReload = true;

		internal void Save()
		{
			Undo.RegisterCompleteObjectUndo(this, "Save Needle Exporter Settings");
			base.Save(true);
		}

		private static SerializedProperty _npmSearchPathDirectoriesProperty;
		internal static SerializedProperty NpmSearchPathDirectoriesProperty
		{
			get
			{
				if (_npmSearchPathDirectoriesProperty == null)
				{
					var serializedObject = new SerializedObject(instance);
					// instance.hideFlags &= ~HideFlags.NotEditable;
					_npmSearchPathDirectoriesProperty = serializedObject.FindProperty(nameof(npmSearchPathDirectories));
				}
				return _npmSearchPathDirectoriesProperty;
			}
		}

		internal static void TryAddNpmSearchPath()
		{
			var path = NpmUtils.TryFindNvmInstallDirectory();
			if (path != null && !instance.npmSearchPathDirectories.Contains(path))
			{
				Debug.Log("Add npm search path: " + path);
				var list = instance.npmSearchPathDirectories.ToList();
				list.Add(path);
				instance.npmSearchPathDirectories = list.ToArray();
			}
		}

		[InitializeOnLoadMethod]
		private static void Init()
		{
			ProcessHelper.GetAdditionalNpmSearchPaths += separator =>
			{
				if (instance.npmSearchPathDirectories.Length <= 0)
				{
					TryAddNpmSearchPath();
				}
				return string.Join(separator, instance.npmSearchPathDirectories.Where(e => !string.IsNullOrWhiteSpace(e)));
			};
		}
	}
}