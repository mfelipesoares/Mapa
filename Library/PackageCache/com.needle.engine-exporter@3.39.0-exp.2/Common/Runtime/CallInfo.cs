#nullable enable

using UnityEngine;
using UnityEngine.Events;

namespace Needle.Engine
{
	public class CallInfo
	{
		public readonly Object Target;
		public readonly string MethodName;
		public readonly string OriginalMethodName;
		public object? Argument;
		public readonly UnityEventCallState State;

		public CallInfo(Object target, string methodName, UnityEventCallState state, object? argument = null)
		{
			Target = target;
			State = state;
			OriginalMethodName = methodName;
			this.Argument = argument;
			if (methodName.Length > 0)
			{
				// Make the method name lowercase
				if (char.IsUpper(methodName[0])) 
					methodName = char.ToLower(methodName[0]) + methodName.Substring(1);
				// Remove property setter prefix
				if(methodName.StartsWith("set_"))
					methodName = methodName.Substring(4);
			}
			MethodName = methodName;
		}

		public override string ToString()
		{
			return Target + "." + MethodName + "(" + Argument + ")";
		}
	}

}