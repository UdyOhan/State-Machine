using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace StateMachine.Editor
{
    public class ReferenceNode : Node
    {
        public StateNode stateNode;
        public EdgeReference reference;
        
        const string HIGHLIGHT_STR = "node-highlight";
        StateGraphView graphView;
        public ReferenceNode(StateGraphView graphView, EdgeReference reference = null)
        {
            this.graphView = graphView;
            CreateInputPort();
            Refresh();
            var styleSheet = Resources.Load<StyleSheet>("Node");
            styleSheets.Add(styleSheet);
            var stateIndex = this.Q<DropdownField>().index;
         
            if(reference != null)
            {
                this.reference = reference;
                this.Q<DropdownField>().index = reference.StateIndex;
                stateIndex = reference.StateIndex;
                
                if (reference.EdgeIndices.Contains(-1))
                {
                    reference.EdgeIndices.RemoveAll(x => x == -1);
                }
            }
            else
            {
                this.reference = new EdgeReference
                {
                    StateIndex = stateIndex,
                    EdgeIndices = new List<int>(),
                    position = GetPosition().position,
                };
                
            }
            stateNode = graphView.GetStateNodeByIndex(stateIndex);
            if (stateNode != null)
            {
                title = stateNode.title;
            }

        }
        public void Refresh()
        {
            this.RefreshExpandedState();
            this.RefreshPorts();
        }

        private Port GeneratePort(Direction portDir, Port.Capacity capacity = Port.Capacity.Multi,
            Orientation orientation = Orientation.Horizontal)
        {
            return InstantiatePort(orientation, portDir, capacity, typeof(float)); //Arbituary type
        }

        void CreateInputPort(int inputIndex = 0,string name = "In")
        {
            var port = GeneratePort(Direction.Input, Port.Capacity.Multi);
            port.portName = name;
            inputContainer.Add(port);
            DropdownField field  = new DropdownField(); 
            field.choices = graphView.StateNames;
            field.index = inputIndex;
            field.RegisterValueChangedCallback(evt =>
            {
                if(stateNode != null)
                {
                  
                    
                    stateNode.RemoveFromClassList(HIGHLIGHT_STR);
                    
                    stateNode = graphView.GetStateNodeByIndex(field.index);
                    graphView.UpdateEdges(reference.EdgeIndices, stateNode.stateObj);
                    reference.StateIndex = field.index;
                    title = stateNode.title;
                    if (selected)
                    {
                        OnSelected();
                    }
                    
                }
                
            });
            field.style.width = 80;
            port.Add(field);
            Refresh();

        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (stateNode == null) return;
            stateNode.OnSelected();
            stateNode.AddToClassList(HIGHLIGHT_STR);
        }

        public override void OnUnselected()
        {

            reference.position = GetPosition().position;
            base.OnUnselected();
            if (stateNode == null) return;
            stateNode.OnUnselected();
            stateNode.RemoveFromClassList(HIGHLIGHT_STR);
        }



    }
}
