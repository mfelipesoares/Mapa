using Newtonsoft.Json;

namespace Needle.Engine.Serialization
{
	public class NewtonsoftSerializer : ISerializer
	{
		private readonly JsonSerializerSettings settings;
		
		public NewtonsoftSerializer(IExportContext context, IValueResolver resolver)
		{
			this.settings = NewtonsoftSettings.Create(context, resolver);
		}

		public NewtonsoftSerializer()
		{
			this.settings = NewtonsoftSettings.Create();
		}
		
		public string Serialize(object val)
		{
			return JsonConvert.SerializeObject(val, Formatting.None, settings);
		}
	}
}