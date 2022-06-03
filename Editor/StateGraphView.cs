using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Events;
using UnityEditor;
using NodeEdge = UnityEditor.Experimental.GraphView.Edge;


namespace StateMachine.Editor
{
    
    public class StateGraphView : GraphView
    {
        private readonly Vector2 defaultNodeSize = new Vector2(100, 200);
        public const string ENTRYNODE_NAME = "ENTRY";
        public const string REFERENCE_NAME = "REFERENCE";
        public Action<ScriptableObject> ElementSelectionAction;
        public NodeSearchWindow searchWindow;
        public bool loaded = false;

        public List<ScriptableObject> allConditions = new List<ScriptableObject>();
        public List<ScriptableObject> allStates = new List<ScriptableObject>();
        public Action<Edge> EdgeRemovalAction;
        public Action<Edge> AddEdgeAction;
        public Func<Edge, int> GetEdgeIndex;
        public Func<List<int>, Edge, int> GetUniqueEdgeIndex;
        public Action<ScriptableObject, ScriptableObject, int, int> NodeNotifyEdgeChangeAction;
        public Action<int, Edge> UpdateEdgeByIndex;
        public Action<ScriptableObject> StateAdditionAction;
        public Action<ScriptableObject> StateRemovalAction;
        public Action<ScriptableObject> AddConditionAction;
        public Action<ScriptableObject> RemoveConditionAction;
        public Action<ScriptableObject> MakeEntryStateAction;
        public Action RemoveEntryStateAction;
        public Action<EdgeReference> AddEdgeReference;
        public Action<EdgeReference> RemoveEdgeReference;
        public Action<int, ScriptableObject> UpdateEdgeAction;
        public Func<List<Edge>> GetEdgeList;


        public List<string> ConditionNames
        {
            get
            {
                List<string> result = new List<string>(allConditions.Count);
                for(int i = 0; i < allConditions.Count; i++)
                {
                    result.Add(allConditions[i].name);
                }
                return result;
            }
        }

        public List<string> StateNames
        {
            get
            {
                List<string> result = new List<string>(allStates.Count);
                for (int i = 0; i < allStates.Count; i++)
                {
                    result.Add(allStates[i].name);
                }
                return result;
            }
        }
        Blackboard blackboard;

        public StateGraphView()
        {
            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
            
            AddElement(GenerateEntryPointNode());
            blackboard = CreateBlackBoard();
            graphViewChanged += OnEdgeCreated;
            graphViewChanged += OnEdgeRemoved;
            graphViewChanged += OnNodeRemoved;
            Add(blackboard);
        }

        public void AddSearchWindow(StateEditorWindow window)
        {
            searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindow.Configure(window, this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);

        }

        public void SetAllUnvisited()
        {
            var stateNodes = nodes.ToList().ConvertAll(x => x as StateNode);
            foreach(var stateNode in stateNodes)
            {
                if(stateNode != null)
                {
                    stateNode.name = "";
                }
            }
        }

      
        public void RemoveState(ScriptableObject obj)
        {
            StateRemovalAction(obj);
            allStates.Remove(obj);
        }

        #region edges-and-ports
        public int FindInputNodeEdgeIndex(Node node)
        {
            var edgeList = edges.ToList();
            var edgeIndex = -1;
            foreach(var edge in edgeList)
            {
                if(edge.input.node == node)
                {

                }
            }
            return edgeIndex;
        }

       
        Edge? GetStateEdgeFomNodeEdge(NodeEdge edge)
        {
            Node prevNode = edge.output.node;
            if (prevNode.name == ENTRYNODE_NAME)
                return null;

            StateNode prevStateNode = prevNode as StateNode;

            Node nextNode = edge.input.node;
            StateNode nextStateNode = null;
            if (nextNode.name == REFERENCE_NAME)
                nextStateNode = (nextNode as ReferenceNode).stateNode;
            else
                nextStateNode = nextNode as StateNode;

            var conditionIndex = edge.output.Q<DropdownField>().index;

            var stateEdge = new Edge
            {
                prevStateIndex = allStates.IndexOf(prevStateNode.stateObj),
                nextStateIndex = allStates.IndexOf(nextStateNode.stateObj),
                conditionIndex = conditionIndex,
            };
            return stateEdge;
        }


        public GraphViewChange OnEdgeCreated(GraphViewChange change)
        {
            var stateNodes = nodes.ToList().ConvertAll(x => x as StateNode);

            //A common function for both scenarios
           
            if(change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output.node.name == ENTRYNODE_NAME) {
                        MakeEntryStateAction((edge.input.node as StateNode).stateObj);
                        continue; 
                    }

                    var stateEdge = GetStateEdgeFomNodeEdge(edge);

                    if (!stateEdge.HasValue) { continue; }

                    AddEdgeAction(stateEdge.Value);

                    if (edge.input.node.name == REFERENCE_NAME) { 
                        var edgeList = new List<int>();
                        foreach(var node in nodes)
                        {
                            if (node.name == REFERENCE_NAME) {
                                var localRefNode = node as ReferenceNode;
                                edgeList.AddRange(localRefNode.reference.EdgeIndices.ToArray());
                            }
                        }
                        var refNode = edge.input.node as ReferenceNode;
                        refNode.reference.EdgeIndices.Add(GetUniqueEdgeIndex(edgeList,stateEdge.Value));
                    }
                  
                }

            }
          
            return change;
        }

        public GraphViewChange OnNodeRemoved(GraphViewChange change)
        {
            if (change.elementsToRemove == null) return change;
            
            var elementsRemoved = change.elementsToRemove;
            foreach (var element in elementsRemoved)
            {
                switch (element)
                {
                    case StateNode node:
                        {
                            RemoveState(node.stateObj);
                        }
                        break;
                    case ReferenceNode refNode:
                        {
                            RemoveEdgeReference(refNode.reference);
                        }
                        break;
                }
            }

            return change;
        }

        public GraphViewChange OnEdgeRemoved(GraphViewChange change)
        {
            if (change.elementsToRemove == null) return change;

            var elementsRemoved = change.elementsToRemove;
            foreach (var element in elementsRemoved)
            {
                switch (element)
                {
                    case NodeEdge edge:
                        {
                            if (edge.output.node != null)
                                if (edge.output.node.name == ENTRYNODE_NAME)
                                {

                                    RemoveEntryStateAction();
                                    continue;
                                }

                            var stateEdge = GetStateEdgeFomNodeEdge(edge);

                            if (!stateEdge.HasValue) { continue; }


                            foreach (var node in nodes) 
                            {
                                if (node.name == REFERENCE_NAME && edge.input.node != node)
                                {
                                    var refNode = node as ReferenceNode;
                                    var index = GetEdgeIndex(stateEdge.Value);
                                    refNode.reference.ShiftEdgeIndicesGreaterThan(index);
                                }
                            }

                            if (edge.input.node.name == REFERENCE_NAME)
                            {
                                var refNode = edge.input.node as ReferenceNode;
                                var index = GetEdgeIndex(stateEdge.Value);

                                refNode.reference.EdgeIndices.Remove(index);
                                refNode.reference.ShiftEdgeIndicesGreaterThan(index);
                            }

                            EdgeRemovalAction(stateEdge.Value);

                        }
                        break;
                }
            }
            return change;
        }

        public int GetEdgeIndexByPort(Port port, StateNode nextNode, out Edge edge, int previousIndex)
        {
           
            int prevIndex = previousIndex;
            var portList = port.node.outputContainer.Query<Port>().ToList();
            var prevNode = port.node as StateNode;
            edge = new Edge()
            {
                conditionIndex = prevIndex,
                prevStateIndex = allStates.IndexOf(prevNode.stateObj),
                nextStateIndex = allStates.IndexOf(nextNode.stateObj),

            };

            int count = 0;
            foreach (var localPort in portList)
            {
                if (localPort == port)
                    break;

                if (!localPort.connected) continue;

                var dropdown = localPort.Q<DropdownField>();

                if (dropdown == null) continue;

                if (dropdown.index != prevIndex) continue;

                var localPortEnumerator = localPort.connections.GetEnumerator();
                localPortEnumerator.MoveNext();
                var portEnumerator = port.connections.GetEnumerator();
                portEnumerator.MoveNext();
                if (localPortEnumerator.Current.input.node == portEnumerator.Current.input.node)
                {
                    count++; 
                }
                
            }

            var edgeList = GetEdgeList();
            int count2 = 0;
            int result = 0;
            foreach(var localEdge in edgeList)
            {
                if(localEdge == edge)
                {
                    if(count2 == count)
                    {
                        return result;
                    }
                    else
                    {
                        count2++;
                    }
                }
                result++;
            }
            return -1;
        }
       

        public StateNode GetEndNodeForNode(StateNode node, Port port)
        {
            

            var edgeList = edges.ToList();
            var edgeIndex = edges.ToList().FindIndex(edge =>
            {
                return edge.output.node == node &&
                edge.output == port;
            });
            if (edgeIndex == -1) { return null; }

            var result = edgeList[edgeIndex].input.node;
            if (result.name == REFERENCE_NAME)
            {
                result = (result as ReferenceNode).stateNode;
            }
            return result as StateNode;


        }
        public void ConnectEdge(Node startNode, Node nextNode, int index)
        {
            Port inputPort = nextNode.inputContainer[0] as Port;
            Port outputPort = startNode.outputContainer[index] as Port;

            NodeEdge edge = new NodeEdge
            {
                input = inputPort,
                output = outputPort
            };
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            AddElement(edge);

        }

        public void RemoveEdgeWithPort(Node node, Port port)
        {
            UnityEditor.Experimental.GraphView.Edge targetEdge = null;
            edges.ToList().ForEach(edge =>
            {
                if (edge.output == port)
                {
                    targetEdge = edge;
                }
            });
            if (targetEdge == null) return;
            var stateEdge = GetStateEdgeFomNodeEdge(targetEdge);
          

            if(targetEdge.input.node.name == REFERENCE_NAME)
            {
                var refNode = targetEdge.input.node as ReferenceNode;
                var index = GetEdgeIndex(stateEdge.Value);
                refNode.reference.EdgeIndices.Remove(index);
                refNode.reference.ShiftEdgeIndicesGreaterThan(index);
            }

          

            if (stateEdge.HasValue)
            {
                foreach (var localNode in nodes)
                {
                    if (localNode.name == REFERENCE_NAME)
                    {
                        var refNode = localNode as ReferenceNode;
                        var index = GetEdgeIndex(stateEdge.Value);

                        refNode.reference.ShiftEdgeIndicesGreaterThan(index);
                    }
                }
                EdgeRemovalAction(stateEdge.Value);
            }
            RemoveElement(targetEdge);
        }

        public void SetReferenceNodeActive(int prevState, int nextState)
        {
           

        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var portList = ports.ToList();
            var compartiblePorts = new List<Port>();
            foreach (var port in portList)
            {
                if(port != startPort && port.node != startPort.node)
                {
                    compartiblePorts.Add(port);
                }
            }
            
            return compartiblePorts;
        }
        #endregion
        #region statenode
      
        public void UpdateEdges(List<int> edgeIndices, ScriptableObject stateObj)
        {
            foreach(var index in edgeIndices)
            {
                UpdateEdgeAction(index, stateObj);
            }
            
        }
        public ReferenceNode GenerateReferenceNode(Vector2 pos,EdgeReference edgeRef = null, bool addReferenceToManager = true)
        {
            var node = new ReferenceNode(this, edgeRef)
            {
                name = REFERENCE_NAME,
            };

            AddElement(node);
            if (addReferenceToManager)
            {
                AddEdgeReference(node.reference);
            }
            node.SetPosition(new Rect(pos, pos));
            return node;
        }



       
        public StateNode GetStateNodeByIndex(int index)
        {
            var localState = allStates[index];
            var retrievedStateNodes = nodes.ToList().ConvertAll(x => x as StateNode);
            foreach(var node in retrievedStateNodes)
            {
                if (node == null) continue;
                if (node.stateObj == localState)
                {
                    return node;
                    break;
                }
            }
            return null;
        }

        Node GenerateEntryPointNode()
        {
            var node = new Node()
            {
                title = ENTRYNODE_NAME,
                name = ENTRYNODE_NAME,
            };


            node.capabilities &= ~Capabilities.Movable;
            node.capabilities &= ~Capabilities.Deletable;
            var stylesheets = Resources.Load<StyleSheet>("NodeEntry");
            node.styleSheets.Add(stylesheets);
            var port = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = "Next";
            node.outputContainer.Add(port);
            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(200, 200, defaultNodeSize.x, defaultNodeSize.y));
            node.style.color = Color.red;
            return node;
        }

        StateNode CreateStateNode(ScriptableObject state, string GUID)
        {
            var node = new StateNode(this)
            {
                title = state.name,
                stateObj = state,
                GUID = GUID,
            };
            
            allStates.Add(state);
            return node;
        }

        public StateNode GenerateNewStateNode(Type type, Vector2 pos = default)
        {
            var obj = ScriptableObject.CreateInstance(type);
            obj.name = type.Name;
            var node = CreateStateNode(obj, GUID.Generate().ToString());
            
            node.SetPosition(new Rect(pos.x, pos.y, defaultNodeSize.x, defaultNodeSize.y));
            AddElement(node);
            StateAdditionAction(obj);
            return node;

        }

      

        public StateNode LoadStateNode(ScriptableObject state, string GUID, Vector2 position)
        {
            var node = CreateStateNode(state, GUID);
            node.SetPosition(new Rect(position.x, position.y, defaultNodeSize.x, defaultNodeSize.y));
            AddElement(node);
            return node;
        }

        public void MakeNodeEntryState(StateNode node)
        {
            Node entryNode = null;
            nodes.ForEach(x =>
            {
                if (x.name == ENTRYNODE_NAME)
                {
                    entryNode = x as Node;
                }
            });
            ConnectEdge(entryNode, node, 0);
        }

      
        public void SetStateAsVisited(int index)
        {
            var state = allStates[index];
            foreach(var node in nodes)
            {
                if (node.name == ENTRYNODE_NAME || node.name == REFERENCE_NAME)
                {
                    continue;
                }

                var stateNode = node as StateNode;
                if (stateNode.stateObj == state )
                {
                    stateNode.name = "visited";
                }
            }
          
        }

        public void SetStateAsLeft(int index)
        {
            var state = allStates[index];
            foreach(var node in nodes)  
            {

                if (node.name == ENTRYNODE_NAME || node.name == REFERENCE_NAME)
                {
                    continue;
                }
                var stateNode = node as StateNode;
                if (stateNode.stateObj == state )
                {
                    stateNode.name = "left";
                }
            }
           
        }

        public void SetStateAsUnvisited(int index)
        {
            var state = allStates[index];
            foreach(var node in nodes)  
            {
                if(node.name == ENTRYNODE_NAME || node.name == REFERENCE_NAME)
                {
                    continue;
                }
                var stateNode = node as StateNode;
                if (stateNode.stateObj == state)
                {
                    stateNode.name = "";
                }
            }
          
        }
        #endregion
        #region blackboard-and-conditions

        public Blackboard CreateBlackBoard()
        {
            var blackboard = new Blackboard(this);

            blackboard.Add(new BlackboardSection()
            {
                title = "Conditions"
            });
            blackboard.editTextRequested = (_blackboard, element, newValue) =>
            {
                var oldProperty = ((StateBlackboardField)element);
                var oldPropertyName = ((BlackboardField)element).text;
              
                var targetIndex = this.allConditions.FindIndex(x => x == oldProperty.condition);
                allConditions[targetIndex].name = newValue;

                ((BlackboardField)element).text = newValue;
                ChangeAllStateNodeDropdown();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };
            var styleSheet = Resources.Load<StyleSheet>("Blackboard");
            blackboard.SetPosition(new Rect(10, 30, 200, 300));
            blackboard.styleSheets.Add(styleSheet);
            return blackboard;
        }

        public void ChangeAllStateNodeDropdown()
        {
            var nodes = this.nodes.ToList().ConvertAll( x => x as StateNode);
            foreach(var node in nodes)
            {
                if(node != null)
                    node.UpdateDropDown(ConditionNames);
            }
        }

        VisualElement CreateCondition(ScriptableObject condition)
        {
            var container = new VisualElement();
            var name = condition.name;
            var blackboardField = new StateBlackboardField
            {
                condition = condition,
                action = ElementSelectionAction,
                text = name,
            };
            var button = new Button(() =>
            {
                RemoveConditionAction(condition);
                blackboard.Remove(container);
            });
            button.text = "delete";
            var styleSheet = Resources.Load<StyleSheet>("Blackboard");
            var blackboardRow = new BlackboardRow(blackboardField, button);
            container.Add(blackboardRow);
            blackboardField.styleSheets.Add(styleSheet);

            return container;

        }
        public void LoadConditionIntoBlackboard()
        {
            foreach(var condition in allConditions)
            {
                blackboard.Add(CreateCondition(condition));
            }
        }

        public void GenerateCondition(Type type)
        {
            var obj = ScriptableObject.CreateInstance(type);
            obj.name = type.Name;
            blackboard.Add(CreateCondition(obj));

            allConditions.Add(obj);
            AddConditionAction(obj);
            
            ChangeAllStateNodeDropdown();
        }
        #endregion



        public void ClearGraph()
        {
            var nodes = this.nodes.ToList();
            var edges = this.edges.ToList();
            allConditions.Clear();
            allStates.Clear();
            blackboard.Clear();
            foreach (var edge in edges)
            {
                RemoveElement(edge);
            }
            foreach (var node in nodes)
            {
                if(node.name == "ENTRY") { continue; }
                RemoveElement(node);
            }

        }

    }
}
