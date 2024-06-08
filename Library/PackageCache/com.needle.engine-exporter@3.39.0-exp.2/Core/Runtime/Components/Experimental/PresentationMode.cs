using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Presentation Mode" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class PresentationMode : MonoBehaviour
	{
		public KeyCode toggleKey = KeyCode.P;

	}
}