# SmartFox

» Fields declaration
    private string defaultHost = "127.0.0.1";
    private int defaultTcpPort = 9933;
    private int defaultWsPort = 8080;
    
    
» Unity's Start callback
    hostInput.text = defaultHost;
    #if !UNITY_WEBGL
    portInput.text = defaultTcpPort.ToString();
    #else
    portInput.text = defaultWsPort.ToString();
    #endif
    
    
» Establishing a connection
    #if !UNITY_WEBGL
    sfs = new SmartFox();
    #else
    sfs = new SmartFox(UseWebSocket.WS_BIN);
    #endif

    sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
    sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);

    sfs.AddLogListener(LogLevel.INFO, OnInfoMessage);
    sfs.AddLogListener(LogLevel.WARN, OnWarnMessage);
    sfs.AddLogListener(LogLevel.ERROR, OnErrorMessage);

    // Set connection parameters
    ConfigData cfg = new ConfigData();
    cfg.Host = hostInput.text;
    cfg.Port = Convert.ToInt32(portInput.text);
    cfg.Zone = "BasicExamples";
    cfg.Debug = debugToggle.isOn;

    // Connect to SFS2X
    sfs.Connect(cfg);
    
» Handling SmartFoxServer events
    void Update() {
        if (sfs != null)
            sfs.ProcessEvents();
    }
    
## CONNECTION event handler
    private void OnConnection(BaseEvent evt) {
        if ((bool)evt.Params["success"]) {
            trace("Connection established successfully");
            trace("SFS2X API version: " + sfs.Version);
            trace("Connection mode is: " + sfs.ConnectionMode);

            // Enable disconnect button
            button.interactable = true;
            buttonLabel.text = "DISCONNECT";
        } else {
            trace("Connection failed; is the server running at all?");

            // Remove SFS2X listeners and re-enable interface
            reset();
        }
    }
    
## CONNECTION LOST event handler
    private void OnConnectionLost(BaseEvent evt) {
        trace("Connection was lost; reason is: " + (string)evt.Params["reason"]);

        // Remove SFS2X listeners and re-enable interface
        reset();
    }
