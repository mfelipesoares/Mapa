using UnityEngine;

namespace Needle.Engine.Deployment
{
	[DeploymentComponent]
	[HelpURL(Constants.DocumentationUrl)]
	[CustomComponentHeaderLink("dce2961da8fdff1419da926c4254ab9f", "https://netlify.com")]
	public class DeployToNetlify : MonoBehaviour
	{
		public string siteName;
	}
}