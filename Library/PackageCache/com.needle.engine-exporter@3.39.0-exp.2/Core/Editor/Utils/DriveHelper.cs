using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Needle.Engine.Utils
{
	internal static class DriveHelper
	{
		private static readonly Dictionary<string, bool> SymlinkSupportCache = new Dictionary<string, bool>();
		private static DriveInfo[] driveInfos;
		
		/// <summary>
		/// Beware: this might not return the correct result immediately 
		/// </summary>
		public static bool HasSymlinkSupport(string path)
		{
			// TODO: make an awaitable / sync version of this
			var root = Path.GetPathRoot(path);
			if (SymlinkSupportCache.TryGetValue(root, out var result)) return result;
			RunDriveHelperInBackgroundTask(root);
			return true;
		}

		private static Task driveHelperBackgroundTask;
		private static void RunDriveHelperInBackgroundTask(string root)
		{
			if (driveHelperBackgroundTask != null) return;
			driveHelperBackgroundTask = Task.Run(() =>
			{
				// Debug.Log(("Check symlink support for: " + root).LowContrast());
				try
				{
					if (driveInfos == null)
						driveInfos = DriveInfo.GetDrives();
					foreach (var drive in driveInfos)
					{
						if (drive == null) continue;
						// ReSharper disable once ConditionIsAlwaysTrueOrFalse
						if (drive.DriveFormat == null) continue;
						if (drive.DriveFormat.StartsWith("FAT32", System.StringComparison.OrdinalIgnoreCase)
						    || drive.DriveFormat.StartsWith("exFAT", System.StringComparison.OrdinalIgnoreCase))
						{
							// ReSharper disable once ConditionIsAlwaysTrueOrFalse
							if (drive.Name != null && root.Replace("\\", "/").StartsWith(drive.Name.Replace("\\", "/")))
							{
								SymlinkSupportCache[root] = false;
								return false;
							}
						}
					}
				}
				catch (Exception)
				{
					// ignored
				}
				SymlinkSupportCache[root] = true;
				return true;
			});
		}
		
		public static bool HasEnoughAvailableDiscSpace(string path, float minSpaceInMb)
		{
			var allDrives = DriveInfo.GetDrives();
			var info = new DirectoryInfo(path);
			var root = info.Root;
			foreach (var drive in allDrives)
			{
				if (drive.RootDirectory.FullName != root.FullName) continue;
				var availableMb = drive.AvailableFreeSpace / (1024 * 1024);
				if (availableMb >= minSpaceInMb)
				{
					return true;
				}
			}
			return false;
		}
	}
}