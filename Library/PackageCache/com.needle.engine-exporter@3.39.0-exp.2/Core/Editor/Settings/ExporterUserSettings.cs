using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Needle.Engine.Settings
{
	[FilePath("UserSettings/NeedleExporterUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class ExporterUserSettings : ScriptableSingleton<ExporterUserSettings>
	{
		public void Save() => Save(true);

		[SerializeField]
		internal bool ShowWelcomeWindowAtStart = true;

		[SerializeField] private bool firstInstallation = true;
		internal static event Action FirstInstall;

		public bool FirstInstallation
		{
			get => firstInstallation;
			set
			{
				// can only set to true once
				if (!firstInstallation || value == true) return;
				firstInstallation = false;
				FirstTimeRunningNeedleEngine = true;
				Save();
				Analytics.RegisterInstallation();
				FirstInstall?.Invoke();
			}
		}
		/// <summary>
		/// True if this editor session is the first time the user has been running Needle Engine (after installing it)
		/// </summary>
		internal static bool FirstTimeRunningNeedleEngine
		{
			get => SessionState.GetBool(("NeedleEngine_FirstTimeRunningNeedleEngine"), false);
			private set => SessionState.SetBool(("NeedleEngine_FirstTimeRunningNeedleEngine"), value);
		}

		public bool UseVSCode = true;
		
		public bool UserReadAndAcceptedEula = false;
		public bool UserConfirmedEulaCompliance = false;
		
		
		
		[InitializeOnLoadMethod]
		private static async void Init()
		{
			while (EditorApplication.isUpdating || EditorApplication.isCompiling) await Task.Delay(1000);
			await Task.Delay(100);

			var s = ExporterUserSettings.instance;
			if (s.FirstInstallation)
			{
				s.FirstInstallation = false;
			}
		}
	}
}