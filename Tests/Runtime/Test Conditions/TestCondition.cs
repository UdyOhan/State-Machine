using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCondition : TestBaseCondition
{
   public List<int> intList = new List<int>();

    public override bool Check(MonoBehaviour value)
    {
        return true;
    }
}
