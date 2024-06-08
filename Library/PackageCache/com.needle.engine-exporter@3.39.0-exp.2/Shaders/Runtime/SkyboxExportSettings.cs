using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Shaders
{
	public class SkyboxExportSettings : MonoBehaviour, ISkyboxExportSettingsProvider
	{
		[field: SerializeField] public int SkyboxResolution { get; set; } = 256;

		[field: SerializeField, HideInInspector]
		public bool HDR { get; set; } = true;

		private void OnValidate()
		{
			SkyboxResolution = Mathf.NextPowerOfTwo(SkyboxResolution);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(SkyboxExportSettings))]
		private class SkyboxEditor : Editor
		{
			private SerializedProperty _skyboxResolution;

			public override void OnInspectorGUI()
			{
				using (var change = new EditorGUI.ChangeCheckScope())
				{
					if (_skyboxResolution == null)
						_skyboxResolution = serializedObject.FindProperty("<SkyboxResolution>k__BackingField");
					EditorGUILayout.DelayedIntField(_skyboxResolution);
					if (change.changed) serializedObject.ApplyModifiedProperties();
				}
			}
		}
#endif
	}
}