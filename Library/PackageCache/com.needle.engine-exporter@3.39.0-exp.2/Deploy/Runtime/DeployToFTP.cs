using System;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	[DeploymentComponent]
	[HelpURL("https://docs.needle.tools/deployment")]
	public class DeployToFTP : MonoBehaviour
	{
		public FTPServer FTPServer;
		public string Path = "/";
		public bool OverrideGzipCompression = true;
		public bool UseGzipCompression = false;

		private void OnValidate()
		{
			Path = Path?.Trim();
		}
	}
}