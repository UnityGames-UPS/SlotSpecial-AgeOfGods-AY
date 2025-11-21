using System.Collections.Generic;
using System;


[Serializable]
public class SocketModel
{
  public static PlayerData playerData = new();
  public static UIData uIData = new();
  public static InitGameData initGameData = new();
  public static Root resultGameData = new();
  public static int currentBetIndex = 0;
}

[Serializable]
public class ResultGameData
{
  public List<List<int>> resultSymbols { get; set; }
  public bool isFreeSpin { get; set; }
  public int freeSpinCount { get; set; }
  public List<List<string>> symbolsToEmit { get; set; }
  public List<int> linesToEmit { get; set; }
  public List<List<int>> goldIndices { get; set; }
}


[Serializable]
public class InitGameData
{
  public List<double> Bets { get; set; }
  public List<List<int>> linesApiData { get; set; }
  public List<List<double>> features { get; set; }
}


[Serializable]
public class UIData
{
  public List<Symbol> symbols { get; set; }
}


// [Serializable]
// public class BetData
// {
//   public double currentBet;
//   public double currentLines;
//   public double spins;
//   //public double TotalLines;
// }

// [Serializable]
// public class AuthData
// {
//   public string GameID;
//   //public double TotalLines;
// }

[Serializable]
public class PlayerData
{
  public double Balance { get; set; }
  public double haveWon { get; set; }
  public double currentWining { get; set; }
}


[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}
