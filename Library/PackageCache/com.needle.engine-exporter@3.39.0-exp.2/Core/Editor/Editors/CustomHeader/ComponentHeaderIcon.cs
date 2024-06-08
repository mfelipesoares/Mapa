using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors.CustomHeader
{
	public class ComponentHeaderIcon : IComponentHeaderDrawer
	{
		public string Tooltip;
		public Func<Editor, bool> BeforeCreate;
		public event Action<Editor, VisualElement> Created;
		
		private readonly Type type;
		private readonly string guid;
		private readonly string url;
		private readonly Action action;

		public static ComponentHeaderIcon Register(Type type, string iconGuid, string url)
		{
			var icon = new ComponentHeaderIcon(type, iconGuid, url, null);
			ComponentHeaderLinks.Register(icon);
			return icon;
		}

		public static ComponentHeaderIcon Register(Type type, string iconGuid, Action action)
		{
			var icon = new ComponentHeaderIcon(type, iconGuid, null, action);
			ComponentHeaderLinks.Register(icon);
			return icon;	
		}

		private ComponentHeaderIcon(Type type, string guid, string url, Action action = null)
		{
			this.type = type;
			this.guid = guid;
			this.url = url;
		}
		
		public VisualElement CreateVisualElement(Editor editor)
		{
			if (type.IsInstanceOfType(editor.target) == false)
				return null;

			if (BeforeCreate != null && BeforeCreate?.Invoke(editor) == false)
				return null;
			
			var path = AssetDatabase.GUIDToAssetPath(this.guid);
			if (string.IsNullOrEmpty(path)) return null;
			var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			if (!logo) return null;
		
			var img = new Image();
			img.image = logo;
			img.style.width = 16;
			img.tooltip = this.Tooltip;
			img.scaleMode = ScaleMode.ScaleToFit;
			img.AddManipulator(new Clickable(() =>
			{
				if(action != null) action();
				else if(!string.IsNullOrEmpty(url)) Application.OpenURL(this.url);
			}));
			Created?.Invoke(editor, img);
			return img;
		}
	}
}