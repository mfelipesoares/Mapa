using UnityEngine;

namespace Needle.Engine.Components
{
	public enum OpenURLMode
	{
		NewTab,
		SameTab,
		NewWindow
	}

	[AddComponentMenu("Needle Engine/Open URL" + Needle.Engine.Constants.NeedleComponentTags)]
	public class OpenURL : MonoBehaviour
	{
		public bool clickable = true;
		[Info("The url can be a website URL or an email address")]
		public string url = "https://needle.tools";
		public OpenURLMode mode;
		public void Open() {}
	}
}