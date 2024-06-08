using System.Reflection;
using UnityEditor;

namespace Needle.Engine.Editors.CustomHeader
{
	internal static class RegisterCustomHeaders
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			// ComponentHeaderIcon.Register(typeof(ExportInfo), "39a802f6842d896498768ef6444afe6f", "https://needle.tools")
			// 	.Tooltip = "Open needle.tools website";
			
			foreach (var type in TypeCache.GetTypesWithAttribute<CustomComponentHeaderLinkAttribute>())
			{
				var icon = type.GetCustomAttribute<CustomComponentHeaderLinkAttribute>();
				var guid = icon.IconIconPathOrGuid;
				if (guid.StartsWith("Assets") || guid.StartsWith("Packages"))
				{
					guid = AssetDatabase.AssetPathToGUID(guid);
				}
				ComponentHeaderIcon.Register(type, guid, icon.Url);
			}
			
			foreach (var type in TypeCache.GetTypesWithAttribute<RequireLicenseAttribute>())
			{
				var licenseAttr = type.GetCustomAttribute<RequireLicenseAttribute>();
				var header = new RequiresLicenseComponentHeader(type, licenseAttr.Type, licenseAttr);
				ComponentHeaderLinks.Register(header);
			}
		}
	}
}