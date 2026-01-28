using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Best.SocketIO;
using Best.HTTP.Proxies;
using System.Threading;
public class GameManager : MonoBehaviour
{
    [Header("scripts")]
    [SerializeField] private SlotController slotManager;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private SocketController socketController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private WheelController wheelController;
    private SocketModel socketModel;

    [Header("For spins")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button StopSpin_Button;
    [SerializeField] private Button ToatlBetMinus_Button;
    [SerializeField] private Button TotalBetPlus_Button;
    [SerializeField] private TMP_Text totalBet_text;
    [SerializeField] private bool isSpinning;
    [SerializeField] private Button Turbo_Button;
    [SerializeField] private GameObject turboAnim;

    [Header("For auto spins")]
    [SerializeField] private Button AutoSpin_Button;

    [SerializeField] private Button[] AutoSpinOptions_Button;
    [SerializeField] private TMP_Text[] AutoSpinOptions_Text;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button AutoSpinPopup_Button;
    [SerializeField] private Button autoSpinUp;
    [SerializeField] private Button autoSpinDown;
    [SerializeField] private bool isAutoSpin;
    [SerializeField] private int autoSpinCounter;
    [SerializeField] private TMP_Text autoSpinText;
    [SerializeField] private TMP_Text autoSpinShowText;
    List<int> autoOptions = new List<int>() { 15, 20, 25, 30, 40, 100 };
    [SerializeField] private int maxAutoSpinValue = 1000;


    [Header("For features")]
    [SerializeField] internal GameObject ThreeinaRow;
    [SerializeField] internal GameObject fourinaRow;
    [SerializeField] internal GameObject fiveinaRow;
    [SerializeField] internal WheelView smallWheel;
    [SerializeField] internal WheelView MediumWheel;
    [SerializeField] internal WheelView LargeWheel;


    [Header("For FreeSpins")]
    [SerializeField] private bool specialSpin;

    private double currentBalance;
    [SerializeField] internal TMP_Text Balance_Text;
    [SerializeField] private double currentTotalBet;
    [SerializeField] internal int betCounter = 0;


    private Coroutine autoSpinRoutine;
    private Coroutine freeSpinRoutine;
    [SerializeField] private int winIterationCount;
    [SerializeField] private int freeSpinCount;
    [SerializeField] private bool isFreeSpin;


    private bool initiated;
    [SerializeField] private bool turboMode;
    static internal bool immediateStop;
    static internal bool winAnimComplete = false;
    private Coroutine winPopUpRoutine;
    bool featureSpin;
    private int autoSpinLeft;
    private Coroutine lineAnimCoroutine;
    private Coroutine spinRoutine;


    void Start()
    {

        SetButton(SlotStart_Button, ExecuteSpin, true);
        SetButton(AutoSpin_Button, () =>
        {
            ExecuteAutoSpin();
            // uIManager.ClosePopup();
        }, true);
        // InitiateAutoSpin();
        SetButton(AutoSpinStop_Button, () => StartCoroutine(StopAutoSpinCoroutine()));
        SetButton(ToatlBetMinus_Button, () => OnBetChange(false));
        SetButton(TotalBetPlus_Button, () => OnBetChange(true));
        SetButton(autoSpinUp, () => OnAutoSpinChange(true));
        SetButton(autoSpinDown, () => OnAutoSpinChange(false));
        SetButton(Turbo_Button, () => ToggleTurboMode());
        SetButton(StopSpin_Button, () => StartCoroutine(StopSpin()));
        // autoSpinCounter=0;
        // SetButton(freeSpinStartButton, () => );

        // autoSpinShowText.text = autoOptions[autoSpinCounter].ToString();


        slotManager.shuffleInitialMatrix();
        socketController.OnInit = InitGame;
        uIManager.ToggleAudio = audioController.ToggleMute;
        uIManager.playButtonAudio = audioController.PlayButtonAudio;
        uIManager.OnExit = () => socketController.CloseSocket();
        socketController.ShowDisconnectionPopup = uIManager.DisconnectionPopup;

        //socketController.OpenSocket();

        // StopSpin_Button.onClick.AddListener(() => StartCoroutine(StopSpin()));
        // Turbo_Button.onClick.AddListener(ToggleTurboMode);
    }


    private void SetButton(Button button, Action action, bool slotButton = false)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            audioController.PlayButtonAudio();
            action?.Invoke();

        });
    }
    void InitGame()
    {
        if (!initiated)
        {
            initiated = true;
            betCounter = 0;
            currentTotalBet = socketController.InitialData.bets[betCounter] * socketController.InitialData.lines.Count;
            currentBalance = socketController.PlayerData.balance;
            Balance_Text.text = socketController.PlayerData.balance.ToString();
            if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
            if (currentBalance < currentTotalBet)
                uIManager.LowBalPopup();

            //uIManager.UpdatePlayerInfo(socketController.PlayerData);
            uIManager.PopulateSymbolsPayout(socketController.InitUiData);
            //  wheelController.PopulateWheels(SocketModel.initGameData.features);
            // Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
        }
        else
        {
            uIManager.PopulateSymbolsPayout(socketController.InitUiData);
            //uIManager.PopulateSymbolsPayout(SocketModel.uIData);
            // wheelController.PopulateWheels(SocketModel.initGameData.features);

        }
        PopulateWheelSymbol();

    }
    internal void PopulateWheelSymbol()
    {

        smallWheel.PopulateValues(socketController.InitFeature.features.bonus.smallWheelFeature.featureValues);
        slotManager.smallWheel.PopulateValues(socketController.InitFeature.features.bonus.smallWheelFeature.featureValues);

        MediumWheel.PopulateValues(socketController.InitFeature.features.bonus.mediumWheelFeature.featureValues);
        slotManager.MediumWheel.PopulateValues(socketController.InitFeature.features.bonus.mediumWheelFeature.featureValues);

        LargeWheel.PopulateValues(socketController.InitFeature.features.bonus.largeWheelFeature.featureValues);
        slotManager.LargeWheel.PopulateValues(socketController.InitFeature.features.bonus.largeWheelFeature.featureValues);


    }
    // void InitiateAutoSpin(){


    //     for (int i = 0; i < autoOptions.Count; i++)
    //     {
    //         int capturedIndex=i;
    //         SetButton(AutoSpinOptions_Button[capturedIndex],()=>ExecuteAutoSpin(autoOptions[capturedIndex]));
    //         AutoSpinOptions_Text[capturedIndex].text=autoOptions[capturedIndex].ToString();
    //         autoSpinCounter=autoOptions[capturedIndex];
    //         // uIManager.ClosePopup();

    //     }


    //}

    void ExecuteSpin()
    {
        if (spinRoutine != null) return;
        spinRoutine = StartCoroutine(SpinRoutine());
    }



    void ExecuteAutoSpin()
    {


        if (!isSpinning)
        {

            isAutoSpin = true;
            // autoSpinText.transform.gameObject.SetActive(true);
            // AutoSpin_Button.gameObject.SetActive(false);

            AutoSpinStop_Button.gameObject.SetActive(true);
            autoSpinRoutine = StartCoroutine(AutoSpinRoutine());
        }

    }

    void ToggleTurboMode()
    {
        turboMode = !turboMode;
        if (turboMode)
            turboAnim.SetActive(true);
        else
            turboAnim.SetActive(false);

    }

    IEnumerator FreeSpinRoutine()
    {
        uIManager.ToggleFreeSpinPanel(true);
        uIManager.EnablePurplebar(true);
        uIManager.CloseFreeSpinPopup();

        if (StopSpin_Button.gameObject.activeSelf)
        {
            StopSpin_Button.gameObject.SetActive(false);
            immediateStop = false;
        }
        while (freeSpinCount > 0)
        {
            freeSpinCount--;
            uIManager.UpdateFreeSpinInfo(freeSpinCount);

            yield return SpinRoutine();
            yield return new WaitForSeconds(1);
        }
        audioController.playBgAudio("FP");

        uIManager.ToggleFreeSpinPanel(false);

        isAutoSpin = false;
        isSpinning = false;
        isFreeSpin = false;

        if (autoSpinLeft > 0)
        {
            // ExecuteAutoSpin(autoSpinLeft);
            uIManager.ClosePopup();

        }
        else
        {
            ToggleButtonGrp(true);

        }

        yield return null;
    }
    IEnumerator AutoSpinRoutine()
    {
        while (isAutoSpin && !isFreeSpin)
        {
            autoSpinText.text = autoSpinLeft.ToString();

            yield return SpinRoutine();

            if (isFreeSpin)
                yield break;

            yield return new WaitForSeconds(0.5f);
        }

        // CLEAN EXIT (NO coroutine calls)
        autoSpinText.transform.gameObject.SetActive(false);
        isSpinning = false;

        AutoSpin_Button.gameObject.SetActive(true);
        AutoSpinStop_Button.gameObject.SetActive(false);
        autoSpinLeft = 0;
        autoSpinText.text = "0";
    }

    private IEnumerator StopAutoSpinCoroutine(bool hard = false)
    {
        isAutoSpin = false;


        AutoSpin_Button.gameObject.SetActive(true);
        AutoSpinStop_Button.gameObject.SetActive(false);
        autoSpinText.transform.gameObject.SetActive(false);
        yield return new WaitUntil(() => !isSpinning);


        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
            autoSpinText.text = "0";
        }

        AutoSpinPopup_Button.gameObject.SetActive(true);
        ToggleButtonGrp(true);
        autoSpinLeft = 0;

        autoSpinText.text = "0";
        yield return null;

    }
    IEnumerator SpinRoutine()
    {
        bool start = OnSpinStart();

        // ===== CASE 1: Spin did not start (low balance etc.)
        if (!start)
        {
            spinRoutine = null;          // ✅ ADD THIS
            isSpinning = false;

            if (isAutoSpin)
            {
                StartCoroutine(StopAutoSpinCoroutine());
            }

            ToggleButtonGrp(true);
            yield break;                 // ← OK now
        }

        slotManager.SetGoldenDarkActive();
        Debug.Log("hajshdfj");

        yield return OnSpin();
        Debug.Log("2222222222");

        yield return OnSpinEnd();
        Debug.Log("2222222222");

        Debug.Log("++++++++++++++++++++++++++++ calling PlatyWheel");
        featureSpin = false;

        // ===== WHEEL PART (no yield break here, so no change needed)
        if (socketController.ResultData.payload.iswheeltrigger)
        {
            yield return new WaitForSeconds(3f);
            ThreeinaRow.SetActive(false);
            fourinaRow.SetActive(false);
            fiveinaRow.SetActive(false);

            Debug.Log("++++++++++ calling PlatyWheel");
            yield return StartCoroutine(
                slotManager.PlayWheel(socketController.ResultData.payload.wheelBonus)
            );

            if (socketController.ResultData.payload.wheelBonus.featureType == "freeSpin")
            {
                featureSpin = true;
            }
        }

        Debug.Log("33333333");

        // ===== CASE 2: Free spin triggered
        if (socketController.ResultData.payload.isfreespintriggered || featureSpin)
        {
            Debug.Log("freespin5555");

            int prevFreeSpin = freeSpinCount;
            freeSpinCount = socketController.ResultData.payload.freeSpinsRemaining;
            uIManager.UpdateFreeSpinInfo(freeSpinCount);
            isFreeSpin = true;

            isAutoSpin = false;

            if (autoSpinRoutine != null)
            {
                StopCoroutine(autoSpinRoutine);
                autoSpinRoutine = null;
            }

            autoSpinText.text = "0";
            AutoSpin_Button.gameObject.SetActive(true);
            AutoSpinStop_Button.gameObject.SetActive(false);

            Debug.Log("freespin666666");

            if (freeSpinRoutine != null)
            {
                StopCoroutine(freeSpinRoutine);

                if (freeSpinCount - prevFreeSpin > 0)
                    uIManager.FreeSpinPopup(freeSpinCount - prevFreeSpin, false);

                yield return new WaitForSeconds(2f);
                uIManager.CloseFreeSpinPopup();
                freeSpinRoutine = StartCoroutine(FreeSpinRoutine());
            }
            else
            {
                uIManager.FreeSpinPopup(freeSpinCount, true);
                audioController.playBgAudio("FP");
                yield return new WaitForSeconds(2f);
                uIManager.CloseFreeSpinPopup();
                freeSpinRoutine = StartCoroutine(FreeSpinRoutine());
            }

            Debug.Log("freespin7777777");

            spinRoutine = null;      // ✅ ADD THIS
            isSpinning = false;      // ✅ ADD THIS
            yield break;             // ← OK now
        }

        Debug.Log("44444444");

        // ===== NORMAL SPIN END
        if (!isAutoSpin && !isFreeSpin)
        {
            isSpinning = false;
        }

        // ===== FINAL CLEANUP (VERY IMPORTANT)
        spinRoutine = null;          // ✅ ADD THIS
        ToggleButtonGrp(true);
    }


    IEnumerator StopSpin()
    {
        if (isAutoSpin || isFreeSpin || immediateStop || specialSpin)
            yield break;
        immediateStop = true;
        StopSpin_Button.interactable = false;
        yield return new WaitUntil(() => !isSpinning);
        immediateStop = false;
        StopSpin_Button.interactable = true;


    }
    bool OnSpinStart()
    {
        slotManager.StopIconAnimation();
        slotManager.StopAnimateALLWins();
        slotManager.SetWildePosOff();
        // slotManager.watchAnimation.StopAnimation();
        slotManager.watchAnimation.StopAnimation();
        isSpinning = true;
        winIterationCount = 0;
        slotManager.disableIconsPanel.SetActive(false);
        if (currentBalance < currentTotalBet && !isFreeSpin)
        {
            uIManager.LowBalPopup();
            return false;
        }
        ToggleButtonGrp(false);
        uIManager.ClosePopup();
        return true;


    }

    IEnumerator OnSpin()
    {
        if (!isFreeSpin && !specialSpin)
            uIManager.DeductBalanceAnim(socketController.PlayerData.balance - currentTotalBet, socketController.PlayerData.balance);

        slotManager.watchAnimation.StartAnimation();

        if (!isFreeSpin && !isAutoSpin && !specialSpin)
            StopSpin_Button.gameObject.SetActive(true);

        if (specialSpin)
            immediateStop = false;

        Debug.Log("immediate stop" + immediateStop);

        yield return slotManager.StartSpin(turboMode: turboMode);
        //var spinData = new { data = new { currentBet = betCounter, currentLines = 1, spins = 1 }, id = "SPIN" };
        //socketController.SendData("message", spinData);
        socketController.AccumulateResult(betCounter);
        yield return new WaitUntil(() => socketController.isResultdone);
        slotManager.PopulateSLotMatrix(socketController.ResultData.matrix, socketController.ResultData.payload.goldenPositions);
        //currentBalance = socketController.PlayerData.balance;

        if (immediateStop || turboMode)
            yield return new WaitForSeconds(0.15f);
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
        // slotManager.StopIconAnimation();
        yield return slotManager.StopSpin(turboMode: turboMode, audioController.PlaySpinStopAudio);

        if (StopSpin_Button.gameObject.activeSelf)
        {
            StopSpin_Button.gameObject.SetActive(false);
        }

    }
    IEnumerator OnSpinEnd()
    {
        //  Debug.Log("----------------1");
        audioController.StopSpinAudio();
        if (socketController.ResultData.payload.goldenPositions.Count > 0)
        {
            slotManager.SetGoldenDarkActive();
        }
        if (socketController.ResultData.payload.wildPositions.Count > 0)
        {
            slotManager.SetGoldenDarkActive();
        }
        if (socketController.ResultData.payload.iswheeltrigger)
        {
            checkForGoldenInarow(socketController.ResultData.payload.goldenPositions);
        }
        //  Debug.Log("----------------2");
        if (!isFreeSpin && !isAutoSpin && !socketController.ResultData.payload.iswheeltrigger)
        {
            StopSpin_Button.interactable = false;
            SlotStart_Button.interactable = true;
        }
        if (socketController.ResultData.payload.lineWins.Count > 0)
        {
            // audioController.PlayWLAudio("electric");
            for (int i = 0; i < socketController.ResultData.payload.lineWins.Count; i++)
            {
                LineWin lineWins = socketController.ResultData.payload.lineWins[i];
                slotManager.AnimateLineWins(lineWins);
                yield return new WaitForSeconds(0.5f);
                //   Debug.Log(i);
                slotManager.StopAnimateLineWins(lineWins);
            }
            audioController.StopWLAaudio();
        }
        //  Debug.Log("----------------3");
        slotManager.SetDarkActive(true, false);
        uIManager.UpdatePlayerInfo();

        //  Debug.Log("----------------4");
        // if (socketController.ResultData.payload.winAmount > 0)
        // {

        //     winAnimComplete = false;
        //     CheckWinPopups(socketController.ResultData.payload.winAmount);
        //     yield return new WaitWhile(() => !winAnimComplete);
        //     winAnimComplete = false;
        //     if (winPopUpRoutine != null)
        //     {
        //         StopCoroutine(winPopUpRoutine);
        //         winPopUpRoutine = null;
        //     }
        //     audioController.StopWLAaudio();

        // }
        //  Debug.Log("----------------5");
        if (isFreeSpin)
            uIManager.UpdateFreeSpinInfo(winnings: socketController.ResultData.payload.winAmount);

        slotManager.StopIconAnimation();
        slotManager.SetWildePosOff();
        slotManager.watchAnimation.StopAnimation();

        // Debug.Log("----------------6");
        yield return null;
    }


    void checkForGoldenInarow(List<GoldenPositions> goldPositions)
    {
        // Highest priority first
        int[] priorities = { 5, 4, 3 };

        List<Vector2Int> allPositions = new();

        foreach (var gp in goldPositions)
        {
            foreach (var pos in gp.positions)
            {
                allPositions.Add(new Vector2Int(pos[0], pos[1])); // row, col
            }
        }

        var groupedByRow = new Dictionary<int, List<int>>();

        foreach (var p in allPositions)
        {
            if (!groupedByRow.ContainsKey(p.x))
                groupedByRow[p.x] = new List<int>();

            groupedByRow[p.x].Add(p.y);
        }

        // Try 5 → 4 → 3 (MAX first)
        foreach (int requiredCount in priorities)
        {
            foreach (var kvp in groupedByRow)
            {
                int row = kvp.Key;
                List<int> cols = kvp.Value;
                cols.Sort();

                List<Vector2Int> streak = new();

                for (int i = 0; i < cols.Count; i++)
                {
                    if (i == 0 || cols[i] == cols[i - 1] + 1)
                    {
                        streak.Add(new Vector2Int(row, cols[i]));
                    }
                    else
                    {
                        streak.Clear();
                        streak.Add(new Vector2Int(row, cols[i]));
                    }

                    if (streak.Count >= requiredCount)
                    {
                        // Take FIRST valid max streak and exit
                        var finalStreak = streak.GetRange(
                            streak.Count - requiredCount,
                            requiredCount
                        );

                        TriggerFeature(requiredCount, finalStreak);
                        return; // ✅ STOP EVERYTHING
                    }
                }
            }
        }
    }

    void TriggerFeature(int count, List<Vector2Int> streak)
    {
        GameObject feature = null;

        if (count == 3) feature = ThreeinaRow;
        else if (count == 4) feature = fourinaRow;
        else if (count == 5) feature = fiveinaRow;

        if (feature == null) return;

        feature.SetActive(true);

        Vector3 finalPosition;

        if (count % 2 == 1) // 3 or 5
        {
            // Exact center
            Vector2Int centerPos = streak[count / 2];
            finalPosition = GetSlotTransform(centerPos.x, centerPos.y).position;
        }
        else // 4 in a row
        {
            // Between middle two slots
            Vector2Int leftCenter = streak[(count / 2) - 1];
            Vector2Int rightCenter = streak[count / 2];

            Transform leftT = GetSlotTransform(leftCenter.x, leftCenter.y);
            Transform rightT = GetSlotTransform(rightCenter.x, rightCenter.y);

            finalPosition = (leftT.position + rightT.position) / 2f;
        }

        feature.transform.position = finalPosition;
    }

    Transform GetSlotTransform(int row, int col)
    {
        return slotManager.WildMatrix[row].slotImages[col].transform;
    }

    void ToggleButtonGrp(bool toggle)
    {
        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (AutoSpinPopup_Button) AutoSpinPopup_Button.interactable = toggle;
        if (ToatlBetMinus_Button) ToatlBetMinus_Button.interactable = toggle;
        if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = toggle;
        uIManager.Settings_Button.interactable = toggle;
    }

    private void OnBetChange(bool inc)
    {
        if (audioController) audioController.PlayButtonAudio();

        if (inc)
        {
            betCounter++;

        }
        else
        {
            betCounter--;

        }
        if (betCounter > socketController.InitialData.bets.Count - 1)
        {
            betCounter = 0;
        }
        if (betCounter < 0)
        {
            betCounter = socketController.InitialData.bets.Count - 1;

        }

        currentTotalBet = socketController.InitialData.bets[betCounter] * socketController.InitialData.lines.Count;
        if (totalBet_text) totalBet_text.text = currentTotalBet.ToString();
        // if (currentBalance < currentTotalBet)
        //     uIManager.LowBalPopup();
        uIManager.PopulateSymbolsPayout(socketController.InitUiData);
    }

    private void OnAutoSpinChange(bool inc)
    {

        if (audioController) audioController.PlayButtonAudio();

        if (inc)
        {
            autoSpinCounter++;
            if (autoSpinCounter > maxAutoSpinValue)
            {
                autoSpinCounter = 1;
            }
        }
        else
        {
            autoSpinCounter--;
            if (autoSpinCounter < 1)
            {
                autoSpinCounter = maxAutoSpinValue;

            }
        }

        autoSpinShowText.text = autoSpinCounter.ToString();


    }


    void CheckWinPopups(double amount)
    {
        // if (amount > 0 && amount < currentTotalBet * 5)
        // {
        //     uIManager.EnableWinPopUp(0, amount);
        // }
        // else if (amount >= currentTotalBet * 5 && amount <= currentTotalBet * 7.5)
        // {
        //     uIManager.EnableWinPopUp(1, amount);
        //     audioController.PlayWLAudio("big");

        // }
        // else if (amount >= currentTotalBet * 7.5 && amount < currentTotalBet * 10)
        // {
        //     uIManager.EnableWinPopUp(2, amount);
        //     audioController.PlayWLAudio("big");

        // }
        if (amount >= currentTotalBet * 10)
        {
            uIManager.EnableWinPopUp(3, amount);
            audioController.PlayWLAudio("big");

        }

    }
}
