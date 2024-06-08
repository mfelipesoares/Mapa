using System;

namespace Needle.Engine
{
	/// <summary>
	/// Add to a class that is a deployment component to be displayed in the Needle Engine Build window
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class DeploymentComponentAttribute : Attribute
	{
		
	}
}