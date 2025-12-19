using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlotIconView : MonoBehaviour
{
    [Header("required fields")]
    [SerializeField] internal int pos;
    [SerializeField] internal int id = -1;
    [SerializeField] internal Image iconImage;
    [SerializeField] internal Image goldenIconImage;
    [SerializeField] internal ImageAnimation bgImage;
    [SerializeField] internal Image goldenBgImage;
    [SerializeField] internal GameObject Dark;
    [SerializeField] internal GameObject Darkest;
    [SerializeField] internal bool isGold;
    [SerializeField] internal Image border;
    [SerializeField] internal ImageAnimation activeanimation;

    Tween iconAnim;

    internal void SetIcon(Sprite image, int ID)
    {
        iconImage.sprite = image;
        id = ID;
        bgImage.gameObject.SetActive(true);
    }

    internal void SetGoldIcon(Sprite image)
    {
        isGold = true;
        goldenIconImage.sprite = image;
        goldenBgImage.gameObject.SetActive(true);
        goldenIconImage.gameObject.SetActive(true);
        Dark.SetActive(false);
        Darkest.SetActive(false);


    }

    internal void AnimateGoldIcons()
    {
        goldenIconImage.DOFade(0, 0.5f);
    }

    internal void Reset()
    {
        isGold = false;
        goldenBgImage.gameObject.SetActive(false);
        goldenIconImage.gameObject.SetActive(false);
        goldenIconImage.color = new Color(1, 1, 1, 1);
        bgImage.StopAnimation();
    }
    internal void StartAnim()
    {
        if (!isGold)
        {
            Dark.SetActive(false);
            Darkest.SetActive(false);
            border.gameObject.SetActive(true);
            bgImage.StartAnimation();

        }

    }
    internal void ResetLineAnim()
    {
        if (!isGold)
        {
            Dark.SetActive(true);
            Darkest.SetActive(false);
            border.gameObject.SetActive(false);
            bgImage.StopAnimation();
        }
    }
    internal void StopAnim()
    {
        iconAnim?.Kill();
        iconImage.transform.localScale = Vector3.one;
        border.gameObject.SetActive(false);
        // activeanimation.StopAnimation();
        bgImage.StopAnimation();
        // Sprite firstSprite = activeanimation.textureArray[0];
        activeanimation.textureArray.Clear();
        // activeanimation.textureArray.Add(firstSprite);


    }

}
