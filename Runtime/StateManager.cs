using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StateMachine
{
    #region editor-interface
#if UNITY_EDITOR
    /// <summary>
    /// This is only for editor purpose
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEditorStateManager<out T> where T : Component
    {
        public void CreateStateEditor(Type type, string name);
        public void CreateConditionEditor(Type type);
        public Type GetStateType();
        public Type GetConditionType();
        public int stateTypeIndex { get; set; }
        public int conditionTypeIndex { get; set; }
        public int currentStateIndex { get; set; }
        public int nextStateIndex { get; set; }
        public int conditionIndex { get; set; }
        public string stateName { get; set; }
        public string conditionName { get; set; }
        public object[] States { get; set; }
        public object[] Conditions { get; set; }
        public int EntryIndex { get; set; } 
        public bool EdgeFoldout { get; set; }
        public List<Edge> Edges { get; set; }
        public int? GetRunnerCurrentIndex { get; }
        public int? GetRunnerPreviousIndex { get; }
        public List<EdgeReference> EdgeReferences { get; set; }
    }
#endif
    #endregion
    [Serializable]
    public struct Edge
    {

        public int conditionIndex;
        public int prevStateIndex;
        public int nextStateIndex;


        
        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(Edge))
                return false;
            Edge other = (Edge)obj;
            return other.conditionIndex.Equals(conditionIndex) && other.prevStateIndex.Equals(prevStateIndex) && other.nextStateIndex.Equals(nextStateIndex) ;
        }

        public static bool operator ==(Edge condition1, Edge condition2)
        {
            return condition1.Equals(condition2);
        }

        public static bool operator !=(Edge condition1, Edge condition2)
        {
            return !condition1.Equals(condition2);
        }
    }

#if UNITY_EDITOR
    [Serializable]
    public class EdgeReference
    {
        public int StateIndex;
        public List<int> EdgeIndices;
        public Vector2 position;

        public void ShiftEdgeIndicesGreaterThan(int edgeIndex)
        {
            if (EdgeIndices.Contains(edgeIndex)) return;
            for(int i = 0; i < EdgeIndices.Count; i++)
            {
                if(EdgeIndices[i] > edgeIndex)
                {
                    EdgeIndices[i] = EdgeIndices[i] - 1;
                }
            }
        }
    }
#endif

    public class StateManager<T> : ScriptableObject
        #region editor-interface-inheritance
#if UNITY_EDITOR
        , IEditorStateManager<T>
#endif
        #endregion
        where T : Component
    {
        [SerializeField] List<State<T>> states = new List<State<T>>();
        [SerializeField] List<Condition<T>> conditions = new List<Condition<T>>();
        [SerializeField, HideInInspector]List<Edge> edges = new List<Edge>();
        [SerializeField, HideInInspector]public int entryIndex = 0;
        Dictionary<State<T>, List<Edge>> edgeMap = new Dictionary<State<T>, List<Edge>>();

        public int EdgeMapCount => edgeMap.Count;

       

        public void Execute(T param, ref int stateIndex, ref bool stateStarted)
        {
            var currentState = states[stateIndex];
            bool conditionMet = false;
            Edge edge = default;
            if (edgeMap.ContainsKey(currentState))
            {
                var stateEdges = edgeMap[currentState];
               
                foreach (var stateEdge in stateEdges)
                {
                    conditionMet = conditions[stateEdge.conditionIndex].Check(param);
                    if (conditionMet)
                    {
                        edge = stateEdge;
                        break;
                    }

                }
            }
           

            if (!stateStarted)
            {
                currentState.OnStart(param);
                stateStarted = true;
            }
            else if (!conditionMet)
            {
                currentState.OnUpdate(param);
            }
            else
            {
                currentState.OnUpdate(param);
                currentState.OnEnd(param);
                stateIndex = edge.nextStateIndex;
                stateStarted = false;
            }

        }

        public void ExecuteGizmos(T param, int stateIndex)
        {
            var currentState = states[stateIndex];
            currentState.OnGizmos(param);
        }

        public void ExecuteAllGizmos(T param)
        {
            foreach(var state in states)
            {
                state.OnGizmos(param);
            }
        }

        public void AddState(State<T> state)
        {
            if (states.Contains(state))
            {
                return;
            }
            states.Add(state);

            edgeMap.Add(state, new List<Edge> { });
        }

        public void AddCondition(Condition<T> condition)
        {
            if (conditions.Contains(condition))
            {
                return;
            }
            conditions.Add(condition);

        }

        public void AddEdge(Condition<T> condition, State<T> currentState, State<T> nextState)
        {
            if (!states.Contains(currentState) || !states.Contains(nextState) || !conditions.Contains(condition))
                return;
            var link = new Edge()
            {
                conditionIndex = conditions.IndexOf(condition),
                prevStateIndex = states.IndexOf(currentState),
                nextStateIndex = states.IndexOf(nextState),

            };
            if (edges.Contains(link))
                return;
            edges.Add(link);
        }

        public void CreateEdgeMap()
        {
            edgeMap.Clear();
            foreach(var link in edges)
            {
                if (edgeMap.ContainsKey(states[link.prevStateIndex]))
                {
                    if(!edgeMap[states[link.prevStateIndex]].Contains(link))
                        edgeMap[states[link.prevStateIndex]].Add(link);
                }
                else
                {
                    edgeMap.Add(states[link.prevStateIndex], new List<Edge>() { link});
                }
            }
        }

        public void RemoveState(State<T> state)
        {
            if (states.Contains(state))
            {
                states.Remove(state);
              
            }
        }

        public void RemoveConditionState(Condition<T> condition, State<T> currentState, State<T> nextState)
        {
            var conditionState = new Edge { conditionIndex = conditions.IndexOf(condition),
                nextStateIndex = states.IndexOf(nextState) };

            var elementsAffectedInLIst = conditions;
            var elementsAffectedInMap =  edgeMap[currentState].RemoveAll(x => x == conditionState);

        }

        public virtual Type GetStateType()
        {
            return typeof(State<T>);
        }

        public virtual Type GetConditionType()
        {
            return typeof(Condition<T>);
        }

        public (Type stateInnerType, Type conditionInnerType) GetInnerTypes()
        {
            return (typeof(T), typeof(T));
        }

        public void Clear()
        {
            states.Clear();
            conditions.Clear();
            edgeMap.Clear();
        }
        #region Editor
#if UNITY_EDITOR
        public int stateTypeIndex { get; set; }
        public int conditionTypeIndex { get; set; }
        public string stateName { get; set; }
        public string conditionName { get; set; }
        public int currentStateIndex { get; set; }
        public int nextStateIndex { get; set; } 
        public int conditionIndex { get; set; }
        public StateRunner<T> selectedRunner { get; set; }
        [SerializeField] List<EdgeReference> edgeReferences = new List<EdgeReference>();
        public List<EdgeReference> EdgeReferences { get => edgeReferences; set => edgeReferences = value; }
        public int? GetRunnerCurrentIndex => selectedRunner?.CurrentIndex;
        public int? GetRunnerPreviousIndex => selectedRunner?.PreviousIndex;
        public object[] States { 
            get 
            { 
                return states.ToArray();
            }  
            set 
            {
                states.Clear();
                var stateObj = value;
                foreach(var state in stateObj)
                {
                    states.Add(state as State<T>);
                }
            } 
        }
        public object[] Conditions
        {
            get
            {
                return conditions.ToArray();
            }
            set
            {
                conditions.Clear();
                var conditionsObj = value;
                foreach(var condition in conditionsObj)
                {
                    conditions.Add(condition as Condition<T>);
                }
            }
        }
        public int EntryIndex { get { return entryIndex; } set { entryIndex = value; } }
        public bool EdgeFoldout { get; set; }
        public List<Edge> Edges { get { return edges; } set { edges = value; } }


        string ParseText(string name)
        {
            var matches = Regex.Matches(name, "[A-Z]+[a-z]*");
            StringBuilder str = new StringBuilder();
            foreach (var match in matches)
            {
                str.Append(match.ToString());
                str.Append(' ');
            }
            return str.ToString();
        }

        public void CreateStateEditor(Type type, string name)
        {
            var typeName = type.Name;
            var stateObj = CreateInstance(type);
            if (string.IsNullOrEmpty(name))
            {
                stateObj.name = ParseText(typeName);
            }
            else
            {
                stateObj.name = name;
            }

            AssetDatabase.AddObjectToAsset(stateObj, this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(stateObj);
            var state = stateObj as State<T>;
            AddState(state);
        }

        public void CreateConditionEditor(Type type)
        {
            var typeName = type.Name;
            var conditionObj = CreateInstance(type);
            conditionObj.name = ParseText(typeName);
            AssetDatabase.AddObjectToAsset(conditionObj, this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(conditionObj);
            var condition = conditionObj as Condition<T>;
            AddCondition(condition);
        }



#endif
        #endregion
    }

    #region custom-editor
#if UNITY_EDITOR
    [CustomEditor(typeof(StateManager<>), true)]
    public class StateManagerEditor : Editor 
    {
        List<string> derivedStateTypeNames;
        List<string> derivedConditionTypeNames;

        TypeCache.TypeCollection derivedStateTypes;
        TypeCache.TypeCollection derivedConditionTypes;

        object[] states;
        object[] conditions;

        List<string> stateNames;
        List<string> conditionNames;

        IEditorStateManager<Component> manager;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Setup();
            CreateLinkInspector();
            CreateEditorInspector();
           
        }

        private void Setup()
        {
            manager = (IEditorStateManager<Component>)target;
            var stateType = manager.GetStateType();
            var conditionType = manager.GetConditionType();

            derivedStateTypes = TypeCache.GetTypesDerivedFrom(stateType);
            derivedConditionTypes = TypeCache.GetTypesDerivedFrom(conditionType);

            states = manager.States;
            conditions = manager.Conditions;

            derivedStateTypeNames = new List<string>();
            derivedConditionTypeNames = new List<string>();

            derivedStateTypeNames.Add("None");
            foreach (var derivedStateType in derivedStateTypes)
            {
                derivedStateTypeNames.Add(derivedStateType.Name);
            }

            derivedConditionTypeNames.Add("None");
            foreach (var derivedConditionType in derivedConditionTypes)
            {
                derivedConditionTypeNames.Add(derivedConditionType.Name);
            }

            stateNames = new List<string>();
            stateNames.Add("None");
            Array.ForEach(states, state => { if (state != null) stateNames.Add(((ScriptableObject)state).name); });

            conditionNames = new List<string>();
            conditionNames.Add("None");
            Array.ForEach(conditions, condition => { if (condition != null) conditionNames.Add(((ScriptableObject)condition).name); });

        }

        private void CreateLinkInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUIStyle style = new GUIStyle(EditorStyles.foldout);
            style.fontStyle = FontStyle.Bold;
            manager.EdgeFoldout = EditorGUILayout.Foldout(manager.EdgeFoldout, "Edges", true, style);
            var edges = manager.Edges;
            var size = Mathf.Max(0, EditorGUILayout.IntField(edges.Count, GUILayout.MaxWidth(50)));

            while(edges.Count > size)
            {
                edges.RemoveAt(edges.Count - 1);
            }


            while(edges.Count < size)
            {
                if (edges.Count > 0)
                    edges.Add(edges[edges.Count - 1]);
                else
                    edges.Add(new Edge());
            }
            EditorGUILayout.EndHorizontal();

            var styleState = new GUIStyleState();
            styleState.background = Texture2D.whiteTexture;
            var boxHoverStyle = new GUIStyle();
            boxHoverStyle.active.background = Texture2D.whiteTexture;
            boxHoverStyle.onActive.background = Texture2D.whiteTexture;



            if (manager.EdgeFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                for (int i = 0; i < edges.Count; i++)
                {
                    Debug.Log("In");
                    EditorGUI.indentLevel++;
                    var link = edges[i];
                   
                    EditorGUILayout.BeginHorizontal(boxHoverStyle);
                   
                    EditorGUILayout.LabelField("Condition", GUILayout.MaxWidth(100));
                    link.conditionIndex = EditorGUILayout.Popup(link.conditionIndex + 1, conditionNames.ToArray()) - 1;

                    EditorGUILayout.LabelField("Start State", GUILayout.MaxWidth(100));
                    link.prevStateIndex = EditorGUILayout.Popup(link.prevStateIndex + 1, stateNames.ToArray()) - 1;

                    EditorGUILayout.LabelField("Next State", GUILayout.MaxWidth(100));
                    link.nextStateIndex = EditorGUILayout.Popup(link.nextStateIndex + 1, stateNames.ToArray()) - 1;
                    EditorGUILayout.EndHorizontal();

                    edges[i] = link;
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
          

            EditorGUILayout.BeginVertical();
            manager.EntryIndex = EditorGUILayout.Popup(new GUIContent("Entry State"), manager.EntryIndex + 1, stateNames.ToArray()) - 1;
            EditorGUILayout.EndVertical();
        }

        void CreateEditorInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
          

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("State Creation", EditorStyles.boldLabel);
            manager.stateTypeIndex = EditorGUILayout.Popup(new GUIContent("State Type"), manager.stateTypeIndex, derivedStateTypeNames.ToArray());

            manager.stateName = EditorGUILayout.TextField(new GUIContent("State Name"), manager.stateName);
            if (manager.stateTypeIndex != 0)
            {
                if (GUILayout.Button("Create State", GUILayout.MaxWidth(150)))
                {
                    manager.CreateStateEditor(derivedStateTypes[manager.stateTypeIndex - 1], manager.stateName);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Condition Creation", EditorStyles.boldLabel);
            manager.conditionTypeIndex = EditorGUILayout.Popup(new GUIContent("Condition Type"), manager.conditionTypeIndex, derivedConditionTypeNames.ToArray());

            if (manager.conditionTypeIndex != 0)
            {
                if (GUILayout.Button("Create Condition", GUILayout.MaxWidth(150)))
                {
                    manager.CreateConditionEditor(derivedConditionTypes[manager.conditionTypeIndex - 1]);
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
          
           
            EditorGUILayout.EndVertical();
        }
    }
#endif
    #endregion
}
