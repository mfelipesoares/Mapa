using System;

namespace Needle.Engine
{
	public static class ActionsMeta
	{
		public static event Action RequestUpdate;

		public static void RequestMetaUpdate()
		{
			RequestUpdate?.Invoke();
		}
	}
}