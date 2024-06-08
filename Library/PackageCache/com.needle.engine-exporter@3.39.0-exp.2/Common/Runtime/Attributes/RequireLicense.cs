using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	/// <summary>
	/// Add to fields to disable without valide license
	/// Add to class to show a hint in the header of the component that a license is required
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
	public class RequireLicenseAttribute : PropertyAttribute
	{
		internal static LicenseType CurrentLicenseType;
		internal readonly LicenseType Type;

		public readonly string Header = null;
		public readonly string Tooltip = null;

		public RequireLicenseAttribute(LicenseType type, string tooltip = null, string header = null)
		{
			Type = type;
			Tooltip = tooltip;
			Header = header;
		}

		public bool HasValidLicense()
		{
			var minimum = (int)Type;
			var current = (int)CurrentLicenseType;
			return current >= minimum;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(RequireLicenseAttribute))]
	public class RequireLicenseDrawer : PropertyDrawer
	{
		private float headerHeight = 0;
		private float marginTop = 0;
		private float headerMarginBottom = 0;
		private static int stackLevel = 0;
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var attr = (RequireLicenseAttribute) attribute;
			var height = EditorGUI.GetPropertyHeight(property, label, true);
				
			marginTop = 0;
			headerHeight = 0;
			headerMarginBottom = 0;

			if (attr.HasValidLicense() == false)
			{
				if (!string.IsNullOrWhiteSpace(attr.Header))
				{
					marginTop = 3;
					headerMarginBottom = 3;
					headerHeight += GetCustomHeaderHeight();
				}
				height += headerHeight + marginTop + headerMarginBottom;
			}
			
			return height;
		}

		private float GetCustomHeaderHeight()
		{
			return 30;
		}

		private GUIStyle learnMoreStyle;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var enabled = GUI.enabled;
			var attr = (RequireLicenseAttribute) attribute;
			var licenseType = attr.Type;
			var licenseStr = licenseType.ToString().ToUpper();
			
			try
			{
				stackLevel += 1;
				if (attr.HasValidLicense() == false)
				{
					position.y += marginTop;
					
					if (!string.IsNullOrWhiteSpace(attr.Header))
					{
						var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
							AssetDatabase.GUIDToAssetPath("39a802f6842d896498768ef6444afe6f"));
						var logoRect = new Rect(position);
						logoRect.width = 28;
						logoRect.height = logoRect.width * logo.height / logo.width;
						GUI.DrawTexture(logoRect, logo);
						
						var labelRect = new Rect(position);
						labelRect.x = logoRect.xMax + 5;
						labelRect.height = EditorGUIUtility.singleLineHeight;
						GUI.Label(labelRect, attr.Header);
						
						var learnMoreRect = new Rect(labelRect);
						learnMoreRect.y = labelRect.y + 14;
						var learnMore = new GUIContent("Learn more", "Opens the Needle Engine pricing page");
						learnMoreRect.width = EditorStyles.label.CalcSize(learnMore).x;
						if (learnMoreStyle == null)
						{
							learnMoreStyle = new GUIStyle(EditorStyles.label);
							learnMoreStyle.normal.textColor = new Color(200, 200, 200, .5f);
						}
						if (GUI.Button(learnMoreRect, learnMore, learnMoreStyle))
						{
							Application.OpenURL("https://needle.tools/pricing");
						}
						EditorGUIUtility.AddCursorRect(learnMoreRect, MouseCursor.Link);

						position.y += headerMarginBottom;
					}

					GUI.enabled = false;
					var tooltip = $"This field requires a Needle Engine {licenseStr} license";
					if (attr.Tooltip != null) tooltip = attr.Tooltip;

					if (string.IsNullOrEmpty(label.tooltip))
					{
						label.tooltip = tooltip;
					}
					else
					{
						label.tooltip = tooltip + "\n" + label.tooltip;
					}
					// label.text += " *";
				}

				position.y += headerHeight;
				position.height -= headerHeight;
				EditorGUI.PropertyField(position, property, label, true);
			}
			finally
			{
				stackLevel -= 1;
				GUI.enabled = enabled;
			}
		}
	}
#endif
}