using System;
using UnityEngine;

namespace Needle.Engine
{
	public static class ActionsBrowser
	{
		public class OpenBrowserArguments
		{
			/// <summary>
			/// This is the url that will be opened in the browser. It is possible to modify it (e.g. append URL parameter)
			/// </summary>
			public string Url;
			/// <summary>
			/// Set to true to prevent the default browser from opening. This is useful if you want to handle the browser opening yourself.
			/// </summary>
			public bool PreventDefault;
		}

		/// <summary>
		/// Subscribe to modify the Needle Engine localhost browser url that will open or to customize the browser opening behavior.
		/// </summary>
		public static event Action<OpenBrowserArguments> BeforeOpen;

		public static void OpenBrowser(string url)
		{
			var args = new OpenBrowserArguments
			{
				Url = url
			};
			BeforeOpen?.Invoke(args);
			if (args.PreventDefault == false)
				Application.OpenURL(args.Url);
		}
	}
}