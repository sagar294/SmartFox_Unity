using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Requests.MMO;

namespace SFS2XExamples.MMORoomDemo {
	public class ConnectionUI : MonoBehaviour {

		//----------------------------------------------------------
		// UI elements
		//----------------------------------------------------------

		public InputField nameInput;
		public Button loginButton;
		public Text errorText;

		//----------------------------------------------------------
		// Private properties
		//----------------------------------------------------------

		private SmartFox sfs;
	
		//----------------------------------------------------------
		// Unity calback methods
		//----------------------------------------------------------

		void Start() {
			// Initialize UI
			errorText.text = "";
		}
	
		void Update() {
			if (sfs != null)
				sfs.ProcessEvents();
		}

		void OnApplicationQuit() {
			// Always disconnect before quitting
			if (sfs != null && sfs.IsConnected)
				sfs.Disconnect ();
		}

		// Disconnect from the socket when ordered by the main Panel scene
		public void Disconnect() {
			OnApplicationQuit();
		}

		//----------------------------------------------------------
		// Public interface methods for UI
		//----------------------------------------------------------
	
		public void OnLoginButtonClick() {
			enableLoginUI(false);
		
			// Set connection parameters
			ConfigData cfg = new ConfigData();
			cfg.Host = SFS2XExamples.Panel.Settings.ipAddress;
			cfg.Port = SFS2XExamples.Panel.Settings.port;
			cfg.Zone = SFS2XExamples.Panel.Settings.zone;
		
			// Initialize SFS2X client and add listeners
			#if !UNITY_WEBGL
			sfs = new SmartFox();
			#else
			sfs = new SmartFox(UseWebSocket.WS_BIN);
			#endif
		
			sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
			sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
			sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
			sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
			sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
		
			// Connect to SFS2X
			sfs.Connect(cfg);
		}

		//----------------------------------------------------------
		// Private helper methods
		//----------------------------------------------------------
	
		private void enableLoginUI(bool enable) {
			nameInput.interactable = enable;
			loginButton.interactable = enable;
			errorText.text = "";
		}
	
		private void reset() {
			// Remove SFS2X listeners
			sfs.RemoveAllEventListeners();
		
			// Enable interface
			enableLoginUI(true);
		}

		//----------------------------------------------------------
		// SmartFoxServer event listeners
		//----------------------------------------------------------

		private void OnConnection(BaseEvent evt) {
			if ((bool)evt.Params["success"]) {
				// Save reference to the SmartFox instance in a static field, to share it among different scenes
				SmartFoxConnection.Connection = sfs;

				Debug.Log("SFS2X API version: " + sfs.Version);
				Debug.Log("Connection mode is: " + sfs.ConnectionMode);

				// Login
				sfs.Send(new Sfs2X.Requests.LoginRequest(nameInput.text));
			}
			else {
				// Remove SFS2X listeners and re-enable interface
				reset();
			
				// Show error message
				errorText.text = "Connection failed; is the server running at all?";
			}
		}
	
		private void OnConnectionLost(BaseEvent evt) {
			// Remove SFS2X listeners and re-enable interface
			reset();
		
			string reason = (string) evt.Params["reason"];
		
			if (reason != ClientDisconnectionReason.MANUAL) {
				// Show error message
				errorText.text = "Connection was lost; reason is: " + reason;
			}
		}
	
		private void OnLogin(BaseEvent evt) {
			string roomName = "UnityMMODemo";

			// We either create the Game Room or join it if it exists already
			if (sfs.RoomManager.ContainsRoom(roomName)) {
				sfs.Send(new JoinRoomRequest(roomName));
			} else {
				MMORoomSettings settings = new MMORoomSettings(roomName);
				settings.DefaultAOI = new Vec3D(25f, 1f, 25f);
				settings.MapLimits = new MapLimits(new Vec3D(-100f, 1f, -100f), new Vec3D(100f, 1f, 100f));
				settings.MaxUsers = 100;
				settings.Extension = new RoomExtension("MMORoomDemo", "sfs2x.extension.mmo.MMORoomDemoExtension");
				sfs.Send(new CreateRoomRequest(settings, true));
			}
		}
	
		private void OnLoginError(BaseEvent evt) {
			// Disconnect
			sfs.Disconnect();
		
			// Remove SFS2X listeners and re-enable interface
			reset();
		
			// Show error message
			errorText.text = "Login failed: " + (string) evt.Params["errorMessage"];
		}
	
		private void OnRoomJoin(BaseEvent evt) {
			// Remove SFS2X listeners and re-enable interface before moving to the main game scene
			reset();

			// Go to main game scene
			SceneManager.LoadScene("07 MMORoomDemoGame");
		}
	
		private void OnRoomJoinError(BaseEvent evt) {
			// Show error message
			errorText.text = "Room join failed: " + (string) evt.Params["errorMessage"];
		}
	}
}