using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Needle.Engine.Utils
{
	public static class WebHelper
	{
		internal static bool HttpConnectionsAllowed
		{
#if UNITY_2022_3_OR_NEWER && UNITY_EDITOR
			get => PlayerSettings.insecureHttpOption != InsecureHttpOption.NotAllowed;
#else
			get => true;
#endif
		}

		private static DateTime _lastTimeInvalidHttpRequest = DateTime.MinValue;

		public static bool CanMakeRequest(string url)
		{
#if UNITY_EDITOR
			if (url.StartsWith("http:") && !HttpConnectionsAllowed)
			{
				NeedleDebug.LogWarning(TracingScenario.NetworkRequests,
					"HTTP requests are not allowed - will not request url: " + url);
				var now = DateTime.Now;
				if (now - _lastTimeInvalidHttpRequest > TimeSpan.FromSeconds(30))
				{
					_lastTimeInvalidHttpRequest = now;
					Debug.LogWarning(
						"HTTP requests are disabled in your Unity project. You can change this in the Player Settings under \"Other Settings\" -> \"Configuration\" -> \"Allow 'unsafe' HTTP requests\"");
				}
				return false;
			}
#endif

			return true;
		}

		public static async Task<UnityWebRequest> MakeHeaderOnlyRequest(string url,
			int timeout = 1000,
			CancellationToken cancel = default)
		{
			// Seems we may need to sanitize // in URLs since otherwise requests can fail on Mac
			// url = url.Replace("//", "/").Replace("//", "/").Replace(":/", "://");

			NeedleDebug.Log(TracingScenario.NetworkRequests, "Making header only request to " + url);

			var req = new UnityWebRequest(url, "HEAD");

			// TODO: this is in seconds but we get curl errors that we can not suppress otherwise
			req.timeout = timeout;
			req.certificateHandler = new AllowAllCertHandler();
#if UNITY_2023_1_OR_NEWER
			await req.SendWebRequest();
#else
			req.SendWebRequest();
#endif
			while (!req.isDone && !cancel.IsCancellationRequested)
			{
				NeedleDebug.Log(TracingScenario.NetworkRequests, "Waiting for header only request response...");
				await Task.Delay(30, cancel);
			}
			return req;
		}

		public static async Task<bool> IsRespondingUnityWebRequest(string url, CancellationToken cancel = default)
		{
			try
			{
				if (!CanMakeRequest(url)) return false;

				var req = new UnityWebRequest(url, "HEAD");
				// TODO: this is in seconds but we get curl errors that we can not suppress otherwise
				req.timeout = 1000;
				if (url.StartsWith("https"))
				{
					req.certificateHandler = new AllowAllCertHandler();
				}
#if UNITY_2023_1_OR_NEWER
				await req.SendWebRequest();
#else
				req.SendWebRequest();
#endif
				while (!req.isDone && !cancel.IsCancellationRequested) await Task.Delay(30, cancel);
				// it is responding so at least the website exists
				if (req.responseCode == 404) return true;
				return req.result == UnityWebRequest.Result.Success;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			catch (SocketException)
			{
				return false;
			}
			catch (Exception)
			{
				// Debug.LogException();
				return false;
			}
		}

		public static async Task<bool> IsResponding(string url, CancellationToken cancel = default)
		{
#if UNITY_EDITOR
			while (EditorApplication.isCompiling) await Task.Delay(1000);
#endif

			// TODO: test on OSX
			return await IsRespondingUnityWebRequest(url, cancel);

			// var previousCallback = ServicePointManager.ServerCertificateValidationCallback;
			// try
			// {
			// 	ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			// 	using var client = new HttpClient();
			// 	var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cancel);
			// 	var isOk = res.StatusCode == HttpStatusCode.OK || 
			// 	           res.StatusCode == HttpStatusCode.NotFound ||
			// 	           res.StatusCode == HttpStatusCode.Unauthorized ||
			// 	           res.StatusCode == HttpStatusCode.Forbidden;
			// 	return isOk;
			// }
			// catch (InvalidOperationException)
			// {
			// 	return false;
			// }
			// catch (SocketException)
			// {
			// 	return false;
			// }
			// catch (Exception ex)
			// {
			// 	// Debug.LogException(ex);
			// 	return false;
			// }
			// finally
			// {
			// 	ServicePointManager.ServerCertificateValidationCallback = previousCallback;
			// }
		}

		public static async
			Task<(bool success, UnityWebRequest.Result result, string error, bool isCertificateError, long code)>
			IsRespondingWithStatus(string url, CancellationToken cancel = default)
		{
#if UNITY_EDITOR
			while (EditorApplication.isCompiling) await Task.Delay(1000);
#endif
			if (string.IsNullOrWhiteSpace(url))
				return (false, UnityWebRequest.Result.ConnectionError, "No url", false, -1);
			var req = new UnityWebRequest(url);
			// TODO: this is in seconds but we get curl errors that we can not suppress otherwise
			req.timeout = 1000;
			req.certificateHandler = new AllowAllCertHandler();
#if UNITY_2023_1_OR_NEWER
			await req.SendWebRequest();
#else
			req.SendWebRequest();
#endif
			while (!req.isDone && !cancel.IsCancellationRequested) await Task.Delay(30, cancel);
			// it is responding so at least the website exists
			var isCertificateError = req.error?.Contains("certificate error") ?? false;
			if (req.responseCode == 404)
				return (true, UnityWebRequest.Result.Success, req.error, isCertificateError, req.responseCode);
			return (req.result == UnityWebRequest.Result.Success, req.result, req.error, isCertificateError,
				req.responseCode);
		}

		public class AllowAllCertHandler : CertificateHandler
		{
			protected override bool ValidateCertificate(byte[] certificateData)
			{
				return true;
			}
		}

		// public static async Task<bool> IsRespondingHeaderOnly(string url, CancellationToken token = default)
		// {
		// 	var client = WebRequest.Create(new Uri(url));
		// 	client.Timeout = 1000;
		// 	client.Method = "HEAD";
		// 	var res = await client.GetResponseAsync();
		// 	return res != null;
		// }

		public static async Task<string> GetLocalServerUrl(bool tryGetIp = true, int port = 3000)
		{
#if UNITY_EDITOR
			while (EditorApplication.isCompiling) await Task.Delay(1000);
#endif
			var ip = default(IPAddress);
			if (tryGetIp)
			{
				ip = await Task.Run(() =>
				{
					if (TryGetIp(out var _ip))
						return _ip;
					return null;
				});
			}
			var url = "https://localhost:" + port;
			if (tryGetIp) url = "https://" + ip + ":" + port;
			return url;
		}

		public static bool TryGetIp(out IPAddress address)
		{
			var interfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (var ni in interfaces.Where(x => x.OperationalStatus == OperationalStatus.Up))
			{
				var isSuitable = IsSuitableNetworkInterface(ni);
				if (!isSuitable) continue;
				foreach (var ip in ni.GetIPProperties().UnicastAddresses)
				{
					if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						address = ip.Address;
						return true;
					}
				}
			}

			bool IsSuitableNetworkInterface(NetworkInterface ni)
			{
				// ignore all but Ethernet / Wifi (e.g. Bluetooth)
				if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
				    ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
					return false;

				// filter out known virtual adapters (e.g. VMWare)
				if (ni.Name.IndexOf("vEthernet", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
				    ni.Name.IndexOf("VMware", StringComparison.InvariantCultureIgnoreCase) >= 0)
					return false;

				var hasInterNetworkAddress = ni
					.GetIPProperties()
					.UnicastAddresses
					.Any(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

				return hasInterNetworkAddress;
			}

			address = null;
			return false;
		}
	}
}