using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StateMachine{

    public abstract class State<T> : ScriptableObject
    {
        public abstract void OnStart(T obj);
        public abstract void OnUpdate(T obj);
        public abstract void OnEnd(T obj);
        public abstract void OnGizmos(T obj); 

#if UNITY_EDITOR
        [SerializeField, HideInInspector] public Vector2 statePosition;
        [SerializeField, HideInInspector] public string GUID;
#endif

    }


}

