using System;
using System.Linq;
using UnityEngine;

namespace Needle.Engine
{
	public class CommandlineSettings
	{
		public static bool EulaAccepted
		{
			get
			{
				if (Application.isBatchMode)
				{
					var cliArgs = Environment.GetCommandLineArgs();
					if (cliArgs.Contains("--accept-needle-eula")) return true;
				}
				return false;
			}
		}
	}
}