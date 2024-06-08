using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Needle.Engine.Components
{
	[AddComponentMenu("Needle Engine/Networking/Synced Room" + Needle.Engine.Constants.NeedleComponentTags)]
	[ExecuteAlways]
	[HelpURL(Constants.DocumentationUrlNetworking)]
	public class SyncedRoom : MonoBehaviour
	{
		[Info("Required component to support networking. Handles connecting clients to rooms")]
		[Tooltip("The room name prefix")]
		public string roomName = null;
		[Tooltip("The room name parameter name in the url: for example ?room=123")]
		public string urlParameterName = "room";
		[Tooltip("Joins random room if the url does not contain a room name yet")]
		public bool joinRandomRoom = true;
		[FormerlySerializedAs("requireRoom"), Tooltip("If enabled clients wont connect to any room unless their url contains a room parameter. If disabled clients will automatically connect to the default room (e.g. when no room name in the url will be found it will just be the base roomName)")] 
		public bool requireRoomParameter = false;

		[Tooltip("Attempt to auto rejoin a room if user was disconnected from networking backend (e.g. server kicked user due to inactivity)")]
		public bool autoRejoin = true;

		[Tooltip("When enabled a Join Room button will be added to the UI")]
		public bool createJoinButton = true;
		
		private void OnValidate()
		{
			if (roomName == null || roomName.Length <= 0)
			{
				roomName = SceneManager.GetActiveScene().name;
			}
		}

		public void tryJoinRoom() {}
		public void tryJoinRandomRoom() {}
		
		#if UNITY_EDITOR
		[CustomEditor(typeof(SyncedRoom))]
		internal class SyncedRoomEditor : Editor
		{
			private Networking networkingComponent;
			
			private void OnEnable()
			{
				networkingComponent = FindAnyObjectByType<Networking>();
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				if (!networkingComponent)
				{
					GUILayout.Space(10);
					EditorGUILayout.LabelField("Networking Information", EditorStyles.boldLabel);
					EditorGUILayout.HelpBox("Your project will connect to the default networking backend. If you want to connect to a different networking backend add the Networking component to your project.", MessageType.Info);
				}
			}
		}
		#endif
	}
}