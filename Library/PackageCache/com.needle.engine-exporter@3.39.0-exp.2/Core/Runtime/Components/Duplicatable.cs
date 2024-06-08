using UnityEngine;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Duplicatable" + Needle.Engine.Constants.NeedleComponentTags)]
	[HelpURL(Constants.DocumentationUrl)]
	public class Duplicatable : MonoBehaviour
	{
		public Transform parent;
		public Transform @object;
	}
}