using System;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Threading.Tasks;
using Needle.Engine.Core;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine.Deployment
{
	public class BuildWindowDeployOptions : INeedleBuildPlatformGUIProvider
	{
		private static Type[] deploymentComponents;
		private static Texture2D[] deploymentComponentIcons;

		public void OnGUI(NeedleEngineBuildOptions options)
		{
			using (new EditorGUILayout.VerticalScope())
			{
				var main = EditorStyles.wordWrappedLabel;

				deploymentComponents = TypeCache.GetTypesWithAttribute<DeploymentComponentAttribute>()
					.Where(t => typeof(MonoBehaviour).IsAssignableFrom(t)).OrderBy(t => t.Name).ToArray();
				if (deploymentComponentIcons == null || deploymentComponentIcons.Length != deploymentComponents.Length)
				{
					deploymentComponentIcons = new Texture2D[deploymentComponents.Length];
					for (var k = 0; k < deploymentComponents.Length; k++)
					{
						var type = deploymentComponents[k];
						if (type.GetCustomAttributes(typeof(CustomComponentHeaderLinkAttribute), true)
							    .FirstOrDefault() is CustomComponentHeaderLinkAttribute comp && !string.IsNullOrEmpty(comp.IconIconPathOrGuid))
						{
							var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(comp.IconIconPathOrGuid));
							deploymentComponentIcons[k] = icon;
						}
					}
				}


				GUILayout.Label(new GUIContent("Add Deployment Components", "Click any of the buttons below to add the deployment component to your scene or (if it already exists) ping the object"), EditorStyles.boldLabel);
				GUILayout.Space(6);
				EditorGUILayout.BeginHorizontal();
				var i = 0;
				const int leftColumnWidth = 200;
				var maxWidthPerButton = (Screen.width - leftColumnWidth) / 3.2f;
#if UNITY_EDITOR_OSX
				// TODO: test on windows / change how we calculate the max width of a button here
				if(Screen.dpi > 0) maxWidthPerButton = Mathf.Min(maxWidthPerButton, Screen.dpi * 0.6f);
#endif
				var width = GUILayout.MaxWidth(maxWidthPerButton);
				var height = GUILayout.Height(18);
				for (var index = 0; index < deploymentComponents.Length; index++)
				{
					var type = deploymentComponents[index];
					var icon = deploymentComponentIcons[index];

					if (i++ >= 3)
					{
						i = 0;
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
					}
					var labelText = ObjectNames.NicifyVariableName(type.Name);
					if (labelText.StartsWith("Deploy To")) labelText = labelText.Substring("Deploy To".Length);
					var label = new GUIContent( labelText, icon, "Click to add the '" + type.Name + "' component to your scene");
					if (GUILayout.Button(label, width, height))
					{
						var existing = Object.FindAnyObjectByType(type);
						if (existing)
						{
							EditorGUIUtility.PingObject(existing);
							Selection.activeObject = existing;
						}
						else
						{
							var exp = ExportInfo.Get();
							if (exp)
							{
								var gameObject = exp.gameObject;
								EditorGUIUtility.PingObject(exp);
								Selection.activeObject = gameObject;
								Debug.Log("Add " + type.Name + " component to " + gameObject, gameObject);
								AddDelayed();

								async void AddDelayed()
								{
									await Task.Delay(300);
									Undo.AddComponent(exp.gameObject, type);
								}
							}
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				
				GUILayout.Space(5);
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(
						new GUIContent(
							"Learn more about all our deployment options by visiting the Needle Engine documentation."), main);
					if (GUILayout.Button("Open Documentation ↗"))
						Help.BrowseURL(Constants.DocumentationUrlDeployment);
				}

			}
		}
	}
}