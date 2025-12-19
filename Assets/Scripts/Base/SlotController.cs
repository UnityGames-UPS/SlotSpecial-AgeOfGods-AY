using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;
using Newtonsoft.Json;

public class SlotController : MonoBehaviour
{
    [SerializeField] internal AudioController audioController;

    [Header("Sprites")]
    [SerializeField] private Sprite[] iconImages;
    [SerializeField] private Sprite[] goldIconImages;
    [SerializeField] private List<Sprite> wildAnimSprite;
    [SerializeField] private List<Sprite> timeMachine1Sprite;
    [SerializeField] private List<Sprite> timeMachine2Sprite;
    [SerializeField] private List<Sprite> circleAnimSprite;
    [SerializeField] private List<Sprite> squareAnimSprite;

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> slotMatrix;
    [SerializeField] internal GameObject disableIconsPanel;
    [SerializeField] private List<SlotImage> allMatrix;
    [SerializeField] internal List<WildImage> WildMatrix;

    [Header("wheel bonus")]
    [SerializeField] internal GameObject wheelPanel;
    [SerializeField] internal WheelView smallWheel;
    [SerializeField] internal WheelView MediumWheel;
    [SerializeField] internal WheelView LargeWheel;


    [Header("Slots Transforms")]
    [SerializeField] private RectTransform[] Slot_Transform;
    [SerializeField] private RectTransform mask_transform;
    [SerializeField] private RectTransform bg_mask_transform;
    [SerializeField] private RectTransform[] bg_slot_transform;
    [SerializeField] private RectTransform[] sideBars;
    [SerializeField] private ImageAnimation[] sideBarsAnim;

    [SerializeField] private RectTransform[] horizontalBars;
    [SerializeField] internal ImageAnimation watchAnimation;
    [SerializeField] internal int level;

    [SerializeField] private TMP_Text noOfWays;

    [Header("tween properties")]
    [SerializeField] private float tweenHeight = 0;
    [SerializeField] private float initialPos;



    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;

    [SerializeField] private List<Image> levelIndicator;
    [SerializeField] internal List<SlotIconView> animatingIcons = new List<SlotIconView>();

    internal IEnumerator StartSpin(bool turboMode)
    {

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            InitializeTweening(Slot_Transform[i], turboMode);
            if (!GameManager.immediateStop)
                yield return new WaitForSeconds(0.1f);

        }
        ResetAllIcons();
        // yield return new WaitForSeconds(0.2f);
    }

    internal void PopulateSLotMatrix(List<List<string>> resultData, List<GoldenPositions> goldPositions) //, List<List<int>> goldPositions
    {

        for (int i = 0; i < resultData.Count; i++)
        {
            for (int j = 0; j < resultData[i].Count; j++)
            {
                slotMatrix[j].slotImages[i].SetIcon(ID: int.Parse(resultData[i][j]), image: iconImages[int.Parse(resultData[i][j])]);
                if (int.Parse(resultData[i][j]) == 10)
                {
                    // Debug.Log(i + "i " + j + "j ");
                    WildMatrix[i].slotImages[j].StopAnimation();
                    WildMatrix[i].slotImages[j].gameObject.SetActive(true);
                    WildMatrix[i].slotImages[j].StartAnimation();
                }
            }
        }
        for (int i = 0; i < goldPositions.Count; i++)
        {
            int id = goldPositions[i].symbolId;
            for (int j = 0; j < goldPositions[i].positions.Count; j++)
            {
                slotMatrix[goldPositions[i].positions[j][1]].slotImages[goldPositions[i].positions[j][0]].SetGoldIcon(goldIconImages[id]);
            }
        }
    }
    internal IEnumerator StopSpin(bool turboMode, Action playFallAudio)
    {

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            StopTweening(Slot_Transform[i], i, turboMode, GameManager.immediateStop);

            if (!GameManager.immediateStop)
            {

                playFallAudio?.Invoke();
                if (turboMode)
                    yield return new WaitForSeconds(0.1f);
                else
                    yield return new WaitForSeconds(0.2f);
            }

        }
        if (GameManager.immediateStop)
        {
            playFallAudio?.Invoke();
            yield return new WaitForSeconds(0.2f);
        }
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < slotMatrix[i].slotImages.Count; j++)
            {
                if (slotMatrix[i].slotImages[j].isGold)
                    slotMatrix[i].slotImages[j].AnimateGoldIcons();
            }
        }
        yield return new WaitForSeconds(0.5f);
        KillAllTweens();

    }

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < allMatrix.Count; i++)
        {
            for (int j = 0; j < allMatrix[i].slotImages.Count; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, iconImages.Length - 1);
                allMatrix[i].slotImages[j].SetIcon(ID: randomIndex, image: iconImages[randomIndex]);
                //  allMatrix[i].slotImages[j].pos = (i * 10 + j);
            }
        }
    }


    internal void ResizeSlotMatrix(int levelCount)
    {

        if (levelCount > 0 && levelCount < 4)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                    slotMatrix[i].slotImages[4 - levelCount].iconImage.sprite = iconImages[UnityEngine.Random.Range(0, 5)];
                else
                    slotMatrix[i].slotImages[4 - levelCount].iconImage.sprite = iconImages[UnityEngine.Random.Range(5, 9)];
            }
        }

        watchAnimation.StopAnimation();

        level = levelCount;
        if (level == 1)
        {
            levelIndicator[0].gameObject.SetActive(true);
            levelIndicator[0].DOColor(Color.white, 1f);
            noOfWays.text = $"1024\nways";
        }
        else if (level == 2)
        {
            levelIndicator[1].gameObject.SetActive(true);
            levelIndicator[1].DOColor(Color.white, 1f);
            noOfWays.text = $"3125\nways";


        }
        else if (level == 3)
        {
            levelIndicator[2].gameObject.SetActive(true);
            levelIndicator[2].DOColor(Color.white, 1f);
            noOfWays.text = $"7776\nways";


        }
        else if (level == 4)
        {
            levelIndicator[3].gameObject.SetActive(true);
            levelIndicator[3].DOColor(Color.white, 1f);
            noOfWays.text = $"16807\nways";


        }
        else if (level == 0)
        {
            noOfWays.text = $"243\nways";

            foreach (var item in levelIndicator)
            {
                item.color = new Color(1, 1, 1, 0);
                item.gameObject.SetActive(false);
            }
        }
        Vector2 sizeDelta = mask_transform.sizeDelta;

        float iconHeight = sizeDelta.y / (3 + level);
        float iconWidth = iconHeight * 1.25f;

        sizeDelta.x = 5 * iconWidth;
        float reelHeight = 15 * iconHeight;
        initialPos = -(iconHeight * (3 + (level - 1) * 0.5f));

        tweenHeight = reelHeight + initialPos;

        mask_transform.DOSizeDelta(sizeDelta, 1f);
        bg_mask_transform.DOSizeDelta(sizeDelta, 1f);
        float offset = iconWidth * 2 + 35;
        bool animateSideBars = true;

        if (level == 4)
        {
            offset = 210;
            foreach (var item in horizontalBars)
            {
                item.sizeDelta = new Vector2(820, 40);
            }
            watchAnimation.StartAnimation();

        }
        else if (level > 0)
        {
            offset = iconWidth * 2 - (level - 1) * 20;
            watchAnimation.StartAnimation();

        }
        else
        {
            animateSideBars = false;
            foreach (var item in horizontalBars)
            {
                item.sizeDelta = new Vector2(890, 40);
            }
        }

        sideBars[0].DOLocalMoveX(offset, 1f);
        sideBars[1].DOLocalMoveX(-offset, 1f);

        if (animateSideBars)
        {
            foreach (var anim in sideBarsAnim)
            {
                anim.StopAnimation();
                anim.StartAnimation();
            }
        }

        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            int index = i;
            Slot_Transform[index].DOSizeDelta(new Vector2(iconWidth, reelHeight), 1f).OnUpdate(() =>
            {

                LayoutRebuilder.ForceRebuildLayoutImmediate(Slot_Transform[index]);

            });
            bg_slot_transform[index].DOSizeDelta(new Vector2(iconWidth, iconHeight * 7), 1f).OnUpdate(() =>
            {

                LayoutRebuilder.ForceRebuildLayoutImmediate(bg_slot_transform[index]);

            });
        }
        for (int i = 0; i < Slot_Transform.Length; i++)
        {
            Vector2 finalPos = new Vector2((i - Slot_Transform.Length / 2) * iconWidth, initialPos);
            Slot_Transform[i].DOLocalMove(finalPos, 1);
            bg_slot_transform[i].DOLocalMove(new Vector2(finalPos.x, -(iconHeight * (2 + (level - 1) * 0.5f))), 1);
        }

    }

    // internal void StartIconAnimation(List<List<string>> iconPos)
    // {
    //     // for (int i = 0; i < iconPos.Count; i++)
    //     // {
    //     //     for (int j = 0; j < iconPos[i].Count; j++)
    //     //     {

    //     //         int[] pos = iconPos[i][j].Split(',').Select(int.Parse).ToArray();
    //     //         slotMatrix[pos[0]].slotImages[pos[1]].StartAnim();
    //     //     }
    //     // }

    // }

    internal void StopIconAnimation()
    {

        foreach (var item in animatingIcons)
        {
            item.StopAnim();

        }

        animatingIcons.Clear();
    }

    internal void ResetAllIcons()
    {

        foreach (var item in slotMatrix)
        {
            foreach (var item1 in item.slotImages)
            {
                item1.Reset();
            }
        }
    }


    #region TweeningCode
    private void InitializeTweening(Transform slotTransform, bool turboMode)
    {
        float delay = 0.5f;



        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, delay).SetLoops(-1, LoopType.Restart).SetDelay(0).SetEase(Ease.Linear);
        alltweens.Add(tweener);
        // tweener.Play();
    }

    private void StopTweening(Transform slotTransform, int index, bool turboMode, bool immediateStop)
    {
        float delay = 0.2f;
        if (turboMode)
            delay = 0.1f;
        if (immediateStop)
            delay = 0;

        alltweens[index].Pause();
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 644.690002f + 190);
        alltweens[index] = slotTransform.DOLocalMoveY(644.690002f, delay).SetEase(Ease.OutBack);

    }


    private void KillAllTweens()
    {
        for (int i = 0; i < alltweens.Count; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion
    internal void AnimateLineWins(LineWin lineWins)
    {
        for (int i = 0; i < lineWins.positions.Count; i++)
        {


            slotMatrix[lineWins.positions[i]].slotImages[lineWins.pattern[i]].StartAnim();

        }
    }
    internal void StopAnimateLineWins(LineWin lineWins)
    {
        for (int i = 0; i < lineWins.positions.Count; i++)
        {


            slotMatrix[lineWins.positions[i]].slotImages[lineWins.pattern[i]].ResetLineAnim();

        }
    }
    internal void SetGoldenDarkActive(bool isTrue = false)
    {
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < slotMatrix[i].slotImages.Count; j++)
            {
                if (slotMatrix[i].slotImages[j].isGold)
                {
                    slotMatrix[i].slotImages[j].Dark.SetActive(isTrue);
                    slotMatrix[i].slotImages[j].Darkest.SetActive(isTrue);
                }
            }
        }
    }
    internal void SetDarkActive(bool isTrue = false, bool goldAlso = true)
    {
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < slotMatrix[i].slotImages.Count; j++)
            {
                if (goldAlso)
                {
                    slotMatrix[i].slotImages[j].Dark.SetActive(isTrue);
                    slotMatrix[i].slotImages[j].Darkest.SetActive(isTrue);
                }
                else
                {
                    if (!slotMatrix[i].slotImages[j].isGold)
                    {
                        slotMatrix[i].slotImages[j].Dark.SetActive(isTrue);
                        slotMatrix[i].slotImages[j].Darkest.SetActive(isTrue);
                    }
                }

            }
        }
    }

    internal void SetWildePosOff()
    {
        for (int i = 0; i < WildMatrix.Count; i++)
        {
            for (int j = 0; j < WildMatrix[i].slotImages.Count; j++)
            {
                WildMatrix[i].slotImages[j].StopAnimation();
                WildMatrix[i].slotImages[j].gameObject.SetActive(false);
            }
        }
    }

    internal IEnumerator PlayWheel(WheelBonus wheelBonus)
    {

        wheelPanel.SetActive(true);

        WheelView activeWheel = null;


        switch (wheelBonus.wheelType.ToLower())
        {
            case "small":
                activeWheel = smallWheel;
                smallWheel.gameObject.SetActive(true);
                break;

            case "medium":
                activeWheel = MediumWheel;
                MediumWheel.gameObject.SetActive(true);
                break;

            case "large":
                activeWheel = LargeWheel;
                LargeWheel.gameObject.SetActive(true);
                break;
        }

        if (activeWheel == null)
            yield break;
        activeWheel.transform.localRotation = Quaternion.identity;

        Debug.Log($"Wheel bonus----- index: {wheelBonus.featureType}" + wheelBonus.featureValue);
        activeWheel.targetIndex = FindTargetIndex(activeWheel, wheelBonus);

        //  Debug.Log($"Wheel stop index: {activeWheel.targetIndex}" + );


        yield return new WaitForSeconds(3f);


        yield return StartCoroutine(activeWheel.StopWheel());
        yield return new WaitForSeconds(0.5f);
        //  audioController.PlayWLAudio("wheel");

        Debug.Log("Wheel finished");
        yield return new WaitForSeconds(3f);
        activeWheel.gameObject.SetActive(false);
        wheelPanel.SetActive(false);

    }
    int FindTargetIndex(WheelView wheel, WheelBonus bonus)
    {
        foreach (var item in wheel.wheelItems)
        {
            if (item == null) continue;

            if (item.type.Equals(bonus.featureType, StringComparison.OrdinalIgnoreCase)
                && item.value == bonus.featureValue)
            {
                Debug.Log($"MATCH → index {item.index}");
                return item.index; // ✅ correct
            }
        }

        Debug.LogWarning("No matching wheel item found");
        return 0;
    }


}

[Serializable]
public class SlotImage
{
    public List<SlotIconView> slotImages = new List<SlotIconView>(10);
}
[Serializable]
public class WildImage
{
    public List<ImageAnimation> slotImages = new List<ImageAnimation>(10);
}

