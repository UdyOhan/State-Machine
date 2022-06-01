using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StateMachineExample : MonoBehaviour
{
    public SampleTransformStateManager stateManager;
    public StateRunner<Transform> runner;
    Dictionary<string, object> extraData = new Dictionary<string, object>();

    private void Start()
    {
        runner = new StateRunner<Transform>(stateManager, transform);
        
    }

    public void Update()
    {
        runner.Run();
#if UNITY_EDITOR
        if(Selection.activeGameObject == gameObject)
        {
            runner.Notify();

            Debug.Log("Noti");
        }
#endif
    }

    public void OnDrawGizmos()
    {
        if (runner == null) return;
        runner.Gizmos();
    }
}
