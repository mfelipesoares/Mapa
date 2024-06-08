using System;
using UnityEngine;

namespace Needle.Engine.Components
{
	[Obsolete]
	[HelpURL(Constants.DocumentationUrl)]
	public class TestSimulateUserData : MonoBehaviour
	{
		public int transformsPerFrame = 10;
		public int interval = 0;
		public bool useFlatbuffers = true;

		private void OnEnable()
		{
			
		}
	}
}