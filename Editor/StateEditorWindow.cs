using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Callbacks;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace StateMachine.Editor
{

    public class AssetHandler
    {
        
        [OnOpenAsset()]
        public static bool OpenEditor(int instance, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instance) as ScriptableObject;
            
            if(obj == null) { return false; }

            var getStateMethodInfo = obj.GetType().GetMethod("GetStateType");
            var getConditionMethodInfo = obj.GetType().GetMethod("GetConditionType");

            if(getStateMethodInfo == null || getStateMethodInfo == null) { return false; }

            Type stateType = getStateMethodInfo.Invoke(obj, null) as Type;
            Type conditionalType = getConditionMethodInfo.Invoke(obj, null) as Type;

            if(stateType == null || conditionalType == null) { return false; }

            StateEditorWindow.Open(obj, stateType, conditionalType);

            return true;
           
        }
    }

    public class StateEditorWindow : EditorWindow
    {

        private StateGraphView graphView;
        ScriptableObject stateManager;
        private Type stateType;
        private Type conditionType;
        FieldInfo positionFieldInfo;
        FieldInfo guidFieldInfo;
        public Type StateType => stateType;
        public Type ConditionType => conditionType;
        public static StateEditorWindow Instance;

        LinkedList<int> stateIndexList = new LinkedList<int>();

        Func<int?> GetCurrentIndex;
        Func<int?> GetPreviousIndex;
        bool allNodesMarkedUnvisited = false;

        int lastLeft = -1;

        

        
        public static void Open(ScriptableObject stateManager, Type stateType, Type conditionType)
        {
            StateEditorWindow wnd = GetWindow<StateEditorWindow>();
            wnd.titleContent = new GUIContent("AI State Editor");
            wnd.stateType = stateType;
            wnd.conditionType = conditionType;
            wnd.stateManager = stateManager;
            wnd.LoadData();

        }

        public void OnEnable()
        {
            Instance = this;
            stateIndexList.Clear();
        }
        public void CreateGUI()
        {
            var inspectorView = CreateInspectorView();
            var splitView = CreateSplitView();
            var graphView = CreateGraphView();
            var toolbar = CreateToolbar();

            var leftView = new VisualElement();
            var rightView = new VisualElement();
            inspectorView.StretchToParentSize();
            leftView.Add(inspectorView);
            rightView.Add(graphView);

            splitView.Add(leftView);
            splitView.Add(rightView);
            graphView.ElementSelectionAction = inspectorView.DisplayElement;

            splitView.fixedPaneIndex = 0;
            splitView.fixedPaneInitialDimension = 200;

            splitView.orientation = TwoPaneSplitViewOrientation.Horizontal;

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(splitView);

            if (stateManager)
            {
                LoadData();
            }
        }

        public void OnInspectorUpdate()
        {
            if (!Application.isPlaying) return;
            allNodesMarkedUnvisited = false;
            var currentIndex = GetCurrentIndex?.Invoke();

            if (!currentIndex.HasValue) return;

            var currentStateIndex = currentIndex.Value;
            graphView.SetStateAsVisited(currentStateIndex);

            var previousIndex = GetPreviousIndex?.Invoke();
            if(!previousIndex.HasValue || previousIndex.Value == currentStateIndex) return;

            var previousStateIndex = previousIndex.Value;
            graphView.SetStateAsLeft(previousStateIndex);

            if (lastLeft == previousIndex) return;

            if(lastLeft != -1)
                graphView.SetStateAsUnvisited(lastLeft);

            lastLeft = previousStateIndex;


        }

        public void OnGUI()
        {
            if(Application.isPlaying || allNodesMarkedUnvisited) return;
            graphView.SetAllUnvisited();
            allNodesMarkedUnvisited = true;
        }


        InspectorView CreateInspectorView()
        {
            return new InspectorView();
        }
        

        SplitView CreateSplitView()
        {
            SplitView splitView = new SplitView();
            return splitView;
        }
       
        StateGraphView CreateGraphView()
        {
            graphView = new StateGraphView();
            graphView.StretchToParentSize();
            var styleSheet = Resources.Load<StyleSheet>("AIStateEditor");
            graphView.styleSheets.Add(styleSheet);
            return graphView;
        }

        Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar();
            var btn = new Button(() => LoadData());
            btn.text = "Reload";
            toolbar.Add(btn);
            
            return toolbar;
        }

      
        public void LoadData()
        {
            IEditorStateManager<Component> manager = stateManager as IEditorStateManager<Component>;

            var getStateMethodInfo = stateManager.GetType().GetMethod("GetStateType");
            var getConditionMethodInfo = stateManager.GetType().GetMethod("GetConditionType");
            var stateObjects = manager.States;
            var edgeObjects = manager.Edges;

           
            graphView.ClearGraph();
            graphView.AddSearchWindow(this);

            stateType = getStateMethodInfo.Invoke(stateManager, null) as Type;
            conditionType = getConditionMethodInfo.Invoke(stateManager, null) as Type;

            guidFieldInfo = manager.GetStateType().GetField("GUID");
            positionFieldInfo = manager.GetStateType().GetField("statePosition");

            #region graphview-events
            graphView.StateAdditionAction = stateToAdd =>
            {
                AssetDatabase.AddObjectToAsset(stateToAdd, this.stateManager);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var localManager = manager;
                var localStates = localManager.States;
                Array.Resize(ref localStates, localStates.Length + 1);
                localStates[localStates.Length - 1] = stateToAdd;
                localManager.States = localStates;
            };

            graphView.StateRemovalAction = stateToRemove =>
            {
                var localManager = manager;
                var localStates = localManager.States;
                var localStatesList = new List<object>(localStates);
                localStatesList.Remove(stateToRemove);
                localManager.States = localStatesList.ToArray();

                graphView.ElementSelectionAction(null);
                AssetDatabase.RemoveObjectFromAsset(stateToRemove);
                DestroyImmediate(stateToRemove, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };

            graphView.NodeNotifyEdgeChangeAction =  (prevState, nextState, previousIndex, newIndex) =>
            {
                var prevStateIndex = graphView.allStates.FindIndex( obj => obj == prevState);
                var nextStateIndex = graphView.allStates.FindIndex( obj => obj == nextState);
                var edge = new Edge()
                {
                    prevStateIndex = prevStateIndex,
                    conditionIndex = previousIndex,
                    nextStateIndex = nextStateIndex,
                };
                var edgeIndex = manager.Edges.IndexOf(edge);
                if (edgeIndex >= 0)
                {
                    edgeObjects[edgeIndex] = new Edge
                    {
                        prevStateIndex = prevStateIndex,
                        conditionIndex = newIndex,
                        nextStateIndex = nextStateIndex,
                    };
                }
            };

            graphView.EdgeRemovalAction = edge =>
            {
                var managerEdges = manager.Edges;
                if (managerEdges.Contains(edge))
                {
                    managerEdges.Remove(edge);
                }
                manager.Edges = managerEdges;
            };

            graphView.AddEdgeAction = edge =>
            {
                var managerEdges = manager.Edges;
                managerEdges.Add(edge);
                manager.Edges = managerEdges;
            };

            graphView.GetEdgeIndex = edge =>
            {
                var managerEdges = manager.Edges;
                return managerEdges.IndexOf(edge);
            };

            graphView.GetUniqueEdgeIndex = (edgeIndices, edge) =>
            {
                var managerEdges = manager.Edges;
                for(int i = 0; i < managerEdges.Count; i++)
                {
                    var localEdge = managerEdges[i];
                    if (localEdge != edge) continue;
                    if (edgeIndices.Contains(i)) continue;
                    return i;
                }
                return -1;
            };
            graphView.AddConditionAction = condition =>
            {
                AssetDatabase.AddObjectToAsset(condition, this.stateManager);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var conditions = manager.Conditions;
                List<ScriptableObject> condList  = new List<object>(conditions).ConvertAll(x => x as ScriptableObject);
                condList.Add(condition);
                manager.Conditions = condList.ToArray();
            };

            graphView.RemoveConditionAction = condition =>
            {
                var condList = new List<object>(manager.Conditions).ConvertAll(x => x as ScriptableObject);
                condList.Remove(condition);
                manager.Conditions = condList.ToArray();
                DestroyImmediate(condition, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };

            graphView.MakeEntryStateAction = state =>
            {
                var states = new List<object>(manager.States).ConvertAll(x => x as ScriptableObject);
                int stateIndex = states.IndexOf(state);
                manager.EntryIndex = stateIndex;
            };

            graphView.RemoveEntryStateAction = () =>
            {
                manager.EntryIndex = -1;
            };

            graphView.AddEdgeReference = (edgeRef) =>
            {
                manager.EdgeReferences.Add(edgeRef);
            };

            graphView.RemoveEdgeReference = (edgeRef) =>
            {
                manager.EdgeReferences.Remove(edgeRef);
            };

            graphView.UpdateEdgeAction = (edgeIndex, stateObj) =>
            {
                var edges = manager.Edges;
                var stateIndex = graphView.allStates.IndexOf(stateObj);
                var edge = edges[edgeIndex];
                edge.nextStateIndex = stateIndex;
                edges[edgeIndex] = edge;
                manager.Edges = edges;
            };

            graphView.GetEdgeList = () => manager.Edges;

            graphView.UpdateEdgeByIndex = (index, edge) =>
            {
                var localEdges = manager.Edges;
                localEdges[index] = edge;
                manager.Edges = localEdges;
            };
            #endregion

            var conditions = manager.Conditions;
            foreach(var condition in conditions)
            {
                var scriptObj = condition as ScriptableObject;
                graphView.allConditions.Add(scriptObj);
            }

            

            int entryIndex = manager.EntryIndex;

            Type type = manager.GetStateType();

            foreach (var stateObj in stateObjects)
            {
                var state = stateObj as ScriptableObject;
                var guid = (string)guidFieldInfo.GetValue(state);
                if (string.IsNullOrEmpty(guid))
                {
                    guidFieldInfo.SetValue(state, GUID.Generate().ToString());
                }

            }


            PopulateGraph(edgeObjects, stateObjects, manager.EdgeReferences,entryIndex);
            graphView.LoadConditionIntoBlackboard();
            graphView.loaded = true;
            GetCurrentIndex = () => manager.GetRunnerCurrentIndex;
            GetPreviousIndex = () => manager.GetRunnerPreviousIndex;

            // Rearrange(edgeObjects, stateObjects);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            
        }

       

       

        public void PopulateGraph(List<Edge> edgeObjects, object[] stateObjects, List<EdgeReference> edgeRefs,int entryIndex)
        {

            Dictionary<int, List<int>> stateEdgeIndexMap = new Dictionary<int, List<int>>();
            for(int i = 0; i < edgeObjects.Count; i++){
                var e = edgeObjects[i];
                if (stateEdgeIndexMap.ContainsKey(e.prevStateIndex))
                {
                    stateEdgeIndexMap[e.prevStateIndex].Add(i);
                }
                else
                {
                    stateEdgeIndexMap.Add(e.prevStateIndex, new List<int>() { i});
                }
            }
         
            //Create the nodes
            List<StateNode> nodes = new List<StateNode>(stateObjects.Length);
            List<ReferenceNode> references = new List<ReferenceNode>(edgeRefs.Count);

            for(int i = 0; i < stateObjects.Length; i++)
            {
                var state = stateObjects[i] as ScriptableObject;
                var guid = (string)guidFieldInfo.GetValue(state);
                var pos = (Vector2)positionFieldInfo.GetValue(state);

                var node = graphView.LoadStateNode(state, guid, pos);
               
                nodes.Add(node);
                
            }

            for(int i = 0; i < edgeRefs.Count; i++)
            {
                var edgeRef = edgeRefs[i];
                var reference = graphView.GenerateReferenceNode(edgeRef.position, edgeRef, false);
                references.Add(reference);
            }
            

            //Connect the nodes
            foreach(var pair in stateEdgeIndexMap)
            {
                var stateNode = nodes[pair.Key];
                int count = 0;
                foreach(var edgeIndex in pair.Value)
                {
                    var edge = edgeObjects[edgeIndex];
                    stateNode.CreateOutputPort(edge.conditionIndex);
                    var refNode = references.Find(node =>
                    {

                        var stateEdgeIndex = edgeIndex;

                        var edgeRef = node.reference;
                        return edgeRef.EdgeIndices.Contains(stateEdgeIndex);
                    });
                    if(refNode != null)
                    {
                        graphView.ConnectEdge(stateNode, refNode, count);
                      
                    }
                    else
                    {
                        var nextNode = nodes[edge.nextStateIndex];
                        graphView.ConnectEdge(stateNode, nextNode, count);
                    }
                   
                    count++;
                }
            }

            if(entryIndex < 0 || entryIndex >= stateObjects.Length) { return; }
            var entryNodeState = nodes[entryIndex];
            graphView.MakeNodeEntryState(entryNodeState);

            foreach(var node in nodes)
            {
                node.Refresh();
            }
          
        }

        
    }

}