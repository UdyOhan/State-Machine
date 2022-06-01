using UnityEngine.UIElements;
using UnityEngine;

namespace StateMachine.Editor
{
    public class InspectorView : VisualElement
    {
        UnityEditor.Editor editor;

        public void DisplayElement(ScriptableObject obj)
        {
            Clear();
            Debug.Log("clear");
            Object.DestroyImmediate(editor);
            if (obj == null)
                return;
            editor = UnityEditor.Editor.CreateEditor(obj);
            IMGUIContainer container = new IMGUIContainer(() => editor.OnInspectorGUI());
            Add(container);
        }
    }
}
