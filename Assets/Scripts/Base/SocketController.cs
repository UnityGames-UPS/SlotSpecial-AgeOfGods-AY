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
  [SerializeField] private UIManager UiManager;
  internal GameData InitialData = null;
  internal UiData InitUiData = null;
  internal Root ResultData = null;
  internal Root InitFeature = null;
  internal Player PlayerData = null;
  [SerializeField] internal bool isResultdone = false;
  private SocketManager manager;
  private Socket GameSocket;
  protected string SocketURI = null;
  protected string TestSocketURI = "http://localhost:5000/";

  protected string nameSpace = "playground"; //BackendChanges
  internal bool SetInit = false;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  private bool isConnected = false;
  private bool hasEverConnected = false;
  [SerializeField] internal JSFunctCalls JSManager;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;
  [SerializeField]
  private string testToken;
  protected string gameID = "SL-AOG";
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
    Application.runInBackground = true;
  }

  private void Start()
  {
    //OpenWebsocket();
    OpenSocket();
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
    // Proceed with connecting to the server using myAuth and socketURL
  }

  // string myAuth = null;

  private void OpenSocket()
  {
    SocketOptions options = new SocketOptions(); //Back2 Start
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
        token = testToken
      };
    };
    options.Auth = authFunction;
    // Proceed with connecting to the server
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
    GameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
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
      UiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  }

  private void OnDisconnected(string response)
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
        UiManager.CheckAndClosePopups();
      }

      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          UiManager.ReconnectionPopup();
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
    PlayerData = myData.player;
    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          InitialData = myData.gameData;
          InitUiData = myData.uiData;
          LineData = myData.gameData.lines;
          InitFeature = myData;

          if (!SetInit)
          {
            PopulateSlotSocket();
            OnInit?.Invoke();
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
          Debug.Log("Receved Result" + ResultData.payload.wildPositions.Count);
          isResultdone = true;
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

  private List<List<int>> LineData;
}

[Serializable]
public class Bonus
{
  public bool enabled;
  public List<int> bonusCount;
  public List<double> wheelProb;
  public List<double> goldSymbolProb;
  public SmallWheelFeature smallWheelFeature;
  public MediumWheelFeature mediumWheelFeature;
  public LargeWheelFeature largeWheelFeature;
}

[Serializable]
public class Features
{
  public Bonus bonus;
}

[Serializable]
public class FeatureValue
{
  public string type;
  public int value;
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
  public List<List<int>> lines;
  public List<double> bets;
  public int totalLines;
}

[Serializable]
public class LargeWheelFeature
{
  public List<FeatureValue> featureValues;
  public List<double> featureProbs;
}

[Serializable]
public class MediumWheelFeature
{
  public List<FeatureValue> featureValues;
  public List<double> featureProbs;
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols;
}

[Serializable]
public class Player
{
  public double balance;
}

[Serializable]
public class Root
{
  public string id;
  public GameData gameData;
  public Features features;
  public UiData uiData;
  public Player player;
  public bool success;
  public List<List<string>> matrix;

  public Payload payload;
}

[Serializable]
public class SmallWheelFeature
{
  public List<FeatureValue> featureValues;
  public List<double> featureProbs;
}

[Serializable]
public class Symbol
{
  public int id;
  public string name;
  public List<int> multiplier;
  public string description;
}

[Serializable]
public class UiData
{
  public Paylines paylines;
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
[Serializable]
public class GoldenPositions
{
  public int symbolId;
  public List<List<int>> positions;
}
[Serializable]
public class Payload
{
  public double winAmount;
  public List<LineWin> lineWins;
  public List<GoldenPositions> goldenPositions;
  public WheelBonus wheelBonus;
  public List<bool> levelUpResponse;
  public List<object> wildPositions;
  public int activeLines;
  public int freeSpinsRemaining;
  public bool isFreeSpinActive;
  public bool iswheeltrigger;
  public int wildFeaturePending;


}
[Serializable]
public class LineWin
{
  public int lineIndex;
  public List<int> positions;
  public List<int> pattern;
  public string symbolId;
  public string symbolName;
  public double payout;
  public int matchCount;
}
[Serializable]
public class WheelBonus
{
  public string wheelType;
  public GoldenSymbols goldenSymbols;
  public string featureType;
  public int featureValue;
  public double awardValue;
  public List<bool> levelUpChain;

  public List<string> wheelTypeChain;
}
[Serializable]
public class GoldenSymbols
{
  public string symbolId;
  public string symbolName;
  public int row;
  public List<int> positions;
  public int count;
}