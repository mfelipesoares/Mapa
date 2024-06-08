using System;
using UnityEngine;

namespace Needle.Engine.Deployment
{
	[CreateAssetMenu(menuName = "Needle Engine/FTP Server Info")]
	public class FTPServer : ScriptableObject
	{
		[Tooltip("Example: subdomain.domain.com or mydomain.com. Check your hosting provider for those.")]
		public string Servername;

		[Tooltip("When disabled deployment will not be allowed without configuring a subfolder path in the DeployToFTP component")]
		public bool AllowTopLevelDeployment = true;
		
		[Tooltip("When enabled SFTP will be used instead of FTP")]
		public bool SFTP = false;

		[Tooltip("Modify Port if your server uses a different port than the default FTP port 21. SFTP uses port 22 by default. Leave this value at -1 if you want to use defaults")]
		public int Port = -1;
		
		public string Username;

		[HideInInspector]
		public string RemoteUrl;
		
		public bool RemoteUrlIsValid => !string.IsNullOrWhiteSpace(RemoteUrl) && (RemoteUrl.StartsWith("www") || RemoteUrl.StartsWith("http"));

		public string GetUrl(string path)
		{
			var url = this.RemoteUrl + "/" + path;
			url = url.Replace("//", "/").Replace("//", "/");
			url = Uri.EscapeUriString(url);
			return url;
		}
		
		public bool TryGetKey(out string key)
		{
			key = Servername + Username;
			return !string.IsNullOrWhiteSpace(Servername) && !string.IsNullOrWhiteSpace(Username);
		}
		
		private void OnValidate()
		{
			Servername = Servername?.Trim();
			Username = Username?.Trim();
		}
	}
}