using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachine;

public class TestState : TestBaseState
{
    public List<int> number = new List<int>();
    
    public override void OnEnd(MonoBehaviour obj)
    {
        Debug.Log($"The is the end {this.name}");
    }

    public override void OnGizmos(MonoBehaviour obj)
    {
        throw new System.NotImplementedException();
    }

    public override void OnStart(MonoBehaviour obj)
    {
        Debug.Log($"The is the start {this.name}");
    }

    public override void OnUpdate(MonoBehaviour obj)
    {
        Debug.Log($"The is the update {this.name}");
    }



}
