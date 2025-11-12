using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;

public class SocketController : MonoBehaviour
{
  [SerializeField] private GameObject RaycastBlocker;
  [SerializeField] internal bool isResultdone = false;
  private SocketManager manager;
  private Socket GameSocket;
  protected string SocketURI = null;
  protected string TestSocketURI = "http://localhost:5000";
  [SerializeField] private string TestToken;
  protected string nameSpace = "playground";
  internal bool SetInit = false;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  private bool isConnected = false; //Back2 Start
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  private Coroutine PingRoutine; //Back2 end
  internal Action OnInit;
  internal Action ShowDisconnectionPopup;

  private void Awake()
  {
    SetInit = false;
    // Debug.unityLogger.logEnabled = false;
  }

  private void Start()
  {
    //OpenWebsocket();
    OpenSocket();
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
  }

  string myAuth = null;

  internal void OpenSocket()
  {
    // Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3); //Back2 end
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("authToken");
    StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = TestToken
      };
    };
    options.Auth = authFunction;
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      yield return null;
    }

    // Once myAuth is set, configure the authFunction
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
      };
    };
    options.Auth = authFunction;

    Debug.Log("Auth function configured with token: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);
  }

  private void SetupSocketManager(SocketOptions options)
  {
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(nameSpace) | string.IsNullOrWhiteSpace(nameSpace))
    {
      GameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + nameSpace);
      GameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    GameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    GameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
    GameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    GameSocket.On<string>("game:init", OnListenEvent);
    GameSocket.On<string>("result", OnListenEvent);
    GameSocket.On<string>("pong", OnPongReceived); //Back2 Start

    manager.Open();
  }

  void OnConnected(ConnectResponse resp)
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      // UiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  }
  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    ResetPingRoutine();
    // UiManager.DisconnectionPopup();
  } //Back2 end

  private void OnError(Error err)
  {
    Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("error");
#endif
  }

  private void OnPongReceived(string data) //Back2 Start
  {
    // Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    // Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    // Debug.Log($"📦 Pong payload: {data}");
  }
  private void OnListenEvent(string data)
  {
    ParseResponse(data);
  }

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  internal IEnumerator CloseSocket() //Back2 Start
  {
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); 
#endif
  } 

  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    // JObject resp = JObject.Parse(jsonObject);

    // int id = (int)resp["payload"]["betIndex"];

    // // var message = resp["message"];
    // // var gameData = message["GameData"];

    // if (resp["message"]["PlayerData"] != null)
    //   SocketModel.playerData = resp["message"]["PlayerData"].ToObject<PlayerData>();

    // switch (id)
    // {
    //   case "InitData":
    //     {
    //       SocketModel.uIData.symbols = resp["message"]["UIData"]["paylines"]["symbols"].ToObject<List<Symbol>>();
    //       SocketModel.initGameData = resp["message"]["GameData"].ToObject<InitGameData>();
    //       OnInit?.Invoke();
    //       Debug.Log("init data" + JsonConvert.SerializeObject(SocketModel.initGameData));
    //       break;
    //     }
    //   case "ResultData":
    //     {

    //       SocketModel.resultGameData = resp["message"]["GameData"].ToObject<ResultGameData>();

    //       Debug.Log("result data" + JsonConvert.SerializeObject(SocketModel.resultGameData));
    //       isResultdone = true;
    //       break;
    //     }

    //   case "ExitUser":
    //     {
    //       if (this.manager != null)
    //       {
    //         Debug.Log("Dispose my Socket");
    //         this.manager.Close();
    //       }
    //       Application.ExternalCall("window.parent.postMessage", "onExit", "*");
    //       break;
    //     }
    // }
  }
  private void SendPing()
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  private void OnDisconnected(string response)
  {
    Debug.Log("Disconnected from the server");
    StopAllCoroutines();
    ShowDisconnectionPopup?.Invoke();
  }
  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      // Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        // UiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          // UiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          // UiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      // Debug.Log("📤 Sending ping...");
      SendData("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  }

  // private void RefreshUI()
  // {
  //     uIManager.InitialiseUIData(initUIData.AbtLogo.link, initUIData.AbtLogo.logoSprite, initUIData.ToULink, initUIData.PopLink, initUIData.paylines);
  // }

  internal void SendData(string eventName, object message = null)
  {

    if (this.manager.Socket == null || !this.manager.Socket.IsOpen)
    {
      Debug.LogWarning("Socket is not connected.");
      return;
    }
    if (message == null)
    {
      this.manager.Socket.Emit(eventName);
      return;
    }
    isResultdone = false;
    string json = JsonConvert.SerializeObject(message);
    this.manager.Socket.Emit(eventName, json);
    Debug.Log("JSON data sent: " + json);

  }
}
