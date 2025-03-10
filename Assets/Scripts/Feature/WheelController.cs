
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WheelController : MonoBehaviour
{

    [SerializeField] WheelView[] wheels;

    internal IEnumerator  StartWheel(){
        yield return null;
    }
    internal void PopulateWheels(List<List<double>> wheelData){

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].PopulateValues(wheelData[i]);
        }

    } 
}