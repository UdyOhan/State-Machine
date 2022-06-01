using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace StateMachine.Editor{

    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private StateEditorWindow window;
        private StateGraphView graphView;
        private Type stateType;
        private Type conditionType;

        private Texture2D indentationIcon;

        public void Configure(StateEditorWindow window, StateGraphView graphView)
        {
            this.window = window;
            this.graphView = graphView;
            this.stateType = window.StateType;
            this.conditionType = window.ConditionType;

            //Transparent 1px indentation icon as a hack
            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            indentationIcon.Apply();
        }

        string ParseText(string name)
        {
            var matches = Regex.Matches(name, "([A-Z]+[a-z]*)|([0-9]+)");
            StringBuilder str = new StringBuilder();
            foreach (var match in matches)
            {
                str.Append(match.ToString());
                str.Append(' ');
            }
            return str.ToString();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create"), 0),
                new SearchTreeGroupEntry(new GUIContent("State"), 1),
            };
            
            var stateTypes = TypeCache.GetTypesDerivedFrom(window.StateType);
           
            foreach (var stateType in stateTypes)
            {
                var entry = new SearchTreeEntry(new GUIContent(ParseText(stateType.Name), indentationIcon))
                {
                    userData = new StateNode()
                    {
                        stateObj = CreateInstance(stateType),
                    },
                    level = 2,
                };
                searchTreeEntries.Add(entry);
            }

            searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent("Condition"), 1));
            var conditionTypes = TypeCache.GetTypesDerivedFrom(window.ConditionType);
            foreach (var conditionType in conditionTypes)
            {
                var entry = new SearchTreeEntry(new GUIContent(ParseText(conditionType.Name), indentationIcon))
                {
                    userData = new StateBlackboardField()
                    {
                        condition = CreateInstance(conditionType),
                    },
                    level = 2,
                };
                searchTreeEntries.Add(entry);
            }

            searchTreeEntries.Add(new SearchTreeEntry(new GUIContent("Reference", indentationIcon)){
                userData = typeof(ReferenceNode),
                level = 1,
            });
           
            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 mousePosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent,
                context.screenMousePosition - window.position.position);
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(mousePosition);
            switch (SearchTreeEntry.userData)
            {
                case StateNode stateNode:
                    graphView.GenerateNewStateNode(stateNode.stateObj.GetType(), graphMousePosition);
                    return true;
                case StateBlackboardField stateCondition:
                    graphView.GenerateCondition(stateCondition.condition.GetType());
                    return true;
                case Type type:
                    if(type == typeof(ReferenceNode))
                    {
                        graphView.GenerateReferenceNode(graphMousePosition);
                    }
                   
                    return true;
                    
            }
            return false;
        }
    }
}
