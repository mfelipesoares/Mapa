using Needle.Engine.Editors;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.EditorSync
{
	internal static class ExportInfoHook
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			ExportInfoEditor.LateInspectorGUI += OnExportInfoGUI;
		}

		private static void OnExportInfoGUI(ExportInfo obj)
		{
			// if (!obj.IsValidDirectory()) return;
			// // Draw UI if no sync button exists
			// if (!SyncSettings.NeedleEditorSync)
			// {
			// 	if (GUILayout.Button("Add EditorSync", GUILayout.Height(32)))
			// 	{
			// 		obj.gameObject.AddComponent<NeedleEditorSync>();
			// 	}
			// }
		}
	}
}