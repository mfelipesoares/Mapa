using System;

namespace Needle.Engine
{
	public class CustomComponentHeaderLinkAttribute : Attribute
	{
		public readonly string IconIconPathOrGuid;
		public readonly string Url;

		public CustomComponentHeaderLinkAttribute(string iconPathOrGuid, string url)
		{
			this.IconIconPathOrGuid = iconPathOrGuid;
			this.Url = url;
		}
	}
}