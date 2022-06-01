using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace StateMachine{

    
    public class StateRunner<T> where T : Component
    {
        T controller;
        StateManager<T> stateManager;
        int currentIndex;
        int previousIndex;

        public int CurrentIndex => currentIndex;
        public int PreviousIndex => previousIndex;
        bool stateStated = false;

        public StateRunner(StateManager<T> manager, T controller)
        {
            this.controller = controller;
            stateManager = manager;
            currentIndex = manager.entryIndex;
            if(manager.EdgeMapCount == 0)
            {
                manager.CreateEdgeMap();
            }

        }
       
        
        public void Run()
        {
            var index = currentIndex;
            stateManager.Execute(controller, ref currentIndex, ref stateStated);
            if(index != currentIndex)
            {
                previousIndex = index;
            }
        }

        public void Gizmos()
        {
            stateManager.ExecuteGizmos(controller, currentIndex);
        }

        public void GizmosAll()
        {
            stateManager.ExecuteAllGizmos(controller);
        }

        public void Notify()
        {
            stateManager.selectedRunner = this;
        }
       

    }
}
