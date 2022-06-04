using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;

namespace SFS2XExamples.AdvancedConnector {
	public class AdvancedConnector : MonoBehaviour {

		//----------------------------------------------------------
		// UI elements
		//----------------------------------------------------------

		public InputField hostInput;
		public InputField portInput;
		public Toggle loginToggle;
		public Toggle debugToggle;
		public Toggle lagMonitorToggle;
		public Button button;
		public Text buttonLabel;
		public ScrollRect debugScrollRect;
		public Text debugText;

		//----------------------------------------------------------
		// Private properties
		//----------------------------------------------------------

		private SmartFox sfs;

		/*
		 * IMPORTANT NOTE
		 * Protocol encryption requires a specific setup of SmartFoxServer 2X and a valid SSL certificate.
		 * For this reason it is disabled by default in this example. If you want to test it, please read
		 * this document carefully before proceeding: http://docs2x.smartfoxserver.com/GettingStarted/cryptography
		 * The code performing the encryption initialization is provided here for reference,
		 * showing how to handle it when building for different platforms.
		 */
		private bool useEncryption = false;

		//----------------------------------------------------------
		// Unity calback methods
		//----------------------------------------------------------

		void Start() {
			// Initialize UI
			hostInput.text = SFS2XExamples.Panel.Settings.ipAddress;
			portInput.text = SFS2XExamples.Panel.Settings.port.ToString();

			debugText.text = "";
		}

		void Update() {
			// As Unity is not thread safe, we process the queued up callbacks on every frame
			if (sfs != null)
				sfs.ProcessEvents();
		}

		//----------------------------------------------------------
		// Public interface methods for UI
		//----------------------------------------------------------

		public void OnButtonClick() {
			if (sfs == null || !sfs.IsConnected) {

				// CONNECT

				// Enable interface
				enableInterface(false);
				
				// Clear console
				debugText.text = "";
				debugScrollRect.verticalNormalizedPosition = 1;
				
				trace("Now connecting...");
				
				// Initialize SFS2X client and add listeners
				// WebGL build uses a different constructor
				#if !UNITY_WEBGL
				sfs = new SmartFox();
				#else
				sfs = new SmartFox(useEncryption ? UseWebSocket.WSS_BIN : UseWebSocket.WS_BIN);
				#endif
				
				sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
				sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
				sfs.AddEventListener(SFSEvent.CRYPTO_INIT, OnCryptoInit);
				sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
				sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
				sfs.AddEventListener(SFSEvent.PING_PONG, OnPingPong);
				
				sfs.AddLogListener(LogLevel.DEBUG, OnDebugMessage);
				sfs.AddLogListener(LogLevel.INFO, OnInfoMessage);
				sfs.AddLogListener(LogLevel.WARN, OnWarnMessage);
				sfs.AddLogListener(LogLevel.ERROR, OnErrorMessage);
				
				// Set connection parameters
				ConfigData cfg = new ConfigData();
				cfg.Host = hostInput.text;
				cfg.Port = Convert.ToInt32(portInput.text);
				cfg.HttpPort = 8080;
				cfg.HttpsPort = 8443;
				cfg.Zone = SFS2XExamples.Panel.Settings.zone;
				cfg.Debug = debugToggle.isOn;
					
				// Connect to SFS2X
				sfs.Connect(cfg);
			} else {

				// DISCONNECT

				// Disable button
				button.interactable = false;
				
				// Disconnect from SFS2X
				sfs.Disconnect();
			}
		}

		//----------------------------------------------------------
		// Private helper methods
		//----------------------------------------------------------
		
		private void enableInterface(bool enable) {
			hostInput.interactable = enable;
			portInput.interactable = enable;
			loginToggle.interactable = enable;
			lagMonitorToggle.interactable = enable;
			debugToggle.interactable = enable;

			button.interactable = enable;
			buttonLabel.text = "CONNECT";
		}
		
		private void trace(string msg) {
			debugText.text += (debugText.text != "" ? "\n" : "") + msg;
			Canvas.ForceUpdateCanvases();
			debugScrollRect.verticalNormalizedPosition = 0;
		}

		private void reset() {
			// Remove SFS2X listeners
			sfs.RemoveEventListener(SFSEvent.CONNECTION, OnConnection);
			sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			sfs.RemoveEventListener(SFSEvent.CRYPTO_INIT, OnCryptoInit);
			sfs.RemoveEventListener(SFSEvent.LOGIN, OnLogin);
			sfs.RemoveEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
			sfs.RemoveEventListener(SFSEvent.PING_PONG, OnPingPong);
			
			sfs.RemoveLogListener(LogLevel.DEBUG, OnDebugMessage);
			sfs.RemoveLogListener(LogLevel.INFO, OnInfoMessage);
			sfs.RemoveLogListener(LogLevel.WARN, OnWarnMessage);
			sfs.RemoveLogListener(LogLevel.ERROR, OnErrorMessage);
			
			sfs = null;
			
			// Enable interface
			enableInterface(true);
		}
		
		private void login() {
			if (loginToggle.isOn) {
				// Login as guest
				sfs.Send(new Sfs2X.Requests.LoginRequest(""));
			} else {
				if (lagMonitorToggle.isOn)
					trace ("Lag monitor can be started after a successful login only");
			}
		}

		//----------------------------------------------------------
		// SmartFoxServer event listeners
		//----------------------------------------------------------
		
		private void OnConnection(BaseEvent evt) {
			if ((bool)evt.Params["success"]) {
				trace("Connection established successfully");
				trace("SFS2X API version: " + sfs.Version);
				trace("Connection mode is: " + sfs.ConnectionMode);
				
				// Enable disconnect button
				button.interactable = true;
				buttonLabel.text = "DISCONNECT";

				#if !UNITY_WEBGL
				// Enable protocol encryption on non-WebGL builds only (WebGL build uses WSS protocol already)
				if (useEncryption) {
					// Initialize encryption
					sfs.InitCrypto();
				} else {
					// Attempt login
					login();
				}
				#else
				// Attempt login
				login();
				#endif
			} else {
				trace("Connection failed; is the server running at all?");
				
				// Remove SFS2X listeners and re-enable interface
				reset();
			}
		}
		
		private void OnConnectionLost(BaseEvent evt) {
			trace("Connection was lost; reason is: " + (string)evt.Params["reason"]);
			
			// Remove SFS2X listeners and re-enable interface
			reset();
		}
		
		private void OnCryptoInit(BaseEvent evt) {
			if ((bool) evt.Params["success"])
			{
				trace("Encryption initialized successfully");
				
				// Attempt login
				login();
			} else {
				trace("Encryption initialization failed: " + (string)evt.Params["errorMessage"]);
			}
		}
		
		private void OnLogin(BaseEvent evt) {
			User user = (Sfs2X.Entities.User)evt.Params["user"];
			
			trace("Login successful");
			trace("Username is: " + user.Name);
			
			// Enable lag monitor
			if (lagMonitorToggle.isOn)
				sfs.EnableLagMonitor(true);
		}
		
		private void OnLoginError(BaseEvent evt) {
			trace("Login failed: " + (string) evt.Params["errorMessage"]);
		}
		
		private void OnPingPong(BaseEvent evt) {
			trace("Measured lag is: " + (int) evt.Params["lagValue"] + "ms");
		}
		
		//----------------------------------------------------------
		// SmartFoxServer log event listeners
		//----------------------------------------------------------
		
		public void OnDebugMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("DEBUG", message);
		}
		
		public void OnInfoMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("INFO", message);
		}
		
		public void OnWarnMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("WARN", message);
		}
		
		public void OnErrorMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("ERROR", message);
		}
		
		private void ShowLogMessage(string level, string message) {
			message = "[SFS > " + level + "] " + message;
			trace(message);
			Debug.Log(message);
		}
	}
}