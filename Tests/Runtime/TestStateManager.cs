using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace StateMachine
{
    [CreateAssetMenu(menuName = "State Manager/Sample", fileName = "New Sample")]
    public class TestStateManager : StateManager<MonoBehaviour>
    {
        public override Type GetStateType()
        {
            return typeof(TestBaseState);
        }

        public override Type GetConditionType()
        {
            return typeof(TestBaseCondition);
        }
    }

}
