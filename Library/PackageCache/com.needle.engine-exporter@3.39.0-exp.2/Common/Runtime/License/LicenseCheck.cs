using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine
{
	internal abstract class LicenseCheck
	{
		public static string LicenseEmail
		{
#if UNITY_EDITOR
			get => EditorPrefs.GetString("NEEDLE_ENGINE_license_id", "");
			set => EditorPrefs.SetString("NEEDLE_ENGINE_license_id", value);
#else
			get => "";
			set { }
#endif
		}
		
		public static string LicenseKey
		{
#if UNITY_EDITOR
			get => EditorPrefs.GetString("NEEDLE_ENGINE_license_key", "");
			set => EditorPrefs.SetString("NEEDLE_ENGINE_license_key", value);
#else
			get => "";
			set { }
#endif
		}

		public static event Action<bool> ReceivedLicenseReply;

		internal static bool LastLicenseCheckReturnedNull { get; private set; } = false;
		
		private static readonly WebClientHelper client = new WebClientHelper("https://urls.needle.tools/license-endpoint");

		public static bool CanMakeLicenseCheck()
		{
			var email = LicenseEmail;
			if (string.IsNullOrWhiteSpace(email))
			{
				LastLicenseResult = null;
				LastLicenseTypeResult = null;
				return false;
			}
			var key = LicenseKey;
			if (string.IsNullOrWhiteSpace(key))
			{
				LastLicenseResult = null;
				LastLicenseTypeResult = null;
				return false;
			}
			return true;
		}

		public static async Task<bool> HasValidLicense(bool printStatus = false)
		{
			var email = LicenseEmail;
			if (string.IsNullOrWhiteSpace(email))
			{
				LastLicenseResult = null;
				LastLicenseTypeResult = null;
				return false;
			}
			var key = LicenseKey;
			if (string.IsNullOrWhiteSpace(key))
			{
				LastLicenseResult = null;
				LastLicenseTypeResult = null;
				return false;
			}
			var endpoint = "/?email=" + email + "&key=" + key + "&version=2";
			LicenseCheckInProcess = true;
			try
			{
				var res = await client.SendGet(endpoint);
				if (res != null && res.IsSuccessStatusCode)
				{
					LastLicenseCheckReturnedNull = false;
					if (printStatus) Debug.Log("License check connection is " + res.StatusCode);
					NeedleDebug.LogAsync(TracingScenario.NetworkRequests, async () =>
					{
						var msg = await res.Content.ReadAsStringAsync();
						return "License check to " + endpoint + ": " + res.StatusCode + "\n" + msg;

					});
					var hasLicense = res.IsSuccessStatusCode;
					LastLicenseResult = hasLicense;
					await TryParseLicenseType(res);
					ReceivedLicenseReply?.Invoke(hasLicense);
					if (printStatus) Debug.Log("License type: \"" + LastLicenseTypeResult + "\"");
					return LastLicenseResult ?? false;
				}
				LastLicenseCheckReturnedNull = true;
				if (printStatus) Debug.LogWarning("License check connection failed: Please check your network connection");
				NeedleDebug.Log(TracingScenario.NetworkRequests,
					"Failed to get license, request returned null: " + endpoint);
				return LastLicenseResult ?? false;
			}
			finally
			{
				LicenseCheckInProcess = false;
			}
		}
		
#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			// Make sure we have the license check result cached
			if (LastLicenseResult != null) return;
			await HasValidLicense(); 
		}
#endif

		private static async Task TryParseLicenseType(HttpResponseMessage msg)
		{
			var body = await msg.Content.ReadAsStringAsync();
			NeedleDebug.Log(TracingScenario.NetworkRequests, body);
			if (body.Trim().StartsWith("{"))
			{
				var json = Newtonsoft.Json.Linq.JObject.Parse(body);
				var license = json["license"]?.ToString();
				LastLicenseTypeResult = license;
				if (license != null)
				{
					switch (license)
					{
						case "enterprise":
							LastLicenseResult = true;
							RequireLicenseAttribute.CurrentLicenseType = LicenseType.Enterprise;
							break;
						case "pro":
							LastLicenseResult = true;
							RequireLicenseAttribute.CurrentLicenseType = LicenseType.Pro;
							break;
						case "indie":
							LastLicenseResult = true;
							RequireLicenseAttribute.CurrentLicenseType = LicenseType.Indie;
							break;
						case "basic":
							LastLicenseResult = false;
							RequireLicenseAttribute.CurrentLicenseType = LicenseType.Basic;
							break;
					}
				}
			}
		}

		internal class LicenseMeta : IBuildConfigProperty
		{
			public string Key => "license";
			public object GetValue(string projectDirectory)
			{
				var obj = new JObject();
				obj["id"] = LicenseEmail;
				obj["key"] = LicenseKey;
				return obj;
				// if (string.IsNullOrWhiteSpace(LastLicenseTypeResult)) return null;
				// return LastLicenseTypeResult;
			}
		}

		internal static bool LicenseCheckInProcess { get; private set; }
		internal static bool HasLicense => LastLicenseResult == true;
		internal static bool? LastLicenseResult { get; private set; } = null;
		internal static string LastLicenseTypeResult { get; private set; }
		internal static bool LasLicenseResultIsProLicense => LastLicenseTypeResult == "pro";

		// private static DateTime lastLicenseCheckTime = DateTime.MinValue;
		// private static bool lastLicenseCheckResult = false;
		//
		// internal static void ClearLicenseCache()
		// {
		// 	lastLicenseCheckTime = DateTime.MinValue;
		// 	lastLicenseCheckResult = false;
		// }
		//
		// public static async Task<bool> HasValidLicenseCached()
		// {
		// 	if(DateTime.Now - lastLicenseCheckTime < TimeSpan.FromMinutes(10))
		// 		return lastLicenseCheckResult;
		// 	lastLicenseCheckTime = DateTime.Now;
		// 	lastLicenseCheckResult = await HasValidLicense();
		// 	return lastLicenseCheckResult;
		// }
	}
}