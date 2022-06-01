using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SampleKeyCondition : SampleTransformBaseCondition
{
    float lastValue = 0;
    public bool useTimer = false;
    public KeyCode key;
    
    public override bool Check(Transform value)
    {
        if (useTimer)
        {
            var currentValue = (Time.time * 1000) % 10000;
            if (lastValue > currentValue)
            {
                lastValue = currentValue;
                return true;
            }
            Debug.Log(currentValue + " " + lastValue);
            lastValue = currentValue;
        }
       
        return Input.GetKeyDown(key);
        
        
    }
}
