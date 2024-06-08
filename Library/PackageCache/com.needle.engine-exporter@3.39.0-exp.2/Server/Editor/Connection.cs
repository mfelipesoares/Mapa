using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using WebSocketSharp;
using Debug = UnityEngine.Debug;
using WebSocket = WebSocketSharp.WebSocket;

namespace Needle.Engine.Server
{
	public class Connection : IDisposable
	{
		// [MenuItem("Needle Engine/Internal/Editor Sync/Toggle Server Debug Logs", priority = 100_000)]
		// private static void ToggleDebug()
		// {
		// 	DebugLog = !DebugLog;
		// 	Debug.Log("Needle Engine Server Debug Logs: " + DebugLog);
		// }

		public static bool DebugLog = false;


		public static Connection Instance
		{
			get
			{
				_instance ??= new Connection();
				if (!_instance.IsConnected) _instance.Connect();
				return _instance;
			}
		}

		[InitializeOnLoadMethod]
		private static void Init()
		{
			_instance = Instance;
		}

		private static Connection _instance;


		public bool IsConnected => client != null && client.ReadyState == WebSocketState.Open;
		public bool KeepAlive = true;


		public void Connect(int port = 1107)
		{
			if (this.port != port) client?.Close();

			switch (client?.ReadyState)
			{
				case WebSocketState.Connecting:
					return;
			}

			if (!IsConnected)
			{
				this.port = port;
				if (port == 0) port = 8080;
				var address = "wss://localhost:" + port;
				if (!address.StartsWith("ws"))
					address = "wss://" + address;
				if (DebugLog)
					Debug.Log("Connect to " + address);
				client = new WebSocket(address);
				if (address.StartsWith("wss"))
					client.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
				client.OnOpen += OnOpen;
				client.OnMessage += OnMessage;
				client.OnError += OnError;
				client.OnClose += OnClose;
				client.ConnectAsync();
			}
		}

		public void Close()
		{
			client?.Close();
			client = null;
		}

		public event Action<RawMessage> Message;
		public event Action Connected;
		public event Action Closed;
		public event Action<string> Error;

		public void Send(string type, JToken data)
		{
			if (!this.client.IsAlive)
			{
				if (KeepAlive) ScheduleReconnect();
				return;
			}
			var obj = new JObject();
			obj["type"] = type;
			obj["data"] = data;
			this.client.Send(obj.ToString());
		}

		public void SendRaw(string str)
		{
			if (!this.client.IsAlive)
			{
				if (KeepAlive) ScheduleReconnect();
				return;
			}
			this.client.Send(str);
		}

		private int port;
		private WebSocket client;

		private void OnOpen(object sender, EventArgs e)
		{
			if (DebugLog) Debug.Log("Connected!");
			failedOpenAttempts = 0;
			this.client.Send("needle:editor=unity");
			Connected?.Invoke();
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnClose(object sender, CloseEventArgs e)
		{
			failedOpenAttempts += 1;
			if (DebugLog) Debug.Log($"Connection closed: {e.Code}, {e.Reason}");
			Closed?.Invoke();
			if (KeepAlive) ScheduleReconnect();
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			if (DebugLog) Debug.Log($"Received message: {e.Data}");
			messages.Enqueue(e);
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			failedOpenAttempts += 1;
			if (DebugLog)
				Debug.LogError(e.Message);
			Error?.Invoke(e.Message);
			if (KeepAlive) ScheduleReconnect();
		}

		public void Dispose()
		{
			client?.CloseAsync();
		}

		private bool reconnectScheduled;
		private int failedOpenAttempts = 0;

		private async void ScheduleReconnect()
		{
			if (reconnectScheduled) return;
			int timeout = 5000 * (1 + failedOpenAttempts) * failedOpenAttempts;
			if (DebugLog)
				Debug.Log($"SCHEDULE RECONNECT AFTER {timeout}MS");
			reconnectScheduled = true;
			await Task.Delay(Math.Abs(timeout));
			reconnectScheduled = false;
			if (KeepAlive)
				Connect(port);
		}

		private readonly Queue<MessageEventArgs> messages = new Queue<MessageEventArgs>();

		private void OnEditorUpdate()
		{
			try
			{
				while (messages.Count > 0)
				{
					var i = messages.Dequeue();
					if (i != null && i.Data != null)
					{
						if (string.IsNullOrEmpty(i.Data)) continue;
						if (i.Data.StartsWith("{") || i.Data.StartsWith("["))
						{
							var msg = JsonConvert.DeserializeObject<RawMessage>(i.Data);
							Message?.Invoke(msg);
						}
						else
						{
							var msg = new RawMessage();
							msg.type = "raw";
							msg.data = i.Data;
							Message?.Invoke(msg);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}
	}
}