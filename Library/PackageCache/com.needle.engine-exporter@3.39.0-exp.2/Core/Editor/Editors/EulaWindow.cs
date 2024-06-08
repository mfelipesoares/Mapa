using System;
using System.Threading.Tasks;
using Needle.Engine.Problems;
using Needle.Engine.Settings;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.Engine.Editors
{
	public class EulaWindow : EditorWindow
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
		}


		[MenuItem("Help/Needle Engine/Open EULA", false, Constants.MenuItemOrder)]
		private static void Open()
		{
			Open(null);
		}

		public static void Open(Action accepted = null)
		{
			var window = GetWindow<EulaWindow>();
			if (window)
			{
				window.accepted = accepted;
				window.Show();
			}
			else
			{
				window = CreateInstance<EulaWindow>();
				window.accepted = accepted;
				window.Show();
			}
		}

		public static bool RequiresEulaAcceptance
		{
			get
			{
				if (CommandlineSettings.EulaAccepted) return false;
				return !ExporterUserSettings.instance.UserReadAndAcceptedEula ||
				       !ExporterUserSettings.instance.UserConfirmedEulaCompliance;
			}
		}

		// The user email is null if a user is not logged in and has not entered an email address in the License field
		// this should change in the future where we can get the email from the license server
		public static Task<bool> HasAllowedContact() => userEmail == null ? Task.FromResult(true) : AnalyticsHelper.HasAllowedContact(userEmail);

		internal static bool DidOpenDuringExport = false;

		private static bool allowContact
		{
			get => EditorPrefs.GetBool("Needle_EULA_AllowContact", false);
			set => EditorPrefs.SetBool("Needle_EULA_AllowContact", value);
		}

		private static bool didToggleAllowContact
		{
			get => EditorPrefs.GetBool("Needle_EULA_DidToggleAllowContact", false);
			set => EditorPrefs.SetBool("Needle_EULA_DidToggleAllowContact", value);
		}

		private static string userEmail
		{
			get
			{
				var email = LicenseCheck.LicenseEmail;
				if (email?.Contains("@") == false) email = CloudProjectSettings.userName;
				if (email?.Contains("@") == false) email = null;
				return email;
			}
		}

		private Action accepted;

		private void OnEnable()
		{
			minSize = new Vector2(515, 550);
			maxSize = new Vector2(515, 550);
			titleContent = new GUIContent("Needle EULA");

			var root = new VisualElement();
			var header = new VisualElement();
			UIComponents.BuildHeader(header);
			header.style.paddingBottom = 20;
			root.Add(header);
			var mainContainer = new IMGUIContainer();
			mainContainer.onGUIHandler += OnDrawUI;
			root.Add(mainContainer);
			VisualElementRegistry.Register(header);
			rootVisualElement.Add(root);
			rootVisualElement.style.paddingLeft = 10;
			rootVisualElement.style.paddingRight = 10;

			acceptedEula = ExporterUserSettings.instance.UserReadAndAcceptedEula;
			acceptedCompliance = ExporterUserSettings.instance.UserConfirmedEulaCompliance;

			HasAllowedContact().ContinueWith(res => allowContact = res.Result, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void OnDrawUI()
		{
			DrawGUI(this);
		}

		private static bool acceptedEula, acceptedCompliance;
		private static Vector2 scroll;
		private static GUIStyle _toggleStyle, _wrappedLabelStyle;

		public static void DrawGUI(EulaWindow window = null)
		{
			using var scope = new GUILayout.ScrollViewScope(scroll);
			scroll = scope.scrollPosition;

			_toggleStyle ??= new GUIStyle(EditorStyles.label);
			_toggleStyle.wordWrap = true;
			_toggleStyle.alignment = TextAnchor.MiddleLeft;
			_toggleStyle.clipping = TextClipping.Overflow;

			_wrappedLabelStyle ??= new GUIStyle(EditorStyles.wordWrappedLabel);
			_wrappedLabelStyle.richText = true;

			EditorGUILayout.LabelField(
				"In order to start using Needle Engine you must read and accept the Needle EULA.\nMost notably, the use of Needle Engine Basic is restricted to evaluation purposes or non-commercial work.\n\nThere are several plans for commercial usage, depending on your needs:\n→ <b>Indie</b>: for individuals or companies with total finances below 100.000€ per year\n→ <b>Pro</b>: for companies with total finances above 100.000€ per year\n→ <b>Enterprise</b>: for companies with total finances above 5.000.000€ per year",
				_wrappedLabelStyle);

			GUILayout.Space(10);
			if (GUILayout.Button("Read the full EULA", GUILayout.Height(32))) Application.OpenURL(Constants.EulaUrl);
			GUILayout.Space(20);


			var height = GUILayout.Height(40);

			const string acceptText =
				"I have read and understood the EULA and the restrictions that apply to the use of Needle Engine Basic, Indie and Pro.";
			const string complianceText =
				"I and/or the entity I work for are using Needle Engine for evaluation purposes or non-commercial work and in compliance with the Needle EULA.";
			const string complianceWhenLicensedText =
				"I and/or the entity I work for are using Needle Engine in compliance with the Needle EULA. I have read and understood that Needle Engine licenses are per-seat and I'm the only person using this seat.";
			
			using (new EditorGUI.DisabledScope(!RequiresEulaAcceptance))
			{
				acceptedEula = EditorGUILayout.ToggleLeft(acceptText, acceptedEula, _toggleStyle, height);
				acceptedCompliance = EditorGUILayout.ToggleLeft(
					LicenseCheck.HasLicense ? complianceWhenLicensedText : complianceText,
					acceptedCompliance,
					_toggleStyle,
					height);
			}

			var currentAllowContact = allowContact | !didToggleAllowContact;
			var email = userEmail;
			// If a user has not entered an email address in the License field and is not logged in we don't show the allow contact toggle
			if (email != null)
			{
				using (var change = new EditorGUI.ChangeCheckScope())
				{
					using var _ = new ColorScope(DidOpenDuringExport ? new Color(1, 1, .5f) : Color.white);
					var allow = EditorGUILayout.ToggleLeft(new GUIContent($"I allow Needle to contact me and/or the entity I work for via email {email} regarding news and my/our use of Needle Engine.", "Allowing us to contact you is required when using Needle Engine Basic/Free (the non-commercial version of Needle Engine). Licensed users can disable this option."), currentAllowContact,
						_toggleStyle, height);
					if (change.changed)
					{
						didToggleAllowContact = true;
						allowContact = allow;
						UpdateAllowContact();
					}
				}
			}
			
			GUILayout.Space(10);
			using (new EditorGUI.DisabledScope(!acceptedEula || !acceptedCompliance || !RequiresEulaAcceptance))
			{
				if (GUILayout.Button("I agree to the EULA", GUILayout.Height(32)))
				{
					ExporterUserSettings.instance.UserReadAndAcceptedEula = acceptedEula;
					ExporterUserSettings.instance.UserConfirmedEulaCompliance = acceptedCompliance;
					ExporterUserSettings.instance.Save();
					
					allowContact = currentAllowContact;
					UpdateAllowContact();

					var acceptedCallback = window?.accepted;
					CloseWindowAfterDelay();

					async void CloseWindowAfterDelay()
					{
						await Task.Delay(1000);
						window?.Close();
						await Task.Delay(300);
						acceptedCallback?.Invoke();
					}
				}
			}

			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("See plans ↗",
				    "Opens the Needle Engine website to buy or manage licenses")))
				Application.OpenURL(Constants.BuyLicenseUrl);

			if (GUILayout.Button(new GUIContent(LicenseCheck.HasLicense ? "Manage license" : "Activate license")))
				SettingsService.OpenProjectSettings("Project/Needle/Needle Engine");

			EditorGUILayout.EndHorizontal();
            
			GUILayout.Space(10);

			if (!RequiresEulaAcceptance && (LicenseCheck.HasLicense || allowContact))
			{
				EditorGUILayout.LabelField("Thank you. You can now close this window and start using Needle Engine!",
					EditorStyles.wordWrappedLabel);
			}
		}
		
		private static async void UpdateAllowContact()
		{
			await AnalyticsHelper.UpdateAllowContact(userEmail, allowContact);
		}
	}
}