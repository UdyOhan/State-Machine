using System;
using System.Collections;
using System.Collections.Generic;
using StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "State Machine/Transform Sample", menuName = "New Transform Sample")]
public class SampleTransformStateManager : StateManager<Transform>
{

    public override Type GetStateType()
    {
        return typeof(SampleTransformBaseState);
    }

    public override Type GetConditionType()
    {
        return typeof(SampleTransformBaseCondition);
    }
}
