using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
#if UNITY_2021_1_OR_NEWER
using SystemInfo = UnityEngine.Device.SystemInfo;
#else
using SystemInfo = UnityEngine.SystemInfo;
#endif

namespace Needle.Engine
{
	internal static class NeedleEngineAuthorization
	{
		private const int trialDuration = 15;

		internal static bool TrialEnded => DaysUntilTrialEnds < 0;
		internal static bool IsInTrialPeriod => DaysUntilTrialEnds >= 0;

		internal static int DaysUntilTrialEnds
		{
			get
			{
				if (_daysUntilTrialEnds != null) return _daysUntilTrialEnds.Value;
				_daysUntilTrialEnds = (GetFirstInstallation().AddDays(trialDuration) - DateTime.Now).Days;
				return _daysUntilTrialEnds.Value;
			}
		}

#if UNITY_EDITOR
		private static DateTime _firstInstallationTime = default;
		
		[InitializeOnLoadMethod]
		private static void Init()
		{
			_firstInstallationTime = GetFirstInstallation();
			_daysUntilTrialEnds = DaysUntilTrialEnds;
		}
#endif

		private static int? _daysUntilTrialEnds;

		private static DateTime GetFirstInstallation()
		{
			try
			{
#if !UNITY_EDITOR
				return DateTime.Now;
#else
				if (_firstInstallationTime != default)
					return _firstInstallationTime;
				var now = DateTime.Now;
				var nowStr = now.ToString(CultureInfo.CurrentCulture);
				var firstInstallTimeStr = EditorPrefs.GetString("NEEDLE_ENGINE_first_installation_date", nowStr);
				if (firstInstallTimeStr == nowStr)
				{
					EditorPrefs.SetString("NEEDLE_ENGINE_first_installation_date", nowStr);
					return now;
				}
				// Looks like trial time was modified -> this is invalid
				if (string.IsNullOrWhiteSpace(firstInstallTimeStr))
				{
				}
				if (DateTime.TryParse(firstInstallTimeStr, DateTimeFormatInfo.CurrentInfo,
					    DateTimeStyles.AllowWhiteSpaces, out var firstInstallTime))
				{
					return firstInstallTime;
				}
				if (DateTime.TryParse(firstInstallTimeStr, DateTimeFormatInfo.InvariantInfo,
					    DateTimeStyles.AllowWhiteSpaces, out firstInstallTime))
				{
					return firstInstallTime;
				}
				return DateTime.Now.Subtract(TimeSpan.FromDays(99999));
#endif
			}
			catch (Exception)
			{
				// ignore
			}
			return DateTime.Now;
		}

		private static HttpClient licenseCheckClient;
		internal static async Task<(bool success, string message)> IsAuthorized(string licenseEmail, int retry = 0)
		{
			var query = "?version=" + Uri.EscapeDataString(ProjectInfo.GetCurrentNeedleExporterPackageVersion(out _));
			query += "&license=" + Uri.EscapeDataString(licenseEmail);
			query += "&device=" + Uri.EscapeDataString(SystemInfo.deviceUniqueIdentifier);
#if UNITY_EDITOR
			query += "&user=" + Uri.EscapeDataString(CloudProjectSettings.userName);
			query += "&org=" + Uri.EscapeDataString(CloudProjectSettings.organizationId);
			query += "&project=" + Uri.EscapeDataString(CloudProjectSettings.projectId);
#endif
			const string url = "https://engine.needle.tools/licensing/editor/check";
			var uri = new Uri(url + query);
			
			NeedleDebug.Log(TracingScenario.NetworkRequests, "License check to: " + uri);

			try
			{
				var timeout = TimeSpan.FromSeconds(5);
				licenseCheckClient ??= new HttpClient(){Timeout = timeout};

				string responseText = null;
				bool isSuccess = false;
				licenseCheckClient.CancelPendingRequests();
				responseText = await licenseCheckClient.GetStringAsync(uri);
				isSuccess = responseText != null;
				return (isSuccess, responseText);
			}
			catch (Exception ex)
			{
				NeedleDebug.LogException(TracingScenario.NetworkRequests, ex);
				return (true, null);
			}
		}
	}
}