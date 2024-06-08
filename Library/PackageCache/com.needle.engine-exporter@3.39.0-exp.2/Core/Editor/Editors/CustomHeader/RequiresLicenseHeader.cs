using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors
{
	public class RequiresLicenseComponentHeader : IComponentHeaderDrawer
	{
		private readonly Type type;
		private readonly LicenseType requiredLicenseType;
		private readonly RequireLicenseAttribute hasValidLicense;

		public RequiresLicenseComponentHeader(Type type,
			LicenseType licenseType,
			RequireLicenseAttribute hasValidLicense)
		{
			this.type = type;
			this.requiredLicenseType = licenseType;
			this.hasValidLicense = hasValidLicense;
		}

		public VisualElement CreateVisualElement(Editor editor)
		{
			if (type.IsInstanceOfType(editor.target) == false)
				return null;

			if (hasValidLicense.HasValidLicense())
				return null;

			var el = new VisualElement();
			el.style.marginTop = -1;
			el.style.display = DisplayStyle.Flex;
			el.style.alignItems = Align.Center;
			el.style.flexDirection = FlexDirection.Row;
			el.style.justifyContent = Justify.Center;
			if (hasValidLicense.Tooltip == null)
				el.tooltip =
					"Certain features require a commercial license. Click to find commercial plans at https://needle.tools/pricing";
			else el.tooltip = hasValidLicense.Tooltip;

			var label = new Label("Requires License");
			label.style.opacity = .8f;
			label.AddManipulator(new Clickable(() => { Application.OpenURL("https://needle.tools/pricing"); }));
			label.style.fontSize = 10;
			el.Add(label);


			return el;
		}
	}
}