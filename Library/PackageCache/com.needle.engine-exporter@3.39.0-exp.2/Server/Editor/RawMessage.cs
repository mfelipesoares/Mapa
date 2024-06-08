using Newtonsoft.Json.Linq;

namespace Needle.Engine.Server
{
	public struct RawMessage
	{
		public string type;
		public JToken data;
	}
}