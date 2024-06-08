using System;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	[DeploymentComponent]
	[HelpURL(Constants.DocumentationUrl)]
	[CustomComponentHeaderLink("a7d777a059a8ed04696b0a9e85eac629", "https://glitch.com")]
	public class DeployToGlitch : MonoBehaviour
	{
		public GlitchModel Glitch;

		// ReSharper disable once Unity.RedundantEventFunction
		private void OnEnable()
		{
			// just for editor
		}

		[ContextMenu("Open Starter Project on Glitch")]
		private void OpenGlitchStarter()
		{
			Application.OpenURL(DeployToGlitchUtils.TemplateUrl);
		}
	}
}