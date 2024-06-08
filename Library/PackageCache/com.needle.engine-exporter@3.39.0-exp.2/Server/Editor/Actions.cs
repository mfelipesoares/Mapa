
namespace Needle.Engine.Server
{
	public static class Actions
	{
		public static bool IsConnected => Connection.Instance.IsConnected;
		
		public static bool RequestSoftServerRestart()
		{
			if (Connection.Instance.IsConnected)
			{
				Connection.Instance.SendRaw("needle:editor:restart");
				return true;
			}
			return false;
		}
	}
}