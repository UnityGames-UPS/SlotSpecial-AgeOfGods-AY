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
    [SerializeField] internal Image bgImage;
    [SerializeField] internal Image goldenBgImage;
    [SerializeField] internal bool isGold;
    [SerializeField] internal Image border;
    [SerializeField] internal ImageAnimation activeanimation;

    Tween iconAnim;

    internal void SetIcon(Sprite image,int ID){
        iconImage.sprite=image;
        id=ID;
    }

    internal void SetGoldIcon(Sprite image){
        isGold=true;
        goldenIconImage.sprite=image;
        goldenBgImage.gameObject.SetActive(true);
        goldenIconImage.gameObject.SetActive(true);


    }

    internal void AnimateGoldIcons(){
        goldenIconImage.DOFade(0,0.5f);
    }

    internal void Reset(){
        isGold=false;
        goldenBgImage.gameObject.SetActive(false);
        goldenIconImage.gameObject.SetActive(false);
        goldenIconImage.color= new Color(1,1,1,1);
    }
    internal void StartAnim()
    {
        if(!isGold){
            iconAnim=iconImage.transform.DOScale(0.7f,0.5f).SetLoops(-1,LoopType.Yoyo);
            border.gameObject.SetActive(true);
        }
        // if(animSprite.Count==0 )
        // {
        //     Debug.Log("no anim sprite");
        //     return;
        // }
        // activeanimation.textureArray.Clear();
        // activeanimation.textureArray.AddRange(animSprite);
        // activeanimation.AnimationSpeed = animSprite.Count;
        // if(activeanimation.textureArray.Count==0)
        //         {
        //     Debug.Log("no anim sprite");
        //     return;
        // }
        // if (id < 6 || id == 11)
        // {
        //     activeanimation.rendererDelegate = circleImage;

        // }
        // else if (id >= 6 && id < 8)
        // {
        //     activeanimation.rendererDelegate = borderImage;

        // }
        // else if (id >= 8 & id < 11)
        // {
        //     activeanimation.rendererDelegate = iconImage;

        // }
        // activeanimation.StartAnimation();

    }

    internal void StopAnim()
    {
        iconAnim?.Kill();
        iconImage.transform.localScale=Vector3.one;
        border.gameObject.SetActive(false);
        // activeanimation.StopAnimation();

        // Sprite firstSprite = activeanimation.textureArray[0];
        activeanimation.textureArray.Clear();
        // activeanimation.textureArray.Add(firstSprite);


    }

}
