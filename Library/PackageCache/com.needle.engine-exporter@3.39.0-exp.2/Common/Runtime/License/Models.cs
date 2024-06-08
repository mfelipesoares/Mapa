using System;
using System.IO;
using Needle.Engine.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Needle.Engine
{
	internal class BaseModel
	{
		public string editor;
		public string editorVersion;
		public bool isPro;
		public string userName;
		public string organization;
		public string ipAddress;
		public string externalIpAddress;
		public string licenseEmail;
		public string licenseKey;

		public BaseModel()
		{
			editor = "unity";
			editorVersion = Application.unityVersion;
			isPro = Application.HasProLicense();
#if UNITY_EDITOR
			userName = CloudProjectSettings.userName;
			organization = CloudProjectSettings.organizationId;
#endif
			if (userName == "anonymous" || string.IsNullOrWhiteSpace(userName))
				userName = AnalyticsHelper.GetUserName();
			ipAddress = AnalyticsHelper.GetIpAddress();
			externalIpAddress = AnalyticsHelper.ExternalIpAddress;
			licenseEmail = LicenseCheck.LicenseEmail;
			licenseKey = LicenseCheck.LicenseKey;
		}
	}

	internal class NewInstallationModel : BaseModel
	{
		public string os;
		public string osDeviceName;
		public string osUserName;
		public string osDomainName;
		public string deviceId;
		public string graphicsDevice;
		public string systemLanguage;
		public string exporterVersion;

		public NewInstallationModel()
		{
			os = SystemInfo.operatingSystem;
			osDeviceName = SystemInfo.deviceName;
			osUserName = AnalyticsHelper.GetUserName();
			osDomainName = Environment.UserDomainName;
			deviceId = SystemInfo.deviceUniqueIdentifier;
			graphicsDevice = SystemInfo.graphicsDeviceName;
			systemLanguage = Application.systemLanguage.ToString();
			exporterVersion = ProjectInfo.GetCurrentNeedleExporterPackageVersion(out _);
		}
	}

	internal class UserCreatedProjectFromTemplateModel : BaseModel
	{
		public string projectName;
		public string templateName;

		public UserCreatedProjectFromTemplateModel(string projectName, string templateName)
		{
			this.projectName = projectName;
			this.templateName = templateName;
		}

		internal static string AnonymizeProjectName(string name)
		{
			var unityProjectNameIndex = name.LastIndexOf(Application.productName, StringComparison.OrdinalIgnoreCase);
			if (unityProjectNameIndex > 0)
			{
				return name.Substring(unityProjectNameIndex);
			}
			return name;
		}
	}

	internal class NewExportModel
	{
		public string editor;
		public string editorVersion;
		public string userName;
		public string projectPath;
		public string projectName;
		public double buildDuration;
		public int totalFilesCount;

		/// <summary>
		/// in MB
		/// </summary>
		public float totalFilesSize;

		public string details;
		
		public string licenseEmail;
		public string licenseKey;

		public NewExportModel()
		{
			editor = "unity";
			editorVersion = Application.unityVersion;
#if UNITY_EDITOR
			userName = CloudProjectSettings.userName;
#endif
			if (userName == "anonymous" || string.IsNullOrWhiteSpace(userName))
				userName = AnalyticsHelper.GetUserName();
			licenseEmail = LicenseCheck.LicenseEmail;
			licenseKey = LicenseCheck.LicenseKey;
		}
	}
    
	internal class NewDeploymentModel
	{
		public string editor = "unity";
		public string editorVersion = Application.unityVersion;
		public string editorProjectName = new DirectoryInfo(Application.dataPath + "/../").Name + "/" +
		                                  SceneManager.GetActiveScene().name;
#if UNITY_EDITOR
		public string userName = CloudProjectSettings.userName;
		public string organization = CloudProjectSettings.organizationName;
#endif
		public string url;
		public string needleEngineVersion;
		public string needleEngineExporterVersion;
		public float size;
		public bool production;
		
		public string licenseEmail;
		public string licenseKey;

		public NewDeploymentModel(string url, bool devBuild)
		{
			this.url = url;
			this.production = !devBuild;
#if UNITY_EDITOR
			if (userName == "anonymous")
				userName = AnalyticsHelper.GetUserName();
#endif

			var exportInfo = ExportInfo.Get();
			if (exportInfo)
			{
				if (PackageUtils.TryReadDependencies(exportInfo.PackageJsonPath, out var deps))
				{
					if (deps.TryGetValue("@needle-tools/engine", out var version))
					{
						if (PackageUtils.TryGetPath(exportInfo.GetProjectDirectory(), version, out var path))
						{
							var localPackageJson = path + "/package.json";
							if (PackageUtils.TryGetVersion(localPackageJson, out var localVersion))
							{
								version = localVersion + " (local)";
							}
						}
						needleEngineVersion = version;
					}
				}
			}

			var v = ProjectInfo.GetCurrentNeedleExporterPackageVersion(out _);
			this.needleEngineExporterVersion = v;

			if (NeedleProjectConfig.TryGetBuildDirectory(out var dir) && Directory.Exists(dir))
			{
				var allAssets = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
				var totalSize = 0L;
				foreach (var asset in allAssets)
				{
					var info = new FileInfo(asset);
					totalSize += info.Length;
				}
				this.size = totalSize / 1024f / 1024;
			}
			
			licenseEmail = LicenseCheck.LicenseEmail;
			licenseKey = LicenseCheck.LicenseKey;
		}
	}
}