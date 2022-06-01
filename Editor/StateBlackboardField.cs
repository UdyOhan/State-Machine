using System;
using UnityEngine.Events;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace StateMachine.Editor
{
    public class StateBlackboardField : BlackboardField
    {
        public ScriptableObject condition;
        public Action<ScriptableObject> action;
        public override void OnSelected()
        {
            base.OnSelected();
            action.Invoke(condition);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            action.Invoke(null);
        }
    }
}
