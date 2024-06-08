using UnityEngine;

namespace Needle.Engine.Deployment
{
	[DeploymentComponent]
	[CustomComponentHeaderLink("40db2806f1e23784e8836db7ac5427dc", "https://docs.github.com/en/pages/getting-started-with-github-pages/about-github-pages#limits-on-use-of-github-pages")]
	public class DeployToGithubPages : MonoBehaviour
	{
		public string repositoryUrl;
	}
}