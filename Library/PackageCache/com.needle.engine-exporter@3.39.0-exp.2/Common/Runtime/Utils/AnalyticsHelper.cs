using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Needle.Engine.Utils
{
	internal static class AnalyticsHelper
	{
		private const string forwardEndpoint = "https://urls.needle.tools/analytics-endpoint-v2";

		// defer creation of helper to avoid HTTPClient error in Unity, see NE-3895
		private static WebClientHelper _helper;

#if NEEDLE_ENGINE_DEV
		internal static WebClientHelper Api => _helper ??= new WebClientHelper("", "http://localhost:3000");
#else
		internal static WebClientHelper Api => _helper ??= new WebClientHelper(forwardEndpoint);
#endif

		internal static async Task<bool> HasAllowedContact(string email)
		{
			try
			{
				if(string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return false;
				var encodedEmail = WebUtility.UrlEncode(email);
				var endpoint = "/api/v2/get/automatic-email/has-permission?email=" + encodedEmail;
				var res = await Api.SendGet(endpoint);
				if (res.IsSuccessStatusCode) return true;
				// a status of 403 means the user has not allowed contact
				if (res.StatusCode == HttpStatusCode.Forbidden) return false;
				// if we still have somehow an unauthorized status, we assume the user has not allowed contact
				if (res.StatusCode == HttpStatusCode.Unauthorized) return true;
				// otherwise we assume the user has not yet allowed contact
				return false;
			}
#if NEEDLE_ENGINE_DEV
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
#else
			catch (Exception)
			{
				// ignore
			}
#endif
			// fallback to true because we don't want this to block users from exporting
			return true;
		}

		internal static async Task UpdateAllowContact(string email, bool allow)
		{
			try
			{
				var encodedEmail = WebUtility.UrlEncode(email);
				var endpoint =
					$"/api/v2/get/automatic-email/update-permission?email={encodedEmail}&permission={(allow ? "1" : "0")}";
				await Api.SendGet(endpoint);
			}
#if NEEDLE_ENGINE_DEV
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
#else
			catch (Exception)
			{
				// ignore
			}
#endif
		}

		internal static async void SendDeploy(string url, bool devBuild)
		{
			try
			{
				var endpoint = "/api/v2/new/deployment";
				await Api.SendPost(new NewDeploymentModel(url, devBuild), endpoint);
			}
#if NEEDLE_ENGINE_DEV
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
#else
			catch (Exception)
			{
				// ignore
			}
#endif
		}

		private static HttpClient client;
		private static string cachedExternalIp;

		internal static async Task<string> GetExternalIpAddressAsync()
		{
			if (cachedExternalIp != null) return cachedExternalIp;
			try
			{
				client ??= new HttpClient();

				var externalIpString = await client.GetStringAsync("https://api.ipify.org");
				externalIpString = externalIpString
					// .Replace("Current IP Address: ", "")
					.Replace("<br/>", "")
					.Replace("\n", "")
					.Trim();
				;

				cachedExternalIp = externalIpString;
			}
			catch
			{
				cachedExternalIp = "unknown";
			}
			return cachedExternalIp;
		}

		internal static string ExternalIpAddress
		{
			get
			{
				if (cachedExternalIp == null)
					AsyncHelper.RunSync(GetExternalIpAddressAsync);
				return cachedExternalIp;
			}
		}

		internal static string GetIpAddress()
		{
			try
			{
				var strHostName = Dns.GetHostName();
				var ipEntry = Dns.GetHostEntry(strHostName);
				var addr = ipEntry.AddressList;
				return addr[addr.Length - 1].ToString();
			}
			catch (Exception)
			{
				return "";
			}
		}

		internal static string GetUserName()
		{
#if UNITY_EDITOR_WIN
			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var docs = new DirectoryInfo(folderPath);
			return docs.Parent?.Name ?? Environment.UserName;
#else
			return Environment.UserName;
#endif
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			await GetExternalIpAddressAsync();
		}
#endif
	}
}