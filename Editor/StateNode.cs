using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Events;
using UnityEditor;
using System;


namespace StateMachine.Editor
{
    public class StateNode : Node
    {
        public string GUID;
        public ScriptableObject stateObj;

        StateGraphView graphView;
        public override string title { 
            get => base.title; 
            set 
            { 
                base.title = value;
                var nameField = this.Q<TextField>();
                if(nameField != null)
                    nameField.SetValueWithoutNotify(title);
            } 
        }

        public StateNode(StateGraphView graphView = null)
        {
            this.graphView = graphView;
            if(graphView != null)
            {
                PopulateSelf();
                Refresh();
            }
            var stylesheets = Resources.Load<StyleSheet>("Node");
            styleSheets.Add(stylesheets);
        }

        private Port GeneratePort(Direction portDir, Port.Capacity capacity = Port.Capacity.Multi,
            Orientation orientation = Orientation.Horizontal)
        {
            return InstantiatePort(orientation, portDir, capacity, typeof(float)); //Arbituary type
        }

        public void Refresh()
        {
            this.RefreshExpandedState();
            this.RefreshPorts();
        }

        public void CreateInputPort(string name = "Input")
        {
            var port = GeneratePort(Direction.Input, Port.Capacity.Multi);
            port.portName = name;
            inputContainer.Add(port);
        }

        public void CreateOutputPort(int outputIndex = 0, string portName = "Output")
        {
            var port = GeneratePort(Direction.Output, Port.Capacity.Single);
            var portLabel = port.contentContainer.Q<Label>();
            port.contentContainer.Remove(portLabel);

            port.contentContainer.Add(new Label("  "));
         
              
            DropdownField dropdown = new DropdownField();
            dropdown.style.width = 80;
            dropdown.choices = graphView.ConditionNames;
            dropdown.index = outputIndex;

            dropdown.RegisterValueChangedCallback(evt =>
            {

                var previousIndex = dropdown.choices.IndexOf(evt.previousValue);


                var index = dropdown.choices.IndexOf(evt.newValue);
                dropdown.index = index;
                if (port.connected)
                {

                    var endState = graphView.GetEndNodeForNode(this, port);
                    var edgeIndex = graphView.GetEdgeIndexByPort(port, endState, out Edge edge, previousIndex);

                    edge.conditionIndex = index;



                    if (endState != null && edgeIndex != -1)
                    {
                        graphView.UpdateEdgeByIndex(edgeIndex, edge);
                    }
                }

                
            });

            port.contentContainer.Add(dropdown);

            var deleteButton = new Button(() => RemovePort(port))
            {
                text = "X",
            };
            deleteButton.name = "delete";
            port.contentContainer.Add(deleteButton);

            port.portName = portName;
            outputContainer.Add(port);
            port.capabilities |= Capabilities.Selectable;
            Refresh();
        }

        private void RemovePort(Port port)
        {

            graphView.RemoveEdgeWithPort(this, port);
            outputContainer.Remove(port);
            RefreshPorts();
            Refresh();
        }

        

        public void PopulateSelf()
        {
            Button btn = new Button( () => CreateOutputPort());
            btn.text = "Add";
            btn.name = "add";
            titleButtonContainer.Add(btn);

            TextField nameField = new TextField();
            nameField.RegisterValueChangedCallback(evt =>
            {
                this.stateObj.name = evt.newValue;
                this.title = evt.newValue;
            });
            mainContainer.Add(nameField);

            nameField.SetValueWithoutNotify(title);

            CreateInputPort();
            Refresh();
        }

        public void UpdateDropDown(List<string> choices)
        {

            var fields = this.Query<UnityEngine.UIElements.DropdownField>();
            if(fields == null) return;
            fields.ToList().ForEach(field =>
            {
                var index = Mathf.Clamp(field.index, 0, choices.Count - 1);
                field.choices = choices;
                field.value = choices[index];
            });
        }

        public override void OnSelected()
        {
            base.OnSelected();
            
            graphView.ElementSelectionAction(stateObj);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            var pos = GetPosition();
            if(stateObj)
                stateObj.GetType().GetField("statePosition").SetValue(stateObj, new Vector2(pos.x, pos.y));
            graphView.ElementSelectionAction(null);
        }
        
      
    }

   
}
