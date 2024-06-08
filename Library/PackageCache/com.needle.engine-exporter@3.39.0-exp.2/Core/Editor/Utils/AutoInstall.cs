using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Needle.Engine.Utils
{
	internal static class AutoInstall
	{
#if UNITY_2020_3_OR_NEWER
		[InitializeOnLoadMethod]
		private static void Init()
		{
			Events.registeredPackages -= OnPackageRegistered;
			Events.registeredPackages += OnPackageRegistered;
		}

		private static void OnPackageRegistered(PackageRegistrationEventArgs args)
		{
			foreach (var arg in args.added)
			{
				if (TryRunProjectSetup(arg)) return;
			}
			foreach (var arg in args.changedTo)
			{
				if (TryRunProjectSetup(arg)) return;
			}
		}

		private static bool TryRunProjectSetup(PackageInfo packageInfo)
		{
			switch (packageInfo.name)
			{
				case Constants.UnityPackageName:
					RunAutoInstall();
					return true;
			}
			return false;
		}

		private static async void RunAutoInstall()
		{
			Actions.StopLocalServer(true);
			await Task.Delay(300);
			var exp = ExportInfo.Get();
			if (exp)
			{
				InternalActions.DeleteViteCaches();
				InternalActions.DeletePackageJsonLock();
				await Actions.RunProjectSetup(true);
			}
		}
#endif
	}
}