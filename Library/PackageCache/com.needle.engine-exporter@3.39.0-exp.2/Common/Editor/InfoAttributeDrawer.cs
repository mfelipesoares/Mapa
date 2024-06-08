using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
	[CustomPropertyDrawer(typeof(InfoAttribute))]
	public class InfoAttributeDrawer : DecoratorDrawer
	{
		private static GUIStyle _richInfoStyle, _fallback;
		private static GUIStyle richInfoStyle {
			get
			{
				if (_richInfoStyle != null) return _richInfoStyle;
				try
				{
					_richInfoStyle = new GUIStyle(EditorStyles.helpBox);
					_richInfoStyle.richText = true;
					return _richInfoStyle;
				}
				catch (Exception)
				{
					// ignored
					return _fallback ??= new GUIStyle();
				}
			}
		}
		
		private bool ShouldBeDisplayed()
		{
			var hasAnyComponent = false;
			var attrib = attribute as InfoAttribute;
			if (attrib != null && attrib.hideIfAnyComponentExists != null)
			{
				foreach (var component in attrib.hideIfAnyComponentExists)
				{
					if (component == null) continue;
					if (Object.FindAnyObjectByType(component) != null)
					{
						hasAnyComponent = true;
						break;
					}
				}
			}
			return !hasAnyComponent;
		}

		public override float GetHeight()
		{
			if (!ShouldBeDisplayed()) return 0;
			
			var info = (attribute as InfoAttribute);
			if (info == null || string.IsNullOrEmpty(info.message)) return 0;
			var content = new GUIContent(info.message);
			var height = richInfoStyle.CalcHeight(content, EditorAccess.contextWidth);
			height += 4;
			return height;
		}

		public override void OnGUI(Rect position)
		{
			if (position.height <= 0) return;
			var info = attribute as InfoAttribute;
			if (info == null) return;
			
			if (!string.IsNullOrEmpty(info.message)) 
				DrawHelpBox(ref position, info);
		}

		private static void DrawHelpBox(ref Rect position, InfoAttribute info)
		{
			position.y += 1;
			position.height -= 4;
			GUI.Label(position, EditorGUIUtility.TrTextContentWithIcon(info.message, (MessageType)info.type), richInfoStyle);
		}
	}
}