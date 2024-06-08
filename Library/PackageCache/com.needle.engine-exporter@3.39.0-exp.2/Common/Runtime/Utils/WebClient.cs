using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Needle.Engine
{
	internal class WebClientHelper
	{
		private readonly string forwardUrl;

		private string apiUrl;
		private HttpClient _client;

		private HttpClient client => _client ??= new HttpClient()
		{
			Timeout = TimeSpan.FromSeconds(3)
		};

		
		public WebClientHelper(string forwardUrl, string apiUrl = null)
		{
			this.forwardUrl = forwardUrl;
			this.apiUrl = apiUrl;
		}

		public async Task<HttpResponseMessage> SendPost(object model, string endpoint)
		{
			try
			{
				var data = JsonConvert.SerializeObject(model);
				var content = new StringContent(data);
				var url = await GetUrl(endpoint);
				var res = await client.PostAsync(url, content);
				return res;
			}
#if NEEDLE_ENGINE_DEV
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
#else
			catch (Exception)
			{
				// ignore
			}
#endif
			
			return null;
		}

		public async Task<HttpResponseMessage> SendGet(string endpoint)
		{
			try
			{
				var url = await GetUrl(endpoint);
				var res = await client.GetAsync(url);
				return res;
			}
#if NEEDLE_ENGINE_DEV
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
#else
			catch (Exception ex)
			{
				NeedleDebug.LogException(TracingScenario.NetworkRequests, ex);
			}
#endif
			
			return null;
		}

		private async Task<string> GetUrl(string endpoint)
		{
			var baseUrl = await GetUrl();
			if(baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
			if(!endpoint.StartsWith("/")) endpoint = "/" + endpoint;
			var url = baseUrl + endpoint;
			return url;
		}

		private async Task<string> GetUrl()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(apiUrl)) return apiUrl;
				var res = await client.GetAsync(forwardUrl);
				if (res.StatusCode == HttpStatusCode.OK)
				{
					apiUrl = await res.Content.ReadAsStringAsync();
					return apiUrl = apiUrl.Trim();
				}
			}
			catch (Exception ex)
			{
				NeedleDebug.LogException(TracingScenario.NetworkRequests, ex);
			}

			apiUrl = null;
			return null;
		}
	}
}