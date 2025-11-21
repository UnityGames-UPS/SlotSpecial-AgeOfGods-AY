using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketController : MonoBehaviour
{
  [SerializeField] private GameObject RaycastBlocker;
  internal GameData InitialData = null;
  internal UiData InitUiData = null;
  internal Root ResultData = null;
  internal Player PlayerData = null;

  [SerializeField] internal bool isResultdone = false;
  private SocketManager manager;
  private Socket GameSocket;
  protected string SocketURI = null;
  protected string TestSocketURI = "http://localhost:5000/";
  [SerializeField] private string TestToken;
  protected string nameSpace = "playground";
  internal bool SetInit = false;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  private bool isConnected = false;
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  private Coroutine PingRoutine;

  internal Action OnInit;
  internal Action ShowDisconnectionPopup;

  private string myAuth = null;

  private void Awake()
  {
    SetInit = false;
    // Debug.unityLogger.logEnabled = false;
  }

  private void Start()
  {
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

  internal void OpenSocket()
  {
    SocketOptions options = new SocketOptions();
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3);
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
    while (myAuth == null)
    {
      yield return null;
    }

    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
      };
    };
    options.Auth = authFunction;

    Debug.Log("Auth function configured with token: " + myAuth);
    SetupSocketManager(options);
  }

  private void SetupSocketManager(SocketOptions options)
  {
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif

    if (string.IsNullOrEmpty(nameSpace) || string.IsNullOrWhiteSpace(nameSpace))
    {
      GameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + nameSpace);
      GameSocket = this.manager.GetSocket("/" + nameSpace);
    }

    GameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    GameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
    GameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    GameSocket.On<string>("game:init", OnListenEvent);
    GameSocket.On<string>("result", OnListenEvent);
    GameSocket.On<string>("pong", OnPongReceived);

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

  private void OnDisconnected()
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    ResetPingRoutine();
    UiManager.DisconnectionPopup();
  }

  private void OnError(Error err)
  {
    Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("error");
#endif
  }

  private void OnPongReceived(string data)
  {
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
  }

  private void OnListenEvent(string data)
  {
    ParseResponse(data);
  }

  private void SendPing()
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
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
      if (missedPongs == 0)
      {
        // UiManager.CheckAndClosePopups();
      }

      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          //UiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          UiManager.DisconnectionPopup();
          yield break;
        }
      }

      waitingForPong = true;
      lastPongTime = Time.time;
      SendData("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  }

  internal void SendData(string eventName, object message = null)
  {
    if (GameSocket == null || !GameSocket.IsOpen)
    {
      Debug.LogWarning("Socket is not connected.");
      return;
    }
    if (message == null)
    {
      GameSocket.Emit(eventName);
      return;
    }

    isResultdone = false;
    string json = JsonConvert.SerializeObject(message);
    GameSocket.Emit(eventName, json);
    Debug.Log("JSON data sent: " + json);
  }

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  internal IEnumerator CloseSocket()
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
    Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          InitialData = myData.gameData;
          InitUiData = myData.uiData;
          LineData = myData.gameData.lines;
          if (!SetInit)
          {
            PopulateSlotSocket();
            SetInit = true;
          }
          else
          {
            RefreshUI();
          }
          break;
        }
      case "ResultData":
        {
          ResultData = myData;

          isResultdone = true;
          break;
        }
      case "ExitUser":
        {
          if (GameSocket != null)
          {
            Debug.Log("Dispose my Socket");
            GameSocket.Disconnect();
            manager.Close();
          }
#if UNITY_WEBGL && !UNITY_EDITOR
                    JSManager.SendCustomMessage("OnExit");
#endif
          break;
        }
    }
  }

  private void RefreshUI()
  {
    //UiManager.InitialiseUIData(InitUiData.paylines);
  }

  private void PopulateSlotSocket()
  {
    SlotManager.shuffleInitialMatrix();
    //SlotManager.SetInitialUI();
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("OnEnter");
#endif
    RaycastBlocker.SetActive(false);
  }

  internal void AccumulateResult(int currBet)
  {
    isResultdone = false;
    MessageData message = new MessageData();
    message.type = "SPIN";
    message.payload.betIndex = currBet;

    string json = JsonUtility.ToJson(message);
    // SendData("request", message);
    GameSocket.Emit("request", json);
  }

  // These properties are assumed to exist based on usage
  [SerializeField]
  private SlotController SlotManager;

  // private GameManager gameManager;
  private UIManager UiManager;
  private List<List<int>> LineData;
}

[Serializable]
public class Bonus
{
  public bool enabled { get; set; }
  public List<int> bonusCount { get; set; }
  public List<double> wheelProb { get; set; }
  public List<double> goldSymbolProb { get; set; }
  public SmallWheelFeature smallWheelFeature { get; set; }
  public MediumWheelFeature mediumWheelFeature { get; set; }
  public LargeWheelFeature largeWheelFeature { get; set; }
}

[Serializable]
public class Features
{
  public Bonus bonus { get; set; }
}

[Serializable]
public class FeatureValue
{
  public string type { get; set; }
  public int value { get; set; }
}

[Serializable]
public class MessageData
{
  public string type;
  public Data payload = new();
}
[Serializable]
public class GameData
{
  public List<List<int>> lines { get; set; }
  public List<double> bets { get; set; }
  public int totalLines { get; set; }
}

[Serializable]
public class LargeWheelFeature
{
  public List<FeatureValue> featureValues { get; set; }
  public List<double> featureProbs { get; set; }
}

[Serializable]
public class MediumWheelFeature
{
  public List<FeatureValue> featureValues { get; set; }
  public List<double> featureProbs { get; set; }
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Player
{
  public double balance { get; set; }
}

[Serializable]
public class Root
{
  public string id { get; set; }
  public GameData gameData { get; set; }
  public Features features { get; set; }
  public UiData uiData { get; set; }
  public Player player { get; set; }
  public bool success { get; set; }
  public List<List<string>> matrix { get; set; }

  public Payload payload { get; set; }
}

[Serializable]
public class SmallWheelFeature
{
  public List<FeatureValue> featureValues { get; set; }
  public List<double> featureProbs { get; set; }
}

[Serializable]
public class Symbol
{
  public int id { get; set; }
  public string name { get; set; }
  public List<int> multiplier { get; set; }
  public string description { get; set; }
}

[Serializable]
public class UiData
{
  public Paylines paylines { get; set; }
}

[Serializable]
public class Data
{
  public int betIndex;
  //  public string Event;
  //    public List<int> index;
  //    public int option;
}


 //Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

public class GoldenPositions
{
}

public class Payload
{
  public double winAmount { get; set; }
  public List<object> lineWins { get; set; }
  public List<GoldenPositions> goldenPositions { get; set; }
  public List<object> levelUpResponse { get; set; }
  public List<object> wildPositions { get; set; }
  public int activeLines { get; set; }
  public int freeSpinsRemaining { get; set; }
  public bool isFreeSpinActive { get; set; }
  public int wildFeaturePending { get; set; }

}
