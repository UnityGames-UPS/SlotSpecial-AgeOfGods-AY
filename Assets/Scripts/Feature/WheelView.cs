using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
public class WheelView : MonoBehaviour
{
    public int segment = 24;
    Tween rotationTween;
    public int targetIndex;
    internal int type=0;
    [SerializeField] internal WheelItem[] wheelItems;
    void Start()
    {
        rotationTween ??= transform.DOLocalRotate(new Vector3(0, 0, -360), 7f, RotateMode.FastBeyond360)
      .SetLoops(-1, LoopType.Incremental)
      .SetEase(Ease.Linear);
    }

    void Update()
    {
        // if(Input.GetMouseButtonDown(0))
        // StartCoroutine(StopWheel());
    }

    internal void PopulateValues(List<double> values){

        for (int i = 2; i < wheelItems.Length; i++)
        {
            wheelItems[i].value=values[i];
            if(i<4)
            wheelItems[i].valueText.text=wheelItems[i].value.ToString("f0");
            else if(i<6)
            wheelItems[i].valueText.text="+"+wheelItems[i].value.ToString("f0");
            else
            wheelItems[i].valueText.text="X"+wheelItems[i].value.ToString();
        }
    }
    IEnumerator StopWheel()
    {
        rotationTween.Kill();

        float currentAngle = transform.localEulerAngles.z % 360f;
        float targetAngle = -targetIndex * (360f / segment);

        // Ensure the rotation always goes forward (clockwise)
        if (targetAngle < currentAngle)
        {
            targetAngle += -360f;
        }

        transform.DOLocalRotate(new Vector3(0, 0, targetAngle), 5f, RotateMode.FastBeyond360);

        yield return new WaitForSeconds(1f);
    }

}

