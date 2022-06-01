using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StateMachine {

    public abstract class  Condition<M> : ScriptableObject
    {
        public abstract bool Check(M value);
    }

}
