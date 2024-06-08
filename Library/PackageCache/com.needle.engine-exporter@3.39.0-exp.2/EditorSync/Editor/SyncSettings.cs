using System;
using Needle.Engine.Editors;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Needle.Engine.EditorSync
{
	internal static class SyncSettings
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorModificationListener.Enabled = Enabled;
			Selection.selectionChanged += () =>
			{
				EditorModificationListener.Enabled = Enabled;
				if (NeedleEditorSync)
				{
					EditorModificationListener.Components = NeedleEditorSync.components;
					EditorModificationListener.Materials = NeedleEditorSync.materials;
				}
			};
			NeedleEditorSync.Validate += () =>
			{
				EditorModificationListener.Enabled = Enabled;
				if (NeedleEditorSync)
				{
					EditorModificationListener.Components = NeedleEditorSync.components;
					EditorModificationListener.Materials = NeedleEditorSync.materials;
				}
			};
		}

		public static bool Enabled
		{
			get
			{
				if (NeedleEditorSync)
				{
					return NeedleEditorSync.enabled;
				}

				return false;
			}
		}

		public static bool Installed
		{
			get
			{
				if (!NeedleEditorSync) return false;
				return EditorSyncActions.CheckIsInstalled();
			}
		}

		public static bool SyncComponents => NeedleEditorSync?.components ?? true;
		public static bool SyncMaterials => NeedleEditorSync?.materials ?? true;

		// private static ExportInfo exportInfo;
		// private static DateTime _lastTimeSearchedExportInfo;
		//
		// private static ExportInfo ExportInfo
		// {
		// 	get
		// 	{
		// 		if (!exportInfo && DateTime.Now - _lastTimeSearchedExportInfo > TimeSpan.FromSeconds(5))
		// 		{
		// 			_lastTimeSearchedExportInfo = DateTime.Now;
		// 			exportInfo = ExportInfo.Get();
		// 		}
		// 		return exportInfo;
		// 	}
		// }

		private static NeedleEditorSync _needleEditorSync;
		private static DateTime _lastTimeSearchedEditor;

		public static NeedleEditorSync NeedleEditorSync
		{
			get
			{
				if (!_needleEditorSync && DateTime.Now - _lastTimeSearchedEditor > TimeSpan.FromSeconds(3))
				{
					_lastTimeSearchedEditor = DateTime.Now;
					_needleEditorSync = Object.FindAnyObjectByType<NeedleEditorSync>();
				}
				return _needleEditorSync;
			}
		}
	}
}