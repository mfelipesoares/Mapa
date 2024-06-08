using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Networking/VOIP" + Needle.Engine.Constants.NeedleComponentTags + " Voice Over IP")]
	[HelpURL(Constants.DocumentationUrl)]
	public class Voip : MonoBehaviour
	{
		public bool autoConnect = true;
		public bool runInBackground = true;
	}
}