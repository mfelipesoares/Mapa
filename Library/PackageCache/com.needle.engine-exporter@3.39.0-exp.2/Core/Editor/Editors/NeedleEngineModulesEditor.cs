using System;
using System.Threading.Tasks;
using Needle.Engine.Core;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Editors
{
	[CustomEditor(typeof(NeedleEngineModules))]
	public class NeedleEngineModulesEditor : Editor
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Builder.BuildEnded += OnBuildEnded;
		}

		private static void OnBuildEnded()
		{
			var settings = FindAnyObjectByType<NeedleEngineModules>();
			if (PhysicsConfig.UsePhysicsAuto() == false && (!settings || settings.PhysicsEngine == PhysicsEngine.Rapier))
			{
				Debug.LogWarning("Your scene does not seem to use physics components: consider adding the " + nameof(NeedleEngineModules) + " component to your scene to disable physics and reduce bundle size.");
			}
		}

#pragma warning disable 414
		private static bool hasChanged;
#pragma warning restore
		
		public override void OnInspectorGUI()
		{
			using (var scope = new EditorGUI.ChangeCheckScope())
			{
				base.OnInspectorGUI();
				
				var comp = (NeedleEngineModules) target;
				var msg = "";
				switch (comp.PhysicsEngine)
				{
					case PhysicsEngine.None:
						msg =
							"No physics engine will be used.\nRapier will be tree-shaked to reduce the bundle size. Certain features are not available (Colliders, Rigidbodies, fast raycasting)";
						break;
					case PhysicsEngine.Auto:
						msg =
							"The physics engine will be automatically enabled or disabled.\nThis currently only checks your main scene for Colliders or Rigidbody components to determine if physics will be enabled. If none can be found then physics will be disabled and Rapier will be tree-shaked to reduce your web app bundle size.";
						break;
					case PhysicsEngine.Rapier:
						msg =
							"Use Rapier physics engine.\nThis allows the usage of Colliders, Rigidbodies and fast raycasting but also increase your web app bundle size";
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				if(!string.IsNullOrEmpty(msg))
					EditorGUILayout.HelpBox(msg, MessageType.None);
				
				if (scope.changed)
				{
					hasChanged = true;
				}

				GUILayout.Space(10);
				var isDisabled = Server.Actions.IsConnected == false;
				using (new EditorGUI.DisabledScope(isDisabled))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();
						var tooltip = "Performs a soft restart";
						if (isDisabled) tooltip = "Server is not connected, start the server using the " + nameof(ExportInfo) + " component.";
						if (GUILayout.Button(new GUIContent("Apply and Restart Server", tooltip), GUILayout.Width(160)))
						{
							ApplyChanges();
						}
					}
				}
			}
		}

		private static async void ApplyChanges()
		{
			Debug.Log("Updating settings and restart server");
			ActionsMeta.RequestMetaUpdate();
			var i = 0;
			while (true)
			{
				await Task.Delay(300);
				// the server might not be connected so we retry a couple of times
				if (!Server.Actions.RequestSoftServerRestart())
				{
					if (i++ > 10)
					{
						Debug.LogWarning("Failed to restart server");
						return;
					}
				}
				else break;
			}
		}
	}
}